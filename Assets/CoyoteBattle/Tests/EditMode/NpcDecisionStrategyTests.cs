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
        /// 強気型が支持可能な最大値へ飛ばず、直前値から3だけ上げることを保証します。
        /// </summary>
        [Test]
        public void Decide_強気型で5が宣言済みである_8を宣言する()
        {
            var decision = new AggressiveNpcDecisionStrategy(new NpcFieldTotalEstimator()).Decide(
                CreateObservation(declarations: new[] { 5 })
            );

            Assert.That(decision.Kind, Is.EqualTo(NpcDecisionKind.Number));
            Assert.That(decision.Number, Is.EqualTo(8));
        }

        /// <summary>
        /// 初手では0を基準にし、強気型は3、慎重型は1まで上げることを保証します。
        /// </summary>
        [Test]
        public void Decide_強気型と慎重型の初手である_0を基準に3と1を宣言する()
        {
            var observation = CreateObservation();

            var aggressive = new AggressiveNpcDecisionStrategy(new NpcFieldTotalEstimator()).Decide(
                observation
            );
            var cautious = new CautiousNpcDecisionStrategy(new NpcFieldTotalEstimator()).Decide(
                observation
            );

            Assert.That(aggressive.Number, Is.EqualTo(3));
            Assert.That(cautious.Number, Is.EqualTo(1));
        }

        /// <summary>
        /// 慎重型が支持可能な最大値へ飛ばず、直前値から1だけ上げることを保証します。
        /// </summary>
        [Test]
        public void Decide_慎重型で5が宣言済みである_6を宣言する()
        {
            var decision = new CautiousNpcDecisionStrategy(new NpcFieldTotalEstimator()).Decide(
                CreateObservation(declarations: new[] { 5 })
            );

            Assert.That(decision.Kind, Is.EqualTo(NpcDecisionKind.Number));
            Assert.That(decision.Number, Is.EqualTo(6));
        }

        /// <summary>
        /// ギャンブル型が選んだ成立確率に対応して、上げ幅4、2、1を使うことを保証します。
        /// </summary>
        [TestCase(0, 9)]
        [TestCase(1, 7)]
        [TestCase(2, 6)]
        public void Decide_ギャンブル型で5が宣言済みである_閾値別の上げ幅を使う(
            int randomValue,
            int expectedNumber
        )
        {
            var decision = new GamblingNpcDecisionStrategy(
                new NpcFieldTotalEstimator(),
                new FixedRandomSource(randomValue)
            ).Decide(CreateObservation(declarations: new[] { 5 }));

            Assert.That(decision.Kind, Is.EqualTo(NpcDecisionKind.Number));
            Assert.That(decision.Number, Is.EqualTo(expectedNumber));
        }

        /// <summary>
        /// 分析型が50%以上70%未満の成立確率では、直前値から2だけ上げることを保証します。
        /// </summary>
        [Test]
        public void Decide_分析型の要求確率が55パーセントで5が宣言済みである_7を宣言する()
        {
            var decision = new AnalyticalNpcDecisionStrategy(new NpcFieldTotalEstimator()).Decide(
                CreateObservation(declarations: new[] { 5 })
            );

            Assert.That(decision.Kind, Is.EqualTo(NpcDecisionKind.Number));
            Assert.That(decision.Number, Is.EqualTo(7));
        }

        /// <summary>
        /// 分析型が50%未満では3、70%以上では1を最大上げ幅に使うことを保証します。
        /// </summary>
        [Test]
        public void Decide_分析型の要求確率が45と70パーセントである_上げ幅3と1を使う()
        {
            var strategy = new AnalyticalNpcDecisionStrategy(new NpcFieldTotalEstimator());

            var boldDecision = strategy.Decide(
                CreateObservation(declarations: new[] { 5 }, userLife: 1)
            );
            var carefulDecision = strategy.Decide(
                CreateObservation(declarations: new[] { 5 }, actorLife: 1)
            );

            Assert.That(boldDecision.Number, Is.EqualTo(8));
            Assert.That(carefulDecision.Number, Is.EqualTo(6));
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
