using System;
using CoyoteBattle.Domain;

namespace CoyoteBattle.Presentation
{
    /// <summary>
    /// 実行環境の疑似乱数をDomainの乱数契約へ適合させます。
    /// </summary>
    public sealed class SystemRandomSource : IRandomSource
    {
        private readonly Random _random;

        /// <summary>
        /// 時刻由来のシードで乱数源を初期化します。
        /// </summary>
        public SystemRandomSource()
            : this(new Random(Guid.NewGuid().GetHashCode())) { }

        internal SystemRandomSource(Random random)
        {
            _random = random ?? throw new ArgumentNullException(nameof(random));
        }

        /// <summary>
        /// 0以上、指定した上限未満の整数を返します。
        /// </summary>
        /// <param name="maxExclusive">含まない正の上限です。</param>
        /// <returns>契約範囲内の疑似乱数です。</returns>
        public int Next(int maxExclusive)
        {
            if (maxExclusive <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxExclusive));
            }

            return _random.Next(maxExclusive);
        }
    }
}
