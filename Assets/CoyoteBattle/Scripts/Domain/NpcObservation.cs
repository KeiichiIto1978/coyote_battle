using System;
using System.Collections.Generic;
using System.Linq;

namespace CoyoteBattle.Domain
{
    /// <summary>
    /// 行動NPCが現在ラウンドで利用できる情報だけを保持する変更不能な観測です。
    /// </summary>
    public sealed class NpcObservation
    {
        private readonly IReadOnlyList<NpcParticipantObservation> _participants;
        private readonly IReadOnlyList<string> _remainingParticipantIds;
        private readonly IReadOnlyList<CardDeal> _visibleCards;
        private readonly IReadOnlyList<NpcDiscardedCardCount> _discardedCards;
        private readonly IReadOnlyList<NumberDeclaration> _declarationHistory;

        /// <summary>
        /// 行動NPCを基準に、参加者、公開カード、宣言履歴を検証してコピーします。
        /// </summary>
        /// <param name="actorId">現在手番の残存NPC識別子です。</param>
        /// <param name="participants">脱落者を含む固定5名の状態です。</param>
        /// <param name="remainingParticipantIds">現ラウンドの残存参加者です。</param>
        /// <param name="visibleCards">行動NPC自身を除く残存参加者のカードです。</param>
        /// <param name="declarationHistory">現ラウンドで成功した数字宣言履歴です。</param>
        /// <param name="discardedCards">現在の使用済み札を種類と値で集約した一覧です。</param>
        public NpcObservation(
            string actorId,
            IReadOnlyList<NpcParticipantObservation> participants,
            IReadOnlyList<string> remainingParticipantIds,
            IReadOnlyList<CardDeal> visibleCards,
            IReadOnlyList<NumberDeclaration> declarationHistory,
            IReadOnlyList<NpcDiscardedCardCount> discardedCards
        )
        {
            ActorId = ValidateActor(actorId, participants);
            _participants = ValidateParticipants(participants);
            _remainingParticipantIds = ValidateRemainingParticipants(
                ActorId,
                _participants,
                remainingParticipantIds
            );
            _visibleCards = ValidateVisibleCards(ActorId, _remainingParticipantIds, visibleCards);
            _discardedCards = ValidateDiscardedCards(discardedCards);
            ValidateCardAvailability(_visibleCards, _discardedCards);
            _declarationHistory = ValidateHistory(
                ActorId,
                _remainingParticipantIds,
                declarationHistory
            );

            ActorLife = _participants.Single(item => item.Id == ActorId).Life;
            CurrentDeclaration = _declarationHistory.LastOrDefault();
            IsFirstDeclaration = CurrentDeclaration == null;
            CanDeclareCoyote = !IsFirstDeclaration;
            CanDeclareNumber =
                CurrentDeclaration == null || CurrentDeclaration.Value < int.MaxValue;
            MinimumNumber =
                !CanDeclareNumber ? (int?)null
                : CurrentDeclaration == null ? 1
                : CurrentDeclaration.Value + 1;
        }

        /// <summary>
        /// 行動するNPCの識別子を取得します。
        /// </summary>
        public string ActorId { get; }

        /// <summary>
        /// 行動NPCの現在ライフを取得します。
        /// </summary>
        public int ActorLife { get; }

        /// <summary>
        /// 脱落者を含む固定5名の参加者状態を取得します。
        /// </summary>
        public IReadOnlyList<NpcParticipantObservation> Participants => _participants;

        /// <summary>
        /// 現ラウンドの残存参加者識別子を取得します。
        /// </summary>
        public IReadOnlyList<string> RemainingParticipantIds => _remainingParticipantIds;

        /// <summary>
        /// 行動NPC自身を除く残存参加者のカードを取得します。
        /// </summary>
        public IReadOnlyList<CardDeal> VisibleCards => _visibleCards;

        /// <summary>
        /// 現在の使用済み札を種類と値ごとに集約した一覧を取得します。
        /// </summary>
        public IReadOnlyList<NpcDiscardedCardCount> DiscardedCards => _discardedCards;

