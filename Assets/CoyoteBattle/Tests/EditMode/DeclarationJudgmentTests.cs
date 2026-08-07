using System;
using CoyoteBattle.Domain;
using NUnit.Framework;

namespace CoyoteBattle.Domain.Tests
{
    public sealed class DeclarationJudgmentTests
    {
        /// <summary>
        /// 比較対象となる数字宣言がない初手では、コヨーテを選べないことを保証します。
        /// </summary>
        [Test]
        public void TryDeclareCoyote_数字宣言前に呼ぶ_初期状態を変えず拒否する()
        {
            var phase = CreatePhase();

            var succeeded = phase.TryDeclareCoyote("user");

            Assert.That(succeeded, Is.False);
            Assert.That(
                phase.Status,
                Is.EqualTo(DeclarationPhaseStatus.AcceptingNumberDeclarations)
            );
            Assert.That(phase.CurrentDeclaration, Is.Null);
            Assert.That(phase.CoyoteDeclarerId, Is.Null);
        }

        /// <summary>
        /// 2番手以降が直前の数字宣言へコヨーテを宣言すると、宣言者を保持して終了することを保証します。
        /// </summary>
        [Test]
        public void TryDeclareCoyote_数字宣言後に別参加者が呼ぶ_宣言者を保持して終了する()
        {
            var phase = CreatePhase();
            phase.TryDeclareNumber("user", 10);

            var succeeded = phase.TryDeclareCoyote("npc-1");

            Assert.That(succeeded, Is.True);
            Assert.That(phase.Status, Is.EqualTo(DeclarationPhaseStatus.CoyoteDeclared));
            Assert.That(phase.CoyoteDeclarerId, Is.EqualTo("npc-1"));
            Assert.That(phase.CurrentDeclaration.ParticipantId, Is.EqualTo("user"));
            Assert.That(phase.CurrentDeclaration.Value, Is.EqualTo(10));
        }

        /// <summary>
        /// 数字宣言者が自分自身の宣言へコヨーテできず、受付中状態を維持することを保証します。
        /// </summary>
        [Test]
        public void TryDeclareCoyote_最後の数字宣言者が呼ぶ_状態を変えず拒否する()
        {
            var phase = CreatePhase();
            phase.TryDeclareNumber("user", 10);

            var succeeded = phase.TryDeclareCoyote("user");

            Assert.That(succeeded, Is.False);
            Assert.That(
                phase.Status,
                Is.EqualTo(DeclarationPhaseStatus.AcceptingNumberDeclarations)
            );
            Assert.That(phase.CoyoteDeclarerId, Is.Null);
            Assert.That(phase.History, Has.Count.EqualTo(1));
        }

        /// <summary>
        /// ラウンド開始時に参加資格がない識別子のコヨーテ宣言を拒否することを保証します。
        /// </summary>
        [Test]
        public void TryDeclareCoyote_未登録参加者が呼ぶ_状態を変えず拒否する()
        {
            var phase = CreatePhase();
            phase.TryDeclareNumber("user", 10);

            var succeeded = phase.TryDeclareCoyote("unknown");

            Assert.That(succeeded, Is.False);
            Assert.That(
                phase.Status,
                Is.EqualTo(DeclarationPhaseStatus.AcceptingNumberDeclarations)
            );
            Assert.That(phase.CoyoteDeclarerId, Is.Null);
        }

        /// <summary>
        /// 呼び出し側の不具合を示す空白のコヨーテ宣言者IDを入力エラーとして拒否することを保証します。
        /// </summary>
        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void TryDeclareCoyote_参加者IDが空白である_入力エラーになる(string participantId)
        {
            var phase = CreatePhase();
            phase.TryDeclareNumber("user", 10);

            Assert.That(
                () => phase.TryDeclareCoyote(participantId),
                Throws.ArgumentException.With.Property("ParamName").EqualTo("participantId")
            );
        }

        /// <summary>
        /// int上限を数字宣言した後も、別参加者はコヨーテを選択できることを保証します。
        /// </summary>
        [Test]
        public void TryDeclareCoyote_int上限を数字宣言済みである_宣言を受理する()
        {
            var phase = CreatePhase();
            phase.TryDeclareNumber("user", int.MaxValue);

            var succeeded = phase.TryDeclareCoyote("npc-1");

            Assert.That(succeeded, Is.True);
            Assert.That(phase.Status, Is.EqualTo(DeclarationPhaseStatus.CoyoteDeclared));
        }

        /// <summary>
        /// コヨーテ後は数字宣言を拒否し、判定に使う最終宣言を固定することを保証します。
        /// </summary>
        [Test]
        public void TryDeclareNumber_コヨーテ宣言後に呼ぶ_終了状態を変えず拒否する()
        {
            var phase = CreateEndedPhase(10);

            var succeeded = phase.TryDeclareNumber("npc-2", 11);

            Assert.That(succeeded, Is.False);
            Assert.That(phase.Status, Is.EqualTo(DeclarationPhaseStatus.CoyoteDeclared));
            Assert.That(phase.CurrentDeclaration.Value, Is.EqualTo(10));
            Assert.That(phase.CoyoteDeclarerId, Is.EqualTo("npc-1"));
            Assert.That(phase.History, Has.Count.EqualTo(1));
        }

        /// <summary>
        /// コヨーテ後は2回目のコヨーテを拒否し、最初の宣言者を固定することを保証します。
        /// </summary>
        [Test]
        public void TryDeclareCoyote_コヨーテ宣言後に呼ぶ_終了状態を変えず拒否する()
        {
            var phase = CreateEndedPhase(10);

            var succeeded = phase.TryDeclareCoyote("npc-2");

            Assert.That(succeeded, Is.False);
            Assert.That(phase.Status, Is.EqualTo(DeclarationPhaseStatus.CoyoteDeclared));
            Assert.That(phase.CoyoteDeclarerId, Is.EqualTo("npc-1"));
            Assert.That(phase.History, Has.Count.EqualTo(1));
        }

