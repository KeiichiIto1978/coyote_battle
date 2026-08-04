using System;

namespace CoyoteBattle.Domain
{
    /// <summary>
    /// 特殊効果を解決した場の合計値と、ラウンド終了処理に必要な情報です。
    /// </summary>
    public sealed class FieldTotalResult
    {
        /// <summary>
        /// 合計値計算の結果を生成します。
        /// </summary>
        /// <param name="total">特殊効果適用後の合計値です。</param>
        /// <param name="additionalCard">「？」で引いたカードです。引いていない場合はnullです。</param>
        /// <param name="shouldRebuildDeck">夜カードにより全山札再構築が必要かを示します。</param>
        internal FieldTotalResult(int total, Card additionalCard, bool shouldRebuildDeck)
        {
            if (additionalCard != null && additionalCard.Kind == CardKind.Mystery)
            {
                throw new ArgumentException(
                    "追加札に別の「？」は指定できません。",
                    nameof(additionalCard)
                );
            }

            Total = total;
            AdditionalCard = additionalCard;
            ShouldRebuildDeck = shouldRebuildDeck;
        }

        /// <summary>
        /// 特殊効果適用後の合計値を取得します。
        /// </summary>
        public int Total { get; }

        /// <summary>
        /// 「？」で引いた追加札を取得します。引いていない場合はnullです。
        /// </summary>
        public Card AdditionalCard { get; }

        /// <summary>
        /// 夜カードによる全山札再構築が必要かを取得します。
        /// </summary>
        public bool ShouldRebuildDeck { get; }
    }
}
