using System;
using System.Collections.Generic;
using CoyoteBattle.Domain;
using NUnit.Framework;

namespace CoyoteBattle.Domain.Tests
{
    public sealed class DeclarationPhaseTests
    {
        /// <summary>
        /// 有効な参加者一覧から、宣言をまだ持たない受付中のフェーズが生成されることを保証します。
        /// </summary>
        [Test]
        public void Constructor_有効な参加者を指定する_初期状態を生成する()
        {
            var phase = CreatePhase();

            Assert.That(
                phase.Status,
                Is.EqualTo(DeclarationPhaseStatus.AcceptingNumberDeclarations)
            );
            Assert.That(phase.CurrentDeclaration, Is.Null);
            Assert.That(phase.History, Is.Empty);
            Assert.That(phase.CoyoteDeclarerId, Is.Null);
        }

        /// <summary>
        /// 呼び出し側が生成元一覧を変更しても、ラウンド開始時の参加資格が変わらないことを保証します。
        /// </summary>
        [Test]
        public void Constructor_生成後に元の一覧を変更する_参加者スナップショットを維持する()
        {
            var participantIds = new List<string> { "user", "npc-1" };
            var phase = new DeclarationPhase(participantIds);

            participantIds[0] = "changed";
            participantIds.Add("npc-2");

            Assert.That(phase.EligibleParticipantIds, Is.EqualTo(new[] { "user", "npc-1" }));
        }

        /// <summary>
        /// 公開された宣言参加者一覧を呼び出し側から変更できないことを保証します。
        /// </summary>
        [Test]
        public void EligibleParticipantIds_読み取り専用一覧を変更しようとする_変更を拒否する()
        {
            var phase = CreatePhase();
            var participantIds = (IList<string>)phase.EligibleParticipantIds;

            Assert.That(() => participantIds.RemoveAt(0), Throws.TypeOf<NotSupportedException>());
            Assert.That(
                phase.EligibleParticipantIds,
                Is.EqualTo(new[] { "user", "npc-1", "npc-2" })
            );
        }

        /// <summary>
        /// 宣言参加者一覧がない状態を生成できず、不完全なフェーズを防ぐことを保証します。
        /// </summary>
        [Test]
        public void Constructor_nullの参加者一覧を指定する_入力エラーになる()
        {
            Assert.That(
                () => new DeclarationPhase(null),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("participantIds")
            );
        }

        /// <summary>
        /// 宣言が成立しない1名以下の参加者一覧を拒否することを保証します。
        /// </summary>
        [TestCaseSource(nameof(TooFewParticipantIdCases))]
        public void Constructor_参加者が2名未満である_入力エラーになる(
            IReadOnlyList<string> participantIds
        )
        {
            Assert.That(
                () => new DeclarationPhase(participantIds),
                Throws.ArgumentException.With.Property("ParamName").EqualTo("participantIds")
            );
        }

        /// <summary>
        /// 無効または重複した識別子を含む参加者一覧を拒否することを保証します。
        /// </summary>
        [TestCaseSource(nameof(InvalidParticipantIdCases))]
        public void Constructor_参加者識別子が不正である_入力エラーになる(
            IReadOnlyList<string> participantIds
        )
        {
            Assert.That(
                () => new DeclarationPhase(participantIds),
                Throws.ArgumentException.With.Property("ParamName").EqualTo("participantIds")
            );
        }

        /// <summary>
        /// 宣言可能範囲の下限と上限を、最初の数字宣言として受理できることを保証します。
        /// </summary>
        [TestCase(1)]
        [TestCase(int.MaxValue)]
        public void TryDeclareNumber_最初に宣言可能範囲の値を指定する_宣言を受理する(int value)
        {
            var phase = CreatePhase();

            var succeeded = phase.TryDeclareNumber("user", value);

            Assert.That(succeeded, Is.True);
            Assert.That(phase.CurrentDeclaration.ParticipantId, Is.EqualTo("user"));
            Assert.That(phase.CurrentDeclaration.Value, Is.EqualTo(value));
            Assert.That(phase.History, Has.Count.EqualTo(1));
        }

        /// <summary>
        /// 0以下は宣言できず、拒否時に初期状態が変化しないことを保証します。
        /// </summary>
        [TestCase(-1)]
        [TestCase(0)]
        public void TryDeclareNumber_最初に0以下を指定する_状態を変えず拒否する(int value)
        {
            var phase = CreatePhase();

            var succeeded = phase.TryDeclareNumber("user", value);

            Assert.That(succeeded, Is.False);
            Assert.That(phase.CurrentDeclaration, Is.Null);
            Assert.That(phase.History, Is.Empty);
            Assert.That(
                phase.Status,
                Is.EqualTo(DeclarationPhaseStatus.AcceptingNumberDeclarations)
            );
        }

        /// <summary>
        /// 直前値より1大きい数字を別参加者が宣言すると、現在値と履歴が更新されることを保証します。
        /// </summary>
        [Test]
        public void TryDeclareNumber_別参加者が直前値より1大きい値を宣言する_宣言を受理する()
        {
            var phase = CreatePhase();
            phase.TryDeclareNumber("user", 1);

            var succeeded = phase.TryDeclareNumber("npc-1", 2);

            Assert.That(succeeded, Is.True);
            Assert.That(phase.CurrentDeclaration.ParticipantId, Is.EqualTo("npc-1"));
            Assert.That(phase.CurrentDeclaration.Value, Is.EqualTo(2));
            Assert.That(phase.History, Has.Count.EqualTo(2));
        }

