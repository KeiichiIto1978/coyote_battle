using UnityEngine;

namespace CoyoteBattle.Presentation
{
    /// <summary>
    /// UI ToolkitだけのシーンでもGameビューを正常に描画済みとするCameraを用意します。
    /// </summary>
    internal static class PresentationRenderingCamera
    {
        /// <summary>
        /// Cameraが存在しない場合は背景クリア専用Cameraを生成し、AudioListenerを1つだけ確保します。
        /// </summary>
        internal static void EnsureExists()
        {
            var camera = Camera.main;
            if (camera == null)
            {
                camera = Object.FindFirstObjectByType<Camera>();
            }

            if (camera == null)
            {
                var gameObject = new GameObject("CoyoteBattleCamera");
                gameObject.tag = "MainCamera";
                camera = gameObject.AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.black;
                camera.cullingMask = 0;
                camera.depth = -100;
                camera.useOcclusionCulling = false;
            }

            var primaryListener = camera.GetComponent<AudioListener>();
            if (primaryListener == null)
            {
                primaryListener = camera.gameObject.AddComponent<AudioListener>();
            }

            primaryListener.enabled = true;
            var listeners = Object.FindObjectsByType<AudioListener>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );
            foreach (var listener in listeners)
            {
                if (listener != primaryListener)
                {
                    listener.enabled = false;
                }
            }
        }
    }
}
