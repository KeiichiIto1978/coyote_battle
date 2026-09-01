using System;
using System.Collections.Generic;
using CoyoteBattle.Domain;
using NUnit.Framework;

namespace CoyoteBattle.Domain.Tests
{
    public sealed class FieldTotalCalculatorTests
    {
        private readonly FieldTotalCalculator _calculator = new FieldTotalCalculator();

        /// <summary>
        /// 正数、負数、0を符号どおり加算する基本計算を保証します。
        /// </summary>
        [Test]
        public void TryCalculate_通常の数字カードを指定する_合計18を返す()
        {
            var cards = new[] { Number(1, 20), Number(2, 1), Number(3, -5), Number(4, 2) };

            var succeeded = _calculator.TryCalculate(cards, null, out var result);

            Assert.That(succeeded, Is.True);
            Assert.That(result.Total, Is.EqualTo(18));
            Assert.That(result.AdditionalCard, Is.Null);
            Assert.That(result.ShouldRebuildDeck, Is.False);
        }

        /// <summary>
        /// カードがない場を0として安全に扱うことを保証します。
        /// </summary>
        [Test]
        public void TryCalculate_空の場を指定する_合計0を返す()
        {
            var succeeded = _calculator.TryCalculate(Array.Empty<Card>(), null, out var result);

            Assert.That(succeeded, Is.True);
            Assert.That(result.Total, Is.Zero);
        }

        /// <summary>
        /// ×2を通常合計の後に一度だけ適用することを保証します。
        /// </summary>
        [Test]
        public void TryCalculate_数字カードと二倍カードを指定する_通常合計を二倍する()
        {
            var cards = new[]
            {
                Number(1, 20),
                Number(2, 1),
                Number(3, -5),
                Number(4, 2),
                Special(5, CardKind.Double),
            };

            var succeeded = _calculator.TryCalculate(cards, null, out var result);

            Assert.That(succeeded, Is.True);
            Assert.That(result.Total, Is.EqualTo(36));
        }

        /// <summary>
        /// MAX→0が最大の数字カード1枚だけを0として扱うことを保証します。
        /// </summary>
        [Test]
        public void TryCalculate_最大値無効カードを指定する_最大値1枚を除外する()
        {
            var cards = new[]
            {
                Number(1, 10),
                Number(2, 5),
                Number(3, 4),
                Special(4, CardKind.MaxToZero),
            };

            var succeeded = _calculator.TryCalculate(cards, null, out var result);

            Assert.That(succeeded, Is.True);
            Assert.That(result.Total, Is.EqualTo(9));
        }

        /// <summary>
        /// 最大値が同値でもMAX→0が全枚ではなく1枚だけを無効化することを保証します。
        /// </summary>
        [Test]
        public void TryCalculate_同じ最大値が複数ある_MAXから1枚だけを除外する()
        {
            var cards = new[]
            {
                Number(1, 10),
                Number(2, 10),
                Number(3, 5),
                Special(4, CardKind.MaxToZero),
            };

            var succeeded = _calculator.TryCalculate(cards, null, out var result);

            Assert.That(succeeded, Is.True);
            Assert.That(result.Total, Is.EqualTo(15));
        }

        /// <summary>
        /// 全て負数でも最も0に近い最大値1枚を0にすることを保証します。
        /// </summary>
        [Test]
        public void TryCalculate_負数だけと最大値無効カードを指定する_最大の負数を除外する()
        {
            var cards = new[] { Number(1, -5), Number(2, -10), Special(3, CardKind.MaxToZero) };

            var succeeded = _calculator.TryCalculate(cards, null, out var result);

            Assert.That(succeeded, Is.True);
            Assert.That(result.Total, Is.EqualTo(-10));
        }

        /// <summary>
        /// 数字カードがない場合のMAX→0を効果なしとして扱うことを保証します。
        /// </summary>
        [Test]
        public void TryCalculate_最大値無効カードだけを指定する_合計0を返す()
        {
            var cards = new[] { Special(1, CardKind.MaxToZero) };

            var succeeded = _calculator.TryCalculate(cards, null, out var result);

            Assert.That(succeeded, Is.True);
            Assert.That(result.Total, Is.Zero);
        }

