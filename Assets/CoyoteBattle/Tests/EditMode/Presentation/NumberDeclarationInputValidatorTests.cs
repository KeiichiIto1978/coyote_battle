using CoyoteBattle.Presentation;
using NUnit.Framework;

namespace CoyoteBattle.Tests.Presentation
{
    /// <summary>
    /// UIから不正な宣言値がApplication層へ渡る回帰を防ぎます。
    /// </summary>
    public sealed class NumberDeclarationInputValidatorTests
    {
        [TestCase("1", null, 1)]
        [TestCase(" 42 ", 10, 42)]
        [TestCase("2147483647", 10, int.MaxValue)]
        public void TryValidate_有効な半角整数_値を返す(string input, int? previous, int expected)
        {
            Assert.That(
                NumberDeclarationInputValidator.TryValidate(
                    input,
                    previous,
                    out var value,
                    out var error
                ),
                Is.True
            );
            Assert.That(value, Is.EqualTo(expected));
            Assert.That(error, Is.Empty);
        }

        [TestCase("")]
        [TestCase(" ")]
        [TestCase("０")]
        [TestCase("+1")]
        [TestCase("1.5")]
        [TestCase("-1")]
        [TestCase("2147483648")]
        public void TryValidate_形式または範囲が不正_エラーを返す(string input)
        {
            Assert.That(
                NumberDeclarationInputValidator.TryValidate(input, null, out _, out var error),
                Is.False
            );
            Assert.That(error, Is.Not.Empty);
        }

        [TestCase("9", 10)]
        [TestCase("10", 10)]
        public void TryValidate_直前宣言以下_エラーを返す(string input, int previous)
        {
            Assert.That(
                NumberDeclarationInputValidator.TryValidate(input, previous, out _, out var error),
                Is.False
            );
            Assert.That(error, Does.Contain("大きい"));
        }
    }
}
