using System;

namespace CoyoteBattle.Domain
{
    /// <summary>
    /// 配布対象と割り当てられたカードの組です。
    /// </summary>
    public sealed class CardDeal
    {
        /// <summary>
        /// 配布結果を生成します。
        /// </summary>
        /// <param name="participantId">配布対象を識別する空でない値です。</param>
        /// <param name="card">対象へ配布されたカードです。</param>
        public CardDeal(string participantId, Card card)
        {
            if (string.IsNullOrWhiteSpace(participantId))
            {
                throw new ArgumentException(
                    "配布対象の識別子を指定してください。",
                    nameof(participantId)
                );
            }

            ParticipantId = participantId;
            Card = card ?? throw new ArgumentNullException(nameof(card));
        }

        /// <summary>
        /// 配布対象の識別子を取得します。
        /// </summary>
        public string ParticipantId { get; }

        /// <summary>
        /// 配布されたカードを取得します。
        /// </summary>
        public Card Card { get; }
    }
}