        /// <summary>
        /// ？で引いた数字を合計し、追加札として結果から追跡できることを保証します。
        /// </summary>
        [Test]
        public void TryCalculate_疑問カードで数字を引く_追加値とカードを返す()
        {
            var additionalCard = Number(3, 15);
            var source = new StubAdditionalCardSource(additionalCard);
            var cards = new[] { Number(1, 1), Special(2, CardKind.Mystery) };

            var succeeded = _calculator.TryCalculate(cards, source, out var result);

            Assert.That(succeeded, Is.True);
            Assert.That(result.Total, Is.EqualTo(16));
            Assert.That(result.AdditionalCard, Is.SameAs(additionalCard));
        }

        /// <summary>
        /// ？で引いた×2をMAX→0と合計の後に適用することを保証します。
        /// </summary>
        [Test]
        public void TryCalculate_疑問カードで二倍カードを引く_確定順で特殊効果を適用する()
        {
            var cards = new[]
            {
                Number(1, 5),
                Number(2, -5),
                Special(3, CardKind.MaxToZero),
                Special(4, CardKind.Mystery),
            };
            var source = new StubAdditionalCardSource(Special(5, CardKind.Double));

            var succeeded = _calculator.TryCalculate(cards, source, out var result);

            Assert.That(succeeded, Is.True);
            Assert.That(result.Total, Is.EqualTo(-10));
        }

        /// <summary>
        /// ？で引いたMAX→0を追加札を含む数字カードへ適用することを保証します。
        /// </summary>
        [Test]
        public void TryCalculate_疑問カードで最大値無効カードを引く_最大値を除外する()
        {
            var cards = new[] { Number(1, 10), Number(2, 5), Special(3, CardKind.Mystery) };
            var source = new StubAdditionalCardSource(Special(4, CardKind.MaxToZero));

            var succeeded = _calculator.TryCalculate(cards, source, out var result);

            Assert.That(succeeded, Is.True);
            Assert.That(result.Total, Is.EqualTo(5));
        }

        /// <summary>
        /// ？で夜カードを引いた場合に0を加え、全山札再構築を要求することを保証します。
        /// </summary>
        [Test]
        public void TryCalculate_疑問カードで夜カードを引く_再構築要求を返す()
        {
            var nightCard = Special(3, CardKind.Night);
            var cards = new[] { Number(1, 20), Special(2, CardKind.Mystery) };
            var source = new StubAdditionalCardSource(nightCard);

            var succeeded = _calculator.TryCalculate(cards, source, out var result);

            Assert.That(succeeded, Is.True);
            Assert.That(result.Total, Is.EqualTo(20));
            Assert.That(result.AdditionalCard, Is.SameAs(nightCard));
            Assert.That(result.ShouldRebuildDeck, Is.True);
        }

        /// <summary>
        /// ？、MAX→0、×2の複合ケースを合意済みの順番で評価することを保証します。
        /// </summary>
        [Test]
        public void TryCalculate_三種類の特殊効果を指定する_疑問解決後に最大値無効と二倍を適用する()
        {
            var cards = new[]
            {
                Number(1, 10),
                Number(2, 5),
                Special(3, CardKind.MaxToZero),
                Special(4, CardKind.Double),
                Special(5, CardKind.Mystery),
            };
            var source = new StubAdditionalCardSource(Number(6, 4));

            var succeeded = _calculator.TryCalculate(cards, source, out var result);

            Assert.That(succeeded, Is.True);
            Assert.That(result.Total, Is.EqualTo(18));
        }

        /// <summary>
        /// 夜カードが直接場にある場合にも山札再構築要求を返すことを保証します。
        /// </summary>
        [Test]
        public void TryCalculate_夜カードが場にある_合計を変えず再構築要求を返す()
        {
            var cards = new[] { Number(1, 5), Special(2, CardKind.Night) };

            var succeeded = _calculator.TryCalculate(cards, null, out var result);

            Assert.That(succeeded, Is.True);
            Assert.That(result.Total, Is.EqualTo(5));
            Assert.That(result.ShouldRebuildDeck, Is.True);
        }

