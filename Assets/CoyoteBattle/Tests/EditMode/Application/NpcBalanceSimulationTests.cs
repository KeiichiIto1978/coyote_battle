using System;
using System.Collections.Generic;
using System.Linq;
using CoyoteBattle.Domain;
using NUnit.Framework;

namespace CoyoteBattle.Application.Tests
{
    /// <summary>
    /// 固定seedの多数ラウンドで、NPC開始時にユーザーへ手番が届く頻度を検証します。
    /// </summary>
    public sealed class NpcBalanceSimulationTests
    {
        private const int RequiredRoundCount = 1000;
        private const double RequiredSuccessRate = 0.80d;
        private const int SeedCount = 100;

        /// <summary>
        /// NPC開始かつユーザー残存の1000ラウンドで、結果前にユーザー手番が80%以上始まることを保証します。
        /// </summary>
        [Test]
        public void NpcTurns_固定seedで1000ラウンドを開始する_80パーセント以上ユーザーへ手番が届く()
        {
            var successfulRounds = 0;
            var targetRounds = 0;
            var discardedCardRounds = 0;
            var reducedParticipantRounds = 0;
            var failedRounds = new List<string>();
            for (var seed = 0; seed < SeedCount; seed++)
            {
                var service = new GameFlowService(
                    new NpcStarterRandomSource(seed),
                    new SeededRandomSource(seed ^ 0x5A17)
                );
                Assert.That(service.TryStartNewGame(), Is.True, $"seed={seed}");

                while (service.State != GameFlowState.GameOver)
                {
                    var isTargetRound = service.StartingParticipantId != "user";
                    var userTurnStarted = false;
                    var discardedCount = service.CardInformation.Sum(item => item.DiscardedCount);
                    var remainingParticipantCount = service.Participants.Count(item =>
                        !item.IsEliminated
                    );
                    var actionCount = 0;
                    while (service.State == GameFlowState.Declaring)
                    {
                        if (service.CurrentParticipantId == "user")
                        {
                            userTurnStarted = true;
                            Assert.That(ExecuteUserTurn(service), Is.True, $"seed={seed}");
                        }
                        else
                        {
                            Assert.That(
                                service.TryExecuteCurrentNpcTurn(),
                                Is.True,
                                $"seed={seed}"
                            );
                        }

                        actionCount++;
                        Assert.That(
                            actionCount,
                            Is.LessThan(1000),
                            $"seed={seed}, round={service.RoundNumber}"
                        );
                    }

                    if (isTargetRound)
                    {
                        targetRounds++;
                        if (userTurnStarted)
                        {
                            successfulRounds++;
                        }
                        else
                        {
                            failedRounds.Add($"{seed}:{service.RoundNumber}");
                        }

                        if (discardedCount > 0)
                        {
                            discardedCardRounds++;
                        }

                        if (remainingParticipantCount < 5)
                        {
                            reducedParticipantRounds++;
                        }
                    }

                    if (service.State == GameFlowState.RoundResult)
                    {
                        Assert.That(service.TryStartNextRound(), Is.True, $"seed={seed}");
                    }
                }
            }

            var successRate = (double)successfulRounds / targetRounds;
            TestContext.Out.WriteLine(
                $"seed一覧=0-{SeedCount - 1}, 対象={targetRounds}, 成功={successfulRounds}, "
                    + $"割合={successRate:P2}, 使用済みあり={discardedCardRounds}, "
                    + $"残存2-4名={reducedParticipantRounds}"
            );
            Assert.That(targetRounds, Is.GreaterThanOrEqualTo(RequiredRoundCount));
            Assert.That(discardedCardRounds, Is.GreaterThan(0));
            Assert.That(reducedParticipantRounds, Is.GreaterThan(0));
            Assert.That(
                successRate,
                Is.GreaterThanOrEqualTo(RequiredSuccessRate),
                $"seed一覧=0-{SeedCount - 1}, 対象={targetRounds}, "
                    + $"成功={successfulRounds}, 割合={successRate:P2}, "
                    + $"使用済みあり={discardedCardRounds}, "
                    + $"残存2-4名={reducedParticipantRounds}, "
                    + $"失敗seed:round={string.Join(",", failedRounds)}"
            );
        }

        private static bool ExecuteUserTurn(GameFlowService service)
        {
            var currentDeclaration = service.DeclarationHistory.LastOrDefault();
            if (currentDeclaration == null)
            {
                return service.TryDeclareNumber("user", 1);
            }

            return currentDeclaration.Value == int.MaxValue
                ? service.TryDeclareCoyote("user")
                : service.TryDeclareNumber("user", currentDeclaration.Value + 1);
        }

        private sealed class NpcStarterRandomSource : IRandomSource
        {
            private readonly SeededRandomSource _randomSource;
            private readonly int _starterIndex;
            private bool _hasReturnedStarter;

            internal NpcStarterRandomSource(int seed)
            {
                _starterIndex = (seed % 4) + 1;
                _randomSource = new SeededRandomSource(seed);
            }

            /// <summary>
            /// 最初にNPC開始位置を返し、以後はseed付き乱数で山札をシャッフルします。
            /// </summary>
            public int Next(int exclusiveUpperBound)
            {
                if (!_hasReturnedStarter)
                {
                    _hasReturnedStarter = true;
                    return _starterIndex;
                }

                return _randomSource.Next(exclusiveUpperBound);
            }
        }

        private sealed class SeededRandomSource : IRandomSource
        {
            private uint _state;

            internal SeededRandomSource(int seed)
            {
                _state = unchecked((uint)seed) + 1u;
            }

            /// <summary>
            /// 同じseedから同じ線形合同法の乱数列を返します。
            /// </summary>
            public int Next(int exclusiveUpperBound)
            {
                if (exclusiveUpperBound <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(exclusiveUpperBound));
                }

                _state = unchecked((_state * 1664525u) + 1013904223u);
                return (int)(_state % (uint)exclusiveUpperBound);
            }
        }
    }
}