        /// <summary>
        /// 同値または小さい数字を拒否し、直前の宣言と履歴を維持することを保証します。
        /// </summary>
        [TestCase(9)]
        [TestCase(10)]
        public void TryDeclareNumber_直前値以下を宣言する_状態を変えず拒否する(int value)
        {
            var phase = CreatePhase();
            phase.TryDeclareNumber("user", 10);

            var succeeded = phase.TryDeclareNumber("npc-1", value);

            Assert.That(succeeded, Is.False);
            Assert.That(phase.CurrentDeclaration.ParticipantId, Is.EqualTo("user"));
            Assert.That(phase.CurrentDeclaration.Value, Is.EqualTo(10));
            Assert.That(phase.History, Has.Count.EqualTo(1));
        }

        /// <summary>
        /// int上限へ到達した後はオーバーフローせず、それ以上の数字宣言を拒否することを保証します。
        /// </summary>
        [Test]
        public void TryDeclareNumber_int上限を宣言済みである_追加の数字宣言を拒否する()
        {
            var phase = CreatePhase();
            phase.TryDeclareNumber("user", int.MaxValue - 1);
            phase.TryDeclareNumber("npc-1", int.MaxValue);

            var succeeded = phase.TryDeclareNumber("npc-2", int.MinValue);

            Assert.That(succeeded, Is.False);
            Assert.That(phase.CurrentDeclaration.Value, Is.EqualTo(int.MaxValue));
            Assert.That(phase.History, Has.Count.EqualTo(2));
        }

        /// <summary>
        /// 同じ参加者が連続して数字宣言できず、履歴が変化しないことを保証します。
        /// </summary>
        [Test]
        public void TryDeclareNumber_直前の宣言者が再度宣言する_状態を変えず拒否する()
        {
            var phase = CreatePhase();
            phase.TryDeclareNumber("user", 10);

            var succeeded = phase.TryDeclareNumber("user", 11);

            Assert.That(succeeded, Is.False);
            Assert.That(phase.CurrentDeclaration.Value, Is.EqualTo(10));
            Assert.That(phase.History, Has.Count.EqualTo(1));
        }

        /// <summary>
        /// ラウンド開始時に参加資格がない識別子の数字宣言を拒否することを保証します。
        /// </summary>
        [Test]
        public void TryDeclareNumber_未登録参加者が宣言する_状態を変えず拒否する()
        {
            var phase = CreatePhase();

            var succeeded = phase.TryDeclareNumber("unknown", 1);

            Assert.That(succeeded, Is.False);
            Assert.That(phase.CurrentDeclaration, Is.Null);
            Assert.That(phase.History, Is.Empty);
        }

        /// <summary>
        /// 呼び出し側の不具合を示す空白の参加者IDを入力エラーとして拒否することを保証します。
        /// </summary>
        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void TryDeclareNumber_参加者IDが空白である_入力エラーになる(string participantId)
        {
            var phase = CreatePhase();

            Assert.That(
                () => phase.TryDeclareNumber(participantId, 1),
                Throws.ArgumentException.With.Property("ParamName").EqualTo("participantId")
            );
        }

        /// <summary>
        /// 複数の成功した数字宣言が、宣言順・宣言者・宣言値どおりに保持されることを保証します。
        /// </summary>
        [Test]
        public void History_複数の数字宣言が成功する_宣言順に内容を返す()
        {
            var phase = CreatePhase();
            phase.TryDeclareNumber("user", 10);
            phase.TryDeclareNumber("npc-1", 15);
            phase.TryDeclareNumber("npc-2", 20);

            Assert.That(phase.History[0].ParticipantId, Is.EqualTo("user"));
            Assert.That(phase.History[0].Value, Is.EqualTo(10));
            Assert.That(phase.History[1].ParticipantId, Is.EqualTo("npc-1"));
            Assert.That(phase.History[1].Value, Is.EqualTo(15));
            Assert.That(phase.History[2].ParticipantId, Is.EqualTo("npc-2"));
            Assert.That(phase.History[2].Value, Is.EqualTo(20));
        }

        /// <summary>
        /// 公開された数字宣言履歴を呼び出し側から変更できないことを保証します。
        /// </summary>
        [Test]
        public void History_読み取り専用一覧を変更しようとする_変更を拒否する()
        {
            var phase = CreatePhase();
            phase.TryDeclareNumber("user", 10);
            var history = (IList<NumberDeclaration>)phase.History;

            Assert.That(() => history.RemoveAt(0), Throws.TypeOf<NotSupportedException>());
            Assert.That(phase.History, Has.Count.EqualTo(1));
        }

        private static IEnumerable<TestCaseData> TooFewParticipantIdCases()
        {
            yield return new TestCaseData((object)Array.Empty<string>());
            yield return new TestCaseData((object)new[] { "user" });
        }

        private static IEnumerable<TestCaseData> InvalidParticipantIdCases()
        {
            yield return new TestCaseData((object)new[] { null, "npc-1" });
            yield return new TestCaseData((object)new[] { "", "npc-1" });
            yield return new TestCaseData((object)new[] { " ", "npc-1" });
            yield return new TestCaseData((object)new[] { "user", "user" });
        }

        private static DeclarationPhase CreatePhase()
        {
            return new DeclarationPhase(new[] { "user", "npc-1", "npc-2" });
        }
    }
}
