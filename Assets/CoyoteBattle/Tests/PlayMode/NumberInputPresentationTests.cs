using System.Collections;
using CoyoteBattle.Application;
using CoyoteBattle.Domain;
using CoyoteBattle.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace CoyoteBattle.Tests.Presentation
{
    /// <summary>
    /// Battle画面の数字入力欄が入力内容と操作状態を判読可能に描画することを保証します。
    /// </summary>
    public sealed class NumberInputPresentationTests
    {
        private static readonly CustomStyleProperty<Color> CursorColorProperty =
            new CustomStyleProperty<Color>("--unity-cursor-color");
        private static readonly CustomStyleProperty<Color> SelectionColorProperty =
            new CustomStyleProperty<Color>("--unity-selection-color");

        /// <summary>
        /// テスト間でコントローラーや表示用カメラが残らないよう破棄します。
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

            yield return null;
        }

        [UnityTest]
        public IEnumerator StartNewGame_数字キー入力と削除_内部テキストへ判読可能に反映する()
        {
            // 値だけ保持され、実際の入力文字が背景と同化して見えない不具合を再現・防止します。
            var root = CreateBattleRoot(CreateUserStarterGame);
            yield return null;
            Click(root.Q<Button>("start-game-button"));
            yield return null;

            var numberInput = root.Q<TextField>("number-input");
            var textInput = numberInput.textEdition as TextElement;
            var inputTarget = numberInput.Q<VisualElement>(TextField.textInputUssName);
            Assert.That(textInput, Is.Not.Null);
            Assert.That(inputTarget, Is.Not.Null);

            numberInput.Focus();
            SendKey(inputTarget, '7', KeyCode.Alpha7);
            yield return null;

            Assert.That(numberInput.value, Is.EqualTo("7"));
            Assert.That(textInput.text, Is.EqualTo("7"));
            Assert.That(textInput.resolvedStyle.color.a, Is.EqualTo(1f).Within(0.001f));
            Assert.That(
                ColorContrast(
                    textInput.resolvedStyle.color,
                    textInput.resolvedStyle.backgroundColor
                ),
                Is.GreaterThan(4.5f)
            );
            Assert.That(textInput.resolvedStyle.unityFontDefinition.font, Is.Not.Null);
            Assert.That(
                textInput.resolvedStyle.unityFontDefinition.font.name,
                Does.Contain("NotoSansJP")
            );

            SendKey(inputTarget, '8', KeyCode.Alpha8);
            SendKey(inputTarget, '\b', KeyCode.Backspace);
            yield return null;

            Assert.That(numberInput.value, Is.EqualTo("7"));
            numberInput.SelectAll();
            SendKey(inputTarget, '\b', KeyCode.Backspace);
            yield return null;
            Assert.That(numberInput.value, Is.Empty);

            foreach (var character in "12345678901")
            {
                SendKey(inputTarget, character, KeyCode.None);
            }

            yield return null;
            Assert.That(numberInput.maxLength, Is.EqualTo(10));
            Assert.That(numberInput.value, Is.EqualTo("1234567890"));
        }

        [UnityTest]
        public IEnumerator StartNewGame_フォーカスと選択_キャレットと選択範囲の色を識別できる()
        {
            // キャレットと選択背景が入力背景へ埋没し、編集位置を判別できない不具合を防ぎます。
            var root = CreateBattleRoot(CreateUserStarterGame);
            yield return null;
            Click(root.Q<Button>("start-game-button"));
            yield return null;

            var numberInput = root.Q<TextField>("number-input");
            var textInput = numberInput.textEdition as TextElement;
            var inputTarget = numberInput.Q<VisualElement>(TextField.textInputUssName);
            Assert.That(textInput, Is.Not.Null);
            Assert.That(inputTarget, Is.Not.Null);
            numberInput.value = "2147483647";
            numberInput.Focus();
            yield return null;
            numberInput.SelectRange(0, numberInput.value.Length);
            yield return null;

            Assert.That(numberInput.textSelection.HasSelection(), Is.True);
            Assert.That(
                inputTarget.customStyle.TryGetValue(CursorColorProperty, out var cursorColor),
                Is.True
            );
            Assert.That(
                inputTarget.customStyle.TryGetValue(SelectionColorProperty, out var selectionColor),
                Is.True
            );
            Assert.That(cursorColor.a, Is.EqualTo(1f).Within(0.001f));
            Assert.That(selectionColor.a, Is.GreaterThanOrEqualTo(0.75f));
            Assert.That(
                ColorContrast(cursorColor, textInput.resolvedStyle.backgroundColor),
                Is.GreaterThan(3f)
            );
            Assert.That(
                ColorContrast(textInput.resolvedStyle.color, selectionColor),
                Is.GreaterThan(3f)
            );
        }

        [UnityTest]
        public IEnumerator StartNewGame_NPC待機中_無効状態を判別でき入力を受理しない()
        {
            // NPC手番中の入力欄を操作可能と誤認せず、既存値があっても読める状態を保証します。
            var root = CreateBattleRoot(CreateNpcStarterGame);
            yield return null;
            var numberInput = root.Q<TextField>("number-input");
            var textInput = numberInput.textEdition as TextElement;
            var inputTarget = numberInput.Q<VisualElement>(TextField.textInputUssName);
            Assert.That(textInput, Is.Not.Null);
            Assert.That(inputTarget, Is.Not.Null);
            var enabledBackground = textInput.resolvedStyle.backgroundColor;
            var enabledBorder = textInput.resolvedStyle.borderLeftColor;

            Click(root.Q<Button>("start-game-button"));
            yield return null;

            Assert.That(numberInput.enabledInHierarchy, Is.False);
            numberInput.SetValueWithoutNotify("214");
            yield return null;
            Assert.That(textInput.text, Is.EqualTo("214"));
            var disabledBackground = textInput.resolvedStyle.backgroundColor;
            Assert.That(disabledBackground, Is.Not.EqualTo(enabledBackground));
            Assert.That(textInput.resolvedStyle.borderLeftColor, Is.Not.EqualTo(enabledBorder));
            Assert.That(textInput.resolvedStyle.color.a, Is.EqualTo(1f).Within(0.001f));
            Assert.That(
                ColorContrast(textInput.resolvedStyle.color, disabledBackground),
                Is.GreaterThan(4.5f)
            );

            SendKey(inputTarget, '7', KeyCode.Alpha7);
            yield return null;

            Assert.That(numberInput.value, Is.EqualTo("214"));
        }

        [UnityTest]
        public IEnumerator DeclareNumber_不正な入力形式_入力文字を表示したまま既存エラーを表示する()
        {
            // 送信時に拒否する文字も入力中は隠さず、利用者が誤入力を修正できることを保証します。
            var root = CreateBattleRoot(CreateUserStarterGame);
            yield return null;
            Click(root.Q<Button>("start-game-button"));
            yield return null;

            var numberInput = root.Q<TextField>("number-input");
            var textInput = numberInput.textEdition as TextElement;
            var inputTarget = numberInput.Q<VisualElement>(TextField.textInputUssName);
            Assert.That(textInput, Is.Not.Null);
            Assert.That(inputTarget, Is.Not.Null);
            var invalidInputs = new[] { "１２", "-1", "1.5", " " };
            foreach (var invalidInput in invalidInputs)
            {
                numberInput.Focus();
                numberInput.SelectAll();
                SendKey(inputTarget, '\b', KeyCode.Backspace);
                foreach (var character in invalidInput)
                {
                    SendCharacter(inputTarget, character);
                }

                yield return null;
                Assert.That(textInput.text, Is.EqualTo(invalidInput));

                Click(root.Q<Button>("declare-number-button"));
                yield return null;

                Assert.That(root.Q<Label>("input-error").text, Is.Not.Empty);
                Assert.That(numberInput.value, Is.EqualTo(invalidInput));
                Assert.That(numberInput.enabledInHierarchy, Is.True);
            }
        }

        /// <summary>
        /// 指定した決定的ゲームでBattle画面を開始し、UIルートを返します。
        /// </summary>
        private static VisualElement CreateBattleRoot(System.Func<GameFlowService> gameFactory)
        {
            var gameObject = new GameObject("NumberInputTest");
            gameObject.SetActive(false);
            var controller = gameObject.AddComponent<GamePresentationController>();
            controller.ConfigureForTests(gameFactory);
            gameObject.SetActive(true);
            return gameObject.GetComponent<UIDocument>().rootVisualElement;
        }

        /// <summary>
        /// ユーザーが最初の手番となる決定的ゲームを生成します。
        /// </summary>
        private static GameFlowService CreateUserStarterGame()
        {
            return new GameFlowService(new ZeroRandomSource(), new ZeroRandomSource());
        }

        /// <summary>
        /// NPC 1が最初の手番となる決定的ゲームを生成します。
        /// </summary>
        private static GameFlowService CreateNpcStarterGame()
        {
            return new GameFlowService(
                new FirstValueThenZeroRandomSource(1),
                new ZeroRandomSource()
            );
        }

        /// <summary>
        /// UI Toolkitの実キーイベントを入力要素へ送ります。
        /// </summary>
        private static void SendKey(VisualElement inputTarget, char character, KeyCode keyCode)
        {
            var keyboardEvent =
                keyCode == KeyCode.Backspace
                    ? Event.KeyboardEvent("backspace")
                    : Event.KeyboardEvent(character.ToString());
            using (var keyDown = KeyDownEvent.GetPooled(keyboardEvent))
            {
                inputTarget.SendEvent(keyDown);
            }
        }

        /// <summary>
        /// UI Toolkitが文字入力時に受け取る文字付きKeyDownEventを送ります。
        /// </summary>
        private static void SendCharacter(VisualElement inputTarget, char character)
        {
            using (
                var keyDown = KeyDownEvent.GetPooled(character, KeyCode.None, EventModifiers.None)
            )
            {
                inputTarget.SendEvent(keyDown);
            }
        }

        /// <summary>
        /// WCAGの相対輝度を使って前景色と背景色のコントラスト比を算出します。
        /// </summary>
        private static float ColorContrast(Color foreground, Color background)
        {
            var foregroundLuminance = RelativeLuminance(foreground);
            var backgroundLuminance = RelativeLuminance(background);
            return (Mathf.Max(foregroundLuminance, backgroundLuminance) + 0.05f)
                / (Mathf.Min(foregroundLuminance, backgroundLuminance) + 0.05f);
        }

        /// <summary>
        /// sRGB色を線形化して相対輝度へ変換します。
        /// </summary>
        private static float RelativeLuminance(Color color)
        {
            var linear = color.linear;
            return (0.2126f * linear.r) + (0.7152f * linear.g) + (0.0722f * linear.b);
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

        private sealed class FirstValueThenZeroRandomSource : IRandomSource
        {
            private readonly int _firstValue;
            private bool _hasReturnedFirstValue;

            internal FirstValueThenZeroRandomSource(int firstValue)
            {
                _firstValue = firstValue;
            }

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
    }
}
