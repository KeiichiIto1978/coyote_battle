using CoyoteBattle.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace CoyoteBattle.Tests.Presentation
{
    /// <summary>
    /// ノッチや横長画面でも主要操作をSafe Area内へ寄せる換算を保証します。
    /// </summary>
    public sealed class SafeAreaPaddingCalculatorTests
    {
        [Test]
        public void Calculate_横長画面の左右100ピクセル_基準解像度へ換算する()
        {
            var padding = SafeAreaPaddingCalculator.Calculate(
                2400,
                1080,
                new Rect(100, 0, 2200, 1080),
                new Vector2(1920, 1080)
            );

            Assert.That(padding, Is.EqualTo(new Vector4(80, 0, 80, 0)));
        }

        [Test]
        public void Calculate_標準画面全域_余白なしを返す()
        {
            var padding = SafeAreaPaddingCalculator.Calculate(
                1280,
                720,
                new Rect(0, 0, 1280, 720),
                new Vector2(1920, 1080)
            );

            Assert.That(padding, Is.EqualTo(Vector4.zero));
        }
    }
}
