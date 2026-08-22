using System.Collections;
using System.Linq;
using CoyoteBattle.Application;

namespace CoyoteBattle.Presentation
{
    /// <summary>
    /// NPCの思考待機、行動表示、連続手番のPresentation進行を管理します。
    /// </summary>
    public sealed partial class GamePresentationController
    {
        private const float NpcThinkingSeconds = 0.8f;
        private const float NpcActionSeconds = 1f;

        /// <summary>
        /// NPC手番なら、未実行の連続進行Coroutineを1つだけ開始します。
        /// </summary>
        private void ContinueNpcTurns()
        {
            if (
                _npcTurnsCoroutine != null
                || _game.State != GameFlowState.Declaring
                || _game.CurrentParticipantId == UserId
            )
            {
                return;
            }

            _isNpcSequenceRunning = true;
            SetInputEnabled(false);
            _npcTurnsCoroutine = StartCoroutine(ExecuteNpcTurns(_operationGeneration));
        }

        /// <summary>
        /// 各NPCの思考、1行動、行動表示を順序どおり実行し、ユーザー手番か結果で停止します。
        /// </summary>
        /// <param name="generation">開始時点の保留処理世代です。</param>
        /// <returns>Unity Coroutineとして進める列挙子です。</returns>
        private IEnumerator ExecuteNpcTurns(int generation)
        {
            var actionRejected = false;
            while (IsCurrentNpcTurn(generation))
            {
                var actorId = _game.CurrentParticipantId;
                _statusLabel.text = $"{PresentationText.ParticipantName(actorId)} が考え中…";
                SetInputEnabled(false);
                yield return _presentationDelay.Wait(NpcThinkingSeconds);
                if (generation != _operationGeneration)
                {
                    yield break;
                }

                if (!_npcTurnExecutor.TryExecute(_game))
                {
                    actionRejected = true;
                    _statusLabel.text =
                        $"{PresentationText.ParticipantName(actorId)} の行動を完了できませんでした";
                    break;
                }

                _actionBanner.text = CreateNpcActionText(actorId);
                if (_game.State == GameFlowState.Declaring)
                {
                    RefreshBattle();
                    SetInputEnabled(false);
                }

                yield return _presentationDelay.Wait(NpcActionSeconds);
            }

            if (generation != _operationGeneration)
            {
                yield break;
            }

            _npcTurnsCoroutine = null;
            _isNpcSequenceRunning = false;
            if (actionRejected)
            {
                SetInputEnabled(false);
            }
            else if (_game.State == GameFlowState.Declaring)
            {
                RefreshBattle();
            }
            else
            {
                ShowResult();
            }
        }

        /// <summary>
        /// 指定世代が有効で、現在手番がNPCである場合だけtrueを返します。
        /// </summary>
        /// <param name="generation">確認対象の保留処理世代です。</param>
        /// <returns>NPC進行を継続できる場合はtrueです。</returns>
        private bool IsCurrentNpcTurn(int generation)
        {
            return generation == _operationGeneration
                && _game.State == GameFlowState.Declaring
                && _game.CurrentParticipantId != UserId;
        }

        /// <summary>
        /// 実行直後のApplication状態から数字宣言またはコヨーテの行動文を生成します。
        /// </summary>
        /// <param name="actorId">行動したNPC識別子です。</param>
        /// <returns>中央バナーへ表示する最新1手です。</returns>
        private string CreateNpcActionText(string actorId)
        {
            var actorName = PresentationText.ParticipantName(actorId);
            var lastDeclaration = _game.DeclarationHistory.LastOrDefault();
            if (lastDeclaration != null && lastDeclaration.ParticipantId == actorId)
            {
                return $"{actorName}：{lastDeclaration.Value}を宣言！";
            }

            if (_game.LastRoundResult != null && _game.LastRoundResult.CoyoteDeclarerId == actorId)
            {
                return $"{actorName}：コヨーテ！";
            }

            return $"{actorName}：行動完了";
        }

        /// <summary>
        /// 画面遷移や破棄時に待機中Coroutineを停止し、古い世代からの状態変更を無効化します。
        /// </summary>
        private void CancelPendingOperations()
        {
            _operationGeneration++;
            if (_npcTurnsCoroutine != null)
            {
                StopCoroutine(_npcTurnsCoroutine);
                _npcTurnsCoroutine = null;
            }

            _isNpcSequenceRunning = false;
        }
    }
}
