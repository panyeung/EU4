using System.Linq;
using NUnit.Framework;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;
using SengokuChronicle.Events;
using SengokuChronicle.Persistence;
using UnityEngine;

namespace SengokuChronicle.Tests.EditMode
{
    /// <summary>
    /// SC-012 Task 3: the application layer appends chronicle records only at a
    /// successful commit boundary. Failed actions, clamped relationship no-ops,
    /// new games and loads must never append.
    /// </summary>
    public sealed class GameSessionChronicleTests
    {
        private GameSession _session;

        [SetUp]
        public void SetUp()
        {
            _session = GameSession.NewGame();
            _session.InitNewGame(1234UL);
        }

        [TearDown]
        public void TearDown()
        {
            if (_session != null)
                Object.DestroyImmediate(_session.gameObject);
        }

        private void TravelTo(LocationId destination)
        {
            TravelPreview? preview = _session.PreviewTravel(destination);
            Assert.That(preview.HasValue, Is.True, "missing travel route to " + destination.Value);
            Assert.That(_session.ConfirmTravel(preview.Value), Is.True);
        }

        private static int Count(HistoryLedger ledger, ChronicleCategory category) =>
            ledger.Entries.Count(entry => entry.Category == category);

        [Test]
        public void NewGameAndNewGameResetStartWithNoRecords()
        {
            Assert.That(_session.ChronicleEntries, Is.Empty);

            _session.TryChangeRelationship(CharacterId.KurodaIori, 5);
            Assert.That(_session.ChronicleEntries, Is.Not.Empty);

            _session.InitNewGame(1234UL);

            Assert.That(_session.ChronicleEntries, Is.Empty,
                "starting a new game must not carry over or recreate records");
        }

        [Test]
        public void CommittedEventRecordsTheEventThenItsRelationshipChanges()
        {
            Assert.That(_session.TryAdvanceToNextMonth(), Is.True, _session.LastEventMessage);
            string eventId = _session.PendingEventId;
            string resultId = _session.CreateEventResultId(eventId, SampleEventIds.OfferRestOptionId);

            EventChoiceResult result = _session.TryChooseEventOption(eventId,
                SampleEventIds.OfferRestOptionId, resultId);

            Assert.That(result.Succeeded, Is.True, result.Message);
            Assert.That(_session.ChronicleEntries, Has.Count.EqualTo(2));

            FamilyChronicleEntry eventEntry = _session.ChronicleEntries[0];
            Assert.That(eventEntry.Category, Is.EqualTo(ChronicleCategory.Event));
            Assert.That(eventEntry.SubjectId, Is.EqualTo(eventId));
            Assert.That(eventEntry.SourceId, Is.EqualTo(SampleEventIds.OfferRestOptionId));
            Assert.That(eventEntry.Sequence, Is.EqualTo(1));
            Assert.That(eventEntry.Year, Is.EqualTo(_session.WorldClock.CurrentDate.Year));
            Assert.That(eventEntry.Month, Is.EqualTo(_session.WorldClock.CurrentDate.Month));

            FamilyChronicleEntry relationshipEntry = _session.ChronicleEntries[1];
            Assert.That(relationshipEntry.Category, Is.EqualTo(ChronicleCategory.Relationship));
            Assert.That(relationshipEntry.SubjectId, Is.EqualTo(CharacterId.KurodaIori.Value));
            Assert.That(relationshipEntry.SourceId, Is.EqualTo("event"));
            Assert.That(relationshipEntry.FromValue, Is.Zero);
            Assert.That(relationshipEntry.ToValue, Is.EqualTo(5));
            Assert.That(relationshipEntry.Sequence, Is.EqualTo(2));
        }

