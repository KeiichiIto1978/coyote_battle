using System;
using System.Collections;
using System.Linq;
using CoyoteBattle.Application;
using CoyoteBattle.Domain;
using CoyoteBattle.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using static CoyoteBattle.Tests.Presentation.NpcActionPresentationTestSupport;

namespace CoyoteBattle.Tests.Presentation
{
    /// <summary>
    /// 対戦画面のカード情報表示と、開閉中の操作制御を保証します。
    /// </summary>
    public sealed class CardInformationPresentationTests
    {
        /// <summary>
        /// 各テストで生成したPresentationオブジェクトを破棄します。
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

            yield return null;
        }

        /// <summary>
        /// 全15表示単位と合計を表示し、開閉しても対戦状態とパネル数を維持することを保証します。
        /// </summary>
        [UnityTest]
        public IEnumerator CardInformation_ユーザー手番で開閉する_全カードを表示して同じ手番へ戻る()
        {
            var setup = CreateController(
                0,
                new ManualPresentationDelay(),
                new SequentialNumberExecutor()
            );
            yield return null;
            Click(setup.Root.Q<Button>("start-game-button"));
            yield return null;

            var roundBefore = setup.Root.Q<Label>("round-label").text;
            var statusBefore = setup.Root.Q<Label>("status-label").text;
            setup.Root.Q<TextField>("number-input").value = "123";
            Click(setup.Root.Q<Button>("card-information-button"));
            yield return null;

            var panel = setup.Root.Q<VisualElement>("card-information-panel");
            var labels = panel.Query<Label>().ToList().Select(label => label.text).ToList();
            Assert.That(panel.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));
            var rows = setup.Root.Query<VisualElement>("card-information-row").ToList();
            Assert.That(rows, Has.Count.EqualTo(15));
            Assert.That(rows.All(row => row.childCount == 3), Is.True);
            Assert.That(labels, Does.Contain("0"));
            Assert.That(labels, Does.Contain("0★"));
            Assert.That(labels, Does.Contain("初期枚数合計：36\n使用済み合計：0"));
            Assert.That(setup.Root.Q<TextField>("number-input").enabledSelf, Is.False);
            Assert.That(setup.Root.Q<Button>("declare-number-button").enabledSelf, Is.False);
            Assert.That(setup.Root.Q<Button>("declare-coyote-button").enabledSelf, Is.False);

            Click(setup.Root.Q<Button>("card-information-close-button"));
            yield return null;
            Assert.That(panel.resolvedStyle.display, Is.EqualTo(DisplayStyle.None));
            Assert.That(setup.Root.Q<Label>("round-label").text, Is.EqualTo(roundBefore));
            Assert.That(setup.Root.Q<Label>("status-label").text, Is.EqualTo(statusBefore));
            Assert.That(setup.Root.Q<TextField>("number-input").value, Is.EqualTo("123"));
            Assert.That(setup.Root.Q<TextField>("number-input").enabledSelf, Is.True);
            Assert.That(setup.Root.Q<Button>("declare-number-button").enabledSelf, Is.True);

