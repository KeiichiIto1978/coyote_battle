using CoyoteBattle.Domain;

namespace CoyoteBattle.Application
{
    /// <summary>
    /// 参加者の識別情報とライフを保持する変更不能な公開状態です。
    /// </summary>
    public sealed class ParticipantState
    {
        internal ParticipantState(Participant participant)
        {
            Id = participant.Id;
            Kind = participant.Kind;
            Life = participant.Life;
            IsEliminated = participant.IsEliminated;
        }

        /// <summary>
        /// 参加者識別子を取得します。
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// ユーザーまたはNPCの種別を取得します。
        /// </summary>
        public ParticipantKind Kind { get; }

        /// <summary>
        /// スナップショット作成時点のライフを取得します。
        /// </summary>
        public int Life { get; }

        /// <summary>
        /// スナップショット作成時点で脱落しているかを取得します。
        /// </summary>
        public bool IsEliminated { get; }
    }
}
