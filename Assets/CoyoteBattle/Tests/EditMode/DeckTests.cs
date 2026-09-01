using System;
using System.Collections.Generic;
using System.Linq;
using CoyoteBattle.Domain;
using NUnit.Framework;

namespace CoyoteBattle.Domain.Tests
{
    public sealed class DeckTests
    {
        /// <summary>
        /// 同じ乱数列から同じ既定山札順を再現できることを保証します。
        /// </summary>
        [Test]
        public void CreateDefault_同じ固定乱数を使う_同じ山札順になる()
        {
            var first = Deck.CreateDefault(new IdentityRandomSource());
            var second = Deck.CreateDefault(new IdentityRandomSource());

            Assert.That(
                first.DrawPile.Select(card => card.Id),
                Is.EqualTo(second.DrawPile.Select(card => card.Id))
            );
            Assert.That(first.TotalCardCount, Is.EqualTo(36));
        }

        /// <summary>
        /// 配布対象の順序とドロー順を維持して各対象へ1枚ずつ配ることを保証します。
        /// </summary>
        [Test]
        public void TryDeal_順序付き対象を指定する_順番どおり各1枚を配布する()
        {
            var deck = CreateNumberDeck(3);

            var succeeded = deck.TryDeal(new[] { "user", "npc-1" }, out var deals);

            Assert.That(succeeded, Is.True);
            Assert.That(
                deals.Select(deal => deal.ParticipantId),
                Is.EqualTo(new[] { "user", "npc-1" })
            );
            Assert.That(deals.Select(deal => deal.Card.Id), Is.EqualTo(new[] { 100, 101 }));
            Assert.That(deck.DrawPileCount, Is.EqualTo(1));
            Assert.That(deck.InPlayCount, Is.EqualTo(2));
            Assert.That(deck.TotalCardCount, Is.EqualTo(3));
        }

        /// <summary>
        /// 配布対象が空なら成功として扱い、山札を変更しないことを保証します。
        /// </summary>
        [Test]
        public void TryDeal_空の対象一覧を指定する_空の結果を返して状態を維持する()
        {
            var deck = CreateNumberDeck(2);

            var succeeded = deck.TryDeal(Array.Empty<string>(), out var deals);

            Assert.That(succeeded, Is.True);
            Assert.That(deals, Is.Empty);
            Assert.That(deck.DrawPileCount, Is.EqualTo(2));
            Assert.That(deck.InPlayCount, Is.Zero);
        }

        /// <summary>
        /// 同じ参加者への二重配布を入力エラーとして拒否し、状態を変更しないことを保証します。
        /// </summary>
        [Test]
        public void TryDeal_重複する対象を指定する_例外を送出して状態を維持する()
        {
            var deck = CreateNumberDeck(2);

            Assert.Throws<ArgumentException>(() => deck.TryDeal(new[] { "user", "user" }, out _));
            Assert.That(deck.DrawPileCount, Is.EqualTo(2));
            Assert.That(deck.InPlayCount, Is.Zero);
        }

        /// <summary>
        /// nullの配布対象一覧を拒否し、山札状態を変更しないことを保証します。
        /// </summary>
        [Test]
        public void TryDeal_nullの対象一覧を指定する_例外を送出して状態を維持する()
        {
            var deck = CreateNumberDeck(2);

            Assert.Throws<ArgumentNullException>(() => deck.TryDeal(null, out _));
            Assert.That(deck.DrawPileCount, Is.EqualTo(2));
        }

        /// <summary>
        /// 通常ラウンドの場札と追加札を使用済み札へ移すことを保証します。
        /// </summary>
        [Test]
        public void TryCompleteRound_夜カードがない_場札と追加札を使用済みにする()
        {
            var cards = new[]
            {
                Card.CreateNumber(1, 1),
                Card.CreateSpecial(2, CardKind.Mystery),
                Card.CreateNumber(3, 2),
            };
            var deck = Deck.Create(cards, new IdentityRandomSource());
            deck.TryDeal(new[] { "user", "npc" }, out _);
            deck.TryDrawAdditional(out _);

            var succeeded = deck.TryCompleteRound();

            Assert.That(succeeded, Is.True);
            Assert.That(deck.DrawPileCount, Is.Zero);
            Assert.That(deck.DiscardPileCount, Is.EqualTo(3));
            Assert.That(deck.InPlayCount, Is.Zero);
            Assert.That(deck.AdditionalCardCount, Is.Zero);
        }

        /// <summary>
        /// 残り山札が要求枚数に足りない場合に使用済み札と合わせて再構築することを保証します。
        /// </summary>
        [Test]
        public void TryDeal_残り山札が不足する_使用済み札と再構築して全対象へ配る()
        {
            var deck = CreateNumberDeck(3);
            deck.TryDeal(new[] { "first", "second" }, out _);
            deck.TryCompleteRound();

            var succeeded = deck.TryDeal(new[] { "third", "fourth" }, out var deals);

            Assert.That(succeeded, Is.True);
            Assert.That(deals, Has.Count.EqualTo(2));
            Assert.That(deck.DrawPileCount, Is.EqualTo(1));
            Assert.That(deck.DiscardPileCount, Is.Zero);
            Assert.That(deck.InPlayCount, Is.EqualTo(2));
            Assert.That(deck.TotalCardCount, Is.EqualTo(3));
        }

