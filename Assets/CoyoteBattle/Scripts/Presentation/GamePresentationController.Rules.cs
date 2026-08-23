using UnityEngine;
using UnityEngine.UIElements;
using static CoyoteBattle.Presentation.PresentationUiFactory;

namespace CoyoteBattle.Presentation
{
    /// <summary>
    /// 初心者向けルール説明画面の構築とTitle間の遷移を担当します。
    /// </summary>
    public sealed partial class GamePresentationController
    {
        private VisualElement _rulesScreen;
        private ScrollView _rulesScrollView;

        /// <summary>
        /// Titleの導線と、最後まで縦スクロールできるRules画面を1つだけ構築します。
        /// </summary>
        private void BuildRulesScreen()
        {
            var openButton = CreateButton("ルール説明", ShowRules, "open-rules-button");
            openButton.style.width = 360;
            openButton.style.height = 64;
            _titleScreen.Add(openButton);

            _rulesScreen = CreateScreen("rules-screen");
            _rulesScreen.style.backgroundColor = new Color(0.02f, 0.06f, 0.1f, 0.96f);
            _rulesScreen.style.alignItems = Align.Stretch;
            _rulesScreen.Add(CreateLabel("ルール説明", 46, "rules-title"));

            _rulesScrollView = new ScrollView(ScrollViewMode.Vertical)
            {
                name = "rules-scroll-view",
                horizontalScrollerVisibility = ScrollerVisibility.Hidden,
                verticalScrollerVisibility = ScrollerVisibility.AlwaysVisible,
            };
            _rulesScrollView.style.flexGrow = 1;
            _rulesScrollView.style.minHeight = 0;
            _rulesScrollView.style.marginTop = 8;
            _rulesScrollView.style.marginBottom = 8;
            _rulesScrollView.contentContainer.style.paddingRight = 18;

            AddRuleSection(
                1,
                "目的",
                "各ラウンドの敗者を見破り、最後の1人になるまで生き残りましょう。",
                "rules-purpose"
            );
            AddRuleSection(
                2,
                "見える情報",
                "自分の札だけは見えません。NPC 4人の札を見て、自分を含む場の実合計を推理します。",
                "rules-visibility"
            );
            AddRuleSection(
                3,
                "手番でできること",
                "1以上で直前より大きい整数を宣言するか、直前の宣言が実合計を超えたと思ったら「コヨーテ！」を選びます。",
                "rules-turn"
            );
            AddRuleSection(
                4,
                "初手",
                "初手は直前の宣言がないためコヨーテ不可です。最初の参加者は数字を宣言します。",
                "rules-first-turn"
            );
            AddRuleSection(
                5,
                "コヨーテ後の判定",
                "宣言値 ＞ 実合計なら、最後の数字宣言者が敗北します。宣言値が実合計以下なら、コヨーテ宣言者が敗北します。等しい場合も宣言は成立しているため、コヨーテ宣言者の敗北です。",
                "rules-judgment"
            );
            AddRuleSection(
                6,
                "ライフと勝敗",
                "5人全員がライフ3で開始します。敗者はライフが1減り、0で脱落します。あなたが脱落すると敗北、NPC全員が脱落すると勝利です。",
                "rules-life"
            );
            AddRuleSection(
                7,
                "カード構成",
                "20×1枚 / 15×2枚 / 10×3枚 / 5・4・3・2・1×各4枚 / 通常0×3枚 / 0★×1枚 / -5×2枚 / -10×1枚 / ×2×1枚 / MAX→0×1枚 / ？×1枚（合計36枚）",
                "rules-deck"
            );
            AddRuleSection(
                8,
                "2種類の0",
                "通常0は合計へ0を加える数字カードです。夜カードの0★も合計は0ですが、ラウンド終了後に全山札を再構築します。",
                "rules-zero"
            );
            AddRuleSection(
                9,
                "特殊効果の順序",
                "？ → MAX→0 → 数字合計 → ×2 の順に解決します。？で引いた札の特殊効果も計算へ含めます。",
                "rules-effect-order"
            );
            AddRuleSection(
                10,
                "特殊効果の注意",
                "MAX→0で0にするのは最大値1枚だけです。0★が場札または？の追加札に出た場合、判定後に全36枚を集めて山札を再構築します。",
                "rules-rebuild"
            );

            _rulesScreen.Add(_rulesScrollView);
            var backButton = CreateButton("タイトルへ戻る", ShowTitle, "rules-back-button");
            backButton.style.width = 320;
            backButton.style.height = 64;
            backButton.style.alignSelf = Align.Center;
            backButton.style.flexShrink = 0;
            _rulesScreen.Add(backButton);
        }

        /// <summary>
        /// 番号、見出し、本文を持つ説明項目をスクロール領域へ追加します。
        /// </summary>
        private void AddRuleSection(int number, string title, string body, string bodyName)
        {
            var section = CreatePanel($"rules-section-{number}");
            section.AddToClassList("rules-section");
            section.style.alignItems = Align.Stretch;
            section.style.minHeight = 110;
            section.style.flexShrink = 0;

            var heading = CreateLabel($"{number}. {title}", 28);
            heading.style.unityTextAlign = TextAnchor.MiddleLeft;
            heading.style.color = new Color(1f, 0.78f, 0.22f);
            heading.style.unityFontStyleAndWeight = FontStyle.Bold;
            var description = CreateLabel(body, 22, bodyName);
            description.style.unityTextAlign = TextAnchor.UpperLeft;
            description.style.whiteSpace = WhiteSpace.Normal;
            description.style.flexShrink = 0;
            section.Add(heading);
            section.Add(description);
            _rulesScrollView.Add(section);
        }

        /// <summary>
        /// ゲーム状態を変更せず、Rules画面を先頭位置から表示します。
        /// </summary>
        private void ShowRules()
        {
            _bgmPlayer.SetTrack(BgmTrack.Title);
            _rulesScrollView.scrollOffset = Vector2.zero;
            SetVisible(_titleScreen, false);
            SetVisible(_rulesScreen, true);
            SetVisible(_battleScreen, false);
            SetVisible(_resultScreen, false);
            SetVisible(_gameOverDialog, false);
        }
    }
}
