using System.Collections;
using CoyoteBattle.Application;
using CoyoteBattle.Domain;
using CoyoteBattle.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace CoyoteBattle.Tests.Presentation
{
    /// <summary>
    /// 空のBootstrapシーンから操作可能なUIが構成されることを保証します。
    /// </summary>
    public sealed class PresentationBootstrapTests
    {
        /// <summary>
        /// テスト間でコントローラーや表示用カメラが残らないよう破棄します。
        /// </summary>
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (
                var controller in Object.FindObjectsByType<GamePresentationController>(
                    FindObjectsSortMode.None
                )
            )
            {
                Object.Destroy(controller.gameObject);
            }
            foreach (var camera in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
            {
                if (camera.name == "CoyoteBattleCamera")
                {
                    Object.Destroy(camera.gameObject);
                }
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator Initialize_起動直後_タイトル画面を表示する()
        {
            var gameObject = new GameObject("PresentationTest");
            gameObject.AddComponent<GamePresentationController>();
            yield return null;

            var document = gameObject.GetComponent<UIDocument>();
            Assert.That(document.panelSettings.themeStyleSheet, Is.Not.Null);
            Assert.That(document.rootVisualElement.Q<VisualElement>("title-screen"), Is.Not.Null);
            Assert.That(document.rootVisualElement.Q<Button>("start-game-button"), Is.Not.Null);
            var title = document.rootVisualElement.Q<Label>("title-label");
            Assert.That(title.resolvedStyle.unityFontDefinition.font, Is.Not.Null);
            Assert.That(
                title.resolvedStyle.unityFontDefinition.font.name,
                Does.Contain("NotoSansJP")
            );
            Object.Destroy(gameObject);
        }

        [UnityTest]
        public IEnumerator Initialize_Cameraがない_NoCamerasRendering警告を防ぐCameraを生成する()
        {
            var existingCamera = Camera.main;
            if (existingCamera != null)
            {
                Object.Destroy(existingCamera.gameObject);
                yield return null;
            }

            var gameObject = new GameObject("PresentationCameraTest");
            gameObject.AddComponent<GamePresentationController>();
            yield return null;

            var renderingCamera = GameObject.Find("CoyoteBattleCamera");
            Assert.That(renderingCamera, Is.Not.Null);
            Assert.That(renderingCamera.GetComponent<Camera>(), Is.Not.Null);
            Assert.That(renderingCamera.GetComponent<AudioListener>(), Is.Not.Null);
            Assert.That(
                Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None),
                Has.Length.EqualTo(1)
            );
            Object.Destroy(gameObject);
            Object.Destroy(renderingCamera);
        }

        /// <summary>
        /// 既存Listenerが無効・非アクティブ・複数でも、表示用Cameraの1つだけを有効にすることを保証します。
        /// </summary>
        [UnityTest]
        public IEnumerator EnsureExists_AudioListenerが無効か複数_表示用Cameraの1つだけを有効にする()
        {
            var cameraObject = new GameObject("ExistingMainCamera");
            cameraObject.tag = "MainCamera";
            cameraObject.AddComponent<Camera>();
            var primaryListener = cameraObject.AddComponent<AudioListener>();
            primaryListener.enabled = false;

            var extraObject = new GameObject("ExtraAudioListener");
            var extraListener = extraObject.AddComponent<AudioListener>();
            var inactiveObject = new GameObject("InactiveAudioListener");
            var inactiveListener = inactiveObject.AddComponent<AudioListener>();
            inactiveObject.SetActive(false);

            PresentationRenderingCamera.EnsureExists();
            yield return null;

            Assert.That(primaryListener.enabled, Is.True);
            Assert.That(extraListener.enabled, Is.False);
            Assert.That(inactiveListener.enabled, Is.False);
            var enabledListenerCount = 0;
            foreach (
                var listener in Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None)
            )
            {
                if (listener.enabled)
                {
                    enabledListenerCount++;
                }
            }
            Assert.That(enabledListenerCount, Is.EqualTo(1));

            Object.Destroy(cameraObject);
            Object.Destroy(extraObject);
            Object.Destroy(inactiveObject);
        }

        [UnityTest]
        public IEnumerator DeclareNumber_ユーザー入力_結果表示から次ラウンドへ遷移する()
        {
            var gameObject = CreateUserStarterController();
            var controller = gameObject.AddComponent<GamePresentationController>();
            controller.ConfigureForTests(CreateDeterministicGame);
            gameObject.SetActive(true);
            yield return null;

            var root = gameObject.GetComponent<UIDocument>().rootVisualElement;
            Click(root.Q<Button>("start-game-button"));
            yield return null;
            Assert.That(
                root.Q<VisualElement>("battle-screen").style.display.value,
                Is.EqualTo(DisplayStyle.Flex)
            );
            Assert.That(root.Q<Label>("user-card").text, Is.EqualTo("伏せ札"));
            Assert.That(root.Q<VisualElement>("npc-row").childCount, Is.EqualTo(4));
            Assert.That(
                root.Q<Label>("status-label").resolvedStyle.unityFontDefinition.font.name,
                Does.Contain("NotoSansJP")
            );

            root.Q<TextField>("number-input").value = "0";
            Click(root.Q<Button>("declare-number-button"));
            yield return null;
            Assert.That(root.Q<Label>("input-error").text, Does.Contain("1以上"));
            Assert.That(root.Q<Label>("status-label").text, Is.EqualTo("あなたの手番"));

            root.Q<TextField>("number-input").value = "1";
            Click(root.Q<Button>("declare-number-button"));
            yield return null;
            Assert.That(root.Q<Label>("declaration-label").text, Does.Contain("1"));

            var timeoutAt = Time.realtimeSinceStartup + 8f;
            while (
                root.Q<VisualElement>("round-result-screen").style.display.value
                    != DisplayStyle.Flex
                && Time.realtimeSinceStartup < timeoutAt
            )
            {
                var coyote = root.Q<Button>("declare-coyote-button");
                if (coyote.enabledSelf)
                {
                    Click(coyote);
                }
                yield return null;
            }

            Assert.That(
                root.Q<VisualElement>("round-result-screen").style.display.value,
                Is.EqualTo(DisplayStyle.Flex)
            );
            Assert.That(root.Q<Label>("result-total").text, Does.Contain("実合計"));
            Click(root.Q<Button>("next-round-button"));
            yield return null;
            Assert.That(
                root.Q<VisualElement>("battle-screen").style.display.value,
                Is.EqualTo(DisplayStyle.Flex)
            );
            Object.Destroy(gameObject);
        }

        private static GameObject CreateUserStarterController()
        {
            var gameObject = new GameObject("PresentationTest");
            gameObject.SetActive(false);
            return gameObject;
        }

        private static GameFlowService CreateDeterministicGame()
        {
            return new GameFlowService(new ZeroRandomSource(), new ZeroRandomSource());
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

        private sealed class ZeroRandomSource : IRandomSource
        {
            public int Next(int maxExclusive)
            {
                return 0;
            }
        }
    }
}
