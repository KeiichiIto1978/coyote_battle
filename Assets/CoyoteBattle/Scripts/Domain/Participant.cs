using System;

namespace CoyoteBattle.Domain
{
    /// <summary>
    /// 操作方法に依存せず、識別情報とライフを管理する参加者です。
    /// </summary>
    public sealed class Participant
    {
        /// <summary>
        /// Version 1で参加者がゲーム開始時に持つライフです。
        /// </summary>
        public const int InitialLife = 3;

        /// <summary>
        /// 指定した識別子と種別で、初期ライフ3の参加者を生成します。
        /// </summary>
        /// <param name="id">空白ではない安定した内部識別子です。</param>
        /// <param name="kind">ユーザーまたはNPCの種別です。</param>
        public Participant(string id, ParticipantKind kind)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("参加者の識別子を指定してください。", nameof(id));
            }

            if (!Enum.IsDefined(typeof(ParticipantKind), kind))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(kind),
                    kind,
                    "定義された参加者種別を指定してください。"
                );
            }

            Id = id;
            Kind = kind;
            Life = InitialLife;
        }

        /// <summary>
        /// 参加者を一意に参照する内部識別子を取得します。
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// ユーザーまたはNPCの種別を取得します。
        /// </summary>
        public ParticipantKind Kind { get; }

        /// <summary>
        /// 0以上3以下の現在ライフを取得します。
        /// </summary>
        public int Life { get; private set; }

        /// <summary>
        /// ライフが0となり脱落しているかを取得します。
        /// </summary>
        public bool IsEliminated => Life == 0;

        /// <summary>
        /// ラウンド敗北を適用し、残存中ならライフを1減らします。
        /// </summary>
        /// <returns>ライフを減らした場合はtrue、既に脱落していた場合はfalseです。</returns>
        public bool TryLoseLife()
        {
            if (IsEliminated)
            {
                return false;
            }

            Life--;
            return true;
        }
    }
}
