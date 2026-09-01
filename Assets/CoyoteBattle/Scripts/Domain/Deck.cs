using System;
using System.Collections.Generic;
using System.Linq;

namespace CoyoteBattle.Domain
{
    /// <summary>
    /// ドロー山札、使用済み札、場札、追加札を一貫して管理します。
    /// </summary>
    public sealed class Deck : IAdditionalCardSource
    {
        private readonly List<Card> _additionalCards = new List<Card>();
        private readonly List<Card> _discardPile = new List<Card>();
        private readonly List<Card> _drawPile;
        private readonly List<Card> _inPlay = new List<Card>();
        private readonly IRandomSource _randomSource;
        private readonly int _totalCardCount;

        private Deck(IEnumerable<Card> cards, IRandomSource randomSource)
        {
            _randomSource = randomSource ?? throw new ArgumentNullException(nameof(randomSource));
            var validatedCards = ValidateCards(cards);
            _totalCardCount = validatedCards.Count;
            _drawPile = ShuffleCopy(validatedCards);
        }

        /// <summary>
        /// 現在のドロー山札をドロー順で取得します。
        /// </summary>
        public IReadOnlyList<Card> DrawPile => _drawPile.AsReadOnly();

        /// <summary>
        /// 現在の使用済み札を取得します。
        /// </summary>
        public IReadOnlyList<Card> DiscardPile => _discardPile.AsReadOnly();

        /// <summary>
        /// 現在の場札を取得します。
        /// </summary>
        public IReadOnlyList<Card> InPlayCards => _inPlay.AsReadOnly();

        /// <summary>
        /// 「？」によって追加されたカードを取得します。
        /// </summary>
        public IReadOnlyList<Card> AdditionalCards => _additionalCards.AsReadOnly();

        /// <summary>
        /// ドロー山札の枚数を取得します。
        /// </summary>
        public int DrawPileCount => _drawPile.Count;

        /// <summary>
        /// 使用済み札の枚数を取得します。
        /// </summary>
        public int DiscardPileCount => _discardPile.Count;

        /// <summary>
        /// 場札の枚数を取得します。
        /// </summary>
        public int InPlayCount => _inPlay.Count;

        /// <summary>
        /// 追加札の枚数を取得します。
        /// </summary>
        public int AdditionalCardCount => _additionalCards.Count;

        /// <summary>
        /// すべての領域に存在するカードの合計枚数を取得します。
        /// </summary>
        public int TotalCardCount =>
            _drawPile.Count + _discardPile.Count + _inPlay.Count + _additionalCards.Count;

        /// <summary>
        /// Version 1の36枚を構築してシャッフルします。
        /// </summary>
        /// <param name="randomSource">シャッフルに使う乱数源です。</param>
        /// <returns>シャッフル済みの既定山札です。</returns>
        public static Deck CreateDefault(IRandomSource randomSource)
        {
            return new Deck(DefaultDeckFactory.Create(), randomSource);
        }

        /// <summary>
        /// 検証可能な任意のカード集合から山札を構築してシャッフルします。
        /// </summary>
        /// <param name="cards">個体識別子と一枚物の特殊種別が重複しないカードです。</param>
        /// <param name="randomSource">シャッフルに使う乱数源です。</param>
        /// <returns>シャッフル済みの山札です。</returns>
        public static Deck Create(IEnumerable<Card> cards, IRandomSource randomSource)
        {
            return new Deck(cards, randomSource);
        }

