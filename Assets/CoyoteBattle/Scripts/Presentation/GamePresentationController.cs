using System.Collections;
using System.Linq;
using CoyoteBattle.Application;
using CoyoteBattle.Domain;
using UnityEngine;
using UnityEngine.UIElements;
using static CoyoteBattle.Presentation.PresentationUiFactory;

namespace CoyoteBattle.Presentation
{
    /// <summary>
    /// Applicationの状態を4画面へ投影し、ユーザー操作とNPC自動進行を仲介します。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GamePresentationController : MonoBehaviour
    {
        private const string UserId = "user";
        private const float NpcThinkingSeconds = 0.8f;
        private GameFlowService _game;
        private UIDocument _document;
        private VisualElement _root;
        private VisualElement _titleScreen;
        private VisualElement _battleScreen;
        private VisualElement _resultScreen;
        private VisualElement _gameOverDialog;
        private VisualElement _npcRow;
        private VisualElement _resultCards;
        private Label _roundLabel;
        private Label _statusLabel;
        private Label _declarationLabel;
        private Label _userLifeLabel;
        private Label _userCardLabel;
        private Label _errorLabel;
        private Label _resultSummary;
        private Label _outcomeLabel;
        private TextField _numberInput;
        private Button _declareButton;
        private Button _coyoteButton;
        private Button _nextRoundButton;
        private int _operationGeneration;
        private bool _initialized;
        private Rect _lastSafeArea;
        private Font _interfaceFont;

        /// <summary>
        /// Unityライフサイクルから画面を初期化します。
        /// </summary>
        private void Awake()
        {
            Initialize();
        }

        /// <summary>
        /// PlayModeテストから通常と同じComposition Rootを明示初期化します。
        /// </summary>
        public void InitializeForTests()
        {
            Initialize();
        }

        /// <summary>
        /// PlayModeテストでボタン操作と同じ開始処理を実行します。
        /// </summary>
        public void StartGameForTests()
        {
            StartNewGame();
        }

        private void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;
            Screen.orientation = ScreenOrientation.LandscapeLeft;
            PresentationRenderingCamera.EnsureExists();
            _game = CreateGame();
            _document = GetComponent<UIDocument>() ?? gameObject.AddComponent<UIDocument>();
            _document.panelSettings = CreatePanelSettings();
            _root = _document.rootVisualElement;
            _interfaceFont = Font.CreateDynamicFontFromOSFont(
                new[] { "Noto Sans CJK JP", "Noto Sans JP", "Yu Gothic UI", "Arial" },
                32
            );
            if (_interfaceFont != null)
            {
                _root.style.unityFontDefinition = new StyleFontDefinition(
                    FontDefinition.FromFont(_interfaceFont)
                );
            }
            BuildUi();
            ShowTitle();
            ApplySafeArea();
        }

        private void Update()
        {
            if (_lastSafeArea != Screen.safeArea)
            {
                ApplySafeArea();
            }
        }
        private void OnDestroy()
        {
            CancelPendingOperations();
            if (_document != null && _document.panelSettings != null)
            {
                Destroy(_document.panelSettings);
            }
            if (_interfaceFont != null)
            {
                Destroy(_interfaceFont);
            }
        }
        private static GameFlowService CreateGame()
        {
            return new GameFlowService(new SystemRandomSource(), new SystemRandomSource());
        }
        private static PanelSettings CreatePanelSettings()
        {
            var settings = ScriptableObject.CreateInstance<PanelSettings>();
            settings.name = "RuntimePanelSettings";
            settings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            settings.referenceResolution = new Vector2Int(1920, 1080);
            settings.match = 0.5f;
            settings.sortingOrder = 10;
            return settings;
        }

