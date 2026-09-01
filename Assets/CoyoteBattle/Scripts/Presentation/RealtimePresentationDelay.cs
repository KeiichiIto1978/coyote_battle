using System;
using System.Collections;
using UnityEngine;

namespace CoyoteBattle.Presentation
{
    /// <summary>
    /// Unityの実時間を使ってPresentationの表示を保持します。
    /// </summary>
    internal sealed class RealtimePresentationDelay : IPresentationDelay
    {
        /// <summary>
        /// 指定秒数をUnityのCoroutine上で待機します。
        /// </summary>
        /// <param name="seconds">待機する0以上の秒数です。</param>
        /// <returns>Unityへ渡す待機Enumeratorです。</returns>
        public IEnumerator Wait(float seconds)
        {
            if (seconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(seconds));
            }

            yield return new WaitForSeconds(seconds);
        }
    }
}
