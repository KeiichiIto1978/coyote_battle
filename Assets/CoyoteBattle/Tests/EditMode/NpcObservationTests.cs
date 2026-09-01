using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using static CoyoteBattle.Domain.Tests.NpcTestHelper;

namespace CoyoteBattle.Domain.Tests
{
    public sealed class NpcObservationTests
    {
        /// <summary>
        /// 行動NPC自身のカードを含めず、他の残存参加者だけを観測できることを保証します。
        /// </summary>
        [Test]
        public void Constructor_npc1用の公開カードを指定する_自分以外の4枚を保持する()
        {
            var observation = CreateObservation();

            Assert.That(observation.ActorId, Is.EqualTo("npc-1"));
            Assert.That(observation.VisibleCards, Has.Count.EqualTo(4));
            Assert.That(
                observation.VisibleCards.Select(item => item.ParticipantId),
                Is.EquivalentTo(new[] { "user", "npc-2", "npc-3", "npc-4" })
            );
            Assert.That(
                observation.VisibleCards.Any(item => item.ParticipantId == "npc-1"),
                Is.False
            );
        }

        /// <summary>
        /// 観測へ自分のカードが混入した場合に、秘密情報の境界違反として拒否することを保証します。
        /// </summary>
        [Test]
        public void Constructor_行動NPC自身のカードを含める_入力エラーになる()
        {
            var cards = new[]
            {
                new CardDeal("user", Card.CreateNumber(100, 20)),
                new CardDeal("npc-1", Card.CreateNumber(101, 5)),
                new CardDeal("npc-2", Card.CreateNumber(102, 4)),
                new CardDeal("npc-3", Card.CreateNumber(103, 3)),
                new CardDeal("npc-4", Card.CreateNumber(104, 2)),
            };

            Assert.That(() => CreateObservation(visibleCards: cards), Throws.ArgumentException);
        }

        /// <summary>
        /// 初手と宣言後から、既存宣言ルールに一致する有効行動範囲を導出することを保証します。
        /// </summary>
        [Test]
        public void Properties_初手と宣言後を生成する_有効行動範囲を返す()
        {
            var first = CreateObservation();
            var afterDeclaration = CreateObservation(declarations: new[] { 10 });

            Assert.That(first.IsFirstDeclaration, Is.True);
            Assert.That(first.MinimumNumber, Is.EqualTo(1));
            Assert.That(first.CanDeclareCoyote, Is.False);
            Assert.That(afterDeclaration.IsFirstDeclaration, Is.False);
            Assert.That(afterDeclaration.MinimumNumber, Is.EqualTo(11));
            Assert.That(afterDeclaration.CanDeclareCoyote, Is.True);
        }

        /// <summary>
        /// 公開された観測一覧を呼び出し側から変更できないことを保証します。
        /// </summary>
        [Test]
        public void PublicLists_観測一覧を変更しようとする_変更を拒否する()
        {
            var observation = CreateObservation();

            Assert.That(
                () => ((IList<CardDeal>)observation.VisibleCards).RemoveAt(0),
                Throws.TypeOf<NotSupportedException>()
            );
            Assert.That(
                () => ((IList<NpcParticipantObservation>)observation.Participants).RemoveAt(0),
                Throws.TypeOf<NotSupportedException>()
            );
            Assert.That(
                () =>
                    ((IList<NpcDiscardedCardCount>)observation.DiscardedCards).Add(
                        new NpcDiscardedCardCount(CardKind.Number, 20, 1)
                    ),
                Throws.TypeOf<NotSupportedException>()
            );
        }

        /// <summary>
        /// 既定山札の枚数を超える使用済み札集計を、推定前に不正入力として拒否することを保証します。
        /// </summary>
        [Test]
        public void NpcDiscardedCardCount_20を2枚指定する_入力エラーになる()
        {
            Assert.That(
                () => new NpcDiscardedCardCount(CardKind.Number, 20, 2),
                Throws.TypeOf<ArgumentOutOfRangeException>()
            );
        }

        /// <summary>
        /// 同じカード面を複数要素へ分割した使用済み札観測を拒否することを保証します。
        /// </summary>
        [Test]
        public void Constructor_使用済み20を重複して指定する_入力エラーになる()
        {
            var discardedCards = new[]
            {
                new NpcDiscardedCardCount(CardKind.Number, 20, 1),
                new NpcDiscardedCardCount(CardKind.Number, 20, 1),
            };

            Assert.That(
                () => CreateObservation(discardedCards: discardedCards),
                Throws.ArgumentException
            );
        }

        /// <summary>
        /// 残存2名では行動NPC以外の1枚だけを観測し、脱落者をカード対象から除くことを保証します。
        /// </summary>
        [Test]
        public void Constructor_ユーザーとnpc1だけが残存する_公開カード1枚で生成する()
        {
            var observation = CreateObservation(remainingIds: new[] { "user", "npc-1" });

            Assert.That(observation.RemainingParticipantIds, Has.Count.EqualTo(2));
            Assert.That(observation.VisibleCards, Has.Count.EqualTo(1));
            Assert.That(observation.Participants.Count(item => item.IsEliminated), Is.EqualTo(3));
        }
    }
}
