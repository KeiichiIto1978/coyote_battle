namespace CoyoteBattle.Domain
{
    /// <summary>
    /// 「？」の解決に使う追加カードの取得元です。
    /// </summary>
    public interface IAdditionalCardSource
    {
        /// <summary>
        /// 追加カードを1枚取得し、取得できない場合は状態を変更せず失敗します。
        /// </summary>
        /// <param name="card">成功時に取得したカードです。</param>
        /// <returns>カードを取得できた場合はtrue、それ以外はfalseです。</returns>
        bool TryDrawAdditional(out Card card);
    }
}
