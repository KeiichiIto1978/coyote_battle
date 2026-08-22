using CoyoteBattle.Domain;
using CoyoteBattle.Presentation;
using NUnit.Framework;

namespace CoyoteBattle.Tests.Presentation
{
    /// <summary>
    /// 画面で用いるゲーム用語と特殊カード表記を固定します。
    /// </summary>
    public sealed class PresentationFormattingTests
    {
        [TestCase("user", "あなた")]
        [TestCase("npc-1", "NPC 1（強気）")]
        [TestCase("npc-4", "NPC 4（分析）")]
        public void ParticipantDisplayName_固定参加者_表示名を返す(string id, string expected)
        {
            Assert.That(PresentationText.ParticipantName(id), Is.EqualTo(expected));
        }

        [TestCase(CardKind.Night, "0★")]
        [TestCase(CardKind.Double, "×2")]
        [TestCase(CardKind.MaxToZero, "MAX→0")]
        [TestCase(CardKind.Mystery, "？")]
        public void CardText_特殊カード_識別可能な表記を返す(CardKind kind, string expected)
        {
            Assert.That(PresentationText.Card(kind, null), Is.EqualTo(expected));
        }
    }
}
