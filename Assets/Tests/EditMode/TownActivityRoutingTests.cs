using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;
using SengokuChronicle.Presentation;
using Object = UnityEngine.Object;

namespace SengokuChronicle.Tests.EditMode
{
    public sealed class TownActivityRoutingTests
    {
        private readonly List<GameObject> _owned = new List<GameObject>();
        private GameSession _session;
        private WorldMapController _controller;
        private SupplyShortageMissionPanel _missionPanel;
        private ShadowEscortCommissionPanel _escortPanel;
        private TacticalMissionPanel _tacticalPanel;

        [SetUp]
        public void SetUp()
        {
            _session = GameSession.NewGame();
            _owned.Add(_session.gameObject);

            var controllerObject = NewObject("WorldMapController_TownActivityRouting_Test");
            _controller = controllerObject.AddComponent<WorldMapController>();
            var townRoot = NewObject("TownMenuRoot_TownActivityRouting_Test", typeof(RectTransform));
            townRoot.SetActive(false);
            _controller.BindReferences(_session, null, null, null, null, null, null, null,
                null, null, null, townRoot, null, null);
            _controller.BindTownOverviewPanel(NewObject("TownOverview_TownActivityRouting_Test")
                .AddComponent<TownOverviewPanel>());

            _missionPanel = NewObject("MissionPanel_TownActivityRouting_Test")
                .AddComponent<SupplyShortageMissionPanel>();
            _controller.BindSupplyShortageMissionPanel(_missionPanel);

            _escortPanel = NewObject("EscortPanel_TownActivityRouting_Test")
                .AddComponent<ShadowEscortCommissionPanel>();
            _controller.BindShadowEscortCommissionPanel(_escortPanel);

            _tacticalPanel = NewObject("TacticalPanel_TownActivityRouting_Test")
                .AddComponent<TacticalMissionPanel>();
            _controller.BindTacticalMissionPanel(_tacticalPanel);
            _controller.OpenTownMenu();
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = _owned.Count - 1; i >= 0; i--)
                if (_owned[i] != null) Object.DestroyImmediate(_owned[i]);
            _owned.Clear();
        }

        [Test]
        public void OpenTownActivity_SelectsAndKeepsTheMatchingMissionPageOpen()
        {
            _controller.OpenTownActivity(TownLocalActions.Kind.SupplyShortage);
            Assert.That(_missionPanel.IsOpen, Is.True);
            Assert.That(_missionPanel.IsM4bPage, Is.False);

            _controller.OpenTownActivity(TownLocalActions.Kind.SealedLetter);
            Assert.That(_missionPanel.IsOpen, Is.True);
            Assert.That(_missionPanel.IsM4bPage, Is.True);

            _controller.OpenTownActivity(TownLocalActions.Kind.SealedLetter);
            Assert.That(_missionPanel.IsOpen, Is.True, "Opening an already selected activity must not close its panel.");
            Assert.That(_missionPanel.IsM4bPage, Is.True);

            _controller.OpenTownActivity(TownLocalActions.Kind.SupplyShortage);
            Assert.That(_missionPanel.IsOpen, Is.True);
            Assert.That(_missionPanel.IsM4bPage, Is.False);
        }

        [Test]
        public void OpenTownActivity_UsesTheExistingEscortAndTacticalPanels()
        {
            _controller.OpenTownActivity(TownLocalActions.Kind.EscortCommission);
            Assert.That(_escortPanel.IsOpen, Is.True);
            Assert.That(_missionPanel.IsOpen, Is.False);
            Assert.That(_tacticalPanel.IsOpen, Is.False);

            _escortPanel.Close();
            _controller.OpenTownActivity(TownLocalActions.Kind.TacticalMission);
            Assert.That(_tacticalPanel.IsOpen, Is.True);
            Assert.That(_missionPanel.IsOpen, Is.False);
        }

        [Test]
        public void OpenTownActivity_MarketTallySelectsTheOtherMissionsPage()
        {
            TravelTo(LocationId.Nagoya);

            _controller.OpenTownActivity(TownLocalActions.Kind.MarketTally);

            Assert.That(_missionPanel.IsOpen, Is.True);
            Assert.That(_missionPanel.IsM4bPage, Is.True);
            Assert.That(_session.CurrentLocation, Is.EqualTo(LocationId.Nagoya));
        }

