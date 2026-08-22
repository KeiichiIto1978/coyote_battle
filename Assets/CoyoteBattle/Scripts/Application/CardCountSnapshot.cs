using CoyoteBattle.Domain;

namespace CoyoteBattle.Application
{
    /// <summary>
    /// カード表示単位ごとの初期枚数と、現在の使用済み枚数を保持する不変スナップショットです。
    /// </summary>
    public sealed class CardCountSnapshot
    {
        /// <summary>
        /// 表示対象のカード種別と値、初期枚数、使用済み枚数を設定します。
        /// </summary>
        /// <param name="kind">カード種別です。</param>
        /// <param name="value">数字カードの値です。特殊カードではnullです。</param>
        /// <param name="initialCount">初期デッキに含まれる枚数です。</param>
        /// <param name="discardedCount">現在の捨て札に含まれる枚数です。</param>
        internal CardCountSnapshot(CardKind kind, int? value, int initialCount, int discardedCount)
        {
            Kind = kind;
            Value = value;
            InitialCount = initialCount;
            DiscardedCount = discardedCount;
        }

        /// <summary>
        /// カード種別を取得します。
        /// </summary>
        public CardKind Kind { get; }

        /// <summary>
        /// 数字カードの値を取得します。特殊カードではnullです。
        /// </summary>
        public int? Value { get; }

        /// <summary>
        /// 初期デッキに含まれる枚数を取得します。
        /// </summary>
        public int InitialCount { get; }

        /// <summary>
        /// 現在の捨て札に含まれる枚数を取得します。
        /// </summary>
        public int DiscardedCount { get; }
    }
}
