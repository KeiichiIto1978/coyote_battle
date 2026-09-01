using System.Collections;
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
    /// Battle画面の数字入力欄を検証対象のGame View解像度とSafe Areaで保証します。
    /// </summary>
    public sealed class NumberInputResolutionPresentationTests
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

        [UnityTest]
        public IEnumerator NumberInput_検証解像度とSafeArea相当余白_1文字から10文字を欠落や重なりなく表示する()
        {
            // 実際のGame Viewを3解像度へ切り替え、境界値1・9・10文字の描画領域を保証します。
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                Assert.Ignore("Game View解像度の検証にはグラフィックスデバイスが必要です。");
            }

            var root = CreateBattleRoot();
            yield return null;
            Click(root.Q<Button>("start-game-button"));
            yield return null;

            var controls = root.Q<VisualElement>("controls");
            var numberInput = root.Q<TextField>("number-input");
            var textInput = numberInput.textEdition as TextElement;
            var inputTarget = numberInput.Q<VisualElement>(TextField.textInputUssName);
            Assert.That(textInput, Is.Not.Null);
            Assert.That(inputTarget, Is.Not.Null);
            var controller = Object.FindFirstObjectByType<GamePresentationController>();
            Assert.That(controller, Is.Not.Null);
            controller.enabled = false;
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
                Assert.That(GetRenderingResolution(), Is.EqualTo(resolution));

                var safeArea = CreateSafeArea(resolution);
                SafeAreaStyleApplier.Apply(root, resolution.x, resolution.y, safeArea);
                yield return null;

                AssertSafeArea(root, controls, resolution, safeArea);
                var label = numberInput.labelElement;
                var buttons = controls.Q<Button>("declare-number-button").parent;
                Assert.That(label.worldBound.Overlaps(textInput.worldBound), Is.False);
                Assert.That(numberInput.worldBound.Overlaps(buttons.worldBound), Is.False);
                foreach (var inputValue in new[] { "1", "123456789", "2147483647" })
                {
                    numberInput.value = inputValue;
                    yield return null;
                    Assert.That(textInput.text, Is.EqualTo(inputValue));
                    var drawableWidth =
                        inputTarget.contentRect.width
                        - textInput.resolvedStyle.paddingLeft
                        - textInput.resolvedStyle.paddingRight
                        - textInput.resolvedStyle.borderLeftWidth
                        - textInput.resolvedStyle.borderRightWidth;
                    Assert.That(
                        drawableWidth,
                        Is.GreaterThan(MeasureTextWidth(textInput)),
                        $"{resolution.x}x{resolution.y}: {inputValue.Length}文字"
                    );
                    Assert.That(
                        textInput.contentRect.height,
                        Is.GreaterThan(MeasureTextHeight(textInput)),
                        $"{resolution.x}x{resolution.y}: {inputValue.Length}文字の描画高さ "
                            + $"text={textInput.contentRect} container={inputTarget.contentRect}"
                    );
                    Assert.That(
                        textInput.style.overflow.value,
                        Is.EqualTo(Overflow.Visible),
                        $"{resolution.x}x{resolution.y}: 入力文字をクリップしない"
                    );
                }
            }
        }

        /// <summary>
        /// 再現端末では上端のシステム領域だけを除外し、既存解像度では従来の余白を返します。
        /// </summary>
        private static Rect CreateSafeArea(Vector2Int resolution)
        {
            if (resolution == new Vector2Int(2520, 1080))
            {
                return new Rect(0, 0, resolution.x, resolution.y - 52);
            }

            var horizontalInset = Mathf.RoundToInt(resolution.x * 0.05f);
            var verticalInset = Mathf.RoundToInt(resolution.y * 0.03f);
            return new Rect(
                horizontalInset,
                verticalInset,
                resolution.x - (horizontalInset * 2),
                resolution.y - (verticalInset * 2)
            );
        }

        /// <summary>
        /// Safe Area余白がルートと操作欄の配置へ反映されたことを検証します。
        /// </summary>
        private static void AssertSafeArea(
            VisualElement root,
            VisualElement controls,
            Vector2Int resolution,
            Rect safeArea
        )
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
            Assert.That(safeBounds.Contains(controls.worldBound.min), Is.True);
            Assert.That(safeBounds.Contains(controls.worldBound.max), Is.True);
        }

        /// <summary>
        /// ユーザーが最初の手番となるBattle画面を生成します。
        /// </summary>
        private static VisualElement CreateBattleRoot()
        {
            var gameObject = new GameObject("NumberInputResolutionTest");
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
                "Issue31NumberInput"
            );
#else
            Screen.SetResolution(resolution.x, resolution.y, false);
#endif
        }

        /// <summary>
        /// 入力要素の現在のフォントと文字列から必要な描画幅を計測します。
        /// </summary>
        private static float MeasureTextWidth(TextElement textInput)
        {
            return textInput
                .MeasureTextSize(
                    textInput.text,
                    0f,
                    VisualElement.MeasureMode.Undefined,
                    textInput.resolvedStyle.height,
                    VisualElement.MeasureMode.Exactly
                )
                .x;
        }

        /// <summary>
        /// 入力要素の現在のフォントと文字列から必要な描画高さを計測します。
        /// </summary>
        private static float MeasureTextHeight(TextElement textInput)
        {
            return textInput
                .MeasureTextSize(
                    textInput.text,
                    textInput.resolvedStyle.width,
                    VisualElement.MeasureMode.Exactly,
                    0f,
                    VisualElement.MeasureMode.Undefined
                )
                .y;
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
            public int Next(int maxExclusive)
            {
                return 0;
            }
        }
    }
}
