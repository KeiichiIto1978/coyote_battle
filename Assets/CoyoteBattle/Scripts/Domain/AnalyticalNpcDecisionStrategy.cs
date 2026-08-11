using System.Linq;

namespace CoyoteBattle.Domain
{
    /// <summary>
    /// ライフ、直前の宣言上昇幅、残存人数から閾値を補正する分析型NPC判断です。
    /// </summary>
    public sealed class AnalyticalNpcDecisionStrategy : ThresholdNpcDecisionStrategy
    {
        /// <summary>
        /// 指定推定器を使う分析型判断を生成します。
        /// </summary>
        /// <param name="estimator">公開札から実合計分布を作る推定器です。</param>
        public AnalyticalNpcDecisionStrategy(NpcFieldTotalEstimator estimator)
            : base(estimator) { }

        protected override double GetThreshold(NpcObservation observation)
        {
            return CalculateRequiredProbability(observation);
        }

        /// <summary>
        /// ライフ、履歴、残存人数の補正後に要求する成立確率を返します。
        /// </summary>
        /// <param name="observation">分析対象となるNPC専用観測です。</param>
        /// <returns>35%以上85%以下に収めた成立確率です。</returns>
        public double CalculateRequiredProbability(NpcObservation observation)
        {
            if (observation == null)
            {
                throw new System.ArgumentNullException(nameof(observation));
            }

            var threshold = 0.60d + GetActorLifeAdjustment(observation.ActorLife);
            if (observation.CurrentDeclaration != null)
            {
                var declarerLife = observation
                    .Participants.Single(item =>
                        item.Id == observation.CurrentDeclaration.ParticipantId
                    )
                    .Life;
                threshold += GetDeclarerLifeAdjustment(declarerLife);
            }

            if (
                observation.DeclarationHistory.Count >= 2
                && observation.DeclarationHistory[^1].Value
                    - observation.DeclarationHistory[^2].Value
                    >= 5
            )
            {
                threshold += 0.10d;
            }

            threshold += GetParticipantCountAdjustment(observation.RemainingParticipantIds.Count);
            return threshold < 0.35d ? 0.35d
                : threshold > 0.85d ? 0.85d
                : threshold;
        }

        private static double GetActorLifeAdjustment(int life)
        {
            return life == 1 ? 0.15d
                : life == 2 ? 0.05d
                : 0d;
        }

        private static double GetDeclarerLifeAdjustment(int life)
        {
            return life == 1 ? -0.10d
                : life == 2 ? -0.05d
                : 0d;
        }

        private static double GetParticipantCountAdjustment(int count)
        {
            return count == 2 ? 0.10d
                : count == 3 ? 0.05d
                : count == 5 ? -0.05d
                : 0d;
        }
    }
}
