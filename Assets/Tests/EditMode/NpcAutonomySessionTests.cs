using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;
using SengokuChronicle.Persistence;
using UnityEngine;

namespace SengokuChronicle.Tests.EditMode
{
    /// <summary>
    /// M03 session coverage: distinct initial plans, deterministic one-plan-per-completion,
    /// no replan while active, interruption and rejection paths, v11 persistence and v10
    /// migration that preserves every in-flight M02 action.
    /// </summary>
    public sealed class NpcAutonomySessionTests
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

        private static NpcAutonomySnapshot Snapshot(GameSession session, PersonId personId)
        {
            Assert.That(session.TryGetNpcAutonomy(personId, out NpcAutonomySnapshot snapshot),
                Is.True, "missing snapshot for " + personId);
            return snapshot;
        }

        private static NpcAutonomyPersonSaveEntry Entry(SaveData data, PersonId personId) =>
            data.npcAutonomy.persons.First(entry => entry.personId == personId.Value);

        private static DailyActionPersonSaveEntry ActionEntry(SaveData data, PersonId personId) =>
            data.dailyActions.persons.First(entry => entry.personId == personId.Value);

        [Test]
        public void NewGame_PlansDistinctApprovedActionsForBothPilots()
        {
            GameSession session = NewSession();

            NpcAutonomySnapshot masatsuna = Snapshot(session, M2FixedScenario.MasatsunaId);
            NpcAutonomySnapshot nobumasa = Snapshot(session, M2FixedScenario.NobumasaId);

            Assert.That(masatsuna.Needs, Is.EqualTo(new NpcNeedValues(40, 75, 45)));
            Assert.That(nobumasa.Needs, Is.EqualTo(new NpcNeedValues(45, 40, 75)));
            Assert.That(masatsuna.HasActiveAction, Is.True);
            Assert.That(masatsuna.ActiveAction.Kind, Is.EqualTo(DailyActionKind.Training));
            Assert.That(masatsuna.ActiveAction.DurationDays, Is.EqualTo(2));
            Assert.That(nobumasa.HasActiveAction, Is.True);
            Assert.That(nobumasa.ActiveAction.Kind, Is.EqualTo(DailyActionKind.Work));
            Assert.That(nobumasa.ActiveAction.DurationDays, Is.EqualTo(3));
            Assert.That(masatsuna.Status, Is.EqualTo(NpcAutonomyStatus.Executing));
            Assert.That(session.NpcAutonomySnapshots.Count, Is.EqualTo(2));
            Assert.That(session.TryGetNpcAutonomy(M2FixedScenario.HarunobuId, out _), Is.False,
                "only the two approved pilots are tracked");

            Assert.That(masatsuna.RecentDecisions[0].Outcome,
                Is.EqualTo(NpcAutonomyCommandOutcomeKind.Accepted));
            Assert.That(masatsuna.RecentDecisions[0].Trigger,
                Is.EqualTo(NpcAutonomyTrigger.Initialization));
            Assert.That(nobumasa.RecentDecisions[0].Outcome,
                Is.EqualTo(NpcAutonomyCommandOutcomeKind.Accepted));
            Assert.That(nobumasa.RecentDecisions[0].Action, Is.EqualTo(DailyActionKind.Work));
        }

        [Test]
        public void OrdinaryDay_DoesNotReplanAnActivePilot()
        {
            GameSession session = NewSession();
            NpcAutonomySnapshot before = Snapshot(session, M2FixedScenario.MasatsunaId);
            int decisionCount = before.RecentDecisions.Count;

            Assert.That(session.AdvanceOneDay(), Is.True);

            NpcAutonomySnapshot after = Snapshot(session, M2FixedScenario.MasatsunaId);
            Assert.That(after.Needs, Is.EqualTo(new NpcNeedValues(44, 78, 48)));
            Assert.That(after.ActiveAction, Is.Not.Null);
            Assert.That(after.ActiveAction.Kind, Is.EqualTo(before.ActiveAction.Kind));
            Assert.That(after.ActiveAction.StartDate, Is.EqualTo(before.ActiveAction.StartDate));
            Assert.That(after.ActiveAction.RemainingDays, Is.EqualTo(1));
            Assert.That(after.RecentDecisions.Count, Is.EqualTo(decisionCount),
                "an ordinary day while active must not append a replan decision");
        }

