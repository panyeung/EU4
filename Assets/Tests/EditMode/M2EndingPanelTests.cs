using NUnit.Framework;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;
using SengokuChronicle.Events;
using SengokuChronicle.Persistence;
using SengokuChronicle.Presentation;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SengokuChronicle.Tests.EditMode
{
    public sealed class M2EndingPanelTests
    {
        private GameSession _session;
        private GameObject _panelRoot;
        private GameObject _titleObject;
        private GameObject _bodyObject;
        private GameObject _closeObject;
        private M2EndingPanel _panel;

        [SetUp]
        public void SetUp()
        {
            _session = GameSession.NewGame();
            _panelRoot = new GameObject("M2EndingPanel", typeof(RectTransform));
            _titleObject = new GameObject("Title", typeof(RectTransform));
            _bodyObject = new GameObject("Body", typeof(RectTransform));
            _closeObject = new GameObject("Close", typeof(RectTransform));
            _titleObject.transform.SetParent(_panelRoot.transform, false);
            _bodyObject.transform.SetParent(_panelRoot.transform, false);
            _closeObject.transform.SetParent(_panelRoot.transform, false);
            _panel = _panelRoot.AddComponent<M2EndingPanel>();
            _panelRoot.SetActive(false);
        }

        [TearDown]
        public void TearDown()
        {
            if (_panelRoot != null) Object.DestroyImmediate(_panelRoot);
            if (_session != null) Object.DestroyImmediate(_session.gameObject);
        }

        [Test]
        public void FinalChoices_ShowDistinctSummariesAndAllowFurtherPlay()
        {
            DriveToFinale();
            SaveData checkpoint = _session.CaptureSave();
            Choose(M2FixedScenario.SeekAidOptionId);

            Bind();
            Assert.That(_panel.IsVisible, Is.True);
            Assert.That(_panel.Title, Does.Contain("旧交来援"));
            Assert.That(_panel.Body, Does.Contain("应邀"));
            Assert.That(_panel.StaticButtonBindingCount, Is.EqualTo(1));
            string seekAidBody = _panel.Body;

            _panel.Close();
            Assert.That(_panel.IsVisible, Is.False);
            Assert.That(_session.AdvanceOneDay(), Is.True,
                "closing the ending must not freeze normal play");

            Assert.That(_session.TryApplySave(checkpoint).Succeeded, Is.True);
            Choose(M2FixedScenario.StandAloneOptionId);
            _panel.Refresh();

            Assert.That(_panel.IsVisible, Is.True);
            Assert.That(_panel.Title, Does.Contain("独自承担"));
            Assert.That(_panel.Body, Is.Not.EqualTo(seekAidBody));
            Assert.That(_panel.Body, Does.Contain("独自承担"));
        }

        [Test]
        public void NoFinalChoice_HidesTheEndingPanel()
        {
            Bind();
            Assert.That(_panel.IsVisible, Is.False);
            Assert.That(_panel.Title, Is.Empty);
        }

        private void Bind()
        {
            _panel.BindReferences(_session, _panelRoot,
                _titleObject.AddComponent<Text>(), _bodyObject.AddComponent<Text>(),
                _closeObject.AddComponent<Button>());
        }

        private void DriveToFinale()
        {
            Assert.That(_session.TryAdvanceToNextMonth(), Is.True, _session.LastEventMessage);
            Choose(M2FixedScenario.KeepPromiseOptionId);
            Assert.That(_session.TryAdvanceToNextMonth(), Is.True, _session.LastEventMessage);
            Choose(SampleEventIds.OfferRestOptionId);
            Assert.That(_session.BeginSuccessionRetirement().Succeeded, Is.True);
            Assert.That(_session.TryAdvanceToNextMonth(), Is.True, _session.LastEventMessage);
            Choose(SC011EventIds.LegacyHonoredRenewOptionId);
            Assert.That(_session.BeginSuccessionRetirement().Succeeded, Is.True);
            Assert.That(_session.TryAdvanceToNextMonth(), Is.True, _session.LastEventMessage);
            Assert.That(_session.PendingEventId, Is.EqualTo(M2FixedScenario.FinaleEventId));
        }

        private void Choose(string optionId)
        {
            string eventId = _session.PendingEventId;
            EventChoiceResult result = _session.TryChooseEventOption(eventId, optionId,
                _session.CreateEventResultId(eventId, optionId));
            Assert.That(result.Succeeded, Is.True, result.Message);
        }
    }
}
