using NUnit.Framework;
using UnityEngine;
using System.Reflection;
using UnityEngine.UI;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;
using SengokuChronicle.Presentation;
using Object = UnityEngine.Object;

namespace SengokuChronicle.Tests.EditMode
{
    public sealed class SupplyShortageMissionPanelTests
    {
        private GameObject _fixture;
        private GameObject _root;
        private GameSession _session;
        private SupplyShortageMissionPanel _panel;
        private Text _stage, _objective, _merit, _feedback;
        private Button _accept, _investigate, _negotiate, _report;

        [SetUp]
        public void SetUp()
        {
            _session = GameSession.NewGame();
            _fixture = new GameObject("MissionPanelTests");
            _panel = _fixture.AddComponent<SupplyShortageMissionPanel>();
            _root = Child("MissionRoot");
            _stage = Text("Stage"); _objective = Text("Objective");
            _merit = Text("Merit"); _feedback = Text("Feedback");
            _accept = Button("Accept"); _investigate = Button("Investigate");
            _negotiate = Button("Negotiate"); _report = Button("Report");
            _panel.BindReferences(_root, _stage, _objective, _merit, _feedback,
                _accept, _investigate, _negotiate, _report);
            _panel.Bind(_session);
            _panel.Open();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_fixture);
            Object.DestroyImmediate(_session.gameObject);
        }

        [Test]
        public void Panel_ShowsKiyosuAcceptBeforeAcceptance()
        {
            Assert.That(_stage.text, Does.Contain("未领取"));
            Assert.That(_objective.text, Does.Contain("清洲"));
            Assert.That(_merit.text, Does.Contain("0"));
            AssertAction(_accept, true, true);
            AssertAction(_investigate, false, false);
            AssertAction(_negotiate, false, false);
            AssertAction(_report, false, false);
            _accept.onClick.Invoke();
            Assert.That(_session.SupplyShortageStage, Is.EqualTo(SupplyShortageStage.Accepted));
        }

        [Test]
        public void Panel_ShowsAtsutaInvestigationThenNegotiation()
        {
            _panel.AcceptMission();
            AssertAction(_investigate, true, false);
            Travel(LocationId.Atsuta);
            _panel.Refresh();
            Assert.That(_objective.text, Does.Contain("热田调查"));
            AssertAction(_investigate, true, true);
            AssertAction(_negotiate, false, false);
            _investigate.onClick.Invoke();
            Assert.That(_stage.text, Does.Contain("已调查"));
            Assert.That(_objective.text, Does.Contain("黑田伊织"));
            AssertAction(_investigate, false, false);
            AssertAction(_negotiate, true, true);
            Assert.That(_merit.text, Does.Contain("0"));
        }

        [Test]
        public void Panel_ShowsKiyosuReportOnlyAfterNegotiation()
        {
            _panel.AcceptMission();
            AssertAction(_report, false, false);
            Travel(LocationId.Atsuta);
            _panel.Investigate();
            AssertAction(_report, false, false);
            _panel.Negotiate();
            Assert.That(_objective.text, Does.Contain("返回清洲报告"));
            AssertAction(_report, true, false);
            Travel(LocationId.Kiyosu);
            _panel.Refresh();
            AssertAction(_report, true, true);
            _report.onClick.Invoke();
            Assert.That(_merit.text, Does.Contain("10"));
            Assert.That(_objective.text, Does.Contain("完成"));
        }

        [Test]
        public void Panel_RefreshesAcrossTownAndReturn()
        {
            var mapObject = Child("Map");
            var map = mapObject.AddComponent<WorldMapController>();
            map.BindReferences(_session, null, null, null, null, null, null, null,
                null, null, null, null, null, null);
            map.BindSupplyShortageMissionPanel(_panel);
            Assert.That(_panel.IsOpen, Is.False);
            map.OpenTownMenu();
            Assert.That(_root.activeSelf, Is.True);
            _panel.AcceptMission();
            map.ReturnToMap();
            Assert.That(_panel.IsOpen, Is.False);
            Assert.That(_root.activeSelf, Is.False);
            map.SelectDestination(LocationId.Atsuta);
            map.ConfirmTravel();
            Assert.That(_panel.IsOpen, Is.True);
            Assert.That(_objective.text, Does.Contain("热田调查"));
            AssertAction(_investigate, true, true);
        }

