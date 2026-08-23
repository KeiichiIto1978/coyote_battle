namespace CoyoteBattle.Domain
{
    /// <summary>
    /// 75%以上成立する最大値だけを選ぶ慎重型NPC判断です。
    /// </summary>
    public sealed class CautiousNpcDecisionStrategy : ThresholdNpcDecisionStrategy
    {
        /// <summary>
        /// 指定推定器を使う慎重型判断を生成します。
        /// </summary>
        /// <param name="estimator">公開札から実合計分布を作る推定器です。</param>
        public CautiousNpcDecisionStrategy(NpcFieldTotalEstimator estimator)
            : base(estimator) { }

        protected override double GetThreshold(NpcObservation observation)
        {
            return 0.75d;
        }

        protected override int GetMaximumRaise(NpcObservation observation, double threshold)
        {
            return 1;
        }
    }
}
