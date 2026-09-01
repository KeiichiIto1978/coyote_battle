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
        public void Calculate_横長画面の左右100ピクセル_パネルの単一スケールで換算する()
        {
            var padding = SafeAreaPaddingCalculator.Calculate(
                2400,
                1080,
                new Rect(100, 0, 2200, 1080),
                new Vector2(1920, 1080)
            );

            var expected = 100f / Mathf.Sqrt(1.25f);
            Assert.That(padding.x, Is.EqualTo(expected).Within(0.001f));
            Assert.That(padding.y, Is.Zero);
            Assert.That(padding.z, Is.EqualTo(expected).Within(0.001f));
            Assert.That(padding.w, Is.Zero);
        }

        /// <summary>
        /// 非対称なノッチも全辺を同じパネルスケールで換算することを保証します。
        /// </summary>
        [Test]
        public void Calculate_非対称な四辺余白_各辺の物理ピクセル数を保つ()
        {
            var padding = SafeAreaPaddingCalculator.Calculate(
                2400,
                1080,
                new Rect(120, 43, 2208, 1015),
                new Vector2(1920, 1080)
            );

            var panelScale = Mathf.Sqrt(1.25f);
            Assert.That(padding.x * panelScale, Is.EqualTo(120f).Within(0.001f));
            Assert.That(padding.y * panelScale, Is.EqualTo(22f).Within(0.001f));
            Assert.That(padding.z * panelScale, Is.EqualTo(72f).Within(0.001f));
            Assert.That(padding.w * panelScale, Is.EqualTo(43f).Within(0.001f));
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
