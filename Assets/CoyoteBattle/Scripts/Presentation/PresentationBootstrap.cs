using UnityEngine;
using UnityEngine.SceneManagement;

namespace CoyoteBattle.Presentation
{
    /// <summary>
    /// 空のBootstrapシーンへPresentationのComposition Rootを生成します。
    /// </summary>
    public static class PresentationBootstrap
    {
        /// <summary>
        /// Bootstrapシーン読込後にUIコントローラーを1つだけ生成します。
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            if (
                SceneManager.GetActiveScene().name != "Bootstrap"
                || Object.FindFirstObjectByType<GamePresentationController>() != null
            )
            {
                return;
            }

            new GameObject("CoyoteBattlePresentation").AddComponent<GamePresentationController>();
        }
    }
}
