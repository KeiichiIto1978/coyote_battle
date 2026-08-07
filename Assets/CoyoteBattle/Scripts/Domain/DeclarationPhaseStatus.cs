namespace CoyoteBattle.Domain
{
    /// <summary>
    /// 1ラウンド内の宣言フェーズが受理できる操作を表します。
    /// </summary>
    public enum DeclarationPhaseStatus
    {
        /// <summary>
        /// 数字宣言または、数字宣言後のコヨーテ宣言を受理できる状態です。
        /// </summary>
        AcceptingNumberDeclarations,

        /// <summary>
        /// コヨーテ宣言により、追加の宣言操作を受理しない状態です。
        /// </summary>
        CoyoteDeclared,
    }
}
