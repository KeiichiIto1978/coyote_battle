using System.Collections;
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
        [UnityTest]
        public IEnumerator Initialize_起動直後_タイトル画面を表示する()
        {
            var gameObject = new GameObject("PresentationTest");
            var controller = gameObject.AddComponent<GamePresentationController>();
            controller.InitializeForTests();
            yield return null;

            var document = gameObject.GetComponent<UIDocument>();
            Assert.That(document.rootVisualElement.Q<VisualElement>("title-screen"), Is.Not.Null);
            Assert.That(document.rootVisualElement.Q<Button>("start-game-button"), Is.Not.Null);
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
            gameObject.AddComponent<GamePresentationController>().InitializeForTests();
            yield return null;

            var renderingCamera = GameObject.Find("CoyoteBattleCamera");
            Assert.That(renderingCamera, Is.Not.Null);
            Assert.That(renderingCamera.GetComponent<Camera>(), Is.Not.Null);
            Object.Destroy(gameObject);
            Object.Destroy(renderingCamera);
        }

        [UnityTest]
        public IEnumerator StartGame_開始ボタン_対戦画面と伏せたユーザーカードを表示する()
        {
            var gameObject = new GameObject("PresentationTest");
            var controller = gameObject.AddComponent<GamePresentationController>();
            controller.InitializeForTests();
            yield return null;

            var root = gameObject.GetComponent<UIDocument>().rootVisualElement;
            controller.StartGameForTests();
            yield return null;

            Assert.That(root.Q<VisualElement>("battle-screen").style.display.value, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(root.Q<Label>("user-card").text, Is.EqualTo("伏せ札"));
            Object.Destroy(gameObject);
        }
    }
}
