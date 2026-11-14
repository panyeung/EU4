using System;
using System.Collections.Generic;
using NUnit.Framework;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;
using SengokuChronicle.Persistence;
using UnityEngine;

namespace SengokuChronicle.Tests.EditMode
{
    /// <summary>
    /// M02 daily-action foundation coverage: mutual exclusion, explicit calendar-day
    /// progress, travel transit state, interruption/replanning, cross-month settlement
    /// and save/load continuation. No Play Mode behavior is asserted here.
    /// </summary>
    public sealed class M02DailyActionTests
    {
        private readonly List<GameSession> _sessions = new List<GameSession>();

        [TearDown]
        public void TearDown()
        {
            foreach (GameSession session in _sessions)
                if (session != null)
                    UnityEngine.Object.DestroyImmediate(session.gameObject);
            _sessions.Clear();
        }

        private GameSession NewSession(ulong seed = GameSession.DefaultRandomSeed)
        {
            var go = new GameObject(nameof(GameSession));
            GameSession session = go.AddComponent<GameSession>();
            session.InitNewGame(seed);
            _sessions.Add(session);
            return session;
        }

        private static void Choose(GameSession session, string eventId, string optionId)
        {
            Assert.That(session.TryChooseEventOption(eventId, optionId,
                session.CreateEventResultId(eventId, optionId)).Succeeded, Is.True,
                session.LastEventMessage);
        }

        [Test]
        public void Person_CannotRunTwoActionsAtOnce()
        {
            GameSession session = NewSession();
            PersonId person = M2FixedScenario.HarunobuId;

            Assert.That(session.TryPlanRestAction(person, 2, out string reason), Is.True, reason);
            Assert.That(session.TryGetDailyAction(person, out DailyActionTask task), Is.True);
            Assert.That(task.Kind, Is.EqualTo(DailyActionKind.Rest));
            Assert.That(task.RemainingDays, Is.EqualTo(2));

            Assert.That(session.TryPlanWorkAction(person, 1, out reason), Is.False);
            Assert.That(reason, Does.Contain("进行中的行动"));
            Assert.That(session.TryGetDailyAction(person, out task), Is.True);
            Assert.That(task.Kind, Is.EqualTo(DailyActionKind.Rest),
                "the rejected command must not replace the active action");
            Assert.That(task.RemainingDays, Is.EqualTo(2));
        }

        [Test]
        public void DayProgress_IsExplicitAndDeterministic()
        {
            GameSession first = NewSession(42UL);
            GameSession second = NewSession(42UL);
            PersonId person = M2FixedScenario.HarunobuId;

            Assert.That(first.TryPlanTrainingAction(person, 2, out string reason), Is.True, reason);
            Assert.That(second.TryPlanTrainingAction(person, 2, out reason), Is.True, reason);

            Assert.That(first.AdvanceOneDay(), Is.True);
            Assert.That(second.AdvanceOneDay(), Is.True);
            Assert.That(first.TryGetDailyAction(person, out DailyActionTask firstTask), Is.True);
            Assert.That(second.TryGetDailyAction(person, out DailyActionTask secondTask), Is.True);
            Assert.That(firstTask.RemainingDays, Is.EqualTo(1));
            Assert.That(secondTask.RemainingDays, Is.EqualTo(1));

            Assert.That(first.AdvanceOneDay(), Is.True);
            Assert.That(second.AdvanceOneDay(), Is.True);
            Assert.That(first.HasActiveDailyAction(person), Is.False);
            Assert.That(second.HasActiveDailyAction(person), Is.False);
            Assert.That(first.LastDailyActionOutcomes.Count, Is.EqualTo(1));
            Assert.That(second.LastDailyActionOutcomes.Count, Is.EqualTo(1));
            Assert.That(first.LastDailyActionOutcomes[0].Outcome,
                Is.EqualTo(DailyActionOutcomeKind.Completed));
            Assert.That(first.LastDailyActionOutcomes[0].Date, Is.EqualTo(new GameDate(1560, 4, 3)));
            Assert.That(second.LastDailyActionOutcomes[0].Date,
                Is.EqualTo(first.LastDailyActionOutcomes[0].Date));
        }

