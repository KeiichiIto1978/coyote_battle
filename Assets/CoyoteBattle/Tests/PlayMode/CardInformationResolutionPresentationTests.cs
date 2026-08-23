using System.Collections;
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
    /// カード情報パネルを実際のGame View解像度とSafe Areaで検証します。
    /// </summary>
    public sealed class CardInformationResolutionPresentationTests
    {
        private Vector2Int? _originalResolution;

        /// <summary>
        /// テスト用UIを破棄し、Game Viewの描画解像度を元へ戻します。
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

            if (_originalResolution.HasValue)
            {
                SetRenderingResolution(_originalResolution.Value);
                yield return null;
            }

            yield return null;
        }

        /// <summary>
        /// 3解像度とSafe Area相当余白で、対戦欄とカード情報が欠落・重複しないことを保証します。
        /// </summary>
        [UnityTest]
        public IEnumerator CardInformation_検証解像度とSafeArea相当余白_対戦欄と重ならず全行を保持する()
        {
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                Assert.Ignore("Game View解像度の検証にはグラフィックスデバイスが必要です。");
            }

            var root = CreateBattleRoot();
            yield return null;
            Click(root.Q<Button>("start-game-button"));
            yield return null;
            Click(root.Q<Button>("card-information-button"));
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
                Assert.That(GetRenderingResolution(), Is.EqualTo(resolution));
                var horizontalInset = Mathf.RoundToInt(resolution.x * 0.05f);
                var verticalInset = Mathf.RoundToInt(resolution.y * 0.03f);
                var safeArea = new Rect(
                    horizontalInset,
                    verticalInset,
                    resolution.x - (horizontalInset * 2),
                    resolution.y - (verticalInset * 2)
                );
                SafeAreaStyleApplier.Apply(root, resolution.x, resolution.y, safeArea);
                yield return null;

                AssertLayout(root, resolution, safeArea);
            }
        }

        /// <summary>
        /// Safe Area反映後の主要領域が内側に収まり、互いに重ならないことを検証します。
        /// </summary>
        private static void AssertLayout(VisualElement root, Vector2Int resolution, Rect safeArea)
        {
            var expectedPadding = SafeAreaPaddingCalculator.Calculate(
                resolution.x,
                resolution.y,
                safeArea,
                new Vector2(1920, 1080)
            );
            Assert.That(root.resolvedStyle.paddingLeft, Is.EqualTo(expectedPadding.x).Within(0.5f));
            Assert.That(root.resolvedStyle.paddingTop, Is.EqualTo(expectedPadding.y).Within(0.5f));
            Assert.That(
                root.resolvedStyle.paddingRight,
                Is.EqualTo(expectedPadding.z).Within(0.5f)
            );
            Assert.That(
                root.resolvedStyle.paddingBottom,
                Is.EqualTo(expectedPadding.w).Within(0.5f)
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
            var main = root.Q<VisualElement>("battle-main");
            var panel = root.Q<VisualElement>("card-information-panel");
            var controls = root.Q<VisualElement>("controls");
            var scroll = root.Q<ScrollView>("card-information-scroll");
            var totals = root.Q<Label>("card-information-totals");
            AssertInside(safeBounds, main.worldBound, resolution);
            AssertInside(safeBounds, panel.worldBound, resolution);
            Assert.That(main.worldBound.Overlaps(panel.worldBound), Is.False);
            Assert.That(controls.worldBound.Overlaps(panel.worldBound), Is.False);
            Assert.That(scroll.worldBound.height, Is.GreaterThan(100f));
            Assert.That(scroll.worldBound.Overlaps(totals.worldBound), Is.False);
            Assert.That(
                root.Query<VisualElement>("card-information-row").ToList(),
                Has.Count.EqualTo(15)
            );
        }

        /// <summary>
        /// 指定領域がSafe Area内に収まることを誤差込みで検証します。
        /// </summary>
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

        /// <summary>
        /// ユーザーが最初の手番となるBattle画面を生成します。
        /// </summary>
        private static VisualElement CreateBattleRoot()
        {
            var gameObject = new GameObject("CardInformationResolutionTest");
            gameObject.SetActive(false);
            var controller = gameObject.AddComponent<GamePresentationController>();
            controller.ConfigureForTests(() =>
                new GameFlowService(new ZeroRandomSource(), new ZeroRandomSource())
            );
            gameObject.SetActive(true);
            return gameObject.GetComponent<UIDocument>().rootVisualElement;
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
                "Issue30CardInformation"
            );
#else
            Screen.SetResolution(resolution.x, resolution.y, false);
#endif
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

        private sealed class ZeroRandomSource : IRandomSource
        {
            /// <summary>
            /// 常にユーザー開始と決定的な山札操作になる0を返します。
            /// </summary>
            public int Next(int maxExclusive)
            {
                return 0;
            }
        }
    }
}
