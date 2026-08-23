using UnityEngine;
using UnityEngine.UIElements;

namespace CoyoteBattle.Presentation
{
    public sealed partial class GamePresentationController
    {
        private BgmPlayer _bgmPlayer;
        private Toggle _bgmToggle;

        /// <summary>
        /// すべての画面で操作できるBGM設定スイッチを構築します。
        /// </summary>
        private void BuildBgmControls()
        {
            _bgmPlayer = BgmPlayer.EnsureExists();
            _bgmToggle = new Toggle("BGM") { name = "bgm-toggle", value = _bgmPlayer.IsEnabled };
            _bgmToggle.style.position = Position.Absolute;
            _bgmToggle.style.top = 20;
            _bgmToggle.style.left = 24;
            _bgmToggle.style.width = 140;
            _bgmToggle.style.height = 48;
            _bgmToggle.style.paddingLeft = 12;
            _bgmToggle.style.paddingRight = 12;
            _bgmToggle.style.backgroundColor = new Color(0.02f, 0.06f, 0.1f, 0.88f);
            _bgmToggle.style.unityFontStyleAndWeight = FontStyle.Bold;
            _bgmToggle.RegisterValueChangedCallback(OnBgmToggleChanged);
            _root.Add(_bgmToggle);
        }

        /// <summary>
        /// BGM設定スイッチの変更を再生状態へ即時反映します。
        /// </summary>
        /// <param name="changeEvent">変更後の設定値を含むイベントです。</param>
        private void OnBgmToggleChanged(ChangeEvent<bool> changeEvent)
        {
            _bgmPlayer.SetEnabled(changeEvent.newValue);
        }

        /// <summary>
        /// 画面破棄後にUIイベントが再生制御へ届かないよう購読を解除します。
        /// </summary>
        private void DetachBgmControls()
        {
            _bgmToggle?.UnregisterValueChangedCallback(OnBgmToggleChanged);
        }
    }
}
