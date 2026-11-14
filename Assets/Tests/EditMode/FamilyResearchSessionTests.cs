using System;
using System.Linq;
using NUnit.Framework;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;
using SengokuChronicle.Events;
using SengokuChronicle.Persistence;
using UnityEngine;

namespace SengokuChronicle.Tests.EditMode
{
    /// <summary>SC-021 Task 2: plan commands and the atomic monthly research transaction.</summary>
    public sealed class FamilyResearchSessionTests
    {
        private GameSession _session;

        [SetUp]
        public void SetUp()
        {
            _session = GameSession.NewGame();
            _session.InitResearchDemoScenario();
        }

        [TearDown]
        public void TearDown()
        {
            if (_session != null)
                UnityEngine.Object.DestroyImmediate(_session.gameObject);
        }

        [Test]
        public void Plan_AcceptsLivingAdultFamilyMembers()
        {
            FamilyResearchPlanCommandResult result = _session.TrySetResearchPlan(
                FamilyResearchCatalog.SmokeMixingResearchId, SC021FamilyResearchScenario.HeadId);

            Assert.That(result.Succeeded, Is.True, result.Message);
            Assert.That(_session.ResearchState.HasPlan, Is.True);
            Assert.That(_session.ResearchState.ResearcherId,
                Is.EqualTo(SC021FamilyResearchScenario.HeadId));
            Assert.That(_session.Research.ProgressLine, Does.Contain("0/2"));
            Assert.That(_session.Research.EligibleResearchers.Count, Is.EqualTo(3));
        }

        [Test]
        public void Plan_RejectsDeadRetiredMinorNonMemberAndIndustryAssignee()
        {
            _session.Succession.World.TrySetLife(SC021FamilyResearchScenario.HeadId,
                new SuccessionPersonLife(SC021FamilyResearchScenario.HeadBirthYear, false, false), out _);
            Assert.That(_session.TrySetResearchPlan(FamilyResearchCatalog.SmokeMixingResearchId,
                SC021FamilyResearchScenario.HeadId).Outcome,
                Is.EqualTo(FamilyResearchPlanOutcome.InvalidResearcher));

            _session = Fresh();
            _session.Succession.World.TrySetLife(SC021FamilyResearchScenario.SpouseId,
                new SuccessionPersonLife(SC021FamilyResearchScenario.SpouseBirthYear, true, true), out _);
            Assert.That(_session.TrySetResearchPlan(FamilyResearchCatalog.SmokeMixingResearchId,
                SC021FamilyResearchScenario.SpouseId).Outcome,
                Is.EqualTo(FamilyResearchPlanOutcome.InvalidResearcher));

            _session = Fresh();
            _session.Succession.World.TrySetLife(SC021FamilyResearchScenario.HeirId,
                new SuccessionPersonLife(1555, true, false), out _);
            Assert.That(_session.TrySetResearchPlan(FamilyResearchCatalog.SmokeMixingResearchId,
                SC021FamilyResearchScenario.HeirId).Outcome,
                Is.EqualTo(FamilyResearchPlanOutcome.InvalidResearcher));

            _session = Fresh();
            Assert.That(_session.TrySetResearchPlan(FamilyResearchCatalog.SmokeMixingResearchId,
                new PersonId("person.not-in-family")).Outcome,
                Is.EqualTo(FamilyResearchPlanOutcome.InvalidResearcher));

            _session = Fresh();
            Assert.That(_session.TryAssignIndustry(IndustryCatalog.FarmId,
                SC021FamilyResearchScenario.HeadId).Succeeded, Is.True);
            Assert.That(_session.TrySetResearchPlan(FamilyResearchCatalog.SmokeMixingResearchId,
                SC021FamilyResearchScenario.HeadId).Outcome,
                Is.EqualTo(FamilyResearchPlanOutcome.InvalidResearcher));
        }

        [Test]
        public void Plan_ReplacingResearcherPreservesProgressAndCompletes()
        {
            Assert.That(_session.TrySetResearchPlan(FamilyResearchCatalog.SmokeMixingResearchId,
                SC021FamilyResearchScenario.HeadId).Succeeded, Is.True);
            Assert.That(_session.TryAdvanceToNextMonth(), Is.True, _session.LastEventMessage);
            ResolvePending(_session);
            Assert.That(_session.ResearchState.Progress, Is.EqualTo(1));

            Assert.That(_session.TrySetResearchPlan(FamilyResearchCatalog.SmokeMixingResearchId,
                SC021FamilyResearchScenario.SpouseId).Succeeded, Is.True);
            Assert.That(_session.ResearchState.Progress, Is.EqualTo(1));
            Assert.That(_session.ResearchState.ResearcherId,
                Is.EqualTo(SC021FamilyResearchScenario.SpouseId));

            Assert.That(_session.TryAdvanceToNextMonth(), Is.True, _session.LastEventMessage);
            ResolvePending(_session);
            Assert.That(_session.ResearchState.HasPlan, Is.False);
            Assert.That(_session.ResearchState.IsKnownTechnique(
                FamilyResearchCatalog.SmokeMixingTechniqueId), Is.True);
        }

