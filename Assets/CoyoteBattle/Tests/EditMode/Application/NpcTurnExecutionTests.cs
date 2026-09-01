using System;
using System.Linq;
using CoyoteBattle.Domain;
using NUnit.Framework;
using static CoyoteBattle.Application.Tests.GameFlowTestHelper;

namespace CoyoteBattle.Application.Tests
{
    public sealed class NpcTurnExecutionTests
    {
        /// <summary>
        /// NPC視点ではユーザー札を公開し、行動NPC自身の札だけを除外することを保証します。
        /// </summary>
        [Test]
        public void TryCreateCurrentNpcObservation_npc1手番である_自分以外の4枚を返す()
        {
            var service = new GameFlowService(
                new FirstValueThenZeroRandomSource(1),
                new ZeroRandomSource()
            );
            service.TryStartNewGame();

            var succeeded = service.TryCreateCurrentNpcObservation(out var observation);

            Assert.That(succeeded, Is.True);
            Assert.That(observation.ActorId, Is.EqualTo("npc-1"));
            Assert.That(observation.VisibleCards, Has.Count.EqualTo(4));
            Assert.That(
                observation.VisibleCards.Any(item => item.ParticipantId == "user"),
                Is.True
            );
            Assert.That(
                observation.VisibleCards.Any(item => item.ParticipantId == "npc-1"),
                Is.False
            );
        }

        /// <summary>
        /// 次ラウンドのNPC観測が、カード情報表示と同じ使用済み札状態を参照することを保証します。
        /// </summary>
        [Test]
        public void TryCreateCurrentNpcObservation_通常ラウンド後のnpc1手番である_カード情報と同じ使用済み枚数を返す()
        {
            var service = new GameFlowService(
                new FirstZeroThenIdentityRandomSource(),
                new ZeroRandomSource()
            );
            Assert.That(service.TryStartNewGame(), Is.True);
            Assert.That(service.TryDeclareNumber("user", int.MaxValue), Is.True);
            Assert.That(service.TryDeclareCoyote("npc-1"), Is.True);
            Assert.That(service.TryStartNextRound(), Is.True);
            Assert.That(service.TryDeclareNumber("user", 1), Is.True);

            var succeeded = service.TryCreateCurrentNpcObservation(out var observation);

            Assert.That(succeeded, Is.True);
            Assert.That(observation.DiscardedCards.Sum(item => item.Count), Is.EqualTo(5));
            foreach (var discardedCard in observation.DiscardedCards)
            {
                Assert.That(
                    service
                        .CardInformation.Single(item =>
                            item.Kind == discardedCard.Kind && item.Value == discardedCard.Value
                        )
                        .DiscardedCount,
                    Is.EqualTo(discardedCard.Count)
                );
            }
        }

        /// <summary>
        /// 夜カードで全山札を再構築した後、NPC観測の使用済み札も0へ戻ることを保証します。
        /// </summary>
        [Test]
        public void TryCreateCurrentNpcObservation_夜カードによる再構築後である_使用済み札を空で返す()
        {
            var service = new GameFlowService(
                new FirstZeroThenIdentityRandomSource(),
                new ZeroRandomSource()
            );
            Assert.That(service.TryStartNewGame(), Is.True);
            var loserIds = new[] { "npc-1", "npc-2", "npc-3", "npc-4", "user", "npc-1" };
            foreach (var loserId in loserIds)
            {
                LoseParticipant(service, loserId);
                Assert.That(service.TryStartNextRound(), Is.True);
            }

            Assert.That(service.CardInformation.Sum(item => item.DiscardedCount), Is.EqualTo(30));
            LoseParticipant(service, "npc-2");
            Assert.That(service.CardInformation.Sum(item => item.DiscardedCount), Is.Zero);
            Assert.That(
                service.TryStartNextRound(),
                Is.True,
                $"state={service.State}, outcome={service.Outcome}, "
                    + $"loser={service.LastRoundResult?.LoserId}"
            );
            if (service.CurrentParticipantId == "user")
            {
                Assert.That(service.TryDeclareNumber("user", 1), Is.True);
            }

            var succeeded = service.TryCreateCurrentNpcObservation(out var observation);

            Assert.That(succeeded, Is.True);
            Assert.That(observation.DiscardedCards, Is.Empty);
        }

