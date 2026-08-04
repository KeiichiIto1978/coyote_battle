using System;
using System.Collections.Generic;
using System.Linq;
using CoyoteBattle.Domain;
using NUnit.Framework;

namespace CoyoteBattle.Domain.Tests
{
    public sealed class ParticipantRosterTests
    {
        /// <summary>
        /// Version 1の固定5名が一意なIDと確定順で生成されることを保証します。
        /// </summary>
        [Test]
        public void Create_既定参加者を生成する_ユーザー1名とNPC4名を確定順で返す()
        {
            var roster = DefaultParticipantFactory.Create();

            Assert.That(
                roster.Participants.Select(participant => participant.Id),
                Is.EqualTo(new[] { "user", "npc-1", "npc-2", "npc-3", "npc-4" })
            );
            Assert.That(
                roster.Participants.Select(participant => participant.Kind),
                Is.EqualTo(
                    new[]
                    {
                        ParticipantKind.User,
                        ParticipantKind.Npc,
                        ParticipantKind.Npc,
                        ParticipantKind.Npc,
                        ParticipantKind.Npc,
                    }
                )
            );
            Assert.That(
                roster.Participants.Select(participant => participant.Id).Distinct().Count(),
                Is.EqualTo(5)
            );
            Assert.That(
                roster.Participants,
                Has.All.Matches<Participant>(participant => participant.Life == 3)
            );
            Assert.That(
                roster.Participants,
                Has.None.Matches<Participant>(participant => participant.IsEliminated)
            );
        }

        /// <summary>
        /// 既知のIDから同一の参加者を取得できることを保証します。
        /// </summary>
        [Test]
        public void TryGetParticipant_既知の識別子を指定する_対応する参加者を返す()
        {
            var roster = DefaultParticipantFactory.Create();

            var succeeded = roster.TryGetParticipant("npc-2", out var participant);

            Assert.That(succeeded, Is.True);
            Assert.That(participant, Is.SameAs(roster.Participants[2]));
        }

        /// <summary>
        /// 未知のID検索が状態を変更せず失敗することを保証します。
        /// </summary>
        [Test]
        public void TryGetParticipant_未知の識別子を指定する_対象なしを返す()
        {
            var roster = DefaultParticipantFactory.Create();

            var succeeded = roster.TryGetParticipant("npc-5", out var participant);

            Assert.That(succeeded, Is.False);
            Assert.That(participant, Is.Null);
            Assert.That(roster.Participants, Has.All.Matches<Participant>(item => item.Life == 3));
        }

        /// <summary>
        /// 検索に利用できないIDを入力エラーとして拒否します。
        /// </summary>
        [TestCase(null)]
        [TestCase("")]
        [TestCase("  ")]
        public void TryGetParticipant_無効な識別子を指定する_例外を送出する(string id)
        {
            var roster = DefaultParticipantFactory.Create();

            Assert.Throws<ArgumentException>(() => roster.TryGetParticipant(id, out _));
        }

        /// <summary>
        /// 敗者1名だけへライフ減少を適用し、他参加者を変更しないことを保証します。
        /// </summary>
        [Test]
        public void TryApplyLoss_残存参加者を指定する_対象者だけライフを1減らす()
        {
            var roster = DefaultParticipantFactory.Create();

            var succeeded = roster.TryApplyLoss("npc-3");

            Assert.That(succeeded, Is.True);
            Assert.That(roster.Participants.Single(item => item.Id == "npc-3").Life, Is.EqualTo(2));
            Assert.That(
                roster.Participants.Where(item => item.Id != "npc-3"),
                Has.All.Matches<Participant>(item => item.Life == 3)
            );
        }

        /// <summary>
        /// 未知の敗者IDを状態変更なしで拒否することを保証します。
        /// </summary>
        [Test]
        public void TryApplyLoss_未知の識別子を指定する_失敗して全員の状態を維持する()
        {
            var roster = DefaultParticipantFactory.Create();

            var succeeded = roster.TryApplyLoss("npc-5");

            Assert.That(succeeded, Is.False);
            Assert.That(roster.Participants, Has.All.Matches<Participant>(item => item.Life == 3));
        }

        /// <summary>
        /// 敗北適用に利用できないIDを入力エラーとして拒否します。
        /// </summary>
        [TestCase(null)]
        [TestCase("")]
        [TestCase("  ")]
        public void TryApplyLoss_無効な識別子を指定する_例外を送出する(string id)
        {
            var roster = DefaultParticipantFactory.Create();

            Assert.Throws<ArgumentException>(() => roster.TryApplyLoss(id));
            Assert.That(roster.Participants, Has.All.Matches<Participant>(item => item.Life == 3));
        }

        /// <summary>
        /// 脱落済み参加者への敗北適用を状態変更なしで拒否します。
        /// </summary>
        [Test]
        public void TryApplyLoss_脱落済み参加者を指定する_失敗して全員の状態を維持する()
        {
            var roster = DefaultParticipantFactory.Create();
            roster.TryApplyLoss("npc-1");
            roster.TryApplyLoss("npc-1");
            roster.TryApplyLoss("npc-1");
            var before = roster.Participants.Select(item => item.Life).ToArray();

            var succeeded = roster.TryApplyLoss("npc-1");

            Assert.That(succeeded, Is.False);
            Assert.That(roster.Participants.Select(item => item.Life), Is.EqualTo(before));
        }

