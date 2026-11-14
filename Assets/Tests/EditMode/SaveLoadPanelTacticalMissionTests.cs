using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;
using SengokuChronicle.Persistence;
using SengokuChronicle.Presentation;
using Object = UnityEngine.Object;

namespace SengokuChronicle.Tests.EditMode
{
    /// <summary>
    /// The M06 operation owns a session-only state, so the in-game save and load controls
    /// must be visibly disabled while it runs and restored at a terminal result. The
    /// GameSession boundary refusal is covered separately; this file asserts the control
    /// state a player sees.
    /// </summary>
    public sealed class SaveLoadPanelTacticalMissionTests
    {
        private GameSession _session;
        private GameObject _host;
        private SaveLoadPanel _panel;
        private SaveSlotStore _store;
        private string _directory;

        [SetUp]
        public void SetUp()
        {
            _session = GameSession.NewGame();
            _directory = Path.Combine(Path.GetTempPath(), "SengokuChronicleTests",
                System.Guid.NewGuid().ToString("N"));
            _store = new SaveSlotStore(_directory);

            _host = new GameObject("SaveLoadPanelHost", typeof(RectTransform));
            Button save = MakeButton(_host.transform, "Save", "存档");
            Button load = MakeButton(_host.transform, "Load", "读档");
            Text status = MakeText(_host.transform, "Status");
            _panel = _host.AddComponent<SaveLoadPanel>();
            _panel.Bind(_session, _store, save, load, status, null);
        }

        [TearDown]
        public void TearDown()
        {
            if (_host != null) Object.DestroyImmediate(_host);
            if (_session != null) Object.DestroyImmediate(_session.gameObject);
            if (Directory.Exists(_directory))
                Directory.Delete(_directory, true);
        }

        [Test]
        public void Controls_AreDisabledWhileTheOperationRunsAndRestoredAfterTerminal()
        {
            Assert.That(_panel.SaveInteractable, Is.True);
            Assert.That(_panel.LoadInteractable, Is.True);

            var eligible = new List<PersonId>();
            foreach (CommissionAssigneeOption option in _session.TacticalOperativeOptions)
                if (option.Eligible)
                    eligible.Add(option.PersonId);
            Assert.That(eligible.Count, Is.GreaterThanOrEqualTo(2));
            Assert.That(_session.TryBeginTacticalMission(eligible[0], eligible[1]).Succeeded,
                Is.True);

            Assert.That(_panel.SaveInteractable, Is.False,
                "the session event must disable the save control at once.");
            Assert.That(_panel.LoadInteractable, Is.False);
            Assert.That(_panel.Status, Does.Contain("潜入"));

            _panel.Save();
            Assert.That(_panel.LastOperationSucceeded, Is.False);
            Assert.That(_panel.Status, Does.Contain("潜入"));
            _panel.Load();
            Assert.That(_panel.LastOperationSucceeded, Is.False);
            Assert.That(_session.IsTacticalMissionActive, Is.True,
                "a refused control must not replace the campaign.");
            Assert.That(File.Exists(_store.FilePath), Is.False);

            Assert.That(_session.TryTacticalWithdraw().Succeeded, Is.True);
            Assert.That(_panel.SaveInteractable, Is.True);
            Assert.That(_panel.LoadInteractable, Is.True);

            _panel.Save();
            Assert.That(_panel.LastOperationSucceeded, Is.True,
                "ordinary saving is allowed again after the terminal result.");
            Assert.That(File.Exists(_store.FilePath), Is.True);
        }

        private static Text MakeText(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            Text text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return text;
        }

        private static Button MakeButton(Transform parent, string name, string label)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image),
                typeof(Button));
            go.transform.SetParent(parent, false);
            Image image = go.GetComponent<Image>();
            Button button = go.GetComponent<Button>();
            button.targetGraphic = image;
            Text text = MakeText(go.transform, "Label");
            text.text = label;
            return button;
        }
    }
}