        /// <summary>
        /// ユーザー手番ではNPC観測生成とNPC行動を拒否し、手番を維持することを保証します。
        /// </summary>
        [Test]
        public void TryExecuteCurrentNpcTurn_ユーザー手番である_状態を変えず拒否する()
        {
            var service = StartService();

            var observationSucceeded = service.TryCreateCurrentNpcObservation(out var observation);
            var executionSucceeded = service.TryExecuteCurrentNpcTurn();

            Assert.That(observationSucceeded, Is.False);
            Assert.That(observation, Is.Null);
            Assert.That(executionSucceeded, Is.False);
            Assert.That(service.CurrentParticipantId, Is.EqualTo("user"));
            Assert.That(service.DeclarationHistory, Is.Empty);
        }

        /// <summary>
        /// NPC手番の1回実行では数字宣言を1件だけ追加し、次の参加者へ進むことを保証します。
        /// </summary>
        [Test]
        public void TryExecuteCurrentNpcTurn_npc1の初手である_1手だけ実行してnpc2へ進む()
        {
            var service = new GameFlowService(
                new FirstValueThenZeroRandomSource(1),
                new ZeroRandomSource()
            );
            service.TryStartNewGame();

            var succeeded = service.TryExecuteCurrentNpcTurn();

            Assert.That(succeeded, Is.True);
            Assert.That(service.State, Is.EqualTo(GameFlowState.Declaring));
            Assert.That(service.DeclarationHistory, Has.Count.EqualTo(1));
            Assert.That(service.DeclarationHistory[0].ParticipantId, Is.EqualTo("npc-1"));
            Assert.That(service.CurrentParticipantId, Is.EqualTo("npc-2"));
        }

        /// <summary>
        /// int上限の次手NPCがコヨーテを選び、既存のラウンド判定まで進むことを保証します。
        /// </summary>
        [Test]
        public void TryExecuteCurrentNpcTurn_int上限宣言後のnpc1手番である_コヨーテで判定する()
        {
            var service = StartService();
            service.TryDeclareNumber("user", int.MaxValue);

            var succeeded = service.TryExecuteCurrentNpcTurn();

            Assert.That(succeeded, Is.True);
            Assert.That(service.State, Is.EqualTo(GameFlowState.RoundResult));
            Assert.That(service.LastRoundResult.CoyoteDeclarerId, Is.EqualTo("npc-1"));
        }

        /// <summary>
        /// ギャンブル型の判断がゲーム進行用乱数を消費しないことを保証します。
        /// </summary>
        [Test]
        public void TryExecuteCurrentNpcTurn_npc3手番である_NPC専用乱数だけを消費する()
        {
            var gameRandom = new FirstValueThenZeroRandomSource(3);
            var npcRandom = new CountingRandomSource(1);
            var service = new GameFlowService(gameRandom, npcRandom);
            service.TryStartNewGame();
            var gameRandomCountAfterStart = gameRandom.CallCount;

            var succeeded = service.TryExecuteCurrentNpcTurn();

            Assert.That(succeeded, Is.True);
            Assert.That(gameRandom.CallCount, Is.EqualTo(gameRandomCountAfterStart));
            Assert.That(npcRandom.CallCount, Is.EqualTo(1));
        }

        /// <summary>
        /// NPCタイプの固定割当がゲーム進行から参照できることを保証します。
        /// </summary>
        [TestCase("npc-1", NpcPersonality.Aggressive)]
        [TestCase("npc-2", NpcPersonality.Cautious)]
        [TestCase("npc-3", NpcPersonality.Gambling)]
        [TestCase("npc-4", NpcPersonality.Analytical)]
        public void NpcPersonalityAssignment_既定NPCを指定する_固定タイプを返す(
            string participantId,
            NpcPersonality expected
        )
        {
            var succeeded = NpcPersonalityAssignment.TryGet(participantId, out var personality);

            Assert.That(succeeded, Is.True);
            Assert.That(personality, Is.EqualTo(expected));
        }

        private sealed class CountingRandomSource : IRandomSource
        {
            private readonly int _value;

            internal CountingRandomSource(int value)
            {
                _value = value;
            }

            internal int CallCount { get; private set; }

            /// <summary>
            /// 指定値を返し、NPC判断からの呼び出し回数を記録します。
            /// </summary>
            public int Next(int exclusiveUpperBound)
            {
                CallCount++;
                return _value;
            }
        }

        private sealed class FirstZeroThenIdentityRandomSource : IRandomSource
        {
            private bool _hasReturnedStarter;

            /// <summary>
            /// 最初はユーザー開始位置、以後は交換しない位置を返します。
            /// </summary>
            public int Next(int exclusiveUpperBound)
            {
                if (!_hasReturnedStarter)
                {
                    _hasReturnedStarter = true;
                    return 0;
                }

                return exclusiveUpperBound - 1;
            }
        }
    }
}
