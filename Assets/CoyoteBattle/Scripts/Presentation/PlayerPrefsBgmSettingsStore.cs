using System;
using UnityEngine;

namespace CoyoteBattle.Presentation
{
    /// <summary>
    /// BGMのON/OFFをPlayerPrefsへ保存します。
    /// </summary>
    internal sealed class PlayerPrefsBgmSettingsStore : IBgmSettingsStore
    {
        private const string DefaultKey = "CoyoteBattle.Audio.BgmEnabled";
        private const string EnabledValue = "1";
        private const string DisabledValue = "0";
        private readonly string _key;

        /// <summary>
        /// 本番用の保存キーで設定ストアを生成します。
        /// </summary>
        public PlayerPrefsBgmSettingsStore()
            : this(DefaultKey) { }

        /// <summary>
        /// 指定した保存キーで設定ストアを生成します。
        /// </summary>
        /// <param name="key">PlayerPrefsへ保存するキーです。</param>
        internal PlayerPrefsBgmSettingsStore(string key)
        {
            _key = string.IsNullOrWhiteSpace(key)
                ? throw new ArgumentException("保存キーを指定してください。", nameof(key))
                : key;
        }

        /// <inheritdoc />
        public bool LoadEnabled()
        {
            var value = PlayerPrefs.GetString(_key, EnabledValue);
            return value != DisabledValue;
        }

        /// <inheritdoc />
        public void SaveEnabled(bool enabled)
        {
            PlayerPrefs.SetString(_key, enabled ? EnabledValue : DisabledValue);
            PlayerPrefs.Save();
        }
    }
}
