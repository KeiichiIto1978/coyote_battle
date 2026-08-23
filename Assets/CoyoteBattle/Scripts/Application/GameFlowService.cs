using System;
using System.Collections.Generic;
using System.Linq;
using CoyoteBattle.Domain;

namespace CoyoteBattle.Application
{
    /// <summary>
    /// Domainルールを統合し、ゲーム開始から終了までの進行を管理します。
    /// </summary>
    public sealed class GameFlowService
    {
        private const string UserParticipantId = "user";
        private static readonly IReadOnlyList<CardDeal> EmptyDeals = Array.Empty<CardDeal>();
        private readonly FieldTotalCalculator _fieldTotalCalculator = new FieldTotalCalculator();
        private readonly IReadOnlyDictionary<string, INpcDecisionStrategy> _npcStrategies;
        private readonly IRandomSource _randomSource;
        private Deck _deck;
        private DeclarationPhase _declarationPhase;
        private IReadOnlyList<CardDeal> _deals = EmptyDeals;
        private ParticipantRoster _roster;

        /// <summary>
        /// ゲーム進行用乱数とNPC判断用乱数を分離して使用します。
        /// </summary>
        /// <param name="randomSource">開始者とデッキへ使う乱数源です。</param>
        /// <param name="npcRandomSource">ギャンブル型の判断だけに使う乱数源です。</param>
        public GameFlowService(IRandomSource randomSource, IRandomSource npcRandomSource)
        {
            _randomSource = randomSource ?? throw new ArgumentNullException(nameof(randomSource));
            if (npcRandomSource == null)
            {
                throw new ArgumentNullException(nameof(npcRandomSource));
            }

            var estimator = new NpcFieldTotalEstimator();
            _npcStrategies = new Dictionary<string, INpcDecisionStrategy>
            {
                ["npc-1"] = NpcDecisionStrategyFactory.Create("npc-1", estimator, npcRandomSource),
                ["npc-2"] = NpcDecisionStrategyFactory.Create("npc-2", estimator, npcRandomSource),
                ["npc-3"] = NpcDecisionStrategyFactory.Create("npc-3", estimator, npcRandomSource),
                ["npc-4"] = NpcDecisionStrategyFactory.Create("npc-4", estimator, npcRandomSource),
            };
            State = GameFlowState.NoGame;
            Outcome = GameOutcome.None;
        }

        /// <summary>
        /// 現在の安定したゲーム進行状態を取得します。
        /// </summary>
        public GameFlowState State { get; private set; }

        /// <summary>
        /// 未確定またはユーザー基準の最終勝敗を取得します。
        /// </summary>
        public GameOutcome Outcome { get; private set; }

        /// <summary>
        /// 進行中または直前に完了したラウンド番号を取得します。
        /// </summary>
        public int RoundNumber { get; private set; }

        /// <summary>
        /// 現在または直前ラウンドの開始参加者識別子を取得します。
        /// </summary>
        public string StartingParticipantId { get; private set; }

        /// <summary>
        /// 宣言中の現在手番を取得します。それ以外の状態ではnullです。
        /// </summary>
        public string CurrentParticipantId { get; private set; }

        /// <summary>
        /// 判定済みラウンドの結果を取得します。宣言中とNoGameではnullです。
        /// </summary>
        public RoundResultSnapshot LastRoundResult { get; private set; }

        /// <summary>
        /// 初期デッキと現在の捨て札を表示単位で集約した、変更できない一覧を取得します。
        /// </summary>
        public IReadOnlyList<CardCountSnapshot> CardInformation =>
            CardInformationAggregator.Create(_deck);

        /// <summary>
        /// 脱落者を含む参加者状態の変更不能なスナップショットを取得します。
        /// </summary>
        public IReadOnlyList<ParticipantState> Participants =>
            _roster == null
                ? Array.Empty<ParticipantState>()
                : _roster
                    .Participants.Select(item => new ParticipantState(item))
                    .ToList()
                    .AsReadOnly();

        /// <summary>
        /// 宣言中の配布カードを取得します。ユーザー自身のカード内容は伏せられます。
        /// </summary>
        public IReadOnlyList<DealtCardState> CurrentCards =>
            State != GameFlowState.Declaring
                ? Array.Empty<DealtCardState>()
                : _deals
                    .Select(deal => new DealtCardState(
                        deal,
                        deal.ParticipantId == UserParticipantId
                    ))
                    .ToList()
                    .AsReadOnly();