        /// <summary>
        /// 脱落者を全参加者に保持しつつ、残存参加者から確定順で除外します。
        /// </summary>
        [Test]
        public void RemainingParticipants_複数名が脱落する_脱落者を除いた相対順を返す()
        {
            var roster = DefaultParticipantFactory.Create();
            Eliminate(roster, "npc-1");
            Eliminate(roster, "npc-3");

            var remaining = roster.RemainingParticipants;

            Assert.That(roster.Participants, Has.Count.EqualTo(5));
            Assert.That(
                remaining.Select(participant => participant.Id),
                Is.EqualTo(new[] { "user", "npc-2", "npc-4" })
            );
        }

        /// <summary>
        /// ユーザー脱落後も残存NPCを取得できることを保証します。
        /// </summary>
        [Test]
        public void RemainingParticipants_ユーザーが脱落する_NPC4名を返す()
        {
            var roster = DefaultParticipantFactory.Create();
            Eliminate(roster, "user");

            Assert.That(
                roster.RemainingParticipants.Select(participant => participant.Id),
                Is.EqualTo(new[] { "npc-1", "npc-2", "npc-3", "npc-4" })
            );
        }

        /// <summary>
        /// NPC全員脱落時にユーザーだけが残存することを保証します。
        /// </summary>
        [Test]
        public void RemainingParticipants_NPC4名が脱落する_ユーザーだけを返す()
        {
            var roster = DefaultParticipantFactory.Create();
            Eliminate(roster, "npc-1");
            Eliminate(roster, "npc-2");
            Eliminate(roster, "npc-3");
            Eliminate(roster, "npc-4");

            Assert.That(
                roster.RemainingParticipants.Select(participant => participant.Id),
                Is.EqualTo(new[] { "user" })
            );
        }

        /// <summary>
        /// 境界値として残存者0名を安全に表現できることを保証します。
        /// </summary>
        [Test]
        public void RemainingParticipants_全員が脱落する_空の一覧を返す()
        {
            var roster = DefaultParticipantFactory.Create();
            foreach (var participant in roster.Participants)
            {
                Eliminate(roster, participant.Id);
            }

            Assert.That(roster.RemainingParticipants, Is.Empty);
            Assert.That(roster.Participants, Has.Count.EqualTo(5));
        }

        /// <summary>
        /// 残存人数の全境界値で、脱落者だけが一覧から除外されることを保証します。
        /// </summary>
        [TestCase(0, 5)]
        [TestCase(1, 4)]
        [TestCase(2, 3)]
        [TestCase(3, 2)]
        [TestCase(4, 1)]
        [TestCase(5, 0)]
        public void RemainingParticipants_指定人数が脱落する_残存0名から5名を取得できる(
            int eliminatedCount,
            int expectedRemainingCount
        )
        {
            var roster = DefaultParticipantFactory.Create();
            var idsToEliminate = roster
                .Participants.Take(eliminatedCount)
                .Select(participant => participant.Id)
                .ToArray();
            foreach (var participantId in idsToEliminate)
            {
                Eliminate(roster, participantId);
            }

            Assert.That(roster.RemainingParticipants, Has.Count.EqualTo(expectedRemainingCount));
            Assert.That(
                roster.RemainingParticipants,
                Has.All.Matches<Participant>(participant => !participant.IsEliminated)
            );
        }

        /// <summary>
        /// 公開一覧から参加者構成を変更できないことを保証します。
        /// </summary>
        [Test]
        public void ParticipantLists_既定参加者を生成する_読み取り専用一覧を返す()
        {
            var roster = DefaultParticipantFactory.Create();
            var all = roster.Participants as IList<Participant>;
            var remaining = roster.RemainingParticipants as IList<Participant>;

            Assert.That(all, Is.Not.Null);
            Assert.That(remaining, Is.Not.Null);
            Assert.That(all.IsReadOnly, Is.True);
            Assert.That(remaining.IsReadOnly, Is.True);
            Assert.Throws<NotSupportedException>(() =>
                all.Add(new Participant("npc-5", ParticipantKind.Npc))
            );
            Assert.Throws<NotSupportedException>(() => remaining.RemoveAt(0));
        }

        /// <summary>
        /// 指定参加者を3回敗北させ、テスト条件として脱落状態へ遷移させます。
        /// </summary>
        /// <param name="roster">変更対象の参加者管理です。</param>
        /// <param name="participantId">脱落させる参加者IDです。</param>
        private static void Eliminate(ParticipantRoster roster, string participantId)
        {
            Assert.That(roster.TryApplyLoss(participantId), Is.True);
            Assert.That(roster.TryApplyLoss(participantId), Is.True);
            Assert.That(roster.TryApplyLoss(participantId), Is.True);
        }
    }
}