        /// <summary>
        /// コヨーテ前には敗者候補が確定していないため、判定を無効状態として拒否することを保証します。
        /// </summary>
        [Test]
        public void DetermineLoser_コヨーテ宣言前に呼ぶ_無効状態として拒否する()
        {
            var phase = CreatePhase();
            phase.TryDeclareNumber("user", 10);

            Assert.That(() => phase.DetermineLoser(9), Throws.InvalidOperationException);
            Assert.That(
                phase.Status,
                Is.EqualTo(DeclarationPhaseStatus.AcceptingNumberDeclarations)
            );
            Assert.That(phase.History, Has.Count.EqualTo(1));
        }

        /// <summary>
        /// 宣言値と実合計の大小関係に応じて、確定ルールどおりの敗者を返すことを保証します。
        /// </summary>
        [TestCase(9, "user")]
        [TestCase(10, "npc-1")]
        [TestCase(11, "npc-1")]
        public void DetermineLoser_宣言値10を判定する_大小関係に応じた敗者を返す(
            int actualTotal,
            string expectedLoserId
        )
        {
            var phase = CreateEndedPhase(10);

            var loserId = phase.DetermineLoser(actualTotal);

            Assert.That(loserId, Is.EqualTo(expectedLoserId));
        }

        /// <summary>
        /// 複数宣言後は、過去の宣言者ではなく最後の数字宣言者を敗者候補にすることを保証します。
        /// </summary>
        [Test]
        public void DetermineLoser_複数宣言後に宣言値を超過する_最後の数字宣言者を返す()
        {
            var phase = CreatePhase();
            phase.TryDeclareNumber("user", 10);
            phase.TryDeclareNumber("npc-1", 15);
            phase.TryDeclareNumber("npc-2", 20);
            phase.TryDeclareCoyote("user");

            var loserId = phase.DetermineLoser(19);

            Assert.That(loserId, Is.EqualTo("npc-2"));
        }

        /// <summary>
        /// 負数と0を含む実合計を、正の最小宣言値と安全に比較できることを保証します。
        /// </summary>
        [TestCase(int.MinValue)]
        [TestCase(-1)]
        [TestCase(0)]
        public void DetermineLoser_最終宣言1で実合計が1未満である_数字宣言者を返す(int actualTotal)
        {
            var phase = CreateEndedPhase(1);

            var loserId = phase.DetermineLoser(actualTotal);

            Assert.That(loserId, Is.EqualTo("user"));
        }

        /// <summary>
        /// int上限付近でも比較がオーバーフローせず、未満と等値を区別できることを保証します。
        /// </summary>
        [TestCase(int.MaxValue - 1, "user")]
        [TestCase(int.MaxValue, "npc-1")]
        public void DetermineLoser_最終宣言がint上限である_境界に応じた敗者を返す(
            int actualTotal,
            string expectedLoserId
        )
        {
            var phase = CreateEndedPhase(int.MaxValue);

            var loserId = phase.DetermineLoser(actualTotal);

            Assert.That(loserId, Is.EqualTo(expectedLoserId));
        }

        /// <summary>
        /// 判定が純粋な参照処理であり、繰り返しても宣言状態を変更しないことを保証します。
        /// </summary>
        [Test]
        public void DetermineLoser_同じ実合計で繰り返す_同じ敗者と宣言状態を維持する()
        {
            var phase = CreateEndedPhase(10);

            var firstLoserId = phase.DetermineLoser(9);
            var secondLoserId = phase.DetermineLoser(9);

            Assert.That(firstLoserId, Is.EqualTo("user"));
            Assert.That(secondLoserId, Is.EqualTo("user"));
            Assert.That(phase.Status, Is.EqualTo(DeclarationPhaseStatus.CoyoteDeclared));
            Assert.That(phase.CurrentDeclaration.Value, Is.EqualTo(10));
            Assert.That(phase.CoyoteDeclarerId, Is.EqualTo("npc-1"));
            Assert.That(phase.History, Has.Count.EqualTo(1));
        }

        /// <summary>
        /// 敗者判定は参加者のライフや脱落状態を変更しないことを保証します。
        /// </summary>
        [Test]
        public void DetermineLoser_参加者IDに対して判定する_参加者状態を変更しない()
        {
            var user = new Participant("user", ParticipantKind.User);
            var npc = new Participant("npc-1", ParticipantKind.Npc);
            var phase = new DeclarationPhase(new[] { user.Id, npc.Id });
            phase.TryDeclareNumber(user.Id, 10);
            phase.TryDeclareCoyote(npc.Id);

            phase.DetermineLoser(9);

            Assert.That(user.Life, Is.EqualTo(Participant.InitialLife));
            Assert.That(user.IsEliminated, Is.False);
            Assert.That(npc.Life, Is.EqualTo(Participant.InitialLife));
            Assert.That(npc.IsEliminated, Is.False);
        }

        private static DeclarationPhase CreatePhase()
        {
            return new DeclarationPhase(new[] { "user", "npc-1", "npc-2" });
        }

        private static DeclarationPhase CreateEndedPhase(int declaredValue)
        {
            var phase = CreatePhase();
            phase.TryDeclareNumber("user", declaredValue);
            phase.TryDeclareCoyote("npc-1");
            return phase;
        }
    }
}
