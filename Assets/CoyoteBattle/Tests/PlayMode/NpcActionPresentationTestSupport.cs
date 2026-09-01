using System;
using System.Collections;
using System.Collections.Generic;
using CoyoteBattle.Application;
using CoyoteBattle.Domain;
using CoyoteBattle.Presentation;
using UnityEngine;
using UnityEngine.UIElements;

namespace CoyoteBattle.Tests.Presentation
{
    /// <summary>
    /// NPC行動表示テスト用の制御可能な待機、実行方法、ゲーム生成を提供します。
    /// </summary>
    internal static class NpcActionPresentationTestSupport
    {
        /// <summary>
        /// 指定した開始者、待機方法、NPC実行方法でテスト対象を生成します。
        /// </summary>
        internal static ControllerSetup CreateController(
            int starterIndex,
            IPresentationDelay delay,
            INpcTurnExecutor executor
        )
        {
            var gameObject = new GameObject("NpcActionPresentationTest");
            gameObject.SetActive(false);
            var controller = gameObject.AddComponent<GamePresentationController>();
            controller.ConfigureForTests(
                () =>
                    new GameFlowService(
                        new StarterRandomSource(starterIndex),
                        new StarterRandomSource(0)
                    ),
                delay,
                executor
            );
            gameObject.SetActive(true);
            return new ControllerSetup(
                gameObject,
                gameObject.GetComponent<UIDocument>().rootVisualElement
            );
        }

        internal sealed class ControllerSetup
        {
            /// <summary>
            /// 破棄対象と問い合わせ用UIルートを保持します。
            /// </summary>
            public ControllerSetup(GameObject gameObject, VisualElement root)
            {
                GameObject = gameObject;
                Root = root;
            }

            public GameObject GameObject { get; }

            public VisualElement Root { get; }
        }

        internal sealed class ManualPresentationDelay : IPresentationDelay
        {
            private readonly List<DelayRequest> _requests = new List<DelayRequest>();

            public IReadOnlyList<float> Seconds => _requests.ConvertAll(item => item.Seconds);

            /// <summary>
            /// 呼び出し側が解放するまで指定秒数の待機要求を保留します。
            /// </summary>
            public IEnumerator Wait(float seconds)
            {
                var request = new DelayRequest(seconds);
                _requests.Add(request);
                while (!request.IsReleased)
                {
                    yield return null;
                }
            }

            /// <summary>
            /// 指定位置の待機要求だけを解放します。
            /// </summary>
            public void Release(int index)
            {
                _requests[index].IsReleased = true;
            }
        }

        internal sealed class SequentialNumberExecutor : INpcTurnExecutor
        {
            public int CallCount { get; private set; }

            /// <summary>
            /// 現在NPCが1、2、3、4の順で数字を宣言します。
            /// </summary>
            public bool TryExecute(GameFlowService game)
            {
                CallCount++;
                return game.TryDeclareNumber(game.CurrentParticipantId, CallCount);
            }
        }

        internal sealed class CoyoteExecutor : INpcTurnExecutor
        {
            /// <summary>
            /// 現在NPCからコヨーテ宣言を1回Applicationへ送ります。
            /// </summary>
            public bool TryExecute(GameFlowService game)
            {
                return game.TryDeclareCoyote(game.CurrentParticipantId);
            }
        }

        internal sealed class RejectingExecutor : INpcTurnExecutor
        {
            public int CallCount { get; private set; }

            /// <summary>
            /// 呼び出し回数だけ記録し、Application境界での拒否を再現します。
            /// </summary>
            public bool TryExecute(GameFlowService game)
            {
                CallCount++;
                return false;
            }
        }

        private sealed class DelayRequest
        {
            /// <summary>
            /// 待機秒数と解放状態を初期化します。
            /// </summary>
            public DelayRequest(float seconds)
            {
                Seconds = seconds;
            }

            public float Seconds { get; }

            public bool IsReleased { get; set; }
        }

        private sealed class StarterRandomSource : IRandomSource
        {
            private readonly int _starterIndex;
            private bool _starterReturned;

            /// <summary>
            /// 最初の乱数だけ開始者位置を返し、その後は決定的な0を返します。
            /// </summary>
            public StarterRandomSource(int starterIndex)
            {
                _starterIndex = starterIndex;
            }

            /// <summary>
            /// 要求範囲内で開始者位置または0を返します。
            /// </summary>
            public int Next(int maxExclusive)
            {
                if (!_starterReturned)
                {
                    _starterReturned = true;
                    return Math.Min(_starterIndex, maxExclusive - 1);
                }

                return 0;
            }
        }
    }
}
