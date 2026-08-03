using System.Linq;
using NUnit.Framework;
using UnityEditor;

namespace CoyoteBattle.Domain.Tests
{
    public sealed class BuildConfigurationTests
    {
        /// <summary>
        /// Androidビルドが対象シーンなしで失敗する問題を防ぎ、登録された全シーンが実在することを保証します。
        /// </summary>
        [Test]
        public void EditorBuildSettings_ビルド設定を確認する_有効なシーンが1件以上存在する()
        {
            var enabledScenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).ToArray();

            Assert.That(
                enabledScenes,
                Is.Not.Empty,
                "ビルド対象のシーンを1件以上登録してください。"
            );
            foreach (var scene in enabledScenes)
            {
                var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scene.path);
                var registeredGuid = scene.guid.ToString();
                var assetGuid = AssetDatabase.AssetPathToGUID(scene.path);

                Assert.That(
                    sceneAsset,
                    Is.Not.Null,
                    $"ビルド対象をSceneAssetとして読み込めません: {scene.path}"
                );
                Assert.That(
                    registeredGuid,
                    Is.EqualTo(assetGuid),
                    $"Build SettingsとSceneAssetのGUIDが一致しません: {scene.path}"
                );
            }
        }
    }
}