        /// <summary>
        /// 現ラウンドで成功した数字宣言履歴を取得します。
        /// </summary>
        public IReadOnlyList<NumberDeclaration> DeclarationHistory => _declarationHistory;

        /// <summary>
        /// 現在の数字宣言を取得します。初手ではnullです。
        /// </summary>
        public NumberDeclaration CurrentDeclaration { get; }

        /// <summary>
        /// 成功した数字宣言がまだないかを取得します。
        /// </summary>
        public bool IsFirstDeclaration { get; }

        /// <summary>
        /// 数字宣言を選択できるかを取得します。
        /// </summary>
        public bool CanDeclareNumber { get; }

        /// <summary>
        /// コヨーテ宣言を選択できるかを取得します。
        /// </summary>
        public bool CanDeclareCoyote { get; }

        /// <summary>
        /// 次に宣言できる最小値を取得します。数字を選べない場合はnullです。
        /// </summary>
        public int? MinimumNumber { get; }

        private static string ValidateActor(
            string actorId,
            IReadOnlyList<NpcParticipantObservation> participants
        )
        {
            if (string.IsNullOrWhiteSpace(actorId))
            {
                throw new ArgumentException("行動NPCの識別子を指定してください。", nameof(actorId));
            }

            if (participants == null)
            {
                throw new ArgumentNullException(nameof(participants));
            }

            return actorId;
        }

        private static IReadOnlyList<NpcParticipantObservation> ValidateParticipants(
            IReadOnlyList<NpcParticipantObservation> participants
        )
        {
            if (participants.Count != 5 || participants.Any(item => item == null))
            {
                throw new ArgumentException(
                    "参加者状態はユーザー1名とNPC4名で指定してください。",
                    nameof(participants)
                );
            }

            if (
                participants.Select(item => item.Id).Distinct().Count() != participants.Count
                || participants.Count(item => item.Kind == ParticipantKind.User) != 1
                || participants.Count(item => item.Kind == ParticipantKind.Npc) != 4
            )
            {
                throw new ArgumentException(
                    "参加者識別子と種別の構成が不正です。",
                    nameof(participants)
                );
            }

            return participants.ToList().AsReadOnly();
        }

        private static IReadOnlyList<string> ValidateRemainingParticipants(
            string actorId,
            IReadOnlyList<NpcParticipantObservation> participants,
            IReadOnlyList<string> remainingParticipantIds
        )
        {
            if (remainingParticipantIds == null)
            {
                throw new ArgumentNullException(nameof(remainingParticipantIds));
            }

            if (
                remainingParticipantIds.Count < 2
                || remainingParticipantIds.Count > 5
                || remainingParticipantIds.Any(string.IsNullOrWhiteSpace)
                || remainingParticipantIds.Distinct().Count() != remainingParticipantIds.Count
            )
            {
                throw new ArgumentException(
                    "残存参加者は重複しない2名以上5名以下で指定してください。",
                    nameof(remainingParticipantIds)
                );
            }

            var participantById = participants.ToDictionary(item => item.Id);
            if (
                !participantById.TryGetValue(actorId, out var actor)
                || actor.Kind != ParticipantKind.Npc
                || actor.IsEliminated
                || !remainingParticipantIds.Contains(actorId)
                || remainingParticipantIds.Any(id =>
                    !participantById.TryGetValue(id, out var item) || item.IsEliminated
                )
                || !participants
                    .Where(item => !item.IsEliminated)
                    .Select(item => item.Id)
                    .OrderBy(id => id)
                    .SequenceEqual(remainingParticipantIds.OrderBy(id => id))
            )
            {
                throw new ArgumentException(
                    "行動者と残存参加者の状態が一致しません。",
                    nameof(remainingParticipantIds)
                );
            }

            return remainingParticipantIds.ToList().AsReadOnly();
        }

