using System.Collections;
using CoyoteBattle.Application;
using CoyoteBattle.Domain;
using CoyoteBattle.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace CoyoteBattle.Tests.Presentation
{
    /// <summary>
    /// Battle画面のNPCポートレートと周辺表示のレイアウトを保証します。
    /// </summary>
    public sealed class NpcPortraitPresentationTests
    {
        /// <summary>
        /// テスト間でコントローラーや表示用カメラが残らないよう破棄します。
        /// </summary>
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (
                var controller in Object.FindObjectsByType<GamePresentationController>(
                    FindObjectsSortMode.None
                )
            )
            {
                Object.Destroy(controller.gameObject);
            }
            foreach (var camera in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
            {
                if (camera.name == "CoyoteBattleCamera")
                {
                    Object.Destroy(camera.gameObject);
                }
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator StartNewGame_Battle表示_NPC画像が描画可能な大きさを持つ()
        {
            // 背景画像を設定済みでも幅0になり、ポートレートが見えなくなる不具合を防ぎます。
            var gameObject = CreateController();
            var controller = gameObject.AddComponent<GamePresentationController>();
            controller.ConfigureForTests(CreateDeterministicGame);
            gameObject.SetActive(true);
            yield return null;

            var root = gameObject.GetComponent<UIDocument>().rootVisualElement;
            Click(root.Q<Button>("start-game-button"));
            yield return null;

            var expectedPortraitNames = new[]
            {
                "NpcAggressive",
                "NpcCautious",
                "NpcGambling",
                "NpcAnalytical",
            };
            for (var index = 1; index <= 4; index++)
            {
                var participantPanel = root.Q<VisualElement>($"npc-{index}");
                var portrait = participantPanel.Q<VisualElement>($"npc-{index}-portrait");
                Assert.That(portrait.resolvedStyle.backgroundImage.texture, Is.Not.Null);
                Assert.That(
                    portrait.resolvedStyle.backgroundImage.texture.name,
                    Is.EqualTo(expectedPortraitNames[index - 1])
                );
                Assert.That(portrait.resolvedStyle.width, Is.GreaterThan(0f));
                Assert.That(portrait.resolvedStyle.height, Is.GreaterThan(0f));
            }

            Object.Destroy(gameObject);
        }

        [UnityTest]
        public IEnumerator StartNewGame_NPCが現在手番_画像と枠強調が同時に表示される()
        {
            // 現在手番の枠線がポートレート領域を潰さず、両方を識別できることを保証します。
            var gameObject = CreateController();
            var controller = gameObject.AddComponent<GamePresentationController>();
            controller.ConfigureForTests(CreateNpcStarterGame);
            gameObject.SetActive(true);
            yield return null;

            var root = gameObject.GetComponent<UIDocument>().rootVisualElement;
            Click(root.Q<Button>("start-game-button"));
            yield return null;

            var participantPanel = root.Q<VisualElement>("npc-1");
            var portrait = participantPanel.Q<VisualElement>("npc-1-portrait");
            Assert.That(participantPanel.resolvedStyle.borderTopWidth, Is.GreaterThan(0f));
            Assert.That(participantPanel.resolvedStyle.borderBottomWidth, Is.GreaterThan(0f));
            Assert.That(portrait.resolvedStyle.width, Is.GreaterThan(0f));
            Assert.That(portrait.resolvedStyle.height, Is.GreaterThan(0f));
            AssertNpcPanelContentsDoNotOverlap(participantPanel, "npc-1");

            Object.Destroy(gameObject);
        }

        [UnityTest]
        public IEnumerator CreateParticipantPanel_脱落済みNPC_画像と脱落表示が同時に表示される()
        {
            // 脱落後も参加者を画像で識別でき、既存の脱落表示を維持することを保証します。
            var game = CreateDeterministicGame();
            Assert.That(game.TryStartNewGame(), Is.True);
            LoseParticipantForRounds(game, "npc-1", 3);
            var participant = game.Participants[1];
            Assert.That(participant.IsEliminated, Is.True);

            var gameObject = new GameObject("EliminatedPortraitTest");
            gameObject.AddComponent<GamePresentationController>();
            yield return null;

            var root = gameObject.GetComponent<UIDocument>().rootVisualElement;
            var container = new VisualElement { name = "eliminated-panel-container" };
            container.style.position = Position.Absolute;
            container.style.width = 400;
            container.style.height = 500;
            var participantPanel = PresentationUiFactory.CreateParticipantPanel(
                participant,
                null,
                null
            );
            participantPanel.style.width = Length.Percent(100);
            participantPanel.style.height = Length.Percent(100);
            container.Add(participantPanel);
            root.Add(container);
            yield return null;

            var portrait = participantPanel.Q<VisualElement>("npc-1-portrait");
            Assert.That(portrait.resolvedStyle.backgroundImage.texture, Is.Not.Null);
            Assert.That(portrait.resolvedStyle.width, Is.GreaterThan(0f));
            Assert.That(portrait.resolvedStyle.height, Is.GreaterThan(0f));
            Assert.That(participantPanel.Q<Label>("npc-1-life").text, Does.Contain("脱落"));
            AssertNpcPanelContentsDoNotOverlap(participantPanel, "npc-1");

            Object.Destroy(gameObject);
        }

        [UnityTest]
        public IEnumerator StartNewGame_検証解像度とSafeArea相当余白_NPC表示が欠落も重複もしない()
        {
            // 対象3解像度と端末余白がある横画面で、4つのNPCパネルが安全に並ぶことを保証します。
            var game = CreateDeterministicGame();
            Assert.That(game.TryStartNewGame(), Is.True);
            var gameObject = new GameObject("NpcResolutionTest");
            gameObject.AddComponent<GamePresentationController>();
            yield return null;

            var root = gameObject.GetComponent<UIDocument>().rootVisualElement;
            var resolutions = new[]
            {
                new Vector2Int(1280, 720),
                new Vector2Int(1920, 1080),
                new Vector2Int(2400, 1080),
            };
            foreach (var resolution in resolutions)
            {
                var viewport = CreateNpcLayoutViewport(game, resolution);
                root.Add(viewport);
                yield return null;

                AssertNpcRowLayout(viewport, resolution);
                viewport.RemoveFromHierarchy();
            }

            Object.Destroy(gameObject);
        }

        [Test]
        public void SetBackground_画像を取得できない_要素を維持して例外を送出しない()
        {
            // 画像資産の取得失敗がUI構築やゲーム進行を中断しないことを保証します。
            var element = new VisualElement();

            Assert.That(
                () => PresentationUiFactory.SetBackground(element, "Art/NotFoundPortrait"),
                Throws.Nothing
            );
            Assert.That(element.resolvedStyle.backgroundImage.texture, Is.Null);
        }

        private static GameObject CreateController()
        {
            var gameObject = new GameObject("NpcPortraitTest");
            gameObject.SetActive(false);
            return gameObject;
        }

        private static GameFlowService CreateDeterministicGame()
        {
            return new GameFlowService(new ZeroRandomSource(), new ZeroRandomSource());
        }

        private static GameFlowService CreateNpcStarterGame()
        {
            return new GameFlowService(
                new FirstValueThenZeroRandomSource(1),
                new ZeroRandomSource()
            );
        }

        /// <summary>
        /// 指定NPCへ3回の敗北を与え、脱落状態を再現します。
        /// </summary>
        private static void LoseParticipantForRounds(
            GameFlowService game,
            string participantId,
            int count
        )
        {
            for (var index = 0; index < count; index++)
            {
                if (game.State == GameFlowState.RoundResult)
                {
                    Assert.That(game.TryStartNextRound(), Is.True);
                }

                var declaration = 1;
                while (game.CurrentParticipantId != participantId)
                {
                    Assert.That(
                        game.TryDeclareNumber(game.CurrentParticipantId, declaration++),
                        Is.True
                    );
                }

                Assert.That(game.TryDeclareNumber(participantId, int.MaxValue), Is.True);
                Assert.That(game.TryDeclareCoyote(game.CurrentParticipantId), Is.True);
            }
        }

        /// <summary>
        /// 指定解像度とSafe Area相当余白を持つNPC行の検証用表示領域を生成します。
        /// </summary>
        private static VisualElement CreateNpcLayoutViewport(
            GameFlowService game,
            Vector2Int resolution
        )
        {
            var viewport = new VisualElement { name = "npc-layout-viewport" };
            viewport.style.position = Position.Absolute;
            viewport.style.width = resolution.x;
            viewport.style.height = resolution.y;
            viewport.style.paddingLeft = viewport.style.paddingRight = 120;
            viewport.style.paddingTop = viewport.style.paddingBottom = 40;
            var npcRow = new VisualElement { name = "npc-row" };
            npcRow.style.flexDirection = FlexDirection.Row;
            npcRow.style.justifyContent = Justify.SpaceAround;
            npcRow.style.height = Length.Percent(47);
            viewport.Add(npcRow);

            for (var index = 1; index <= 4; index++)
            {
                var participant = game.Participants[index];
                var card = game.CurrentCards[index];
                npcRow.Add(
                    PresentationUiFactory.CreateParticipantPanel(
                        participant,
                        game.CurrentParticipantId,
                        card
                    )
                );
            }

            return viewport;
        }

        /// <summary>
        /// NPC行全体が指定解像度で欠落せず、隣接パネルや内部要素と重ならないことを検証します。
        /// </summary>
        private static void AssertNpcRowLayout(VisualElement root, Vector2Int resolution)
        {
            var npcRow = root.Q<VisualElement>("npc-row");
            var panels = new VisualElement[4];
            for (var index = 0; index < panels.Length; index++)
            {
                var participantId = $"npc-{index + 1}";
                panels[index] = root.Q<VisualElement>(participantId);
                var portrait = panels[index].Q<VisualElement>($"{participantId}-portrait");
                var card = panels[index].Q<Label>($"{participantId}-card");
                Assert.That(
                    portrait.resolvedStyle.width,
                    Is.GreaterThan(0f),
                    $"{resolution.x}x{resolution.y}: {participantId} の画像幅"
                );
                Assert.That(
                    portrait.resolvedStyle.height,
                    Is.GreaterThanOrEqualTo(resolution.x >= 2400 ? 132f : 72f),
                    $"{resolution.x}x{resolution.y}: {participantId} の画像高さ"
                );
                if (resolution.x >= 2400)
                {
                    Assert.That(
                        portrait.worldBound.Overlaps(card.worldBound),
                        Is.False,
                        $"{resolution.x}x{resolution.y}: 横長画面では画像とカードを横方向に並べる"
                    );
                    Assert.That(
                        Mathf.Abs(portrait.worldBound.center.y - card.worldBound.center.y),
                        Is.LessThan(80f),
                        $"{resolution.x}x{resolution.y}: 横長画面では画像とカードの縦位置を近づける"
                    );
                }
                Assert.That(
                    npcRow.worldBound.Contains(panels[index].worldBound.min),
                    Is.True,
                    $"{resolution.x}x{resolution.y}: {participantId} の左上"
                );
                Assert.That(
                    npcRow.worldBound.Contains(panels[index].worldBound.max),
                    Is.True,
                    $"{resolution.x}x{resolution.y}: {participantId} の右下"
                );
                AssertNpcPanelContentsDoNotOverlap(panels[index], participantId);
            }

            for (var index = 0; index < panels.Length - 1; index++)
            {
                Assert.That(
                    panels[index].worldBound.Overlaps(panels[index + 1].worldBound),
                    Is.False,
                    $"{resolution.x}x{resolution.y}: 隣接NPCパネル"
                );
            }
        }

        /// <summary>
        /// ポートレートが名前、ライフ、カードの各表示領域と重ならないことを検証します。
        /// </summary>
        private static void AssertNpcPanelContentsDoNotOverlap(
            VisualElement participantPanel,
            string participantId
        )
        {
            var portrait = participantPanel.Q<VisualElement>($"{participantId}-portrait");
            var name = participantPanel.Q<Label>($"{participantId}-name");
            var life = participantPanel.Q<Label>($"{participantId}-life");
            var card = participantPanel.Q<Label>($"{participantId}-card");
            Assert.That(portrait.worldBound.Overlaps(name.worldBound), Is.False);
            Assert.That(portrait.worldBound.Overlaps(life.worldBound), Is.False);
            Assert.That(portrait.worldBound.Overlaps(card.worldBound), Is.False);
        }

        private static void Click(Button button)
        {
            var position = button.worldBound.center;
            var downEvent = new Event
            {
                type = EventType.MouseDown,
                button = 0,
                mousePosition = position,
            };
            using (var pointerDown = PointerDownEvent.GetPooled(downEvent))
            {
                button.SendEvent(pointerDown);
            }

            var upEvent = new Event
            {
                type = EventType.MouseUp,
                button = 0,
                mousePosition = position,
            };
            using (var pointerUp = PointerUpEvent.GetPooled(upEvent))
            {
                button.SendEvent(pointerUp);
            }
            button.ReleasePointer(PointerId.mousePointerId);
        }

        private sealed class ZeroRandomSource : IRandomSource
        {
            public int Next(int maxExclusive)
            {
                return 0;
            }
        }

        private sealed class FirstValueThenZeroRandomSource : IRandomSource
        {
            private readonly int _firstValue;
            private bool _hasReturnedFirstValue;

            internal FirstValueThenZeroRandomSource(int firstValue)
            {
                _firstValue = firstValue;
            }

            public int Next(int maxExclusive)
            {
                if (_hasReturnedFirstValue)
                {
                    return 0;
                }

                _hasReturnedFirstValue = true;
                return _firstValue;
            }
        }
    }
}