        [Test]
        public void CompletedAction_ReplansExactlyOnceAndReducesTheMatchingNeed()
        {
            GameSession session = NewSession();

            Assert.That(session.AdvanceOneDay(), Is.True);
            Assert.That(session.AdvanceOneDay(), Is.True);

            NpcAutonomySnapshot masatsuna = Snapshot(session, M2FixedScenario.MasatsunaId);
            Assert.That(masatsuna.Needs, Is.EqualTo(new NpcNeedValues(48, 41, 51)),
                "two days of growth then one Training reduction");
            Assert.That(masatsuna.HasActiveAction, Is.True);
            Assert.That(masatsuna.ActiveAction.Kind, Is.EqualTo(DailyActionKind.Training),
                "the completed Training need is still the highest score");
            Assert.That(masatsuna.RecentDecisions[0].Outcome,
                Is.EqualTo(NpcAutonomyCommandOutcomeKind.Accepted));
            Assert.That(masatsuna.RecentDecisions[0].Trigger,
                Is.EqualTo(NpcAutonomyTrigger.ActionCompleted));
            Assert.That(masatsuna.RecentDecisions.Count, Is.EqualTo(2),
                "initial Admission + one completion replan, never a duplicate");
            Assert.That(masatsuna.RecentDecisions[0].Trigger,
                Is.EqualTo(NpcAutonomyTrigger.ActionCompleted));
            Assert.That(session.LastDailyActionOutcomes, Is.Empty,
                "the M02 completion channel excludes autonomous pilot actions");
        }

        [Test]
        public void PlayerInterrupt_ReplansImmediatelyAndLogsTheInterruption()
        {
            GameSession session = NewSession();

            Assert.That(session.TryInterruptDailyAction(M2FixedScenario.MasatsunaId,
                "玩家改为优先训练。"), Is.True, session.LastDailyActionMessage);

            NpcAutonomySnapshot snapshot = Snapshot(session, M2FixedScenario.MasatsunaId);
            Assert.That(snapshot.HasActiveAction, Is.True,
                "the AI must re-evaluate and schedule after an interruption");
            Assert.That(snapshot.ActiveAction.Kind, Is.EqualTo(DailyActionKind.Training));
            Assert.That(snapshot.RecentDecisions.Select(record => record.Outcome),
                Does.Contain(NpcAutonomyCommandOutcomeKind.Interrupted));
            Assert.That(snapshot.RecentDecisions.Select(record => record.Outcome),
                Does.Contain(NpcAutonomyCommandOutcomeKind.Accepted));
            Assert.That(snapshot.RecentDecisions[0].Trigger,
                Is.EqualTo(NpcAutonomyTrigger.ActionInterrupted));
        }

        [Test]
        public void PlayerReplannedAction_IsNotOverwrittenByTheNextDay()
        {
            GameSession session = NewSession();
            Assert.That(session.TryReplanDailyAction(M2FixedScenario.MasatsunaId,
                DailyActionKind.Rest, 5, default, out string reason), Is.True, reason);

            NpcAutonomySnapshot afterReplan = Snapshot(session, M2FixedScenario.MasatsunaId);
            Assert.That(afterReplan.ActiveAction.Kind, Is.EqualTo(DailyActionKind.Rest));
            Assert.That(afterReplan.ActiveAction.RemainingDays, Is.EqualTo(5));
            int decisionCount = afterReplan.RecentDecisions.Count;

            Assert.That(session.AdvanceOneDay(), Is.True);

            NpcAutonomySnapshot afterDay = Snapshot(session, M2FixedScenario.MasatsunaId);
            Assert.That(afterDay.ActiveAction.Kind, Is.EqualTo(DailyActionKind.Rest),
                "an ordinary day must not replace the player's action");
            Assert.That(afterDay.ActiveAction.RemainingDays, Is.EqualTo(4));
            Assert.That(afterDay.RecentDecisions.Count, Is.EqualTo(decisionCount));
        }

        [Test]
        public void PendingEvent_DefersInterruptionReplanWithoutAnAction()
        {
            GameSession session = NewSession();
            Assert.That(session.TryAdvanceToNextMonth(), Is.True, session.LastEventMessage);
            Assert.That(session.PendingEventId, Is.Not.Empty,
                "the April settlement must leave a pending event for this path");

            SaveData pending = session.CaptureSave();
            DailyActionPersonSaveEntry row = ActionEntry(pending, M2FixedScenario.MasatsunaId);
            row.hasLocation = true;
            row.locationId = LocationId.Kiyosu.Value;
            row.hasAction = true;
            row.actionKind = (int)DailyActionKind.Training;
            row.actionDurationDays = 2;
            row.actionElapsedDays = 0;
            row.actionStartYear = pending.year;
            row.actionStartMonth = pending.month;
            row.actionStartDay = pending.day;
            row.travelOriginId = string.Empty;
            row.travelDestinationId = string.Empty;
            row.actionReason = "待处理事件期间的人工行动。";
            Assert.That(SaveDataValidator.Validate(pending).Succeeded, Is.True);

            GameSession restored = NewSession(7UL);
            Assert.That(restored.TryApplySave(pending).Succeeded, Is.True,
                restored.LastEventMessage);
            Assert.That(restored.PendingEventId, Is.Not.Empty);
            Assert.That(restored.HasActiveDailyAction(M2FixedScenario.MasatsunaId), Is.True);

            Assert.That(restored.TryInterruptDailyAction(M2FixedScenario.MasatsunaId,
                "事件期间测试中断。"), Is.True);
            NpcAutonomySnapshot snapshot = Snapshot(restored, M2FixedScenario.MasatsunaId);
            Assert.That(snapshot.HasActiveAction, Is.False,
                "a pending event is a short-term block, not a new plan");
            Assert.That(snapshot.RecentDecisions.Select(record => record.Outcome),
                Does.Contain(NpcAutonomyCommandOutcomeKind.Interrupted));
            Assert.That(snapshot.RecentDecisions.Select(record => record.Outcome),
                Does.Contain(NpcAutonomyCommandOutcomeKind.Deferred),
                "the AI reports a deferred command while the event is pending");
        }

