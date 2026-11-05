using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;
using SengokuChronicle.Events;
using SengokuChronicle.Persistence;

namespace SengokuChronicle.Tests.EditMode
{
    /// <summary>SC-020 Task 2: fixed scenario, plan commands and the annual month transaction.</summary>
    public sealed class SC020EducationSessionTests
    {
        private GameSession _session;

        [SetUp]
        public void SetUp()
        {
            _session = GameSession.NewGame();
            _session.InitEducationDemoScenario();
        }

        [TearDown]
        public void TearDown()
        {
            if (_session != null)
                UnityEngine.Object.DestroyImmediate(_session.gameObject);
        }

        [Test]
        public void Scenario_HasFixedDateFamilyAgesRelationshipsAndValues()
        {
            Assert.That(_session.WorldClock.CurrentDate, Is.EqualTo(new GameDate(1560, 1, 1)));
            Assert.That(_session.Succession.World.Family.Id, Is.EqualTo(SC020EducationScenario.FamilyId));
            Assert.That(_session.Succession.HeadPersonId, Is.EqualTo(SC020EducationScenario.HeadId));
            Assert.That(_session.Succession.World.PersonCount, Is.EqualTo(3));

            AssertAge(SC020EducationScenario.HeadId, 40);
            AssertAge(SC020EducationScenario.SpouseId, 38);
            AssertAge(SC020EducationScenario.ChildId, 15);

            Assert.That(_session.Development.TryGetValues(SC020EducationScenario.ChildId,
                out CharacterGrowthValues values), Is.True);
            Assert.That(values.Ninjutsu, Is.EqualTo(15));
            Assert.That(values.MartialArts, Is.EqualTo(12));
            Assert.That(values.Scholarship, Is.EqualTo(20));
            Assert.That(values.Management, Is.EqualTo(10));
            Assert.That(values.Social, Is.EqualTo(13));

            IReadOnlyList<KinshipLink> links = _session.Succession.World.Kinship.Links;
            Assert.That(links.Count(link => link.Type == KinshipType.Spouse), Is.EqualTo(1));
            Assert.That(links.Count(link => link.Type == KinshipType.ParentChild &&
                link.To == SC020EducationScenario.ChildId), Is.EqualTo(2));
            Assert.That(_session.Succession.World.Family.TryGetRole(
                SC020EducationScenario.HeadId, out FamilyMemberRole role), Is.True);
            Assert.That(role, Is.EqualTo(FamilyMemberRole.Head));
        }

        [Test]
        public void Scenario_UsesSc019OpeningIndustryResourcesAndDoesNotActivateSc013()
        {
            Assert.That(_session.Industry.Resources.Funds, Is.EqualTo(30));
            Assert.That(_session.Industry.Resources.Supplies, Is.EqualTo(12));
            Assert.That(_session.Industry.Resources.Intelligence, Is.Zero);
            Assert.That(_session.Industry.LastSettledMonthIndex,
                Is.EqualTo(IndustryCalendar.MonthIndex(new GameDate(1560, 1, 1)) - 1));
            Assert.That(_session.IsM2ScenarioActive, Is.False);
            Assert.That(SC020EducationScenario.IsActive(_session.Succession), Is.True);
        }

        [Test]
        public void Preview_ShowsExactPlusNineAndPlusSeven()
        {
            EducationPreview spouse = _session.PreviewEducation(EducationDirection.Scholarship,
                SC020EducationScenario.SpouseId);
            Assert.That(spouse.Succeeded, Is.True, spouse.Error);
            Assert.That(spouse.Before.Scholarship, Is.EqualTo(20));
            Assert.That(spouse.After.Scholarship, Is.EqualTo(29));
            Assert.That(spouse.PreviewLine, Does.Contain("20 → 29"));

            EducationPreview head = _session.PreviewEducation(EducationDirection.Scholarship,
                SC020EducationScenario.HeadId);
            Assert.That(head.Succeeded, Is.True, head.Error);
            Assert.That(head.After.Scholarship, Is.EqualTo(27));
            Assert.That(head.PreviewLine, Does.Contain("20 → 27"));

            EducationPreview free = _session.PreviewEducation(EducationDirection.FreeGrowth, default);
            Assert.That(free.Succeeded, Is.True, free.Error);
            Assert.That(free.After.Scholarship, Is.EqualTo(21));
        }

