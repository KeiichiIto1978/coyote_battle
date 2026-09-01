using System.Collections;
using System.Collections.Generic;
using CoyoteBattle.Application;
using CoyoteBattle.Domain;
using CoyoteBattle.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CoyoteBattle.Tests.Presentation
{
    /// <summary>
    /// NPC行動バナーと結果の優先情報を実際のGame View解像度とSafe Areaで保証します。
    /// </summary>
    public sealed class NpcActionResolutionPresentationTests
    {
        private Vector2Int? _originalResolution;

        /// <summary>
        /// テスト用UIを破棄し、Game Viewの描画解像度を元へ戻します。
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

            if (_originalResolution.HasValue)
            {
                SetRenderingResolution(_originalResolution.Value);
            }

            yield return null;
        }

        /// <summary>
        /// 対象3解像度とSafe Areaで行動バナーと結果情報が欠落・重複しないことを保証します。
        /// </summary>
        [UnityTest]
        public IEnumerator NpcAction_検証解像度とSafeArea相当余白_行動と結果の優先情報が重ならない()
        {
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                Assert.Ignore("Game View解像度の検証にはグラフィックスデバイスが必要です。");
            }

            var delay = new ManualDelay();
            var gameObject = new GameObject("NpcActionResolutionTest");
            gameObject.SetActive(false);
            var controller = gameObject.AddComponent<GamePresentationController>();
            controller.ConfigureForTests(
                () =>
                    new GameFlowService(
                        new FirstValueThenZeroRandomSource(1),
                        new ZeroRandomSource()
                    ),
                delay,
                new MaxThenCoyoteExecutor()
            );
            gameObject.SetActive(true);
            yield return null;
            var root = gameObject.GetComponent<UIDocument>().rootVisualElement;
            Click(root.Q<Button>("start-game-button"));
            yield return WaitForRequest(delay, 0);
            delay.Release(0);
            yield return WaitForRequest(delay, 1);
            Assert.That(
                root.Q<Label>("action-banner").text,
                Is.EqualTo($"NPC 1（強気）：{int.MaxValue}を宣言！")
            );

            _originalResolution = GetRenderingResolution();
            var resolutions = new[]
            {
                new Vector2Int(1280, 720),
                new Vector2Int(1920, 1080),
                new Vector2Int(2400, 1080),
                new Vector2Int(2520, 1080),
            };
            foreach (var resolution in resolutions)
            {
                SetRenderingResolution(resolution);
                yield return null;
                ApplySafeArea(root, resolution);
                yield return null;
                AssertBattleLayout(root, resolution);
            }

            delay.Release(1);
            yield return WaitForRequest(delay, 2);
            delay.Release(2);
            yield return WaitForRequest(delay, 3);
            Assert.That(
                root.Q<Label>("action-banner").text,
                Is.EqualTo("NPC 2（慎重）：コヨーテ！")
            );
            delay.Release(3);
            yield return AdvanceFrames(3);
            foreach (var resolution in resolutions)
            {
                SetRenderingResolution(resolution);
                yield return null;
                ApplySafeArea(root, resolution);
                yield return null;
                AssertResultLayout(root, resolution);
            }

            UnityEngine.Object.Destroy(gameObject);
        }

        /// <summary>
        /// Safe Areaを本番と同じ変換処理でUIルートへ適用します。
        /// </summary>
        private static void ApplySafeArea(VisualElement root, Vector2Int resolution)
        {
            if (resolution == new Vector2Int(2520, 1080))
            {
                SafeAreaStyleApplier.Apply(
                    root,
                    resolution.x,
                    resolution.y,
                    new Rect(0, 0, resolution.x, resolution.y - 52)
                );
                return;
            }

            var horizontalInset = Mathf.RoundToInt(resolution.x * 0.05f);
            var verticalInset = Mathf.RoundToInt(resolution.y * 0.03f);
            SafeAreaStyleApplier.Apply(
                root,
                resolution.x,
                resolution.y,
                new Rect(
                    horizontalInset,
                    verticalInset,
                    resolution.x - (horizontalInset * 2),
                    resolution.y - (verticalInset * 2)
                )
            );
        }

        /// <summary>
        /// Battleの主要3領域と中央テキストが互いに重ならないことを検証します。
        /// </summary>
        private static void AssertBattleLayout(VisualElement root, Vector2Int resolution)
        {
            var npcRow = root.Q<VisualElement>("npc-row");
            var declarationPanel = root.Q<VisualElement>("declaration-panel");
            var userArea = root.Q<VisualElement>("user-area");
            var userPanel = root.Q<Label>("user-life").parent;
            var controls = root.Q<VisualElement>("controls");
            var declaration = root.Q<Label>("declaration-label");
            var banner = root.Q<Label>("action-banner");
            for (var index = 1; index <= 4; index++)
            {
                var portrait = root.Q<VisualElement>($"npc-{index}-portrait");
                Assert.That(
                    portrait.resolvedStyle.height,
                    Is.GreaterThanOrEqualTo(72f),
                    $"{resolution.x}x{resolution.y}: NPC {index} のキャラアイコンを判別できる大きさに保つ"
                );
            }
            Assert.That(npcRow.worldBound.Overlaps(declarationPanel.worldBound), Is.False);
            Assert.That(declarationPanel.worldBound.Overlaps(userArea.worldBound), Is.False);
            Assert.That(
                declarationPanel.worldBound.Overlaps(userPanel.worldBound),
                Is.False,
                $"{resolution.x}x{resolution.y}: 宣言パネルとユーザー情報"
            );
            Assert.That(
                declarationPanel.worldBound.Overlaps(controls.worldBound),
                Is.False,
                $"{resolution.x}x{resolution.y}: 宣言パネルと入力操作"
            );
            Assert.That(
                userArea.worldBound.Contains(userPanel.worldBound.min)
                    && userArea.worldBound.Contains(userPanel.worldBound.max),
                Is.True,
                $"{resolution.x}x{resolution.y}: ユーザー情報を下部領域内に収める "
                    + $"area={userArea.worldBound} panel={userPanel.worldBound}"
            );
            Assert.That(
                userArea.worldBound.Contains(controls.worldBound.min)
                    && userArea.worldBound.Contains(controls.worldBound.max),
                Is.True,
                $"{resolution.x}x{resolution.y}: 入力操作を下部領域内に収める "
                    + $"area={userArea.worldBound} controls={controls.worldBound}"
            );
            Assert.That(declaration.worldBound.Overlaps(banner.worldBound), Is.False);
            Assert.That(
                declarationPanel.worldBound.Contains(banner.worldBound.min),
                Is.True,
                $"{resolution.x}x{resolution.y}: 行動バナー左上"
            );
            Assert.That(
                declarationPanel.worldBound.Contains(banner.worldBound.max),
                Is.True,
                $"{resolution.x}x{resolution.y}: 行動バナー右下 panel={declarationPanel.worldBound} banner={banner.worldBound}"
            );
            var requiredTextWidth = banner
                .MeasureTextSize(
                    banner.text,
                    0f,
                    VisualElement.MeasureMode.Undefined,
                    banner.resolvedStyle.height,
                    VisualElement.MeasureMode.Exactly
                )
                .x;
            Assert.That(
                banner.contentRect.width,
                Is.GreaterThanOrEqualTo(requiredTextWidth),
                $"{resolution.x}x{resolution.y}: 行動バナー文字幅"
            );
        }

        /// <summary>
        /// 結果の敗者、実合計、宣言情報が表示枠内で互いに重ならないことを検証します。
        /// </summary>
        private static void AssertResultLayout(VisualElement root, Vector2Int resolution)
        {
            var summary = root.Q<VisualElement>("result-summary");
            var labels = new[]
            {
                root.Q<Label>("result-loser"),
                root.Q<Label>("result-total"),
                root.Q<Label>("result-declaration"),
                root.Q<Label>("result-details"),
            };
            foreach (var label in labels)
            {
                Assert.That(
                    summary.worldBound.Contains(label.worldBound.min),
                    Is.True,
                    $"{resolution.x}x{resolution.y}: {label.name} 左上"
                );
                Assert.That(
                    summary.worldBound.Contains(label.worldBound.max),
                    Is.True,
                    $"{resolution.x}x{resolution.y}: {label.name} 右下"
                );
            }

            for (var index = 0; index < labels.Length - 1; index++)
            {
                Assert.That(
                    labels[index].worldBound.Overlaps(labels[index + 1].worldBound),
                    Is.False
                );
            }
        }

        /// <summary>
        /// 指定位置の手動待機が登録されるまでフレームを進めます。
        /// </summary>
        private static IEnumerator WaitForRequest(ManualDelay delay, int index)
        {
            var timeoutAt = Time.realtimeSinceStartup + 2f;
            while (delay.Count <= index && Time.realtimeSinceStartup < timeoutAt)
            {
                yield return null;
            }

            Assert.That(delay.Count, Is.GreaterThan(index));
        }

        /// <summary>
        /// 親子Coroutineを完了させるため、指定数のフレームを進めます。
        /// </summary>
        private static IEnumerator AdvanceFrames(int count)
        {
            for (var index = 0; index < count; index++)
            {
                yield return null;
            }
        }

        /// <summary>
        /// 現在選択されているGame Viewの描画解像度を返します。
        /// </summary>
        private static Vector2Int GetRenderingResolution()
        {
#if UNITY_EDITOR
            PlayModeWindow.GetRenderingResolution(out var width, out var height);
            return new Vector2Int((int)width, (int)height);
#else
            return new Vector2Int(Screen.width, Screen.height);
#endif
        }

        /// <summary>
        /// Game ViewまたはPlayerの描画解像度を指定値へ変更します。
        /// </summary>
        private static void SetRenderingResolution(Vector2Int resolution)
        {
#if UNITY_EDITOR
            PlayModeWindow.SetCustomRenderingResolution(
                (uint)resolution.x,
                (uint)resolution.y,
                "Issue27NpcAction"
            );
#else
            Screen.SetResolution(resolution.x, resolution.y, false);
#endif
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

        private sealed class ManualDelay : IPresentationDelay
        {
            private readonly List<Request> _requests = new List<Request>();

            public int Count => _requests.Count;

            /// <summary>
            /// 呼び出し側が解放するまで待機要求を保留します。
            /// </summary>
            public IEnumerator Wait(float seconds)
            {
                var request = new Request();
                _requests.Add(request);
                while (!request.IsReleased)
                {
                    yield return null;
                }
            }

            /// <summary>
            /// 指定位置の待機要求を解放します。
            /// </summary>
            public void Release(int index)
            {
                _requests[index].IsReleased = true;
            }
        }

        private sealed class Request
        {
            public bool IsReleased { get; set; }
        }

        private sealed class MaxThenCoyoteExecutor : INpcTurnExecutor
        {
            private int _callCount;

            /// <summary>
            /// 最初のNPCから最大整数、次のNPCからコヨーテをApplicationへ送ります。
            /// </summary>
            public bool TryExecute(GameFlowService game)
            {
                _callCount++;
                return _callCount == 1
                    ? game.TryDeclareNumber(game.CurrentParticipantId, int.MaxValue)
                    : game.TryDeclareCoyote(game.CurrentParticipantId);
            }
        }

        private sealed class FirstValueThenZeroRandomSource : IRandomSource
        {
            private readonly int _firstValue;
            private bool _hasReturnedFirstValue;

            /// <summary>
            /// 最初の要求だけ指定開始位置を返します。
            /// </summary>
            public FirstValueThenZeroRandomSource(int firstValue)
            {
                _firstValue = firstValue;
            }

            /// <summary>
            /// 最初は指定値、その後は再現可能な0を返します。
            /// </summary>
            public int Next(int maxExclusive)
            {
                if (_hasReturnedFirstValue)
                {
                    return 0;
                }

                _hasReturnedFirstValue = true;
                return _firstValue;
            }
        }

        private sealed class ZeroRandomSource : IRandomSource
        {
            /// <summary>
            /// 再現可能な最小値0を返します。
            /// </summary>
            public int Next(int maxExclusive)
            {
                return 0;
            }
        }
    }
}
