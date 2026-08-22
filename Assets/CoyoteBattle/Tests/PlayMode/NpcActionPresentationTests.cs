using System.Collections;
using CoyoteBattle.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using static CoyoteBattle.Tests.Presentation.NpcActionPresentationTestSupport;

namespace CoyoteBattle.Tests.Presentation
{
    /// <summary>
    /// NPCの思考と行動が時間順に一度ずつ表示され、保留処理が安全に停止することを保証します。
    /// </summary>
    public sealed class NpcActionPresentationTests
    {
        /// <summary>
        /// テスト間でコントローラーと表示用カメラが残らないよう破棄します。
        /// </summary>
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (
                var controller in UnityEngine.Object.FindObjectsByType<GamePresentationController>(
                    FindObjectsSortMode.None
                )
            )
            {
                UnityEngine.Object.Destroy(controller.gameObject);
            }

            foreach (
                var camera in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None)
            )
            {
                if (camera.name == "CoyoteBattleCamera")
                {
                    UnityEngine.Object.Destroy(camera.gameObject);
                }
            }

            yield return null;
        }

        /// <summary>
        /// 4人のNPCが連続して数字を宣言しても、各思考と各宣言を順番どおり一度ずつ表示することを保証します。
        /// </summary>
        [UnityTest]
        public IEnumerator ExecuteNpcTurns_Npcが4手連続する_考え中と各数字宣言を順番に一度ずつ表示する()
        {
            var delay = new ManualPresentationDelay();
            var executor = new SequentialNumberExecutor();
            var setup = CreateController(1, delay, executor);
            yield return null;

            Click(setup.Root.Q<Button>("start-game-button"));
            for (var index = 0; index < 4; index++)
            {
                yield return WaitForRequest(delay, index * 2);
                var participantNumber = index + 1;
                Assert.That(delay.Seconds[index * 2], Is.EqualTo(0.8f));
                Assert.That(
                    setup.Root.Q<Label>("status-label").text,
                    Does.Contain($"NPC {participantNumber}").And.Contain("考え中")
                );
                Assert.That(
                    setup
                        .Root.Q<VisualElement>($"npc-{participantNumber}")
                        .resolvedStyle.borderTopWidth,
                    Is.GreaterThan(0f)
                );
                AssertInputEnabled(setup.Root, false);

                delay.Release(index * 2);
                yield return WaitForRequest(delay, index * 2 + 1);
                Assert.That(delay.Seconds[index * 2 + 1], Is.EqualTo(1f));
                Assert.That(
                    setup.Root.Q<Label>("action-banner").text,
                    Is.EqualTo(
                        $"NPC {participantNumber}（{Personality(participantNumber)}）：{participantNumber}を宣言！"
                    )
                );
                Assert.That(executor.CallCount, Is.EqualTo(participantNumber));
                AssertInputEnabled(setup.Root, false);

                delay.Release(index * 2 + 1);
            }

            yield return AdvanceFrames(3);
            Assert.That(setup.Root.Q<Label>("status-label").text, Is.EqualTo("あなたの手番"));
            Assert.That(
                setup.Root.Q<Label>("declaration-label").text,
                Does.Contain("現在の宣言値：4").And.Contain("直前の宣言者：NPC 4（分析）")
            );
            Assert.That(setup.Root.Q<Label>("action-banner").text, Does.Contain("NPC 4"));
            Assert.That(setup.Root.Query<Label>("action-banner").ToList(), Has.Count.EqualTo(1));
            AssertInputEnabled(setup.Root, true);
            UnityEngine.Object.Destroy(setup.GameObject);
        }

        /// <summary>
        /// NPCのコヨーテ表示を1秒保持し終えるまで結果画面へ遷移しないことを保証します。
        /// </summary>
        [UnityTest]
        public IEnumerator ExecuteNpcTurns_Npcがコヨーテを宣言する_1秒保持後に結果を優先表示する()
        {
            var delay = new ManualPresentationDelay();
            var executor = new CoyoteExecutor();
            var setup = CreateController(0, delay, executor);
            yield return null;

            Click(setup.Root.Q<Button>("start-game-button"));
            yield return null;
            setup.Root.Q<TextField>("number-input").value = "1";
            Click(setup.Root.Q<Button>("declare-number-button"));
            yield return WaitForRequest(delay, 0);
            delay.Release(0);
            yield return WaitForRequest(delay, 1);

            Assert.That(delay.Seconds[1], Is.EqualTo(1f));
            Assert.That(
                setup.Root.Q<Label>("action-banner").text,
                Is.EqualTo("NPC 1（強気）：コヨーテ！")
            );
            Assert.That(
                setup.Root.Q<VisualElement>("battle-screen").style.display.value,
                Is.EqualTo(DisplayStyle.Flex)
            );
            Assert.That(
                setup.Root.Q<VisualElement>("round-result-screen").style.display.value,
                Is.EqualTo(DisplayStyle.None)
            );

            delay.Release(1);
            yield return AdvanceFrames(3);
            Assert.That(
                setup.Root.Q<VisualElement>("round-result-screen").style.display.value,
                Is.EqualTo(DisplayStyle.Flex)
            );
            Assert.That(setup.Root.Q<Label>("result-loser").text, Does.StartWith("敗者"));
            Assert.That(setup.Root.Q<Label>("result-total").text, Does.StartWith("実合計"));
            Assert.That(
                setup.Root.Q<Label>("result-declaration").text,
                Does.Contain("最終宣言値").And.Contain("最終宣言者").And.Contain("コヨーテ宣言者")
            );
            UnityEngine.Object.Destroy(setup.GameObject);
        }

        /// <summary>
        /// NPCのコヨーテでユーザーが脱落する場合も、1秒保持後にGameOverへ進み、再戦で保留表示を破棄することを保証します。
        /// </summary>
        [UnityTest]
        public IEnumerator ExecuteNpcTurns_ユーザーが3回敗北する_保持後にGameOverを表示して再戦状態を初期化する()
        {
            var delay = new ManualPresentationDelay();
            var setup = CreateController(0, delay, new CoyoteExecutor());
            yield return null;

            Click(setup.Root.Q<Button>("start-game-button"));
            yield return null;
            for (var roundIndex = 0; roundIndex < 3; roundIndex++)
            {
                setup.Root.Q<TextField>("number-input").value = int.MaxValue.ToString();
                Click(setup.Root.Q<Button>("declare-number-button"));
                var thinkingIndex = roundIndex * 2;
                yield return WaitForRequest(delay, thinkingIndex);
                delay.Release(thinkingIndex);
                yield return WaitForRequest(delay, thinkingIndex + 1);
                Assert.That(
                    setup.Root.Q<VisualElement>("game-over-dialog").style.display.value,
                    Is.EqualTo(DisplayStyle.None)
                );
                delay.Release(thinkingIndex + 1);
                yield return AdvanceFrames(3);

                if (roundIndex < 2)
                {
                    Assert.That(
                        setup.Root.Q<VisualElement>("round-result-screen").style.display.value,
                        Is.EqualTo(DisplayStyle.Flex)
                    );
                    Click(setup.Root.Q<Button>("next-round-button"));
                    yield return null;
                }
            }

            Assert.That(
                setup.Root.Q<VisualElement>("game-over-dialog").style.display.value,
                Is.EqualTo(DisplayStyle.Flex)
            );
            Assert.That(setup.Root.Q<Label>("outcome-label").text, Is.EqualTo("敗北…"));
            Assert.That(setup.Root.Q<Label>("result-loser").text, Does.Contain("あなた"));

            Click(setup.Root.Q<Button>("restart-button"));
            yield return null;
            Assert.That(
                setup.Root.Q<VisualElement>("game-over-dialog").style.display.value,
                Is.EqualTo(DisplayStyle.None)
            );
            Assert.That(setup.Root.Q<Label>("action-banner").text, Is.Empty);
            Assert.That(setup.Root.Q<Label>("round-label").text, Is.EqualTo("ROUND 1"));
            UnityEngine.Object.Destroy(setup.GameObject);
        }

        /// <summary>
        /// ApplicationがNPC行動を拒否した場合に同じ手番を再試行し続けないことを保証します。
        /// </summary>
        [UnityTest]
        public IEnumerator ExecuteNpcTurns_Applicationが行動を拒否する_一度だけ呼び出して停止する()
        {
            var delay = new ManualPresentationDelay();
            var executor = new RejectingExecutor();
            var setup = CreateController(1, delay, executor);
            yield return null;

            Click(setup.Root.Q<Button>("start-game-button"));
            yield return WaitForRequest(delay, 0);
            delay.Release(0);
            yield return AdvanceFrames(5);

            Assert.That(executor.CallCount, Is.EqualTo(1));
            Assert.That(delay.Seconds, Has.Count.EqualTo(1));
            Assert.That(
                setup.Root.Q<Label>("status-label").text,
                Does.Contain("完了できませんでした")
            );
            AssertInputEnabled(setup.Root, false);
            UnityEngine.Object.Destroy(setup.GameObject);
        }

        /// <summary>
        /// 思考待機中に画面を破棄した場合、保留していたNPC行動をApplicationへ送らないことを保証します。
        /// </summary>
        [UnityTest]
        public IEnumerator ExecuteNpcTurns_思考待機中に画面を破棄する_保留行動を実行しない()
        {
            var delay = new ManualPresentationDelay();
            var executor = new SequentialNumberExecutor();
            var setup = CreateController(1, delay, executor);
            yield return null;

            Click(setup.Root.Q<Button>("start-game-button"));
            yield return WaitForRequest(delay, 0);
            UnityEngine.Object.Destroy(setup.GameObject);
            yield return null;
            delay.Release(0);
            yield return AdvanceFrames(3);

            Assert.That(executor.CallCount, Is.Zero);
        }

        /// <summary>
        /// 指定位置の待機要求が登録されるまでフレームを進めます。
        /// </summary>
        private static IEnumerator WaitForRequest(ManualPresentationDelay delay, int index)
        {
            var timeoutAt = Time.realtimeSinceStartup + 2f;
            while (delay.Seconds.Count <= index && Time.realtimeSinceStartup < timeoutAt)
            {
                yield return null;
            }

            Assert.That(delay.Seconds.Count, Is.GreaterThan(index));
        }

        /// <summary>
        /// Coroutineの親子Enumeratorが次の状態へ進むためのフレームを供給します。
        /// </summary>
        private static IEnumerator AdvanceFrames(int count)
        {
            for (var index = 0; index < count; index++)
            {
                yield return null;
            }
        }

        /// <summary>
        /// 入力欄と宣言ボタンが同じ操作可否になっていることを検証します。
        /// </summary>
        private static void AssertInputEnabled(VisualElement root, bool enabled)
        {
            Assert.That(root.Q<TextField>("number-input").enabledSelf, Is.EqualTo(enabled));
            Assert.That(root.Q<Button>("declare-number-button").enabledSelf, Is.EqualTo(enabled));
        }

        /// <summary>
        /// UI Toolkitのポインターイベントでボタンを1回押します。
        /// </summary>
        private static void Click(Button button)
        {
            var position = button.worldBound.center;
            var down = new Event
            {
                type = EventType.MouseDown,
                button = 0,
                mousePosition = position,
            };
            using (var pointerDown = PointerDownEvent.GetPooled(down))
            {
                button.SendEvent(pointerDown);
            }

            var up = new Event
            {
                type = EventType.MouseUp,
                button = 0,
                mousePosition = position,
            };
            using (var pointerUp = PointerUpEvent.GetPooled(up))
            {
                button.SendEvent(pointerUp);
            }

            button.ReleasePointer(PointerId.mousePointerId);
        }

        /// <summary>
        /// 既定NPC番号に対応する表示用性格名を返します。
        /// </summary>
        private static string Personality(int participantNumber)
        {
            switch (participantNumber)
            {
                case 1:
                    return "強気";
                case 2:
                    return "慎重";
                case 3:
                    return "ギャンブル";
                default:
                    return "分析";
            }
        }
    }
}
