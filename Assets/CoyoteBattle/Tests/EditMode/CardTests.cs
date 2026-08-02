using CoyoteBattle.Domain;
using NUnit.Framework;

namespace CoyoteBattle.Domain.Tests
{
    public sealed class CardTests
    {
        /// <summary>
        /// コンストラクターへ渡した値がカードから取得できることを保証します。
        /// </summary>
        [Test]
        public void Value_生成時に数値を指定する_指定した数値を返す()
        {
            var card = new Card(10);

            Assert.That(card.Value, Is.EqualTo(10));
        }

        /// <summary>
        /// 通常値だけでなく、特殊カードに必要な負数とゼロも変換せず保持することを保証します。
        /// </summary>
        /// <param name="value">確認対象となるカードの値です。</param>
        [TestCase(-10)]
        [TestCase(0)]
        [TestCase(20)]
        public void Value_正負とゼロのカードを生成する_値をそのまま保持する(int value)
        {
            var card = new Card(value);

            Assert.That(card.Value, Is.EqualTo(value));
        }
    }
}
