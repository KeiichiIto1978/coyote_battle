using System;
using System.Collections.Generic;
using System.Linq;
using CoyoteBattle.Domain;
using NUnit.Framework;

namespace CoyoteBattle.Application.Tests
{
    /// <summary>
    /// Applicationが山札の内部状態を漏らさず、初期構成と現在の使用済み枚数だけを公開することを保証します。
    /// </summary>
    public sealed class CardInformationTests
    {
        /// <summary>
        /// ゲーム開始前は山札が存在しないため、カード情報を空の変更不能一覧として返すことを保証します。
        /// </summary>
        [Test]
        public void CardInformation_ゲーム開始前である_空の一覧を返す()
        {
            var service = CreateService();

            var information = service.CardInformation;

            Assert.That(information, Is.Empty);
            Assert.That(information, Is.AssignableTo<IReadOnlyList<CardCountSnapshot>>());
        }

        /// <summary>
        /// 既定36枚をカード表記単位で集計し、通常0と夜カードを別行として公開することを保証します。
        /// </summary>
        [Test]
        public void CardInformation_ゲーム開始直後_初期36枚と使用済み0を15種で返す()
        {
            var service = StartIdentityDeckService();

            var information = service.CardInformation;

            Assert.That(information, Has.Count.EqualTo(15));
            Assert.That(information.Sum(item => item.InitialCount), Is.EqualTo(36));
            Assert.That(information.Sum(item => item.DiscardedCount), Is.Zero);
            Assert.That(
                information
                    .Single(item => item.Kind == CardKind.Number && item.Value == 0)
                    .InitialCount,
                Is.EqualTo(3)
            );
            Assert.That(
                information
                    .Single(item => item.Kind == CardKind.Night && item.Value == null)
                    .InitialCount,
                Is.EqualTo(1)
            );
        }

        /// <summary>
        /// 配布済みの現在ラウンド札は、ラウンド終了前の使用済み枚数へ含めないことを保証します。
        /// </summary>
        [Test]
        public void CardInformation_第1ラウンドを宣言中_場札5枚を使用済みに数えない()
        {
            var service = StartIdentityDeckService();

            var discardedCount = service.CardInformation.Sum(item => item.DiscardedCount);

            Assert.That(service.CurrentCards, Has.Count.EqualTo(5));
            Assert.That(discardedCount, Is.Zero);
        }

        /// <summary>
        /// 夜カードのない通常ラウンド完了後、公開結果の5枚を使用済みへ種別別に反映することを保証します。
        /// </summary>
        [Test]
        public void CardInformation_通常ラウンドを完了する_回収した5枚を種別別に加算する()
        {
            var service = StartIdentityDeckService();
            Assert.That(service.TryDeclareNumber("user", int.MaxValue), Is.True);
            Assert.That(service.TryDeclareCoyote("npc-1"), Is.True);

            var information = service.CardInformation;

            Assert.That(information.Sum(item => item.DiscardedCount), Is.EqualTo(5));
            foreach (
                var group in service.LastRoundResult.DealtCards.GroupBy(item => new
                {
                    item.Card.Kind,
                    item.Card.Value,
                })
            )
            {
                Assert.That(
                    information
                        .Single(item =>
                            item.Kind == group.Key.Kind && item.Value == group.Key.Value
                        )
                        .DiscardedCount,
                    Is.EqualTo(group.Count())
                );
            }
        }

        /// <summary>
        /// 公開一覧と各要素を呼び出し側から変更できず、再取得しても集計状態を保護することを保証します。
        /// </summary>
        [Test]
        public void CardInformation_公開一覧を変更しようとする_変更を拒否して値を維持する()
        {
            var service = StartIdentityDeckService();
            var information = (IList<CardCountSnapshot>)service.CardInformation;

            Assert.That(() => information.RemoveAt(0), Throws.TypeOf<NotSupportedException>());
            Assert.That(service.CardInformation.Sum(item => item.InitialCount), Is.EqualTo(36));
        }

        /// <summary>
        /// 夜カードが出たラウンドでは全山札再構築により、それまでの使用済み集計も0へ戻ることを保証します。
        /// </summary>
        [Test]
        public void CardInformation_夜カードのラウンドを完了する_全表示単位の使用済みを0へ戻す()
        {
            var service = StartIdentityDeckService();
            for (var round = 0; round < 6; round++)
            {
                CompleteRoundWithNextParticipantLoss(service);
                Assert.That(service.TryStartNextRound(), Is.True);
            }

            Assert.That(service.CardInformation.Sum(item => item.DiscardedCount), Is.EqualTo(30));
            CompleteRoundWithNextParticipantLoss(service);

            Assert.That(service.CardInformation.Sum(item => item.DiscardedCount), Is.Zero);
        }

        /// <summary>
        /// 現在手番が1を宣言し、次の参加者がコヨーテを宣言してラウンドを完了します。
        /// </summary>
        /// <param name="service">宣言中のゲーム進行です。</param>
        private static void CompleteRoundWithNextParticipantLoss(GameFlowService service)
        {
            Assert.That(service.TryDeclareNumber(service.CurrentParticipantId, 1), Is.True);
            Assert.That(service.TryDeclareCoyote(service.CurrentParticipantId), Is.True);
        }

        /// <summary>
        /// 開始者をユーザー、山札順を既定順のままにしたゲームを開始します。
        /// </summary>
        private static GameFlowService StartIdentityDeckService()
        {
            var service = CreateService();
            Assert.That(service.TryStartNewGame(), Is.True);
            return service;
        }

        /// <summary>
        /// 最初だけ開始位置0、その後はシャッフル対象自身を返す乱数でゲームを生成します。
        /// </summary>
        private static GameFlowService CreateService()
        {
            return new GameFlowService(
                new FirstZeroThenIdentityRandomSource(),
                new FirstZeroThenIdentityRandomSource()
            );
        }

        private sealed class FirstZeroThenIdentityRandomSource : IRandomSource
        {
            private bool _hasReturnedStarter;

            /// <summary>
            /// 最初は開始位置0、以後はFisher-Yatesで交換しない位置を返します。
            /// </summary>
            public int Next(int maxExclusive)
            {
                if (!_hasReturnedStarter)
                {
                    _hasReturnedStarter = true;
                    return 0;
                }

                return maxExclusive - 1;
            }
        }
    }
}
