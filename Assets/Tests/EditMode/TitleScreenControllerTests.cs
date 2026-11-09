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
        public void MissingSlot_DisablesContinueAndPromptsNewGame()
        {
            _controller.RefreshTitleState();

            Assert.That(_continueButton.interactable, Is.False);
            Assert.That(_controller.Status, Does.Contain("尚未找到存档"));
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
