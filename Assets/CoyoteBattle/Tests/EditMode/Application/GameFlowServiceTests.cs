using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using static CoyoteBattle.Application.Tests.GameFlowTestHelper;

namespace CoyoteBattle.Application.Tests
{
    public sealed class GameFlowServiceTests
    {
        /// <summary>
        /// 固定5名を生成し、乱数で選んだ開始者から配布・宣言を開始することを保証します。
        /// </summary>
        [Test]
        public void TryStartNewGame_NoGameで開始する_固定5名の第1ラウンドを開始する()
        {
            var service = CreateService();

            var succeeded = service.TryStartNewGame();

            Assert.That(succeeded, Is.True);
            Assert.That(service.State, Is.EqualTo(GameFlowState.Declaring));
            Assert.That(service.RoundNumber, Is.EqualTo(1));
            Assert.That(service.StartingParticipantId, Is.EqualTo("user"));
            Assert.That(service.CurrentParticipantId, Is.EqualTo("user"));
            Assert.That(service.Participants.Select(item => item.Id), Is.EqualTo(ParticipantIds));
            Assert.That(service.Participants.All(item => item.Life == 3), Is.True);
            Assert.That(
                service.CurrentCards.Select(item => item.ParticipantId),
                Is.EqualTo(ParticipantIds)
            );
        }

        /// <summary>
        /// 固定リングの開始位置だけを乱数で変え、配布順と現在手番を同じ開始者へ揃えることを保証します。
        /// </summary>
        [Test]
        public void TryStartNewGame_開始位置3を返す乱数を使う_npc3からリング順に配布する()
        {
            var service = new GameFlowService(new FirstValueThenZeroRandomSource(3));

            service.TryStartNewGame();

            Assert.That(service.StartingParticipantId, Is.EqualTo("npc-3"));
            Assert.That(service.CurrentParticipantId, Is.EqualTo("npc-3"));
            Assert.That(
                service.CurrentCards.Select(item => item.ParticipantId),
                Is.EqualTo(new[] { "npc-3", "npc-4", "user", "npc-1", "npc-2" })
            );
        }

        /// <summary>
        /// 開始位置の乱数契約違反を例外で通知し、不完全なゲーム状態を公開しないことを保証します。
        /// </summary>
        [Test]
        public void TryStartNewGame_開始位置5を返す乱数を使う_NoGameのまま入力契約違反になる()
        {
            var service = new GameFlowService(new FirstValueThenZeroRandomSource(5));

            Assert.That(() => service.TryStartNewGame(), Throws.InvalidOperationException);
            Assert.That(service.State, Is.EqualTo(GameFlowState.NoGame));
            Assert.That(service.Participants, Is.Empty);
            Assert.That(service.CurrentCards, Is.Empty);
        }

        /// <summary>
        /// 宣言中はユーザー自身のカードだけを伏せ、NPCカードは公開することを保証します。
        /// </summary>
        [Test]
        public void CurrentCards_宣言中である_ユーザーのカードだけを伏せる()
        {
            var service = StartService();

            var userCard = service.CurrentCards.Single(item => item.ParticipantId == "user");
            var npcCards = service.CurrentCards.Where(item => item.ParticipantId != "user");

            Assert.That(userCard.IsHidden, Is.True);
            Assert.That(userCard.Card, Is.Null);
            Assert.That(npcCards.All(item => !item.IsHidden && item.Card != null), Is.True);
        }

        /// <summary>
        /// 有効な数字宣言だけが手番を固定リング上の次参加者へ進めることを保証します。
        /// </summary>
        [Test]
        public void TryDeclareNumber_有効な現在手番と無効な別手番を指定する_成功時だけ手番を進める()
        {
            var service = StartService();

            var wrongTurnSucceeded = service.TryDeclareNumber("npc-1", 1);
            var validSucceeded = service.TryDeclareNumber("user", 1);

            Assert.That(wrongTurnSucceeded, Is.False);
            Assert.That(validSucceeded, Is.True);
            Assert.That(service.CurrentParticipantId, Is.EqualTo("npc-1"));
            Assert.That(service.DeclarationHistory, Has.Count.EqualTo(1));
        }

        /// <summary>
        /// 初手のコヨーテを拒否し、数字宣言後の次手では受理することを保証します。
        /// </summary>
        [Test]
        public void TryDeclareCoyote_初手と数字宣言後に実行する_初手だけを拒否する()
        {
            var service = StartService();

            var firstSucceeded = service.TryDeclareCoyote("user");
            service.TryDeclareNumber("user", 1);
            var secondSucceeded = service.TryDeclareCoyote("npc-1");

            Assert.That(firstSucceeded, Is.False);
            Assert.That(secondSucceeded, Is.True);
            Assert.That(service.State, Is.EqualTo(GameFlowState.RoundResult));
        }

        /// <summary>
        /// int上限の宣言後は数字を拒否してコヨーテだけを受理し、敗者へ一度だけ反映することを保証します。
        /// </summary>
        [Test]
        public void TryDeclareCoyote_int上限宣言後に実行する_宣言者だけがライフを失う()
        {
            var service = StartService();
            service.TryDeclareNumber("user", int.MaxValue);

            var numberSucceeded = service.TryDeclareNumber("npc-1", int.MaxValue);
            var coyoteSucceeded = service.TryDeclareCoyote("npc-1");
            var repeatedSucceeded = service.TryDeclareCoyote("npc-1");

            Assert.That(numberSucceeded, Is.False);
            Assert.That(coyoteSucceeded, Is.True);
            Assert.That(repeatedSucceeded, Is.False);
            Assert.That(service.Participants.Single(item => item.Id == "user").Life, Is.EqualTo(2));
            Assert.That(
                service.Participants.Single(item => item.Id == "npc-1").Life,
                Is.EqualTo(3)
            );
        }

