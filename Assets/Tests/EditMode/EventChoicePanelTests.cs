using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;
using SengokuChronicle.Events;
using SengokuChronicle.Presentation;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SengokuChronicle.Tests.EditMode
{
    public sealed class EventChoicePanelTests
    {
        private GameSession _session;
        private GameObject _panelRoot;
        private GameObject _modalRoot;
        private Transform _optionsContent;
        private Text _feedback;
        private EventChoicePanel _panel;

        [SetUp]
        public void SetUp()
        {
            _session = GameSession.NewGame();
            _session.InitStandardDemo(1234UL);
        }

        [TearDown]
        public void TearDown()
        {
            if (_panelRoot != null)
                Object.DestroyImmediate(_panelRoot);
            if (_session != null)
                Object.DestroyImmediate(_session.gameObject);
        }

        [Test]
        public void MissingBindingsAreReportedWithoutThrowing()
        {
            _panelRoot = new GameObject("EventChoiceTestRoot");
            _panel = _panelRoot.AddComponent<EventChoicePanel>();

            Assert.DoesNotThrow(() => _panel.Refresh());
            Assert.That(_panel.IsBound, Is.False);
            Assert.That(_panel.TryChooseOption("event.missing", "option.missing"), Is.False);
        }

        [Test]
        public void PendingEventRendersLocalizedOptionsAndUpdatesAfterChoice()
        {
            CreatePanelFixture();
            Assert.That(_session.TryAdvanceToNextMonth(), Is.True, _session.LastEventMessage);

            Assert.That(_modalRoot.activeSelf, Is.True);
            Assert.That(_optionsContent.childCount, Is.EqualTo(2));
            Button first = Buttons().First();
            Assert.That(first.GetComponentInChildren<Text>(true).text, Does.Contain("请他进屋歇息"));
            first.onClick.Invoke();

            Assert.That(_session.PendingEventId, Is.Empty);
            Assert.That(_session.TryGetRelationship(CharacterId.KurodaIori).CurrentValue, Is.EqualTo(5));
            Assert.That(_modalRoot.activeSelf, Is.False);
            Assert.That(_feedback.text, Does.Contain("信任增加"));
        }

        [Test]
        public void LoadingPendingEventAutomaticallyOpensPanelAgain()
        {
            CreatePanelFixture();
            Assert.That(_session.TryAdvanceToNextMonth(), Is.True, _session.LastEventMessage);
            var pendingSave = _session.CaptureSave();

            _session.InitStandardDemo(1234UL);
            Assert.That(_modalRoot.activeSelf, Is.False);
            Assert.That(_session.TryApplySave(pendingSave).Succeeded, Is.True);

            Assert.That(_modalRoot.activeSelf, Is.True);
            Assert.That(_session.PendingEventId, Is.EqualTo(SampleEventIds.EventId));
            Assert.That(_optionsContent.childCount, Is.EqualTo(2));
        }

        [Test]
        public void StaleAndRepeatedCallbacksCannotCommitTwice()
        {
            CreatePanelFixture();
            Assert.That(_session.TryAdvanceToNextMonth(), Is.True, _session.LastEventMessage);
            string eventId = _session.PendingEventId;
            const string optionId = SampleEventIds.OfferRestOptionId;

            Assert.That(_panel.TryChooseOption(eventId, optionId), Is.True);
            Assert.That(_panel.TryChooseOption(eventId, optionId), Is.False);
            Assert.That(_session.TryChooseEventOption(eventId, optionId,
                _session.CreateEventResultId(eventId, optionId)).Succeeded, Is.False);
            Assert.That(_session.TryGetRelationship(CharacterId.KurodaIori).CurrentValue, Is.EqualTo(5));
            Assert.That(_session.CaptureSave().eventRuntime.history, Has.Length.EqualTo(1));
        }

        [Test]
        public void ListenerFromPreviousRenderCannotCommit()
        {
            CreatePanelFixture();
            Assert.That(_session.TryAdvanceToNextMonth(), Is.True, _session.LastEventMessage);
            UnityAction[] staleListeners = CaptureOwnedListeners();
            Assert.That(staleListeners, Has.Length.EqualTo(1),
                "only the currently available option registers a listener");

            // Re-render: the old buttons and their listeners belong to an earlier generation.
            _panel.Refresh();
            Assert.That(Buttons().Length, Is.EqualTo(2));

            foreach (UnityAction staleListener in staleListeners)
                staleListener.Invoke();

            Assert.That(_session.PendingEventId, Is.EqualTo(SampleEventIds.EventId),
                "a listener from an earlier render must not consume the pending event");
            Assert.That(_session.TryGetRelationship(CharacterId.KurodaIori).CurrentValue, Is.Zero);
            Assert.That(_session.CaptureSave().eventRuntime.history, Is.Empty);

            Buttons().Single(button =>
                    button.GetComponentInChildren<Text>(true).text.Contains("请他进屋歇息"))
                .onClick.Invoke();
            Assert.That(_session.PendingEventId, Is.Empty, "the fresh listener must still work");
            Assert.That(_session.TryGetRelationship(CharacterId.KurodaIori).CurrentValue, Is.EqualTo(5));
        }

        [Test]
        public void DisabledRequirementShowsReasonAndCannotBeClicked()
        {
            CreatePanelFixture();
            Assert.That(_session.TryAdvanceToNextMonth(), Is.True, _session.LastEventMessage);

            Button disabled = Buttons().Single(button =>
                button.GetComponentInChildren<Text>(true).text.Contains("请他明晨再来"));

            Assert.That(disabled.interactable, Is.False);
            Assert.That(disabled.GetComponentInChildren<Text>(true).text, Does.Contain("需要先取得伊织的信任"));
            disabled.onClick.Invoke();
            Assert.That(_session.PendingEventId, Is.EqualTo(SampleEventIds.EventId));
            Assert.That(_session.TryGetRelationship(CharacterId.KurodaIori).CurrentValue, Is.Zero);
        }

        [Test]
        public void ChoiceFeedbackStaysTopAnchoredAfterResolution()
        {
            CreatePanelFixture();
            // Start from the legacy serialized rect that FoundationScene still stores.
            RectTransform feedbackRect = _feedback.rectTransform;
            feedbackRect.anchorMin = feedbackRect.anchorMax = feedbackRect.pivot = new Vector2(0.5f, 0.5f);
            feedbackRect.anchoredPosition = new Vector2(0f, -322f);
            feedbackRect.sizeDelta = new Vector2(1180f, 42f);
            _feedback.alignment = TextAnchor.MiddleCenter;

            Assert.That(_session.TryAdvanceToNextMonth(), Is.True, _session.LastEventMessage);
            Buttons().Single(button =>
                    button.GetComponentInChildren<Text>(true).text.Contains("请他进屋歇息"))
                .onClick.Invoke();

            Assert.That(_feedback.text, Does.Contain("信任增加"));
            Assert.That(EventChoicePanel.IsFeedbackBannerLayout(feedbackRect), Is.True,
                "Refresh must move the result banner out of the map HUD band.");
            Assert.That(feedbackRect.anchorMin.y, Is.EqualTo(1f));
        }

        [Test]
        public void FeedbackBannerClampsInsideANarrowRoot()
        {
            CreatePanelFixture();
            var narrowRoot = new GameObject("NarrowEventChoiceRoot", typeof(RectTransform));
            try
            {
                narrowRoot.GetComponent<RectTransform>().sizeDelta = new Vector2(1100f, 720f);
                _feedback.transform.SetParent(narrowRoot.transform, false);

                EventChoicePanel.ApplyFeedbackBannerLayout(_feedback.rectTransform);

                Assert.That(_feedback.rectTransform.sizeDelta.x,
                    Is.EqualTo(1100f - 2f * EventChoicePanel.FeedbackSideMargin).Within(0.01f),
                    "the banner must not extend past a narrow canvas");
                Assert.That(EventChoicePanel.IsFeedbackBannerLayout(_feedback.rectTransform), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(narrowRoot);
            }
        }

        [Test]
        public void DisableTearsDownOwnedButtonsAndHidesModal()
        {
            CreatePanelFixture();
            Assert.That(_session.TryAdvanceToNextMonth(), Is.True, _session.LastEventMessage);
            _panel.Refresh();
            Assert.That(_optionsContent.childCount, Is.EqualTo(2));
            UnityAction[] tornDownListeners = CaptureOwnedListeners();
            Assert.That(tornDownListeners, Has.Length.EqualTo(1),
                "only the currently available option registers a listener");

            _panelRoot.SetActive(false);
            // Plain MonoBehaviours do not receive EditMode lifecycle dispatch, so invoke
            // the same callback Play Mode deactivation runs; teardown must be exercised.
            InvokePanelLifecycle("OnDisable");

            Assert.That(_optionsContent.childCount, Is.Zero);
            Assert.That(_modalRoot.activeSelf, Is.False);
            Assert.That(CaptureOwnedListeners(), Is.Empty,
                "teardown must release every owned listener");
            Assert.That(_session.PendingEventId, Is.EqualTo(SampleEventIds.EventId));

            foreach (UnityAction staleListener in tornDownListeners)
                staleListener.Invoke();
            Assert.That(_session.PendingEventId, Is.EqualTo(SampleEventIds.EventId),
                "a listener owned by the torn-down render must not commit after disable");
            Assert.That(_session.TryGetRelationship(CharacterId.KurodaIori).CurrentValue, Is.Zero);
        }

        private void CreatePanelFixture()
        {
            _panelRoot = new GameObject("EventChoiceTestRoot");
            _panel = _panelRoot.AddComponent<EventChoicePanel>();

            _modalRoot = new GameObject("EventChoiceModal", typeof(RectTransform), typeof(Image));
            _modalRoot.transform.SetParent(_panelRoot.transform, false);
            _modalRoot.SetActive(false);
            Text title = CreateText("Title", _modalRoot.transform);
            Text body = CreateText("Body", _modalRoot.transform);
            GameObject content = new GameObject("Options", typeof(RectTransform));
            content.transform.SetParent(_modalRoot.transform, false);
            _optionsContent = content.transform;
            Button prefab = CreateButton("OptionTemplate", _modalRoot.transform);
            prefab.gameObject.SetActive(false);
            _feedback = CreateText("Feedback", _panelRoot.transform);

            _panel.BindReferences(_session, _session.EventCatalog, _session.EventText,
                _modalRoot, title, body, _optionsContent, prefab, _feedback);
        }

        private Button[] Buttons() => _optionsContent.GetComponentsInChildren<Button>(true)
            .Where(button => button.gameObject != null && button.gameObject.name != "OptionTemplate")
            .ToArray();

        private UnityAction[] CaptureOwnedListeners()
        {
            FieldInfo ownedField = typeof(EventChoicePanel).GetField(
                "_ownedButtons", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(ownedField, Is.Not.Null, "EventChoicePanel._ownedButtons is missing");

            var listeners = new List<UnityAction>();
            var ownedButtons = (System.Collections.IEnumerable)ownedField.GetValue(_panel);
            foreach (object owned in ownedButtons)
            {
                FieldInfo listenerField = owned.GetType().GetField(
                    "Listener", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (listenerField != null && listenerField.GetValue(owned) is UnityAction listener)
                    listeners.Add(listener);
            }
            return listeners.ToArray();
        }

        private void InvokePanelLifecycle(string methodName)
        {
            MethodInfo method = typeof(EventChoicePanel).GetMethod(
                methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, "EventChoicePanel." + methodName + " is missing");
            method.Invoke(_panel, null);
        }

        private static Text CreateText(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            Text text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return text;
        }

        private static Button CreateButton(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            Button button = go.GetComponent<Button>();
            button.targetGraphic = go.GetComponent<Image>();
            CreateText("Label", go.transform);
            return button;
        }
    }
}
