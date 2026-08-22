using System;
using CoyoteBattle.Application;

namespace CoyoteBattle.Presentation
{
    /// <summary>
    /// 既存のApplication APIへ現在NPCの行動を委譲します。
    /// </summary>
    internal sealed class ApplicationNpcTurnExecutor : INpcTurnExecutor
    {
        /// <summary>
        /// 現在NPCの観測、判断、宣言を一度だけ実行します。
        /// </summary>
        /// <param name="game">操作対象のゲーム進行です。</param>
        /// <returns>NPC行動が受理された場合はtrueです。</returns>
        public bool TryExecute(GameFlowService game)
        {
            if (game == null)
            {
                throw new ArgumentNullException(nameof(game));
            }

            return game.TryExecuteCurrentNpcTurn();
        }
    }
}