        [Test]
        public void SetPlan_DoesNotGrantGrowthOrWriteChronicle()
        {
            int chronicle = _session.ChronicleEntries.Count;

            EducationPlanCommandResult result = _session.TrySetEducationPlan(
                SC020EducationScenario.ChildId, EducationDirection.Scholarship,
                SC020EducationScenario.SpouseId);

            Assert.That(result.Succeeded, Is.True, result.Message);
            Assert.That(_session.ChronicleEntries.Count, Is.EqualTo(chronicle));
            Assert.That(_session.Development.TryGetValues(SC020EducationScenario.ChildId,
                out CharacterGrowthValues values), Is.True);
            Assert.That(values.Scholarship, Is.EqualTo(20),
                "submission must never grant the annual reward immediately");
            Assert.That(_session.Development.TryGetPlan(SC020EducationScenario.ChildId, 15,
                out EducationYearPlan plan), Is.True);
            Assert.That(plan.Mentor, Is.EqualTo(SC020EducationScenario.SpouseId));
            Assert.That(_session.Education.HasPlan, Is.True);
        }

        [Test]
        public void SetPlan_ReplacesPendingPlanWithoutGrowth()
        {
            Assert.That(_session.TrySetEducationPlan(SC020EducationScenario.ChildId,
                EducationDirection.Scholarship, SC020EducationScenario.SpouseId).Succeeded, Is.True);
            Assert.That(_session.TrySetEducationPlan(SC020EducationScenario.ChildId,
                EducationDirection.MartialArts, SC020EducationScenario.HeadId).Succeeded, Is.True);

            Assert.That(_session.Development.TryGetPlan(SC020EducationScenario.ChildId, 15,
                out EducationYearPlan plan), Is.True);
            Assert.That(plan.Direction, Is.EqualTo(EducationDirection.MartialArts));
            Assert.That(plan.Mentor, Is.EqualTo(SC020EducationScenario.HeadId));
            Assert.That(_session.Development.TryGetValues(SC020EducationScenario.ChildId,
                out CharacterGrowthValues values), Is.True);
            Assert.That(values.MartialArts, Is.EqualTo(12));
            Assert.That(_session.ChronicleEntries.Count, Is.Zero);
        }

        [Test]
        public void SetPlan_RejectsNonMemberDeadRetiredAndChildMentors()
        {
            EducationPlanCommandResult unknown = _session.TrySetEducationPlan(
                SC020EducationScenario.ChildId, EducationDirection.Scholarship,
                new PersonId("person.unknown"));
            Assert.That(unknown.Succeeded, Is.False);
            Assert.That(unknown.Outcome, Is.EqualTo(EducationPlanOutcome.InvalidMentor));

            EducationPlanCommandResult self = _session.TrySetEducationPlan(
                SC020EducationScenario.ChildId, EducationDirection.Scholarship,
                SC020EducationScenario.ChildId);
            Assert.That(self.Succeeded, Is.False);
            Assert.That(self.Outcome, Is.EqualTo(EducationPlanOutcome.InvalidMentor));

            Assert.That(_session.Succession.World.TrySetLife(SC020EducationScenario.SpouseId,
                new SuccessionPersonLife(SC020EducationScenario.SpouseBirthYear, false, false),
                out string deadError), Is.True, deadError);
            Assert.That(_session.TrySetEducationPlan(SC020EducationScenario.ChildId,
                EducationDirection.Scholarship, SC020EducationScenario.SpouseId).Succeeded, Is.False);

            Assert.That(_session.Succession.World.TrySetLife(SC020EducationScenario.SpouseId,
                new SuccessionPersonLife(SC020EducationScenario.SpouseBirthYear, true, true),
                out string retiredError), Is.True, retiredError);
            Assert.That(_session.TrySetEducationPlan(SC020EducationScenario.ChildId,
                EducationDirection.Scholarship, SC020EducationScenario.SpouseId).Succeeded, Is.False);

            Assert.That(_session.TrySetEducationPlan(SC020EducationScenario.ChildId,
                EducationDirection.FreeGrowth, SC020EducationScenario.SpouseId).Succeeded, Is.False);
        }

