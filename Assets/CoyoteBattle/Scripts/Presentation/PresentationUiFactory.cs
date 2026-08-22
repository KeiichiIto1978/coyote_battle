using System;
using CoyoteBattle.Application;
using CoyoteBattle.Domain;
using UnityEngine;
using UnityEngine.UIElements;

namespace CoyoteBattle.Presentation
{
    /// <summary>
    /// Presentation画面で共通利用するUI Toolkit要素を生成します。
    /// </summary>
    internal static class PresentationUiFactory
    {
        private static readonly Color NumberInputTextColor = new Color32(24, 32, 40, 255);
        private static readonly Color NumberInputBackgroundColor = new Color32(250, 247, 238, 255);
        private static readonly Color NumberInputBorderColor = new Color32(126, 82, 40, 255);
        private static readonly Color DisabledNumberInputTextColor = new Color32(65, 68, 72, 255);
        private static readonly Color DisabledNumberInputBackgroundColor = new Color32(
            208,
            207,
            201,
            255
        );
        private static readonly Color DisabledNumberInputBorderColor = new Color32(
            111,
            113,
            115,
            255
        );

        internal static VisualElement CreateScreen(string name)
        {
            var screen = new VisualElement { name = name };
            screen.style.position = Position.Absolute;
            screen.style.left = screen.style.right = screen.style.top = screen.style.bottom = 0;
            screen.style.paddingLeft = screen.style.paddingRight = 32;
            screen.style.paddingTop = screen.style.paddingBottom = 24;
            return screen;
        }

        internal static VisualElement CreatePanel(string name = null)
        {
            var panel = new VisualElement { name = name };
            panel.style.backgroundColor = new Color(0.02f, 0.12f, 0.14f, 0.88f);
            panel.style.borderTopLeftRadius = panel.style.borderTopRightRadius = 18;
            panel.style.borderBottomLeftRadius = panel.style.borderBottomRightRadius = 18;
            panel.style.paddingLeft = panel.style.paddingRight = 16;
            panel.style.paddingTop = panel.style.paddingBottom = 12;
            panel.style.marginLeft = panel.style.marginRight = 8;
            panel.style.marginTop = panel.style.marginBottom = 8;
            panel.style.alignItems = Align.Center;
            return panel;
        }

        internal static Label CreateLabel(string text, int size, string name = null)
        {
            var label = new Label(text) { name = name };
            label.style.fontSize = size;
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            label.style.marginTop = label.style.marginBottom = 5;
            return label;
        }

        internal static Button CreateButton(string text, Action action, string name)
        {
            var button = new Button(action) { text = text, name = name };
            button.style.height = 58;
            button.style.minWidth = 210;
            button.style.fontSize = 23;
            button.style.marginLeft = button.style.marginRight = 10;
            button.style.marginTop = button.style.marginBottom = 8;
            button.style.backgroundColor = new Color(0.87f, 0.42f, 0.12f);
            button.style.color = Color.white;
            return button;
        }

        internal static void SetVisible(VisualElement element, bool visible)
        {
            element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        internal static void SetBackground(VisualElement element, string resourcePath)
        {
            var texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null)
            {
                return;
            }

            element.style.backgroundImage = new StyleBackground(texture);
            element.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Cover);
        }

        /// <summary>
        /// ルートと既存の全テキスト要素に同梱フォントを適用します。
        /// </summary>
        /// <param name="root">フォントを継承させるUIルートです。</param>
        /// <param name="font">日本語表示に使用する同梱フォントです。</param>
        internal static void ApplyFont(VisualElement root, Font font)
        {
            if (font == null)
            {
                Debug.LogError("UI用フォント NotoSansJP を読み込めませんでした。");
                return;
            }

            var definition = new StyleFontDefinition(FontDefinition.FromFont(font));
            root.style.unityFontDefinition = definition;
            root.Query<TextElement>()
                .ForEach(element => element.style.unityFontDefinition = definition);
        }

        /// <summary>
        /// 数字入力欄の内部テキストへ、同梱フォントと可読性を保つ描画スタイルを適用します。
        /// </summary>
        /// <param name="textField">Battle画面で数値宣言に使用する入力欄です。</param>
        /// <param name="font">入力文字へ明示適用する同梱フォントです。</param>
        internal static void ConfigureNumberInput(TextField textField, Font font)
        {
            if (textField == null)
            {
                throw new ArgumentNullException(nameof(textField));
            }

            var textInput = textField.textEdition as TextElement;
            var inputContainer = textField.Q<VisualElement>(TextField.textInputUssName);
            if (textInput == null || inputContainer == null)
            {
                Debug.LogError("数字入力欄の内部テキスト要素を取得できませんでした。");
                return;
            }

            var styleSheet = Resources.Load<StyleSheet>("Styles/NumberInput");
            if (styleSheet != null)
            {
                textField.styleSheets.Add(styleSheet);
            }
            else
            {
                Debug.LogError("数字入力欄のスタイルシートを読み込めませんでした。");
            }

            textField.style.height = 55;
            textField.style.width = Length.Percent(100);
            textField.style.fontSize = 24;
            textField.style.opacity = 1f;
            textField.labelElement.style.width = 72;
            textField.labelElement.style.minWidth = 72;
            textField.labelElement.style.flexShrink = 0;
            textField.labelElement.style.color = Color.white;
            inputContainer.style.flexGrow = 1;
            inputContainer.style.minWidth = 0;
            inputContainer.style.height = 48;
            textInput.style.flexGrow = 1;
            textInput.style.minWidth = 0;
            textInput.style.height = 48;
            textInput.style.paddingLeft = textInput.style.paddingRight = 12;
            textInput.style.unityTextAlign = TextAnchor.MiddleLeft;
            textInput.style.whiteSpace = WhiteSpace.NoWrap;
            textInput.style.overflow = Overflow.Hidden;
            textInput.style.borderTopLeftRadius = textInput.style.borderTopRightRadius = 6;
            textInput.style.borderBottomLeftRadius = textInput.style.borderBottomRightRadius = 6;
            textInput.style.borderLeftWidth = textInput.style.borderRightWidth = 2;
            textInput.style.borderTopWidth = textInput.style.borderBottomWidth = 2;

            if (font != null)
            {
                textInput.style.unityFontDefinition = new StyleFontDefinition(
                    FontDefinition.FromFont(font)
                );
            }

            ApplyNumberInputEnabledStyle(textField, textField.enabledInHierarchy);
        }

