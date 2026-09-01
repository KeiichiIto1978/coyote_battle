using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    /// 初心者向けRules画面の遷移、内容、状態不変、スクロールとSafe Areaを保証します。
    /// </summary>
    public sealed class RulesPresentationTests
    {
        private Vector2Int? _originalResolution;

        /// <summary>
        /// テスト用UIと常駐音源を破棄し、Game View解像度を元へ戻します。
        /// </summary>
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (
                var controller in Object.FindObjectsByType<GamePresentationController>(
                    FindObjectsSortMode.None
                )
            )
            {
                Object.Destroy(controller.gameObject);
            }

            foreach (var player in Object.FindObjectsByType<BgmPlayer>(FindObjectsSortMode.None))
            {
                Object.Destroy(player.gameObject);
            }

            foreach (var camera in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
            {
                if (camera.name == "CoyoteBattleCamera")
                {
                    Object.Destroy(camera.gameObject);
                }
            }

            if (_originalResolution.HasValue)
            {
                SetRenderingResolution(_originalResolution.Value);
                yield return null;
            }

            yield return null;
        }

        /// <summary>
        /// Rulesを3往復してもゲームを開始せず、画面とイベントを増やさず、その後は通常開始できることを保証します。
        /// </summary>
        [UnityTest]
        public IEnumerator RulesNavigation_3回往復_ゲーム状態と生成回数を変えず通常開始できる()
        {
            var createdGames = new List<GameFlowService>();
            var root = CreateRoot(createdGames);
            yield return null;

            Assert.That(createdGames, Has.Count.EqualTo(1));
            var initialGame = createdGames[0];
            Assert.That(initialGame.State, Is.EqualTo(GameFlowState.NoGame));
            Assert.That(initialGame.Participants, Is.Empty);

            for (var index = 0; index < 3; index++)
            {
                Click(root.Q<Button>("open-rules-button"));
                yield return null;

                Assert.That(
                    root.Q<VisualElement>("rules-screen").resolvedStyle.display,
                    Is.EqualTo(DisplayStyle.Flex)
                );
                Assert.That(
                    root.Q<VisualElement>("title-screen").resolvedStyle.display,
                    Is.EqualTo(DisplayStyle.None)
                );
                Assert.That(createdGames, Has.Count.EqualTo(1));
                Assert.That(initialGame.State, Is.EqualTo(GameFlowState.NoGame));
                Assert.That(initialGame.Participants, Is.Empty);

                Click(root.Q<Button>("rules-back-button"));
                yield return null;

                Assert.That(
                    root.Q<VisualElement>("title-screen").resolvedStyle.display,
                    Is.EqualTo(DisplayStyle.Flex)
                );
            }

            Assert.That(root.Query<VisualElement>("rules-screen").ToList(), Has.Count.EqualTo(1));
            Assert.That(root.Query<Button>("open-rules-button").ToList(), Has.Count.EqualTo(1));
            Assert.That(root.Query<Button>("rules-back-button").ToList(), Has.Count.EqualTo(1));

            Click(root.Q<Button>("start-game-button"));
            yield return null;

            Assert.That(createdGames, Has.Count.EqualTo(2));
            Assert.That(createdGames[1].State, Is.EqualTo(GameFlowState.Declaring));
            Assert.That(createdGames[1].Participants, Has.Count.EqualTo(5));
        }

        /// <summary>
        /// 必須10項目と36枚構成を表示し、最上部と最下部を超えず最後まで読めることを保証します。
        /// </summary>
        [UnityTest]
        public IEnumerator RulesContent_必須項目と36枚構成_縦スクロールで最後まで表示する()
        {
            var root = CreateRoot(new List<GameFlowService>());
            yield return null;
            Click(root.Q<Button>("open-rules-button"));
            yield return null;

            var scroll = root.Q<ScrollView>("rules-scroll-view");
            var sections = scroll.Query<VisualElement>(className: "rules-section").ToList();
            Assert.That(scroll.mode, Is.EqualTo(ScrollViewMode.Vertical));
            Assert.That(sections, Has.Count.EqualTo(10));
            Assert.That(root.Q<Label>("rules-purpose").text, Does.Contain("最後の1人"));
            Assert.That(root.Q<Label>("rules-visibility").text, Does.Contain("自分の札だけ"));
            Assert.That(root.Q<Label>("rules-turn").text, Does.Contain("直前より大きい"));
            Assert.That(
                root.Q<Label>("rules-first-turn").text,
                Does.Contain("初手").And.Contain("不可")
            );
            Assert.That(
                root.Q<Label>("rules-judgment").text,
                Does.Contain("宣言値 ＞ 実合計").And.Contain("等しい")
            );
            Assert.That(
                root.Q<Label>("rules-life").text,
                Does.Contain("ライフ3").And.Contain("0で脱落")
            );
            Assert.That(
                root.Q<Label>("rules-deck").text,
                Does.Contain("20×1枚")
                    .And.Contain("15×2枚")
                    .And.Contain("10×3枚")
                    .And.Contain("5・4・3・2・1×各4枚")
                    .And.Contain("通常0×3枚")
                    .And.Contain("0★×1枚")
                    .And.Contain("-5×2枚")
                    .And.Contain("-10×1枚")
                    .And.Contain("×2×1枚")
                    .And.Contain("MAX→0×1枚")
                    .And.Contain("？×1枚")
                    .And.Contain("合計36枚")
            );
            Assert.That(root.Q<Label>("rules-zero").text, Does.Contain("通常0").And.Contain("0★"));
            Assert.That(
                root.Q<Label>("rules-effect-order").text,
                Does.Contain("？ → MAX→0 → 数字合計 → ×2")
            );
            Assert.That(
                root.Q<Label>("rules-rebuild").text,
                Does.Contain("最大値1枚").And.Contain("全36枚")
            );

            scroll.scrollOffset = new Vector2(0, -1000);
            yield return null;
            Assert.That(scroll.scrollOffset.y, Is.GreaterThanOrEqualTo(0f));
            Assert.That(scroll.verticalScroller.highValue, Is.GreaterThan(0f));
            scroll.verticalScroller.value = scroll.verticalScroller.highValue;
            yield return null;

            var lastSection = sections[^1];
            Assert.That(
                lastSection.worldBound.yMax,
                Is.LessThanOrEqualTo(scroll.contentViewport.worldBound.yMax + 1f)
            );
            Assert.That(
                scroll.scrollOffset.y,
                Is.LessThanOrEqualTo(scroll.verticalScroller.highValue + 1f)
            );

            Click(root.Q<Button>("rules-back-button"));
            yield return null;
            Click(root.Q<Button>("open-rules-button"));
            yield return null;
            Assert.That(scroll.scrollOffset.y, Is.EqualTo(0f).Within(1f));
        }

        /// <summary>
        /// 3解像度とSafe Area相当余白でRules操作が隠れず、最終項目まで到達できることを保証します。
        /// </summary>
        [UnityTest]
        public IEnumerator RulesLayout_3解像度とSafeArea_戻る操作と最終項目を表示範囲内に保つ()
        {
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                Assert.Ignore("Game View解像度の検証にはグラフィックスデバイスが必要です。");
            }

            var root = CreateRoot(new List<GameFlowService>());
            yield return null;
            Click(root.Q<Button>("open-rules-button"));
            yield return null;
            var controller = Object.FindFirstObjectByType<GamePresentationController>();
            Assert.That(controller, Is.Not.Null);
            controller.enabled = false;
            _originalResolution = GetRenderingResolution();

            foreach (
                var resolution in new[]
                {
                    new Vector2Int(1280, 720),
                    new Vector2Int(1920, 1080),
                    new Vector2Int(2400, 1080),
                }
            )
            {
                SetRenderingResolution(resolution);
                yield return null;
                var leftInset = Mathf.RoundToInt(resolution.x * 0.05f);
                var rightInset = Mathf.RoundToInt(resolution.x * 0.03f);
                var bottomInset = Mathf.RoundToInt(resolution.y * 0.04f);
                var topInset = Mathf.RoundToInt(resolution.y * 0.02f);
                var safeArea = new Rect(
                    leftInset,
                    bottomInset,
                    resolution.x - leftInset - rightInset,
                    resolution.y - bottomInset - topInset
                );
                SafeAreaStyleApplier.Apply(root, resolution.x, resolution.y, safeArea);
                yield return null;

                var scroll = root.Q<ScrollView>("rules-scroll-view");
                scroll.verticalScroller.value = scroll.verticalScroller.highValue;
                yield return null;

                AssertRulesLayout(root, resolution, safeArea);
            }
        }

        /// <summary>
        /// 指定解像度でSafe AreaとRulesのスクロール終端を検証します。
        /// </summary>
        private static void AssertRulesLayout(
            VisualElement root,
            Vector2Int resolution,
            Rect safeArea
        )
        {
            var panelScaleX = resolution.x / root.worldBound.width;
            var panelScaleY = resolution.y / root.worldBound.height;
            Assert.That(panelScaleX, Is.EqualTo(panelScaleY).Within(0.01f));
            Assert.That(
                root.resolvedStyle.paddingLeft * panelScaleX,
                Is.EqualTo(safeArea.xMin).Within(1f)
            );
            Assert.That(
                root.resolvedStyle.paddingTop * panelScaleY,
                Is.EqualTo(resolution.y - safeArea.yMax).Within(1f)
            );
            Assert.That(
                root.resolvedStyle.paddingRight * panelScaleX,
                Is.EqualTo(resolution.x - safeArea.xMax).Within(1f)
            );
            Assert.That(
                root.resolvedStyle.paddingBottom * panelScaleY,
                Is.EqualTo(safeArea.yMin).Within(1f)
            );
            var safeBounds = new Rect(
                root.worldBound.xMin + root.resolvedStyle.paddingLeft,
                root.worldBound.yMin + root.resolvedStyle.paddingTop,
                root.worldBound.width
                    - root.resolvedStyle.paddingLeft
                    - root.resolvedStyle.paddingRight,
                root.worldBound.height
                    - root.resolvedStyle.paddingTop
                    - root.resolvedStyle.paddingBottom
            );
            var rules = root.Q<VisualElement>("rules-screen");
            var scroll = root.Q<ScrollView>("rules-scroll-view");
            var back = root.Q<Button>("rules-back-button");
            AssertInside(safeBounds, rules.worldBound, resolution);
            AssertInside(safeBounds, back.worldBound, resolution);
            Assert.That(scroll.worldBound.Overlaps(back.worldBound), Is.False);
            Assert.That(scroll.worldBound.height, Is.GreaterThan(100f));

            var lastSection = root.Q<VisualElement>("rules-section-10");
            Assert.That(
                lastSection.worldBound.yMax,
                Is.LessThanOrEqualTo(scroll.contentViewport.worldBound.yMax + 1f),
                $"{resolution.x}x{resolution.y}: last section"
            );
        }

        private static void AssertInside(Rect safeBounds, Rect target, Vector2Int resolution)
        {
            const float tolerance = 1f;
            Assert.That(target.xMin, Is.GreaterThanOrEqualTo(safeBounds.xMin - tolerance));
            Assert.That(target.yMin, Is.GreaterThanOrEqualTo(safeBounds.yMin - tolerance));
            Assert.That(
                target.xMax,
                Is.LessThanOrEqualTo(safeBounds.xMax + tolerance),
                $"{resolution.x}x{resolution.y}: right"
            );
            Assert.That(
                target.yMax,
                Is.LessThanOrEqualTo(safeBounds.yMax + tolerance),
                $"{resolution.x}x{resolution.y}: bottom"
            );
        }

        private static VisualElement CreateRoot(ICollection<GameFlowService> createdGames)
        {
            var gameObject = new GameObject("RulesPresentationTest");
            gameObject.SetActive(false);
            var controller = gameObject.AddComponent<GamePresentationController>();
            controller.ConfigureForTests(() =>
            {
                var game = new GameFlowService(new ZeroRandomSource(), new ZeroRandomSource());
                createdGames.Add(game);
                return game;
            });
            gameObject.SetActive(true);
            return gameObject.GetComponent<UIDocument>().rootVisualElement;
        }

        private static Vector2Int GetRenderingResolution()
        {
#if UNITY_EDITOR
            PlayModeWindow.GetRenderingResolution(out var width, out var height);
            return new Vector2Int((int)width, (int)height);
#else
            return new Vector2Int(Screen.width, Screen.height);
#endif
        }

        private static void SetRenderingResolution(Vector2Int resolution)
        {
#if UNITY_EDITOR
            PlayModeWindow.SetCustomRenderingResolution(
                (uint)resolution.x,
                (uint)resolution.y,
                "Issue29Rules"
            );
#else
            Screen.SetResolution(resolution.x, resolution.y, false);
#endif
        }

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

        private sealed class ZeroRandomSource : IRandomSource
        {
            /// <summary>
            /// 常に0を返し、開始参加者と山札操作を再現可能にします。
            /// </summary>
            public int Next(int maxExclusive)
            {
                return 0;
            }
        }
    }
}