        [Test]
        public void SetPlan_RejectsUnknownLearnerAndAdultLearner()
        {
            Assert.That(_session.TrySetEducationPlan(new PersonId("person.unknown"),
                EducationDirection.FreeGrowth, default).Outcome,
                Is.EqualTo(EducationPlanOutcome.InvalidLearner));

            Assert.That(_session.TrySetEducationPlan(SC020EducationScenario.HeadId,
                EducationDirection.FreeGrowth, default).Outcome,
                Is.EqualTo(EducationPlanOutcome.InvalidAge));

            Assert.That(_session.AdvanceDaysForLegacyFixtureSetup(366), Is.True);
            Assert.That(_session.WorldClock.CurrentDate, Is.EqualTo(new GameDate(1561, 1, 1)));
            Assert.That(_session.TrySetEducationPlan(SC020EducationScenario.ChildId,
                EducationDirection.FreeGrowth, default).Outcome,
                Is.EqualTo(EducationPlanOutcome.InvalidAge));
            Assert.That(_session.CanEditEducationPlans, Is.False);
        }

        [Test]
        public void ElevenMonthEnds_DoNotSettleEducation()
        {
            AdvanceMonths(11);

            Assert.That(_session.WorldClock.CurrentDate, Is.EqualTo(new GameDate(1560, 12, 1)));
            Assert.That(_session.Development.TryGetValues(SC020EducationScenario.ChildId,
                out CharacterGrowthValues values), Is.True);
            Assert.That(values.Scholarship, Is.EqualTo(20));
            Assert.That(EducationEntries(), Is.Zero);
        }

        [Test]
        public void DecemberEnd_SettlesScholarshipOnceAndReachesAgeSixteen()
        {
            Assert.That(_session.TrySetEducationPlan(SC020EducationScenario.ChildId,
                EducationDirection.Scholarship, SC020EducationScenario.SpouseId).Succeeded, Is.True);
            AdvanceMonths(11);
            Assert.That(_session.WorldClock.CurrentDate, Is.EqualTo(new GameDate(1560, 12, 1)));

            Assert.That(_session.TryAdvanceToNextMonth(), Is.True, _session.LastEventMessage);
            ResolvePendingEvent();

            Assert.That(_session.WorldClock.CurrentDate, Is.EqualTo(new GameDate(1561, 1, 1)));
            Assert.That(_session.Development.TryGetValues(SC020EducationScenario.ChildId,
                out CharacterGrowthValues values), Is.True);
            Assert.That(values.Scholarship, Is.EqualTo(29));
            Assert.That(values.Ninjutsu, Is.EqualTo(15));
            AssertAge(SC020EducationScenario.ChildId, 16);
            Assert.That(_session.Education.IsEligible, Is.False);
            Assert.That(EducationEntries(), Is.EqualTo(1));

            FamilyChronicleEntry record = EducationRecord();
            Assert.That(record.SubjectId, Is.EqualTo(SC020EducationScenario.ChildIdValue));
            Assert.That(record.SourceId, Is.EqualTo("education.scholarship"));
            Assert.That(record.FromValue, Is.EqualTo(20));
            Assert.That(record.ToValue, Is.EqualTo(29));
            Assert.That(record.Summary, Does.Contain("学问"));
            Assert.That(record.Summary, Does.Contain("小夜"));
            Assert.That(record.Summary, Does.Contain("20 → 29"));
            Assert.That(record.Title, Does.Not.Contain("person."));
        }

