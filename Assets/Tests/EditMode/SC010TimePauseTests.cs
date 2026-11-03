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
    /// <summary>
    /// SC-010 regression: when succession pauses time, the clock panel must report the
    /// succession reason it received instead of blaming the supported-date limit. The
    /// clock panel itself has no message label, so this asserts the feedback the panel
    /// publishes for diagnostics and for any scene where its advance control is reachable.
    /// </summary>
    public sealed class SC010TimePauseTests
    {
        private GameSession _session;
        private GameObject _panelObject;
        private WorldClockPanel _panel;

        [SetUp]
        public void SetUp()
        {
            _session = GameSession.NewGame();
            _session.InitStandardDemo();
            _panelObject = new GameObject("WorldClockPanel_SC010Test");
            _panel = _panelObject.AddComponent<WorldClockPanel>();
            _panel.SetDependencies(_session, null, null);
        }

        [TearDown]
        public void TearDown()
        {
            if (_panelObject != null)
                Object.DestroyImmediate(_panelObject);
            if (_session != null)
                Object.DestroyImmediate(_session.gameObject);
        }

        [Test]
        public void TryAdvanceOneDay_WhileSuccessionChoicePending_ReportsSuccessionReason()
        {
            GameDate date = _session.WorldClock.CurrentDate;
            _session.BeginSuccessionRetirement();

            Assert.That(_panel.TryAdvanceOneDay(), Is.False);
            Assert.That(_session.WorldClock.CurrentDate, Is.EqualTo(date));
            Assert.That(_panel.LastAdvanceMessage, Does.Contain("继承"));
            Assert.That(_panel.LastAdvanceMessage, Does.Not.Contain("上限"),
                "A succession pause must not be reported as the supported-date limit.");
        }

        [Test]
        public void TryAdvanceOneDay_WhileAdultHandoffAwaitsConfirmation_ReportsHandoffAction()
        {
            _session.BeginSuccessionRetirement();
            Assert.That(_session.SelectSuccessionSuccessor(
                new PersonId("person.akechi.tokihime")).Succeeded, Is.True);
            Assert.That(_session.SelectSuccessionRegent(
                new PersonId("person.akechi.chie")).Succeeded, Is.True);
            Assert.That(_session.ConfirmSuccession().Succeeded, Is.True);
            Assert.That(_session.AdvanceDays(800), Is.True);
            GameDate pausedDate = _session.WorldClock.CurrentDate;

            Assert.That(_panel.TryAdvanceOneDay(), Is.False);
            Assert.That(_session.WorldClock.CurrentDate, Is.EqualTo(pausedDate));
            Assert.That(_panel.LastAdvanceMessage, Does.Contain("完成成年交权"),
                "The pause message must name the explicit confirmation the UI requires.");
            Assert.That(_panel.LastAdvanceMessage, Does.Not.Contain("上限"));
        }

        [Test]
        public void TryAdvanceOneDay_WhenNotBlocked_AdvancesAndClearsMessage()
        {
            GameDate date = _session.WorldClock.CurrentDate;

            Assert.That(_panel.TryAdvanceOneDay(), Is.True);
            Assert.That(_session.WorldClock.CurrentDate, Is.Not.EqualTo(date));
            Assert.That(_panel.LastAdvanceMessage, Is.Empty);
        }

        [Test]
        public void ExtinctFamily_DisablesClockControlsAndReenablesThemAfterCheckpointLoad()
        {
            Button advance = CreateButton("Advance_SC010Extinct");
            Button endMonth = CreateButton("EndMonth_SC010Extinct");
            Text dateText = CreateText("Date_SC010Extinct");
            try
            {
                _panel.SetDependencies(_session, advance, dateText);
                _panel.BindMonthTurn(endMonth, null);
                _panel.Refresh();
                Assert.That(advance.interactable, Is.True);
                Assert.That(endMonth.interactable, Is.True);

                _session.StartCrisisDemo();
                _session.BeginSuccessionRetirement();
                SaveData checkpoint = _session.CaptureSave();
                _session.DeclineCrisis();

                Assert.That(_session.IsFamilyExtinct, Is.True);
                Assert.That(advance.interactable, Is.False,
                    "SessionStateChanged must disable the day control as soon as the family is extinct.");
                Assert.That(endMonth.interactable, Is.False,
                    "SessionStateChanged must disable the month control as soon as the family is extinct.");

                Assert.That(_session.TryApplySave(checkpoint).Succeeded, Is.True);
                Assert.That(_session.IsFamilyExtinct, Is.False);
                Assert.That(advance.interactable, Is.True,
                    "Loading an earlier checkpoint must restore the clock controls.");
                Assert.That(endMonth.interactable, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(advance.gameObject);
                Object.DestroyImmediate(endMonth.gameObject);
                Object.DestroyImmediate(dateText.gameObject);
            }
        }

        [Test]
        public void ExtinctFamily_DisablesMapTravelAndLocationControls()
        {
            var mapObject = new GameObject("WorldMap_SC010ExtinctTest");
            Button confirm = CreateButton("Confirm_SC010Extinct");
            Button cancel = CreateButton("Cancel_SC010Extinct");
            Button back = CreateButton("Back_SC010Extinct");
            Button kiyosu = CreateButton("Kiyosu_SC010Extinct");
            Button atsuta = CreateButton("Atsuta_SC010Extinct");
            Button nagoya = CreateButton("Nagoya_SC010Extinct");
            Text marker = CreateText("Marker_SC010Extinct");
            Text feedback = CreateText("Feedback_SC010Extinct");
            Text currentDate = CreateText("Date_SC010ExtinctMap");
            Text destination = CreateText("Destination_SC010Extinct");
            Text travelTime = CreateText("TravelTime_SC010Extinct");
            Text arrival = CreateText("Arrival_SC010Extinct");
            Text kiyosuLabel = CreateText("KiyosuLabel_SC010Extinct");
            Text atsutaLabel = CreateText("AtsutaLabel_SC010Extinct");
            Text nagoyaLabel = CreateText("NagoyaLabel_SC010Extinct");
            try
            {
                WorldMapController map = mapObject.AddComponent<WorldMapController>();
                map.BindReferences(_session, null, marker, feedback, currentDate, destination,
                    travelTime, arrival, confirm, cancel, back, null, null, null);
                map.BindLocationButtons(kiyosu, kiyosuLabel, atsuta, atsutaLabel, nagoya, nagoyaLabel);
                map.RefreshAll();

                map.SelectDestination(LocationId.Atsuta);
                Assert.That(confirm.interactable, Is.True);
                Assert.That(atsuta.interactable, Is.True);

                GameDate date = _session.WorldClock.CurrentDate;
                LocationId location = _session.CurrentLocation;
                _session.StartCrisisDemo();
                _session.BeginSuccessionRetirement();
                SaveData checkpoint = _session.CaptureSave();
                _session.DeclineCrisis();

                Assert.That(_session.IsFamilyExtinct, Is.True);
                Assert.That(confirm.interactable, Is.False,
                    "SessionStateChanged must disable travel confirmation.");
                Assert.That(cancel.interactable, Is.False);
                Assert.That(kiyosu.interactable, Is.False);
                Assert.That(atsuta.interactable, Is.False);
                Assert.That(nagoya.interactable, Is.False);
                Assert.That(map.CurrentPreview, Is.Null,
                    "A terminal family must not keep a stale travel preview.");
                Assert.That(_session.WorldClock.CurrentDate, Is.EqualTo(date));
                Assert.That(_session.CurrentLocation, Is.EqualTo(location));

                Assert.That(_session.TryApplySave(checkpoint).Succeeded, Is.True);
                Assert.That(atsuta.interactable, Is.True,
                    "Loading an earlier checkpoint must restore map travel selection.");
                Assert.That(nagoya.interactable, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(mapObject);
                Object.DestroyImmediate(confirm.gameObject);
                Object.DestroyImmediate(cancel.gameObject);
                Object.DestroyImmediate(back.gameObject);
                Object.DestroyImmediate(kiyosu.gameObject);
                Object.DestroyImmediate(atsuta.gameObject);
                Object.DestroyImmediate(nagoya.gameObject);
                Object.DestroyImmediate(marker.gameObject);
                Object.DestroyImmediate(feedback.gameObject);
                Object.DestroyImmediate(currentDate.gameObject);
                Object.DestroyImmediate(destination.gameObject);
                Object.DestroyImmediate(travelTime.gameObject);
                Object.DestroyImmediate(arrival.gameObject);
                Object.DestroyImmediate(kiyosuLabel.gameObject);
                Object.DestroyImmediate(atsutaLabel.gameObject);
                Object.DestroyImmediate(nagoyaLabel.gameObject);
            }
        }

        private static Text CreateText(string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.localScale = Vector3.one;
            return go.AddComponent<Text>();
        }

        private static Button CreateButton(string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            go.AddComponent<Image>();
            return go.AddComponent<Button>();
        }
    }
}