        /// <summary>
        /// 宣言中の成功した数字宣言履歴を読み取り専用で取得します。
        /// </summary>
        public IReadOnlyList<NumberDeclaration> DeclarationHistory =>
            _declarationPhase == null
                ? Array.Empty<NumberDeclaration>()
                : _declarationPhase.History;

        /// <summary>
        /// NoGameまたはGameOverから、初期化された固定5名のゲームを開始します。
        /// </summary>
        /// <returns>新しいゲームを開始した場合はtrue、それ以外はfalseです。</returns>
        public bool TryStartNewGame()
        {
            if (State != GameFlowState.NoGame && State != GameFlowState.GameOver)
            {
                return false;
            }

            var createdRoster = DefaultParticipantFactory.Create();
            var starterIndex = _randomSource.Next(createdRoster.Participants.Count);
            if (starterIndex < 0 || starterIndex >= createdRoster.Participants.Count)
            {
                throw new InvalidOperationException("乱数源が指定範囲外の開始位置を返しました。");
            }

            var startingParticipantId = createdRoster.Participants[starterIndex].Id;
            var createdDeck = Deck.CreateDefault(_randomSource);
            var orderedParticipantIds = CreateOrderedRemainingIds(
                createdRoster,
                startingParticipantId
            );
            if (!createdDeck.TryDeal(orderedParticipantIds, out var createdDeals))
            {
                throw new InvalidOperationException(
                    "既定山札から固定参加者へ配布できませんでした。"
                );
            }

            _roster = createdRoster;
            _deck = createdDeck;
            _deals = createdDeals;
            _declarationPhase = new DeclarationPhase(orderedParticipantIds);
            RoundNumber = 1;
            StartingParticipantId = startingParticipantId;
            CurrentParticipantId = startingParticipantId;
            LastRoundResult = null;
            Outcome = GameOutcome.None;
            State = GameFlowState.Declaring;
            return true;
        }

        /// <summary>
        /// 現在手番の数字宣言を受理し、成功時だけ次の残存参加者へ手番を進めます。
        /// </summary>
        /// <param name="participantId">操作する参加者識別子です。</param>
        /// <param name="value">宣言する1以上の整数です。</param>
        /// <returns>数字宣言と手番更新を完了した場合はtrue、それ以外はfalseです。</returns>
        public bool TryDeclareNumber(string participantId, int value)
        {
            ValidateParticipantId(participantId);
            if (
                State != GameFlowState.Declaring
                || !string.Equals(CurrentParticipantId, participantId, StringComparison.Ordinal)
                || !_declarationPhase.TryDeclareNumber(participantId, value)
            )
            {
                return false;
            }

            CurrentParticipantId = FindNextRemainingParticipantId(participantId);
            return true;
        }

        /// <summary>
        /// 現在手番のコヨーテ宣言を受理し、ラウンド判定を同期的に完了します。
        /// </summary>
        /// <param name="participantId">コヨーテを宣言する参加者識別子です。</param>
        /// <returns>宣言からラウンド判定まで完了した場合はtrue、それ以外はfalseです。</returns>
        public bool TryDeclareCoyote(string participantId)
        {
            ValidateParticipantId(participantId);
            if (
                State != GameFlowState.Declaring
                || !string.Equals(CurrentParticipantId, participantId, StringComparison.Ordinal)
                || !_declarationPhase.TryDeclareCoyote(participantId)
            )
            {
                return false;
            }

            CompleteJudgment();
            return true;
        }

