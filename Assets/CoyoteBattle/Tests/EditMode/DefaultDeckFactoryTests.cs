using System.Linq;
using CoyoteBattle.Domain;
using NUnit.Framework;

namespace CoyoteBattle.Domain.Tests
{
    public sealed class DefaultDeckFactoryTests
    {
        /// <summary>
        /// Version 1の山札が合意済みの36枚構成と完全に一致することを保証します。
        /// </summary>
        [Test]
        public void Create_既定山札を生成する_種類別枚数が仕様と一致する()
        {
            var cards = DefaultDeckFactory.Create();

            Assert.That(cards, Has.Count.EqualTo(36));
            AssertNumberCount(cards, 20, 1);
            AssertNumberCount(cards, 15, 2);
            AssertNumberCount(cards, 10, 3);
            AssertNumberCount(cards, 5, 4);
            AssertNumberCount(cards, 4, 4);
            AssertNumberCount(cards, 3, 4);
            AssertNumberCount(cards, 2, 4);
            AssertNumberCount(cards, 1, 4);
            AssertNumberCount(cards, 0, 3);
            AssertNumberCount(cards, -5, 2);
            AssertNumberCount(cards, -10, 1);
            AssertKindCount(cards, CardKind.Night, 1);
            AssertKindCount(cards, CardKind.Double, 1);
            AssertKindCount(cards, CardKind.MaxToZero, 1);
            AssertKindCount(cards, CardKind.Mystery, 1);
            Assert.That(cards.Select(card => card.Id), Is.Unique);
        }

        /// <summary>
        /// 指定値を持つ数字カードの枚数が期待値と一致することを確認します。
        /// </summary>
        private static void AssertNumberCount(
            System.Collections.Generic.IEnumerable<Card> cards,
            int value,
            int expectedCount
        )
        {
            Assert.That(
                cards.Count(card => card.Kind == CardKind.Number && card.Value == value),
                Is.EqualTo(expectedCount),
                $"数字カード {value} の枚数"
            );
        }

        /// <summary>
        /// 指定された特殊カードの枚数が期待値と一致することを確認します。
        /// </summary>
        private static void AssertKindCount(
            System.Collections.Generic.IEnumerable<Card> cards,
            CardKind kind,
            int expectedCount
        )
        {
            Assert.That(
                cards.Count(card => card.Kind == kind),
                Is.EqualTo(expectedCount),
                $"特殊カード {kind} の枚数"
            );
        }
    }
}
