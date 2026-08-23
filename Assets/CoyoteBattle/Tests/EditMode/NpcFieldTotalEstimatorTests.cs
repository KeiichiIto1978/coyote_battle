using System.Linq;
using NUnit.Framework;
using static CoyoteBattle.Domain.Tests.NpcTestHelper;

namespace CoyoteBattle.Domain.Tests
{
    public sealed class NpcFieldTotalEstimatorTests
    {
        /// <summary>
        /// 既定山札から公開札を除いた伏せ札候補で確率分布を作ることを保証します。
        /// </summary>
        [Test]
        public void Estimate_通常カードだけを観測する_合計確率100パーセントを返す()
        {
            var distribution = new NpcFieldTotalEstimator().Estimate(CreateObservation());

            Assert.That(distribution.Probabilities.Values.Sum(), Is.EqualTo(1d).Within(0.0000001d));
            Assert.That(distribution.Probabilities, Is.Not.Empty);
        }

        /// <summary>
        /// 公開された二倍カードを全ての伏せ札候補へ適用することを保証します。
        /// </summary>
        [Test]
        public void Estimate_二倍カードを観測する_全ての合計値が偶数になる()
        {
            var cards = new[]
            {
                new CardDeal("user", Card.CreateSpecial(100, CardKind.Double)),
                new CardDeal("npc-2", Card.CreateNumber(101, 5)),
                new CardDeal("npc-3", Card.CreateNumber(102, -5)),
                new CardDeal("npc-4", Card.CreateNumber(103, 0)),
            };

            var distribution = new NpcFieldTotalEstimator().Estimate(
                CreateObservation(visibleCards: cards)
            );

            Assert.That(distribution.Probabilities.Keys.All(total => total % 2 == 0), Is.True);
        }

        /// <summary>
        /// 現在値を成立させる確率を、分布の該当値以上の合計として返すことを保証します。
        /// </summary>
        [Test]
        public void ProbabilityAtLeast_分布の最小値と最大値を指定する_境界確率を返す()
        {
            var distribution = new NpcFieldTotalEstimator().Estimate(CreateObservation());
            var minimum = distribution.Probabilities.Keys.Min();
            var maximum = distribution.Probabilities.Keys.Max();

            Assert.That(
                distribution.ProbabilityAtLeast(minimum),
                Is.EqualTo(1d).Within(0.0000001d)
            );
            Assert.That(distribution.ProbabilityAtLeast(maximum), Is.GreaterThan(0d));
        }

        /// <summary>
        /// 公開された疑問カードについて、残り札を追加札候補として分布へ反映することを保証します。
        /// </summary>
        [Test]
        public void Estimate_疑問カードを観測する_複数の追加札結果を返す()
        {
            var cards = new[]
            {
                new CardDeal("user", Card.CreateSpecial(100, CardKind.Mystery)),
                new CardDeal("npc-2", Card.CreateNumber(101, 5)),
                new CardDeal("npc-3", Card.CreateNumber(102, -5)),
                new CardDeal("npc-4", Card.CreateNumber(103, 0)),
            };

            var distribution = new NpcFieldTotalEstimator().Estimate(
                CreateObservation(visibleCards: cards)
            );

            Assert.That(distribution.Probabilities.Count, Is.GreaterThan(5));
            Assert.That(distribution.Probabilities.Values.Sum(), Is.EqualTo(1d).Within(0.0000001d));
        }

        /// <summary>
        /// MAX→0を公開している場合に、伏せ札を含む最大数字1枚だけを除外することを保証します。
        /// </summary>
        [Test]
        public void Estimate_最大値無効カードと正数を観測する_合計上限を19にする()
        {
            var cards = new[]
            {
                new CardDeal("user", Card.CreateSpecial(100, CardKind.MaxToZero)),
                new CardDeal("npc-2", Card.CreateNumber(101, 10)),
                new CardDeal("npc-3", Card.CreateNumber(102, 5)),
                new CardDeal("npc-4", Card.CreateNumber(103, 4)),
            };

            var distribution = new NpcFieldTotalEstimator().Estimate(
                CreateObservation(visibleCards: cards)
            );

            Assert.That(distribution.Probabilities.Keys.Max(), Is.EqualTo(19));
        }

