namespace CoyoteBattle.Domain
{
    /// <summary>
    /// NPC専用観測からルール上有効な1つの宣言を選択します。
    /// </summary>
    public interface INpcDecisionStrategy
    {
        /// <summary>
        /// 現在の観測に対する数字宣言またはコヨーテ宣言を返します。
        /// </summary>
        /// <param name="observation">行動NPC専用の変更不能な観測です。</param>
        /// <returns>既存宣言ルールへ適用できる1つの行動です。</returns>
        NpcDecision Decide(NpcObservation observation);
    }
}
