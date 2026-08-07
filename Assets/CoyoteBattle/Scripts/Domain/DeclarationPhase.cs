using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace CoyoteBattle.Domain
{
    /// <summary>
    /// 1ラウンドの数字宣言履歴、コヨーテ宣言、敗者判定を管理します。
    /// </summary>
    public sealed class DeclarationPhase
    {
        private readonly HashSet<string> _eligibleParticipantIdSet;
        private readonly ReadOnlyCollection<string> _eligibleParticipantIds;
        private readonly List<NumberDeclaration> _history = new List<NumberDeclaration>();
        private readonly ReadOnlyCollection<NumberDeclaration> _readOnlyHistory;

        /// <summary>
        /// ラウンド開始時点の宣言参加者から、数字宣言受付中のフェーズを生成します。
        /// </summary>
        /// <param name="participantIds">重複しない2名以上の残存参加者IDです。</param>
        public DeclarationPhase(IReadOnlyList<string> participantIds)
        {
            if (participantIds == null)
            {
                throw new ArgumentNullException(nameof(participantIds));
            }

            if (participantIds.Count < 2)
            {
                throw new ArgumentException(
                    "宣言参加者は2名以上指定してください。",
                    nameof(participantIds)
                );
            }

            var participantIdSnapshot = new List<string>(participantIds.Count);
            _eligibleParticipantIdSet = new HashSet<string>(StringComparer.Ordinal);
            foreach (var participantId in participantIds)
            {
                if (string.IsNullOrWhiteSpace(participantId))
                {
                    throw new ArgumentException(
                        "宣言参加者の識別子を指定してください。",
                        nameof(participantIds)
                    );
                }

                if (!_eligibleParticipantIdSet.Add(participantId))
                {
                    throw new ArgumentException(
                        "宣言参加者の識別子は重複できません。",
                        nameof(participantIds)
                    );
                }

                participantIdSnapshot.Add(participantId);
            }

            _eligibleParticipantIds = participantIdSnapshot.AsReadOnly();
            _readOnlyHistory = _history.AsReadOnly();
            Status = DeclarationPhaseStatus.AcceptingNumberDeclarations;
        }

        /// <summary>
        /// ラウンド開始時点で宣言に参加できる識別子を指定順で取得します。
        /// </summary>
        public IReadOnlyList<string> EligibleParticipantIds => _eligibleParticipantIds;

        /// <summary>
        /// 現在の宣言フェーズ状態を取得します。
        /// </summary>
        public DeclarationPhaseStatus Status { get; private set; }

        /// <summary>
        /// 最後に成功した数字宣言を取得します。宣言前はnullです。
        /// </summary>
        public NumberDeclaration CurrentDeclaration { get; private set; }

        /// <summary>
        /// 成功した数字宣言だけを宣言順に取得します。
        /// </summary>
        public IReadOnlyList<NumberDeclaration> History => _readOnlyHistory;

        /// <summary>
        /// コヨーテ宣言者の識別子を取得します。宣言前はnullです。
        /// </summary>
        public string CoyoteDeclarerId { get; private set; }

        /// <summary>
        /// 参加資格、宣言者、値、状態が有効な場合に数字宣言を履歴へ追加します。
        /// </summary>
        /// <param name="participantId">数字を宣言する参加者IDです。</param>
        /// <param name="value">直前より大きい1以上の宣言値です。</param>
        /// <returns>宣言を受理した場合はtrue、それ以外はfalseです。</returns>
        public bool TryDeclareNumber(string participantId, int value)
        {
            ValidateParticipantId(participantId);

            if (
                Status != DeclarationPhaseStatus.AcceptingNumberDeclarations
                || !_eligibleParticipantIdSet.Contains(participantId)
                || value < 1
                || (
                    CurrentDeclaration != null
                    && (
                        string.Equals(
                            CurrentDeclaration.ParticipantId,
                            participantId,
                            StringComparison.Ordinal
                        )
                        || value <= CurrentDeclaration.Value
                    )
                )
            )
            {
                return false;
            }

            var declaration = new NumberDeclaration(participantId, value);
            _history.Add(declaration);
            CurrentDeclaration = declaration;
            return true;
        }

        /// <summary>
        /// 有効な数字宣言に対して別の参加者がコヨーテを宣言し、宣言フェーズを終了します。
        /// </summary>
        /// <param name="participantId">コヨーテを宣言する参加者IDです。</param>
        /// <returns>コヨーテ宣言を受理した場合はtrue、それ以外はfalseです。</returns>
        public bool TryDeclareCoyote(string participantId)
        {
            ValidateParticipantId(participantId);

            if (
                Status != DeclarationPhaseStatus.AcceptingNumberDeclarations
                || CurrentDeclaration == null
                || !_eligibleParticipantIdSet.Contains(participantId)
                || string.Equals(
                    CurrentDeclaration.ParticipantId,
                    participantId,
                    StringComparison.Ordinal
                )
            )
            {
                return false;
            }

            CoyoteDeclarerId = participantId;
            Status = DeclarationPhaseStatus.CoyoteDeclared;
            return true;
        }

        /// <summary>
        /// 特殊効果まで解決済みの実合計と最終宣言値を比較し、ラウンド敗者IDを返します。
        /// </summary>
        /// <param name="actualTotal">負数と0を含む、カード効果解決後の実合計です。</param>
        /// <returns>最後の数字宣言者またはコヨーテ宣言者のIDです。</returns>
        /// <exception cref="InvalidOperationException">
        /// コヨーテ宣言前で敗者候補が確定していない場合に送出します。
        /// </exception>
        public string DetermineLoser(int actualTotal)
        {
            if (Status != DeclarationPhaseStatus.CoyoteDeclared)
            {
                throw new InvalidOperationException(
                    "コヨーテ宣言後にラウンド敗者を判定してください。"
                );
            }

            return CurrentDeclaration.Value > actualTotal
                ? CurrentDeclaration.ParticipantId
                : CoyoteDeclarerId;
        }

        /// <summary>
        /// 操作に使用する参加者識別子が入力として有効であることを検証します。
        /// </summary>
        /// <param name="participantId">検証対象の参加者識別子です。</param>
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