        [Test]
        public void Travel_IsInTransitAndCannotBeAtBothEndpoints()
        {
            GameSession session = NewSession();
            PersonId person = M2FixedScenario.HarunobuId;

            Assert.That(session.TryPlanTravelAction(person, LocationId.Atsuta, out string reason),
                Is.True, reason);
            Assert.That(session.TryGetDailyAction(person, out DailyActionTask travel), Is.True);
            Assert.That(travel.Kind, Is.EqualTo(DailyActionKind.Travel));
            Assert.That(travel.Origin, Is.EqualTo(LocationId.Kiyosu));
            Assert.That(travel.Destination, Is.EqualTo(LocationId.Atsuta));
            Assert.That(session.IsPersonInTransit(person), Is.True);
            Assert.That(session.TryGetPersonLocation(person, out _), Is.False,
                "a traveller has no endpoint location while the action is active");
            Assert.That(session.CurrentLocation, Is.EqualTo(LocationId.Kiyosu),
                "the legacy location surface keeps the last confirmed departure point");

            Assert.That(session.AdvanceOneDay(), Is.True);
            Assert.That(session.HasActiveDailyAction(person), Is.False);
            Assert.That(session.IsPersonInTransit(person), Is.False);
            Assert.That(session.TryGetPersonLocation(person, out LocationId arrived), Is.True);
            Assert.That(arrived, Is.EqualTo(LocationId.Atsuta));
            Assert.That(session.CurrentLocation, Is.EqualTo(LocationId.Atsuta));
            Assert.That(session.LastDailyActionOutcomes[0].Outcome,
                Is.EqualTo(DailyActionOutcomeKind.Completed));

            Assert.That(session.TryPlanTravelAction(person, LocationId.Nagoya, out reason), Is.False,
                "Atsuta and Nagoya have no direct route");
            Assert.That(session.HasActiveDailyAction(person), Is.False);
        }

        [Test]
        public void Interruption_AndReplanning_LeaveOneClearAction()
        {
            GameSession session = NewSession();
            PersonId person = M2FixedScenario.HarunobuId;

            Assert.That(session.TryPlanWorkAction(person, 3, out string reason), Is.True, reason);
            Assert.That(session.TryInterruptDailyAction(person, "改为优先训练。"), Is.True);
            Assert.That(session.LastDailyActionOutcomes[0].Outcome,
                Is.EqualTo(DailyActionOutcomeKind.Interrupted));
            Assert.That(session.LastDailyActionOutcomes[0].Reason, Does.Contain("改为优先训练"));
            Assert.That(session.HasActiveDailyAction(person), Is.False);

            Assert.That(session.TryPlanTrainingAction(person, 2, out reason), Is.True, reason);
            Assert.That(session.TryReplanDailyAction(person, DailyActionKind.Work, 1,
                default, out reason), Is.True, reason);
            Assert.That(session.HasActiveDailyAction(person), Is.True);
            Assert.That(session.TryGetDailyAction(person, out DailyActionTask replanned), Is.True);
            Assert.That(replanned.Kind, Is.EqualTo(DailyActionKind.Work));
            Assert.That(session.LastDailyActionOutcomes.Count, Is.EqualTo(2));
            Assert.That(session.LastDailyActionOutcomes[0].Outcome,
                Is.EqualTo(DailyActionOutcomeKind.Interrupted));
            Assert.That(session.LastDailyActionOutcomes[1].Outcome,
                Is.EqualTo(DailyActionOutcomeKind.Planned));
        }

