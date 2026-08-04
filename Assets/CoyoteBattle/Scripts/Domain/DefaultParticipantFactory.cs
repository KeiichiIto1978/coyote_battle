namespace CoyoteBattle.Domain
{
    /// <summary>
    /// Version 1で使用する固定5名の参加者管理を生成します。
    /// </summary>
    public static class DefaultParticipantFactory
    {
        /// <summary>
        /// ユーザー1名とNPC4名を確定した識別子と順序で生成します。
        /// </summary>
        /// <returns>全員が初期ライフ3の参加者管理です。</returns>
        public static ParticipantRoster Create()
        {
            var participants = new[]
            {
                new Participant("user", ParticipantKind.User),
                new Participant("npc-1", ParticipantKind.Npc),
                new Participant("npc-2", ParticipantKind.Npc),
                new Participant("npc-3", ParticipantKind.Npc),
                new Participant("npc-4", ParticipantKind.Npc),
            };

            return new ParticipantRoster(participants);
        }
    }
}
