using System;
using UnityEngine;

namespace CoyoteBattle.Presentation
{
    /// <summary>
    /// 端末のSafe Area外側をUI Toolkit基準解像度の余白へ変換します。
    /// </summary>
    public static class SafeAreaPaddingCalculator
    {
        /// <summary>
        /// 左、上、右、下の順で基準解像度上の余白を返します。
        /// </summary>
        /// <param name="screenWidth">端末画面のピクセル幅です。</param>
        /// <param name="screenHeight">端末画面のピクセル高さです。</param>
        /// <param name="safeArea">端末ピクセル座標のSafe Areaです。</param>
        /// <param name="referenceResolution">UI Toolkitの基準解像度です。</param>
        /// <returns>左、上、右、下の余白です。</returns>
        public static Vector4 Calculate(
            int screenWidth,
            int screenHeight,
            Rect safeArea,
            Vector2 referenceResolution
        )
        {
            if (screenWidth <= 0 || screenHeight <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(screenWidth));
            }

            var widthScale = referenceResolution.x / screenWidth;
            var heightScale = referenceResolution.y / screenHeight;
            return new Vector4(
                safeArea.xMin * widthScale,
                (screenHeight - safeArea.yMax) * heightScale,
                (screenWidth - safeArea.xMax) * widthScale,
                safeArea.yMin * heightScale
            );
        }
    }
}