        [Test]
        public void UnplannedDecember_UsesFreeGrowthAndWritesOneRecord()
        {
            AdvanceMonths(11);
            Assert.That(_session.TryAdvanceToNextMonth(), Is.True, _session.LastEventMessage);
            ResolvePendingEvent();

            Assert.That(_session.Development.TryGetValues(SC020EducationScenario.ChildId,
                out CharacterGrowthValues values), Is.True);
            Assert.That(values.Ninjutsu, Is.EqualTo(16));
            Assert.That(values.MartialArts, Is.EqualTo(13));
            Assert.That(values.Scholarship, Is.EqualTo(21));
            Assert.That(values.Management, Is.EqualTo(11));
            Assert.That(values.Social, Is.EqualTo(14));
            Assert.That(EducationEntries(), Is.EqualTo(1));
            Assert.That(EducationRecord().SourceId, Is.EqualTo("education.free-growth"));
        }

        [Test]
        public void RepeatedMonthEnd_AfterSettlement_DoesNotGrantGrowthAgain()
        {
            Assert.That(_session.TrySetEducationPlan(SC020EducationScenario.ChildId,
                EducationDirection.Scholarship, SC020EducationScenario.SpouseId).Succeeded, Is.True);
            AdvanceMonths(11);
            Assert.That(_session.TryAdvanceToNextMonth(), Is.True, _session.LastEventMessage);
            ResolvePendingEvent();

            Assert.That(_session.TryAdvanceToNextMonth(), Is.True, _session.LastEventMessage);
            ResolvePendingEvent();

            Assert.That(_session.Development.TryGetValues(SC020EducationScenario.ChildId,
                out CharacterGrowthValues values), Is.True);
            Assert.That(values.Scholarship, Is.EqualTo(29));
            Assert.That(EducationEntries(), Is.EqualTo(1));
            Assert.That(_session.LastMonthTurnOutcome, Is.EqualTo(MonthTurnOutcome.Succeeded));
        }

        [Test]
        public void FaultDuringDecemberSettlement_RollsBackEverythingAndRetrySucceedsOnce()
        {
            Assert.That(_session.TrySetEducationPlan(SC020EducationScenario.ChildId,
                EducationDirection.Scholarship, SC020EducationScenario.SpouseId).Succeeded, Is.True);
            AdvanceMonths(11);
            GameDate dateBefore = _session.WorldClock.CurrentDate;
            int chronicleBefore = _session.ChronicleEntries.Count;
            string rngBefore = UnityEngine.JsonUtility.ToJson(_session.RandomState.CaptureState());
            string runtimeBefore = UnityEngine.JsonUtility.ToJson(_session.EventRuntimeStateForSave);

            _session.MonthSettlementFaultInjection = () =>
                throw new System.InvalidOperationException("injected SC-020 settlement failure");
            try
            {
                Assert.That(_session.TryAdvanceToNextMonth(), Is.False);
            }
            finally
            {
                _session.MonthSettlementFaultInjection = null;
            }

            Assert.That(_session.WorldClock.CurrentDate, Is.EqualTo(dateBefore));
            Assert.That(_session.LastMonthTurnOutcome, Is.EqualTo(MonthTurnOutcome.SettlementFailed));
            Assert.That(_session.ChronicleEntries.Count, Is.EqualTo(chronicleBefore));
            Assert.That(_session.Development.TryGetValues(SC020EducationScenario.ChildId,
                out CharacterGrowthValues values), Is.True);
            Assert.That(values.Scholarship, Is.EqualTo(20));
            Assert.That(_session.Development.TryGetPlan(SC020EducationScenario.ChildId, 15, out _), Is.True);
            Assert.That(UnityEngine.JsonUtility.ToJson(_session.RandomState.CaptureState()),
                Is.EqualTo(rngBefore));
            Assert.That(UnityEngine.JsonUtility.ToJson(_session.EventRuntimeStateForSave),
                Is.EqualTo(runtimeBefore));

            Assert.That(_session.TryAdvanceToNextMonth(), Is.True, _session.LastEventMessage);
            ResolvePendingEvent();
            Assert.That(_session.Development.TryGetValues(SC020EducationScenario.ChildId,
                out values), Is.True);
            Assert.That(values.Scholarship, Is.EqualTo(29));
            Assert.That(EducationEntries(), Is.EqualTo(1));
        }