        [Test]
        public void Panel_ShowsOperationFeedback()
        {
            var date = _session.WorldClock.CurrentDate;
            _panel.Investigate(); // Reject out-of-order input in GameSession.
            Assert.That(_feedback.text, Does.Contain("请先领取"));
            Assert.That(_session.SupplyShortageStage, Is.EqualTo(SupplyShortageStage.NotAccepted));
            _panel.AcceptMission();
            Assert.That(_feedback.text, Does.Contain("已领取"));
            _panel.Investigate(); // Reject correct stage at the wrong town.
            Assert.That(_feedback.text, Does.Contain("请在热田"));
            Assert.That(_session.SupplyShortageStage, Is.EqualTo(SupplyShortageStage.Accepted));
            Assert.That(_session.WorldClock.CurrentDate, Is.EqualTo(date));
            Assert.That(_session.Merit, Is.Zero);
        }

        [Test]
        public void Panel_DisablesRepeatedReport()
        {
            _panel.AcceptMission(); Travel(LocationId.Atsuta);
            _panel.Investigate(); _panel.Negotiate(); Travel(LocationId.Kiyosu);
            _panel.Report();
            AssertAction(_report, false, false);
            _panel.Report(); // Direct/stale input is still rejected by the application.
            Assert.That(_session.Merit, Is.EqualTo(10));
            Assert.That(_feedback.text, Does.Contain("奖励只能领取一次"));
        }

        [Test]
        public void Panel_HandlesMissingSessionOrReferences()
        {
            _panel.Bind(null);
            Assert.That(_objective.text, Does.Contain("缺少"));
            AssertAction(_accept, false, false);
            Assert.DoesNotThrow(() => { _panel.AcceptMission(); _panel.Investigate();
                _panel.Negotiate(); _panel.Report(); });
            _panel.BindReferences(null, null, null, null, null, null, null, null, null);
            _panel.Bind(_session);
            Assert.DoesNotThrow(() => { _panel.Open(); _panel.Refresh(); _panel.Close(); });
            _accept.onClick.Invoke(); // Rebinding removed listeners from the old reference.
            Assert.That(_session.SupplyShortageStage, Is.EqualTo(SupplyShortageStage.NotAccepted));
        }

        [Test]
        public void Panel_RemovesListenersWhileDisabledAndRestoresOnEnable()
        {
            // Direct calls avoid EditMode SendMessage guards; Play Mode dispatch is not tested.
            _panel.enabled = false;
            InvokeLifecycle("OnDisable");
            _accept.onClick.Invoke();
            Assert.That(_session.SupplyShortageStage, Is.EqualTo(SupplyShortageStage.NotAccepted));
            _panel.enabled = true;
            InvokeLifecycle("OnEnable");
            _accept.onClick.Invoke();
            Assert.That(_session.SupplyShortageStage, Is.EqualTo(SupplyShortageStage.Accepted));
            Assert.That(_feedback.text, Does.Contain("已领取补给短缺任务"));
            Travel(LocationId.Atsuta);
            InvokeLifecycle("OnDestroy");
            _investigate.onClick.Invoke();
            Assert.That(_session.SupplyShortageStage, Is.EqualTo(SupplyShortageStage.Accepted));
            Object.DestroyImmediate(_panel);
            _investigate.onClick.Invoke();
            Assert.That(_session.SupplyShortageStage, Is.EqualTo(SupplyShortageStage.Accepted));
        }

        private void InvokeLifecycle(string methodName)
        {
            MethodInfo method = typeof(SupplyShortageMissionPanel).GetMethod(
                methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, $"Missing lifecycle method: {methodName}");
            method.Invoke(_panel, null);
        }

        private void Travel(LocationId destination)
        {
            var preview = _session.PreviewTravel(destination);
            Assert.That(preview.HasValue, Is.True);
            Assert.That(_session.ConfirmTravel(preview.Value), Is.True);
        }

        private static void AssertAction(Button button, bool visible, bool interactable)
        {
            Assert.That(button.gameObject.activeSelf, Is.EqualTo(visible));
            Assert.That(button.interactable, Is.EqualTo(interactable));
        }

        private GameObject Child(string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(_fixture.transform, false);
            return go;
        }
        private Text Text(string name)
        {
            var go = Child(name);
            go.transform.SetParent(_root.transform, false);
            return go.AddComponent<Text>();
        }
        private Button Button(string name)
        {
            var go = Child(name);
            go.transform.SetParent(_root.transform, false);
            return go.AddComponent<Button>();
        }
    }
}
