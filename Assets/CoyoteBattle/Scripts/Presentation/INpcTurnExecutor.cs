using CoyoteBattle.Application;

namespace CoyoteBattle.Presentation
{
    /// <summary>
    /// PresentationからApplicationへNPCの現在手番を一度だけ送る境界です。
    /// </summary>
    internal interface INpcTurnExecutor
    {
        /// <summary>
        /// 現在NPCの1行動だけをApplicationへ送ります。
        /// </summary>
        /// <param name="game">操作対象のゲーム進行です。</param>
        /// <returns>NPC行動が受理された場合はtrueです。</returns>
        bool TryExecute(GameFlowService game);
    }
}