        [Test]
        public void FailedReplan_RestoresTheOriginalAction()
        {
            GameSession session = NewSession();
            PersonId person = M2FixedScenario.HarunobuId;

            Assert.That(session.TryPlanRestAction(person, 3, out string reason), Is.True, reason);
            Assert.That(session.TryReplanDailyAction(person, DailyActionKind.Work, 0,
                default, out reason), Is.False);
            Assert.That(reason, Does.Contain("至少为一天"));
            Assert.That(session.TryGetDailyAction(person, out DailyActionTask original), Is.True);
            Assert.That(original.Kind, Is.EqualTo(DailyActionKind.Rest),
                "a rejected replan must not destroy the original action");
            Assert.That(original.RemainingDays, Is.EqualTo(3));
        }

        [Test]
        public void Replan_IsAtomicForObservers_AndRejectedReplanKeepsOriginal()
        {
            GameSession session = NewSession();
            PersonId person = M2FixedScenario.HarunobuId;
            Assert.That(session.TryPlanRestAction(person, 3, out string reason), Is.True, reason);

            int notifications = 0;
            bool sawTransientNoAction = false;
            DailyActionKind observedKind = DailyActionKind.Rest;
            int observedRemaining = 0;
            Action observer = () =>
            {
                notifications++;
                if (!session.TryGetDailyAction(person, out DailyActionTask observed))
                {
                    sawTransientNoAction = true;
                    return;
                }
                observedKind = observed.Kind;
                observedRemaining = observed.RemainingDays;
            };

            session.SessionStateChanged += observer;
            try
            {
                Assert.That(session.TryReplanDailyAction(person, DailyActionKind.Work, 1,
                    default, out reason), Is.True, reason);
            }
            finally
            {
                session.SessionStateChanged -= observer;
            }

            Assert.That(notifications, Is.EqualTo(1),
                "a successful replan must publish exactly one final state");
            Assert.That(sawTransientNoAction, Is.False,
                "observers must never see the gap between interrupt and replacement");
            Assert.That(observedKind, Is.EqualTo(DailyActionKind.Work));
            Assert.That(observedRemaining, Is.EqualTo(1));
            Assert.That(session.TryGetDailyAction(person, out DailyActionTask active), Is.True);
            Assert.That(active.Kind, Is.EqualTo(DailyActionKind.Work));
            Assert.That(active.RemainingDays, Is.EqualTo(1));

            notifications = 0;
            sawTransientNoAction = false;
            session.SessionStateChanged += observer;
            try
            {
                Assert.That(session.TryReplanDailyAction(person, DailyActionKind.Training, 0,
                    default, out reason), Is.False);
            }
            finally
            {
                session.SessionStateChanged -= observer;
            }

            Assert.That(reason, Does.Contain("至少为一天"));
            Assert.That(notifications, Is.Zero,
                "a rejected replan leaves state unchanged and publishes no transient state");
            Assert.That(sawTransientNoAction, Is.False);
            Assert.That(session.TryGetDailyAction(person, out active), Is.True);
            Assert.That(active.Kind, Is.EqualTo(DailyActionKind.Work),
                "a rejected replan must restore the original task");
            Assert.That(active.RemainingDays, Is.EqualTo(1));
        }

