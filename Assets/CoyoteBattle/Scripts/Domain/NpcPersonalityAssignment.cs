using System;

namespace CoyoteBattle.Domain
{
    /// <summary>
    /// Version 1の固定NPC識別子へ思考タイプを対応付けます。
    /// </summary>
    public static class NpcPersonalityAssignment
    {
        /// <summary>
        /// 既定NPCの固定思考タイプを返します。
        /// </summary>
        /// <param name="participantId">固定割当を検索するNPC識別子です。</param>
        /// <param name="personality">成功時の思考タイプです。</param>
        /// <returns>既定NPCの割当が存在する場合はtrueです。</returns>
        public static bool TryGet(string participantId, out NpcPersonality personality)
        {
            if (string.IsNullOrWhiteSpace(participantId))
            {
                throw new ArgumentException("NPC識別子を指定してください。", nameof(participantId));
            }

            switch (participantId)
            {
                case "npc-1":
                    personality = NpcPersonality.Aggressive;
                    return true;
                case "npc-2":
                    personality = NpcPersonality.Cautious;
                    return true;
                case "npc-3":
                    personality = NpcPersonality.Gambling;
                    return true;
                case "npc-4":
                    personality = NpcPersonality.Analytical;
                    return true;
                default:
                    personality = default;
                    return false;
            }
        }
    }

    /// <summary>
    /// Version 1で固定割当する4種類のNPC思考タイプです。
    /// </summary>
    public enum NpcPersonality
    {
        Aggressive,
        Cautious,
        Gambling,
        Analytical,
    }
}