        [Test]
        public void OpenTownActivity_WhenActivityIsUnavailableAtCurrentLocationDoesNothing()
        {
            TravelTo(LocationId.Atsuta);
            _controller.OpenTownActivity(TownLocalActions.Kind.SupplyShortage);

            Assert.That(_missionPanel.IsOpen, Is.False);
            Assert.That(_escortPanel.IsOpen, Is.False);
            Assert.That(_tacticalPanel.IsOpen, Is.False);
            Assert.That(_session.CurrentLocation, Is.EqualTo(LocationId.Atsuta));
            Assert.That(_session.WorldClock.CurrentDate, Is.EqualTo(new GameDate(1560, 4, 2)));
        }

        [Test]
        public void OpenTownActivity_WhenTaskStageChangesAfterListingDoesNothing()
        {
            Assert.That(TownLocalActions.GetAvailable(_session)
                .Any(entry => entry.Kind == TownLocalActions.Kind.SupplyShortage), Is.True);
            Assert.That(_session.TryAcceptSupplyShortageMission().Succeeded, Is.True);

            _controller.OpenTownActivity(TownLocalActions.Kind.SupplyShortage);

            Assert.That(_session.SupplyShortageStage, Is.EqualTo(SupplyShortageStage.Accepted));
            Assert.That(_missionPanel.IsOpen, Is.False);
            Assert.That(_escortPanel.IsOpen, Is.False);
            Assert.That(_tacticalPanel.IsOpen, Is.False);
        }

        [Test]
        public void OpenTownActivity_DoesNotAdvanceTimeOrChangeResourcesOrTaskStages()
        {
            string saveBefore = JsonUtility.ToJson(_session.CaptureSave());
            GameDate date = _session.WorldClock.CurrentDate;
            IndustryResources resources = _session.Industry.Resources;
            SupplyShortageStage supply = _session.SupplyShortageStage;
            SealedLetterStage letter = _session.SealedLetterStage;
            MarketTallyStage market = _session.MarketTallyStage;
            ShadowEscortStage escort = _session.CommissionStage;

            _controller.OpenTownActivity(TownLocalActions.Kind.SupplyShortage);
            _controller.OpenTownActivity(TownLocalActions.Kind.SealedLetter);
            _controller.OpenTownActivity(TownLocalActions.Kind.EscortCommission);
            _controller.OpenTownActivity(TownLocalActions.Kind.TacticalMission);

            Assert.That(_session.WorldClock.CurrentDate, Is.EqualTo(date));
            Assert.That(_session.CurrentLocation, Is.EqualTo(LocationId.Kiyosu));
            Assert.That(_session.Industry.Resources, Is.EqualTo(resources));
            Assert.That(_session.SupplyShortageStage, Is.EqualTo(supply));
            Assert.That(_session.SealedLetterStage, Is.EqualTo(letter));
            Assert.That(_session.MarketTallyStage, Is.EqualTo(market));
            Assert.That(_session.CommissionStage, Is.EqualTo(escort));
            Assert.That(JsonUtility.ToJson(_session.CaptureSave()), Is.EqualTo(saveBefore));
        }

        [Test]
        public void TownMenu_FacilitySlotsStayEmptyWhenTheTownPageRenders()
        {
            var slots = new[]
            {
                NewObject("FacilitySlot1_TownActivityRouting_Test").AddComponent<UnityEngine.UI.Text>(),
                NewObject("FacilitySlot2_TownActivityRouting_Test").AddComponent<UnityEngine.UI.Text>(),
                NewObject("FacilitySlot3_TownActivityRouting_Test").AddComponent<UnityEngine.UI.Text>(),
            };
            _controller.BindReferences(_session, null, null, null, null, null, null, null,
                null, null, null, _controller.GetTownMenuRoot(), null, slots);
            _controller.OpenTownMenu();

            foreach (UnityEngine.UI.Text slot in slots)
            {
                Assert.That(slot.text, Is.Empty,
                    "总览页存在时设施槽不得再写占位，也不得重复列总览页已承担的条目。");
                Assert.That(slot.enabled, Is.False);
            }
        }

        private GameObject NewObject(string name, params System.Type[] components)
        {
            var value = new GameObject(name, components);
            _owned.Add(value);
            return value;
        }

        private void TravelTo(LocationId destination)
        {
            TravelPreview? preview = _session.PreviewTravel(destination);
            Assert.That(preview.HasValue, Is.True);
            Assert.That(_session.ConfirmTravel(preview.Value), Is.True);
        }
    }
}
