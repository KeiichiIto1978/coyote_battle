using System;

namespace CoyoteBattle.Domain
{
    /// <summary>
    /// 判断ごとに20%、50%、80%の閾値を等確率で選ぶギャンブル型NPC判断です。
    /// </summary>
    public sealed class GamblingNpcDecisionStrategy : ThresholdNpcDecisionStrategy
    {
        private static readonly double[] Thresholds = { 0.20d, 0.50d, 0.80d };
        private readonly IRandomSource _randomSource;

        /// <summary>
        /// 共通推定器とNPC判断専用乱数を使うギャンブル型判断を生成します。
        /// </summary>
        /// <param name="estimator">公開札から実合計分布を作る推定器です。</param>
        /// <param name="randomSource">閾値を選ぶNPC判断専用乱数源です。</param>
        public GamblingNpcDecisionStrategy(
            NpcFieldTotalEstimator estimator,
            IRandomSource randomSource
        )
            : base(estimator)
        {
            _randomSource = randomSource ?? throw new ArgumentNullException(nameof(randomSource));
        }

        protected override double GetThreshold(NpcObservation observation)
        {
            var index = _randomSource.Next(Thresholds.Length);
            if (index < 0 || index >= Thresholds.Length)
            {
                throw new InvalidOperationException("NPC判断乱数が指定範囲外の値を返しました。");
            }

            return Thresholds[index];
        }
    }
}