        [Test]
        public void SuccessfulMonth_SpendsOneFundAndSupplyAndAddsOneProgress()
        {
            Assert.That(_session.TrySetResearchPlan(FamilyResearchCatalog.SmokeMixingResearchId,
                SC021FamilyResearchScenario.HeadId).Succeeded, Is.True);

            Assert.That(_session.TryAdvanceToNextMonth(), Is.True, _session.LastEventMessage);

            Assert.That(_session.WorldClock.CurrentDate, Is.EqualTo(new GameDate(1560, 2, 1)));
            Assert.That(_session.Industry.Resources.Funds, Is.EqualTo(2),
                "the opening budget pays the household food bill before the research month");
            Assert.That(_session.Industry.Resources.Supplies, Is.EqualTo(1));
            Assert.That(_session.ResearchState.Progress, Is.EqualTo(1));
            Assert.That(_session.LastResearchReport, Is.Not.Null);
            Assert.That(_session.LastResearchReport.Completed, Is.False);
        }

        [Test]
        public void Completion_WritesOneReadableChronicleAndNoPersonalMastery()
        {
            Assert.That(_session.TrySetResearchPlan(FamilyResearchCatalog.SmokeMixingResearchId,
                SC021FamilyResearchScenario.HeadId).Succeeded, Is.True);
            Assert.That(_session.TryAdvanceToNextMonth(), Is.True, _session.LastEventMessage);
            ResolvePending(_session);
            Assert.That(_session.TryAdvanceToNextMonth(), Is.True, _session.LastEventMessage);
            ResolvePending(_session);

            Assert.That(_session.ResearchState.KnownTechniqueIds, Has.Count.EqualTo(1));
            Assert.That(_session.ResearchState.KnownTechniqueIds[0],
                Is.EqualTo(FamilyResearchCatalog.SmokeMixingTechniqueId));
            Assert.That(_session.Industry.Resources.Funds, Is.EqualTo(1),
                "the second month has no stocked food to buy, so its food money stays unspent");
            Assert.That(_session.Industry.Resources.Supplies, Is.Zero);

            FamilyChronicleEntry[] researchEntries = _session.ChronicleEntries
                .Where(entry => entry.Category == ChronicleCategory.Research).ToArray();
            Assert.That(researchEntries, Has.Length.EqualTo(1));
            Assert.That(researchEntries[0].SubjectId,
                Is.EqualTo(FamilyResearchCatalog.SmokeMixingResearchId));
            Assert.That(researchEntries[0].SourceId,
                Is.EqualTo(FamilyResearchCatalog.SmokeMixingTechniqueId));
            Assert.That(researchEntries[0].Title, Is.EqualTo("家族研究"));
            Assert.That(researchEntries[0].Summary, Does.Contain("烟幕调制术"));
            Assert.That(researchEntries[0].Summary, Does.Contain("影山 宗次"));
            Assert.That(researchEntries[0].Summary, Does.Contain("1 → 2"));
            Assert.That(researchEntries[0].Summary, Does.Not.Contain("research."));
            Assert.That(researchEntries[0].Summary, Does.Not.Contain("technique."));
            Assert.That(researchEntries[0].Summary, Does.Not.Contain("person."));

            foreach (PersonState person in _session.Succession.World.Persons)
            {
                Assert.That(_session.Development.TryGetValues(person.Id,
                    out CharacterGrowthValues values), Is.True);
                Assert.That(values.Ninjutsu, Is.Zero);
                Assert.That(values.MartialArts, Is.Zero);
                Assert.That(values.Scholarship, Is.Zero);
                Assert.That(values.Management, Is.Zero);
                Assert.That(values.Social, Is.Zero);
            }
        }

        [Test]
        public void Shortage_PausesWithoutSpendingOrProgress()
        {
            Assert.That(_session.TrySetResearchPlan(FamilyResearchCatalog.SmokeMixingResearchId,
                SC021FamilyResearchScenario.HeadId).Succeeded, Is.True);
            SaveData data = _session.CaptureSave();
            data.industry.funds = 0;
            Assert.That(_session.TryApplySave(data).Succeeded, Is.True);

            Assert.That(_session.TryAdvanceToNextMonth(), Is.True, _session.LastEventMessage);
            ResolvePending(_session);

            Assert.That(_session.WorldClock.CurrentDate, Is.EqualTo(new GameDate(1560, 2, 1)));
            Assert.That(_session.Industry.Resources.Funds, Is.Zero);
            Assert.That(_session.Industry.Resources.Supplies, Is.EqualTo(2));
            Assert.That(_session.ResearchState.Progress, Is.Zero);
            Assert.That(_session.ResearchState.KnownTechniqueIds, Is.Empty);
            Assert.That(_session.LastResearchReport.Paused, Is.True);
            Assert.That(_session.LastResearchReport.Summary, Does.Contain("资金不足"));
        }

