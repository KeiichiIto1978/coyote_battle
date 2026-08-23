using UnityEngine;
using UnityEngine.UIElements;

namespace CoyoteBattle.Presentation
{
    public sealed partial class GamePresentationController
    {
        private BgmPlayer _bgmPlayer;
        private Button _bgmButton;

        /// <summary>
        /// すべての画面で操作でき、現在状態を明記するBGM設定ボタンを構築します。
        /// </summary>
        private void BuildBgmControls()
        {
            _bgmPlayer = BgmPlayer.EnsureExists();
            _bgmButton = new Button(OnBgmButtonClicked) { name = "bgm-toggle-button" };
            _bgmButton.style.position = Position.Absolute;
            _bgmButton.style.top = 20;
            _bgmButton.style.left = 24;
            _bgmButton.style.width = 150;
            _bgmButton.style.height = 52;
            _bgmButton.style.fontSize = 18;
            _bgmButton.style.paddingLeft = 12;
            _bgmButton.style.paddingRight = 12;
            _bgmButton.style.borderTopWidth = 2;
            _bgmButton.style.borderRightWidth = 2;
            _bgmButton.style.borderBottomWidth = 2;
            _bgmButton.style.borderLeftWidth = 2;
            _bgmButton.style.unityFontStyleAndWeight = FontStyle.Bold;
            _bgmButton.style.opacity = 1;
            ApplyBgmButtonVisual();
            _root.Add(_bgmButton);
        }

        /// <summary>
        /// BGM設定を反転し、再生とボタン表示へ即時反映します。
        /// </summary>
        private void OnBgmButtonClicked()
        {
            _bgmPlayer.SetEnabled(!_bgmPlayer.IsEnabled);
            ApplyBgmButtonVisual();
        }

        /// <summary>
        /// ONとOFFを文字と配色の両方で区別し、OFFでも操作可能な外観を維持します。
        /// </summary>
        private void ApplyBgmButtonVisual()
        {
            var isEnabled = _bgmPlayer.IsEnabled;
            _bgmButton.text = isEnabled ? "BGM ON" : "BGM OFF";
            _bgmButton.style.color = Color.white;
            _bgmButton.style.backgroundColor = isEnabled
                ? new Color(0.04f, 0.32f, 0.26f, 1f)
                : new Color(0.42f, 0.18f, 0.06f, 1f);
            var borderColor = isEnabled
                ? new Color(0.25f, 0.9f, 0.7f)
                : new Color(1f, 0.55f, 0.28f);
            _bgmButton.style.borderTopColor = borderColor;
            _bgmButton.style.borderRightColor = borderColor;
            _bgmButton.style.borderBottomColor = borderColor;
            _bgmButton.style.borderLeftColor = borderColor;
        }

        /// <summary>
        /// 画面破棄後にUIイベントが再生制御へ届かないよう購読を解除します。
        /// </summary>
        private void DetachBgmControls()
        {
            if (_bgmButton != null)
            {
                _bgmButton.clicked -= OnBgmButtonClicked;
            }
        }
    }
}
