using System.Collections.Generic;

namespace CoyoteBattle.Domain
{
    /// <summary>
    /// Version 1で使用する36枚のカードを生成します。
    /// </summary>
    public static class DefaultDeckFactory
    {
        /// <summary>
        /// 合意済みの種類別枚数を持つ、未シャッフルの山札を生成します。
        /// </summary>
        /// <returns>個体識別子が重複しない36枚のカードです。</returns>
        public static IReadOnlyList<Card> Create()
        {
            var cards = new List<Card>(36);
            var nextId = 0;

            AddNumbers(cards, ref nextId, 20, 1);
            AddNumbers(cards, ref nextId, 15, 2);
            AddNumbers(cards, ref nextId, 10, 3);
            AddNumbers(cards, ref nextId, 5, 4);
            AddNumbers(cards, ref nextId, 4, 4);
            AddNumbers(cards, ref nextId, 3, 4);
            AddNumbers(cards, ref nextId, 2, 4);
            AddNumbers(cards, ref nextId, 1, 4);
            AddNumbers(cards, ref nextId, 0, 3);
            AddNumbers(cards, ref nextId, -5, 2);
            AddNumbers(cards, ref nextId, -10, 1);
            cards.Add(Card.CreateSpecial(nextId++, CardKind.Night));
            cards.Add(Card.CreateSpecial(nextId++, CardKind.Double));
            cards.Add(Card.CreateSpecial(nextId++, CardKind.MaxToZero));
            cards.Add(Card.CreateSpecial(nextId, CardKind.Mystery));

            return cards.AsReadOnly();
        }

        /// <summary>
        /// 同じ値を持つ数字カードを指定枚数追加します。
        /// </summary>
        /// <param name="cards">追加先のカード一覧です。</param>
        /// <param name="nextId">次に割り当てる個体識別子です。</param>
        /// <param name="value">追加する数字カードの値です。</param>
        /// <param name="count">追加枚数です。</param>
        private static void AddNumbers(List<Card> cards, ref int nextId, int value, int count)
        {
            for (var index = 0; index < count; index++)
            {
                cards.Add(Card.CreateNumber(nextId++, value));
            }
        }
    }
}
