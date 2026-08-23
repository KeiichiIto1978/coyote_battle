using System.Collections;
using CoyoteBattle.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace CoyoteBattle.Tests.Presentation
{
    /// <summary>
    /// BGMの単一再生、設定UI、欠落時の継続、および中断復帰を保証します。
    /// </summary>
    public sealed class BgmPresentationTests
    {
        private const string TestKey = "CoyoteBattle.Tests.BgmPresentationEnabled";

        /// <summary>
        /// 常駐オブジェクトと保存設定を破棄し、テスト間の干渉を防ぎます。
        /// </summary>
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (var player in Object.FindObjectsByType<BgmPlayer>(FindObjectsSortMode.None))
            {
                Object.Destroy(player.gameObject);
            }

            foreach (
                var controller in Object.FindObjectsByType<GamePresentationController>(
                    FindObjectsSortMode.None
                )
            )
            {
                Object.Destroy(controller.gameObject);
            }

            PlayerPrefs.DeleteKey(TestKey);
            yield return null;
        }

        /// <summary>
        /// 初期化と画面コントローラー追加を繰り返してもAudioSourceが1つだけでループ再生されることを保証します。
        /// </summary>
        [UnityTest]
        public IEnumerator EnsureExists_複数回呼び出す_AudioSourceを1つだけループ再生する()
        {
            var clip = AudioClip.Create("TestBgm", 44100, 1, 44100, false);
            var player = CreateConfiguredPlayer(clip);
            yield return null;

            var second = BgmPlayer.EnsureExists();

            Assert.That(second, Is.SameAs(player));
            Assert.That(
                Object.FindObjectsByType<BgmPlayer>(FindObjectsSortMode.None),
                Has.Length.EqualTo(1)
            );
            Assert.That(player.GetComponents<AudioSource>(), Has.Length.EqualTo(1));
            Assert.That(player.AudioSource.loop, Is.True);
            Assert.That(player.AudioSource.clip, Is.SameAs(clip));
            Assert.That(player.AudioSource.isPlaying, Is.True);
        }

        /// <summary>
        /// TitleからBattleへ切り替えてもBGMプレイヤーと再生位置を作り直さないことを保証します。
        /// </summary>
        [UnityTest]
        public IEnumerator StartNewGame_TitleからBattleへ遷移_BGMを先頭へ戻さず継続する()
        {
            var clip = AudioClip.Create("TestBgm", 176400, 1, 44100, false);
            var player = CreateConfiguredPlayer(clip);
            var presentation = new GameObject("Presentation");
            presentation.AddComponent<GamePresentationController>();
            yield return null;
            player.AudioSource.timeSamples = 22050;

            var root = presentation.GetComponent<UIDocument>().rootVisualElement;
            Click(root.Q<Button>("start-game-button"));
            yield return null;

            Assert.That(
                root.Q<VisualElement>("battle-screen").resolvedStyle.display,
                Is.EqualTo(DisplayStyle.Flex)
            );
            Assert.That(BgmPlayer.EnsureExists(), Is.SameAs(player));
            Assert.That(player.AudioSource.timeSamples, Is.GreaterThanOrEqualTo(22050));
            Assert.That(
                Object.FindObjectsByType<BgmPlayer>(FindObjectsSortMode.None),
                Has.Length.EqualTo(1)
            );
        }

        /// <summary>
        /// 音源を同梱できない状態でも警告に留まり、画面を操作可能なまま生成できることを保証します。
        /// </summary>
        [UnityTest]
        public IEnumerator Initialize_AudioClipが欠落_タイトル画面の生成を継続する()
        {
            LogAssert.Expect(
                LogType.Warning,
                "BGM音源を読み込めないため、無音でゲームを続行します。"
            );
            CreateConfiguredPlayer(null);
            var presentation = new GameObject("Presentation");
            presentation.AddComponent<GamePresentationController>();
            yield return null;

            Assert.That(
                presentation
                    .GetComponent<UIDocument>()
                    .rootVisualElement.Q<Button>("start-game-button"),
                Is.Not.Null
            );
        }

        /// <summary>
        /// 共通BGMスイッチが全画面の外側にあり、OFFとONを再生へ即時反映して保存することを保証します。
        /// </summary>
        [UnityTest]
        public IEnumerator BgmToggle_OFFからONへ変更_即時反映して設定を保存する()
        {
            var clip = AudioClip.Create("TestBgm", 44100, 1, 44100, false);
            var player = CreateConfiguredPlayer(clip);
            var presentation = new GameObject("Presentation");
            presentation.AddComponent<GamePresentationController>();
            yield return null;

            var toggle = presentation
                .GetComponent<UIDocument>()
                .rootVisualElement.Q<Toggle>("bgm-toggle");
            Assert.That(toggle, Is.Not.Null);
            Assert.That(toggle.value, Is.True);

            toggle.value = false;
            yield return null;
            Assert.That(player.AudioSource.isPlaying, Is.False);
            Assert.That(new PlayerPrefsBgmSettingsStore(TestKey).LoadEnabled(), Is.False);

            toggle.value = true;
            yield return null;
            Assert.That(player.AudioSource.isPlaying, Is.True);
            Assert.That(new PlayerPrefsBgmSettingsStore(TestKey).LoadEnabled(), Is.True);
        }

        /// <summary>
        /// アプリ中断時の一時停止と復帰で再生位置を巻き戻さないことを保証します。
        /// </summary>
        [UnityTest]
        public IEnumerator OnApplicationPause_中断後に復帰_同じ位置から再開する()
        {
            var clip = AudioClip.Create("TestBgm", 88200, 1, 44100, false);
            var player = CreateConfiguredPlayer(clip);
            yield return null;
            player.AudioSource.timeSamples = 22050;

            player.HandleApplicationPause(true);
            var pausedAt = player.AudioSource.timeSamples;
            player.HandleApplicationPause(false);

            Assert.That(pausedAt, Is.GreaterThanOrEqualTo(22050));
            Assert.That(player.AudioSource.timeSamples, Is.GreaterThanOrEqualTo(pausedAt));
            Assert.That(player.AudioSource.isPlaying, Is.True);
        }

        private static BgmPlayer CreateConfiguredPlayer(AudioClip clip)
        {
            var gameObject = new GameObject("BgmPlayerTest");
            gameObject.SetActive(false);
            var player = gameObject.AddComponent<BgmPlayer>();
            player.ConfigureForTests(new PlayerPrefsBgmSettingsStore(TestKey), clip);
            gameObject.SetActive(true);
            return player;
        }

        private static void Click(Button button)
        {
            var position = button.worldBound.center;
            var downEvent = new Event
            {
                type = EventType.MouseDown,
                button = 0,
                mousePosition = position,
            };
            using (var pointerDown = PointerDownEvent.GetPooled(downEvent))
            {
                button.SendEvent(pointerDown);
            }

            var upEvent = new Event
            {
                type = EventType.MouseUp,
                button = 0,
                mousePosition = position,
            };
            using (var pointerUp = PointerUpEvent.GetPooled(upEvent))
            {
                button.SendEvent(pointerUp);
            }
            button.ReleasePointer(PointerId.mousePointerId);
        }
    }
}
