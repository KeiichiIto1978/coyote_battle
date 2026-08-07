namespace CoyoteBattle.Domain
{
    /// <summary>
    /// 受理された1回の数字宣言を表す変更不能な記録です。
    /// </summary>
    public sealed class NumberDeclaration
    {
        /// <summary>
        /// 検証済みの宣言者と宣言値から記録を生成します。
        /// </summary>
        /// <param name="participantId">宣言者の安定した内部識別子です。</param>
        /// <param name="value">1以上の宣言値です。</param>
        internal NumberDeclaration(string participantId, int value)
        {
            ParticipantId = participantId;
            Value = value;
        }

        /// <summary>
        /// 宣言した参加者の内部識別子を取得します。
        /// </summary>
        public string ParticipantId { get; }

        /// <summary>
        /// 宣言した1以上の整数を取得します。
        /// </summary>
        public int Value { get; }
    }
}