        /// <summary>
        /// 現在手番が残存NPCなら、自分のカードだけを除外した変更不能な観測を生成します。
        /// </summary>
        /// <param name="observation">成功時のNPC専用観測です。</param>
        /// <returns>宣言中のNPC手番から観測を生成できた場合はtrueです。</returns>
        public bool TryCreateCurrentNpcObservation(out NpcObservation observation)
        {
            observation = null;
            if (
                State != GameFlowState.Declaring
                || string.IsNullOrEmpty(CurrentParticipantId)
                || !_roster.TryGetParticipant(CurrentParticipantId, out var actor)
                || actor.Kind != ParticipantKind.Npc
                || actor.IsEliminated
                || !_npcStrategies.ContainsKey(actor.Id)
            )
            {
                return false;
            }

            var participants = _roster
                .Participants.Select(item => new NpcParticipantObservation(
                    item.Id,
                    item.Kind,
                    item.Life
                ))
                .ToList()
                .AsReadOnly();
            var remainingIds = _roster
                .RemainingParticipants.Select(item => item.Id)
                .ToList()
                .AsReadOnly();
            var visibleCards = _deals
                .Where(deal => deal.ParticipantId != actor.Id)
                .ToList()
                .AsReadOnly();
            var discardedCards = _deck
                .DiscardPile.GroupBy(card => new { card.Kind, card.Value })
                .Select(group => new NpcDiscardedCardCount(
                    group.Key.Kind,
                    group.Key.Value,
                    group.Count()
                ))
                .ToList()
                .AsReadOnly();
            observation = new NpcObservation(
                actor.Id,
                participants,
                remainingIds,
                visibleCards,
                _declarationPhase.History,
                discardedCards
            );
            return true;
        }

        /// <summary>
        /// 現在手番NPCの観測、判断、既存宣言APIへの適用を1手だけ実行します。
        /// </summary>
        /// <returns>NPCの数字宣言またはコヨーテ宣言を受理した場合はtrueです。</returns>
        public bool TryExecuteCurrentNpcTurn()
        {
            if (!TryCreateCurrentNpcObservation(out var observation))
            {
                return false;
            }

            var decision = _npcStrategies[observation.ActorId].Decide(observation);
            if (decision == null)
            {
                return false;
            }

            switch (decision.Kind)
            {
                case NpcDecisionKind.Number:
                    return decision.Number.HasValue
                        && TryDeclareNumber(observation.ActorId, decision.Number.Value);
                case NpcDecisionKind.Coyote:
                    return TryDeclareCoyote(observation.ActorId);
                default:
                    return false;
            }
        }

        /// <summary>
        /// RoundResultから開始者を引き継ぎ、残存参加者の次ラウンドを開始します。
        /// </summary>
        /// <returns>次ラウンドの配布と宣言開始を完了した場合はtrue、それ以外はfalseです。</returns>
        public bool TryStartNextRound()
        {
            if (State != GameFlowState.RoundResult || LastRoundResult == null)
            {
                return false;
            }

            var nextStarterId = DetermineNextRoundStarter(LastRoundResult.LoserId);
            var orderedParticipantIds = CreateOrderedRemainingIds(_roster, nextStarterId);
            if (
                orderedParticipantIds.Count < 2
                || !_deck.TryDeal(orderedParticipantIds, out var deals)
            )
            {
                return false;
            }

            _deals = deals;
            _declarationPhase = new DeclarationPhase(orderedParticipantIds);
            RoundNumber++;
            StartingParticipantId = nextStarterId;
            CurrentParticipantId = nextStarterId;
            LastRoundResult = null;
            Outcome = GameOutcome.None;
            State = GameFlowState.Declaring;
            return true;
        }

        /// <summary>
        /// 現在のゲーム状態を破棄し、タイトルに対応するNoGameへ戻します。
        /// </summary>
        /// <returns>存在したゲーム状態を破棄した場合はtrue、既にNoGameならfalseです。</returns>
        public bool TryReturnToTitle()
        {
            if (State == GameFlowState.NoGame)
            {
                return false;
            }

            _roster = null;
            _deck = null;
            _declarationPhase = null;
            _deals = EmptyDeals;
            RoundNumber = 0;
            StartingParticipantId = null;
            CurrentParticipantId = null;
            LastRoundResult = null;
            Outcome = GameOutcome.None;
            State = GameFlowState.NoGame;
            return true;
        }

