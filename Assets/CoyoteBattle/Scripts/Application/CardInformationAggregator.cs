using System;
using System.Collections.Generic;
using System.Linq;
using CoyoteBattle.Domain;

namespace CoyoteBattle.Application
{
    /// <summary>
    /// Domainの山札状態を、表示に必要なカード別枚数だけへ集約します。
    /// </summary>
    internal static class CardInformationAggregator
    {
        /// <summary>
        /// 初期デッキの表示順を保ち、初期枚数と現在の捨て札枚数を集約します。
        /// </summary>
        /// <param name="deck">集約対象の山札です。nullの場合は空の一覧を返します。</param>
        /// <returns>変更できないカード別枚数一覧です。</returns>
        public static IReadOnlyList<CardCountSnapshot> Create(Deck deck)
        {
            if (deck == null)
            {
                return Array.Empty<CardCountSnapshot>();
            }

            var initialGroups = DefaultDeckFactory
                .Create()
                .GroupBy(card => new CardIdentity(card.Kind, card.Value));
            var snapshots = initialGroups
                .Select(group => new CardCountSnapshot(
                    group.Key.Kind,
                    group.Key.Value,
                    group.Count(),
                    deck.DiscardPile.Count(card =>
                        card.Kind == group.Key.Kind && card.Value == group.Key.Value
                    )
                ))
                .ToList();
            return snapshots.AsReadOnly();
        }

        private readonly struct CardIdentity
        {
            /// <summary>
            /// 集約キーとなるカード種別と値を設定します。
            /// </summary>
            /// <param name="kind">カード種別です。</param>
            /// <param name="value">数字カードの値です。</param>
            public CardIdentity(CardKind kind, int? value)
            {
                Kind = kind;
                Value = value;
            }

            /// <summary>
            /// カード種別を取得します。
            /// </summary>
            public CardKind Kind { get; }

            /// <summary>
            /// 数字カードの値を取得します。
            /// </summary>
            public int? Value { get; }
        }
    }
}