        /// <summary>
        /// 全利用可能カードでも不足する場合に部分配布せず状態を維持することを保証します。
        /// </summary>
        [Test]
        public void TryDeal_再構築後も不足する_失敗して部分配布しない()
        {
            var deck = CreateNumberDeck(2);
            deck.TryDeal(new[] { "first", "second" }, out _);

            var succeeded = deck.TryDeal(new[] { "third" }, out var deals);

            Assert.That(succeeded, Is.False);
            Assert.That(deals, Is.Empty);
            Assert.That(deck.DrawPileCount, Is.Zero);
            Assert.That(deck.InPlayCount, Is.EqualTo(2));
            Assert.That(deck.TotalCardCount, Is.EqualTo(2));
        }

        /// <summary>
        /// 夜カードが場にある場合に全カードを回収して新しい山札へ戻すことを保証します。
        /// </summary>
        [Test]
        public void TryCompleteRound_夜カードが場にある_全カードを再構築する()
        {
            var cards = new[]
            {
                Card.CreateSpecial(1, CardKind.Night),
                Card.CreateNumber(2, 1),
                Card.CreateNumber(3, 2),
            };
            var deck = Deck.Create(cards, new IdentityRandomSource());
            deck.TryDeal(new[] { "user" }, out _);

            var succeeded = deck.TryCompleteRound();

            Assert.That(succeeded, Is.True);
            Assert.That(deck.DrawPileCount, Is.EqualTo(3));
            Assert.That(deck.DiscardPileCount, Is.Zero);
            Assert.That(deck.InPlayCount, Is.Zero);
            Assert.That(deck.AdditionalCardCount, Is.Zero);
            Assert.That(deck.TotalCardCount, Is.EqualTo(3));
        }

        /// <summary>
        /// 夜カードを追加札として引いた場合も全カード再構築を行うことを保証します。
        /// </summary>
        [Test]
        public void TryCompleteRound_夜カードを追加札として引く_全カードを再構築する()
        {
            var cards = new[]
            {
                Card.CreateSpecial(1, CardKind.Mystery),
                Card.CreateSpecial(2, CardKind.Night),
                Card.CreateNumber(3, 1),
            };
            var deck = Deck.Create(cards, new IdentityRandomSource());
            deck.TryDeal(new[] { "user" }, out _);
            deck.TryDrawAdditional(out var additionalCard);

            var succeeded = deck.TryCompleteRound();

            Assert.That(additionalCard.Kind, Is.EqualTo(CardKind.Night));
            Assert.That(succeeded, Is.True);
            Assert.That(deck.DrawPileCount, Is.EqualTo(3));
            Assert.That(deck.DiscardPileCount, Is.Zero);
            Assert.That(deck.InPlayCount, Is.Zero);
            Assert.That(deck.AdditionalCardCount, Is.Zero);
        }

        /// <summary>
        /// 1枚の？から追加札を複数回引けないことを保証します。
        /// </summary>
        [Test]
        public void TryDrawAdditional_同じ疑問カードで二度呼ぶ_二枚目を引かず失敗する()
        {
            var cards = new[]
            {
                Card.CreateSpecial(1, CardKind.Mystery),
                Card.CreateNumber(2, 1),
                Card.CreateNumber(3, 2),
            };
            var deck = Deck.Create(cards, new IdentityRandomSource());
            deck.TryDeal(new[] { "user" }, out _);
            Assert.That(deck.TryDrawAdditional(out _), Is.True);

            var succeeded = deck.TryDrawAdditional(out var secondCard);

            Assert.That(succeeded, Is.False);
            Assert.That(secondCard, Is.Null);
            Assert.That(deck.DrawPileCount, Is.EqualTo(1));
            Assert.That(deck.AdditionalCardCount, Is.EqualTo(1));
            Assert.That(deck.TotalCardCount, Is.EqualTo(3));
        }

        /// <summary>
        /// 同じラウンドを二重に回収してカード状態を破壊しないことを保証します。
        /// </summary>
        [Test]
        public void TryCompleteRound_回収済みの状態でもう一度呼ぶ_失敗して状態を維持する()
        {
            var deck = CreateNumberDeck(2);
            deck.TryDeal(new[] { "user" }, out _);
            Assert.That(deck.TryCompleteRound(), Is.True);

            var succeeded = deck.TryCompleteRound();

            Assert.That(succeeded, Is.False);
            Assert.That(deck.DrawPileCount, Is.EqualTo(1));
            Assert.That(deck.DiscardPileCount, Is.EqualTo(1));
            Assert.That(deck.TotalCardCount, Is.EqualTo(2));
        }

