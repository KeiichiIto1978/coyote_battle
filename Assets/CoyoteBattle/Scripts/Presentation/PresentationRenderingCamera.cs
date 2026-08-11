using UnityEngine;

namespace CoyoteBattle.Presentation
{
    /// <summary>
    /// UI ToolkitだけのシーンでもGameビューを正常に描画済みとするCameraを用意します。
    /// </summary>
    internal static class PresentationRenderingCamera
    {
        /// <summary>
        /// Cameraが存在しない場合だけ、背景クリア専用Cameraを生成します。
        /// </summary>
        internal static void EnsureExists()
        {
            if (Camera.allCamerasCount > 0)
            {
                return;
            }

            var gameObject = new GameObject("CoyoteBattleCamera");
            gameObject.tag = "MainCamera";
            var camera = gameObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.cullingMask = 0;
            camera.depth = -100;
            camera.useOcclusionCulling = false;
        }
    }
}
