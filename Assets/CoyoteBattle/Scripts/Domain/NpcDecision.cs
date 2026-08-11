using System;

namespace CoyoteBattle.Domain
{
    /// <summary>
    /// NPCが選択した数字宣言またはコヨーテ宣言を表します。
    /// </summary>
    public sealed class NpcDecision
    {
        private NpcDecision(NpcDecisionKind kind, int? number)
        {
            Kind = kind;
            Number = number;
        }

        /// <summary>
        /// 選択した行動種別を取得します。
        /// </summary>
        public NpcDecisionKind Kind { get; }

        /// <summary>
        /// 数字宣言値を取得します。コヨーテではnullです。
        /// </summary>
        public int? Number { get; }

        /// <summary>
        /// 1以上の数字宣言を生成します。
        /// </summary>
        /// <param name="number">宣言する1以上の整数です。</param>
        /// <returns>指定値を保持する数字宣言です。</returns>
        public static NpcDecision DeclareNumber(int number)
        {
            if (number < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(number));
            }

            return new NpcDecision(NpcDecisionKind.Number, number);
        }

        /// <summary>
        /// コヨーテ宣言を生成します。
        /// </summary>
        /// <returns>数字を持たないコヨーテ宣言です。</returns>
        public static NpcDecision DeclareCoyote()
        {
            return new NpcDecision(NpcDecisionKind.Coyote, null);
        }
    }

    /// <summary>
    /// NPC判断が返す宣言種別です。
    /// </summary>
    public enum NpcDecisionKind
    {
        Number,
        Coyote,
    }
}
