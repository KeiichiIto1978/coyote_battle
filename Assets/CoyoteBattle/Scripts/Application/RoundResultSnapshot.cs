using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CoyoteBattle.Domain;

namespace CoyoteBattle.Application
{
    /// <summary>
    /// カード回収後も表示できる、変更不能なラウンド判定結果です。
    /// </summary>
    public sealed class RoundResultSnapshot
    {
        internal RoundResultSnapshot(
            int roundNumber,
            IEnumerable<CardDeal> dealtCards,
            Card additionalCard,
            int actualTotal,
            NumberDeclaration finalDeclaration,
            string coyoteDeclarerId,
            string loserId,
            IEnumerable<Participant> participants,
            string eliminatedParticipantId,
            GameOutcome outcome
        )
        {
            RoundNumber = roundNumber;
            DealtCards = new ReadOnlyCollection<DealtCardState>(
                dealtCards.Select(deal => new DealtCardState(deal, false)).ToList()
            );
            AdditionalCards = new ReadOnlyCollection<CardState>(
                additionalCard == null
                    ? new List<CardState>()
                    : new List<CardState> { new CardState(additionalCard) }
            );
            ActualTotal = actualTotal;
            NumberDeclarerId = finalDeclaration.ParticipantId;
            DeclaredNumber = finalDeclaration.Value;
            CoyoteDeclarerId = coyoteDeclarerId;
            LoserId = loserId;
            Participants = new ReadOnlyCollection<ParticipantState>(
                participants.Select(item => new ParticipantState(item)).ToList()
            );
            EliminatedParticipantIds = new ReadOnlyCollection<string>(
                eliminatedParticipantId == null
                    ? new List<string>()
                    : new List<string> { eliminatedParticipantId }
            );
            Outcome = outcome;
        }

        /// <summary>
        /// 判定したラウンド番号を取得します。
        /// </summary>
        public int RoundNumber { get; }

        /// <summary>
        /// 参加者へ配布された全カードを取得します。
        /// </summary>
        public IReadOnlyList<DealtCardState> DealtCards { get; }

        /// <summary>
        /// 「？」で引いた追加札を取得します。
        /// </summary>
        public IReadOnlyList<CardState> AdditionalCards { get; }

        /// <summary>
        /// 特殊効果適用後の実合計を取得します。
        /// </summary>
        public int ActualTotal { get; }

        /// <summary>
        /// 最後の数字宣言者識別子を取得します。
        /// </summary>
        public string NumberDeclarerId { get; }

        /// <summary>
        /// 最後の数字宣言値を取得します。
        /// </summary>
        public int DeclaredNumber { get; }

        /// <summary>
        /// コヨーテ宣言者識別子を取得します。
        /// </summary>
        public string CoyoteDeclarerId { get; }

        /// <summary>
        /// ラウンド敗者識別子を取得します。
        /// </summary>
        public string LoserId { get; }

        /// <summary>
        /// ライフ減少反映後の全参加者状態を取得します。
        /// </summary>
        public IReadOnlyList<ParticipantState> Participants { get; }

        /// <summary>
        /// この判定で脱落した参加者識別子を取得します。
        /// </summary>
        public IReadOnlyList<string> EliminatedParticipantIds { get; }

        /// <summary>
        /// この判定後の最終勝敗を取得します。
        /// </summary>
        public GameOutcome Outcome { get; }
    }
}
