using System;
using UnityEngine;

namespace CoyoteBattle.Presentation
{
    /// <summary>
    /// 画面とシーンをまたいで、オリジナルBGMを1つだけループ再生します。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BgmPlayer : MonoBehaviour
    {
        private const string BgmResourcePath = "Audio/CoyoteBattleTheme";
        private static BgmPlayer _instance;
        private IBgmSettingsStore _settingsStore = new PlayerPrefsBgmSettingsStore();
        private AudioClip _configuredClip;
        private AudioSource _audioSource;
        private bool _hasConfiguredClip;
        private bool _hasStartedPlayback;
        private bool _isApplicationPaused;
        private bool _isInitialized;

        /// <summary>
        /// 現在BGM設定がONかどうかを取得します。
        /// </summary>
        public bool IsEnabled { get; private set; } = true;

        /// <summary>
        /// BGM再生に使用するAudioSourceを取得します。
        /// </summary>
        internal AudioSource AudioSource => _audioSource;

        /// <summary>
        /// 既存インスタンスを返し、存在しない場合だけ常駐プレイヤーを生成します。
        /// </summary>
        /// <returns>アプリ全体で共有するBGMプレイヤーです。</returns>
        public static BgmPlayer EnsureExists()
        {
            if (_instance != null)
            {
                return _instance;
            }

            var existing = FindFirstObjectByType<BgmPlayer>();
            if (existing != null)
            {
                _instance = existing;
                return existing;
            }

            return new GameObject("CoyoteBattleAudio").AddComponent<BgmPlayer>();
        }

        /// <summary>
        /// Unityライフサイクルから単一インスタンスとAudioSourceを初期化します。
        /// </summary>
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            Initialize();
        }

        /// <summary>
        /// 破棄されたインスタンスを静的参照から外します。
        /// </summary>
        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        /// <summary>
        /// アプリ中断中は一時停止し、復帰時は設定がONなら同じ位置から再開します。
        /// </summary>
        /// <param name="pauseStatus">アプリが中断された場合はtrueです。</param>
        internal void HandleApplicationPause(bool pauseStatus)
        {
            _isApplicationPaused = pauseStatus;
            if (_audioSource == null || _audioSource.clip == null)
            {
                return;
            }

            if (pauseStatus)
            {
                _audioSource.Pause();
            }
            else if (IsEnabled)
            {
                ResumePlayback();
            }
        }

        /// <summary>
        /// Unityから通知されたアプリ中断状態を再生制御へ反映します。
        /// </summary>
        /// <param name="pauseStatus">アプリが中断された場合はtrueです。</param>
        private void OnApplicationPause(bool pauseStatus)
        {
            HandleApplicationPause(pauseStatus);
        }

        /// <summary>
        /// BGMのON/OFFを即時反映し、次回起動向けに保存します。
        /// </summary>
        /// <param name="enabled">BGMを再生する場合はtrueです。</param>
        public void SetEnabled(bool enabled)
        {
            IsEnabled = enabled;
            _settingsStore.SaveEnabled(enabled);
            ApplyPlaybackState();
        }

        /// <summary>
        /// PlayModeテスト用に保存先と音源を初期化前に差し替えます。
        /// </summary>
        /// <param name="settingsStore">テストで利用する設定ストアです。</param>
        /// <param name="clip">再生するテスト音源です。欠落動作ではnullを指定します。</param>
        internal void ConfigureForTests(IBgmSettingsStore settingsStore, AudioClip clip)
        {
            if (_isInitialized)
            {
                throw new InvalidOperationException("初期化後にBGM構成は変更できません。");
            }

            _settingsStore =
                settingsStore ?? throw new ArgumentNullException(nameof(settingsStore));
            _configuredClip = clip;
            _hasConfiguredClip = true;
        }

        private void Initialize()
        {
            if (_isInitialized)
            {
                return;
            }

            _isInitialized = true;
            IsEnabled = _settingsStore.LoadEnabled();
            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
            }
            _audioSource.playOnAwake = false;
            _audioSource.loop = true;
            _audioSource.clip = _hasConfiguredClip
                ? _configuredClip
                : Resources.Load<AudioClip>(BgmResourcePath);
            if (_audioSource.clip == null)
            {
                Debug.LogWarning("BGM音源を読み込めないため、無音でゲームを続行します。");
                return;
            }

            ApplyPlaybackState();
        }

        private void ApplyPlaybackState()
        {
            if (_audioSource == null || _audioSource.clip == null)
            {
                return;
            }

            if (IsEnabled && !_isApplicationPaused)
            {
                ResumePlayback();
            }
            else
            {
                _audioSource.Pause();
            }
        }

        private void ResumePlayback()
        {
            if (_hasStartedPlayback)
            {
                _audioSource.UnPause();
                return;
            }

            _audioSource.Play();
            _hasStartedPlayback = true;
        }
    }
}
