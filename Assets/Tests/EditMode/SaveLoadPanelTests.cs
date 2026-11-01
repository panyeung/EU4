using System;
using System.IO;
using NUnit.Framework;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;
using SengokuChronicle.Persistence;
using SengokuChronicle.Presentation;
using UnityEngine;
using UnityEngine.UI;

namespace SengokuChronicle.Tests.EditMode
{
    public class SaveLoadPanelTests
    {
        private GameSession _session;
        private GameObject _panelObject;
        private SaveLoadPanel _panel;
        private Button _saveButton;
        private Button _loadButton;
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

            _panelObject = new GameObject("SaveLoadPanel");
            _panel = _panelObject.AddComponent<SaveLoadPanel>();

            _saveButton = new GameObject("SaveButton", typeof(RectTransform)).AddComponent<Button>();
            _loadButton = new GameObject("LoadButton", typeof(RectTransform)).AddComponent<Button>();
            _statusText = new GameObject("Status", typeof(RectTransform)).AddComponent<Text>();

            _panel.Bind(_session, _store, _saveButton, _loadButton, _statusText, null);
        }

        [TearDown]
        public void TearDown()
        {
            if (_panelObject != null)
                UnityEngine.Object.DestroyImmediate(_panelObject);
            if (_saveButton != null)
                UnityEngine.Object.DestroyImmediate(_saveButton.gameObject);
            if (_loadButton != null)
                UnityEngine.Object.DestroyImmediate(_loadButton.gameObject);
            if (_statusText != null)
                UnityEngine.Object.DestroyImmediate(_statusText.gameObject);
            if (_session != null)
                UnityEngine.Object.DestroyImmediate(_session.gameObject);
            if (Directory.Exists(_directory))
                Directory.Delete(_directory, true);
        }

        [Test]
        public void Save_WritesSlotAndReportsSuccess()
        {
            _session.AdvanceOneDay();

            _panel.Save();

            Assert.That(_panel.LastOperationSucceeded, Is.True, _panel.Status);
            Assert.That(_panel.Status, Does.Contain("已保存"));
            Assert.That(_statusText.text, Is.EqualTo(_panel.Status));
            Assert.That(_store.Exists(), Is.True);
            Assert.That(_store.Load().Data.day, Is.EqualTo(2));
        }

        [Test]
        public void SaveButton_InvokesSave()
        {
            _saveButton.onClick.Invoke();

            Assert.That(_store.Exists(), Is.True);
            Assert.That(_panel.LastOperationSucceeded, Is.True);
        }

        [Test]
        public void LoadButton_InvokesLoad()
        {
            _session.AdvanceOneDay();
            _panel.Save();
            _session.InitNewGame();

            _loadButton.onClick.Invoke();

            Assert.That(_panel.LastOperationSucceeded, Is.True, _panel.Status);
            Assert.That(_session.WorldClock.CurrentDate, Is.EqualTo(new GameDate(1560, 4, 2)));
        }

        [Test]
        public void Load_RestoresSessionState()
        {
            _session.AdvanceOneDay();
            _session.TryChangeRelationship(CharacterId.KurodaIori, 6);
            _panel.Save();
            _session.InitNewGame();

            _panel.Load();

            Assert.That(_panel.LastOperationSucceeded, Is.True, _panel.Status);
            Assert.That(_session.WorldClock.CurrentDate, Is.EqualTo(new GameDate(1560, 4, 2)));
            Assert.That(_session.TryGetRelationship(CharacterId.KurodaIori).CurrentValue, Is.EqualTo(6));
        }

        [Test]
        public void Load_MissingSlot_ReportsErrorAndKeepsState()
        {
            GameDate date = _session.WorldClock.CurrentDate;

            _panel.Load();

            Assert.That(_panel.LastOperationSucceeded, Is.False);
            Assert.That(_panel.Status, Is.Not.Empty);
            Assert.That(_statusText.text, Is.EqualTo(_panel.Status));
            Assert.That(_session.WorldClock.CurrentDate, Is.EqualTo(date));
        }

        [Test]
        public void Save_WithoutSession_ReportsError()
        {
            _panel.Bind(null, _store, _saveButton, _loadButton, _statusText, null);

            _panel.Save();

            Assert.That(_panel.LastOperationSucceeded, Is.False);
            Assert.That(_panel.Status, Does.Contain("GameSession"));
        }

        [Test]
        public void Load_WithoutStore_ReportsError()
        {
            _panel.SetStore(null);

            _panel.Load();

            Assert.That(_panel.LastOperationSucceeded, Is.False);
            Assert.That(_panel.Status, Does.Contain("存档槽"));
        }

        [Test]
        public void Bind_IsIdempotentForButtonListeners()
        {
            _panel.Bind(_session, _store, _saveButton, _loadButton, _statusText, null);
            _session.AdvanceOneDay();

            _saveButton.onClick.Invoke();

            Assert.That(_store.Exists(), Is.True);
            Assert.That(_panel.LastOperationSucceeded, Is.True);
        }
    }
}