        [Test]
        public void CrossMonthAction_SettlesTheMonthExactlyOnce()
        {
            GameSession session = NewSession();
            PersonId person = M2FixedScenario.HarunobuId;
            Assert.That(session.AdvanceDays(28), Is.True);
            Assert.That(session.WorldClock.CurrentDate, Is.EqualTo(new GameDate(1560, 4, 29)));

            Assert.That(session.TryAssignIndustry(IndustryCatalog.FarmId, person).Succeeded, Is.True);
            Assert.That(session.TryPlanRestAction(person, 4, out string reason), Is.True, reason);

            // Apr 29 -> Apr 30 stays inside the month.
            Assert.That(session.AdvanceActionDay(), Is.True);
            Assert.That(session.WorldClock.CurrentDate, Is.EqualTo(new GameDate(1560, 4, 30)));
            Assert.That(session.TryGetDailyAction(person, out DailyActionTask task), Is.True);
            Assert.That(task.RemainingDays, Is.EqualTo(3));

            // Apr 30 -> May 1 must use the existing monthly transaction exactly once and
            // consume one action day, not zero or two.
            Assert.That(session.AdvanceActionDay(), Is.True, session.LastEventMessage);
            Assert.That(session.WorldClock.CurrentDate, Is.EqualTo(new GameDate(1560, 5, 1)));
            Assert.That(session.TryGetDailyAction(person, out task), Is.True);
            Assert.That(task.RemainingDays, Is.EqualTo(2));
            Assert.That(session.LastIndustryReport, Is.Not.Null);
            Assert.That(session.LastIndustryReport.Month, Is.EqualTo(4));
            Assert.That(session.Industry.LastSettledMonthIndex,
                Is.EqualTo(IndustryCalendar.MonthIndex(new GameDate(1560, 5, 1)) - 1));
            int fundsAfterApril = session.Industry.Resources.Funds;
            int industryDelta =
                session.LastIndustryReport.After.Funds - session.LastIndustryReport.Before.Funds;
            Assert.That(industryDelta, Is.GreaterThan(0),
                "the farm month itself still earns funds");
            Assert.That(fundsAfterApril,
                Is.EqualTo(session.LastIndustryReport.After.Funds -
                             session.LastMarketReport.FoodBillFunds),
                "the household food bill is applied once on top of the industry month");

            if (!string.IsNullOrEmpty(session.PendingEventId))
                Choose(session, M2FixedScenario.PledgeEventId, M2FixedScenario.KeepPromiseOptionId);

            // May 2 and May 3 are ordinary days: no second April settlement.
            Assert.That(session.AdvanceOneDay(), Is.True);
            Assert.That(session.AdvanceOneDay(), Is.True);
            Assert.That(session.WorldClock.CurrentDate, Is.EqualTo(new GameDate(1560, 5, 3)));
            Assert.That(session.HasActiveDailyAction(person), Is.False);
            Assert.That(session.LastDailyActionOutcomes[0].Outcome,
                Is.EqualTo(DailyActionOutcomeKind.Completed));
            Assert.That(session.Industry.Resources.Funds, Is.EqualTo(fundsAfterApril),
                "in-flight action days inside May must not repeat April settlement");
            Assert.That(session.LastIndustryReport.Month, Is.EqualTo(4));

            // June settlement is exactly one further month, not a replay of April/May.
            Assert.That(session.TryAdvanceToNextMonth(), Is.True, session.LastEventMessage);
            Assert.That(session.WorldClock.CurrentDate, Is.EqualTo(new GameDate(1560, 6, 1)));
            Assert.That(session.LastIndustryReport.Month, Is.EqualTo(5));
            Assert.That(session.Industry.Resources.Funds,
                Is.EqualTo(fundsAfterApril +
                           (session.LastIndustryReport.After.Funds -
                            session.LastIndustryReport.Before.Funds) -
                           session.LastMarketReport.FoodBillFunds),
                "only the next month's own industry and food values are applied");
        }