        [Test]
        public void RejectedEventChoiceRecordsNothingAndLeavesRelationshipsUntouched()
        {
            Assert.That(_session.TryAdvanceToNextMonth(), Is.True, _session.LastEventMessage);
            string eventId = _session.PendingEventId;

            EventChoiceResult result = _session.TryChooseEventOption(eventId, "option.unknown",
                "result.unknown");

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.AppliedRelationships, Is.Empty);
            Assert.That(_session.ChronicleEntries, Is.Empty);
            Assert.That(_session.TryGetRelationship(CharacterId.KurodaIori).CurrentValue, Is.Zero);
            Assert.That(_session.PendingEventId, Is.EqualTo(eventId));
        }

        [Test]
        public void DuplicateCommittedResultIdCannotLeakRelationshipEffectsIntoTheLedger()
        {
            // The engine preflights effects, so a duplicate result ID is rejected before
            // any relationship mutates. The assertion still proves the application layer
            // receives no applied effects and therefore appends nothing.
            Assert.That(_session.EventCatalogReady, Is.True, _session.LastEventMessage);
            var engine = new EventEngine(_session.EventCatalog, _session.EventText);
            var relationships = new CharacterRelationships();
            Assert.That(EventRuntimeState.TryRestore(new EventRuntimeSaveData
            {
                phase = (int)StableGamePhase.AwaitingEventChoice,
                pendingEventIds = new[] { SampleEventIds.EventId },
                committedResultIds = new[] { "result.already-committed" }
            }, out EventRuntimeState state, out string restoreError), Is.True, restoreError);

            EventChoiceResult result = engine.TryChooseEventOption(SampleEventIds.EventId,
                SampleEventIds.OfferRestOptionId, "result.already-committed",
                _session.WorldClock.CurrentDate, relationships, state, default);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.AppliedRelationships, Is.Empty);
            Assert.That(relationships.TryGet(CharacterId.KurodaIori).CurrentValue, Is.Zero);
            Assert.That(_session.ChronicleEntries, Is.Empty);
        }

        [Test]
        public void DialogueRelationshipChangeRecordsPreviousAndCurrentValues()
        {
            DialogueStartResult started = _session.TryStartDialogue(CharacterId.KurodaIori);
            Assert.That(started.Succeeded, Is.True);

            DialogueSelectionResult result = _session.TrySelectDialogueOption(started.DialogueInstanceId,
                started.CurrentNodeId, BranchingDialogueCatalog.KurodaOfferOption);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(_session.ChronicleEntries, Has.Count.EqualTo(1));
            FamilyChronicleEntry entry = _session.ChronicleEntries[0];
            Assert.That(entry.Category, Is.EqualTo(ChronicleCategory.Relationship));
            Assert.That(entry.SourceId, Is.EqualTo("dialogue"));
            Assert.That(entry.FromValue, Is.Zero);
            Assert.That(entry.ToValue, Is.EqualTo(10));
        }

        [Test]
        public void DirectRelationshipChangeRecordsOnlyActualClampedChanges()
        {
            Assert.That(_session.TryChangeRelationship(CharacterId.KurodaIori, 100).Succeeded, Is.True);
            Assert.That(_session.ChronicleEntries, Has.Count.EqualTo(1));
            Assert.That(_session.ChronicleEntries[0].SourceId, Is.EqualTo("direct"));
            Assert.That(_session.ChronicleEntries[0].FromValue, Is.Zero);
            Assert.That(_session.ChronicleEntries[0].ToValue, Is.EqualTo(100));

            RelationshipResult clamped = _session.TryChangeRelationship(CharacterId.KurodaIori, 50);

            Assert.That(clamped.Succeeded, Is.True);
            Assert.That(clamped.CurrentValue, Is.EqualTo(100));
            Assert.That(_session.ChronicleEntries, Has.Count.EqualTo(1),
                "a clamped no-op must not create a record");

            Assert.That(_session.TryChangeRelationship(CharacterId.KurodaIori, -30).Succeeded, Is.True);
            Assert.That(_session.ChronicleEntries, Has.Count.EqualTo(2),
                "each real change must produce its own record");
            Assert.That(_session.ChronicleEntries[1].FromValue, Is.EqualTo(100));
            Assert.That(_session.ChronicleEntries[1].ToValue, Is.EqualTo(70));
        }

        [Test]
        public void MissionStagesAreRecordedOnceAndRejectedActionsAreSilent()
        {
            Assert.That(_session.TryInvestigateSupplyShortage().Succeeded, Is.False);
            Assert.That(_session.ChronicleEntries, Is.Empty);

            Assert.That(_session.TryAcceptSupplyShortageMission().Succeeded, Is.True);
            Assert.That(_session.TryAcceptSupplyShortageMission().Succeeded, Is.False);
            TravelTo(LocationId.Atsuta);
            Assert.That(_session.TryAcceptSupplyShortageMission().Succeeded, Is.False);
            Assert.That(_session.TryInvestigateSupplyShortage().Succeeded, Is.True);
            Assert.That(_session.TryInvestigateSupplyShortage().Succeeded, Is.False);
            Assert.That(_session.TryNegotiateSupplyShortage(CharacterId.KurodaIori).Succeeded, Is.True);
            TravelTo(LocationId.Kiyosu);
            Assert.That(_session.TryReportSupplyShortage().Succeeded, Is.True);

            Assert.That(_session.ChronicleEntries, Has.Count.EqualTo(4));
            Assert.That(_session.ChronicleEntries.All(entry =>
                entry.Category == ChronicleCategory.Mission), Is.True);
            Assert.That(_session.ChronicleEntries.Select(entry => entry.FromStage),
                Is.EqualTo(new[] { 0, 1, 2, 3 }));
            Assert.That(_session.ChronicleEntries.Select(entry => entry.ToStage),
                Is.EqualTo(new[] { 1, 2, 3, 4 }));
            Assert.That(_session.ChronicleEntries.Select(entry => entry.SubjectId).Distinct().Single(),
                Is.EqualTo("mission.supply-shortage"));
            Assert.That(_session.ChronicleEntries.Select(entry => entry.Sequence),
                Is.EqualTo(new[] { 1, 2, 3, 4 }));
        }

        [Test]
        public void SealedLetterMissionRecordsBothStageChanges()
        {
            Assert.That(_session.TryAcceptSealedLetterMission().Succeeded, Is.True);
            TravelTo(LocationId.Nagoya);
            Assert.That(_session.TryDeliverSealedLetter().Succeeded, Is.True);

            Assert.That(_session.ChronicleEntries, Has.Count.EqualTo(2));
            Assert.That(_session.ChronicleEntries.All(entry =>
                entry.Category == ChronicleCategory.Mission), Is.True);
            Assert.That(_session.ChronicleEntries.Select(entry => entry.SubjectId).Distinct().Single(),
                Is.EqualTo(FamilyChronicleEntry.SealedLetterMissionId));
            Assert.That(_session.ChronicleEntries.Select(entry => entry.FromStage),
                Is.EqualTo(new[] { 0, 1 }));
            Assert.That(_session.ChronicleEntries.Select(entry => entry.ToStage),
                Is.EqualTo(new[] { 1, 2 }));
        }

        [Test]
        public void MarketTallyMissionRecordsEveryStageChangeOnce()
        {
            TravelTo(LocationId.Nagoya);
            Assert.That(_session.TryAcceptMarketTallyMission().Succeeded, Is.True);
            TravelTo(LocationId.Kiyosu);
            Assert.That(_session.TryInvestigateMarketTally().Succeeded, Is.True);
            TravelTo(LocationId.Nagoya);
            Assert.That(_session.TryReportMarketTally().Succeeded, Is.True);
            Assert.That(_session.TryReportMarketTally().Succeeded, Is.False);

            Assert.That(_session.ChronicleEntries, Has.Count.EqualTo(3));
            Assert.That(_session.ChronicleEntries.All(entry =>
                entry.Category == ChronicleCategory.Mission), Is.True);
            Assert.That(_session.ChronicleEntries.Select(entry => entry.SubjectId).Distinct().Single(),
                Is.EqualTo(FamilyChronicleEntry.MarketTallyMissionId));
            Assert.That(_session.ChronicleEntries.Select(entry => entry.FromStage),
                Is.EqualTo(new[] { 0, 1, 2 }));
            Assert.That(_session.ChronicleEntries.Select(entry => entry.ToStage),
                Is.EqualTo(new[] { 1, 2, 3 }));
        }

        [Test]
        public void FailedSuccessionCommandRecordsNothing()
        {
            SuccessionResult failedConfirm = _session.ConfirmSuccession();
            Assert.That(failedConfirm.Succeeded, Is.False);
            Assert.That(_session.ChronicleEntries, Is.Empty);

            Assert.That(_session.BeginSuccessionRetirement().Succeeded, Is.True);
            int afterBegin = _session.ChronicleEntries.Count;
            SuccessionResult failedSelect = _session.SelectSuccessionSuccessor(
                new PersonId("person.unknown"));
            Assert.That(failedSelect.Succeeded, Is.False);
            Assert.That(_session.ChronicleEntries.Count, Is.EqualTo(afterBegin),
                "a rejected succession command must not append");
        }

        [Test]
        public void AdultSuccessionRecordsPhaseChangesButNotAPendingSelection()
        {
            Assert.That(_session.BeginSuccessionRetirement().Succeeded, Is.True);
            Assert.That(_session.ChronicleEntries, Has.Count.EqualTo(1));
            Assert.That(_session.ChronicleEntries[0].Category, Is.EqualTo(ChronicleCategory.Succession));
            Assert.That(_session.ChronicleEntries[0].FromPhase,
                Is.EqualTo((int)SuccessionPhase.None));
            Assert.That(_session.ChronicleEntries[0].ToPhase,
                Is.EqualTo((int)SuccessionPhase.AwaitingSuccessionChoice));

            Assert.That(_session.SelectSuccessionSuccessor(
                new PersonId("person.akechi.masatsuna")).Succeeded, Is.True);
            Assert.That(_session.ChronicleEntries, Has.Count.EqualTo(1),
                "selecting a candidate changes only the pending ID, not the recorded phase, head or result");

            Assert.That(_session.ConfirmSuccession().Succeeded, Is.True);
            Assert.That(_session.ChronicleEntries, Has.Count.EqualTo(2));
            FamilyChronicleEntry handoff = _session.ChronicleEntries[1];
            Assert.That(handoff.FromPhase, Is.EqualTo((int)SuccessionPhase.AwaitingSuccessionChoice));
            Assert.That(handoff.ToPhase, Is.EqualTo((int)SuccessionPhase.None));
            Assert.That(handoff.SubjectId, Is.EqualTo("person.akechi.masatsuna"));
            Assert.That(handoff.SourceId, Is.EqualTo(_session.Succession.CompletedResultId));
        }

        [Test]
        public void AdultDesignatedLivingHeirHandoffRecordsTheSamePhaseHeadChange()
        {
            PersonId heir = SuccessionSample.StandardDesignatedHeirId;
            Assert.That(_session.Succession.World.TrySetLife(heir,
                new SuccessionPersonLife(1540, true, false), out string setupError), Is.True, setupError);
            PersonId previousHead = _session.Succession.HeadPersonId;
            Assert.That(previousHead, Is.Not.EqualTo(heir));

            SuccessionResult result = _session.BeginSuccessionRetirement();

            Assert.That(result.Succeeded, Is.True, result.Message);
            Assert.That(_session.Succession.Phase, Is.EqualTo(SuccessionPhase.None),
                "an adult designated heir takes office without a pending phase");
            Assert.That(_session.Succession.HeadPersonId, Is.EqualTo(heir));
            Assert.That(_session.ChronicleEntries, Has.Count.EqualTo(1),
                "the real head/result change must still produce one chronicle row");
            FamilyChronicleEntry entry = _session.ChronicleEntries[0];
            Assert.That(entry.Category, Is.EqualTo(ChronicleCategory.Succession));
            Assert.That(entry.FromPhase, Is.EqualTo((int)SuccessionPhase.None));
            Assert.That(entry.ToPhase, Is.EqualTo((int)SuccessionPhase.None));
            Assert.That(entry.FromHeadId, Is.EqualTo(previousHead.Value));
            Assert.That(entry.ToHeadId, Is.EqualTo(heir.Value));
            Assert.That(entry.SubjectId, Is.EqualTo(heir.Value));
            Assert.That(entry.SourceId, Is.EqualTo(_session.Succession.CompletedResultId));
            Assert.That(entry.SourceId, Is.Not.Empty);
        }

        [Test]
        public void MinorRegencyRecordsSelectionRegencyAndCompletion()
        {
            Assert.That(_session.BeginSuccessionRetirement().Succeeded, Is.True);
            Assert.That(_session.SelectSuccessionSuccessor(
                new PersonId("person.akechi.tokihime")).Succeeded, Is.True);

            Assert.That(_session.ChronicleEntries, Has.Count.EqualTo(2));
            Assert.That(_session.ChronicleEntries[1].FromPhase,
                Is.EqualTo((int)SuccessionPhase.AwaitingSuccessionChoice));
            Assert.That(_session.ChronicleEntries[1].ToPhase,
                Is.EqualTo((int)SuccessionPhase.AwaitingRegentChoice));
            Assert.That(_session.ChronicleEntries[1].SubjectId,
                Is.EqualTo("person.akechi.tokihime"));
            Assert.That(_session.ChronicleEntries[1].Title, Is.EqualTo("明智 時姫"),
                "the title must name the recorded subject, not the still-in-office head");

            Assert.That(_session.SelectSuccessionRegent(
                new PersonId("person.akechi.chie")).Succeeded, Is.True);
            Assert.That(_session.ChronicleEntries, Has.Count.EqualTo(2),
                "selecting a regent does not change the recorded phase, head or result");

            Assert.That(_session.ConfirmSuccession().Succeeded, Is.True);
            Assert.That(_session.ChronicleEntries, Has.Count.EqualTo(3));
            Assert.That(_session.ChronicleEntries[2].ToPhase,
                Is.EqualTo((int)SuccessionPhase.Regency));

            Assert.That(_session.AdvanceDays(800), Is.True);
            Assert.That(_session.TryCompleteSuccessionRegency().Succeeded, Is.True);

            Assert.That(_session.ChronicleEntries, Has.Count.EqualTo(4));
            Assert.That(_session.ChronicleEntries[3].FromPhase,
                Is.EqualTo((int)SuccessionPhase.Regency));
            Assert.That(_session.ChronicleEntries[3].ToPhase,
                Is.EqualTo((int)SuccessionPhase.None));
        }

        [Test]
        public void ANewRetirementAfterAHandoffUsesItsOwnSourceAndSubjectName()
        {
            Assert.That(_session.BeginSuccessionRetirement().Succeeded, Is.True);
            Assert.That(_session.SelectSuccessionSuccessor(
                new PersonId("person.akechi.masatsuna")).Succeeded, Is.True);
            Assert.That(_session.ConfirmSuccession().Succeeded, Is.True);
            string previousResultId = _session.Succession.CompletedResultId;
            Assert.That(previousResultId, Is.Not.Empty);
            int recordsBefore = _session.ChronicleEntries.Count;

            Assert.That(_session.BeginSuccessionRetirement().Succeeded, Is.True);

            Assert.That(_session.ChronicleEntries.Count, Is.EqualTo(recordsBefore + 1));
            FamilyChronicleEntry retirement = _session.ChronicleEntries[recordsBefore];
            Assert.That(retirement.SourceId, Is.Not.EqualTo(previousResultId),
                "a new retirement must not inherit the previous handoff's result ID");
            Assert.That(retirement.SourceId, Is.EqualTo(SuccessionOutcome.Succeeded.ToString()));
            Assert.That(retirement.SubjectId, Is.EqualTo("person.akechi.masatsuna"));
            Assert.That(retirement.Title, Is.EqualTo("明智 正綱"));
            Assert.That(retirement.FromPhase, Is.EqualTo((int)SuccessionPhase.None));
            Assert.That(retirement.ToPhase,
                Is.EqualTo((int)SuccessionPhase.AwaitingSuccessionChoice));
        }

        [Test]
        public void CrisisAdoptionAndExtinctionAreRecordedButTheDemoResetIsNot()
        {
            Assert.That(_session.StartCrisisDemo(), Is.True);
            Assert.That(_session.ChronicleEntries, Is.Empty,
                "the fixed crisis demo reset is not a player succession event");

            Assert.That(_session.BeginSuccessionRetirement().Succeeded, Is.True);
            Assert.That(_session.ChronicleEntries, Has.Count.EqualTo(1));
            Assert.That(_session.ChronicleEntries[0].ToPhase,
                Is.EqualTo((int)SuccessionPhase.CrisisDecision));

            Assert.That(_session.DeclineCrisis().Succeeded, Is.True);
            Assert.That(_session.ChronicleEntries, Has.Count.EqualTo(2));
            Assert.That(_session.ChronicleEntries[1].FromPhase,
                Is.EqualTo((int)SuccessionPhase.CrisisDecision));
            Assert.That(_session.ChronicleEntries[1].ToPhase,
                Is.EqualTo((int)SuccessionPhase.Extinct));

            _session.InitNewGame(1234UL);
            Assert.That(_session.StartCrisisDemo(), Is.True);
            Assert.That(_session.BeginSuccessionRetirement().Succeeded, Is.True);
            Assert.That(_session.AcceptCrisisAdoption().Succeeded, Is.True);
            Assert.That(_session.ChronicleEntries, Has.Count.EqualTo(2));
            Assert.That(_session.ChronicleEntries[1].ToPhase, Is.EqualTo((int)SuccessionPhase.None));
            Assert.That(_session.ChronicleEntries[1].SubjectId, Is.EqualTo("person.adopted.family.akechi.1"));
        }

        [Test]
        public void RecordsKeepStableSameMonthOrderAcrossCategories()
        {
            Assert.That(_session.TryChangeRelationship(CharacterId.KurodaIori, 4).Succeeded, Is.True);
            Assert.That(_session.TryAcceptSupplyShortageMission().Succeeded, Is.True);
            Assert.That(_session.BeginSuccessionRetirement().Succeeded, Is.True);

            Assert.That(_session.ChronicleEntries.Select(entry => entry.Sequence),
                Is.EqualTo(new[] { 1, 2, 3 }));
            Assert.That(_session.ChronicleEntries.Select(entry => entry.Category),
                Is.EqualTo(new[]
                {
                    ChronicleCategory.Relationship,
                    ChronicleCategory.Mission,
                    ChronicleCategory.Succession
                }));
            Assert.That(_session.ChronicleEntries.All(entry =>
                entry.Year == 1560 && entry.Month == 4), Is.True);
        }

        [Test]
        public void SaveThenLoadRebuildsTheExactSameRecordOrder()
        {
            Assert.That(_session.TryChangeRelationship(CharacterId.KurodaIori, 4).Succeeded, Is.True);
            Assert.That(_session.TryAcceptSupplyShortageMission().Succeeded, Is.True);
            Assert.That(_session.BeginSuccessionRetirement().Succeeded, Is.True);
            SaveData saved = _session.CaptureSave();
            string[] originalIds = _session.ChronicleEntries.Select(entry => entry.EntryId).ToArray();

            GameSession rebuilt = GameSession.NewGame();
            try
            {
                Assert.That(rebuilt.TryApplySave(saved).Succeeded, Is.True);
                Assert.That(rebuilt.ChronicleEntries.Select(entry => entry.EntryId),
                    Is.EqualTo(originalIds));
                Assert.That(rebuilt.ChronicleEntries.Count, Is.EqualTo(originalIds.Length),
                    "loading must not append or duplicate records");
            }
            finally
            {
                Object.DestroyImmediate(rebuilt.gameObject);
            }
        }
    }
}
