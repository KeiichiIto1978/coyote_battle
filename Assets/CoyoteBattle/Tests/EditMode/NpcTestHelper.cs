using System.Collections.Generic;
using System.Linq;

namespace CoyoteBattle.Domain.Tests
{
    /// <summary>
    /// NPC判断テスト用の変更不能な観測を、固定5名の整合した状態から生成します。
    /// </summary>
    internal static class NpcTestHelper
    {
        internal static NpcObservation CreateObservation(
            string actorId = "npc-1",
            IReadOnlyList<CardDeal> visibleCards = null,
            IReadOnlyList<int> declarations = null,
            int actorLife = 3,
            IReadOnlyList<string> remainingIds = null
        )
        {
            var activeIds = remainingIds ?? new[] { "user", "npc-1", "npc-2", "npc-3", "npc-4" };
            var participants = new[]
            {
                new NpcParticipantObservation(
                    "user",
                    ParticipantKind.User,
                    activeIds.Contains("user") ? 3 : 0
                ),
                new NpcParticipantObservation(
                    "npc-1",
                    ParticipantKind.Npc,
                    actorId == "npc-1" ? actorLife
                        : activeIds.Contains("npc-1") ? 3
                        : 0
                ),
                new NpcParticipantObservation(
                    "npc-2",
                    ParticipantKind.Npc,
                    actorId == "npc-2" ? actorLife
                        : activeIds.Contains("npc-2") ? 3
                        : 0
                ),
                new NpcParticipantObservation(
                    "npc-3",
                    ParticipantKind.Npc,
                    actorId == "npc-3" ? actorLife
                        : activeIds.Contains("npc-3") ? 3
                        : 0
                ),
                new NpcParticipantObservation(
                    "npc-4",
                    ParticipantKind.Npc,
                    actorId == "npc-4" ? actorLife
                        : activeIds.Contains("npc-4") ? 3
                        : 0
                ),
            };
            var cards = visibleCards ?? CreateDefaultVisibleCards(actorId, activeIds);
            var phase = new DeclarationPhase(activeIds);
            if (declarations != null)
            {
                for (var index = 0; index < declarations.Count; index++)
                {
                    phase.TryDeclareNumber(activeIds[index % activeIds.Count], declarations[index]);
                }
            }

            return new NpcObservation(actorId, participants, activeIds, cards, phase.History);
        }

        private static IReadOnlyList<CardDeal> CreateDefaultVisibleCards(
            string actorId,
            IReadOnlyList<string> remainingIds
        )
        {
            var cards = new List<CardDeal>();
            var values = new[] { 20, 5, -5, 0, 1 };
            for (var index = 0; index < remainingIds.Count; index++)
            {
                if (remainingIds[index] == actorId)
                {
                    continue;
                }

                cards.Add(
                    new CardDeal(remainingIds[index], Card.CreateNumber(100 + index, values[index]))
                );
            }

            return cards.AsReadOnly();
        }
    }
}