        /// <summary>
        /// 順序付き配布対象へ各1枚を原子的に配布します。
        /// </summary>
        /// <param name="participantIds">重複しない配布対象識別子の一覧です。</param>
        /// <param name="deals">成功時の配布結果です。</param>
        /// <returns>全対象へ配布できた場合はtrue、不足した場合はfalseです。</returns>
        public bool TryDeal(IReadOnlyList<string> participantIds, out IReadOnlyList<CardDeal> deals)
        {
            ValidateParticipantIds(participantIds);
            deals = Array.Empty<CardDeal>();
            if (participantIds.Count == 0)
            {
                return true;
            }

            if (!TryPrepareDrawPile(participantIds.Count))
            {
                return false;
            }

            var createdDeals = new List<CardDeal>(participantIds.Count);
            for (var index = 0; index < participantIds.Count; index++)
            {
                var card = _drawPile[0];
                _drawPile.RemoveAt(0);
                _inPlay.Add(card);
                createdDeals.Add(new CardDeal(participantIds[index], card));
            }

            deals = createdDeals.AsReadOnly();
            EnsureCardCountInvariant();
            return true;
        }

        /// <summary>
        /// 「？」を解決する追加カードを取得し、追加札として追跡します。
        /// </summary>
        /// <param name="card">成功時に取得した追加カードです。</param>
        /// <returns>取得できた場合はtrue、それ以外はfalseです。</returns>
        public bool TryDrawAdditional(out Card card)
        {
            card = null;
            if (
                _additionalCards.Count > 0
                || _inPlay.All(inPlayCard => inPlayCard.Kind != CardKind.Mystery)
            )
            {
                return false;
            }

            if (!TryPrepareDrawPile(1))
            {
                return false;
            }

            card = _drawPile[0];
            _drawPile.RemoveAt(0);
            _additionalCards.Add(card);
            EnsureCardCountInvariant();
            return true;
        }

        /// <summary>
        /// 現在のラウンドを回収し、夜カードがあれば全カードを再構築します。
        /// </summary>
        /// <returns>回収対象があり処理できた場合はtrue、それ以外はfalseです。</returns>
        public bool TryCompleteRound()
        {
            if (_inPlay.Count == 0 && _additionalCards.Count == 0)
            {
                return false;
            }

            var shouldRebuildAll = _inPlay
                .Concat(_additionalCards)
                .Any(card => card.Kind == CardKind.Night);
            if (shouldRebuildAll)
            {
                var allCards = _drawPile
                    .Concat(_discardPile)
                    .Concat(_inPlay)
                    .Concat(_additionalCards)
                    .ToList();
                var shuffledCards = ShuffleCopy(allCards);
                _drawPile.Clear();
                _drawPile.AddRange(shuffledCards);
                _discardPile.Clear();
            }
            else
            {
                _discardPile.AddRange(_inPlay);
                _discardPile.AddRange(_additionalCards);
            }

            _inPlay.Clear();
            _additionalCards.Clear();
            EnsureCardCountInvariant();
            return true;
        }

        /// <summary>
        /// 必要枚数を取得できるよう、必要な場合だけ残り山札と使用済み札を再構築します。
        /// </summary>
        /// <param name="requiredCount">今回必要なカード枚数です。</param>
        /// <returns>必要枚数を用意できた場合はtrue、それ以外はfalseです。</returns>
        private bool TryPrepareDrawPile(int requiredCount)
        {
            if (_drawPile.Count >= requiredCount)
            {
                return true;
            }

            if (_drawPile.Count + _discardPile.Count < requiredCount)
            {
                return false;
            }

            var shuffledCards = ShuffleCopy(_drawPile.Concat(_discardPile).ToList());
            _drawPile.Clear();
            _drawPile.AddRange(shuffledCards);
            _discardPile.Clear();
            return true;
        }

        /// <summary>
        /// コピー上でFisher-Yatesシャッフルを完了し、元の状態を保護します。
        /// </summary>
        /// <param name="cards">シャッフル対象です。</param>
        /// <returns>シャッフル済みの新しい一覧です。</returns>
        private List<Card> ShuffleCopy(IReadOnlyList<Card> cards)
        {
            var shuffledCards = cards.ToList();
            for (var currentIndex = shuffledCards.Count - 1; currentIndex > 0; currentIndex--)
            {
                var selectedIndex = _randomSource.Next(currentIndex + 1);
                if (selectedIndex < 0 || selectedIndex > currentIndex)
                {
                    throw new InvalidOperationException("乱数源が指定範囲外の値を返しました。");
                }

                var selectedCard = shuffledCards[selectedIndex];
                shuffledCards[selectedIndex] = shuffledCards[currentIndex];
                shuffledCards[currentIndex] = selectedCard;
            }

            return shuffledCards;
        }

