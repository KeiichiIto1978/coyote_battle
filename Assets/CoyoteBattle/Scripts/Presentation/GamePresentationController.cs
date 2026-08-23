using System;
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
    public sealed partial class GamePresentationController : MonoBehaviour
    {
        private const string UserId = "user";
        private GameFlowService _game;
        private UIDocument _document;
        private VisualElement _root;
        private VisualElement _titleScreen;
        private VisualElement _battleScreen;
        private VisualElement _battleMain;
        private VisualElement _resultScreen;
        private VisualElement _gameOverDialog;
        private VisualElement _npcRow;
        private VisualElement _resultCards;
        private Label _roundLabel;
        private Label _statusLabel;
        private Label _declarationLabel;
        private Label _actionBanner;
        private Label _userLifeLabel;
        private Label _userCardLabel;
        private Label _errorLabel;
        private Label _resultLoser;
        private Label _resultTotal;
        private Label _resultDeclaration;
        private Label _resultDetails;
        private Label _outcomeLabel;
        private TextField _numberInput;
        private Button _declareButton;
        private Button _coyoteButton;
        private Button _cardInformationButton;
        private Button _nextRoundButton;
        private int _operationGeneration;
        private Coroutine _npcTurnsCoroutine;
        private bool _isNpcSequenceRunning;
        private bool _initialized;
        private Rect _lastSafeArea;
        private Font _interfaceFont;
        private ThemeStyleSheet _themeStyleSheet;
        private Func<GameFlowService> _gameFactory = CreateGame;
        private IPresentationDelay _presentationDelay = new RealtimePresentationDelay();
        private INpcTurnExecutor _npcTurnExecutor = new ApplicationNpcTurnExecutor();

        /// <summary>
        /// Unityライフサイクルから画面を初期化します。
        /// </summary>
        private void Awake()
        {
            Initialize();
        }

        /// <summary>
        /// PlayModeテストで再現可能なゲーム生成方法を初期化前に設定します。
        /// </summary>
        /// <param name="gameFactory">コントローラーが利用するゲーム生成方法です。</param>
        internal void ConfigureForTests(Func<GameFlowService> gameFactory)
        {
            if (_initialized)
            {
                throw new InvalidOperationException("初期化後にゲーム生成方法は変更できません。");
            }

            _gameFactory = gameFactory ?? throw new ArgumentNullException(nameof(gameFactory));
        }

        /// <summary>
        /// PlayModeテストでNPCの待機とApplication呼び出しを手動制御できるよう設定します。
        /// </summary>
        /// <param name="gameFactory">再現可能なゲーム生成方法です。</param>
        /// <param name="presentationDelay">思考中と行動表示の待機方法です。</param>
        /// <param name="npcTurnExecutor">NPCの1行動をApplicationへ送る方法です。</param>
        internal void ConfigureForTests(
            Func<GameFlowService> gameFactory,
            IPresentationDelay presentationDelay,
            INpcTurnExecutor npcTurnExecutor
        )
        {
            ConfigureForTests(gameFactory);
            _presentationDelay =
                presentationDelay ?? throw new ArgumentNullException(nameof(presentationDelay));
            _npcTurnExecutor =
                npcTurnExecutor ?? throw new ArgumentNullException(nameof(npcTurnExecutor));
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
            _game = _gameFactory();
            _document = GetComponent<UIDocument>() ?? gameObject.AddComponent<UIDocument>();
            _themeStyleSheet = Resources.Load<ThemeStyleSheet>("DefaultRuntimeTheme");
            _document.panelSettings = CreatePanelSettings(_themeStyleSheet);
            _root = _document.rootVisualElement;
            _interfaceFont = Resources.Load<Font>("Fonts/NotoSansJP");
            BuildUi();
            ApplyFont(_root, _interfaceFont);
            ConfigureNumberInput(_numberInput, _interfaceFont);
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
            DetachBgmControls();
            if (_document != null && _document.panelSettings != null)
            {
                Destroy(_document.panelSettings);
            }
        }

        private static GameFlowService CreateGame()
        {
            return new GameFlowService(new SystemRandomSource(), new SystemRandomSource());
        }

        private static PanelSettings CreatePanelSettings(ThemeStyleSheet themeStyleSheet)
        {
            var settings = ScriptableObject.CreateInstance<PanelSettings>();
            settings.name = "RuntimePanelSettings";
            settings.themeStyleSheet = themeStyleSheet;
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
            _battleScreen.style.flexDirection = FlexDirection.Row;
            _battleMain = new VisualElement { name = "battle-main" };
            _battleMain.style.flexGrow = 1;
            _battleMain.style.minWidth = 0;
            _battleScreen.Add(_battleMain);
            var battleHeader = new VisualElement { name = "battle-header" };
            battleHeader.style.flexDirection = FlexDirection.Row;
            battleHeader.style.alignItems = Align.Center;
            _roundLabel = CreateLabel(string.Empty, 28, "round-label");
            _roundLabel.style.flexGrow = 1;
            _roundLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            _cardInformationButton = CreateButton(
                "カード情報",
                OpenCardInformation,
                "card-information-button"
            );
            _cardInformationButton.style.width = 180;
            battleHeader.Add(_roundLabel);
            battleHeader.Add(_cardInformationButton);
            _battleMain.Add(battleHeader);
            _npcRow = new VisualElement { name = "npc-row" };
            _npcRow.style.flexDirection = FlexDirection.Row;
            _npcRow.style.justifyContent = Justify.SpaceAround;
            _npcRow.style.height = Length.Percent(47);
            _battleMain.Add(_npcRow);
            var centerPanel = CreatePanel("declaration-panel");
            centerPanel.style.alignSelf = Align.Center;
            centerPanel.style.width = Length.Percent(70);
            centerPanel.style.height = 210;
            centerPanel.style.flexShrink = 0;
            _statusLabel = CreateLabel(string.Empty, 21, "status-label");
            _statusLabel.style.marginTop = _statusLabel.style.marginBottom = 2;
            _declarationLabel = CreateLabel(string.Empty, 26, "declaration-label");
            _declarationLabel.style.marginTop = _declarationLabel.style.marginBottom = 2;
            _declarationLabel.style.whiteSpace = WhiteSpace.Normal;
            _actionBanner = CreateLabel(string.Empty, 32, "action-banner");
            _actionBanner.style.width = Length.Percent(100);
            _actionBanner.style.flexShrink = 0;
            _actionBanner.style.marginTop = _actionBanner.style.marginBottom = 2;
            _actionBanner.style.color = new Color(1f, 0.78f, 0.22f);
            _actionBanner.style.unityFontStyleAndWeight = FontStyle.Bold;
            _actionBanner.style.whiteSpace = WhiteSpace.NoWrap;
            centerPanel.Add(_statusLabel);
            centerPanel.Add(_declarationLabel);
            centerPanel.Add(_actionBanner);
            _battleMain.Add(centerPanel);
            _battleMain.Add(BuildUserArea());
            BuildCardInformation();

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
            var resultSummary = CreatePanel("result-summary");
            resultSummary.style.width = Length.Percent(38);
            resultSummary.style.alignItems = Align.Stretch;
            _resultLoser = CreateLabel(string.Empty, 44, "result-loser");
            _resultLoser.style.color = new Color(1f, 0.45f, 0.28f);
            _resultLoser.style.unityFontStyleAndWeight = FontStyle.Bold;
            _resultTotal = CreateLabel(string.Empty, 38, "result-total");
            _resultDeclaration = CreateLabel(string.Empty, 30, "result-declaration");
            _resultDeclaration.style.whiteSpace = WhiteSpace.Normal;
            _resultDetails = CreateLabel(string.Empty, 22, "result-details");
            _resultDetails.style.whiteSpace = WhiteSpace.Normal;
            resultSummary.Add(_resultLoser);
            resultSummary.Add(_resultTotal);
            resultSummary.Add(_resultDeclaration);
            resultSummary.Add(_resultDetails);
            resultBody.Add(_resultCards);
            resultBody.Add(resultSummary);
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
            _gameOverDialog.Add(
                CreateButton("タイトルへ戻る", ReturnToTitle, "return-title-button")
            );

            _root.Add(_titleScreen);
            _root.Add(_battleScreen);
            _root.Add(_resultScreen);
            _root.Add(_gameOverDialog);
            BuildBgmControls();
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
            HideCardInformation();
            _game = _gameFactory();
            if (!_game.TryStartNewGame())
            {
                return;
            }

            _numberInput.value = string.Empty;
            _errorLabel.text = string.Empty;
            _actionBanner.text = string.Empty;
            ShowBattle();
            ContinueNpcTurns();
        }

        private void StartNextRound()
        {
            CancelPendingOperations();
            HideCardInformation();
            SetButtonEnabled(_nextRoundButton, false);
            if (!_game.TryStartNextRound())
            {
                return;
            }

            _actionBanner.text = string.Empty;
            ShowBattle();
            ContinueNpcTurns();
        }

        private void DeclareNumber()
        {
            SetInputEnabled(false);
            var previous = _game.DeclarationHistory.LastOrDefault()?.Value;
            if (
                !NumberDeclarationInputValidator.TryValidate(
                    _numberInput.value,
                    previous,
                    out var value,
                    out var error
                )
            )
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

        private void ShowTitle()
        {
            HideCardInformation();
            _bgmPlayer.SetTrack(BgmTrack.Title);
            SetVisible(_titleScreen, true);
            SetVisible(_battleScreen, false);
            SetVisible(_resultScreen, false);
            SetVisible(_gameOverDialog, false);
        }

        private void ShowBattle()
        {
            _bgmPlayer.SetTrack(BgmTrack.Battle);
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
            _declarationLabel.text =
                last == null
                    ? "最初の数字を宣言してください"
                    : $"現在の宣言値：{last.Value}\n直前の宣言者：{PresentationText.ParticipantName(last.ParticipantId)}";
            _statusLabel.text =
                _game.CurrentParticipantId == UserId
                    ? "あなたの手番"
                    : $"{PresentationText.ParticipantName(_game.CurrentParticipantId)} の手番";
            _npcRow.Clear();
            foreach (
                var participant in _game.Participants.Where(item =>
                    item.Kind == ParticipantKind.Npc
                )
            )
            {
                var card = _game.CurrentCards.FirstOrDefault(item =>
                    item.ParticipantId == participant.Id
                );
                _npcRow.Add(CreateParticipantPanel(participant, _game.CurrentParticipantId, card));
            }

            var user = _game.Participants.Single(item => item.Id == UserId);
            _userLifeLabel.text = $"ライフ {user.Life}";
            _userCardLabel.text = "伏せ札";
            SetInputEnabled(!_isNpcSequenceRunning && _game.CurrentParticipantId == UserId);
        }

        private void ReturnToTitle()
        {
            CancelPendingOperations();
            _game.TryReturnToTitle();
            ShowTitle();
        }

        private void SetInputEnabled(bool enabled)
        {
            var isUserTurn =
                enabled
                && !_isCardInformationOpen
                && _game.State == GameFlowState.Declaring
                && _game.CurrentParticipantId == UserId;
            _numberInput.SetEnabled(isUserTurn);
            ApplyNumberInputEnabledStyle(_numberInput, isUserTurn);
            var last = _game.DeclarationHistory.LastOrDefault();
            SetButtonEnabled(
                _declareButton,
                isUserTurn && (last == null || last.Value < int.MaxValue)
            );
            SetButtonEnabled(_coyoteButton, isUserTurn && _game.DeclarationHistory.Count > 0);
            SetButtonEnabled(_cardInformationButton, isUserTurn && !_isNpcSequenceRunning);
        }

        private void ApplySafeArea()
        {
            _lastSafeArea = Screen.safeArea;
            SafeAreaStyleApplier.Apply(_root, Screen.width, Screen.height, _lastSafeArea);
        }

        private static void SetButtonEnabled(Button button, bool enabled)
        {
            button.SetEnabled(enabled);
        }
    }
}
