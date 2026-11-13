using System.Reflection;
using NUnit.Framework;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;
using SengokuChronicle.Events;
using SengokuChronicle.Persistence;

namespace SengokuChronicle.Tests.EditMode
{
    /// <summary>SC-019 Task 3: only ending the month may cross a month or settle it.</summary>
    public sealed class IndustryMonthCrossingTests
    {
        private GameSession _session;

        [SetUp]
        public void SetUp() => _session = GameSession.NewGame();

        [TearDown]
        public void TearDown()
        {
            if (_session != null)
                UnityEngine.Object.DestroyImmediate(_session.gameObject);
        }

        private static void Choose(GameSession session, string eventId, string optionId)
        {
            Assert.That(session.TryChooseEventOption(eventId, optionId,
                session.CreateEventResultId(eventId, optionId)).Succeeded, Is.True,
                session.LastEventMessage);
        }

        [Test]
        public void NewGame_CanAdvanceOneDayWithinMonth()
        {
            Assert.That(_session.CanAdvanceOneDayWithinMonth, Is.True);
            Assert.That(_session.Industry.LastSettledMonthIndex,
                Is.EqualTo(IndustryCalendar.MonthIndex(_session.WorldClock.CurrentDate) - 1));
            Assert.That(_session.Succession.IsPending, Is.False);
            Assert.That(_session.PendingEventId, Is.Empty);
        }

        [Test]
        public void AdvanceOneDay_OnLastDay_RefusesAndKeepsDate()
        {
            Assert.That(_session.AdvanceDays(29), Is.True);
            GameDate lastDay = _session.WorldClock.CurrentDate;
            Assert.That(lastDay, Is.EqualTo(new GameDate(1560, 4, 30)));

            Assert.That(_session.AdvanceOneDay(), Is.False);
            Assert.That(_session.WorldClock.CurrentDate, Is.EqualTo(lastDay));
            Assert.That(_session.LastMonthTurnOutcome, Is.EqualTo(MonthTurnOutcome.MonthSettlementRequired));
            Assert.That(_session.LastEventMessage, Does.Contain("结束本月"));
            Assert.That(_session.CanAdvanceOneDayWithinMonth, Is.False);
        }

        [Test]
        public void AdvanceDays_CrossingMonth_RefusesAtomically()
        {
            GameDate date = _session.WorldClock.CurrentDate;
            Assert.That(_session.AdvanceDays(30), Is.False);
            Assert.That(_session.WorldClock.CurrentDate, Is.EqualTo(date));
            Assert.That(_session.LastMonthTurnOutcome, Is.EqualTo(MonthTurnOutcome.MonthSettlementRequired));
        }

        [Test]
        public void PreviewTravel_CrossingMonth_RefusesWithReason()
        {
            Assert.That(_session.AdvanceDays(29), Is.True);
            GameDate date = _session.WorldClock.CurrentDate;
            LocationId location = _session.CurrentLocation;

            Assert.That(_session.PreviewTravel(LocationId.Nagoya).HasValue, Is.False);
            Assert.That(_session.LastTravelBlockReason, Does.Contain("跨越月末"));
            Assert.That(_session.WorldClock.CurrentDate, Is.EqualTo(date));
            Assert.That(_session.CurrentLocation, Is.EqualTo(location));
        }

        [Test]
        public void ConfirmTravel_CrossingMonth_RefusesAndKeepsLocation()
        {
            Assert.That(_session.AdvanceDays(29), Is.True);
            GameDate date = _session.WorldClock.CurrentDate;
            LocationId location = _session.CurrentLocation;
            var forged = new TravelPreview(LocationId.Nagoya, "那古野", 1, new GameDate(1560, 5, 1));

            Assert.That(_session.ConfirmTravel(forged), Is.False);
            Assert.That(_session.LastTravelBlockReason, Does.Contain("跨越月末"));
            Assert.That(_session.WorldClock.CurrentDate, Is.EqualTo(date));
            Assert.That(_session.CurrentLocation, Is.EqualTo(location));
        }

        [Test]
        public void EndMonth_SettlesExactlyOnceAndAdvances()
        {
            _session.TryAssignIndustry(IndustryCatalog.FarmId, M2FixedScenario.HarunobuId);
            _session.TryAssignIndustry(IndustryCatalog.TradingHouseId, M2FixedScenario.ChieId);

            Assert.That(_session.TryAdvanceToNextMonth(), Is.True, _session.LastEventMessage);

            Assert.That(_session.WorldClock.CurrentDate, Is.EqualTo(new GameDate(1560, 5, 1)));
            Assert.That(_session.Industry.Resources.Funds, Is.EqualTo(32));
            Assert.That(_session.Industry.Resources.Supplies, Is.EqualTo(13));
            Assert.That(_session.Industry.Resources.Intelligence, Is.EqualTo(1));
            Assert.That(_session.LastIndustryReport, Is.Not.Null);
            Assert.That(_session.LastIndustryReport.Year, Is.EqualTo(1560));
            Assert.That(_session.LastIndustryReport.Month, Is.EqualTo(4));
        }

