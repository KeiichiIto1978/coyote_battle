namespace CoyoteBattle.Domain
{
    /// <summary>
    /// 場の合計値に加算される数字カードです。
    /// </summary>
    public sealed class Card
    {
        /// <summary>
        /// 指定された数値を保持する数字カードを生成します。
        /// </summary>
        /// <param name="value">場の合計値へ加算する値です。正数、負数、ゼロを指定できます。</param>
        public Card(int value)
        {
            Value = value;
        }

        public int Value { get; }
    }
}
