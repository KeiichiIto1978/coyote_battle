using System;
using CoyoteBattle.Domain;
using NUnit.Framework;

namespace CoyoteBattle.Domain.Tests
{
    public sealed class CardTests
    {
        /// <summary>
        /// 数字カードが識別子と許可された値を保持することを保証します。
        /// </summary>
        [Test]
        public void CreateNumber_許可された数値を指定する_数字カードを生成する()
        {
            var card = Card.CreateNumber(7, 10);

            Assert.That(card.Id, Is.EqualTo(7));
            Assert.That(card.Kind, Is.EqualTo(CardKind.Number));
            Assert.That(card.Value, Is.EqualTo(10));
        }

        /// <summary>
        /// 通常の0と山札再構築効果を持つ夜カードを混同しないことを保証します。
        /// </summary>
        [Test]
        public void Create_通常の0と夜カードを生成する_異なる種類として識別する()
        {
            var zero = Card.CreateNumber(1, 0);
            var night = Card.CreateSpecial(2, CardKind.Night);

            Assert.That(zero.Kind, Is.EqualTo(CardKind.Number));
            Assert.That(zero.Value, Is.EqualTo(0));
            Assert.That(night.Kind, Is.EqualTo(CardKind.Night));
            Assert.That(night.Value, Is.Null);
        }

        /// <summary>
        /// Version 1の山札に存在しない数値カードを生成できないことを保証します。
        /// </summary>
        [Test]
        public void CreateNumber_未定義の数値を指定する_例外を送出する()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Card.CreateNumber(1, 6));
        }

        /// <summary>
        /// 特殊カード生成APIから数字カードを生成する不正状態を防ぎます。
        /// </summary>
        [Test]
        public void CreateSpecial_数字カード種別を指定する_例外を送出する()
        {
            Assert.Throws<ArgumentException>(() => Card.CreateSpecial(1, CardKind.Number));
        }

        /// <summary>
        /// カード個体の識別に使えない負の識別子を拒否することを保証します。
        /// </summary>
        [Test]
        public void CreateNumber_負の識別子を指定する_例外を送出する()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Card.CreateNumber(-1, 1));
        }
    }
}
