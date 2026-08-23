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
            var titleClip = AudioClip.Create("TitleBgm", 44100, 1, 44100, false);
            var battleClip = AudioClip.Create("BattleBgm", 44100, 1, 44100, false);
            var player = CreateConfiguredPlayer(titleClip, battleClip);
            yield return null;

            var second = BgmPlayer.EnsureExists();

            Assert.That(second, Is.SameAs(player));
            Assert.That(
                Object.FindObjectsByType<BgmPlayer>(FindObjectsSortMode.None),
                Has.Length.EqualTo(1)
            );
            Assert.That(player.GetComponents<AudioSource>(), Has.Length.EqualTo(1));
            Assert.That(player.AudioSource.loop, Is.True);
            Assert.That(player.AudioSource.clip, Is.SameAs(titleClip));
            Assert.That(player.AudioSource.isPlaying, Is.True);
        }

        /// <summary>
        /// Titleとゲーム中で別のBGMへ切り替え、同じ画面種別の再指定では先頭へ戻らないことを保証します。
        /// </summary>
        [UnityTest]
        public IEnumerator SetTrack_TitleからBattleとTitleへ切替_指定曲を一度だけ先頭から再生する()
        {
            var titleClip = AudioClip.Create("TitleBgm", 176400, 1, 44100, false);
            var battleClip = AudioClip.Create("BattleBgm", 176400, 1, 44100, false);
            var player = CreateConfiguredPlayer(titleClip, battleClip);
            yield return null;
            player.AudioSource.timeSamples = 22050;

            player.SetTrack(BgmTrack.Battle);

            Assert.That(player.AudioSource.clip, Is.SameAs(battleClip));
            Assert.That(player.AudioSource.timeSamples, Is.LessThan(22050));
            player.AudioSource.timeSamples = 33075;

            player.SetTrack(BgmTrack.Battle);

            Assert.That(player.AudioSource.timeSamples, Is.GreaterThanOrEqualTo(33075));

            player.SetTrack(BgmTrack.Title);

            Assert.That(player.AudioSource.clip, Is.SameAs(titleClip));
            Assert.That(player.AudioSource.timeSamples, Is.LessThan(22050));
        }

        /// <summary>
        /// Title・Battle・Titleの画面遷移でプレイヤーを作り直さず、対応する曲へ切り替えることを保証します。
        /// </summary>
        [UnityTest]
        public IEnumerator ScreenTransition_TitleとBattleを往復_同じプレイヤーで対応曲を再生する()
        {
            var titleClip = AudioClip.Create("TitleBgm", 176400, 1, 44100, false);
            var battleClip = AudioClip.Create("BattleBgm", 176400, 1, 44100, false);
            var player = CreateConfiguredPlayer(titleClip, battleClip);
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
            Assert.That(player.CurrentTrack, Is.EqualTo(BgmTrack.Battle));
            Assert.That(player.AudioSource.clip, Is.SameAs(battleClip));
            Assert.That(
                Object.FindObjectsByType<BgmPlayer>(FindObjectsSortMode.None),
                Has.Length.EqualTo(1)
            );

            player.AudioSource.timeSamples = 22050;
            root.Q<VisualElement>("game-over-dialog").style.display = DisplayStyle.Flex;
            yield return null;
            Click(root.Q<Button>("return-title-button"));
            yield return null;

            Assert.That(
                root.Q<VisualElement>("title-screen").resolvedStyle.display,
                Is.EqualTo(DisplayStyle.Flex)
            );
            Assert.That(player.CurrentTrack, Is.EqualTo(BgmTrack.Title));
            Assert.That(player.AudioSource.clip, Is.SameAs(titleClip));
            Assert.That(player.AudioSource.timeSamples, Is.LessThan(22050));
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
            CreateConfiguredPlayer(null, null);
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
        /// 共通BGMボタンが状態を明記し、連続操作を再生へ即時反映して保存することを保証します。
        /// </summary>
        [UnityTest]
        public IEnumerator BgmButton_4回連続クリック_ONとOFFを明記して再生と設定へ反映する()
        {
            var titleClip = AudioClip.Create("TitleBgm", 176400, 1, 44100, false);
            var battleClip = AudioClip.Create("BattleBgm", 176400, 1, 44100, false);
            var player = CreateConfiguredPlayer(titleClip, battleClip);
            var presentation = new GameObject("Presentation");
            presentation.AddComponent<GamePresentationController>();
            yield return null;

            var button = presentation
                .GetComponent<UIDocument>()
                .rootVisualElement.Q<Button>("bgm-toggle-button");
            Assert.That(button, Is.Not.Null);
            Assert.That(button.text, Is.EqualTo("BGM ON"));
            Assert.That(button.enabledInHierarchy, Is.True);
            Assert.That(button.resolvedStyle.display, Is.Not.EqualTo(DisplayStyle.None));
            Assert.That(button.resolvedStyle.opacity, Is.EqualTo(1f));
            Assert.That(button.worldBound.width, Is.GreaterThan(0f));
            Assert.That(button.worldBound.height, Is.GreaterThan(0f));
            var pickedElement = button.panel.Pick(button.worldBound.center);
            Assert.That(
                pickedElement == button
                    || (pickedElement != null && button.Contains(pickedElement)),
                Is.True
            );
            var onBackgroundColor = button.resolvedStyle.backgroundColor;
            Assert.That(onBackgroundColor.a, Is.EqualTo(1f));
            Assert.That(onBackgroundColor.g, Is.GreaterThan(onBackgroundColor.r));
            Assert.That(onBackgroundColor.g, Is.GreaterThan(onBackgroundColor.b));
            player.AudioSource.timeSamples = 11025;

            Click(button);
            yield return null;
            Assert.That(button.text, Is.EqualTo("BGM OFF"));
            Assert.That(button.enabledInHierarchy, Is.True);
            Assert.That(button.resolvedStyle.opacity, Is.EqualTo(1f));
            var offBackgroundColor = button.resolvedStyle.backgroundColor;
            Assert.That(offBackgroundColor, Is.Not.EqualTo(onBackgroundColor));
            Assert.That(offBackgroundColor.a, Is.EqualTo(1f));
            Assert.That(offBackgroundColor.r, Is.GreaterThan(offBackgroundColor.g));
            Assert.That(offBackgroundColor.g, Is.GreaterThan(offBackgroundColor.b));
            Assert.That(player.AudioSource.isPlaying, Is.False);
            Assert.That(new PlayerPrefsBgmSettingsStore(TestKey).LoadEnabled(), Is.False);
            var firstPausedAt = player.AudioSource.timeSamples;
            Assert.That(firstPausedAt, Is.GreaterThanOrEqualTo(11025));

            Click(button);
            yield return null;
            Assert.That(button.text, Is.EqualTo("BGM ON"));
            Assert.That(button.enabledInHierarchy, Is.True);
            Assert.That(player.AudioSource.isPlaying, Is.True);
            Assert.That(new PlayerPrefsBgmSettingsStore(TestKey).LoadEnabled(), Is.True);
            Assert.That(player.AudioSource.timeSamples, Is.GreaterThanOrEqualTo(firstPausedAt));

            player.AudioSource.timeSamples = 33075;

            Click(button);
            yield return null;
            Assert.That(button.text, Is.EqualTo("BGM OFF"));
            Assert.That(button.enabledInHierarchy, Is.True);
            Assert.That(player.AudioSource.isPlaying, Is.False);
            var secondPausedAt = player.AudioSource.timeSamples;
            Assert.That(secondPausedAt, Is.GreaterThanOrEqualTo(33075));

            Click(button);
            yield return null;
            Assert.That(button.text, Is.EqualTo("BGM ON"));
            Assert.That(button.enabledInHierarchy, Is.True);
            Assert.That(player.AudioSource.isPlaying, Is.True);
            Assert.That(player.AudioSource.timeSamples, Is.GreaterThanOrEqualTo(secondPausedAt));
        }

        /// <summary>
        /// アプリ中断時の一時停止と復帰で再生位置を巻き戻さないことを保証します。
        /// </summary>
        [UnityTest]
        public IEnumerator OnApplicationPause_中断後に復帰_同じ位置から再開する()
        {
            var titleClip = AudioClip.Create("TitleBgm", 88200, 1, 44100, false);
            var battleClip = AudioClip.Create("BattleBgm", 88200, 1, 44100, false);
            var player = CreateConfiguredPlayer(titleClip, battleClip);
            yield return null;
            player.AudioSource.timeSamples = 22050;

            player.HandleApplicationPause(true);
            var pausedAt = player.AudioSource.timeSamples;
            player.HandleApplicationPause(false);

            Assert.That(pausedAt, Is.GreaterThanOrEqualTo(22050));
            Assert.That(player.AudioSource.timeSamples, Is.GreaterThanOrEqualTo(pausedAt));
            Assert.That(player.AudioSource.isPlaying, Is.True);
        }

        private static BgmPlayer CreateConfiguredPlayer(AudioClip titleClip, AudioClip battleClip)
        {
            var gameObject = new GameObject("BgmPlayerTest");
            gameObject.SetActive(false);
            var player = gameObject.AddComponent<BgmPlayer>();
            player.ConfigureForTests(
                new PlayerPrefsBgmSettingsStore(TestKey),
                titleClip,
                battleClip
            );
            gameObject.SetActive(true);
            return player;
        }

        private static void Click(VisualElement element)
        {
            var position = element.worldBound.center;
            var downEvent = new Event
            {
                type = EventType.MouseDown,
                button = 0,
                mousePosition = position,
            };
            using (var pointerDown = PointerDownEvent.GetPooled(downEvent))
            {
                element.SendEvent(pointerDown);
            }

            var upEvent = new Event
            {
                type = EventType.MouseUp,
                button = 0,
                mousePosition = position,
            };
            using (var pointerUp = PointerUpEvent.GetPooled(upEvent))
            {
                element.SendEvent(pointerUp);
            }
            element.ReleasePointer(PointerId.mousePointerId);
        }
    }
}