        [Test]
        public void SaveLoadAtDecember_PreservesPlanAndSettlesOnce()
        {
            Assert.That(_session.TrySetEducationPlan(SC020EducationScenario.ChildId,
                EducationDirection.Scholarship, SC020EducationScenario.SpouseId).Succeeded, Is.True);
            AdvanceMonths(11);
            SaveData data = _session.CaptureSave();
            Assert.That(SaveDataValidator.Validate(data).Succeeded, Is.True);

            GameSession restored = GameSession.NewGame();
            try
            {
                Assert.That(restored.TryApplySave(data).Succeeded, Is.True);
                Assert.That(restored.WorldClock.CurrentDate, Is.EqualTo(new GameDate(1560, 12, 1)));
                Assert.That(restored.Development.TryGetPlan(SC020EducationScenario.ChildId, 15,
                    out EducationYearPlan plan), Is.True);
                Assert.That(plan.Direction, Is.EqualTo(EducationDirection.Scholarship));
                Assert.That(restored.ChronicleEntries.Count,
                    Is.EqualTo(_session.ChronicleEntries.Count));

                Assert.That(restored.TryAdvanceToNextMonth(), Is.True, restored.LastEventMessage);
                ResolvePendingEvent(restored);
                Assert.That(restored.Development.TryGetValues(SC020EducationScenario.ChildId,
                    out CharacterGrowthValues values), Is.True);
                Assert.That(values.Scholarship, Is.EqualTo(29));
                Assert.That(EducationEntries(restored), Is.EqualTo(1));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(restored.gameObject);
            }
        }

        private void AssertAge(PersonId personId, int expected)
        {
            Assert.That(_session.Succession.World.TryGetLife(personId,
                out SuccessionPersonLife life), Is.True);
            Assert.That(life.AgeAt(_session.WorldClock.CurrentDate), Is.EqualTo(expected));
        }

        private void AdvanceMonths(int count)
        {
            for (int i = 0; i < count; i++)
            {
                Assert.That(_session.TryAdvanceToNextMonth(), Is.True, _session.LastEventMessage);
                ResolvePendingEvent();
            }
        }

        private int EducationEntries() => EducationEntries(_session);

        private static int EducationEntries(GameSession session) =>
            session.ChronicleEntries.Count(entry => entry.Category == ChronicleCategory.Education);

        private FamilyChronicleEntry EducationRecord() =>
            _session.ChronicleEntries.First(entry => entry.Category == ChronicleCategory.Education);

        private static void ResolvePendingEvent(GameSession session) => ResolvePendingEventStatic(session);

        private void ResolvePendingEvent() => ResolvePendingEventStatic(_session);

        private static void ResolvePendingEventStatic(GameSession session)
        {
            if (string.IsNullOrEmpty(session.PendingEventId))
                return;
            Assert.That(session.EventCatalogReady, Is.True, session.LastEventMessage);
            string pending = session.PendingEventId;
            EventDefinitionData definition = session.EventCatalog.events
                .FirstOrDefault(item => item.eventId == pending);
            Assert.That(definition, Is.Not.Null, "pending event must exist in the catalog");
            foreach (EventOptionData option in definition.options)
            {
                if (!session.TryGetEventOptionAvailability(pending, option.optionId,
                        out bool enabled, out _) || !enabled)
                    continue;
                EventChoiceResult result = session.TryChooseEventOption(pending, option.optionId,
                    session.CreateEventResultId(pending, option.optionId));
                Assert.That(result.Succeeded, Is.True, result.Feedback);
                return;
            }
            Assert.Fail("no enabled option for pending event " + pending);
        }
    }
}
