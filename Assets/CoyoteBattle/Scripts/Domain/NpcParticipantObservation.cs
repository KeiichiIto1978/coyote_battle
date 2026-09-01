using System;

namespace CoyoteBattle.Domain
{
    /// <summary>
    /// NPC判断時点の参加者識別情報とライフを保持する変更不能な観測です。
    /// </summary>
    public sealed class NpcParticipantObservation
    {
        /// <summary>
        /// 参加者の公開状態を生成します。
        /// </summary>
        /// <param name="id">空白ではない参加者識別子です。</param>
        /// <param name="kind">ユーザーまたはNPCの種別です。</param>
        /// <param name="life">0以上3以下のライフです。</param>
        public NpcParticipantObservation(string id, ParticipantKind kind, int life)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("参加者の識別子を指定してください。", nameof(id));
            }

            if (!Enum.IsDefined(typeof(ParticipantKind), kind))
            {
                throw new ArgumentOutOfRangeException(nameof(kind));
            }

            if (life < 0 || life > Participant.InitialLife)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(life),
                    life,
                    "ライフは0以上3以下で指定してください。"
                );
            }

            Id = id;
            Kind = kind;
            Life = life;
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
        /// 判断時点のライフを取得します。
        /// </summary>
        public int Life { get; }

        /// <summary>
        /// ライフ0で脱落しているかを取得します。
        /// </summary>
        public bool IsEliminated => Life == 0;
    }
}
