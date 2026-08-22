using System.Collections;

namespace CoyoteBattle.Presentation
{
    /// <summary>
    /// Presentationの時間待機を本番時間とテスト制御へ差し替える境界です。
    /// </summary>
    internal interface IPresentationDelay
    {
        /// <summary>
        /// 指定秒数が経過するまで呼び出し元Coroutineを待機させます。
        /// </summary>
        /// <param name="seconds">待機する0以上の秒数です。</param>
        /// <returns>待機処理を表すEnumeratorです。</returns>
        IEnumerator Wait(float seconds);
    }
}
