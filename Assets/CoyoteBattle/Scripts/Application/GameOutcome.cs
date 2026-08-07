namespace CoyoteBattle.Application
{
    /// <summary>
    /// ユーザーを基準にしたゲームの最終勝敗です。
    /// </summary>
    public enum GameOutcome
    {
        /// <summary>最終勝敗がまだ確定していません。</summary>
        None,

        /// <summary>ユーザー以外の参加者が全員脱落しました。</summary>
        UserVictory,

        /// <summary>ユーザーが脱落しました。</summary>
        UserDefeat,
    }
}
