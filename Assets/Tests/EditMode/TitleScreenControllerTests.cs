using System;
using System.IO;
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
        private Button _newGameButton;
        private Button _continueButton;
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
            _newGameButton = new GameObject("NewGameButton", typeof(RectTransform)).AddComponent<Button>();
            _continueButton = new GameObject("ContinueButton", typeof(RectTransform)).AddComponent<Button>();
            _statusText = new GameObject("Status", typeof(RectTransform)).AddComponent<Text>();

            _controller.Bind(_session, _store, null, _titleRoot, _newGameButton, _continueButton, _statusText);
        }

        [TearDown]
        public void TearDown()
        {
            if (_controllerObject != null)
                Object.DestroyImmediate(_controllerObject);
            if (_titleRoot != null)
                Object.DestroyImmediate(_titleRoot);
            if (_newGameButton != null)
                Object.DestroyImmediate(_newGameButton.gameObject);
            if (_continueButton != null)
                Object.DestroyImmediate(_continueButton.gameObject);
            if (_statusText != null)
                Object.DestroyImmediate(_statusText.gameObject);
            if (_session != null)
                Object.DestroyImmediate(_session.gameObject);
            if (Directory.Exists(_directory))
                Directory.Delete(_directory, true);
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
            _controller.ShowTitle();
            _controller.RefreshTitleState();

            Assert.That(_continueButton.interactable, Is.False);
            Assert.That(_controller.Status, Does.Contain("尚未找到存档"));
            Assert.That(_statusText.text, Is.EqualTo(_controller.Status));
            Assert.That(_controller.IsTitleVisible, Is.True);
        }

        [Test]
        public void TryContinue_WithMissingSlot_KeepsTitleAndSession()
        {
            _controller.ShowTitle();

            Assert.That(_controller.TryContinue(), Is.False);
            Assert.That(_controller.LastOperationSucceeded, Is.False);
            Assert.That(_controller.IsTitleVisible, Is.True);
            Assert.That(_session.WorldClock.CurrentDate.Day, Is.EqualTo(1));
        }

        [Test]
        public void ValidSlot_EnablesContinueAndShowsSummary()
        {
            Assert.That(_store.Save(CompletedData()).Succeeded, Is.True);

            _controller.ShowTitle();
            _controller.RefreshTitleState();

            Assert.That(_continueButton.interactable, Is.True);
            Assert.That(_controller.Status, Does.Contain("可继续上次进度"));
        }

        [Test]
        public void TryContinue_LoadsSavedStateAndHidesTitle()
        {
            Assert.That(_store.Save(CompletedData()).Succeeded, Is.True);
            _controller.ShowTitle();

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

            _controller.ShowTitle();
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

            _controller.ShowTitle();
            _controller.RefreshTitleState();

            Assert.That(_continueButton.interactable, Is.False);
            Assert.That(_controller.TryContinue(), Is.False);
            Assert.That(_controller.IsTitleVisible, Is.True);
        }

        [Test]
        public void StartNewGame_ResetsSessionAndHidesTitle()
        {
            Assert.That(_session.AdvanceDays(3), Is.True);
            _controller.ShowTitle();

            Assert.That(_controller.StartNewGame(), Is.True);

            Assert.That(_controller.IsTitleVisible, Is.False);
            Assert.That(_session.WorldClock.CurrentDate.Day, Is.EqualTo(1));
            Assert.That(_session.CurrentLocation, Is.EqualTo(LocationId.Kiyosu));
            Assert.That(_session.Merit, Is.EqualTo(0));
            Assert.That(_controller.Status, Does.Contain("已开始新游戏"));
        }

        [Test]
        public void Buttons_InvokeTheirActions()
        {
            _controller.ShowTitle();
            _newGameButton.onClick.Invoke();
            Assert.That(_controller.IsTitleVisible, Is.False);

            _controller.ShowTitle();
            _continueButton.onClick.Invoke();
            Assert.That(_controller.LastOperationSucceeded, Is.False);
            Assert.That(_controller.IsTitleVisible, Is.True);
        }
    }
}
