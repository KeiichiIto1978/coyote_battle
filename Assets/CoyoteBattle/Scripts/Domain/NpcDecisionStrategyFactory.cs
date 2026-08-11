using System;

namespace CoyoteBattle.Domain
{
    /// <summary>
    /// 固定NPC割当から対応する共通インターフェースの判断戦略を生成します。
    /// </summary>
    public static class NpcDecisionStrategyFactory
    {
        /// <summary>
        /// 既定NPCの思考タイプに対応する判断戦略を生成します。
        /// </summary>
        /// <param name="participantId">戦略を生成する既定NPC識別子です。</param>
        /// <param name="estimator">全タイプで共有できる実合計推定器です。</param>
        /// <param name="npcRandomSource">ギャンブル型専用の乱数源です。</param>
        /// <returns>固定割当に対応する判断戦略です。</returns>
        public static INpcDecisionStrategy Create(
            string participantId,
            NpcFieldTotalEstimator estimator,
            IRandomSource npcRandomSource
        )
        {
            if (estimator == null)
            {
                throw new ArgumentNullException(nameof(estimator));
            }

            if (npcRandomSource == null)
            {
                throw new ArgumentNullException(nameof(npcRandomSource));
            }

            if (!NpcPersonalityAssignment.TryGet(participantId, out var personality))
            {
                throw new ArgumentException(
                    "既定NPCの識別子を指定してください。",
                    nameof(participantId)
                );
            }

            switch (personality)
            {
                case NpcPersonality.Aggressive:
                    return new AggressiveNpcDecisionStrategy(estimator);
                case NpcPersonality.Cautious:
                    return new CautiousNpcDecisionStrategy(estimator);
                case NpcPersonality.Gambling:
                    return new GamblingNpcDecisionStrategy(estimator, npcRandomSource);
                case NpcPersonality.Analytical:
                    return new AnalyticalNpcDecisionStrategy(estimator);
                default:
                    throw new InvalidOperationException("未定義のNPC思考タイプです。");
            }
        }
    }
}