        /// <summary>
        /// 実合計、敗者、回収、ライフ、勝敗、結果スナップショットを一度だけ確定します。
        /// </summary>
        private void CompleteJudgment()
        {
            var finalDeclaration = _declarationPhase.CurrentDeclaration;
            var coyoteDeclarerId = _declarationPhase.CoyoteDeclarerId;
            if (!_fieldTotalCalculator.TryCalculate(_deck.InPlayCards, _deck, out var totalResult))
            {
                throw new InvalidOperationException(
                    "既定山札から場の合計値を確定できませんでした。"
                );
            }

            var loserId = _declarationPhase.DetermineLoser(totalResult.Total);
            var dealtCardSnapshot = _deals.ToList();
            if (!_deck.TryCompleteRound())
            {
                throw new InvalidOperationException(
                    "判定済みラウンドのカードを回収できませんでした。"
                );
            }

            if (!_roster.TryApplyLoss(loserId))
            {
                throw new InvalidOperationException(
                    "判定済みの敗者へライフ減少を適用できませんでした。"
                );
            }

            _roster.TryGetParticipant(loserId, out var loser);
            Outcome = DetermineOutcome();
            LastRoundResult = new RoundResultSnapshot(
                RoundNumber,
                dealtCardSnapshot,
                totalResult.AdditionalCard,
                totalResult.Total,
                finalDeclaration,
                coyoteDeclarerId,
                loserId,
                _roster.Participants,
                loser.IsEliminated ? loserId : null,
                Outcome
            );
            _deals = EmptyDeals;
            _declarationPhase = null;
            CurrentParticipantId = null;
            State =
                Outcome == GameOutcome.None ? GameFlowState.RoundResult : GameFlowState.GameOver;
        }

        /// <summary>
        /// ユーザー脱落を優先し、NPC全滅との排他的な勝敗を返します。
        /// </summary>
        private GameOutcome DetermineOutcome()
        {
            _roster.TryGetParticipant(UserParticipantId, out var user);
            if (user.IsEliminated)
            {
                return GameOutcome.UserDefeat;
            }

            return _roster
                .Participants.Where(item => item.Kind == ParticipantKind.Npc)
                .All(item => item.IsEliminated)
                ? GameOutcome.UserVictory
                : GameOutcome.None;
        }

        /// <summary>
        /// 敗者が残存すれば敗者、脱落済みならリング上の次の残存者を返します。
        /// </summary>
        private string DetermineNextRoundStarter(string loserId)
        {
            _roster.TryGetParticipant(loserId, out var loser);
            return loser.IsEliminated ? FindNextRemainingParticipantId(loserId) : loserId;
        }

        /// <summary>
        /// 固定リング上で指定参加者の次にいる残存参加者を返します。
        /// </summary>
        private string FindNextRemainingParticipantId(string participantId)
        {
            var allParticipants = _roster.Participants;
            var currentIndex = allParticipants
                .Select((item, index) => new { item.Id, Index = index })
                .Single(item => item.Id == participantId)
                .Index;
            for (var offset = 1; offset <= allParticipants.Count; offset++)
            {
                var candidate = allParticipants[(currentIndex + offset) % allParticipants.Count];
                if (!candidate.IsEliminated)
                {
                    return candidate.Id;
                }
            }

            throw new InvalidOperationException("次の残存参加者が存在しません。");
        }

        /// <summary>
        /// 開始者を先頭に固定リングを巡回し、残存参加者IDを並べます。
        /// </summary>
        private static IReadOnlyList<string> CreateOrderedRemainingIds(
            ParticipantRoster roster,
            string startingParticipantId
        )
        {
            var allParticipants = roster.Participants;
            var startIndex = allParticipants
                .Select((item, index) => new { item.Id, Index = index })
                .Single(item => item.Id == startingParticipantId)
                .Index;
            var orderedIds = new List<string>();
            for (var offset = 0; offset < allParticipants.Count; offset++)
            {
                var participant = allParticipants[(startIndex + offset) % allParticipants.Count];
                if (!participant.IsEliminated)
                {
                    orderedIds.Add(participant.Id);
                }
            }

            return orderedIds.AsReadOnly();
        }

        /// <summary>
        /// 操作元参加者IDの入力不備を状態確認より先に拒否します。
        /// </summary>
        private static void ValidateParticipantId(string participantId)
        {
            if (string.IsNullOrWhiteSpace(participantId))
            {
                throw new ArgumentException(
                    "参加者の識別子を指定してください。",
                    nameof(participantId)
                );
            }
        }
    }
}