        private void BuildUi()
        {
            _root.name = "safe-area-root";
            SetBackground(_root, "Art/TableBackground");
            _root.style.flexGrow = 1;
            _root.style.color = new Color(1f, 0.95f, 0.82f);

            _titleScreen = CreateScreen("title-screen");
            _titleScreen.style.justifyContent = Justify.Center;
            _titleScreen.style.alignItems = Align.Center;
            var emblem = new VisualElement { name = "title-emblem" };
            emblem.style.width = 210;
            emblem.style.height = 210;
            SetBackground(emblem, "Art/PawEmblem");
            _titleScreen.Add(emblem);
            _titleScreen.Add(CreateLabel("COYOTE BATTLE", 64, "title-label"));
            _titleScreen.Add(CreateLabel("見えない自分の札を読み、荒野の勝負を生き残れ", 24));
            var start = CreateButton("ゲーム開始", StartNewGame, "start-game-button");
            start.style.width = 360;
            start.style.height = 76;
            _titleScreen.Add(start);

            _battleScreen = CreateScreen("battle-screen");
            _roundLabel = CreateLabel(string.Empty, 28, "round-label");
            _roundLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            _battleScreen.Add(_roundLabel);
            _npcRow = new VisualElement { name = "npc-row" };
            _npcRow.style.flexDirection = FlexDirection.Row;
            _npcRow.style.justifyContent = Justify.SpaceAround;
            _npcRow.style.height = Length.Percent(47);
            _battleScreen.Add(_npcRow);
            var centerPanel = CreatePanel("declaration-panel");
            centerPanel.style.alignSelf = Align.Center;
            centerPanel.style.width = Length.Percent(56);
            centerPanel.style.height = 92;
            _statusLabel = CreateLabel(string.Empty, 23, "status-label");
            _declarationLabel = CreateLabel(string.Empty, 28, "declaration-label");
            centerPanel.Add(_statusLabel);
            centerPanel.Add(_declarationLabel);
            _battleScreen.Add(centerPanel);
            _battleScreen.Add(BuildUserArea());

            _resultScreen = CreateScreen("round-result-screen");
            _resultScreen.style.backgroundColor = new Color(0.02f, 0.06f, 0.1f, 0.92f);
            _resultScreen.Add(CreateLabel("ラウンド結果", 44));
            var resultBody = new VisualElement();
            resultBody.style.flexDirection = FlexDirection.Row;
            resultBody.style.flexGrow = 1;
            _resultCards = new VisualElement { name = "result-cards" };
            _resultCards.style.flexDirection = FlexDirection.Row;
            _resultCards.style.flexWrap = Wrap.Wrap;
            _resultCards.style.width = Length.Percent(62);
            _resultSummary = CreateLabel(string.Empty, 25, "result-summary");
            _resultSummary.style.whiteSpace = WhiteSpace.Normal;
            _resultSummary.style.width = Length.Percent(38);
            resultBody.Add(_resultCards);
            resultBody.Add(_resultSummary);
            _resultScreen.Add(resultBody);
            _nextRoundButton = CreateButton("次のラウンドへ", StartNextRound, "next-round-button");
            _resultScreen.Add(_nextRoundButton);

            _gameOverDialog = CreatePanel("game-over-dialog");
            _gameOverDialog.style.position = Position.Absolute;
            _gameOverDialog.style.width = 620;
            _gameOverDialog.style.height = 360;
            _gameOverDialog.style.left = Length.Percent(34);
            _gameOverDialog.style.top = Length.Percent(32);
            _gameOverDialog.style.justifyContent = Justify.Center;
            _outcomeLabel = CreateLabel(string.Empty, 52, "outcome-label");
            _gameOverDialog.Add(_outcomeLabel);
            _gameOverDialog.Add(CreateButton("もう一度遊ぶ", StartNewGame, "restart-button"));
            _gameOverDialog.Add(CreateButton("タイトルへ戻る", ReturnToTitle, "return-title-button"));

            _root.Add(_titleScreen);
            _root.Add(_battleScreen);
            _root.Add(_resultScreen);
            _root.Add(_gameOverDialog);
        }

        private VisualElement BuildUserArea()
        {
            var row = new VisualElement { name = "user-area" };
            row.style.flexDirection = FlexDirection.Row;
            row.style.flexGrow = 1;
            row.style.alignItems = Align.Center;
            row.style.justifyContent = Justify.Center;
            var userPanel = CreatePanel();
            userPanel.style.width = 280;
            _userLifeLabel = CreateLabel(string.Empty, 22, "user-life");
            _userCardLabel = CreateLabel("伏せ札", 30, "user-card");
            _userCardLabel.style.height = 100;
            _userCardLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            SetBackground(_userCardLabel, "Art/CardBack");
            userPanel.Add(CreateLabel("あなた", 26));
            userPanel.Add(_userLifeLabel);
            userPanel.Add(_userCardLabel);
            row.Add(userPanel);

            var controls = CreatePanel("controls");
            controls.style.width = 600;
            controls.style.marginLeft = 24;
            _numberInput = new TextField("数字") { name = "number-input", maxLength = 10 };
            _numberInput.style.height = 55;
            _numberInput.style.fontSize = 24;
            var buttons = new VisualElement();
            buttons.style.flexDirection = FlexDirection.Row;
            _declareButton = CreateButton("数字を宣言", DeclareNumber, "declare-number-button");
            _coyoteButton = CreateButton("コヨーテ！", DeclareCoyote, "declare-coyote-button");
            buttons.Add(_declareButton);
            buttons.Add(_coyoteButton);
            _errorLabel = CreateLabel(string.Empty, 19, "input-error");
            _errorLabel.style.color = new Color(1f, 0.55f, 0.35f);
            controls.Add(_numberInput);
            controls.Add(buttons);
            controls.Add(_errorLabel);
            row.Add(controls);
            return row;
        }

