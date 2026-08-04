using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace CoyoteBattle.Domain
{
    /// <summary>
    /// Version 1の固定参加者と、そのライフ・脱落状態を一貫して管理します。
    /// </summary>
    public sealed class ParticipantRoster
    {
        private readonly ReadOnlyCollection<Participant> _participants;

        /// <summary>
        /// 検証済みの固定5名から参加者管理を生成します。
        /// </summary>
        /// <param name="participants">ユーザー1名とNPC4名の確定順一覧です。</param>
        internal ParticipantRoster(IReadOnlyList<Participant> participants)
        {
            if (participants == null)
            {
                throw new ArgumentNullException(nameof(participants));
            }

            if (
                participants.Count != 5
                || participants.Any(participant => participant == null)
                || participants.Count(participant => participant.Kind == ParticipantKind.User) != 1
                || participants.Count(participant => participant.Kind == ParticipantKind.Npc) != 4
                || participants.Select(participant => participant.Id).Distinct().Count() != 5
            )
            {
                throw new ArgumentException(
                    "参加者は識別子が一意なユーザー1名とNPC4名で指定してください。",
                    nameof(participants)
                );
            }

            _participants = new List<Participant>(participants).AsReadOnly();
        }

        /// <summary>
        /// 脱落者を含む固定5名を生成順で取得します。
        /// </summary>
        public IReadOnlyList<Participant> Participants => _participants;

        /// <summary>
        /// ライフが残っている参加者だけを生成時の相対順で取得します。
        /// </summary>
        public IReadOnlyList<Participant> RemainingParticipants =>
            _participants.Where(participant => !participant.IsEliminated).ToList().AsReadOnly();

        /// <summary>
        /// 識別子に対応する参加者を検索します。
        /// </summary>
        /// <param name="participantId">検索する空白ではない参加者識別子です。</param>
        /// <param name="participant">成功時に見つかった参加者です。</param>
        /// <returns>対応する参加者が存在する場合はtrue、それ以外はfalseです。</returns>
        public bool TryGetParticipant(string participantId, out Participant participant)
        {
            ValidateParticipantId(participantId);
            participant = _participants.FirstOrDefault(item =>
                string.Equals(item.Id, participantId, StringComparison.Ordinal)
            );
            return participant != null;
        }

        /// <summary>
        /// 識別子で指定された残存参加者へラウンド敗北を適用します。
        /// </summary>
        /// <param name="participantId">敗者となった参加者識別子です。</param>
        /// <returns>対象者のライフを減らした場合はtrue、未知または脱落済みならfalseです。</returns>
        public bool TryApplyLoss(string participantId)
        {
            if (!TryGetParticipant(participantId, out var participant))
            {
                return false;
            }

            return participant.TryLoseLife();
        }

        /// <summary>
        /// 参加者検索に使う識別子が有効であることを検証します。
        /// </summary>
        /// <param name="participantId">検証する識別子です。</param>
        private static void ValidateParticipantId(string participantId)
        {
            if (string.IsNullOrWhiteSpace(participantId))
            {
                throw new ArgumentException(
                    "参加者の識別子を指定してください。",
                    nameof(participantId)
                );
            }
        }
    }
}