        private static IReadOnlyList<CardDeal> ValidateVisibleCards(
            string actorId,
            IReadOnlyList<string> remainingParticipantIds,
            IReadOnlyList<CardDeal> visibleCards
        )
        {
            if (visibleCards == null)
            {
                throw new ArgumentNullException(nameof(visibleCards));
            }

            var expectedOwners = remainingParticipantIds.Where(id => id != actorId).ToList();
            if (
                visibleCards.Count != expectedOwners.Count
                || visibleCards.Any(item => item == null || item.ParticipantId == actorId)
                || visibleCards.Select(item => item.ParticipantId).Distinct().Count()
                    != visibleCards.Count
                || visibleCards.Select(item => item.Card.Id).Distinct().Count()
                    != visibleCards.Count
                || !visibleCards
                    .Select(item => item.ParticipantId)
                    .OrderBy(id => id)
                    .SequenceEqual(expectedOwners.OrderBy(id => id))
            )
            {
                throw new ArgumentException(
                    "公開カードは行動NPC自身を除く残存参加者へ1枚ずつ指定してください。",
                    nameof(visibleCards)
                );
            }

            return visibleCards.ToList().AsReadOnly();
        }

        private static IReadOnlyList<NumberDeclaration> ValidateHistory(
            string actorId,
            IReadOnlyList<string> remainingParticipantIds,
            IReadOnlyList<NumberDeclaration> declarationHistory
        )
        {
            if (declarationHistory == null)
            {
                throw new ArgumentNullException(nameof(declarationHistory));
            }

            var previousValue = 0;
            string previousParticipantId = null;
            foreach (var declaration in declarationHistory)
            {
                if (
                    declaration == null
                    || !remainingParticipantIds.Contains(declaration.ParticipantId)
                    || declaration.Value <= previousValue
                    || declaration.ParticipantId == previousParticipantId
                )
                {
                    throw new ArgumentException(
                        "宣言履歴が参加資格または単調増加規則と一致しません。",
                        nameof(declarationHistory)
                    );
                }

                previousValue = declaration.Value;
                previousParticipantId = declaration.ParticipantId;
            }

            if (
                declarationHistory.Count > 0
                && declarationHistory[declarationHistory.Count - 1].ParticipantId == actorId
            )
            {
                throw new ArgumentException(
                    "直前の数字宣言者を現在手番NPCに指定できません。",
                    nameof(declarationHistory)
                );
            }

            return declarationHistory.ToList().AsReadOnly();
        }

        private static IReadOnlyList<NpcDiscardedCardCount> ValidateDiscardedCards(
            IReadOnlyList<NpcDiscardedCardCount> discardedCards
        )
        {
            if (discardedCards == null)
            {
                throw new ArgumentNullException(nameof(discardedCards));
            }

            if (
                discardedCards.Any(item => item == null)
                || discardedCards
                    .GroupBy(item => new { item.Kind, item.Value })
                    .Any(group => group.Count() > 1)
            )
            {
                throw new ArgumentException(
                    "使用済み札はカード面ごとに重複なく指定してください。",
                    nameof(discardedCards)
                );
            }

            return discardedCards.ToList().AsReadOnly();
        }

        private static void ValidateCardAvailability(
            IReadOnlyList<CardDeal> visibleCards,
            IReadOnlyList<NpcDiscardedCardCount> discardedCards
        )
        {
            var exceedsInitialCount = DefaultDeckFactory
                .Create()
                .GroupBy(card => new { card.Kind, card.Value })
                .Any(group =>
                    visibleCards.Count(deal =>
                        deal.Card.Kind == group.Key.Kind && deal.Card.Value == group.Key.Value
                    )
                        + discardedCards
                            .Where(item =>
                                item.Kind == group.Key.Kind && item.Value == group.Key.Value
                            )
                            .Sum(item => item.Count)
                    > group.Count()
                );
            if (exceedsInitialCount)
            {
                throw new ArgumentException(
                    "公開カードと使用済み札が既定山札の種類別枚数を超えています。",
                    nameof(discardedCards)
                );
            }
        }
    }
}