        /// <summary>
        /// 既定山札に1枚しかない夜カードを複数公開する不正構成を拒否することを保証します。
        /// </summary>
        [Test]
        public void Estimate_夜カードを2枚観測する_入力エラーになる()
        {
            var cards = new[]
            {
                new CardDeal("user", Card.CreateSpecial(100, CardKind.Night)),
                new CardDeal("npc-2", Card.CreateSpecial(101, CardKind.Night)),
                new CardDeal("npc-3", Card.CreateNumber(102, 5)),
                new CardDeal("npc-4", Card.CreateNumber(103, 4)),
            };

            Assert.That(
                () => new NpcFieldTotalEstimator().Estimate(CreateObservation(visibleCards: cards)),
                Throws.ArgumentException
            );
        }

        /// <summary>
        /// 使用済みの20を伏せ札と疑問カードの追加札の両候補から除外することを保証します。
        /// </summary>
        [Test]
        public void Estimate_20が使用済みで疑問カード候補になり得る_最大推定値を30にする()
        {
            var cards = new[]
            {
                new CardDeal("user", Card.CreateSpecial(100, CardKind.Double)),
                new CardDeal("npc-2", Card.CreateNumber(101, 5)),
                new CardDeal("npc-3", Card.CreateNumber(102, -5)),
                new CardDeal("npc-4", Card.CreateNumber(103, 0)),
            };
            var discardedCards = new[] { new NpcDiscardedCardCount(CardKind.Number, 20, 1) };

            var distribution = new NpcFieldTotalEstimator().Estimate(
                CreateObservation(visibleCards: cards, discardedCards: discardedCards)
            );

            Assert.That(distribution.Probabilities.Keys.Max(), Is.EqualTo(30));
        }

        /// <summary>
        /// 1枚物の20が公開札と使用済み札に重複する不正観測を拒否することを保証します。
        /// </summary>
        [Test]
        public void Estimate_20が公開札と使用済み札に存在する_入力エラーになる()
        {
            Assert.That(
                () =>
                    CreateObservation(
                        discardedCards: new[] { new NpcDiscardedCardCount(CardKind.Number, 20, 1) }
                    ),
                Throws.ArgumentException
            );
        }

        /// <summary>
        /// 未使用候補が疑問カード1枚だけなら、追加札時に再構築される使用済み札から分布を作ることを保証します。
        /// </summary>
        [Test]
        public void Estimate_使用済み31枚で伏せ札候補が疑問カードだけである_再構築後の追加札分布を返す()
        {
            var defaultCards = DefaultDeckFactory.Create();
            var participantIds = new[] { "user", "npc-2", "npc-3", "npc-4" };
            var visibleCards = defaultCards
                .Take(4)
                .Select((card, index) => new CardDeal(participantIds[index], card))
                .ToList()
                .AsReadOnly();
            var discardedCards = defaultCards
                .Skip(4)
                .Take(31)
                .GroupBy(card => new { card.Kind, card.Value })
                .Select(group => new NpcDiscardedCardCount(
                    group.Key.Kind,
                    group.Key.Value,
                    group.Count()
                ))
                .ToList()
                .AsReadOnly();

            var distribution = new NpcFieldTotalEstimator().Estimate(
                CreateObservation(visibleCards: visibleCards, discardedCards: discardedCards)
            );

            Assert.That(distribution.Probabilities, Is.Not.Empty);
            Assert.That(distribution.Probabilities.Values.Sum(), Is.EqualTo(1d).Within(0.0000001d));
            Assert.That(distribution.Probabilities.Keys.Max(), Is.EqualTo(120));
            Assert.That(
                distribution.ProbabilityAtLeast(120),
                Is.EqualTo(1d / 31d).Within(0.0000001d)
            );
        }
    }
}
