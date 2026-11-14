using System;
using System.Collections.Generic;
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
                    learning = first.learning, duty = first.duty,
                    memories = new NpcActionMemorySaveEntry[0]
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
            // A historical v10 payload predates the v14 commission block entirely.
            string v10Json = WithVersion(
                RemoveRootProperty(RemoveRootProperty(RemoveRootProperty(
                    RemoveRootProperty(RemoveRootProperty(RemoveRootProperty(
                        RemoveRootProperty(v11Json,
                        "npcAutonomy"), "commission"),
                    "familyStanding"), "injury"), "missionHistory"), "campaign"), "market"), 10);

            Assert.That(v10Json, Does.Not.Contain("\"npcAutonomy\":"));
            Assert.That(v10Json, Does.Not.Contain("\"commission\":"),
                "a historical v10 fixture must not carry the v14 commission block");
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
        public void CompletedAction_RecordsCompletedMemoryForThatPilotOnly()
        {
            GameSession session = NewSession();
            Assert.That(Snapshot(session, M2FixedScenario.MasatsunaId).Memories, Is.Empty);
            Assert.That(Snapshot(session, M2FixedScenario.NobumasaId).Memories, Is.Empty);

            Assert.That(session.AdvanceOneDay(), Is.True);
            Assert.That(session.AdvanceOneDay(), Is.True);

            NpcAutonomySnapshot masatsuna = Snapshot(session, M2FixedScenario.MasatsunaId);
            Assert.That(masatsuna.Memories, Has.Count.EqualTo(1));
            Assert.That(masatsuna.Memories[0].Kind,
                Is.EqualTo(NpcActionMemoryKind.Completed));
            Assert.That(masatsuna.Memories[0].Action, Is.EqualTo(DailyActionKind.Training));
            Assert.That(masatsuna.Memories[0].Date, Is.EqualTo(new GameDate(1560, 4, 3)));
            Assert.That(masatsuna.Memories[0].PersonId,
                Is.EqualTo(M2FixedScenario.MasatsunaId));
            Assert.That(masatsuna.Memories[0].Reason, Is.Empty);

            NpcAutonomySnapshot nobumasa = Snapshot(session, M2FixedScenario.NobumasaId);
            Assert.That(nobumasa.Memories, Is.Empty,
                "a completion must never write another pilot's memory");
            Assert.That(nobumasa.HasActiveAction, Is.True,
                "the other pilot's in-flight Work action is unaffected");
        }

        [Test]
        public void PlayerInterrupt_RecordsInterruptedMemoryForThatPilotOnly()
        {
            GameSession session = NewSession();
            const string reason = "玩家中止训练以改做其他事务。";

            Assert.That(session.TryInterruptDailyAction(M2FixedScenario.MasatsunaId, reason),
                Is.True, session.LastDailyActionMessage);

            NpcAutonomySnapshot masatsuna = Snapshot(session, M2FixedScenario.MasatsunaId);
            Assert.That(masatsuna.Memories, Has.Count.EqualTo(1));
            Assert.That(masatsuna.Memories[0].Kind,
                Is.EqualTo(NpcActionMemoryKind.Interrupted));
            Assert.That(masatsuna.Memories[0].Action, Is.EqualTo(DailyActionKind.Training));
            Assert.That(masatsuna.Memories[0].Date, Is.EqualTo(new GameDate(1560, 4, 1)));
            Assert.That(masatsuna.Memories[0].Reason, Does.Contain(reason));

            Assert.That(Snapshot(session, M2FixedScenario.NobumasaId).Memories, Is.Empty,
                "interrupting one pilot must not touch the other pilot's memory");
        }

        [Test]
        public void PlayerPlannedCompletion_RecordsCompletedMemoryForThatPilotOnly()
        {
            GameSession session = NewSession();
            SaveData idle = IdlePilotSave(session);
            Assert.That(session.TryApplySave(idle).Succeeded, Is.True,
                session.LastEventMessage);
            Assert.That(Snapshot(session, M2FixedScenario.MasatsunaId).Memories, Is.Empty);
            Assert.That(Snapshot(session, M2FixedScenario.NobumasaId).Memories, Is.Empty);

            Assert.That(session.TryPlanDailyAction(M2FixedScenario.MasatsunaId,
                DailyActionKind.Rest, 1, default, out string reason), Is.True, reason);
            Assert.That(session.AdvanceOneDay(), Is.True);

            NpcAutonomySnapshot masatsuna = Snapshot(session, M2FixedScenario.MasatsunaId);
            Assert.That(masatsuna.Memories, Has.Count.EqualTo(1),
                "a player-arranged completion is recorded exactly once");
            Assert.That(masatsuna.Memories[0].Kind,
                Is.EqualTo(NpcActionMemoryKind.Completed));
            Assert.That(masatsuna.Memories[0].Action, Is.EqualTo(DailyActionKind.Rest));
            Assert.That(masatsuna.Memories[0].Date, Is.EqualTo(new GameDate(1560, 4, 2)));
            Assert.That(masatsuna.Memories[0].PersonId,
                Is.EqualTo(M2FixedScenario.MasatsunaId));
            Assert.That(masatsuna.Memories[0].Reason, Is.Empty);

            Assert.That(Snapshot(session, M2FixedScenario.NobumasaId).Memories, Is.Empty,
                "the other pilot's autonomous plan must not record this completion");
        }

        [Test]
        public void FailedReplan_RollsBackWithoutLeavingAnInterruptionMemory()
        {
            GameSession session = NewSession();
            Assert.That(session.TryGetDailyAction(M2FixedScenario.MasatsunaId,
                out DailyActionTask before), Is.True);
            Assert.That(Snapshot(session, M2FixedScenario.MasatsunaId).Memories, Is.Empty);

            Assert.That(session.TryReplanDailyAction(M2FixedScenario.MasatsunaId,
                DailyActionKind.Travel, 0, default, out string reason), Is.False);
            Assert.That(reason, Is.Not.Empty);

            NpcAutonomySnapshot after = Snapshot(session, M2FixedScenario.MasatsunaId);
            Assert.That(after.Memories, Is.Empty,
                "a rolled-back interruption must not leave a memory");
            Assert.That(after.ActiveAction, Is.Not.Null);
            Assert.That(after.ActiveAction.Kind, Is.EqualTo(before.Kind));
            Assert.That(after.ActiveAction.StartDate, Is.EqualTo(before.StartDate));
        }

        [Test]
        public void RejectedActionCommand_DoesNotRecordMemory()
        {
            GameSession session = NewSession();

            Assert.That(session.TryPlanDailyAction(M2FixedScenario.NobumasaId,
                DailyActionKind.Rest, 1, default, out _), Is.False,
                "the pilot already has an active action");

            Assert.That(Snapshot(session, M2FixedScenario.NobumasaId).Memories, Is.Empty);
            Assert.That(Snapshot(session, M2FixedScenario.MasatsunaId).Memories, Is.Empty);
        }

        [Test]
        public void RolledBackMonthlySettlement_DoesNotLeaveACompletionMemory()
        {
            GameSession source = NewSession();
            SaveData saved = IdlePilotSave(source);
            saved.year = 1560;
            saved.month = 4;
            saved.day = 30;
            DailyActionPersonSaveEntry row = ActionEntry(saved, M2FixedScenario.MasatsunaId);
            row.hasAction = true;
            row.actionKind = (int)DailyActionKind.Training;
            row.actionDurationDays = 1;
            row.actionElapsedDays = 0;
            row.actionStartYear = saved.year;
            row.actionStartMonth = saved.month;
            row.actionStartDay = saved.day;
            row.actionReason = "月末完成的训练。";
            Assert.That(SaveDataValidator.Validate(saved).Succeeded, Is.True);

            GameSession restored = NewSession(31UL);
            Assert.That(restored.TryApplySave(saved).Succeeded, Is.True,
                restored.LastEventMessage);
            Assert.That(Snapshot(restored, M2FixedScenario.MasatsunaId).Memories, Is.Empty);

            restored.MonthSettlementFaultInjection = () =>
                throw new InvalidOperationException("月度结算故障注入。");
            Assert.That(restored.TryAdvanceToNextMonth(), Is.False);

            NpcAutonomySnapshot after = Snapshot(restored, M2FixedScenario.MasatsunaId);
            Assert.That(after.Memories, Is.Empty,
                "a completion inside a rolled-back monthly settlement must not persist");
            Assert.That(after.HasActiveAction, Is.True,
                "the pre-settlement training action is restored");
        }

        [Test]
        public void DepartedPilotCleanup_DoesNotTransferInterruptionMemoryToTheSurvivor()
        {
            GameSession session = NewSession();
            Assert.That(session.AdvanceOneDay(), Is.True);
            Assert.That(session.Succession.World.TryGetLife(M2FixedScenario.NobumasaId,
                out SuccessionPersonLife life), Is.True);
            Assert.That(session.Succession.World.TrySetLife(M2FixedScenario.NobumasaId,
                new SuccessionPersonLife(life.BirthYear, false, life.Retired),
                out string lifeError), Is.True, lifeError);

            SaveData saved = session.CaptureSave();
            SaveOperationResult validation = SaveDataValidator.Validate(saved);
            Assert.That(validation.Succeeded, Is.True, validation.Message);
            Assert.That(saved.npcAutonomy.persons, Has.Length.EqualTo(1),
                "the departed pilot's autonomy row is removed from a stable save point");
            Assert.That(saved.npcAutonomy.persons[0].personId,
                Is.EqualTo(M2FixedScenario.MasatsunaId.Value));
            Assert.That(saved.npcAutonomy.persons[0].memories, Is.Empty,
                "cleanup interruption must never be written onto the surviving pilot");

            GameSession restored = NewSession(43UL);
            Assert.That(restored.TryApplySave(saved).Succeeded, Is.True,
                restored.LastEventMessage);
            Assert.That(restored.TryGetNpcAutonomy(M2FixedScenario.NobumasaId, out _),
                Is.False);
        }

        [Test]
        public void LongSession_KeepsAtMostOneMemoryPerKind()
        {
            GameSession session = NewSession();
            Assert.That(session.AdvanceDays(14), Is.True, session.LastEventMessage);

            foreach (NpcAutonomySnapshot snapshot in session.NpcAutonomySnapshots)
            {
                Assert.That(snapshot.Memories.Count, Is.LessThanOrEqualTo(2));
                Assert.That(snapshot.Memories.Count(memory =>
                        memory.Kind == NpcActionMemoryKind.Completed),
                    Is.LessThanOrEqualTo(1));
                Assert.That(snapshot.Memories.Count(memory =>
                        memory.Kind == NpcActionMemoryKind.Interrupted),
                    Is.LessThanOrEqualTo(1));
            }
        }

        [Test]
        public void SaveLoad_RestoresBothMemoryKindsWithOwnerDateActionAndReason()
        {
            GameSession source = NewSession();
            Assert.That(source.AdvanceOneDay(), Is.True);
            Assert.That(source.AdvanceOneDay(), Is.True);
            Assert.That(source.TryInterruptDailyAction(M2FixedScenario.MasatsunaId,
                "保存前中断。"), Is.True, source.LastDailyActionMessage);

            NpcAutonomySnapshot before = Snapshot(source, M2FixedScenario.MasatsunaId);
            Assert.That(before.Memories, Has.Count.EqualTo(2));
            SaveData saved = source.CaptureSave();
            NpcAutonomyPersonSaveEntry savedEntry = Entry(saved, M2FixedScenario.MasatsunaId);
            Assert.That(savedEntry.memories, Has.Length.EqualTo(2));

            GameSession restored = NewSession(37UL);
            Assert.That(restored.TryApplySave(saved).Succeeded, Is.True,
                restored.LastEventMessage);

            NpcAutonomySnapshot after = Snapshot(restored, M2FixedScenario.MasatsunaId);
            Assert.That(after.Memories, Has.Count.EqualTo(2));
            for (int i = 0; i < before.Memories.Count; i++)
                Assert.That(after.Memories[i], Is.EqualTo(before.Memories[i]),
                    "memory owner, kind, action, date and reason must round-trip exactly");
            Assert.That(after.Memories[0].Kind, Is.EqualTo(NpcActionMemoryKind.Completed));
            Assert.That(after.Memories[1].Kind, Is.EqualTo(NpcActionMemoryKind.Interrupted));
            Assert.That(after.Memories[1].Reason, Does.Contain("保存前中断。"));
            Assert.That(Snapshot(restored, M2FixedScenario.NobumasaId).Memories, Is.Empty);
        }

        [Test]
        public void V11Migration_AddsEmptyMemoriesAndPreservesNeedsAndDailyActions()
        {
            GameSession session = NewSession();
            Assert.That(session.AdvanceOneDay(), Is.True);
            SaveData legacy = session.CaptureSave();
            string dailyActionsJson = JsonUtility.ToJson(legacy.dailyActions);
            legacy.saveFormatVersion = 11;
            legacy.rulesVersion = 7;
            foreach (NpcAutonomyPersonSaveEntry entry in legacy.npcAutonomy.persons)
                entry.memories = null;

            Assert.That(SaveDataMigrator.TryMigrate(legacy, null, out SaveData current,
                out SaveOperationResult result), Is.True, result.Message);

            Assert.That(current.saveFormatVersion,
                Is.EqualTo(SaveDataValidator.CurrentVersion));
            Assert.That(current.npcAutonomy.persons, Has.Length.EqualTo(2));
            foreach (NpcAutonomyPersonSaveEntry entry in current.npcAutonomy.persons)
                Assert.That(entry.memories, Is.Empty,
                    "v11 migration must create empty memory lists, never infer history");
            Assert.That(Entry(current, M2FixedScenario.MasatsunaId).rest, Is.EqualTo(44));
            Assert.That(Entry(current, M2FixedScenario.MasatsunaId).learning, Is.EqualTo(78));
            Assert.That(Entry(current, M2FixedScenario.MasatsunaId).duty, Is.EqualTo(48));
            Assert.That(JsonUtility.ToJson(current.dailyActions), Is.EqualTo(dailyActionsJson));
        }

        [Test]
        public void V11Migration_RejectsAPayloadThatAlreadyCarriesMemories()
        {
            GameSession session = NewSession();
            Assert.That(session.AdvanceOneDay(), Is.True);
            Assert.That(session.AdvanceOneDay(), Is.True);
            SaveData legacy = session.CaptureSave();
            Assert.That(Entry(legacy, M2FixedScenario.MasatsunaId).memories, Is.Not.Empty);
            legacy.saveFormatVersion = 11;
            legacy.rulesVersion = 7;

            Assert.That(SaveDataMigrator.TryMigrate(legacy, null, out SaveData current,
                out SaveOperationResult result), Is.False);
            Assert.That(result.Message, Does.Contain("memories"));
            Assert.That(current, Is.Null);
        }

        [Test]
        public void V12Validation_RejectsMissingOrInvalidMemoryData()
        {
            GameSession session = NewSession();
            SaveData valid = session.CaptureSave();
            Assert.That(SaveDataValidator.Validate(valid).Succeeded, Is.True);

            SaveData missing = session.CaptureSave();
            Entry(missing, M2FixedScenario.MasatsunaId).memories = null;
            SaveOperationResult missingResult = SaveDataValidator.Validate(missing);
            Assert.That(missingResult.Succeeded, Is.False);
            Assert.That(missingResult.Message, Does.Contain("memories"));

            SaveData wrongOwner = session.CaptureSave();
            Entry(wrongOwner, M2FixedScenario.MasatsunaId).memories =
                new[] { Memory(M2FixedScenario.NobumasaId, NpcActionMemoryKind.Completed,
                    DailyActionKind.Rest, valid.year, valid.month, valid.day, string.Empty) };
            Assert.That(SaveDataValidator.Validate(wrongOwner).Succeeded, Is.False,
                "nested memory owner must match its pilot row");

            SaveData future = session.CaptureSave();
            Assert.That(GameDate.TryAddDays(
                new GameDate(valid.year, valid.month, valid.day), 1,
                out GameDate tomorrow));
            Entry(future, M2FixedScenario.MasatsunaId).memories =
                new[] { Memory(M2FixedScenario.MasatsunaId, NpcActionMemoryKind.Completed,
                    DailyActionKind.Rest, tomorrow.Year, tomorrow.Month, tomorrow.Day,
                    string.Empty) };
            SaveOperationResult futureResult = SaveDataValidator.Validate(future);
            Assert.That(futureResult.Succeeded, Is.False);
            Assert.That(futureResult.Message, Does.Contain("晚于当前日期"));

            SaveData badKind = session.CaptureSave();
            NpcActionMemorySaveEntry bad = Memory(M2FixedScenario.MasatsunaId,
                NpcActionMemoryKind.Completed, DailyActionKind.Rest,
                valid.year, valid.month, valid.day, string.Empty);
            bad.kind = 9;
            Entry(badKind, M2FixedScenario.MasatsunaId).memories = new[] { bad };
            Assert.That(SaveDataValidator.Validate(badKind).Succeeded, Is.False);

            SaveData badAction = session.CaptureSave();
            NpcActionMemorySaveEntry badActionMemory = Memory(M2FixedScenario.MasatsunaId,
                NpcActionMemoryKind.Completed, DailyActionKind.Rest,
                valid.year, valid.month, valid.day, string.Empty);
            badActionMemory.action = 99;
            Entry(badAction, M2FixedScenario.MasatsunaId).memories =
                new[] { badActionMemory };
            Assert.That(SaveDataValidator.Validate(badAction).Succeeded, Is.False);

            SaveData badDate = session.CaptureSave();
            Entry(badDate, M2FixedScenario.MasatsunaId).memories =
                new[] { Memory(M2FixedScenario.MasatsunaId,
                    NpcActionMemoryKind.Completed, DailyActionKind.Rest,
                    1560, 2, 30, string.Empty) };
            SaveOperationResult badDateResult = SaveDataValidator.Validate(badDate);
            Assert.That(badDateResult.Succeeded, Is.False);
            Assert.That(badDateResult.Message, Does.Contain("日期无效"));

            SaveData tooMany = session.CaptureSave();
            Entry(tooMany, M2FixedScenario.MasatsunaId).memories = new[]
            {
                Memory(M2FixedScenario.MasatsunaId, NpcActionMemoryKind.Completed,
                    DailyActionKind.Rest, valid.year, valid.month, valid.day, string.Empty),
                Memory(M2FixedScenario.MasatsunaId, NpcActionMemoryKind.Interrupted,
                    DailyActionKind.Work, valid.year, valid.month, valid.day, "中断。"),
                Memory(M2FixedScenario.MasatsunaId, NpcActionMemoryKind.Completed,
                    DailyActionKind.Training, valid.year, valid.month, valid.day, string.Empty)
            };
            SaveOperationResult tooManyResult = SaveDataValidator.Validate(tooMany);
            Assert.That(tooManyResult.Succeeded, Is.False);
            Assert.That(tooManyResult.Message, Does.Contain("最多两条"));

            SaveData duplicate = session.CaptureSave();
            Entry(duplicate, M2FixedScenario.MasatsunaId).memories = new[]
            {
                Memory(M2FixedScenario.MasatsunaId, NpcActionMemoryKind.Completed,
                    DailyActionKind.Rest, valid.year, valid.month, valid.day, string.Empty),
                Memory(M2FixedScenario.MasatsunaId, NpcActionMemoryKind.Completed,
                    DailyActionKind.Work, valid.year, valid.month, valid.day, string.Empty)
            };
            SaveOperationResult duplicateResult = SaveDataValidator.Validate(duplicate);
            Assert.That(duplicateResult.Succeeded, Is.False);
            Assert.That(duplicateResult.Message, Does.Contain("重复"));

            SaveData unstable = session.CaptureSave();
            Entry(unstable, M2FixedScenario.MasatsunaId).memories = new[]
            {
                Memory(M2FixedScenario.MasatsunaId, NpcActionMemoryKind.Interrupted,
                    DailyActionKind.Work, valid.year, valid.month, valid.day, "中断。"),
                Memory(M2FixedScenario.MasatsunaId, NpcActionMemoryKind.Completed,
                    DailyActionKind.Rest, valid.year, valid.month, valid.day, string.Empty)
            };
            SaveOperationResult unstableResult = SaveDataValidator.Validate(unstable);
            Assert.That(unstableResult.Succeeded, Is.False);
            Assert.That(unstableResult.Message, Does.Contain("顺序"));
        }

        [TestCase("kind", ",\"kind\":0")]
        [TestCase("action", ",\"action\":0")]
        [TestCase("reason", ",\"reason\":\"\"")]
        public void V12Json_RejectsMissingMemoryField(string field, string fragment)
        {
            GameSession session = NewSession();
            SaveData data = session.CaptureSave();
            var memory = Memory(M2FixedScenario.MasatsunaId, NpcActionMemoryKind.Completed,
                DailyActionKind.Rest, data.year, data.month, data.day, string.Empty);
            Entry(data, M2FixedScenario.MasatsunaId).memories = new[] { memory };
            Assert.That(SaveDataValidator.Validate(data).Succeeded, Is.True);
            string memoryJson = JsonUtility.ToJson(memory);
            Assert.That(memoryJson, Does.Contain(fragment));
            string json = JsonUtility.ToJson(data).Replace(memoryJson,
                memoryJson.Replace(fragment, string.Empty));

            SaveOperationResult result = SaveDataValidator.Validate(
                JsonUtility.FromJson<SaveData>(json));

            Assert.That(result.Succeeded, Is.False,
                "missing " + field + " must not be replaced with a valid default");
        }

        [Test]
        public void V12Presence_RejectsMissingNestedMemoryArray()
        {
            GameSession session = NewSession();
            string json = JsonUtility.ToJson(session.CaptureSave(), true);
            SaveDataPresenceProbe probe = JsonUtility.FromJson<SaveDataPresenceProbe>(json);
            probe.npcAutonomy.persons[0].memories = null;

            SaveOperationResult presence = SaveDataValidator.ValidatePresence(probe, json);

            Assert.That(presence.Succeeded, Is.False);
            Assert.That(presence.Message, Does.Contain("memories"));
        }

        private static NpcActionMemorySaveEntry Memory(PersonId personId,
            NpcActionMemoryKind kind, DailyActionKind action, int year, int month, int day,
            string reason) =>
            new NpcActionMemorySaveEntry
            {
                personId = personId.Value,
                kind = (int)kind,
                action = (int)action,
                year = year,
                month = month,
                day = day,
                reason = reason
            };

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

        [Test]
        public void AdvanceDays_MatchesEquivalentOneDayAdvances()
        {
            GameSession bulk = NewSession();
            GameSession daily = NewSession();

            Assert.That(bulk.AdvanceDays(5), Is.True, bulk.LastEventMessage);
            for (int day = 0; day < 5; day++)
                Assert.That(daily.AdvanceOneDay(), Is.True, daily.LastEventMessage);

            Assert.That(bulk.WorldClock.CurrentDate, Is.EqualTo(daily.WorldClock.CurrentDate));
            Assert.That(AutonomySignature(bulk, M2FixedScenario.MasatsunaId),
                Is.EqualTo(AutonomySignature(daily, M2FixedScenario.MasatsunaId)));
            Assert.That(AutonomySignature(bulk, M2FixedScenario.NobumasaId),
                Is.EqualTo(AutonomySignature(daily, M2FixedScenario.NobumasaId)));
        }

        [Test]
        public void MonthlySettlement_MatchesEquivalentDailyActionDays()
        {
            GameSession monthly = NewSession();
            GameSession daily = NewSession();

            Assert.That(monthly.TryAdvanceToNextMonth(), Is.True, monthly.LastEventMessage);
            int guard = 0;
            while (daily.WorldClock.CurrentDate < monthly.WorldClock.CurrentDate)
            {
                Assert.That(++guard, Is.LessThan(40), "monthly equivalence loop did not converge");
                Assert.That(daily.AdvanceActionDay(), Is.True, daily.LastEventMessage);
            }

            Assert.That(daily.WorldClock.CurrentDate, Is.EqualTo(monthly.WorldClock.CurrentDate));
            Assert.That(AutonomySignature(monthly, M2FixedScenario.MasatsunaId),
                Is.EqualTo(AutonomySignature(daily, M2FixedScenario.MasatsunaId)));
            Assert.That(AutonomySignature(monthly, M2FixedScenario.NobumasaId),
                Is.EqualTo(AutonomySignature(daily, M2FixedScenario.NobumasaId)));
        }

        [Test]
        public void MonthlyCompletionWithPendingEvent_RecordsOneBlockedReplanDecision()
        {
            GameSession source = NewSession();
            SaveData saved = IdlePilotSave(source);
            saved.year = 1560;
            saved.month = 4;
            saved.day = 30;
            DailyActionPersonSaveEntry row = ActionEntry(saved, M2FixedScenario.MasatsunaId);
            row.hasAction = true;
            row.actionKind = (int)DailyActionKind.Training;
            row.actionDurationDays = 1;
            row.actionElapsedDays = 0;
            row.actionStartYear = saved.year;
            row.actionStartMonth = saved.month;
            row.actionStartDay = saved.day;
            row.actionReason = "月末完成的训练。";
            Assert.That(SaveDataValidator.Validate(saved).Succeeded, Is.True);

            GameSession restored = NewSession(19UL);
            Assert.That(restored.TryApplySave(saved).Succeeded, Is.True,
                restored.LastEventMessage);
            Assert.That(restored.TryAdvanceToNextMonth(), Is.True, restored.LastEventMessage);
            Assert.That(restored.PendingEventId, Is.Not.Empty,
                "the May settlement must leave the pending event used by this regression");

            GameDate settlementDate = new GameDate(1560, 5, 1);
            NpcAutonomyDecisionRecord[] blocked = restored.NpcAutonomyDecisions
                .Where(record => record.PersonId == M2FixedScenario.MasatsunaId &&
                                 record.Date == settlementDate &&
                                 (record.Outcome == NpcAutonomyCommandOutcomeKind.Deferred ||
                                  record.Outcome == NpcAutonomyCommandOutcomeKind.Rejected))
                .ToArray();
            Assert.That(blocked.Length, Is.EqualTo(1),
                "completion and daily-check passes must not duplicate one blocked replan");
            Assert.That(blocked[0].Trigger, Is.EqualTo(NpcAutonomyTrigger.ActionCompleted));

            NpcAutonomySnapshot snapshot = Snapshot(restored, M2FixedScenario.MasatsunaId);
            Assert.That(snapshot.HasLastOutcome, Is.True);
            Assert.That(snapshot.LastOutcome.Status, Is.EqualTo(NpcAutonomyStatus.Completed));
            Assert.That(snapshot.LastOutcome.Action, Is.EqualTo(DailyActionKind.Training));
        }

        [Test]
        public void ConfirmTravel_TriggersEligibilityReplanAndAdvancesNeeds()
        {
            GameSession source = NewSession();
            SaveData idle = IdlePilotSave(source);
            Assert.That(SaveDataValidator.Validate(idle).Succeeded, Is.True);

            GameSession restored = NewSession(23UL);
            Assert.That(restored.TryApplySave(idle).Succeeded, Is.True,
                restored.LastEventMessage);
            NpcNeedValues before = Snapshot(restored, M2FixedScenario.MasatsunaId).Needs;

            TravelPreview? preview = restored.PreviewTravel(LocationId.Atsuta);
            Assert.That(preview.HasValue, Is.True, restored.LastTravelBlockReason);
            Assert.That(restored.ConfirmTravel(preview.Value), Is.True,
                restored.LastTravelBlockReason);

            Assert.That(restored.CurrentLocation, Is.EqualTo(LocationId.Atsuta));
            NpcAutonomySnapshot after = Snapshot(restored, M2FixedScenario.MasatsunaId);
            Assert.That(after.Needs, Is.EqualTo(before.WithDailyGrowth()),
                "direct travel must still advance the simulated day's needs");
            Assert.That(after.HasActiveAction, Is.True,
                "idle pilots must be replanned after the travel eligibility change");
            Assert.That(after.RecentDecisions.Any(record =>
                    record.Trigger == NpcAutonomyTrigger.EligibilityChanged),
                Is.True, "the travel replan must carry the eligibility trigger");
        }

        [Test]
        public void SuccessionEligibilityChange_PlansIdlePilotsWithEligibilityTrigger()
        {
            GameSession gates = NewSession();
            Assert.That(gates.TryAdvanceToNextMonth(), Is.True, gates.LastEventMessage);
            ChooseM2(gates, M2FixedScenario.PledgeEventId, M2FixedScenario.KeepPromiseOptionId);
            Assert.That(gates.TryAdvanceToNextMonth(), Is.True, gates.LastEventMessage);
            ChooseM2(gates, SampleEventIds.EventId, SampleEventIds.OfferRestOptionId);

            SaveData idle = IdlePilotSave(gates);
            GameSession restored = NewSession(29UL);
            Assert.That(restored.TryApplySave(idle).Succeeded, Is.True,
                restored.LastEventMessage);

            SuccessionResult result = restored.BeginSuccessionRetirement();
            Assert.That(result.Succeeded, Is.True, result.Message);

            NpcAutonomySnapshot masatsuna = Snapshot(restored, M2FixedScenario.MasatsunaId);
            Assert.That(masatsuna.RecentDecisions.Any(record =>
                    record.Trigger == NpcAutonomyTrigger.EligibilityChanged),
                Is.True, "roster/succession changes must run the eligibility pass");
            Assert.That(masatsuna.RecentDecisions[0].Trigger,
                Is.EqualTo(NpcAutonomyTrigger.EligibilityChanged));
            Assert.That(masatsuna.RecentDecisions[0].Outcome,
                Is.EqualTo(NpcAutonomyCommandOutcomeKind.Accepted)
                    .Or.EqualTo(NpcAutonomyCommandOutcomeKind.Deferred));
        }

        [Test]
        public void UnregisteredPilot_EligibilityChangeRecordsRejectedDecision()
        {
            GameSession session = NewSession();
            session.InitResearchDemoScenario();

            SuccessionResult result = session.BeginSuccessionRetirement();
            Assert.That(result.Succeeded, Is.True, result.Message);

            NpcAutonomyDecisionRecord[] rejected = session.NpcAutonomyDecisions
                .Where(record => record.PersonId == M2FixedScenario.MasatsunaId &&
                                 record.Outcome == NpcAutonomyCommandOutcomeKind.Rejected &&
                                 record.Trigger == NpcAutonomyTrigger.EligibilityChanged)
                .ToArray();
            Assert.That(rejected.Length, Is.EqualTo(1),
                "the unregistered-pilot rejection path must be reachable and logged once");
            Assert.That(rejected[0].Reason, Does.Contain("未登记"));
        }

        [Test]
        public void InspectorReads_ArePureAndLeaveDurableStateUntouched()
        {
            GameSession session = NewSession();
            string saveBefore = JsonUtility.ToJson(session.CaptureSave());
            int decisionsBefore = session.NpcAutonomyDecisions.Count;

            IReadOnlyList<NpcAutonomySnapshot> snapshots = session.NpcAutonomySnapshots;
            Assert.That(snapshots.Count, Is.EqualTo(2));
            Assert.That(session.TryGetNpcAutonomy(M2FixedScenario.MasatsunaId, out _), Is.True);
            Assert.That(session.TryGetNpcAutonomy(M2FixedScenario.NobumasaId, out _), Is.True);
            Assert.That(session.NpcAutonomyDecisions.Count, Is.EqualTo(decisionsBefore));

            string saveAfter = JsonUtility.ToJson(session.CaptureSave());
            Assert.That(saveAfter, Is.EqualTo(saveBefore),
                "read-only Inspector getters must not schedule, reconcile or grow needs");
        }

        [Test]
        public void V11RulesVersionMismatch_IsRejectedNotNormalized()
        {
            GameSession session = NewSession();
            SaveData saved = session.CaptureSave();
            saved.rulesVersion = SaveData.CurrentRulesVersion - 1;

            Assert.That(SaveDataValidator.Validate(saved).Succeeded, Is.False);
            Assert.That(SaveDataMigrator.TryMigrate(saved, out _, out SaveOperationResult result),
                Is.False);
            Assert.That(result.Error, Is.EqualTo(SaveErrorCode.UnsupportedVersion));
        }

        [Test]
        public void V10RulesVersion_IsNormalizedByIntendedMigration()
        {
            GameSession session = NewSession();
            SaveData legacy = session.CaptureSave();
            legacy.saveFormatVersion = 10;
            legacy.rulesVersion = 6;
            legacy.npcAutonomy = null;

            Assert.That(SaveDataMigrator.TryMigrate(legacy, null, out SaveData current,
                out SaveOperationResult result), Is.True, result.Message);
            Assert.That(current.saveFormatVersion, Is.EqualTo(SaveDataValidator.CurrentVersion));
            Assert.That(current.rulesVersion, Is.EqualTo(SaveData.CurrentRulesVersion));
            Assert.That(current.npcAutonomy.persons.Length, Is.EqualTo(2));
        }

        private static void ChooseM2(GameSession session, string eventId, string optionId)
        {
            Assert.That(session.PendingEventId, Is.EqualTo(eventId), session.LastEventMessage);
            EventChoiceResult result = session.TryChooseEventOption(eventId, optionId,
                session.CreateEventResultId(eventId, optionId));
            Assert.That(result.Succeeded, Is.True, result.Message);
        }

        private static string WithVersion(string json, int version) =>
            json.Replace("\"saveFormatVersion\":" + SaveDataValidator.CurrentVersion,
                "\"saveFormatVersion\":" + version.ToString());

        private static SaveData IdlePilotSave(GameSession session)
        {
            SaveData saved = session.CaptureSave();
            foreach (DailyActionPersonSaveEntry row in saved.dailyActions.persons)
            {
                if (row.personId != M2FixedScenario.MasatsunaId.Value &&
                    row.personId != M2FixedScenario.NobumasaId.Value)
                    continue;
                ClearAction(row);
            }
            return saved;
        }

        private static void ClearAction(DailyActionPersonSaveEntry row)
        {
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
        }

        private static string AutonomySignature(GameSession session, PersonId personId)
        {
            NpcAutonomySnapshot snapshot = Snapshot(session, personId);
            DailyActionTask task = snapshot.ActiveAction;
            string action = task == null
                ? "none"
                : task.Kind + "/" + task.DurationDays + "/" + task.ElapsedDays + "/" +
                  task.StartDate;
            return snapshot.Needs + "|" + snapshot.Status + "|" + action + "|" +
                   string.Join(";", snapshot.RecentDecisions.Select(record =>
                       record.Date + "/" + record.Outcome + "/" + record.Action + "/" +
                       record.HasAction + "/" + record.Trigger + "/" + record.Reason));
        }

        /// <summary>
        /// Removes the last declared root property (npcAutonomy is last in SaveData)
        /// from compact JsonUtility output while keeping the JSON well formed.
        /// </summary>
        private static string RemoveRootProperty(string json, string propertyName)
        {
            int depth = 0;
            int index = 0;
            while (index < json.Length)
            {
                char current = json[index];
                if (current == '"')
                {
                    int keyStart = index;
                    int contentStart = ++index;
                    bool escaped = false;
                    while (index < json.Length)
                    {
                        char character = json[index];
                        if (escaped)
                            escaped = false;
                        else if (character == '\\')
                            escaped = true;
                        else if (character == '"')
                            break;
                        index++;
                    }
                    if (index >= json.Length)
                        break;

                    int contentLength = index - contentStart;
                    index++;
                    if (depth == 1 && contentLength == propertyName.Length &&
                        string.CompareOrdinal(json, contentStart, propertyName, 0,
                            contentLength) == 0)
                    {
                        int lookahead = index;
                        while (lookahead < json.Length && char.IsWhiteSpace(json[lookahead]))
                            lookahead++;
                        if (lookahead < json.Length && json[lookahead] == ':')
                        {
                            int valueStart = lookahead + 1;
                            while (valueStart < json.Length && char.IsWhiteSpace(json[valueStart]))
                                valueStart++;
                            int valueEnd = SkipJsonValue(json, valueStart);
                            int removeStart = keyStart;
                            int before = keyStart - 1;
                            while (before >= 0 && char.IsWhiteSpace(json[before]))
                                before--;
                            if (before >= 0 && json[before] == ',')
                            {
                                removeStart = before;
                            }
                            else
                            {
                                int after = valueEnd;
                                while (after < json.Length && char.IsWhiteSpace(json[after]))
                                    after++;
                                if (after < json.Length && json[after] == ',')
                                    valueEnd = after + 1;
                            }

                            return json.Substring(0, removeStart) + json.Substring(valueEnd);
                        }
                    }
                    continue;
                }

                if (current == '{' || current == '[')
                    depth++;
                else if (current == '}' || current == ']')
                    depth--;
                index++;
            }

            throw new InvalidOperationException("Root property was not found: " + propertyName);
        }

        private static int SkipJsonValue(string json, int index)
        {
            while (index < json.Length && char.IsWhiteSpace(json[index]))
                index++;
            if (index >= json.Length)
                return index;

            if (json[index] == '"')
            {
                index++;
                bool escaped = false;
                while (index < json.Length)
                {
                    char character = json[index];
                    if (escaped)
                        escaped = false;
                    else if (character == '\\')
                        escaped = true;
                    else if (character == '"')
                        return index + 1;
                    index++;
                }
                return index;
            }

            if (json[index] == '{' || json[index] == '[')
            {
                int depth = 0;
                while (index < json.Length)
                {
                    char character = json[index];
                    if (character == '"')
                    {
                        index = SkipJsonString(json, index);
                        continue;
                    }
                    if (character == '{' || character == '[')
                        depth++;
                    else if (character == '}' || character == ']')
                    {
                        depth--;
                        if (depth == 0)
                            return index + 1;
                    }
                    index++;
                }
                return index;
            }

            while (index < json.Length && json[index] != ',' && json[index] != '}' &&
                   json[index] != ']')
                index++;
            return index;
        }

        private static int SkipJsonString(string json, int index)
        {
            index++;
            bool escaped = false;
            while (index < json.Length)
            {
                char character = json[index];
                if (escaped)
                    escaped = false;
                else if (character == '\\')
                    escaped = true;
                else if (character == '"')
                    return index + 1;
                index++;
            }
            return index;
        }
    }
}
