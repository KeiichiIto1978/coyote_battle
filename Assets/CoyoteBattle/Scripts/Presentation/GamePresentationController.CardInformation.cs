using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using static CoyoteBattle.Presentation.PresentationUiFactory;

namespace CoyoteBattle.Presentation
{
    /// <summary>
    /// 対戦中のカード情報パネルの構築、表示、操作制御を担当します。
    /// </summary>
    public sealed partial class GamePresentationController
    {
        private VisualElement _cardInformationPanel;
        private VisualElement _cardInformationRows;
        private Label _cardInformationTotals;
        private bool _isCardInformationOpen;

        /// <summary>
        /// 対戦画面の右側に、再利用するカード情報パネルを1つ構築します。
        /// </summary>
        private void BuildCardInformation()
        {
            _cardInformationPanel = CreatePanel("card-information-panel");
            _cardInformationPanel.style.width = 500;
            _cardInformationPanel.style.flexShrink = 0;
            _cardInformationPanel.style.marginLeft = 12;
            _cardInformationPanel.style.alignItems = Align.Stretch;
            _cardInformationPanel.style.display = DisplayStyle.None;

            var header = new VisualElement { name = "card-information-header" };
            header.style.flexDirection = FlexDirection.Row;
            header.style.alignItems = Align.Center;
            var title = CreateLabel("カード情報", 30, "card-information-title");
            title.style.flexGrow = 1;
            var closeButton = CreateButton(
                "閉じる",
                CloseCardInformation,
                "card-information-close-button"
            );
            closeButton.style.width = 140;
            header.Add(title);
            header.Add(closeButton);
            _cardInformationPanel.Add(header);

            var columns = new VisualElement { name = "card-information-columns" };
            columns.style.flexDirection = FlexDirection.Row;
            columns.Add(CreateColumnLabel("カード", 0.46f));
            columns.Add(CreateColumnLabel("初期", 0.27f));
            columns.Add(CreateColumnLabel("使用済み", 0.27f));
            _cardInformationPanel.Add(columns);

            var scrollView = new ScrollView(ScrollViewMode.Vertical)
            {
                name = "card-information-scroll",
            };
            scrollView.style.flexGrow = 1;
            _cardInformationRows = new VisualElement { name = "card-information-rows" };
            scrollView.Add(_cardInformationRows);
            _cardInformationPanel.Add(scrollView);
            _cardInformationTotals = CreateLabel(string.Empty, 19, "card-information-totals");
            _cardInformationTotals.style.whiteSpace = WhiteSpace.Normal;
            _cardInformationPanel.Add(_cardInformationTotals);
            _battleScreen.Add(_cardInformationPanel);
        }

        /// <summary>
        /// 指定文字列と幅比率を持つカード情報列ラベルを生成します。
        /// </summary>
        /// <param name="text">列に表示する文字列です。</param>
        /// <param name="grow">行内で割り当てる幅比率です。</param>
        /// <returns>中央揃えの列ラベルです。</returns>
        private static Label CreateColumnLabel(string text, float grow)
        {
            var label = CreateLabel(text, 19);
            label.style.flexGrow = grow;
            label.style.flexBasis = 0;
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            return label;
        }

        /// <summary>
        /// ユーザー手番中に最新の集計を反映し、カード情報パネルを開きます。
        /// </summary>
        private void OpenCardInformation()
        {
            if (
                _isNpcSequenceRunning
                || _game.State != CoyoteBattle.Application.GameFlowState.Declaring
                || _game.CurrentParticipantId != UserId
            )
            {
                return;
            }

            RefreshCardInformation();
            _isCardInformationOpen = true;
            _cardInformationPanel.style.display = DisplayStyle.Flex;
            SetInputEnabled(false);
        }

        /// <summary>
        /// カード情報パネルを閉じ、同じ手番の入力可否を復元します。
        /// </summary>
        private void CloseCardInformation()
        {
            HideCardInformation();
            SetInputEnabled(!_isNpcSequenceRunning && _game.CurrentParticipantId == UserId);
        }

        /// <summary>
        /// 画面遷移時にカード情報パネルを閉じた状態へ戻します。
        /// </summary>
        private void HideCardInformation()
        {
            _isCardInformationOpen = false;
            if (_cardInformationPanel != null)
            {
                _cardInformationPanel.style.display = DisplayStyle.None;
            }
        }

        /// <summary>
        /// Applicationの公開集計から全カード行と合計値を再構築します。
        /// </summary>
        private void RefreshCardInformation()
        {
            var information = _game.CardInformation;
            _cardInformationRows.Clear();
            foreach (var item in information)
            {
                var row = new VisualElement { name = "card-information-row" };
                row.style.flexDirection = FlexDirection.Row;
                row.style.minHeight = 36;
                row.Add(CreateColumnLabel(PresentationText.Card(item.Kind, item.Value), 0.46f));
                row.Add(CreateColumnLabel(item.InitialCount.ToString(), 0.27f));
                row.Add(CreateColumnLabel(item.DiscardedCount.ToString(), 0.27f));
                _cardInformationRows.Add(row);
            }

            _cardInformationTotals.text =
                $"初期枚数合計：{information.Sum(item => item.InitialCount)}\n"
                + $"使用済み合計：{information.Sum(item => item.DiscardedCount)}";
        }
    }
}