        [Test]
        public void EndMonth_PendingEvent_DoesNotSettle()
        {
            _session.TryAssignIndustry(IndustryCatalog.FarmId, M2FixedScenario.HarunobuId);
            Assert.That(_session.TryAdvanceToNextMonth(), Is.True, _session.LastEventMessage);
            Assert.That(_session.PendingEventId, Is.Not.Empty);
            IndustryResources resources = _session.Industry.Resources;
            int settled = _session.Industry.LastSettledMonthIndex;

            Assert.That(_session.TryAdvanceToNextMonth(), Is.False);
            Assert.That(_session.LastMonthTurnOutcome, Is.EqualTo(MonthTurnOutcome.PendingEvent));
            Assert.That(_session.Industry.Resources, Is.EqualTo(resources));
            Assert.That(_session.Industry.LastSettledMonthIndex, Is.EqualTo(settled));
        }

        [Test]
        public void EndMonth_PendingSuccession_DoesNotSettle()
        {
            _session.InitStandardDemo();
            _session.TryAssignIndustry(IndustryCatalog.FarmId, SuccessionSample.StandardHeadId);
            _session.BeginSuccessionRetirement();
            Assert.That(_session.Succession.Phase, Is.EqualTo(SuccessionPhase.AwaitingSuccessionChoice));
            IndustryResources resources = _session.Industry.Resources;
            GameDate date = _session.WorldClock.CurrentDate;

            Assert.That(_session.TryAdvanceToNextMonth(), Is.False);
            Assert.That(_session.LastMonthTurnOutcome, Is.EqualTo(MonthTurnOutcome.PendingSuccession));
            Assert.That(_session.Industry.Resources, Is.EqualTo(resources));
            Assert.That(_session.WorldClock.CurrentDate, Is.EqualTo(date));
        }

        [Test]
        public void EndMonth_AtMaxDate_DoesNotSettle()
        {
            Assert.That(_session.AdvanceDaysForLegacyFixtureSetup(160981), Is.True);
            Assert.That(_session.WorldClock.CurrentDate, Is.EqualTo(new GameDate(2000, 12, 31)));
            IndustryResources resources = _session.Industry.Resources;
            int settled = _session.Industry.LastSettledMonthIndex;

            Assert.That(_session.TryAdvanceToNextMonth(), Is.False);
            Assert.That(_session.LastMonthTurnOutcome, Is.EqualTo(MonthTurnOutcome.DateOutOfRange));
            Assert.That(_session.Industry.Resources, Is.EqualTo(resources));
            Assert.That(_session.Industry.LastSettledMonthIndex, Is.EqualTo(settled));
        }

        [Test]
        public void EndMonth_SettlementGap_DoesNotSettle()
        {
            _session.TryAssignIndustry(IndustryCatalog.FarmId, M2FixedScenario.HarunobuId);
            IndustryState state = State(_session);
            Assert.That(state.TrySetLastSettledMonthIndex(
                IndustryCalendar.MonthIndex(_session.WorldClock.CurrentDate) - 2, out _), Is.True);
            IndustryResources resources = _session.Industry.Resources;
            GameDate date = _session.WorldClock.CurrentDate;

            Assert.That(_session.TryAdvanceToNextMonth(), Is.False);
            Assert.That(_session.LastMonthTurnOutcome, Is.EqualTo(MonthTurnOutcome.IndustryStateInvalid));
            Assert.That(_session.LastEventMessage, Does.Contain("未结算"));
            Assert.That(_session.Industry.Resources, Is.EqualTo(resources));
            Assert.That(_session.WorldClock.CurrentDate, Is.EqualTo(date));
        }

