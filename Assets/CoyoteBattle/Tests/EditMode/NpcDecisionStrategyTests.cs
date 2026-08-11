using NUnit.Framework;
using static CoyoteBattle.Domain.Tests.NpcTestHelper;

namespace CoyoteBattle.Domain.Tests
{
    public sealed class NpcDecisionStrategyTests
    {
        /// <summary>
        /// 同じ観測では強気型が慎重型以上の数字を選ぶことを保証します。
        /// </summary>
        [Test]
        public void Decide_同じ初手観測を指定する_強気型が慎重型以上を宣言する()
        {
            var observation = CreateObservation();
            var estimator = new NpcFieldTotalEstimator();
            var aggressive = new AggressiveNpcDecisionStrategy(estimator);
            var cautious = new CautiousNpcDecisionStrategy(estimator);

            var aggressiveDecision = aggressive.Decide(observation);
            var cautiousDecision = cautious.Decide(observation);

            Assert.That(aggressiveDecision.Kind, Is.EqualTo(NpcDecisionKind.Number));
            Assert.That(cautiousDecision.Kind, Is.EqualTo(NpcDecisionKind.Number));
            Assert.That(
                aggressiveDecision.Number,
                Is.GreaterThanOrEqualTo(cautiousDecision.Number)
            );
        }

        /// <summary>
        /// int上限後は全タイプがオーバーフローせずコヨーテを選ぶことを保証します。
        /// </summary>
        [Test]
        public void Decide_int上限が宣言済みである_全タイプがコヨーテを返す()
        {
            var observation = CreateObservation(declarations: new[] { int.MaxValue });
            var estimator = new NpcFieldTotalEstimator();
            INpcDecisionStrategy[] strategies =
            {
                new AggressiveNpcDecisionStrategy(estimator),
                new CautiousNpcDecisionStrategy(estimator),
                new GamblingNpcDecisionStrategy(estimator, new FixedRandomSource(0)),
                new AnalyticalNpcDecisionStrategy(estimator),
            };

            foreach (var strategy in strategies)
            {
                Assert.That(strategy.Decide(observation).Kind, Is.EqualTo(NpcDecisionKind.Coyote));
            }
        }

        /// <summary>
        /// ギャンブル型が固定乱数を1回だけ使い、同じ入力から判断を再現することを保証します。
        /// </summary>
        [Test]
        public void Decide_ギャンブル型へ固定乱数を渡す_同じ判断を再現する()
        {
            var observation = CreateObservation();
            var firstRandom = new FixedRandomSource(1);
            var secondRandom = new FixedRandomSource(1);

            var first = new GamblingNpcDecisionStrategy(
                new NpcFieldTotalEstimator(),
                firstRandom
            ).Decide(observation);
            var second = new GamblingNpcDecisionStrategy(
                new NpcFieldTotalEstimator(),
                secondRandom
            ).Decide(observation);

            Assert.That(first.Kind, Is.EqualTo(second.Kind));
            Assert.That(first.Number, Is.EqualTo(second.Number));
            Assert.That(firstRandom.CallCount, Is.EqualTo(1));
            Assert.That(secondRandom.CallCount, Is.EqualTo(1));
        }

        /// <summary>
        /// int上限の直前でも加算によるオーバーフローを起こさず、コヨーテを返すことを保証します。
        /// </summary>
        [Test]
        public void Decide_int上限の1つ前が宣言済みである_コヨーテを返す()
        {
            var observation = CreateObservation(declarations: new[] { int.MaxValue - 1 });

            var decision = new AggressiveNpcDecisionStrategy(new NpcFieldTotalEstimator()).Decide(
                observation
            );

            Assert.That(decision.Kind, Is.EqualTo(NpcDecisionKind.Coyote));
        }

        /// <summary>
        /// 乱数源が排他的上限以上を返した場合に、契約違反として拒否することを保証します。
        /// </summary>
        [Test]
        public void Decide_ギャンブル型乱数が3を返す_無効操作例外になる()
        {
            var strategy = new GamblingNpcDecisionStrategy(
                new NpcFieldTotalEstimator(),
                new FixedRandomSource(3)
            );

            Assert.That(
                () => strategy.Decide(CreateObservation()),
                Throws.InvalidOperationException
            );
        }

        /// <summary>
        /// 初手の推定合計が1未満でも、ルール上必要な最低値1を宣言することを保証します。
        /// </summary>
        [Test]
        public void Decide_負数中心の初手観測である_数字1以上を返す()
        {
            var cards = new[]
            {
                new CardDeal("user", Card.CreateNumber(100, -10)),
                new CardDeal("npc-2", Card.CreateNumber(101, -5)),
                new CardDeal("npc-3", Card.CreateNumber(102, -5)),
                new CardDeal("npc-4", Card.CreateNumber(103, 0)),
            };

            var decision = new CautiousNpcDecisionStrategy(new NpcFieldTotalEstimator()).Decide(
                CreateObservation(visibleCards: cards)
            );

            Assert.That(decision.Kind, Is.EqualTo(NpcDecisionKind.Number));
            Assert.That(decision.Number, Is.GreaterThanOrEqualTo(1));
        }

        /// <summary>
        /// 分析型が自分のライフと残存人数を仕様どおり成立確率へ反映することを保証します。
        /// </summary>
        [Test]
        public void CalculateRequiredProbability_ライフと残存人数を変える_補正値を返す()
        {
            var strategy = new AnalyticalNpcDecisionStrategy(new NpcFieldTotalEstimator());
            var fiveParticipants = CreateObservation(actorLife: 3);
            var twoParticipants = CreateObservation(
                actorLife: 1,
                remainingIds: new[] { "user", "npc-1" }
            );

            Assert.That(
                strategy.CalculateRequiredProbability(fiveParticipants),
                Is.EqualTo(0.55d).Within(0.0000001d)
            );
            Assert.That(
                strategy.CalculateRequiredProbability(twoParticipants),
                Is.EqualTo(0.85d).Within(0.0000001d)
            );
        }

        /// <summary>
        /// 分析型が直前の宣言上昇幅5を慎重方向の補正へ反映することを保証します。
        /// </summary>
        [Test]
        public void CalculateRequiredProbability_宣言が5上昇する_10ポイント加算する()
        {
            var strategy = new AnalyticalNpcDecisionStrategy(new NpcFieldTotalEstimator());
            var smallRaise = CreateObservation(actorId: "npc-2", declarations: new[] { 5, 9 });
            var largeRaise = CreateObservation(actorId: "npc-2", declarations: new[] { 5, 10 });

            Assert.That(
                strategy.CalculateRequiredProbability(largeRaise)
                    - strategy.CalculateRequiredProbability(smallRaise),
                Is.EqualTo(0.10d).Within(0.0000001d)
            );
        }

        private sealed class FixedRandomSource : IRandomSource
        {
            private readonly int _value;

            internal FixedRandomSource(int value)
            {
                _value = value;
            }

            internal int CallCount { get; private set; }

            /// <summary>
            /// 指定済みの値を返し、呼び出し回数を記録します。
            /// </summary>
            public int Next(int exclusiveUpperBound)
            {
                CallCount++;
                return _value;
            }
        }
    }
}
