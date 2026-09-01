namespace CoyoteBattle.Application
{
    /// <summary>
    /// 外部から安定して観測できるゲーム進行状態です。
    /// </summary>
    public enum GameFlowState
    {
        /// <summary>ゲーム固有状態が存在しません。</summary>
        NoGame,

        /// <summary>カード配布後の宣言を受け付けています。</summary>
        Declaring,

        /// <summary>ラウンド判定を完了し、結果を保持しています。</summary>
        RoundResult,

        /// <summary>最終勝敗と最終ラウンド結果を保持しています。</summary>
        GameOver,
    }
}