            Click(setup.Root.Q<Button>("card-information-button"));
            yield return null;
            Click(setup.Root.Q<Button>("card-information-close-button"));
            yield return null;
            Assert.That(
                setup.Root.Query<VisualElement>("card-information-panel").ToList(),
                Has.Count.EqualTo(1)
            );
        }

        /// <summary>
        /// NPC思考待ちではカード情報を開けず、待機中のNPC進行も維持されることを保証します。
        /// </summary>
        [UnityTest]
        public IEnumerator CardInformation_NPC手番で待機中_ボタンを無効化して進行を止めない()
        {
            var delay = new ManualPresentationDelay();
            var executor = new SequentialNumberExecutor();
            var setup = CreateController(1, delay, executor);
            yield return null;
            Click(setup.Root.Q<Button>("start-game-button"));
            yield return WaitForRequest(delay, 0);

            var openButton = setup.Root.Q<Button>("card-information-button");
            Assert.That(openButton.enabledSelf, Is.False);
            Click(openButton);
            yield return null;
            Assert.That(
                setup.Root.Q<VisualElement>("card-information-panel").resolvedStyle.display,
                Is.EqualTo(DisplayStyle.None)
            );
            Assert.That(delay.Seconds, Has.Count.EqualTo(1));
            Assert.That(executor.CallCount, Is.Zero);
        }

        /// <summary>
        /// 通常ラウンド後に、場から回収された5枚だけを使用済み合計へ反映することを保証します。
        /// </summary>
        [UnityTest]
        public IEnumerator CardInformation_通常ラウンド後に次のラウンドを始める_使用済み5枚を表示する()
        {
            var delay = new ManualPresentationDelay();
            var setup = CreateIdentityController(delay);
            yield return null;
            Click(setup.Root.Q<Button>("start-game-button"));
            yield return null;
            setup.Root.Q<TextField>("number-input").value = int.MaxValue.ToString();
            Click(setup.Root.Q<Button>("declare-number-button"));
            yield return WaitForRequest(delay, 0);
            delay.Release(0);
            yield return WaitForRequest(delay, 1);
            delay.Release(1);
            yield return null;
            yield return null;

            var nextRoundButton = setup.Root.Q<Button>("next-round-button");
            Assert.That(nextRoundButton.enabledSelf, Is.True);
            Click(nextRoundButton);
            yield return null;
            yield return null;
            var informationButton = setup.Root.Q<Button>("card-information-button");
            Assert.That(informationButton.enabledSelf, Is.True);
            Click(informationButton);
            yield return null;

            Assert.That(
                setup.Root.Q<Label>("card-information-totals").text,
                Is.EqualTo("初期枚数合計：36\n使用済み合計：5")
            );
        }

        /// <summary>
        /// 山札順を維持したユーザー開始ゲームと、コヨーテを宣言するNPCを構築します。
        /// </summary>
        private static ControllerSetup CreateIdentityController(ManualPresentationDelay delay)
        {
            var gameObject = new GameObject("CardInformationPresentationTest");
            gameObject.SetActive(false);
            var controller = gameObject.AddComponent<GamePresentationController>();
            controller.ConfigureForTests(
                () =>
                    new GameFlowService(
                        new FirstZeroThenIdentityRandomSource(),
                        new FirstZeroThenIdentityRandomSource()
                    ),
                delay,
                new CoyoteExecutor()
            );
            gameObject.SetActive(true);
            return new ControllerSetup(
                gameObject,
                gameObject.GetComponent<UIDocument>().rootVisualElement
            );
        }

        /// <summary>
        /// 指定位置の手動待機要求が発行されるまでフレームを進めます。
        /// </summary>
        private static IEnumerator WaitForRequest(ManualPresentationDelay delay, int index)
        {
            for (var frame = 0; frame < 10 && delay.Seconds.Count <= index; frame++)
            {
                yield return null;
            }

            Assert.That(delay.Seconds.Count, Is.GreaterThan(index));
        }

        /// <summary>
        /// PointerDownとPointerUpを送り、実際のボタン操作を再現します。
        /// </summary>
        private static void Click(Button button)
        {
            var position = button.worldBound.center;
            var downEvent = new Event
            {
                type = EventType.MouseDown,
                button = 0,
                mousePosition = position,
            };
            using (var pointerDown = PointerDownEvent.GetPooled(downEvent))
            {
                button.SendEvent(pointerDown);
            }

            var upEvent = new Event
            {
                type = EventType.MouseUp,
                button = 0,
                mousePosition = position,
            };
            using (var pointerUp = PointerUpEvent.GetPooled(upEvent))
            {
                button.SendEvent(pointerUp);
            }
            button.ReleasePointer(PointerId.mousePointerId);
        }

        private sealed class FirstZeroThenIdentityRandomSource : IRandomSource
        {
            private bool _hasReturnedStarter;

            /// <summary>
            /// 最初はユーザー位置を返し、その後のシャッフルでは順序を維持します。
            /// </summary>
            public int Next(int maxExclusive)
            {
                if (!_hasReturnedStarter)
                {
                    _hasReturnedStarter = true;
                    return 0;
                }

                return maxExclusive - 1;
            }
        }
    }
}