        [Test]
        public void PendingEvent_BlocksAnotherMonthAndPlanChanges()
        {
            Assert.That(_session.TrySetResearchPlan(FamilyResearchCatalog.SmokeMixingResearchId,
                SC021FamilyResearchScenario.HeadId).Succeeded, Is.True);
            Assert.That(_session.TryAdvanceToNextMonth(), Is.True, _session.LastEventMessage);
            Assert.That(_session.PendingEventId, Is.Not.Empty);

            GameDate date = _session.WorldClock.CurrentDate;
            int progress = _session.ResearchState.Progress;

            Assert.That(_session.TryAdvanceToNextMonth(), Is.False);
            Assert.That(_session.LastMonthTurnOutcome, Is.EqualTo(MonthTurnOutcome.PendingEvent));
            Assert.That(_session.WorldClock.CurrentDate, Is.EqualTo(date));
            Assert.That(_session.ResearchState.Progress, Is.EqualTo(progress));

            Assert.That(_session.TrySetResearchPlan(FamilyResearchCatalog.SmokeMixingResearchId,
                SC021FamilyResearchScenario.SpouseId).Outcome,
                Is.EqualTo(FamilyResearchPlanOutcome.Blocked));
        }

        [Test]
        public void FaultAfterResearchPlanning_RollsBackEverythingAndRetrySucceeds()
        {
            Assert.That(_session.TrySetResearchPlan(FamilyResearchCatalog.SmokeMixingResearchId,
                SC021FamilyResearchScenario.HeadId).Succeeded, Is.True);

            GameDate date = _session.WorldClock.CurrentDate;
            IndustryResources resources = _session.Industry.Resources;
            int progress = _session.ResearchState.Progress;
            int chronicle = _session.ChronicleEntries.Count;
            RandomStateSaveData rngBefore = _session.RandomState.CaptureState();
            string rng = JsonUtility.ToJson(rngBefore);
            string eventStreamBefore = EventStreamSnapshot(rngBefore);
            string runtime = JsonUtility.ToJson(_session.EventRuntimeStateForSave);
            string pending = _session.PendingEventId;
            string eventStreamAtFault = null;

            _session.MonthSettlementFaultInjection = () =>
            {
                eventStreamAtFault = EventStreamSnapshot(_session.RandomState.CaptureState());
                throw new InvalidOperationException("boom");
            };
            Assert.That(_session.TryAdvanceToNextMonth(), Is.False);
            Assert.That(_session.LastMonthTurnOutcome, Is.EqualTo(MonthTurnOutcome.SettlementFailed));

            Assert.That(eventStreamAtFault, Is.Not.Null,
                "the injected fault must run inside the settlement commit phase");
            Assert.That(eventStreamAtFault, Is.Not.EqualTo(eventStreamBefore),
                "event scheduling must consume the event random stream before the fault");
            Assert.That(_session.WorldClock.CurrentDate, Is.EqualTo(date));
            Assert.That(_session.Industry.Resources, Is.EqualTo(resources));
            Assert.That(_session.ResearchState.Progress, Is.EqualTo(progress));
            Assert.That(_session.ResearchState.KnownTechniqueIds, Is.Empty);
            Assert.That(_session.ChronicleEntries.Count, Is.EqualTo(chronicle));
            Assert.That(JsonUtility.ToJson(_session.RandomState.CaptureState()), Is.EqualTo(rng),
                "every deterministic random stream, including the consumed event stream, must roll back");
            Assert.That(JsonUtility.ToJson(_session.EventRuntimeStateForSave), Is.EqualTo(runtime),
                "the event runtime queue and markers must roll back with the date");
            Assert.That(_session.PendingEventId, Is.EqualTo(pending));

            _session.MonthSettlementFaultInjection = null;
            Assert.That(_session.TryAdvanceToNextMonth(), Is.True, _session.LastEventMessage);
            ResolvePending(_session);
            Assert.That(_session.WorldClock.CurrentDate, Is.EqualTo(new GameDate(1560, 2, 1)));
            Assert.That(_session.Industry.Resources.Funds, Is.EqualTo(2),
                "the family food bill is paid before the research month is charged");
            Assert.That(_session.Industry.Resources.Supplies, Is.EqualTo(1));
            Assert.That(_session.ResearchState.Progress, Is.EqualTo(1));

            Assert.That(_session.TryAdvanceToNextMonth(), Is.True, _session.LastEventMessage);
            ResolvePending(_session);
            Assert.That(_session.ResearchState.IsKnownTechnique(
                FamilyResearchCatalog.SmokeMixingTechniqueId), Is.True);
        }

