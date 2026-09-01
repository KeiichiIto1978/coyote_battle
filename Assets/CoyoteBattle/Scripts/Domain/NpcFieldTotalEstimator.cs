using System;
using System.Collections.Generic;
using System.Linq;

namespace CoyoteBattle.Domain
{
    /// <summary>
    /// 既定山札から現在の公開札と使用済み札を除き、NPCの伏せ札を含む実合計分布を推定します。
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
                RemoveCard(
                    remainingCards,
                    deal.Card.Kind,
                    deal.Card.Value,
                    nameof(observation),
                    "公開カードが既定山札の種類別枚数を超えています。"
                );
            }

            foreach (var discardedCard in observation.DiscardedCards)
            {
                for (var count = 0; count < discardedCard.Count; count++)
                {
                    RemoveCard(
                        remainingCards,
                        discardedCard.Kind,
                        discardedCard.Value,
                        nameof(observation),
                        "公開カードと使用済み札が既定山札の種類別枚数を超えています。"
                    );
                }
            }

            if (remainingCards.Count < 1)
            {
                throw new ArgumentException("伏せ札候補が不足しています。", nameof(observation));
            }

            var visibleField = observation.VisibleCards.Select(item => item.Card).ToList();
            var discardedCards = CreateDiscardedCards(observation.DiscardedCards);
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
                    var additionalCandidates =
                        remainingAfterHidden.Count > 0 ? remainingAfterHidden : discardedCards;
                    if (additionalCandidates.Count == 0)
                    {
                        throw new ArgumentException(
                            "疑問カードの追加札候補が不足しています。",
                            nameof(observation)
                        );
                    }

                    foreach (var additionalCard in additionalCandidates)
                    {
                        AddWeight(weights, CalculateTotal(fieldCards, additionalCard), 1);
                    }
                }
                else
                {
                    AddWeight(
                        weights,
                        CalculateTotal(fieldCards, additionalCard: null),
                        Math.Max(remainingAfterHidden.Count, 1)
                    );
                }
            }

            return new FieldTotalProbabilityDistribution(weights);
        }

        private static IReadOnlyList<Card> CreateDiscardedCards(
            IReadOnlyList<NpcDiscardedCardCount> discardedCardCounts
        )
        {
            var defaultCards = DefaultDeckFactory.Create();
            var cards = new List<Card>();
            foreach (var discardedCardCount in discardedCardCounts)
            {
                cards.AddRange(
                    defaultCards
                        .Where(card =>
                            card.Kind == discardedCardCount.Kind
                            && card.Value == discardedCardCount.Value
                        )
                        .Take(discardedCardCount.Count)
                );
            }

            return cards.AsReadOnly();
        }

        private static void RemoveCard(
            IList<Card> cards,
            CardKind kind,
            int? value,
            string parameterName,
            string message
        )
        {
            var index = -1;
            for (var cardIndex = 0; cardIndex < cards.Count; cardIndex++)
            {
                if (cards[cardIndex].Kind == kind && cards[cardIndex].Value == value)
                {
                    index = cardIndex;
                    break;
                }
            }

            if (index < 0)
            {
                throw new ArgumentException(message, parameterName);
            }

            cards.RemoveAt(index);
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