        private void StartNewGame()
        {
            CancelPendingOperations();
            _game = CreateGame();
            if (!_game.TryStartNewGame())
            {
                return;
            }

            _numberInput.value = string.Empty;
            _errorLabel.text = string.Empty;
            ShowBattle();
            ContinueNpcTurns();
        }

        private void StartNextRound()
        {
            SetButtonEnabled(_nextRoundButton, false);
            if (!_game.TryStartNextRound())
            {
                return;
            }

            ShowBattle();
            ContinueNpcTurns();
        }

        private void DeclareNumber()
        {
            SetInputEnabled(false);
            var previous = _game.DeclarationHistory.LastOrDefault()?.Value;
            if (!NumberDeclarationInputValidator.TryValidate(_numberInput.value, previous, out var value, out var error))
            {
                _errorLabel.text = error;
                RefreshBattle();
                return;
            }

            if (!_game.TryDeclareNumber(UserId, value))
            {
                _errorLabel.text = "現在は数字を宣言できません。";
                RefreshBattle();
                return;
            }

            _numberInput.value = string.Empty;
            _errorLabel.text = string.Empty;
            RefreshBattle();
            ContinueNpcTurns();
        }

        private void DeclareCoyote()
        {
            SetInputEnabled(false);
            if (_game.TryDeclareCoyote(UserId))
            {
                ShowResult();
            }
            else
            {
                _errorLabel.text = "まだコヨーテを宣言できません。";
                RefreshBattle();
            }
        }

        private void ContinueNpcTurns()
        {
            if (_game.State == GameFlowState.Declaring && _game.CurrentParticipantId != UserId)
            {
                StartCoroutine(ExecuteNpcTurns(_operationGeneration));
            }
        }

        private IEnumerator ExecuteNpcTurns(int generation)
        {
            while (
                generation == _operationGeneration
                && _game.State == GameFlowState.Declaring
                && _game.CurrentParticipantId != UserId
            )
            {
                _statusLabel.text = $"{PresentationText.ParticipantName(_game.CurrentParticipantId)} が考え中…";
                SetInputEnabled(false);
                yield return new WaitForSeconds(NpcThinkingSeconds);
                if (generation != _operationGeneration || !_game.TryExecuteCurrentNpcTurn())
                {
                    yield break;
                }

                if (_game.State == GameFlowState.Declaring)
                {
                    RefreshBattle();
                }
            }

            if (generation == _operationGeneration)
            {
                if (_game.State == GameFlowState.Declaring) RefreshBattle();
                else ShowResult();
            }
        }

        private void ShowTitle()
        {
            SetVisible(_titleScreen, true);
            SetVisible(_battleScreen, false);
            SetVisible(_resultScreen, false);
            SetVisible(_gameOverDialog, false);
        }

        private void ShowBattle()
        {
            SetVisible(_titleScreen, false);
            SetVisible(_battleScreen, true);
            SetVisible(_resultScreen, false);
            SetVisible(_gameOverDialog, false);
            RefreshBattle();
        }

        private void RefreshBattle()
        {
            _roundLabel.text = $"ROUND {_game.RoundNumber}";
            var last = _game.DeclarationHistory.LastOrDefault();
            _declarationLabel.text = last == null
                ? "最初の数字を宣言してください"
                : $"直前の宣言：{last.Value}（{PresentationText.ParticipantName(last.ParticipantId)}）";
            _statusLabel.text = _game.CurrentParticipantId == UserId
                ? "あなたの手番"
                : $"{PresentationText.ParticipantName(_game.CurrentParticipantId)} の手番";
            _npcRow.Clear();
            foreach (var participant in _game.Participants.Where(item => item.Kind == ParticipantKind.Npc))
            {
                _npcRow.Add(CreateParticipantPanel(participant));
            }

            var user = _game.Participants.Single(item => item.Id == UserId);
            _userLifeLabel.text = $"ライフ {user.Life}";
            _userCardLabel.text = "伏せ札";
            SetInputEnabled(_game.CurrentParticipantId == UserId);
        }

