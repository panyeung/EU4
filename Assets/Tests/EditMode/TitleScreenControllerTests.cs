using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;
using SengokuChronicle.Persistence;
using SengokuChronicle.Presentation;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SengokuChronicle.Tests.EditMode
{
    public class TitleScreenControllerTests
    {
        private GameSession _session;
        private GameObject _controllerObject;
        private TitleScreenController _controller;
        private GameObject _titleRoot;
        private GameObject _mainMenuRoot;
        private GameObject _selectorRoot;
        private Button _newGameButton;
        private Button _continueButton;
        private Button _fixedScenarioButton;
        private Button _backButton;
        private Button _standardButton;
        private Button _crisisButton;
        private Button _educationButton;
        private Button _researchButton;
        private Button _familyFormationButton;
        private Button _seclusionButton;
        private Button _familyDoctrineButton;
        private Button _debugEntryButton;
        private GameObject _debugPanelParent;
        private DebugModePanel _debugPanel;
        private Text _statusText;
        private string _directory;
        private SaveSlotStore _store;

        [SetUp]
        public void SetUp()
        {
            _session = GameSession.NewGame();
            _directory = Path.Combine(
                Path.GetTempPath(), "SengokuChronicleTests", Guid.NewGuid().ToString("N"));
            _store = new SaveSlotStore(_directory);

            _controllerObject = new GameObject("TitleScreenController");
            _controller = _controllerObject.AddComponent<TitleScreenController>();

            _titleRoot = new GameObject("M6TitleRoot", typeof(RectTransform));
            _mainMenuRoot = new GameObject("M6MainMenuRoot", typeof(RectTransform));
            _selectorRoot = new GameObject("M6ScenarioSelectRoot", typeof(RectTransform));
            _mainMenuRoot.transform.SetParent(_titleRoot.transform, false);
            _selectorRoot.transform.SetParent(_titleRoot.transform, false);
            _newGameButton = MakeButton("NewGameButton");
            _continueButton = MakeButton("ContinueButton");
            _fixedScenarioButton = MakeButton("SC013FixedScenarioButton");
            _backButton = MakeButton("M6ScenarioBackButton");
            _standardButton = MakeButton("StandardButton");
            _crisisButton = MakeButton("CrisisButton");
            _educationButton = MakeButton("EducationButton");
            _researchButton = MakeButton("ResearchButton");
            _familyFormationButton = MakeButton("FamilyFormationButton");
            _seclusionButton = MakeButton("SeclusionButton");
            _familyDoctrineButton = MakeButton("FamilyDoctrineButton");
            _debugEntryButton = MakeButton("DebugModeEntryButton");
            _statusText = new GameObject("Status", typeof(RectTransform)).AddComponent<Text>();

            _controller.Bind(_session, _store, null, _titleRoot, _newGameButton, _continueButton, _statusText);
            _controller.BindScenarioSelection(
                _mainMenuRoot, _selectorRoot, _fixedScenarioButton, _backButton);
            _controller.BindScenarioButtons(_standardButton, _crisisButton);
            _controller.BindEducationScenarioButton(_educationButton);
            _controller.BindResearchScenarioButton(_researchButton);
            _controller.BindFamilyFormationScenarioButton(_familyFormationButton);
            _controller.BindSeclusionScenarioButton(_seclusionButton);
            _controller.BindFamilyDoctrineScenarioButton(_familyDoctrineButton);
            _debugPanelParent = new GameObject("DebugPanelParent", typeof(RectTransform));
            _debugPanel = DebugModePanel.CreateWindowUnder(_debugPanelParent.transform, _session);
            _controller.BindDebugModeEntry(_debugEntryButton);
            _controller.BindDebugModePanel(_debugPanel);
            _controller.ShowTitle();
        }

        [TearDown]
        public void TearDown()
        {
            if (_controllerObject != null)
                Object.DestroyImmediate(_controllerObject);
            if (_titleRoot != null)
                Object.DestroyImmediate(_titleRoot);
            Destroy(_newGameButton);
            Destroy(_continueButton);
            Destroy(_fixedScenarioButton);
            Destroy(_backButton);
            Destroy(_standardButton);
            Destroy(_crisisButton);
            Destroy(_educationButton);
            Destroy(_researchButton);
            Destroy(_familyFormationButton);
            Destroy(_seclusionButton);
            Destroy(_familyDoctrineButton);
            Destroy(_debugEntryButton);
            if (_debugPanel != null)
                Object.DestroyImmediate(_debugPanel.gameObject);
            if (_debugPanelParent != null)
                Object.DestroyImmediate(_debugPanelParent);
            if (_statusText != null)
                Object.DestroyImmediate(_statusText.gameObject);
            if (_session != null)
                Object.DestroyImmediate(_session.gameObject);
            if (Directory.Exists(_directory))
                Directory.Delete(_directory, true);
        }

        private static Button MakeButton(string name) =>
            new GameObject(name, typeof(RectTransform)).AddComponent<Button>();

        private static void Destroy(Button button)
        {
            if (button != null)
                Object.DestroyImmediate(button.gameObject);
        }

        private SaveData CompletedData()
        {
            return new SaveData
            {
                saveFormatVersion = SaveDataValidator.CurrentVersion,
                year = 1560,
                month = 4,
                day = 5,
                locationId = LocationId.Kiyosu.Value,
                supplyShortageStage = (int)SupplyShortageStage.RewardClaimed,
                sealedLetterStage = (int)SealedLetterStage.RewardClaimed,
                marketTallyStage = (int)MarketTallyStage.RewardClaimed,
                relationships = null
            };
        }


        [Test]
        public void DebugPanelReset_ReturnsThePlayerToTheStartPage()
        {
            _debugEntryButton.onClick.Invoke();
            Assert.That(_controller.IsTitleVisible, Is.False);
            Assert.That(_debugPanel.IsOpen, Is.True);

            _debugPanel.ResetDebugSession();

            Assert.That(_debugPanel.IsOpen, Is.False);
            Assert.That(_controller.IsTitleVisible, Is.True,
                "reset must restore the start page the entry hid.");
            Assert.That(_controller.IsMainMenuVisible, Is.True);
            Assert.That(_session.IsDebugSessionActive, Is.False);
            Assert.That(_controller.Status, Does.Contain("已重置调试会话"));
            Assert.That(_controller.SlotLoadCount, Is.Zero,
                "returning to the start page must not read the slot.");
        }

        [Test]
        public void DebugPanelExit_ReturnsThePlayerToTheStartPage()
        {
            _debugEntryButton.onClick.Invoke();
            Assert.That(_controller.IsTitleVisible, Is.False);

            _debugPanel.ExitDebugSession();

            Assert.That(_debugPanel.IsOpen, Is.False);
            Assert.That(_controller.IsTitleVisible, Is.True,
                "exit must restore the start page the entry hid.");
            Assert.That(_controller.IsMainMenuVisible, Is.True);
            Assert.That(_session.IsDebugSessionActive, Is.False);
            Assert.That(_controller.Status, Does.Contain("已退出 Debug 模式"));
            Assert.That(_controller.SlotLoadCount, Is.Zero);
            Assert.That(_debugEntryButton.gameObject.activeInHierarchy, Is.True,
                "the entry is visible again on the restored title page.");
        }

        [Test]
        public void DebugEntry_ReopensAfterAReset()
        {
            _debugEntryButton.onClick.Invoke();
            _debugPanel.ResetDebugSession();
            Assert.That(_debugPanel.IsOpen, Is.False);
            Assert.That(_controller.IsTitleVisible, Is.True);

            _debugEntryButton.onClick.Invoke();

            Assert.That(_debugPanel.IsOpen, Is.True,
                "the entry must stay armed after a reset returns to the start page.");
            Assert.That(_session.IsDebugSessionActive, Is.True);
            Assert.That(_controller.IsTitleVisible, Is.False);
        }


        [Test]
        public void SerializedPanelField_IsSubscribedByTheLifecycle()
        {
            // A scene may assign the window in the Inspector only. Nothing calls the bind
            // method then, so the lifecycle must still subscribe to its reset/exit event.
            var host = new GameObject("SerializedPanelHost", typeof(RectTransform));
            DebugModePanel serializedPanel = DebugModePanel.CreateWindowUnder(host.transform,
                _session);
            try
            {
                typeof(TitleScreenController)
                    .GetField("debugModePanel",
                        System.Reflection.BindingFlags.Instance |
                        System.Reflection.BindingFlags.NonPublic)
                    .SetValue(_controller, serializedPanel);

                InvokeLifecycle(_controller, "OnEnable");
                Assert.That(_controller.DebugModePanelBound, Is.True);

                Assert.That(_controller.StartDebugSession(), Is.True);
                Assert.That(serializedPanel.IsOpen, Is.True);
                Assert.That(_controller.IsTitleVisible, Is.False);

                serializedPanel.ResetDebugSession();

                Assert.That(serializedPanel.IsOpen, Is.False);
                Assert.That(_controller.IsTitleVisible, Is.True,
                    "the serialized window's reset must restore the start page.");
                Assert.That(_session.IsDebugSessionActive, Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(serializedPanel.gameObject);
                UnityEngine.Object.DestroyImmediate(host);
            }
        }


        [Test]
        public void EntryUnderTitleRoot_StaysOnTheTitlePageAndReopensTheSameDebugSession()
        {
            // The saved entry is a child of the title root. It must stay there while the
            // title page is visible, move out only while the title is hidden, and return
            // when the start page comes back.
            _debugEntryButton.transform.SetParent(_titleRoot.transform, false);
            _controller.BindDebugModeEntry(_debugEntryButton);

            Assert.That(_debugEntryButton.transform.parent, Is.SameAs(_titleRoot.transform),
                "the entry belongs to the title page while the title is visible.");
            Assert.That(_debugEntryButton.transform.GetSiblingIndex(),
                Is.EqualTo(_titleRoot.transform.childCount - 1));
            Assert.That(_debugEntryButton.gameObject.activeInHierarchy, Is.True);

            _debugEntryButton.onClick.Invoke();
            DebugSession firstSession = _session.CurrentDebugSession;
            Assert.That(_controller.IsTitleVisible, Is.False);
            Assert.That(_debugPanel.IsOpen, Is.True);
            Assert.That(_session.IsDebugSessionActive, Is.True);
            Assert.That(_debugEntryButton.transform.parent, Is.Not.SameAs(_titleRoot.transform),
                "once the title is hidden the entry must leave the title overlay.");

            _debugPanel.Close();

            Assert.That(_debugPanel.IsOpen, Is.False);
            Assert.That(_session.IsDebugSessionActive, Is.True,
                "closing only hides the panel; the in-memory session stays alive.");
            Assert.That(_debugEntryButton.gameObject.activeInHierarchy, Is.True,
                "the entry must stay reachable while the debug session is active.");

            _debugEntryButton.onClick.Invoke();

            Assert.That(_debugPanel.IsOpen, Is.True);
            Assert.That(_session.CurrentDebugSession, Is.SameAs(firstSession),
                "reopening after close keeps the same in-memory debug session.");
            Assert.That(_controller.IsTitleVisible, Is.False);

            _debugPanel.ResetDebugSession();

            Assert.That(_debugPanel.IsOpen, Is.False);
            Assert.That(_controller.IsTitleVisible, Is.True,
                "reset returns to the start page.");
            Assert.That(_session.IsDebugSessionActive, Is.False);
            Assert.That(_debugEntryButton.transform.parent, Is.SameAs(_titleRoot.transform),
                "the entry returns under the title root.");
            Assert.That(_debugEntryButton.transform.GetSiblingIndex(),
                Is.EqualTo(_titleRoot.transform.childCount - 1));
            Assert.That(_debugEntryButton.gameObject.activeInHierarchy, Is.True,
                "the entry is visible on the restored title page.");

            _debugEntryButton.onClick.Invoke();
            Assert.That(_session.IsDebugSessionActive, Is.True,
                "the entry still opens a fresh session after a reset.");
            Assert.That(_debugPanel.IsOpen, Is.True);

            _debugPanel.ExitDebugSession();

            Assert.That(_debugPanel.IsOpen, Is.False);
            Assert.That(_controller.IsTitleVisible, Is.True,
                "exit returns to the start page.");
            Assert.That(_session.IsDebugSessionActive, Is.False);
            Assert.That(_debugEntryButton.transform.parent, Is.SameAs(_titleRoot.transform));
        }

        [Test]
        public void DebugEntry_InGame_StaysBelowTheEventAndTacticalOverlays()
        {
            var canvasObject = new GameObject("DebugOverlayCanvas", typeof(RectTransform),
                typeof(Canvas));
            var eventRoot = new GameObject("EventChoiceRoot", typeof(RectTransform));
            var tactical = new GameObject("M6TacticalMissionOverlay", typeof(RectTransform));
            try
            {
                canvasObject.transform.SetParent(null, false);
                _titleRoot.transform.SetParent(canvasObject.transform, false);
                eventRoot.transform.SetParent(canvasObject.transform, false);
                tactical.transform.SetParent(canvasObject.transform, false);
                _controller.ShowTitle();

                Assert.That(_debugEntryButton.transform.parent, Is.SameAs(_titleRoot.transform),
                    "on the title page the entry stays under the title root.");

                _debugEntryButton.onClick.Invoke();

                Assert.That(_controller.IsTitleVisible, Is.False);
                Assert.That(_debugEntryButton.transform.parent,
                    Is.SameAs(canvasObject.transform),
                    "over the game the entry moves to the root Canvas.");
                Assert.That(_debugEntryButton.transform.GetSiblingIndex(),
                    Is.LessThan(eventRoot.transform.GetSiblingIndex()),
                    "the entry must render below the forced-event overlay.");
                Assert.That(_debugEntryButton.transform.GetSiblingIndex(),
                    Is.LessThan(tactical.transform.GetSiblingIndex()),
                    "the entry must render below the tactical overlay.");
                Assert.That(_debugEntryButton.gameObject.activeInHierarchy, Is.False,
                    "the entry stays hidden while the window covers the screen.");

                _debugPanel.Close();

                Assert.That(_debugEntryButton.gameObject.activeInHierarchy, Is.True);
                Assert.That(_debugEntryButton.transform.parent,
                    Is.SameAs(canvasObject.transform));
                Assert.That(_debugEntryButton.transform.GetSiblingIndex(),
                    Is.LessThan(eventRoot.transform.GetSiblingIndex()));

                _debugPanel.ExitDebugSession();

                Assert.That(_controller.IsTitleVisible, Is.True);
                Assert.That(_debugEntryButton.transform.parent,
                    Is.SameAs(_titleRoot.transform),
                    "returning to the start page restores the saved parent.");
                Assert.That(_debugEntryButton.gameObject.activeInHierarchy, Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(tactical);
                UnityEngine.Object.DestroyImmediate(eventRoot);
                UnityEngine.Object.DestroyImmediate(canvasObject);
            }
        }


        [Test]
        public void DebugEntry_IsHiddenWhileTheWindowIsOpenAndReturnsAfterClose()
        {
            Assert.That(_debugEntryButton.gameObject.activeInHierarchy, Is.True,
                "the entry is offered on the title page.");

            _debugEntryButton.onClick.Invoke();

            Assert.That(_debugPanel.IsOpen, Is.True);
            Assert.That(_debugEntryButton.gameObject.activeInHierarchy, Is.False,
                "the entry must not cover the open window.");

            _debugPanel.Close();

            Assert.That(_debugPanel.IsOpen, Is.False);
            Assert.That(_session.IsDebugSessionActive, Is.True,
                "closing keeps the live debug session.");
            Assert.That(_debugEntryButton.gameObject.activeInHierarchy, Is.True,
                "the entry returns once the window is closed so the session can be reopened.");

            _debugPanel.ResetDebugSession();

            Assert.That(_controller.IsTitleVisible, Is.True);
            Assert.That(_session.IsDebugSessionActive, Is.False);
            Assert.That(_debugEntryButton.gameObject.activeInHierarchy, Is.True,
                "the entry stays visible on the restored title page.");
        }

        [Test]
        public void DebugEntry_IsHiddenDuringOrdinaryGameplay()
        {
            Assert.That(_debugEntryButton.gameObject.activeInHierarchy, Is.True,
                "the entry is offered on the title page.");

            _controller.StartNewGame();

            Assert.That(_controller.IsTitleVisible, Is.False);
            Assert.That(_debugEntryButton.gameObject.activeInHierarchy, Is.False,
                "the development entry must not float over an ordinary game.");
        }

        [Test]
        public void StartupPath_DoesNotReadThePlayerSlot()
        {
            // Construction and ShowTitle are the startup path the scene uses; neither may
            // read the slot, so the Debug entry can start before any save check.
            Assert.That(_controller.SlotLoadCount, Is.Zero);

            _controller.ShowTitle();

            Assert.That(_controller.SlotLoadCount, Is.Zero);
            Assert.That(_controller.IsMainMenuVisible, Is.True);
        }

        [Test]
        public void DebugModeEntry_StartsAnIsolatedSessionAndOpensTheWindow()
        {
            Assert.That(_controller.SlotLoadCount, Is.Zero);
            Assert.That(_controller.DebugModeEntryBound, Is.True);
            Assert.That(_controller.DebugModePanelBound, Is.True);
            Assert.That(_debugPanel.IsOpen, Is.False);

            _debugEntryButton.onClick.Invoke();

            Assert.That(_controller.SlotLoadCount, Is.Zero,
                "the Debug entry must not read the player slot.");
            Assert.That(_session.IsDebugSessionActive, Is.True);
            Assert.That(_debugPanel.IsOpen, Is.True,
                "the entry must open the window that owns the session.");
            Assert.That(_debugPanel.Session.IsActive, Is.True);
            Assert.That(_session.Succession.World.Family.Id,
                Is.EqualTo(SuccessionSample.StandardFamilyId));
            Assert.That(_controller.IsTitleVisible, Is.False);
            Assert.That(_controller.Status, Does.Contain("Debug"));
            Assert.That(_controller.Status, Does.Contain("不会读写玩家存档"));
        }

        [Test]
        public void DebugModeEntry_WithoutAWindow_DoesNotStartASession()
        {
            _controller.BindDebugModePanel(null);

            Assert.That(_controller.StartDebugSession(), Is.False);

            Assert.That(_session.IsDebugSessionActive, Is.False,
                "the entry must never enter a session with no window.");
            Assert.That(_controller.IsTitleVisible, Is.True);
            Assert.That(_controller.Status, Does.Contain("未接入"));
            Assert.That(_controller.SlotLoadCount, Is.Zero);
        }

        [Test]
        public void DebugModeEntry_InvokedWhileTheTitleIsHidden_DoesNotStartASession()
        {
            // 验证脚本直接调用 onClick，因此即使隐藏的标题层让入口不在屏幕上，
            // 也仍能把它按下去。从这个状态进入 Debug 模式会把普通战役
            // 变成玩家槽被拒的调试会话。
            Assert.That(_controller.StartNewGame(), Is.True);
            Assert.That(_controller.IsTitleVisible, Is.False);
            Assert.That(_debugEntryButton.gameObject.activeInHierarchy, Is.False,
                "普通战役进行中，入口不应出现在屏幕上。");

            _debugEntryButton.onClick.Invoke();

            Assert.That(_session.IsDebugSessionActive, Is.False,
                "按动隐藏的入口不应创建调试会话。");
            Assert.That(_debugPanel.IsOpen, Is.False);
            Assert.That(_session.CurrentDebugSession, Is.Null);
            Assert.That(_controller.IsTitleVisible, Is.False,
                "被拒绝的入口不应改变当前界面。");
            Assert.That(_controller.LastOperationSucceeded, Is.False);
            Assert.That(_controller.Status, Does.Contain("标题页"));
            Assert.That(_session.Succession.World.Family.Id,
                Is.EqualTo(new FamilyId("family.akechi.sc013")),
                "被拒绝的入口不应改动普通战役。");
            Assert.That(_controller.SlotLoadCount, Is.Zero);
        }

        [Test]
        public void NormalStart_PlayerSlotRoundTripsDateFamilyAndResources()
        {
            Assert.That(_controller.StartNewGame(), Is.True);
            Assert.That(_session.IsDebugSessionActive, Is.False);
            Assert.That(_debugPanel.IsOpen, Is.False, "调试窗口应保持关闭。");

            GameDate savedDate = _session.WorldClock.CurrentDate;
            int savedFunds = _session.Industry.Resources.Funds;
            int savedSupplies = _session.Industry.Resources.Supplies;
            PersonId savedHead = _session.Succession.HeadPersonId;

            SaveOperationResult saved = _session.SaveTo(_store);
            Assert.That(saved.Succeeded, Is.True, saved.Message);
            Assert.That(_store.Exists(), Is.True);

            Assert.That(_session.WorldClock.AdvanceOneDay(), Is.True);
            Assert.That(_session.WorldClock.CurrentDate, Is.Not.EqualTo(savedDate));

            SaveOperationResult loaded = _session.LoadFrom(_store);
            Assert.That(loaded.Succeeded, Is.True, loaded.Message);
            Assert.That(_session.WorldClock.CurrentDate, Is.EqualTo(savedDate));
            Assert.That(_session.Industry.Resources.Funds, Is.EqualTo(savedFunds));
            Assert.That(_session.Industry.Resources.Supplies, Is.EqualTo(savedSupplies));
            Assert.That(_session.Succession.HeadPersonId, Is.EqualTo(savedHead));
        }

        [Test]
        public void DebugModeEntry_PlayerSlotSaveIsStillRejectedAndWritesNothing()
        {
            _debugEntryButton.onClick.Invoke();
            Assert.That(_session.IsDebugSessionActive, Is.True);

            SaveOperationResult result = _session.SaveTo(_store);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Message, Is.EqualTo("调试会话不会写入玩家存档。"));
            Assert.That(_store.Exists(), Is.False,
                "调试会话不应创建玩家存档槽。");
            Assert.That(File.Exists(_store.FilePath), Is.False,
                "调试会话不应写入玩家存档文件。");
        }

        [Test]
        public void NormalStartAfterDebugMode_LeavesTheDebugSessionAndClosesTheWindow()
        {
            _debugEntryButton.onClick.Invoke();
            Assert.That(_session.IsDebugSessionActive, Is.True);
            Assert.That(_debugPanel.IsOpen, Is.True);

            _controller.StartNewGame();

            Assert.That(_session.IsDebugSessionActive, Is.False);
            Assert.That(_session.Succession.World.Family.Id,
                Is.EqualTo(new FamilyId("family.akechi.sc013")));
            Assert.That(_debugPanel.IsOpen, Is.False,
                "a normal start ends the session and must not leave the debug window over it.");
            Assert.That(_controller.SlotLoadCount, Is.Zero);
        }

        [Test]
        public void MissingSlot_DisablesContinueAndPromptsNewGame()
        {
            _controller.RefreshTitleState();

            Assert.That(_continueButton.interactable, Is.False);
            Assert.That(_controller.Status, Does.Contain("尚未找到存档"));
            Assert.That(_controller.SlotLoadCount, Is.EqualTo(1),
                "one explicit refresh is the only slot read.");
            Assert.That(_statusText.text, Is.EqualTo(_controller.Status));
            Assert.That(_controller.IsTitleVisible, Is.True);
            Assert.That(_controller.IsMainMenuVisible, Is.True);
            Assert.That(_controller.IsScenarioSelectionVisible, Is.False);
        }

        [Test]
        public void TryContinue_WithMissingSlot_KeepsTitleAndSession()
        {
            Assert.That(_controller.TryContinue(), Is.False);
            Assert.That(_controller.LastOperationSucceeded, Is.False);
            Assert.That(_controller.IsTitleVisible, Is.True);
            Assert.That(_session.WorldClock.CurrentDate.Day, Is.EqualTo(1));
        }

        [Test]
        public void ValidSlot_EnablesContinueAndShowsSummary()
        {
            Assert.That(_store.Save(CompletedData()).Succeeded, Is.True);

            _controller.RefreshTitleState();

            Assert.That(_continueButton.interactable, Is.True);
            Assert.That(_controller.Status, Does.Contain("可继续上次进度"));
        }

        [Test]
        public void TryContinue_LoadsSavedStateAndHidesTitle()
        {
            Assert.That(_store.Save(CompletedData()).Succeeded, Is.True);

            Assert.That(_controller.TryContinue(), Is.True);

            Assert.That(_controller.IsTitleVisible, Is.False);
            Assert.That(_session.WorldClock.CurrentDate.Day, Is.EqualTo(5));
            Assert.That(_session.CurrentLocation, Is.EqualTo(LocationId.Kiyosu));
            Assert.That(_session.SupplyShortageStage, Is.EqualTo(SupplyShortageStage.RewardClaimed));
            Assert.That(_session.SealedLetterStage, Is.EqualTo(SealedLetterStage.RewardClaimed));
            Assert.That(_session.MarketTallyStage, Is.EqualTo(MarketTallyStage.RewardClaimed));
            Assert.That(_session.Merit, Is.EqualTo(20));
            Assert.That(_controller.Status, Does.Contain("已继续上次进度"));
        }

        [Test]
        public void CorruptSlot_DisablesContinueAndTryContinueReportsWithoutMutation()
        {
            Directory.CreateDirectory(_directory);
            File.WriteAllText(_store.FilePath, "{ this is not json");

            _controller.RefreshTitleState();
            Assert.That(_continueButton.interactable, Is.False);
            Assert.That(_controller.Status, Is.Not.Empty);

            Assert.That(_controller.TryContinue(), Is.False);
            Assert.That(_controller.IsTitleVisible, Is.True);
            Assert.That(_session.WorldClock.CurrentDate.Day, Is.EqualTo(1));
            Assert.That(File.ReadAllText(_store.FilePath), Is.EqualTo("{ this is not json"));
        }

        [Test]
        public void UnsupportedVersion_IsReportedGracefully()
        {
            SaveData data = CompletedData();
            data.saveFormatVersion = SaveDataValidator.CurrentVersion + 1;
            Directory.CreateDirectory(_directory);
            File.WriteAllText(_store.FilePath, UnityEngine.JsonUtility.ToJson(data));

            _controller.RefreshTitleState();

            Assert.That(_continueButton.interactable, Is.False);
            Assert.That(_controller.TryContinue(), Is.False);
            Assert.That(_controller.IsTitleVisible, Is.True);
        }

        [Test]
        public void StartNewGame_ResetsSessionAndHidesTitle()
        {
            Assert.That(_session.AdvanceDays(3), Is.True);

            Assert.That(_controller.StartNewGame(), Is.True);

            Assert.That(_controller.IsTitleVisible, Is.False);
            Assert.That(_session.WorldClock.CurrentDate.Day, Is.EqualTo(1));
            Assert.That(_session.CurrentLocation, Is.EqualTo(LocationId.Kiyosu));
            Assert.That(_session.Merit, Is.EqualTo(0));
            Assert.That(_controller.Status, Does.Contain("SC-013"));
        }

        [Test]
        public void ScenarioSelectionButton_OpensSelectorWithoutStartingOrMutatingSession()
        {
            Assert.That(_session.AdvanceDays(3), Is.True);
            int day = _session.WorldClock.CurrentDate.Day;
            FamilyId familyId = _session.Succession.World.Family.Id;
            bool m2Active = _session.IsM2ScenarioActive;

            _newGameButton.onClick.Invoke();

            Assert.That(_controller.IsTitleVisible, Is.True);
            Assert.That(_controller.IsMainMenuVisible, Is.False);
            Assert.That(_controller.IsScenarioSelectionVisible, Is.True);
            Assert.That(_session.WorldClock.CurrentDate.Day, Is.EqualTo(day));
            Assert.That(_session.Succession.World.Family.Id, Is.EqualTo(familyId));
            Assert.That(_session.IsM2ScenarioActive, Is.EqualTo(m2Active));
        }

        [Test]
        public void ScenarioSelectionBackButton_ReturnsToMainMenu()
        {
            _newGameButton.onClick.Invoke();
            Assert.That(_controller.IsScenarioSelectionVisible, Is.True);

            _backButton.onClick.Invoke();

            Assert.That(_controller.IsTitleVisible, Is.True);
            Assert.That(_controller.IsMainMenuVisible, Is.True);
            Assert.That(_controller.IsScenarioSelectionVisible, Is.False);
            Assert.That(_session.WorldClock.CurrentDate.Day, Is.EqualTo(1));
        }

        [Test]
        public void ShowTitle_ResetsToMainMenu()
        {
            _newGameButton.onClick.Invoke();
            Assert.That(_controller.IsScenarioSelectionVisible, Is.True);

            _controller.ShowTitle();

            Assert.That(_controller.IsTitleVisible, Is.True);
            Assert.That(_controller.IsMainMenuVisible, Is.True);
            Assert.That(_controller.IsScenarioSelectionVisible, Is.False);
        }

        [Test]
        public void FixedScenarioEntry_StartsTheSc013Scenario()
        {
            _fixedScenarioButton.onClick.Invoke();

            Assert.That(_controller.IsTitleVisible, Is.False);
            Assert.That(_session.IsM2ScenarioActive, Is.True);
            Assert.That(_session.Succession.World.Family.Id,
                Is.EqualTo(new FamilyId("family.akechi.sc013")));
            Assert.That(_controller.Status, Does.Contain("SC-013"));
        }

        [Test]
        public void Bindings_AreIdempotentWithinTheBindLifecycle()
        {
            _controller.BindScenarioSelection(
                _mainMenuRoot, _selectorRoot, _fixedScenarioButton, _backButton);
            _controller.BindScenarioButtons(_standardButton, _crisisButton);
            _controller.BindEducationScenarioButton(_educationButton);

            // EditMode does not reliably call OnDisable/OnEnable from the enabled property,
            // so invoke the real lifecycle methods explicitly.
            InvokeLifecycle(_controller, "OnDisable");
            _newGameButton.onClick.Invoke();
            _fixedScenarioButton.onClick.Invoke();
            _standardButton.onClick.Invoke();
            _educationButton.onClick.Invoke();

            Assert.That(_controller.IsTitleVisible, Is.True);
            Assert.That(_controller.IsMainMenuVisible, Is.True);
            Assert.That(_controller.IsScenarioSelectionVisible, Is.False);
            Assert.That(_session.WorldClock.CurrentDate.Day, Is.EqualTo(1));
            Assert.That(_session.IsM2ScenarioActive, Is.True, "InitNewGame from SetUp must remain the only start.");

            InvokeLifecycle(_controller, "OnEnable");
            _newGameButton.onClick.Invoke();
            Assert.That(_controller.IsScenarioSelectionVisible, Is.True);
        }

        private static void InvokeLifecycle(TitleScreenController controller, string methodName)
        {
            MethodInfo method = typeof(TitleScreenController).GetMethod(
                methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, $"Missing {methodName} lifecycle method.");
            method.Invoke(controller, null);
        }

        [Test]
        public void NewGameButton_WithoutSelector_FallsBackToDirectSc013Start()
        {
            _controller.BindScenarioSelection(null, null, null, null);

            _newGameButton.onClick.Invoke();

            Assert.That(_controller.IsTitleVisible, Is.False);
            Assert.That(_session.IsM2ScenarioActive, Is.True);
            Assert.That(_session.Succession.World.Family.Id,
                Is.EqualTo(new FamilyId("family.akechi.sc013")));
            Assert.That(_controller.Status, Does.Contain("SC-013"));
        }

        [Test]
        public void BindScenarioSelection_WithNulls_DetachesPreviousNavigationListeners()
        {
            _controller.BindScenarioSelection(null, null, null, null);

            _fixedScenarioButton.onClick.Invoke();
            _backButton.onClick.Invoke();

            Assert.That(_controller.IsTitleVisible, Is.True);
            Assert.That(_session.WorldClock.CurrentDate.Day, Is.EqualTo(1));
            Assert.That(_session.IsM2ScenarioActive, Is.True);
            Assert.That(_controller.LastOperationSucceeded, Is.False);
        }

        [Test]
        public void ScenarioSelectionVisibility_TracksTitleVisibilityAfterAStart()
        {
            _newGameButton.onClick.Invoke();
            Assert.That(_controller.IsScenarioSelectionVisible, Is.True);

            _fixedScenarioButton.onClick.Invoke();

            Assert.That(_controller.IsTitleVisible, Is.False);
            Assert.That(_controller.IsScenarioSelectionVisible, Is.False);
        }

        [Test]
        public void EveryScenarioEntry_IsReachableAndLeavesTheSaveSlotUntouched()
        {
            AssertEntryStarts(_fixedScenarioButton, () =>
                Assert.That(_session.Succession.World.Family.Id,
                    Is.EqualTo(new FamilyId("family.akechi.sc013"))));
            AssertEntryStarts(_standardButton, () =>
                Assert.That(_session.Succession.World.Family.Id,
                    Is.EqualTo(SuccessionSample.StandardFamilyId)));
            AssertEntryStarts(_crisisButton, () =>
            {
                Assert.That(_session.Succession.World.Family.Id,
                    Is.EqualTo(SuccessionSample.StandardFamilyId));
                Assert.That(_session.Succession.World.PersonCount, Is.EqualTo(4));
            });
            AssertEntryStarts(_educationButton, () =>
                Assert.That(_session.Succession.World.Family.Id,
                    Is.EqualTo(SC020EducationScenario.FamilyId)));
            AssertEntryStarts(_researchButton, () =>
                Assert.That(_session.Succession.World.Family.Id,
                    Is.EqualTo(SC021FamilyResearchScenario.FamilyId)));
            AssertEntryStarts(_familyFormationButton, () =>
                Assert.That(_session.Succession.World.Family.Id,
                    Is.EqualTo(SC022FamilyFormationScenario.FamilyId)));
            AssertEntryStarts(_seclusionButton, () =>
                Assert.That(_session.Succession.World.Family.Id,
                    Is.EqualTo(SC023SeclusionScenario.FamilyId)));
            AssertEntryStarts(_familyDoctrineButton, () =>
                Assert.That(SC024FamilyDoctrineScenario.HasHeir(_session.Succession), Is.True));
        }

        private void AssertEntryStarts(Button entry, Action assertSessionState)
        {
            _controller.ShowTitle();
            Assert.That(File.Exists(_store.FilePath), Is.False);
            _newGameButton.onClick.Invoke();
            Assert.That(_controller.IsMainMenuVisible, Is.False);
            Assert.That(_controller.IsScenarioSelectionVisible, Is.True);

            entry.onClick.Invoke();

            Assert.That(_controller.IsTitleVisible, Is.False);
            Assert.That(_controller.IsMainMenuVisible, Is.False);
            Assert.That(_controller.IsScenarioSelectionVisible, Is.False);
            Assert.That(File.Exists(_store.FilePath), Is.False,
                "Starting a scenario must not create or overwrite the save slot.");
            assertSessionState();
        }

        [Test]
        public void AllScenarioEntries_AndNavigation_AreBound()
        {
            Assert.That(_controller.ScenarioEntriesBound, Is.True);
            Assert.That(_controller.EducationScenarioBound, Is.True);
            Assert.That(_controller.ResearchScenarioBound, Is.True);
            Assert.That(_controller.FamilyFormationScenarioBound, Is.True);
            Assert.That(_controller.SeclusionScenarioBound, Is.True);
            Assert.That(_controller.FamilyDoctrineScenarioBound, Is.True);
            Assert.That(_controller.FixedScenarioBound, Is.True);
            Assert.That(_controller.BackButtonBound, Is.True);
        }

        [Test]
        public void StartsM2StandardAndCrisisEntriesIndependently()
        {
            Assert.That(_controller.StartNewGame(), Is.True);
            Assert.That(_session.IsM2ScenarioActive, Is.True);
            Assert.That(_session.Succession.World.Family.Id,
                Is.EqualTo(new FamilyId("family.akechi.sc013")));
            Assert.That(_session.SuccessionHeadName, Is.EqualTo("明智 晴信"));
            Assert.That(_session.Succession.World.PersonCount, Is.EqualTo(6));

            Assert.That(_controller.StartStandardSample(), Is.True);
            Assert.That(_session.IsM2ScenarioActive, Is.False);
            Assert.That(_session.Succession.World.Family.Id,
                Is.EqualTo(SuccessionSample.StandardFamilyId));
            Assert.That(_session.Succession.HeadPersonId, Is.EqualTo(SuccessionSample.StandardHeadId));

            Assert.That(_controller.StartCrisisDemo(), Is.True);
            Assert.That(_session.IsM2ScenarioActive, Is.False);
            Assert.That(_session.Succession.World.Family.Id,
                Is.EqualTo(SuccessionSample.StandardFamilyId));
            Assert.That(_session.Succession.World.PersonCount, Is.EqualTo(4));
            Assert.That(_session.Succession.World.TryGetPerson(
                new PersonId("person.sc013.harunobu"), out _), Is.False,
                "the crisis entry must not retain any M2 roster data");
        }

        [Test]
        public void Buttons_InvokeTheirActions()
        {
            _newGameButton.onClick.Invoke();
            Assert.That(_controller.IsScenarioSelectionVisible, Is.True);
            Assert.That(_controller.IsTitleVisible, Is.True);

            _backButton.onClick.Invoke();
            Assert.That(_controller.IsMainMenuVisible, Is.True);

            _continueButton.onClick.Invoke();
            Assert.That(_controller.LastOperationSucceeded, Is.False);
            Assert.That(_controller.IsTitleVisible, Is.True);
        }
    }
}
