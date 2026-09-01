using System;
using System.Collections.Generic;
using System.Linq;

namespace CoyoteBattle.Domain
{
    /// <summary>
    /// 「？」、MAX→0、数字合計、×2の順に場の合計値を計算します。
    /// </summary>
    public sealed class FieldTotalCalculator
    {
        /// <summary>
        /// 場のカードと追加カード源から、特殊効果適用後の合計値を計算します。
        /// </summary>
        /// <param name="fieldCards">公開された場のカードです。</param>
        /// <param name="additionalCardSource">「？」がある場合に使う追加カード源です。</param>
        /// <param name="result">成功時の合計値と追加札、再構築要求です。</param>
        /// <returns>合計値を確定できた場合はtrue、追加札を取得できない場合はfalseです。</returns>
        public bool TryCalculate(
            IReadOnlyList<Card> fieldCards,
            IAdditionalCardSource additionalCardSource,
            out FieldTotalResult result
        )
        {
            var validatedCards = ValidateFieldCards(fieldCards);
            result = null;

            var mysteryCard = validatedCards.SingleOrDefault(card => card.Kind == CardKind.Mystery);
            Card additionalCard = null;
            if (mysteryCard != null)
            {
                if (
                    additionalCardSource == null
                    || !additionalCardSource.TryDrawAdditional(out additionalCard)
                )
                {
                    return false;
                }

                if (additionalCard == null)
                {
                    throw new InvalidOperationException("追加カード源が成功時にnullを返しました。");
                }

                if (additionalCard.Kind == CardKind.Mystery)
                {
                    throw new InvalidOperationException(
                        "有効な山札から「？」を連鎖して引くことはできません。"
                    );
                }
            }

            var effectiveCards = validatedCards
                .Where(card => card.Kind != CardKind.Mystery)
                .ToList();
            if (additionalCard != null)
            {
                effectiveCards.Add(additionalCard);
            }

            ValidateResolvedCards(effectiveCards);
            var total = CalculateTotal(effectiveCards);
            var shouldRebuildDeck = effectiveCards.Any(card => card.Kind == CardKind.Night);
            result = new FieldTotalResult(total, additionalCard, shouldRebuildDeck);
            return true;
        }

        /// <summary>
        /// 場の入力がnull、重複個体、同種特殊カードを含まないことを検証します。
        /// </summary>
        /// <param name="fieldCards">検証対象の場札です。</param>
        /// <returns>検証済みの新しい一覧です。</returns>
        private static List<Card> ValidateFieldCards(IReadOnlyList<Card> fieldCards)
        {
            if (fieldCards == null)
            {
                throw new ArgumentNullException(nameof(fieldCards));
            }

            if (fieldCards.Any(card => card == null))
            {
                throw new ArgumentException(
                    "場にnullのカードを含めることはできません。",
                    nameof(fieldCards)
                );
            }

            if (fieldCards.Select(card => card.Id).Distinct().Count() != fieldCards.Count)
            {
                throw new ArgumentException(
                    "同じカード個体が場で重複しています。",
                    nameof(fieldCards)
                );
            }

            ValidateSpecialCardCounts(fieldCards, exceptionForResolvedCards: false);
            return fieldCards.ToList();
        }

        /// <summary>
        /// 「？」解決後もカード個体と一枚物の特殊種別が重複しないことを検証します。
        /// </summary>
        /// <param name="cards">解決後のカードです。</param>
        private static void ValidateResolvedCards(IReadOnlyList<Card> cards)
        {
            if (cards.Select(card => card.Id).Distinct().Count() != cards.Count)
            {
                throw new InvalidOperationException("追加札が場札と重複しています。");
            }

            ValidateSpecialCardCounts(cards, exceptionForResolvedCards: true);
        }

        /// <summary>
        /// 各特殊カードが高々1枚であることを検証します。
        /// </summary>
        /// <param name="cards">検証対象のカードです。</param>
        /// <param name="exceptionForResolvedCards">解決後の山札矛盾として報告するかを示します。</param>
        private static void ValidateSpecialCardCounts(
            IEnumerable<Card> cards,
            bool exceptionForResolvedCards
        )
        {
            var hasDuplicate = cards
                .Where(card => card.Kind != CardKind.Number)
                .GroupBy(card => card.Kind)
                .Any(group => group.Count() > 1);
            if (!hasDuplicate)
            {
                return;
            }

            if (exceptionForResolvedCards)
            {
                throw new InvalidOperationException(
                    "追加札の解決後に同種特殊カードが重複しました。"
                );
            }

            throw new ArgumentException("同種特殊カードを場に複数指定できません。", nameof(cards));
        }

        /// <summary>
        /// MAX→0、数字合計、×2の順に最終値を求めます。
        /// </summary>
        /// <param name="cards">「？」解決後のカードです。</param>
        /// <returns>特殊効果適用後の合計値です。</returns>
        private static int CalculateTotal(IReadOnlyCollection<Card> cards)
        {
            var numberValues = cards
                .Where(card => card.Kind == CardKind.Number)
                .Select(card => card.Value.Value)
                .ToList();

            var total = numberValues.Sum();
            var hasMaxToZero = cards.Any(card => card.Kind == CardKind.MaxToZero);
            if (hasMaxToZero && numberValues.Count > 0)
            {
                total = checked(total - numberValues.Max());
            }

            if (cards.Any(card => card.Kind == CardKind.Double))
            {
                total = checked(total * 2);
            }

            return total;
        }
    }
}