        private VisualElement CreateParticipantPanel(ParticipantState participant)
        {
            var panel = CreatePanel(participant.Id);
            panel.style.width = Length.Percent(23);
            if (participant.Id == _game.CurrentParticipantId)
            {
                panel.style.borderTopColor = panel.style.borderBottomColor = new Color(1f, 0.72f, 0.2f);
                panel.style.borderTopWidth = panel.style.borderBottomWidth = 5;
            }
            var avatar = new VisualElement();
            avatar.style.height = Length.Percent(62);
            SetBackground(avatar, AvatarResource(participant.Id));
            var card = _game.CurrentCards.FirstOrDefault(item => item.ParticipantId == participant.Id);
            panel.Add(avatar);
            panel.Add(CreateLabel(PresentationText.ParticipantName(participant.Id), 20));
            panel.Add(CreateLabel($"ライフ {participant.Life}" + (participant.IsEliminated ? " / 脱落" : string.Empty), 18));
            var cardLabel = CreateLabel(card?.Card == null ? "—" : PresentationText.Card(card.Card.Kind, card.Card.Value), 27);
            cardLabel.style.width = 88;
            cardLabel.style.height = 112;
            if (card?.Card != null)
            {
                SetBackground(cardLabel, CardResource(card.Card));
                cardLabel.style.color = card.Card.Kind == CardKind.Number ? Color.black : Color.white;
            }
            panel.Add(cardLabel);
            return panel;
        }

        private void ShowResult()
        {
            SetVisible(_titleScreen, false);
            SetVisible(_battleScreen, false);
            SetVisible(_resultScreen, true);
            var result = _game.LastRoundResult;
            _resultCards.Clear();
            foreach (var deal in result.DealtCards)
            {
                _resultCards.Add(CreateResultCard(PresentationText.ParticipantName(deal.ParticipantId), deal.Card));
            }
            foreach (var card in result.AdditionalCards)
            {
                _resultCards.Add(CreateResultCard("？の追加札", card));
            }
            _resultSummary.text =
                $"最終宣言　{result.DeclaredNumber}\n"
                + $"宣言者　{PresentationText.ParticipantName(result.NumberDeclarerId)}\n"
                + $"コヨーテ　{PresentationText.ParticipantName(result.CoyoteDeclarerId)}\n\n"
                + $"実合計　{result.ActualTotal}\n"
                + $"敗者　{PresentationText.ParticipantName(result.LoserId)}\n\n"
                + string.Join("\n", result.Participants.Select(item => $"{PresentationText.ParticipantName(item.Id)}：ライフ {item.Life}{(item.IsEliminated ? "（脱落）" : string.Empty)}"));
            SetButtonEnabled(_nextRoundButton, _game.State == GameFlowState.RoundResult);
            SetVisible(_gameOverDialog, _game.State == GameFlowState.GameOver);
            _outcomeLabel.text = _game.Outcome == GameOutcome.UserVictory ? "勝利！" : "敗北…";
        }

        private VisualElement CreateResultCard(string owner, CardState card)
        {
            var panel = CreatePanel();
            panel.style.width = 190;
            panel.style.height = 210;
            panel.Add(CreateLabel(owner, 18));
            var cardLabel = CreateLabel(PresentationText.Card(card.Kind, card.Value), 38);
            cardLabel.style.width = 105;
            cardLabel.style.height = 140;
            cardLabel.style.color = card.Kind == CardKind.Number ? Color.black : Color.white;
            SetBackground(cardLabel, CardResource(card));
            panel.Add(cardLabel);
            return panel;
        }

        private void ReturnToTitle()
        {
            CancelPendingOperations();
            _game.TryReturnToTitle();
            ShowTitle();
        }

        private void CancelPendingOperations()
        {
            _operationGeneration++;
            StopAllCoroutines();
        }

        private void SetInputEnabled(bool enabled)
        {
            var isUserTurn = enabled && _game.State == GameFlowState.Declaring && _game.CurrentParticipantId == UserId;
            _numberInput.SetEnabled(isUserTurn);
            var last = _game.DeclarationHistory.LastOrDefault();
            SetButtonEnabled(_declareButton, isUserTurn && (last == null || last.Value < int.MaxValue));
            SetButtonEnabled(_coyoteButton, isUserTurn && _game.DeclarationHistory.Count > 0);
        }

        private void ApplySafeArea()
        {
            _lastSafeArea = Screen.safeArea;
            var padding = SafeAreaPaddingCalculator.Calculate(
                Mathf.Max(1, Screen.width),
                Mathf.Max(1, Screen.height),
                _lastSafeArea,
                new Vector2(1920, 1080)
            );
            _root.style.paddingLeft = padding.x;
            _root.style.paddingTop = padding.y;
            _root.style.paddingRight = padding.z;
            _root.style.paddingBottom = padding.w;
        }

        private static void SetButtonEnabled(Button button, bool enabled)
        {
            button.SetEnabled(enabled);
        }
    }
}
