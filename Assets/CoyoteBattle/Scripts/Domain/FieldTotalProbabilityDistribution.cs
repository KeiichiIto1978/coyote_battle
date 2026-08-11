using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace CoyoteBattle.Domain
{
    /// <summary>
    /// NPCから見た実合計ごとの確率を保持する変更不能な分布です。
    /// </summary>
    public sealed class FieldTotalProbabilityDistribution
    {
        private readonly IReadOnlyDictionary<int, double> _probabilities;

        internal FieldTotalProbabilityDistribution(IReadOnlyDictionary<int, long> weights)
        {
            if (weights == null || weights.Count == 0 || weights.Values.Any(value => value <= 0))
            {
                throw new ArgumentException(
                    "1件以上の正の重みを指定してください。",
                    nameof(weights)
                );
            }

            var totalWeight = weights.Values.Sum();
            var probabilities = weights.ToDictionary(
                item => item.Key,
                item => (double)item.Value / totalWeight
            );
            _probabilities = new ReadOnlyDictionary<int, double>(probabilities);
        }

        /// <summary>
        /// 実合計をキーとする確率一覧を取得します。
        /// </summary>
        public IReadOnlyDictionary<int, double> Probabilities => _probabilities;

        /// <summary>
        /// 実合計が指定値以上となる確率を返します。
        /// </summary>
        /// <param name="value">成立確率を求める宣言値です。</param>
        /// <returns>0以上1以下の成立確率です。</returns>
        public double ProbabilityAtLeast(int value)
        {
            return _probabilities.Where(item => item.Key >= value).Sum(item => item.Value);
        }
    }
}