        /// <summary>
        /// 数字入力欄の操作可否を、既存値を読める配色を維持しながら描き分けます。
        /// </summary>
        /// <param name="textField">表示状態を更新する数字入力欄です。</param>
        /// <param name="enabled">ユーザー入力を受け付ける場合はtrueです。</param>
        internal static void ApplyNumberInputEnabledStyle(TextField textField, bool enabled)
        {
            var textInput = textField?.textEdition as TextElement;
            if (textInput == null)
            {
                return;
            }

            textField.style.opacity = 1f;
            textInput.style.opacity = 1f;
            textInput.style.color = enabled ? NumberInputTextColor : DisabledNumberInputTextColor;
            textInput.style.backgroundColor = enabled
                ? NumberInputBackgroundColor
                : DisabledNumberInputBackgroundColor;
            var borderColor = enabled ? NumberInputBorderColor : DisabledNumberInputBorderColor;
            textInput.style.borderLeftColor = textInput.style.borderRightColor = borderColor;
            textInput.style.borderTopColor = textInput.style.borderBottomColor = borderColor;
        }

        /// <summary>
        /// NPCのアバター、ライフ、公開カードを1つの表示パネルにまとめます。
        /// </summary>
        internal static VisualElement CreateParticipantPanel(
            ParticipantState participant,
            string currentParticipantId,
            DealtCardState card
        )
        {
            var panel = CreatePanel(participant.Id);
            panel.style.width = Length.Percent(23);
            if (participant.Id == currentParticipantId)
            {
                panel.style.borderTopColor = panel.style.borderBottomColor = new Color(
                    1f,
                    0.72f,
                    0.2f
                );
                panel.style.borderTopWidth = panel.style.borderBottomWidth = 5;
            }

            var avatar = new VisualElement { name = $"{participant.Id}-portrait" };
            avatar.style.width = Length.Percent(100);
            avatar.style.flexGrow = 1;
            avatar.style.minHeight = 1;
            SetBackground(avatar, AvatarResource(participant.Id));
            avatar.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
            avatar.style.backgroundRepeat = new BackgroundRepeat(Repeat.NoRepeat, Repeat.NoRepeat);
            panel.Add(avatar);
            panel.Add(
                CreateLabel(
                    PresentationText.ParticipantName(participant.Id),
                    20,
                    $"{participant.Id}-name"
                )
            );
            panel.Add(
                CreateLabel(
                    $"ライフ {participant.Life}"
                        + (participant.IsEliminated ? " / 脱落" : string.Empty),
                    18,
                    $"{participant.Id}-life"
                )
            );

            var cardLabel = CreateLabel(
                card?.Card == null ? "—" : PresentationText.Card(card.Card.Kind, card.Card.Value),
                27,
                $"{participant.Id}-card"
            );
            cardLabel.style.width = 88;
            cardLabel.style.height = 112;
            if (card?.Card != null)
            {
                SetBackground(cardLabel, CardResource(card.Card));
                cardLabel.style.color =
                    card.Card.Kind == CardKind.Number ? Color.black : Color.white;
            }

            panel.Add(cardLabel);
            return panel;
        }

        /// <summary>
        /// ラウンド結果に公開する所有者名付きカードを生成します。
        /// </summary>
        internal static VisualElement CreateResultCard(string owner, CardState card)
        {
            var panel = CreatePanel();
            panel.style.width = 190;
            panel.style.height = 210;
            panel.Add(CreateLabel(owner, 18));
            var cardLabel = CreateLabel(PresentationText.Card(card.Kind, card.Value), 38);
            cardLabel.style.width = 105;
            cardLabel.style.height = 140;
            cardLabel.style.color = card.Kind == CardKind.Number ? Color.black : Color.white;
            SetBackground(cardLabel, CardResource(card));
            panel.Add(cardLabel);
            return panel;
        }

        internal static string AvatarResource(string id)
        {
            switch (id)
            {
                case "npc-1":
                    return "Art/NpcAggressive";
                case "npc-2":
                    return "Art/NpcCautious";
                case "npc-3":
                    return "Art/NpcGambling";
                default:
                    return "Art/NpcAnalytical";
            }
        }

        internal static string CardResource(CardState card)
        {
            if (card.Kind == CardKind.Number)
            {
                if (card.Value > 0)
                    return "Art/CardPositive";
                if (card.Value < 0)
                    return "Art/CardNegative";
                return "Art/CardZero";
            }

            switch (card.Kind)
            {
                case CardKind.Night:
                    return "Art/CardNight";
                case CardKind.Double:
                    return "Art/CardDouble";
                case CardKind.MaxToZero:
                    return "Art/CardMaxToZero";
                default:
                    return "Art/CardMystery";
            }
        }
    }
}
