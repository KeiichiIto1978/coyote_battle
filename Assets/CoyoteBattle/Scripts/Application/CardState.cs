using CoyoteBattle.Domain;

namespace CoyoteBattle.Application
{
    /// <summary>
    /// Presentation層へ公開できる変更不能なカード情報です。
    /// </summary>
    public sealed class CardState
    {
        internal CardState(Card card)
        {
            Id = card.Id;
            Kind = card.Kind;
            Value = card.Value;
        }

        /// <summary>
        /// カード個体識別子を取得します。
        /// </summary>
        public int Id { get; }

        /// <summary>
        /// カード種別を取得します。
        /// </summary>
        public CardKind Kind { get; }

        /// <summary>
        /// 数字カードの値を取得します。特殊カードではnullです。
        /// </summary>
        public int? Value { get; }
    }
}
