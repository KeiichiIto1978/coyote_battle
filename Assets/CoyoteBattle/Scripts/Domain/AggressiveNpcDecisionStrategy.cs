namespace CoyoteBattle.Domain
{
    /// <summary>
    /// 35%以上成立する最大値を選ぶ強気型NPC判断です。
    /// </summary>
    public sealed class AggressiveNpcDecisionStrategy : ThresholdNpcDecisionStrategy
    {
        /// <summary>
        /// 指定推定器を使う強気型判断を生成します。
        /// </summary>
        /// <param name="estimator">公開札から実合計分布を作る推定器です。</param>
        public AggressiveNpcDecisionStrategy(NpcFieldTotalEstimator estimator)
            : base(estimator) { }

        protected override double GetThreshold(NpcObservation observation)
        {
            return 0.35d;
        }

        protected override int GetMaximumRaise(NpcObservation observation, double threshold)
        {
            return 3;
        }
    }
}
