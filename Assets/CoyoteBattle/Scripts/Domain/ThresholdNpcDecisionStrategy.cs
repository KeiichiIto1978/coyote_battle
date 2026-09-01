using System;
using System.Linq;

namespace CoyoteBattle.Domain
{
    /// <summary>
    /// 合計値の成立確率と性格別閾値から共通規則で行動を選択します。
    /// </summary>
    public abstract class ThresholdNpcDecisionStrategy : INpcDecisionStrategy
    {
        private readonly NpcFieldTotalEstimator _estimator;

        /// <summary>
        /// 性格別閾値と組み合わせる共通推定器を設定します。
        /// </summary>
        /// <param name="estimator">公開札から実合計分布を作る推定器です。</param>
        protected ThresholdNpcDecisionStrategy(NpcFieldTotalEstimator estimator)
        {
            _estimator = estimator ?? throw new ArgumentNullException(nameof(estimator));
        }

        /// <summary>
        /// 閾値を満たす最大値を上限に、性格別の上げ幅で数字またはコヨーテを返します。
        /// </summary>
        public NpcDecision Decide(NpcObservation observation)
        {
            if (observation == null)
            {
                throw new ArgumentNullException(nameof(observation));
            }

            var threshold = GetThreshold(observation);
            if (threshold < 0d || threshold > 1d)
            {
                throw new InvalidOperationException(
                    "成立確率の閾値は0以上1以下である必要があります。"
                );
            }

            if (!observation.CanDeclareNumber)
            {
                return NpcDecision.DeclareCoyote();
            }

            var distribution = _estimator.Estimate(observation);
            var minimumNumber = observation.MinimumNumber.Value;
            var candidate = distribution
                .Probabilities.Keys.Where(value => value >= minimumNumber)
                .Where(value =>
                    distribution.ProbabilityAtLeast(value) + 0.000000000001d >= threshold
                )
                .DefaultIfEmpty(int.MinValue)
                .Max();
            if (candidate != int.MinValue)
            {
                var maximumRaise = GetMaximumRaise(observation, threshold);
                if (maximumRaise < 1)
                {
                    throw new InvalidOperationException(
                        "宣言の最大上げ幅は1以上である必要があります。"
                    );
                }

                var currentValue = observation.CurrentDeclaration?.Value ?? 0;
                var maximumAllowed =
                    currentValue > int.MaxValue - maximumRaise
                        ? int.MaxValue
                        : currentValue + maximumRaise;
                return NpcDecision.DeclareNumber(Math.Min(candidate, maximumAllowed));
            }

            return observation.CanDeclareCoyote
                ? NpcDecision.DeclareCoyote()
                : NpcDecision.DeclareNumber(1);
        }

        /// <summary>
        /// 現在観測で数字宣言に要求する成立確率を返します。
        /// </summary>
        protected abstract double GetThreshold(NpcObservation observation);

        /// <summary>
        /// 現在観測と成立確率から、直前宣言値に対する最大上げ幅を返します。
        /// </summary>
        protected abstract int GetMaximumRaise(NpcObservation observation, double threshold);
    }
}
