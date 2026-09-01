using CoyoteBattle.Presentation;
using NUnit.Framework;

namespace CoyoteBattle.Tests.Presentation
{
    /// <summary>
    /// 本番用乱数源がDomain契約の範囲を守ることを保証します。
    /// </summary>
    public sealed class SystemRandomSourceTests
    {
        [Test]
        public void Next_正の上限_0以上上限未満を返す()
        {
            var random = new SystemRandomSource();
            for (var index = 0; index < 100; index++)
            {
                Assert.That(random.Next(5), Is.InRange(0, 4));
            }
        }
    }
}
