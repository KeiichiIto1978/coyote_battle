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
        private const string TitleBgmResourcePath = "Audio/CoyoteBattleTitleTheme";
        private const string BattleBgmResourcePath = "Audio/CoyoteBattleTheme";
        private static BgmPlayer _instance;
        private IBgmSettingsStore _settingsStore = new PlayerPrefsBgmSettingsStore();
        private AudioClip _titleClip;
        private AudioClip _battleClip;
        private AudioSource _audioSource;
        private bool _hasConfiguredClips;
        private bool _hasStartedPlayback;
        private bool _isApplicationPaused;
        private bool _isInitialized;

        /// <summary>
        /// 現在BGM設定がONかどうかを取得します。
        /// </summary>
        public bool IsEnabled { get; private set; } = true;

        /// <summary>
        /// 現在選択されているBGMの画面種別を取得します。
        /// </summary>
        internal BgmTrack CurrentTrack { get; private set; } = BgmTrack.Title;

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
        /// 画面種別に対応するBGMへ切り替えます。同じ曲の再指定では再生位置を維持します。
        /// </summary>
        /// <param name="track">再生するBGMの画面種別です。</param>
        internal void SetTrack(BgmTrack track)
        {
            if (CurrentTrack == track)
            {
                return;
            }

            CurrentTrack = track;
            _audioSource.Stop();
            _hasStartedPlayback = false;
            _audioSource.clip = ClipFor(track);
            if (_audioSource.clip == null)
            {
                Debug.LogWarning($"{track}用BGM音源を読み込めないため、無音でゲームを続行します。");
                return;
            }

            ApplyPlaybackState();
        }

        /// <summary>
        /// PlayModeテスト用に保存先と音源を初期化前に差し替えます。
        /// </summary>
        /// <param name="settingsStore">テストで利用する設定ストアです。</param>
        /// <param name="titleClip">Titleで再生するテスト音源です。</param>
        /// <param name="battleClip">ゲーム中に再生するテスト音源です。</param>
        internal void ConfigureForTests(
            IBgmSettingsStore settingsStore,
            AudioClip titleClip,
            AudioClip battleClip
        )
        {
            if (_isInitialized)
            {
                throw new InvalidOperationException("初期化後にBGM構成は変更できません。");
            }

            _settingsStore =
                settingsStore ?? throw new ArgumentNullException(nameof(settingsStore));
            _titleClip = titleClip;
            _battleClip = battleClip;
            _hasConfiguredClips = true;
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
            if (!_hasConfiguredClips)
            {
                _titleClip = Resources.Load<AudioClip>(TitleBgmResourcePath);
                _battleClip = Resources.Load<AudioClip>(BattleBgmResourcePath);
            }

            _audioSource.clip = ClipFor(CurrentTrack);
            if (_audioSource.clip == null)
            {
                Debug.LogWarning("BGM音源を読み込めないため、無音でゲームを続行します。");
                return;
            }

            ApplyPlaybackState();
        }

        private AudioClip ClipFor(BgmTrack track)
        {
            return track == BgmTrack.Title ? _titleClip : _battleClip;
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
