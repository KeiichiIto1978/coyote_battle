using System;
using System.Collections.Generic;
using System.Linq;

namespace CoyoteBattle.Domain
{
    /// <summary>
    /// 既定山札と現在の公開札から、NPCの伏せ札を含む実合計分布を推定します。
    /// </summary>
    public sealed class NpcFieldTotalEstimator
    {
        /// <summary>
        /// 行動NPCの観測だけを使い、特殊カードを含む実合計分布を返します。
        /// </summary>
        /// <param name="observation">現在ラウンドのNPC専用観測です。</param>
        /// <returns>候補枚数比で重み付けされた実合計分布です。</returns>
        public FieldTotalProbabilityDistribution Estimate(NpcObservation observation)
        {
            if (observation == null)
            {
                throw new ArgumentNullException(nameof(observation));
            }

            var remainingCards = DefaultDeckFactory.Create().ToList();
            foreach (var deal in observation.VisibleCards)
            {
                var index = remainingCards.FindIndex(card => HasSameFace(card, deal.Card));
                if (index < 0)
                {
                    throw new ArgumentException(
                        "公開カードが既定山札の種類別枚数を超えています。",
                        nameof(observation)
                    );
                }

                remainingCards.RemoveAt(index);
            }

            if (remainingCards.Count < 2)
            {
                throw new ArgumentException("伏せ札候補が不足しています。", nameof(observation));
            }

            var visibleField = observation.VisibleCards.Select(item => item.Card).ToList();
            var weights = new Dictionary<int, long>();
            for (var hiddenIndex = 0; hiddenIndex < remainingCards.Count; hiddenIndex++)
            {
                var hiddenCard = remainingCards[hiddenIndex];
                var fieldCards = new List<Card>(visibleField) { hiddenCard };
                var remainingAfterHidden = remainingCards
                    .Where((_, index) => index != hiddenIndex)
                    .ToList();

                if (fieldCards.Any(card => card.Kind == CardKind.Mystery))
                {
                    foreach (var additionalCard in remainingAfterHidden)
                    {
                        AddWeight(weights, CalculateTotal(fieldCards, additionalCard), 1);
                    }
                }
                else
                {
                    AddWeight(
                        weights,
                        CalculateTotal(fieldCards, additionalCard: null),
                        remainingAfterHidden.Count
                    );
                }
            }

            return new FieldTotalProbabilityDistribution(weights);
        }

        private static bool HasSameFace(Card left, Card right)
        {
            return left.Kind == right.Kind && left.Value == right.Value;
        }

        private static int CalculateTotal(IReadOnlyList<Card> fieldCards, Card additionalCard)
        {
            var effectiveCards = fieldCards.Where(card => card.Kind != CardKind.Mystery).ToList();
            if (additionalCard != null)
            {
                if (additionalCard.Kind == CardKind.Mystery)
                {
                    throw new InvalidOperationException("「？」を追加札として連鎖できません。");
                }

                effectiveCards.Add(additionalCard);
            }

            var numberValues = effectiveCards
                .Where(card => card.Kind == CardKind.Number)
                .Select(card => card.Value.Value)
                .ToList();
            var total = numberValues.Sum();
            if (
                numberValues.Count > 0
                && effectiveCards.Any(card => card.Kind == CardKind.MaxToZero)
            )
            {
                total = checked(total - numberValues.Max());
            }

            if (effectiveCards.Any(card => card.Kind == CardKind.Double))
            {
                total = checked(total * 2);
            }

            return total;
        }

        private static void AddWeight(IDictionary<int, long> weights, int total, long weight)
        {
            weights[total] = weights.TryGetValue(total, out var current)
                ? current + weight
                : weight;
        }
    }
}
