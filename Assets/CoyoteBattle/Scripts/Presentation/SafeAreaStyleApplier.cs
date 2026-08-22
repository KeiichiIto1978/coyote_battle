using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace CoyoteBattle.Presentation
{
    /// <summary>
    /// 端末のSafe Areaを基準解像度のUI余白へ反映します。
    /// </summary>
    internal static class SafeAreaStyleApplier
    {
        /// <summary>
        /// 画面寸法とSafe Areaから算出した余白をUIルートへ設定します。
        /// </summary>
        /// <param name="root">余白を適用するUIルートです。</param>
        /// <param name="screenWidth">端末画面のピクセル幅です。</param>
        /// <param name="screenHeight">端末画面のピクセル高さです。</param>
        /// <param name="safeArea">端末画面上のSafe Areaです。</param>
        internal static void Apply(
            VisualElement root,
            int screenWidth,
            int screenHeight,
            Rect safeArea
        )
        {
            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            var padding = SafeAreaPaddingCalculator.Calculate(
                Mathf.Max(1, screenWidth),
                Mathf.Max(1, screenHeight),
                safeArea,
                new Vector2(1920, 1080)
            );
            root.style.paddingLeft = padding.x;
            root.style.paddingTop = padding.y;
            root.style.paddingRight = padding.z;
            root.style.paddingBottom = padding.w;
        }
    }
}
