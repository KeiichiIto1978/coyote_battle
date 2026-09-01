using System;
using CoyoteBattle.Domain;
using NUnit.Framework;

namespace CoyoteBattle.Application.Tests
{
    /// <summary>
    /// 複数の進行テストで使う再現可能なゲーム操作を提供します。
    /// </summary>
    internal static class GameFlowTestHelper
    {
        internal static readonly string[] ParticipantIds =
        {
            "user",
            "npc-1",
            "npc-2",
            "npc-3",
            "npc-4",
        };

        internal static GameFlowService CreateService()
        {
            return new GameFlowService(new ZeroRandomSource(), new ZeroRandomSource());
        }

        internal static GameFlowService StartService()
        {
            var service = CreateService();
            Assert.That(service.TryStartNewGame(), Is.True);
            return service;
        }

        internal static void LoseParticipantForRounds(
            GameFlowService service,
            string participantId,
            int count
        )
        {
            for (var index = 0; index < count; index++)
            {
                if (service.State == GameFlowState.RoundResult)
                {
                    Assert.That(service.TryStartNextRound(), Is.True);
                }

                LoseParticipant(service, participantId);
            }
        }

        internal static void LoseCurrentParticipant(GameFlowService service)
        {
            LoseParticipant(service, service.CurrentParticipantId);
        }

        internal static void LoseParticipant(GameFlowService service, string participantId)
        {
            var declarationValue = 1;
            while (service.CurrentParticipantId != participantId)
            {
                Assert.That(
                    service.TryDeclareNumber(service.CurrentParticipantId, declarationValue++),
                    Is.True
                );
            }

            Assert.That(service.TryDeclareNumber(participantId, int.MaxValue), Is.True);
            Assert.That(service.TryDeclareCoyote(service.CurrentParticipantId), Is.True);
        }

        internal sealed class FirstValueThenZeroRandomSource : IRandomSource
        {
            private readonly int _firstValue;
            private bool _hasReturnedFirstValue;

            internal FirstValueThenZeroRandomSource(int firstValue)
            {
                _firstValue = firstValue;
            }

            internal int CallCount { get; private set; }

            /// <summary>
            /// 最初の開始位置だけ指定値を返し、以後のシャッフルを再現可能にします。
            /// </summary>
            public int Next(int exclusiveUpperBound)
            {
                CallCount++;
                if (_hasReturnedFirstValue)
                {
                    return 0;
                }

                _hasReturnedFirstValue = true;
                return _firstValue;
            }
        }

        internal sealed class ZeroRandomSource : IRandomSource
        {
            /// <summary>
            /// 常に下限を返し、開始者とシャッフルを再現可能にします。
            /// </summary>
            public int Next(int exclusiveUpperBound)
            {
                return 0;
            }
        }
    }
}