        /// <summary>
        /// 同種特殊カードが複数ある不正な場を計算前に拒否します。
        /// </summary>
        [Test]
        public void TryCalculate_同種特殊カードが複数ある_例外を送出する()
        {
            var cards = new[] { Special(1, CardKind.Double), Special(2, CardKind.Double) };

            Assert.Throws<ArgumentException>(() => _calculator.TryCalculate(cards, null, out _));
        }

        /// <summary>
        /// ？の追加カードを取得できない場合に計算結果を確定しないことを保証します。
        /// </summary>
        [Test]
        public void TryCalculate_追加カードを取得できない_失敗して結果を返さない()
        {
            var cards = new[] { Special(1, CardKind.Mystery) };
            var source = new StubAdditionalCardSource();

            var succeeded = _calculator.TryCalculate(cards, source, out var result);

            Assert.That(succeeded, Is.False);
            Assert.That(result, Is.Null);
        }

        /// <summary>
        /// 山札を追加カード源として使い、？で引いた夜カードを追跡して再構築できることを保証します。
        /// </summary>
        [Test]
        public void TryCalculate_山札から夜カードを追加する_計算結果と山札領域を連携する()
        {
            var deck = Deck.Create(
                new[] { Special(1, CardKind.Mystery), Special(2, CardKind.Night), Number(3, 1) },
                new IdentityRandomSource()
            );
            deck.TryDeal(new[] { "user" }, out _);

            var succeeded = _calculator.TryCalculate(deck.InPlayCards, deck, out var result);

            Assert.That(succeeded, Is.True);
            Assert.That(result.AdditionalCard.Kind, Is.EqualTo(CardKind.Night));
            Assert.That(result.ShouldRebuildDeck, Is.True);
            Assert.That(deck.AdditionalCardCount, Is.EqualTo(1));
            Assert.That(deck.TryCompleteRound(), Is.True);
            Assert.That(deck.DrawPileCount, Is.EqualTo(3));
        }

        /// <summary>
        /// 山札に追加札がない場合、？の計算失敗前後で全領域を維持することを保証します。
        /// </summary>
        [Test]
        public void TryCalculate_山札に追加札がない_計算と山札を変更せず失敗する()
        {
            var deck = Deck.Create(
                new[] { Special(1, CardKind.Mystery) },
                new IdentityRandomSource()
            );
            deck.TryDeal(new[] { "user" }, out _);

            var succeeded = _calculator.TryCalculate(deck.InPlayCards, deck, out var result);

            Assert.That(succeeded, Is.False);
            Assert.That(result, Is.Null);
            Assert.That(deck.DrawPileCount, Is.Zero);
            Assert.That(deck.InPlayCount, Is.EqualTo(1));
            Assert.That(deck.AdditionalCardCount, Is.Zero);
        }

        /// <summary>
        /// nullの場を入力エラーとして拒否することを保証します。
        /// </summary>
        [Test]
        public void TryCalculate_nullの場を指定する_例外を送出する()
        {
            Assert.Throws<ArgumentNullException>(() => _calculator.TryCalculate(null, null, out _));
        }

        /// <summary>
        /// 数字カードを簡潔に生成します。
        /// </summary>
        private static Card Number(int id, int value)
        {
            return Card.CreateNumber(id, value);
        }

        /// <summary>
        /// 特殊カードを簡潔に生成します。
        /// </summary>
        private static Card Special(int id, CardKind kind)
        {
            return Card.CreateSpecial(id, kind);
        }

        private sealed class StubAdditionalCardSource : IAdditionalCardSource
        {
            private readonly Queue<Card> _cards;

            /// <summary>
            /// 取得順が固定された追加カード源を生成します。
            /// </summary>
            public StubAdditionalCardSource(params Card[] cards)
            {
                _cards = new Queue<Card>(cards);
            }

            /// <summary>
            /// キューの先頭カードを返し、空なら状態を変えず失敗します。
            /// </summary>
            public bool TryDrawAdditional(out Card card)
            {
                if (_cards.Count == 0)
                {
                    card = null;
                    return false;
                }

                card = _cards.Dequeue();
                return true;
            }
        }

        private sealed class IdentityRandomSource : IRandomSource
        {
            /// <summary>
            /// Fisher-Yatesで現在位置を選び、入力順を維持します。
            /// </summary>
            public int Next(int exclusiveUpperBound)
            {
                return exclusiveUpperBound - 1;
            }
        }
    }
}
