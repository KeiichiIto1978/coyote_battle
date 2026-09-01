using System;
using CoyoteBattle.Domain;
using NUnit.Framework;

namespace CoyoteBattle.Domain.Tests
{
    public sealed class ParticipantTests
    {
        /// <summary>
        /// ユーザーとNPCを同じ参加者型で扱い、生成直後の状態を保証します。
        /// </summary>
        [TestCase("user", ParticipantKind.User)]
        [TestCase("npc-1", ParticipantKind.Npc)]
        public void Constructor_有効な識別子と種別を指定する_初期ライフ3で生成する(
            string id,
            ParticipantKind kind
        )
        {
            var participant = new Participant(id, kind);

            Assert.That(participant.Id, Is.EqualTo(id));
            Assert.That(participant.Kind, Is.EqualTo(kind));
            Assert.That(participant.Life, Is.EqualTo(3));
            Assert.That(participant.IsEliminated, Is.False);
        }

        /// <summary>
        /// 識別子と種別を外部から変更できない公開契約を保証します。
        /// </summary>
        [Test]
        public void Properties_参加者を生成する_識別子と種別が読み取り専用である()
        {
            Assert.That(typeof(Participant).GetProperty(nameof(Participant.Id)).CanWrite, Is.False);
            Assert.That(
                typeof(Participant).GetProperty(nameof(Participant.Kind)).CanWrite,
                Is.False
            );
            Assert.That(
                typeof(Participant).GetProperty(nameof(Participant.Life)).SetMethod.IsPublic,
                Is.False
            );
        }

        /// <summary>
        /// 有効な敗北適用がライフを1だけ減らすことを保証します。
        /// </summary>
        [Test]
        public void TryLoseLife_ライフ3で敗北する_ライフ2で残存する()
        {
            var participant = new Participant("user", ParticipantKind.User);

            var succeeded = participant.TryLoseLife();

            Assert.That(succeeded, Is.True);
            Assert.That(participant.Life, Is.EqualTo(2));
            Assert.That(participant.IsEliminated, Is.False);
        }

        /// <summary>
        /// ライフ1から0への遷移と脱落判定が同時に行われることを保証します。
        /// </summary>
        [Test]
        public void TryLoseLife_ライフ1で敗北する_ライフ0で脱落する()
        {
            var participant = new Participant("user", ParticipantKind.User);
            participant.TryLoseLife();
            participant.TryLoseLife();
            Assert.That(participant.Life, Is.EqualTo(1));

            var succeeded = participant.TryLoseLife();

            Assert.That(succeeded, Is.True);
            Assert.That(participant.Life, Is.Zero);
            Assert.That(participant.IsEliminated, Is.True);
        }

        /// <summary>
        /// 脱落後の重複減算を拒否し、ライフを負数にしないことを保証します。
        /// </summary>
        [Test]
        public void TryLoseLife_脱落後に再度敗北する_失敗してライフ0を維持する()
        {
            var participant = new Participant("user", ParticipantKind.User);
            participant.TryLoseLife();
            participant.TryLoseLife();
            participant.TryLoseLife();

            var succeeded = participant.TryLoseLife();

            Assert.That(succeeded, Is.False);
            Assert.That(participant.Life, Is.Zero);
            Assert.That(participant.IsEliminated, Is.True);
        }

        /// <summary>
        /// 参加者を識別できないnull、空、空白のIDを拒否します。
        /// </summary>
        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void Constructor_無効な識別子を指定する_例外を送出する(string id)
        {
            Assert.Throws<ArgumentException>(() => new Participant(id, ParticipantKind.User));
        }

        /// <summary>
        /// Version 1で定義されていない参加者種別を生成できないことを保証します。
        /// </summary>
        [Test]
        public void Constructor_未定義の種別を指定する_例外を送出する()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new Participant("unknown", (ParticipantKind)999)
            );
        }
    }
}
