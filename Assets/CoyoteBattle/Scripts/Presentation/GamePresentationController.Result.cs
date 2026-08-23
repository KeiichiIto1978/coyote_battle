using System.Linq;
using CoyoteBattle.Application;
using static CoyoteBattle.Presentation.PresentationUiFactory;

namespace CoyoteBattle.Presentation
{
    /// <summary>
    /// ラウンド結果とゲーム終了の情報優先度を画面へ反映します。
    /// </summary>
    public sealed partial class GamePresentationController
    {
        /// <summary>
        /// 判定済みカードと敗者、実合計、最終宣言を結果画面へ表示します。
        /// </summary>
        private void ShowResult()
        {
            HideCardInformation();
            SetVisible(_titleScreen, false);
            SetVisible(_rulesScreen, false);
            SetVisible(_battleScreen, false);
            SetVisible(_resultScreen, true);
            var result = _game.LastRoundResult;
            _resultCards.Clear();
            foreach (var deal in result.DealtCards)
            {
                _resultCards.Add(
                    CreateResultCard(
                        PresentationText.ParticipantName(deal.ParticipantId),
                        deal.Card
                    )
                );
            }

            foreach (var card in result.AdditionalCards)
            {
                _resultCards.Add(CreateResultCard("？の追加札", card));
            }

            _resultLoser.text = $"敗者　{PresentationText.ParticipantName(result.LoserId)}";
            _resultTotal.text = $"実合計　{result.ActualTotal}";
            _resultDeclaration.text =
                $"最終宣言値　{result.DeclaredNumber}\n"
                + $"最終宣言者　{PresentationText.ParticipantName(result.NumberDeclarerId)}\n"
                + $"コヨーテ宣言者　{PresentationText.ParticipantName(result.CoyoteDeclarerId)}";
            _resultDetails.text = string.Join(
                "\n",
                result.Participants.Select(item =>
                    $"{PresentationText.ParticipantName(item.Id)}：ライフ {item.Life}{(item.IsEliminated ? "（脱落）" : string.Empty)}"
                )
            );
            SetButtonEnabled(_nextRoundButton, _game.State == GameFlowState.RoundResult);
            SetVisible(_gameOverDialog, _game.State == GameFlowState.GameOver);
            _outcomeLabel.text = _game.Outcome == GameOutcome.UserVictory ? "勝利！" : "敗北…";
        }
    }
}
