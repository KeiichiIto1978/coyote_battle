namespace CoyoteBattle.Domain
{
    /// <summary>
    /// 山札のシャッフルに使用する差し替え可能な乱数源です。
    /// </summary>
    public interface IRandomSource
    {
        /// <summary>
        /// 0以上かつ指定された排他的上限未満の整数を返します。
        /// </summary>
        /// <param name="exclusiveUpperBound">1以上の排他的上限です。</param>
        /// <returns>0以上、排他的上限未満の整数です。</returns>
        int Next(int exclusiveUpperBound);
    }
}