        /// <summary>
        /// 山札内で同じ個体識別子を持つカードを拒否することを保証します。
        /// </summary>
        [Test]
        public void Create_識別子が重複するカードを渡す_例外を送出する()
        {
            var cards = new[] { Card.CreateNumber(1, 1), Card.CreateNumber(1, 2) };

            Assert.Throws<ArgumentException>(() => Deck.Create(cards, new IdentityRandomSource()));
        }

        /// <summary>
        /// Version 1に一枚しかない特殊カードの重複を山札生成時に拒否します。
        /// </summary>
        [Test]
        public void Create_同種特殊カードを複数渡す_例外を送出する()
        {
            var cards = new[]
            {
                Card.CreateSpecial(1, CardKind.Double),
                Card.CreateSpecial(2, CardKind.Double),
            };

            Assert.Throws<ArgumentException>(() => Deck.Create(cards, new IdentityRandomSource()));
        }

        /// <summary>
        /// 既定山札の種類別枚数を超えた数字カード構成を拒否します。
        /// </summary>
        [Test]
        public void Create_20のカードを2枚渡す_例外を送出する()
        {
            var cards = new[] { Card.CreateNumber(1, 20), Card.CreateNumber(2, 20) };

            Assert.Throws<ArgumentException>(() => Deck.Create(cards, new IdentityRandomSource()));
        }

        /// <summary>
        /// 通常回収後も全領域を通じて各カード個体が一度だけ存在することを保証します。
        /// </summary>
        [Test]
        public void TryCompleteRound_通常回収を行う_全カード個体が一意に残る()
        {
            var deck = CreateNumberDeck(3);
            var originalIds = deck.DrawPile.Select(card => card.Id).OrderBy(id => id).ToArray();
            deck.TryDeal(new[] { "user", "npc" }, out _);

            deck.TryCompleteRound();

            var currentIds = deck
                .DrawPile.Concat(deck.DiscardPile)
                .Concat(deck.InPlayCards)
                .Concat(deck.AdditionalCards)
                .Select(card => card.Id)
                .ToArray();
            Assert.That(currentIds, Is.Unique);
            Assert.That(currentIds.OrderBy(id => id), Is.EqualTo(originalIds));
        }

        /// <summary>
        /// 夜カード再構築中の乱数異常でも再構築前の領域を維持することを保証します。
        /// </summary>
        [Test]
        public void TryCompleteRound_夜カード再構築時の乱数値が範囲外_例外を送出して状態を維持する()
        {
            var randomSource = new SwitchableRandomSource();
            var deck = Deck.Create(
                new[] { Card.CreateSpecial(1, CardKind.Night), Card.CreateNumber(2, 1) },
                randomSource
            );
            deck.TryDeal(new[] { "user" }, out _);
            randomSource.ShouldReturnInvalidValue = true;

            Assert.Throws<InvalidOperationException>(() => deck.TryCompleteRound());
            Assert.That(deck.DrawPileCount, Is.EqualTo(1));
            Assert.That(deck.InPlayCount, Is.EqualTo(1));
            Assert.That(deck.DiscardPileCount, Is.Zero);
            Assert.That(deck.TotalCardCount, Is.EqualTo(2));
        }

        /// <summary>
        /// 乱数源が契約外の値を返した場合、再構築前の全状態を維持することを保証します。
        /// </summary>
        [Test]
        public void TryDeal_再構築時の乱数値が範囲外_例外を送出して状態を維持する()
        {
            var randomSource = new SwitchableRandomSource();
            var deck = Deck.Create(
                new[] { Card.CreateNumber(1, 1), Card.CreateNumber(2, 2), Card.CreateNumber(3, 3) },
                randomSource
            );
            deck.TryDeal(new[] { "first", "second" }, out _);
            deck.TryCompleteRound();
            randomSource.ShouldReturnInvalidValue = true;

            Assert.Throws<InvalidOperationException>(() =>
                deck.TryDeal(new[] { "third", "fourth" }, out _)
            );
            Assert.That(deck.DrawPileCount, Is.EqualTo(1));
            Assert.That(deck.DiscardPileCount, Is.EqualTo(2));
            Assert.That(deck.InPlayCount, Is.Zero);
            Assert.That(deck.TotalCardCount, Is.EqualTo(3));
        }

        /// <summary>
        /// 少数カードで山札ライフサイクルを検証できるテスト用山札を生成します。
        /// </summary>
        private static Deck CreateNumberDeck(int count)
        {
            var cards = Enumerable
                .Range(0, count)
                .Select(index => Card.CreateNumber(100 + index, index % 5 + 1));
            return Deck.Create(cards, new IdentityRandomSource());
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

        private sealed class SwitchableRandomSource : IRandomSource
        {
            public bool ShouldReturnInvalidValue { get; set; }

            /// <summary>
            /// 切替後だけ排他的上限と同じ不正値を返します。
            /// </summary>
            public int Next(int exclusiveUpperBound)
            {
                return ShouldReturnInvalidValue ? exclusiveUpperBound : exclusiveUpperBound - 1;
            }
        }
    }
}