        /// <summary>
        /// 山札へ渡されたカード集合の個体と特殊種別が重複していないことを検証します。
        /// </summary>
        /// <param name="cards">検証対象のカードです。</param>
        /// <returns>検証済みのカード一覧です。</returns>
        private static List<Card> ValidateCards(IEnumerable<Card> cards)
        {
            if (cards == null)
            {
                throw new ArgumentNullException(nameof(cards));
            }

            var validatedCards = cards.ToList();
            if (validatedCards.Count == 0)
            {
                throw new ArgumentException("山札には1枚以上のカードが必要です。", nameof(cards));
            }

            if (validatedCards.Any(card => card == null))
            {
                throw new ArgumentException(
                    "山札にnullのカードを含めることはできません。",
                    nameof(cards)
                );
            }

            if (validatedCards.Select(card => card.Id).Distinct().Count() != validatedCards.Count)
            {
                throw new ArgumentException("カード個体識別子が重複しています。", nameof(cards));
            }

            var hasDuplicatedSpecial = validatedCards
                .Where(card => card.Kind != CardKind.Number)
                .GroupBy(card => card.Kind)
                .Any(group => group.Count() > 1);
            if (hasDuplicatedSpecial)
            {
                throw new ArgumentException(
                    "同種の特殊カードは山札に1枚だけ指定できます。",
                    nameof(cards)
                );
            }

            var hasTooManyNumbers = validatedCards
                .Where(card => card.Kind == CardKind.Number)
                .GroupBy(card => card.Value.Value)
                .Any(group => group.Count() > GetMaximumNumberCount(group.Key));
            if (hasTooManyNumbers)
            {
                throw new ArgumentException("既定山札の種類別枚数を超えています。", nameof(cards));
            }

            return validatedCards;
        }

        /// <summary>
        /// Version 1の既定山札に存在する数字カードの最大枚数を返します。
        /// </summary>
        /// <param name="value">カードの数値です。</param>
        /// <returns>既定山札に含められる最大枚数です。</returns>
        private static int GetMaximumNumberCount(int value)
        {
            switch (value)
            {
                case 20:
                case -10:
                    return 1;
                case 15:
                case -5:
                    return 2;
                case 10:
                case 0:
                    return 3;
                case 5:
                case 4:
                case 3:
                case 2:
                case 1:
                    return 4;
                default:
                    throw new InvalidOperationException("未定義の数字カードです。");
            }
        }

        /// <summary>
        /// 配布対象一覧がnull、空文字、重複を含まないことを検証します。
        /// </summary>
        /// <param name="participantIds">検証対象の一覧です。</param>
        private static void ValidateParticipantIds(IReadOnlyList<string> participantIds)
        {
            if (participantIds == null)
            {
                throw new ArgumentNullException(nameof(participantIds));
            }

            if (participantIds.Any(string.IsNullOrWhiteSpace))
            {
                throw new ArgumentException(
                    "配布対象の識別子を指定してください。",
                    nameof(participantIds)
                );
            }

            if (participantIds.Distinct(StringComparer.Ordinal).Count() != participantIds.Count)
            {
                throw new ArgumentException("配布対象が重複しています。", nameof(participantIds));
            }
        }

        /// <summary>
        /// すべての領域を通じてカード枚数が変化していないことを検証します。
        /// </summary>
        private void EnsureCardCountInvariant()
        {
            if (TotalCardCount != _totalCardCount)
            {
                throw new InvalidOperationException("山札内のカードが重複または消失しました。");
            }
        }
    }
}
