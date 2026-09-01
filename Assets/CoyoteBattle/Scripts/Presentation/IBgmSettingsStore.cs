namespace CoyoteBattle.Presentation
{
    /// <summary>
    /// BGMの有効設定を永続化する境界です。
    /// </summary>
    internal interface IBgmSettingsStore
    {
        /// <summary>
        /// 保存済みのBGM有効設定を読み込みます。
        /// </summary>
        /// <returns>BGMを再生する場合はtrueです。</returns>
        bool LoadEnabled();

        /// <summary>
        /// BGMの有効設定を保存します。
        /// </summary>
        /// <param name="enabled">BGMを再生する場合はtrueです。</param>
        void SaveEnabled(bool enabled);
    }
}
