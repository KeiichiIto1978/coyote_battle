using CoyoteBattle.Domain;

namespace CoyoteBattle.Presentation
{
    /// <summary>
    /// Domain/Applicationの識別値を日本語UI表記へ変換します。
    /// </summary>
    public static class PresentationText
    {
        /// <summary>
        /// 固定参加者IDから画面表示名を返します。
        /// </summary>
        public static string ParticipantName(string participantId)
        {
            switch (participantId)
            {
                case "user":
                    return "あなた";
                case "npc-1":
                    return "NPC 1（強気）";
                case "npc-2":
                    return "NPC 2（慎重）";
                case "npc-3":
                    return "NPC 3（ギャンブル）";
                case "npc-4":
                    return "NPC 4（分析）";
                default:
                    return participantId ?? string.Empty;
            }
        }

        /// <summary>
        /// カード種別と値を短い識別表記へ変換します。
        /// </summary>
        public static string Card(CardKind kind, int? value)
        {
            switch (kind)
            {
                case CardKind.Number:
                    return value?.ToString() ?? "?";
                case CardKind.Night:
                    return "夜";
                case CardKind.Double:
                    return "×2";
                case CardKind.MaxToZero:
                    return "MAX→0";
                case CardKind.Mystery:
                    return "？";
                default:
                    return "?";
            }
        }
    }
}