        [Test]
        public void UnknownLocation_RejectsPlanningAndKeepsOtherActionsUnchanged()
        {
            GameSession source = NewSession();
            SaveData saved = source.CaptureSave();
            DailyActionPersonSaveEntry row = ActionEntry(saved, M2FixedScenario.MasatsunaId);
            row.hasLocation = false;
            row.locationId = string.Empty;
            row.hasAction = false;
            row.actionKind = 0;
            row.actionDurationDays = 0;
            row.actionElapsedDays = 0;
            row.actionStartYear = 0;
            row.actionStartMonth = 0;
            row.actionStartDay = 0;
            row.travelOriginId = string.Empty;
            row.travelDestinationId = string.Empty;
            row.actionReason = string.Empty;
            Assert.That(SaveDataValidator.Validate(saved).Succeeded, Is.True);

            GameSession restored = NewSession(11UL);
            Assert.That(restored.TryApplySave(saved).Succeeded, Is.True,
                restored.LastEventMessage);
            Assert.That(restored.HasActiveDailyAction(M2FixedScenario.MasatsunaId), Is.False);
            NpcAutonomySnapshot rejected = Snapshot(restored, M2FixedScenario.MasatsunaId);
            Assert.That(rejected.LatestEvaluation.HasSelection, Is.False);

            Assert.That(restored.TryGetDailyAction(M2FixedScenario.NobumasaId,
                out DailyActionTask nobumasaBefore), Is.True);
            Assert.That(restored.AdvanceOneDay(), Is.True);
            rejected = Snapshot(restored, M2FixedScenario.MasatsunaId);
            Assert.That(restored.HasActiveDailyAction(M2FixedScenario.MasatsunaId), Is.False);
            Assert.That(rejected.RecentDecisions, Is.Not.Empty);
            Assert.That(rejected.RecentDecisions[0].Outcome,
                Is.EqualTo(NpcAutonomyCommandOutcomeKind.Rejected));
            Assert.That(restored.TryGetDailyAction(M2FixedScenario.NobumasaId,
                out DailyActionTask nobumasaAfter), Is.True);
            Assert.That(nobumasaAfter.Kind, Is.EqualTo(nobumasaBefore.Kind));
            Assert.That(nobumasaAfter.RemainingDays,
                Is.EqualTo(nobumasaBefore.RemainingDays - 1),
                "a rejected planning command must not disturb another pilot's action");
        }

        [Test]
        public void SaveLoad_RestoresNeedsAndInFlightActionsExactlyOnce()
        {
            GameSession source = NewSession();
            Assert.That(source.AdvanceOneDay(), Is.True);
            NpcAutonomySnapshot masatsunaBefore =
                Snapshot(source, M2FixedScenario.MasatsunaId);
            NpcAutonomySnapshot nobumasaBefore =
                Snapshot(source, M2FixedScenario.NobumasaId);

            SaveData saved = source.CaptureSave();
            GameSession restored = NewSession(13UL);
            Assert.That(restored.TryApplySave(saved).Succeeded, Is.True,
                restored.LastEventMessage);

            NpcAutonomySnapshot masatsunaAfter =
                Snapshot(restored, M2FixedScenario.MasatsunaId);
            NpcAutonomySnapshot nobumasaAfter =
                Snapshot(restored, M2FixedScenario.NobumasaId);
            Assert.That(masatsunaAfter.Needs, Is.EqualTo(masatsunaBefore.Needs));
            Assert.That(nobumasaAfter.Needs, Is.EqualTo(nobumasaBefore.Needs));
            Assert.That(masatsunaAfter.ActiveAction.Kind,
                Is.EqualTo(masatsunaBefore.ActiveAction.Kind));
            Assert.That(masatsunaAfter.ActiveAction.RemainingDays,
                Is.EqualTo(masatsunaBefore.ActiveAction.RemainingDays));
            Assert.That(nobumasaAfter.ActiveAction.RemainingDays,
                Is.EqualTo(nobumasaBefore.ActiveAction.RemainingDays));
            Assert.That(restored.NpcAutonomyDecisions, Is.Empty,
                "decision history is session-only and must not be loaded");

            Assert.That(restored.AdvanceOneDay(), Is.True);
            NpcAutonomySnapshot masatsunaNext =
                Snapshot(restored, M2FixedScenario.MasatsunaId);
            NpcAutonomySnapshot nobumasaNext =
                Snapshot(restored, M2FixedScenario.NobumasaId);
            Assert.That(nobumasaNext.ActiveAction.RemainingDays,
                Is.EqualTo(nobumasaBefore.ActiveAction.RemainingDays - 1),
                "the restored Work action advances once, not twice");
            Assert.That(masatsunaNext.HasActiveAction, Is.True,
                "the completed pilot is replanned after its action resolves");
        }