        [Test]
        public void EndMonth_FaultAfterDateAdvance_RollsBackDateResourcesLastSettledAndChronicle()
        {
            _session.TryAssignIndustry(IndustryCatalog.FarmId, M2FixedScenario.HarunobuId);
            GameDate dateBefore = _session.WorldClock.CurrentDate;
            IndustryResources resourcesBefore = _session.Industry.Resources;
            int settledBefore = _session.Industry.LastSettledMonthIndex;
            int chronicleBefore = _session.ChronicleEntries.Count;
            string pendingBefore = _session.PendingEventId;
            RandomStateSaveData rngBefore = _session.RandomState.CaptureState();
            EventRuntimeSaveData runtimeBefore = _session.EventRuntimeStateForSave;
            string rngSnapshotBefore = Snapshot(rngBefore);
            string runtimeSnapshotBefore = Snapshot(runtimeBefore);
            string eventStreamBefore = EventStreamSnapshot(rngBefore);
            string eventStreamAtFault = null;

            _session.MonthSettlementFaultInjection = () =>
            {
                eventStreamAtFault = EventStreamSnapshot(_session.RandomState.CaptureState());
                throw new System.InvalidOperationException("injected settlement failure");
            };
            try
            {
                Assert.That(_session.TryAdvanceToNextMonth(), Is.False);
            }
            finally
            {
                _session.MonthSettlementFaultInjection = null;
            }

            Assert.That(eventStreamAtFault, Is.Not.Null,
                "the injected fault must run inside the settlement commit phase");
            Assert.That(eventStreamAtFault, Is.Not.EqualTo(eventStreamBefore),
                "the injected fault must run after event scheduling consumed the event stream");
            Assert.That(_session.LastMonthTurnOutcome, Is.EqualTo(MonthTurnOutcome.SettlementFailed));
            Assert.That(_session.WorldClock.CurrentDate, Is.EqualTo(dateBefore),
                "the clock must be restored after a commit-phase exception");
            Assert.That(_session.Industry.Resources, Is.EqualTo(resourcesBefore));
            Assert.That(_session.Industry.LastSettledMonthIndex, Is.EqualTo(settledBefore));
            Assert.That(_session.ChronicleEntries.Count, Is.EqualTo(chronicleBefore),
                "the rollback must remove the provisional monthly chronicle entry");
            Assert.That(_session.PendingEventId, Is.EqualTo(pendingBefore));
            Assert.That(_session.LastIndustryReport, Is.Null);
            Assert.That(Snapshot(_session.RandomState.CaptureState()), Is.EqualTo(rngSnapshotBefore),
                "the rollback must restore every deterministic random stream, including the consumed event stream");
            Assert.That(Snapshot(_session.EventRuntimeStateForSave), Is.EqualTo(runtimeSnapshotBefore),
                "the rollback must restore the event runtime to its pre-settlement queue and markers");

            Assert.That(_session.TryAdvanceToNextMonth(), Is.True, _session.LastEventMessage);
            Assert.That(_session.WorldClock.CurrentDate, Is.EqualTo(new GameDate(1560, 5, 1)));
            Assert.That(_session.Industry.Resources.Funds, Is.EqualTo(31));
            Assert.That(_session.Industry.Resources.Supplies, Is.EqualTo(15));
            Assert.That(_session.ChronicleEntries.Count, Is.EqualTo(chronicleBefore + 1));
            Assert.That(_session.PendingEventId, Is.Not.Empty,
                "the retry must schedule the monthly event again after the rollback");
        }

        [Test]
        public void EndMonth_DuplicateCalls_SettleDistinctMonths()
        {
            _session.TryAssignIndustry(IndustryCatalog.FarmId, M2FixedScenario.HarunobuId);
            Assert.That(_session.TryAdvanceToNextMonth(), Is.True, _session.LastEventMessage);
            Choose(_session, M2FixedScenario.PledgeEventId, M2FixedScenario.BreakPromiseOptionId);
            int firstSettled = _session.Industry.LastSettledMonthIndex;

            Assert.That(_session.TryAdvanceToNextMonth(), Is.True, _session.LastEventMessage);
            int secondSettled = _session.Industry.LastSettledMonthIndex;

            Assert.That(secondSettled, Is.EqualTo(firstSettled + 1));
            Assert.That(_session.WorldClock.CurrentDate, Is.EqualTo(new GameDate(1560, 6, 1)));
        }

        [Test]
        public void LegacyFixtureSeam_IsInternalAndNotUsedByProduction()
        {
            MethodInfo seam = typeof(GameSession).GetMethod("AdvanceDaysForLegacyFixtureSetup",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(seam, Is.Not.Null);
            Assert.That(seam.IsAssembly, Is.True, "the fixture seam must stay internal");
            Assert.That(typeof(GameSession).GetMethod("AdvanceDaysForLegacyFixtureSetup",
                BindingFlags.Instance | BindingFlags.Public), Is.Null);

            IndustryResources resources = _session.Industry.Resources;
            Assert.That(_session.AdvanceDaysForLegacyFixtureSetup(800), Is.True);
            Assert.That(_session.Industry.Resources, Is.EqualTo(resources),
                "the seam must not produce resources");
            Assert.That(_session.Industry.LastSettledMonthIndex,
                Is.EqualTo(IndustryCalendar.MonthIndex(_session.WorldClock.CurrentDate) - 1));
        }

        private static string Snapshot(object state) => UnityEngine.JsonUtility.ToJson(state);

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

        private static IndustryState State(GameSession session)
        {
            FieldInfo field = typeof(GameSession).GetField("_industryState",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            return (IndustryState)field.GetValue(session);
        }
    }
}
