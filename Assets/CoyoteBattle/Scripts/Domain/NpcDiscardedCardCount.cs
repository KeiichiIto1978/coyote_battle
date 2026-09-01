using System;
using System.Linq;

namespace CoyoteBattle.Domain
{
    /// <summary>
    /// NPCへ公開する使用済み札を、カードの種類と値ごとの枚数で表します。
    /// </summary>
    public sealed class NpcDiscardedCardCount
    {
        /// <summary>
        /// 既定山札に存在するカード面と、1枚以上の使用済み枚数を設定します。
        /// </summary>
        /// <param name="kind">カード種別です。</param>
        /// <param name="value">数字カードの値です。特殊カードではnullです。</param>
        /// <param name="count">現在の使用済み枚数です。</param>
        public NpcDiscardedCardCount(CardKind kind, int? value, int count)
        {
            var initialCount = DefaultDeckFactory
                .Create()
                .Count(card => card.Kind == kind && card.Value == value);
            if (initialCount == 0)
            {
                throw new ArgumentException("既定山札に存在するカード面を指定してください。");
            }

            if (count < 1 || count > initialCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(count),
                    count,
                    $"使用済み枚数は1以上{initialCount}以下で指定してください。"
                );
            }

            Kind = kind;
            Value = value;
            Count = count;
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
        /// 現在の使用済み枚数を取得します。
        /// </summary>
        public int Count { get; }
    }
}
