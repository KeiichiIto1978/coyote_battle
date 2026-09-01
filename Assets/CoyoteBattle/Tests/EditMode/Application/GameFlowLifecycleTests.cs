using System.Linq;
using CoyoteBattle.Domain;
using NUnit.Framework;
using static CoyoteBattle.Application.Tests.GameFlowTestHelper;

namespace CoyoteBattle.Application.Tests
{
    public sealed class GameFlowLifecycleTests
    {
        /// <summary>
        /// ユーザーがライフ0になった時点で敗北し、通常操作を停止することを保証します。
        /// </summary>
        [Test]
        public void TryDeclareCoyote_ユーザーが3回敗北する_ユーザー敗北でゲームを終了する()
        {
            var service = StartService();

            LoseParticipantForRounds(service, "user", 3);

            Assert.That(service.State, Is.EqualTo(GameFlowState.GameOver));
            Assert.That(service.Outcome, Is.EqualTo(GameOutcome.UserDefeat));
            Assert.That(service.TryStartNextRound(), Is.False);
            Assert.That(service.TryDeclareNumber("npc-1", 1), Is.False);
        }

        /// <summary>
        /// NPC4名が全員脱落するとユーザー勝利になることを保証します。
        /// </summary>
        [Test]
        public void TryDeclareCoyote_NPC全員が3回敗北する_ユーザー勝利でゲームを終了する()
        {
            var service = StartService();

            foreach (var npcId in ParticipantIds.Skip(1))
            {
                LoseParticipantForRounds(service, npcId, 3);
            }

            Assert.That(service.State, Is.EqualTo(GameFlowState.GameOver));
            Assert.That(service.Outcome, Is.EqualTo(GameOutcome.UserVictory));
            Assert.That(
                service
                    .Participants.Where(item => item.Kind == ParticipantKind.Npc)
                    .All(item => item.IsEliminated),
                Is.True
            );
        }

        /// <summary>
        /// ゲーム終了後の再戦で参加者、ラウンド、宣言、結果、勝敗を完全初期化することを保証します。
        /// </summary>
        [Test]
        public void TryStartNewGame_ゲーム終了後に再戦する_ゲーム状態を初期化する()
        {
            var service = StartService();
            LoseParticipantForRounds(service, "user", 3);

            var succeeded = service.TryStartNewGame();

            Assert.That(succeeded, Is.True);
            Assert.That(service.State, Is.EqualTo(GameFlowState.Declaring));
            Assert.That(service.Outcome, Is.EqualTo(GameOutcome.None));
            Assert.That(service.RoundNumber, Is.EqualTo(1));
            Assert.That(
                service.Participants.All(item => item.Life == 3 && !item.IsEliminated),
                Is.True
            );
            Assert.That(service.DeclarationHistory, Is.Empty);
            Assert.That(service.LastRoundResult, Is.Null);
        }

        /// <summary>
        /// タイトル復帰で進行状態を破棄し、重複復帰を拒否することを保証します。
        /// </summary>
        [Test]
        public void TryReturnToTitle_宣言中から2回実行する_初回だけ状態を破棄する()
        {
            var service = StartService();
            service.TryDeclareNumber("user", 1);

            var firstSucceeded = service.TryReturnToTitle();
            var secondSucceeded = service.TryReturnToTitle();

            Assert.That(firstSucceeded, Is.True);
            Assert.That(secondSucceeded, Is.False);
            Assert.That(service.State, Is.EqualTo(GameFlowState.NoGame));
            Assert.That(service.RoundNumber, Is.EqualTo(0));
            Assert.That(service.Participants, Is.Empty);
            Assert.That(service.CurrentCards, Is.Empty);
            Assert.That(service.DeclarationHistory, Is.Empty);
            Assert.That(service.LastRoundResult, Is.Null);
        }

        /// <summary>
        /// 判定結果表示中とゲーム終了後のどちらからでもタイトルへ戻れることを保証します。
        /// </summary>
        [TestCase(1)]
        [TestCase(3)]
        public void TryReturnToTitle_判定後に実行する_全ゲーム状態を破棄する(int userLossCount)
        {
            var service = StartService();
            LoseParticipantForRounds(service, "user", userLossCount);

            var succeeded = service.TryReturnToTitle();

            Assert.That(succeeded, Is.True);
            Assert.That(service.State, Is.EqualTo(GameFlowState.NoGame));
            Assert.That(service.Outcome, Is.EqualTo(GameOutcome.None));
            Assert.That(service.Participants, Is.Empty);
            Assert.That(service.LastRoundResult, Is.Null);
        }

        /// <summary>
        /// 進行中の新規ゲーム開始を拒否し、誤操作による状態消失を防ぐことを保証します。
        /// </summary>
        [Test]
        public void TryStartNewGame_宣言中に再度実行する_状態を変えず拒否する()
        {
            var service = StartService();
            service.TryDeclareNumber("user", 10);

            var succeeded = service.TryStartNewGame();

            Assert.That(succeeded, Is.False);
            Assert.That(service.RoundNumber, Is.EqualTo(1));
            Assert.That(service.CurrentParticipantId, Is.EqualTo("npc-1"));
            Assert.That(service.DeclarationHistory, Has.Count.EqualTo(1));
        }

        /// <summary>
        /// ラウンド結果表示中の新規ゲーム開始を拒否し、結果確認中の誤消去を防ぐことを保証します。
        /// </summary>
        [Test]
        public void TryStartNewGame_ラウンド結果表示中に実行する_結果を維持して拒否する()
        {
            var service = StartService();
            LoseCurrentParticipant(service);
            var result = service.LastRoundResult;

            var succeeded = service.TryStartNewGame();

            Assert.That(succeeded, Is.False);
            Assert.That(service.State, Is.EqualTo(GameFlowState.RoundResult));
            Assert.That(service.LastRoundResult, Is.SameAs(result));
        }
    }
}