        [Test]
        public void IndustryAssignee_PausesAtMonthEndWithoutResearchProgress()
        {
            Assert.That(_session.TrySetResearchPlan(FamilyResearchCatalog.SmokeMixingResearchId,
                SC021FamilyResearchScenario.HeadId).Succeeded, Is.True);
            Assert.That(_session.TryAssignIndustry(IndustryCatalog.FarmId,
                SC021FamilyResearchScenario.HeadId).Succeeded, Is.True);

            Assert.That(_session.TryAdvanceToNextMonth(), Is.True, _session.LastEventMessage);
            ResolvePending(_session);

            Assert.That(_session.ResearchState.Progress, Is.Zero);
            Assert.That(_session.ResearchState.KnownTechniqueIds, Is.Empty);
            Assert.That(_session.LastResearchReport.Paused, Is.True);
            Assert.That(_session.LastResearchReport.Summary, Does.Contain("产业"));
        }

        [Test]
        public void Succession_RetainsFamilyKnowledgeWithoutGrantingPersonalMastery()
        {
            UnityEngine.Object.DestroyImmediate(_session.gameObject);
            _session = GameSession.NewGame();
            _session.InitStandardDemo();
            Assert.That(_session.TrySetResearchPlan(FamilyResearchCatalog.SmokeMixingResearchId,
                new PersonId("person.akechi.harunobu")).Succeeded, Is.True);
            Assert.That(_session.TryAdvanceToNextMonth(), Is.True, _session.LastEventMessage);
            ResolvePending(_session);
            Assert.That(_session.TryAdvanceToNextMonth(), Is.True, _session.LastEventMessage);
            ResolvePending(_session);
            Assert.That(_session.ResearchState.IsKnownTechnique(
                FamilyResearchCatalog.SmokeMixingTechniqueId), Is.True);

            Assert.That(_session.BeginSuccessionRetirement().Succeeded, Is.True);
            Assert.That(_session.SelectSuccessionSuccessor(
                new PersonId("person.akechi.masatsuna")).Succeeded, Is.True);
            Assert.That(_session.ConfirmSuccession().Succeeded, Is.True);

            Assert.That(_session.SuccessionHeadName, Is.EqualTo("明智 正綱"));
            Assert.That(_session.ResearchState.IsKnownTechnique(
                FamilyResearchCatalog.SmokeMixingTechniqueId), Is.True,
                "family knowledge must survive succession");
            Assert.That(_session.Development.TryGetValues(
                new PersonId("person.akechi.masatsuna"), out CharacterGrowthValues values), Is.True);
            Assert.That(values.Ninjutsu, Is.Zero);
            Assert.That(values.MartialArts, Is.Zero);
            Assert.That(values.Scholarship, Is.Zero);
            Assert.That(values.Management, Is.Zero);
            Assert.That(values.Social, Is.Zero);
        }

        private static string EventStreamSnapshot(RandomStateSaveData state)
        {
            if (state == null || state.streams == null)
                return null;
            string streamId = RandomStateManager.StableId(RandomStreamKind.Events);
            foreach (RandomStreamSaveEntry entry in state.streams)
                if (entry != null && entry.streamId == streamId)
                    return entry.state0Hex + ":" + entry.state1Hex;
            return null;
        }

        private GameSession Fresh()
        {
            UnityEngine.Object.DestroyImmediate(_session.gameObject);
            _session = GameSession.NewGame();
            _session.InitResearchDemoScenario();
            return _session;
        }

        private static void ResolvePending(GameSession session)
        {
            if (string.IsNullOrEmpty(session.PendingEventId))
                return;
            string pending = session.PendingEventId;
            EventDefinitionData definition = session.EventCatalog.events
                .FirstOrDefault(item => item.eventId == pending);
            Assert.That(definition, Is.Not.Null);
            foreach (EventOptionData option in definition.options)
            {
                if (!session.TryGetEventOptionAvailability(pending, option.optionId,
                        out bool enabled, out _) || !enabled)
                    continue;
                Assert.That(session.TryChooseEventOption(pending, option.optionId,
                    session.CreateEventResultId(pending, option.optionId)).Succeeded, Is.True);
                return;
            }
            Assert.Fail("no enabled option for " + pending);
        }
    }
}