        [Test]
        public void SaveLoad_ResumesAnInFlightAction()
        {
            GameSession source = NewSession();
            PersonId person = M2FixedScenario.HarunobuId;
            Assert.That(source.TryPlanTrainingAction(person, 3, out string reason), Is.True, reason);
            Assert.That(source.AdvanceOneDay(), Is.True);
            Assert.That(source.TryGetDailyAction(person, out DailyActionTask before), Is.True);
            Assert.That(before.RemainingDays, Is.EqualTo(2));

            string json = JsonUtility.ToJson(source.CaptureSave());
            SaveData saved = JsonUtility.FromJson<SaveData>(json);
            GameSession restored = NewSession();

            Assert.That(restored.TryApplySave(saved).Succeeded, Is.True,
                restored.LastEventMessage);
            Assert.That(restored.WorldClock.CurrentDate, Is.EqualTo(new GameDate(1560, 4, 2)));
            Assert.That(restored.TryGetDailyAction(person, out DailyActionTask resumed), Is.True);
            Assert.That(resumed.Kind, Is.EqualTo(DailyActionKind.Training));
            Assert.That(resumed.ElapsedDays, Is.EqualTo(1));
            Assert.That(resumed.RemainingDays, Is.EqualTo(2));

            Assert.That(restored.AdvanceOneDay(), Is.True);
            Assert.That(restored.TryGetDailyAction(person, out resumed), Is.True);
            Assert.That(resumed.RemainingDays, Is.EqualTo(1));
            Assert.That(restored.AdvanceOneDay(), Is.True);
            Assert.That(restored.HasActiveDailyAction(person), Is.False);
            Assert.That(restored.LastDailyActionOutcomes[0].Date,
                Is.EqualTo(new GameDate(1560, 4, 4)));
        }

        [Test]
        public void SaveLoad_ResumesInTransitTravelWithoutAnEndpointLocation()
        {
            GameSession source = NewSession();
            PersonId person = M2FixedScenario.HarunobuId;
            Assert.That(source.TryPlanTravelAction(person, LocationId.Nagoya, out string reason),
                Is.True, reason);

            SaveData saved = JsonUtility.FromJson<SaveData>(
                JsonUtility.ToJson(source.CaptureSave()));
            GameSession restored = NewSession();
            Assert.That(restored.TryApplySave(saved).Succeeded, Is.True,
                restored.LastEventMessage);
            Assert.That(restored.IsPersonInTransit(person), Is.True);
            Assert.That(restored.TryGetPersonLocation(person, out _), Is.False);

            Assert.That(restored.AdvanceOneDay(), Is.True);
            Assert.That(restored.TryGetPersonLocation(person, out LocationId arrived), Is.True);
            Assert.That(arrived, Is.EqualTo(LocationId.Nagoya));
            Assert.That(restored.CurrentLocation, Is.EqualTo(LocationId.Nagoya));
        }

        [Test]
        public void V9Save_MigratesToEmptyDailyTimelineWithoutReplayingDays()
        {
            GameSession session = NewSession();
            SaveData legacy = session.CaptureSave();
            legacy.saveFormatVersion = 9;
            legacy.rulesVersion = 5;
            legacy.dailyActions = null;

            Assert.That(SaveDataMigrator.TryMigrate(legacy, null, out SaveData current,
                out SaveOperationResult result), Is.True, result.Message);
            Assert.That(current.saveFormatVersion, Is.EqualTo(SaveDataValidator.CurrentVersion));
            Assert.That(current.rulesVersion, Is.EqualTo(SaveData.CurrentRulesVersion));
            Assert.That(current.dailyActions, Is.Not.Null);
            Assert.That(current.dailyActions.persons.Length,
                Is.EqualTo(legacy.succession.persons.Length));
            foreach (DailyActionPersonSaveEntry person in current.dailyActions.persons)
            {
                Assert.That(person.hasAction, Is.False);
                Assert.That(person.hasLocation, Is.True);
                Assert.That(person.locationId, Is.EqualTo(legacy.locationId));
            }
        }

        [Test]
        public void SaveValidation_RejectsTravelWhoseDurationBreaksWorldMapRules()
        {
            GameSession source = NewSession();
            PersonId person = M2FixedScenario.HarunobuId;
            Assert.That(source.TryPlanTravelAction(person, LocationId.Atsuta, out string reason),
                Is.True, reason);
            SaveData saved = source.CaptureSave();

            bool changed = false;
            foreach (DailyActionPersonSaveEntry entry in saved.dailyActions.persons)
            {
                if (entry.personId != person.Value)
                    continue;
                entry.actionDurationDays = 2;
                changed = true;
            }
            Assert.That(changed, Is.True);
            Assert.That(SaveDataValidator.Validate(saved).Succeeded, Is.False);
        }
    }
}