        [Test]
        public void V10Migration_InitializesPilotNeedsAndPreservesDailyActions()
        {
            GameSession session = NewSession();
            Assert.That(session.AdvanceOneDay(), Is.True);
            SaveData legacy = session.CaptureSave();
            string dailyActionsJson = JsonUtility.ToJson(legacy.dailyActions);
            legacy.saveFormatVersion = 10;
            legacy.rulesVersion = 6;
            legacy.npcAutonomy = null;

            Assert.That(SaveDataMigrator.TryMigrate(legacy, null, out SaveData current,
                out SaveOperationResult result), Is.True, result.Message);

            Assert.That(current.saveFormatVersion, Is.EqualTo(SaveDataValidator.CurrentVersion));
            Assert.That(current.rulesVersion, Is.EqualTo(SaveData.CurrentRulesVersion));
            Assert.That(current.dailyActions, Is.Not.Null);
            Assert.That(JsonUtility.ToJson(current.dailyActions), Is.EqualTo(dailyActionsJson),
                "v10 migration must preserve every logical daily-action field");
            Assert.That(current.npcAutonomy.persons.Length, Is.EqualTo(2));
            Assert.That(Entry(current, M2FixedScenario.MasatsunaId).rest, Is.EqualTo(40));
            Assert.That(Entry(current, M2FixedScenario.MasatsunaId).learning, Is.EqualTo(75));
            Assert.That(Entry(current, M2FixedScenario.MasatsunaId).duty, Is.EqualTo(45));
            Assert.That(Entry(current, M2FixedScenario.NobumasaId).rest, Is.EqualTo(45));
            Assert.That(Entry(current, M2FixedScenario.NobumasaId).learning, Is.EqualTo(40));
            Assert.That(Entry(current, M2FixedScenario.NobumasaId).duty, Is.EqualTo(75));
        }

        [Test]
        public void V11Validation_RejectsBadPilotRowsAndMissingCoverage()
        {
            GameSession session = NewSession();
            SaveData valid = session.CaptureSave();
            Assert.That(SaveDataValidator.Validate(valid).Succeeded, Is.True);

            SaveData outOfRange = session.CaptureSave();
            Entry(outOfRange, M2FixedScenario.MasatsunaId).rest = 101;
            Assert.That(SaveDataValidator.Validate(outOfRange).Succeeded, Is.False);

            SaveData unknown = session.CaptureSave();
            Entry(unknown, M2FixedScenario.MasatsunaId).personId = "person.unknown";
            Assert.That(SaveDataValidator.Validate(unknown).Succeeded, Is.False);

            SaveData duplicate = session.CaptureSave();
            NpcAutonomyPersonSaveEntry first = duplicate.npcAutonomy.persons[0];
            duplicate.npcAutonomy.persons = new[]
            {
                first,
                new NpcAutonomyPersonSaveEntry
                {
                    personId = first.personId, rest = first.rest,
                    learning = first.learning, duty = first.duty
                }
            };
            Assert.That(SaveDataValidator.Validate(duplicate).Succeeded, Is.False);

            SaveData missing = session.CaptureSave();
            missing.npcAutonomy.persons = new[] { missing.npcAutonomy.persons[0] };
            Assert.That(SaveDataValidator.Validate(missing).Succeeded, Is.False);
        }

        [Test]
        public void V11Presence_RejectsMissingAutonomyRootProperty()
        {
            GameSession session = NewSession();
            string json = JsonUtility.ToJson(session.CaptureSave(), true);
            string withoutAutonomy = json.Replace("\"npcAutonomy\":", "\"ignoredNpcAutonomy\":");

            SaveOperationResult presence = SaveDataValidator.ValidatePresence(
                JsonUtility.FromJson<SaveDataPresenceProbe>(withoutAutonomy), withoutAutonomy);

            Assert.That(presence.Succeeded, Is.False);
            Assert.That(presence.Message, Does.Contain("npcAutonomy"));
        }
    }
}
