using System;
using CoyoteBattle.Domain;

namespace CoyoteBattle.Application
{
    /// <summary>
    /// 配布先と、公開可否を適用したカード情報です。
    /// </summary>
    public sealed class DealtCardState
    {
        internal DealtCardState(CardDeal deal, bool isHidden)
        {
            if (deal == null)
            {
                throw new ArgumentNullException(nameof(deal));
            }

            ParticipantId = deal.ParticipantId;
            IsHidden = isHidden;
            Card = isHidden ? null : new CardState(deal.Card);
        }

        /// <summary>
        /// カードを配布された参加者識別子を取得します。
        /// </summary>
        public string ParticipantId { get; }

        /// <summary>
        /// カード内容が伏せられているかを取得します。
        /// </summary>
        public bool IsHidden { get; }

        /// <summary>
        /// 公開可能なカード情報を取得します。伏せ札ではnullです。
        /// </summary>
        public CardState Card { get; }
    }
}