        /// <summary>
        /// 回収後も全カードと判定情報を変更不能な結果として保持することを保証します。
        /// </summary>
        [Test]
        public void LastRoundResult_コヨーテ判定を完了する_全カードと結果を保持する()
        {
            var service = StartService();
            service.TryDeclareNumber("user", int.MaxValue);

            service.TryDeclareCoyote("npc-1");

            Assert.That(service.LastRoundResult, Is.Not.Null);
            Assert.That(service.LastRoundResult.DealtCards, Has.Count.EqualTo(5));
            Assert.That(
                service.LastRoundResult.DealtCards.All(item => !item.IsHidden && item.Card != null),
                Is.True
            );
            Assert.That(service.LastRoundResult.NumberDeclarerId, Is.EqualTo("user"));
            Assert.That(service.LastRoundResult.DeclaredNumber, Is.EqualTo(int.MaxValue));
            Assert.That(service.LastRoundResult.CoyoteDeclarerId, Is.EqualTo("npc-1"));
            Assert.That(service.LastRoundResult.LoserId, Is.EqualTo("user"));
            Assert.That(service.CurrentCards, Is.Empty);
            Assert.That(
                service.LastRoundResult.Participants.Single(item => item.Id == "user").Life,
                Is.EqualTo(2)
            );
        }

        /// <summary>
        /// 公開一覧を呼び出し側から変更できず、Applicationの進行状態を保護することを保証します。
        /// </summary>
        [Test]
        public void PublicSnapshots_公開一覧を変更しようとする_変更を拒否する()
        {
            var service = StartService();
            var participants = (IList<ParticipantState>)service.Participants;
            var cards = (IList<DealtCardState>)service.CurrentCards;

            Assert.That(() => participants.RemoveAt(0), Throws.TypeOf<NotSupportedException>());
            Assert.That(() => cards.RemoveAt(0), Throws.TypeOf<NotSupportedException>());

            LoseCurrentParticipant(service);
            var resultCards = (IList<DealtCardState>)service.LastRoundResult.DealtCards;
            var resultParticipants = (IList<ParticipantState>)service.LastRoundResult.Participants;

            Assert.That(() => resultCards.RemoveAt(0), Throws.TypeOf<NotSupportedException>());
            Assert.That(
                () => resultParticipants.RemoveAt(0),
                Throws.TypeOf<NotSupportedException>()
            );
        }

        /// <summary>
        /// 敗者が残存する場合は、その敗者を次ラウンドの開始者にすることを保証します。
        /// </summary>
        [Test]
        public void TryStartNextRound_敗者が残存する_敗者から次ラウンドを開始する()
        {
            var service = StartService();
            LoseCurrentParticipant(service);

            var succeeded = service.TryStartNextRound();

            Assert.That(succeeded, Is.True);
            Assert.That(service.RoundNumber, Is.EqualTo(2));
            Assert.That(service.StartingParticipantId, Is.EqualTo("user"));
            Assert.That(service.CurrentParticipantId, Is.EqualTo("user"));
            Assert.That(service.LastRoundResult, Is.Null);
        }

        /// <summary>
        /// 同じラウンド結果から次ラウンドを二重開始できず、配布とラウンド番号を保護することを保証します。
        /// </summary>
        [Test]
        public void TryStartNextRound_同じ結果から2回実行する_初回だけ受理する()
        {
            var service = StartService();
            LoseCurrentParticipant(service);

            var firstSucceeded = service.TryStartNextRound();
            var secondSucceeded = service.TryStartNextRound();

            Assert.That(firstSucceeded, Is.True);
            Assert.That(secondSucceeded, Is.False);
            Assert.That(service.RoundNumber, Is.EqualTo(2));
            Assert.That(service.CurrentCards, Has.Count.EqualTo(5));
        }

        /// <summary>
        /// 脱落した敗者を配布と手番から除外し、リング上の次の残存者へ開始を渡すことを保証します。
        /// </summary>
        [Test]
        public void TryStartNextRound_敗者npc1が脱落する_npc2から残存4名で開始する()
        {
            var service = StartService();
            LoseParticipantForRounds(service, "npc-1", 3);

            var succeeded = service.TryStartNextRound();

            Assert.That(succeeded, Is.True);
            Assert.That(service.StartingParticipantId, Is.EqualTo("npc-2"));
            Assert.That(service.CurrentCards, Has.Count.EqualTo(4));
            Assert.That(service.CurrentCards.Any(item => item.ParticipantId == "npc-1"), Is.False);
        }

        /// <summary>
        /// 空白の参加者IDを入力エラーとして扱い、不明IDは状態不変で拒否することを保証します。
        /// </summary>
        [Test]
        public void TryDeclareNumber_空白と不明な参加者IDを指定する_契約どおり拒否する()
        {
            var service = StartService();

            Assert.That(() => service.TryDeclareNumber(" ", 1), Throws.ArgumentException);
            Assert.That(service.TryDeclareNumber("unknown", 1), Is.False);
            Assert.That(service.CurrentParticipantId, Is.EqualTo("user"));
            Assert.That(service.DeclarationHistory, Is.Empty);
        }
    }
}
