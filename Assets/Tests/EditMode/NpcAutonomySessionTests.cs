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
                "the M02 outcome list keeps its player-issued contract");
            Assert.That(session.LastDailyActionMessage, Does.Contain(masatsuna.DisplayName),
                "the readable M02 message still explains the autonomous completion");
            Assert.That(session.LastDailyActionMessage, Does.Contain("训练行动按计划完成"));
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

        [Test]
        public void V10CapturedJsonWithoutAutonomy_PassesPresenceAndMigratesWithoutDailyActionLoss()
        {
            GameSession session = NewSession();
            Assert.That(session.AdvanceOneDay(), Is.True);
            SaveData captured = session.CaptureSave();
            string dailyActionsJson = JsonUtility.ToJson(captured.dailyActions);
            string v11Json = JsonUtility.ToJson(captured);
            string v10Json = WithVersion(RemoveRootProperty(v11Json, "npcAutonomy"), 10);

            Assert.That(v10Json, Does.Not.Contain("\"npcAutonomy\":"));
            SaveDataPresenceProbe probe =
                JsonUtility.FromJson<SaveDataPresenceProbe>(v10Json);
            Assert.That(probe.saveFormatVersion, Is.EqualTo(10));
            SaveOperationResult presence = SaveDataValidator.ValidatePresence(probe, v10Json);
            Assert.That(presence.Succeeded, Is.True, presence.Message);

            SaveData legacy = JsonUtility.FromJson<SaveData>(v10Json);
            Assert.That(SaveDataMigrator.TryMigrate(legacy, v10Json, out SaveData current,
                out SaveOperationResult result), Is.True, result.Message);

            Assert.That(current.saveFormatVersion,
                Is.EqualTo(SaveDataValidator.CurrentVersion));
            Assert.That(JsonUtility.ToJson(current.dailyActions), Is.EqualTo(dailyActionsJson),
                "v10 raw migration must preserve the complete dailyActions snapshot");
            Assert.That(current.npcAutonomy.persons.Length, Is.EqualTo(2));
            Assert.That(Entry(current, M2FixedScenario.MasatsunaId).rest, Is.EqualTo(40));
            Assert.That(Entry(current, M2FixedScenario.MasatsunaId).learning, Is.EqualTo(75));
            Assert.That(Entry(current, M2FixedScenario.MasatsunaId).duty, Is.EqualTo(45));
            Assert.That(Entry(current, M2FixedScenario.NobumasaId).rest, Is.EqualTo(45));
            Assert.That(Entry(current, M2FixedScenario.NobumasaId).learning, Is.EqualTo(40));
            Assert.That(Entry(current, M2FixedScenario.NobumasaId).duty, Is.EqualTo(75));

            GameSession restored = NewSession(17UL);
            Assert.That(restored.TryApplySave(current).Succeeded, Is.True,
                restored.LastEventMessage);
            Assert.That(JsonUtility.ToJson(restored.CaptureSave().dailyActions),
                Is.EqualTo(dailyActionsJson),
                "the migrated action finishes exactly once after a real load");
        }

        [Test]
        public void V10JsonWithV11OnlyAutonomyRoot_IsRejectedByPresenceAndMigrator()
        {
            GameSession session = NewSession();
            string v10WithAutonomy = WithVersion(
                JsonUtility.ToJson(session.CaptureSave()), 10);
            Assert.That(v10WithAutonomy, Does.Contain("\"npcAutonomy\":"));

            SaveOperationResult presence = SaveDataValidator.ValidatePresence(
                JsonUtility.FromJson<SaveDataPresenceProbe>(v10WithAutonomy),
                v10WithAutonomy);
            Assert.That(presence.Succeeded, Is.False);
            Assert.That(presence.Message, Does.Contain("npcAutonomy"));

            SaveData legacy = JsonUtility.FromJson<SaveData>(v10WithAutonomy);
            Assert.That(SaveDataMigrator.TryMigrate(legacy, v10WithAutonomy,
                out _, out SaveOperationResult result), Is.False);
            Assert.That(result.Message, Does.Contain("npcAutonomy"));
        }

        [Test]
        public void DecisionLog_RecordsAcceptedInterruptedAndReplanOutcomesWithReasons()
        {
            GameSession session = NewSession();
            NpcAutonomySnapshot initial = Snapshot(session, M2FixedScenario.MasatsunaId);
            Assert.That(initial.Status, Is.EqualTo(NpcAutonomyStatus.Executing));
            Assert.That(initial.RecentDecisions[0].Outcome,
                Is.EqualTo(NpcAutonomyCommandOutcomeKind.Accepted));
            Assert.That(initial.RecentDecisions[0].Trigger,
                Is.EqualTo(NpcAutonomyTrigger.Initialization));
            Assert.That(initial.RecentDecisions[0].Reason, Does.Contain("分数"));

            const string reason = "玩家改为优先训练。";
            Assert.That(session.TryInterruptDailyAction(M2FixedScenario.MasatsunaId, reason),
                Is.True, session.LastDailyActionMessage);

            NpcAutonomySnapshot interrupted = Snapshot(session, M2FixedScenario.MasatsunaId);
            NpcAutonomyDecisionRecord interruption = interrupted.RecentDecisions
                .First(record => record.Outcome == NpcAutonomyCommandOutcomeKind.Interrupted);
            Assert.That(interruption.Trigger,
                Is.EqualTo(NpcAutonomyTrigger.ActionInterrupted));
            Assert.That(interruption.Reason, Does.Contain(reason));
            Assert.That(interruption.HasAction, Is.True);
            Assert.That(interruption.StatusName, Is.EqualTo("被中断"));

            NpcAutonomyDecisionRecord replan = interrupted.RecentDecisions.First(record =>
                record.Outcome == NpcAutonomyCommandOutcomeKind.Accepted &&
                record.Trigger == NpcAutonomyTrigger.ActionInterrupted);
            Assert.That(replan.Reason, Does.Contain("分数"));
            Assert.That(replan.StatusName, Is.EqualTo("执行中"));

            Assert.That(session.LastDailyActionOutcomes, Has.Count.EqualTo(2));
            Assert.That(session.LastDailyActionOutcomes[0].Outcome,
                Is.EqualTo(DailyActionOutcomeKind.Interrupted));
            Assert.That(session.LastDailyActionOutcomes[0].Reason, Does.Contain(reason));
            Assert.That(session.LastDailyActionOutcomes[1].Outcome,
                Is.EqualTo(DailyActionOutcomeKind.Planned));
            Assert.That(session.LastDailyActionMessage, Does.Contain(reason));
            Assert.That(session.LastDailyActionMessage, Does.Contain("训练行动已安排"));
        }

        [Test]
        public void PlayerReplan_KeepsInterruptedAndPlannedFeedbackVisible()
        {
            GameSession session = NewSession();

            Assert.That(session.TryReplanDailyAction(M2FixedScenario.MasatsunaId,
                DailyActionKind.Rest, 3, default, out string reason), Is.True, reason);

            Assert.That(session.LastDailyActionOutcomes, Has.Count.EqualTo(2));
            Assert.That(session.LastDailyActionOutcomes[0].Outcome,
                Is.EqualTo(DailyActionOutcomeKind.Interrupted));
            Assert.That(session.LastDailyActionOutcomes[1].Outcome,
                Is.EqualTo(DailyActionOutcomeKind.Planned));
            Assert.That(session.LastDailyActionOutcomes[1].Kind,
                Is.EqualTo(DailyActionKind.Rest));
            Assert.That(session.LastDailyActionMessage, Does.Contain("已重新规划"));
            Assert.That(session.LastDailyActionMessage, Does.Contain("休息行动已安排"));

            NpcAutonomySnapshot snapshot = Snapshot(session, M2FixedScenario.MasatsunaId);
            Assert.That(snapshot.HasActiveAction, Is.True);
            Assert.That(snapshot.ActiveAction.Kind, Is.EqualTo(DailyActionKind.Rest));
            Assert.That(snapshot.Status, Is.EqualTo(NpcAutonomyStatus.Executing));
            Assert.That(snapshot.RecentDecisions.Select(record => record.Outcome),
                Does.Contain(NpcAutonomyCommandOutcomeKind.Interrupted));
        }

        [Test]
        public void DeferredDecision_ExplainsPendingEventAndLeavesStateUnchanged()
        {
            GameSession session = NewSession();
            Assert.That(session.TryAdvanceToNextMonth(), Is.True, session.LastEventMessage);
            Assert.That(session.PendingEventId, Is.Not.Empty);

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
            NpcAutonomySnapshot before = Snapshot(restored, M2FixedScenario.MasatsunaId);
            NpcNeedValues needsBefore = before.Needs;
            string nobumasaBefore = JsonUtility.ToJson(
                ActionEntry(restored.CaptureSave(), M2FixedScenario.NobumasaId));

            Assert.That(restored.TryInterruptDailyAction(M2FixedScenario.MasatsunaId,
                "事件期间测试中断。"), Is.True);

            NpcAutonomySnapshot after = Snapshot(restored, M2FixedScenario.MasatsunaId);
            NpcAutonomyDecisionRecord deferred = after.RecentDecisions[0];
            Assert.That(deferred.Outcome,
                Is.EqualTo(NpcAutonomyCommandOutcomeKind.Deferred));
            Assert.That(deferred.Trigger, Is.EqualTo(NpcAutonomyTrigger.ActionInterrupted));
            Assert.That(deferred.Reason, Does.Contain("待回应"));
            Assert.That(deferred.StatusName, Is.EqualTo("空闲"));
            Assert.That(after.Status, Is.EqualTo(NpcAutonomyStatus.Idle));
            Assert.That(after.HasActiveAction, Is.False,
                "a deferred command must not invent an action");
            Assert.That(after.Needs, Is.EqualTo(needsBefore),
                "a deferred command must not mutate needs");
            string nobumasaAfter = JsonUtility.ToJson(
                ActionEntry(restored.CaptureSave(), M2FixedScenario.NobumasaId));
            Assert.That(nobumasaAfter, Is.EqualTo(nobumasaBefore),
                "a deferred command must not disturb another pilot");
        }

        [Test]
        public void RejectedDecision_ExplainsUnknownLocationAndOnlyGrowsNeeds()
        {
            GameSession source = NewSession();
            SaveData saved = source.CaptureSave();
            DailyActionPersonSaveEntry masatsuna =
                ActionEntry(saved, M2FixedScenario.MasatsunaId);
            masatsuna.hasLocation = false;
            masatsuna.locationId = string.Empty;
            masatsuna.hasAction = false;
            masatsuna.actionKind = 0;
            masatsuna.actionDurationDays = 0;
            masatsuna.actionElapsedDays = 0;
            masatsuna.actionStartYear = 0;
            masatsuna.actionStartMonth = 0;
            masatsuna.actionStartDay = 0;
            masatsuna.travelOriginId = string.Empty;
            masatsuna.travelDestinationId = string.Empty;
            masatsuna.actionReason = string.Empty;
            Assert.That(SaveDataValidator.Validate(saved).Succeeded, Is.True);

            GameSession restored = NewSession(11UL);
            Assert.That(restored.TryApplySave(saved).Succeeded, Is.True,
                restored.LastEventMessage);
            NpcAutonomySnapshot before = Snapshot(restored, M2FixedScenario.MasatsunaId);
            Assert.That(before.HasActiveAction, Is.False);
            Assert.That(restored.TryGetDailyAction(M2FixedScenario.NobumasaId,
                out DailyActionTask nobumasaBefore), Is.True);
            DailyActionPersonSaveEntry actionBefore =
                ActionEntry(restored.CaptureSave(), M2FixedScenario.MasatsunaId);

            Assert.That(restored.AdvanceOneDay(), Is.True);

            NpcAutonomySnapshot after = Snapshot(restored, M2FixedScenario.MasatsunaId);
            Assert.That(after.LatestEvaluation.HasSelection, Is.False);
            Assert.That(after.LatestEvaluation.Summary, Does.Contain("地点未知"));
            Assert.That(after.Status, Is.EqualTo(NpcAutonomyStatus.Idle));
            NpcAutonomyDecisionRecord rejected = after.RecentDecisions[0];
            Assert.That(rejected.Outcome,
                Is.EqualTo(NpcAutonomyCommandOutcomeKind.Rejected));
            Assert.That(rejected.Trigger, Is.EqualTo(NpcAutonomyTrigger.DailyCheck));
            Assert.That(rejected.Reason, Does.Contain("地点未知"));
            Assert.That(rejected.StatusName, Is.EqualTo("空闲"));
            Assert.That(after.Needs, Is.EqualTo(new NpcNeedValues(44, 78, 48)),
                "a rejected command must only receive the normal daily growth");
            DailyActionPersonSaveEntry actionAfter =
                ActionEntry(restored.CaptureSave(), M2FixedScenario.MasatsunaId);
            Assert.That(actionAfter.hasAction, Is.False);
            Assert.That(actionAfter.hasLocation, Is.False);
            Assert.That(actionAfter.locationId, Is.EqualTo(actionBefore.locationId));
            Assert.That(restored.TryGetDailyAction(M2FixedScenario.NobumasaId,
                out DailyActionTask nobumasaAfter), Is.True);
            Assert.That(nobumasaAfter.Kind, Is.EqualTo(nobumasaBefore.Kind));
            Assert.That(nobumasaAfter.RemainingDays,
                Is.EqualTo(nobumasaBefore.RemainingDays - 1),
                "a rejected command must not disturb another pilot's action");
        }

        private static string WithVersion(string json, int version) =>
            json.Replace("\"saveFormatVersion\":11",
                "\"saveFormatVersion\":" + version.ToString());

        /// <summary>
        /// Removes the last declared root property (npcAutonomy is last in SaveData)
        /// from compact JsonUtility output while keeping the JSON well formed.
        /// </summary>
        private static string RemoveRootProperty(string json, string propertyName)
        {
            Assert.That(json, Does.EndWith("}"));
            string token = "\"" + propertyName + "\":";
            int index = json.LastIndexOf(token, StringComparison.Ordinal);
            Assert.That(index, Is.GreaterThan(0), "missing root property " + propertyName);
            int comma = index - 1;
            while (comma >= 0 && char.IsWhiteSpace(json[comma]))
                comma--;
            Assert.That(comma, Is.GreaterThan(0));
            Assert.That(json[comma], Is.EqualTo(','),
                propertyName + " must be the last root property");
            return json.Substring(0, comma) + "}";
        }
    }
}
