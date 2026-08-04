using System;

namespace CoyoteBattle.Domain
{
    /// <summary>
    /// 山札内の個体を識別できる、不変なカードです。
    /// </summary>
    public sealed class Card
    {
        private static readonly int[] AllowedNumberValues =
        {
            -10,
            -5,
            0,
            1,
            2,
            3,
            4,
            5,
            10,
            15,
            20,
        };

        private Card(int id, CardKind kind, int? value)
        {
            Id = id;
            Kind = kind;
            Value = value;
        }

        /// <summary>
        /// 山札内でカード個体を識別する値を取得します。
        /// </summary>
        public int Id { get; }

        /// <summary>
        /// カードの種類を取得します。
        /// </summary>
        public CardKind Kind { get; }

        /// <summary>
        /// 数字カードの値を取得します。特殊カードではnullです。
        /// </summary>
        public int? Value { get; }

        /// <summary>
        /// Version 1で許可された値を持つ数字カードを生成します。
        /// </summary>
        /// <param name="id">0以上のカード個体識別子です。</param>
        /// <param name="value">Version 1の山札に存在する数値です。</param>
        /// <returns>指定値を持つ数字カードです。</returns>
        public static Card CreateNumber(int id, int value)
        {
            ValidateId(id);
            if (Array.IndexOf(AllowedNumberValues, value) < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    value,
                    "Version 1の山札に存在しない数値です。"
                );
            }

            return new Card(id, CardKind.Number, value);
        }

        /// <summary>
        /// 数値を持たない特殊カードを生成します。
        /// </summary>
        /// <param name="id">0以上のカード個体識別子です。</param>
        /// <param name="kind">数字カード以外のカード種別です。</param>
        /// <returns>指定された種類の特殊カードです。</returns>
        public static Card CreateSpecial(int id, CardKind kind)
        {
            ValidateId(id);
            if (kind == CardKind.Number || !Enum.IsDefined(typeof(CardKind), kind))
            {
                throw new ArgumentException("特殊カードの種類を指定してください。", nameof(kind));
            }

            return new Card(id, kind, null);
        }

        /// <summary>
        /// カード個体識別子が有効であることを検証します。
        /// </summary>
        /// <param name="id">検証対象の識別子です。</param>
        private static void ValidateId(int id)
        {
            if (id < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(id),
                    id,
                    "識別子は0以上で指定してください。"
                );
            }
        }
    }
}
