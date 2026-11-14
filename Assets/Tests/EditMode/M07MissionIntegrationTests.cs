using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;
using SengokuChronicle.Persistence;
using Object = UnityEngine.Object;

namespace SengokuChronicle.Tests.EditMode
{
    /// <summary>
    /// M07 shared settlement coverage: approved formula and thresholds, the effect table,
    /// clamping, manual/delegated equality, idempotency, injury, succession on a head's
    /// death and save/load of the new blocks.
    /// </summary>
    public sealed class M07MissionIntegrationTests
    {
        // Frozen briefing inputs for the manual/delegated parity fixtures. The fixed
        // roster starts at zero Ninjutsu, so a score in the full or costly band is
        // mathematically unreachable without explicit values: with 40/41 and 5
        // intelligence the maximum no-prep score is 40 + 5 + 100 - 50 = 95.
        private const int FirstBriefingNinjutsu = 40;
        private const int SecondBriefingNinjutsu = 41;
        private const int BriefingIntelligence = 5;

        private GameSession _session;

        [SetUp]
        public void SetUp() => _session = GameSession.NewGame();

        [TearDown]
        public void TearDown()
        {
            if (_session != null)
                Object.DestroyImmediate(_session.gameObject);
        }

        [Test]
        public void Score_UsesApprovedFormulaAndThresholds()
        {
            Assert.That(TacticalMissionResolutionRules.Score(40, 41, 5, 10, 100),
                Is.EqualTo(105),
                "floor((40+41)/2)=40, +5 intelligence, +10 preparation, +100-50 = 105");
            Assert.That(TacticalMissionResolutionRules.Classify(90),
                Is.EqualTo(SharedMissionOutcome.FullSuccess));
            Assert.That(TacticalMissionResolutionRules.Classify(89),
                Is.EqualTo(SharedMissionOutcome.CostlySuccess));
            Assert.That(TacticalMissionResolutionRules.Classify(65),
                Is.EqualTo(SharedMissionOutcome.CostlySuccess));
            Assert.That(TacticalMissionResolutionRules.Classify(64),
                Is.EqualTo(SharedMissionOutcome.SafeFailure));
            Assert.That(TacticalMissionResolutionRules.Classify(15),
                Is.EqualTo(SharedMissionOutcome.SafeFailure));
            Assert.That(TacticalMissionResolutionRules.Classify(14),
                Is.EqualTo(SharedMissionOutcome.SevereFailure));
            Assert.That(TacticalMissionResolutionRules.Score(0, 0, 20, 0, 100),
                Is.EqualTo(60), "intelligence is clamped at +10");
        }

        [Test]
        public void ManualMapping_DistinguishesAlertExtractionWithdrawalAndCapture()
        {
            Assert.That(TacticalMissionResolutionRules.TryMapManual(
                TacticalMissionOutcome.Succeeded, false, out SharedMissionOutcome clean,
                out _), Is.True);
            Assert.That(clean, Is.EqualTo(SharedMissionOutcome.FullSuccess));
            Assert.That(TacticalMissionResolutionRules.TryMapManual(
                TacticalMissionOutcome.Succeeded, true, out SharedMissionOutcome alerted,
                out _), Is.True);
            Assert.That(alerted, Is.EqualTo(SharedMissionOutcome.CostlySuccess));
            Assert.That(TacticalMissionResolutionRules.TryMapManual(
                TacticalMissionOutcome.Withdrawn, true, out SharedMissionOutcome withdrawn,
                out _), Is.True);
            Assert.That(withdrawn, Is.EqualTo(SharedMissionOutcome.SafeFailure));
            Assert.That(TacticalMissionResolutionRules.TryMapManual(
                TacticalMissionOutcome.Failed, true, out SharedMissionOutcome captured,
                out _), Is.True);
            Assert.That(captured, Is.EqualTo(SharedMissionOutcome.SevereFailure));
        }

        [Test]
        public void EffectTable_RejectsUnsupportedMissionResults()
        {
            Assert.That(MissionOutcomeTable.TryGetEffects(MissionKind.ShadowEscort,
                SharedMissionOutcome.FullSuccess, out MissionOutcomeEffect full, out _), Is.True);
            Assert.That(full.FameDelta, Is.EqualTo(3));
            Assert.That(full.RelationDelta, Is.EqualTo(5));
            Assert.That(full.ConcealmentDelta, Is.EqualTo(3));
            Assert.That(full.RelationId, Is.EqualTo(MissionRelationIds.ShadowEscortMerchant));
            Assert.That(MissionOutcomeTable.TryGetEffects(MissionKind.ShadowEscort,
                SharedMissionOutcome.SevereFailure, out _, out string m05Error), Is.False);
            Assert.That(m05Error, Does.Contain("M05"));
            Assert.That(MissionOutcomeTable.TryGetEffects(MissionKind.TacticalInfiltration,
                SharedMissionOutcome.PartialSuccess, out _, out string m06Error), Is.False);
            Assert.That(m06Error, Does.Contain("M06"));
            Assert.That(MissionOutcomeTable.TryGetEffects(MissionKind.TacticalInfiltration,
                SharedMissionOutcome.CostlySuccess, out MissionOutcomeEffect costly, out _), Is.True);
            Assert.That(costly.ExtraSupplyCost, Is.EqualTo(1));
            Assert.That(costly.FameDelta, Is.EqualTo(2));
            Assert.That(costly.RelationDelta, Is.EqualTo(2));
            Assert.That(costly.ConcealmentDelta, Is.EqualTo(1));
        }

        [Test]
        public void Standing_ClampsAndReportsActualBeforeAfter()
        {
            Assert.That(FamilyStandingState.TryCreate(99, 1, null,
                out FamilyStandingState state, out string createError), Is.True, createError);
            var deltas = new[] { new MissionRelationDelta("employer.test", -5) };
            Assert.That(state.TryApplyDeltas(5, -5, deltas,
                out MissionStandingResult result, out string applyError), Is.True, applyError);
            Assert.That(result.FameBefore, Is.EqualTo(99));
            Assert.That(result.FameAfter, Is.EqualTo(100), "fame clamps at 100");
            Assert.That(result.ConcealmentBefore, Is.EqualTo(1));
            Assert.That(result.ConcealmentAfter, Is.EqualTo(0), "concealment clamps at 0");
            Assert.That(result.RelationChanges[0].Before, Is.EqualTo(0));
            Assert.That(result.RelationChanges[0].After, Is.EqualTo(-5));
            Assert.That(state.GetRelation("missing.relation"), Is.Zero,
                "unrecorded relations default to 0");
            Assert.That(state.TryApplyDeltas(0, 0,
                new[] { new MissionRelationDelta("a", 1), new MissionRelationDelta("a", 2) },
                out _, out string duplicateError), Is.False);
            Assert.That(duplicateError, Does.Contain("重复"));
        }

        [Test]
        public void RelationDisplayName_MapsStableIdsToPlayerText()
        {
            Assert.That(GameSession.RelationDisplayName(MissionRelationIds.ShadowEscortMerchant),
                Is.EqualTo("尾张商队信用"));
            Assert.That(GameSession.RelationDisplayName(
                    MissionRelationIds.InterceptedDispatchTarget),
                Is.EqualTo("截获密令目标势力"));
            Assert.That(GameSession.RelationDisplayName("other.relation"),
                Is.EqualTo("未知关系"),
                "an unmapped ID must not leak into player text.");
            Assert.That(GameSession.RelationDisplayName(string.Empty),
                Is.EqualTo("未知关系"));
        }

        [Test]
        public void MissionRecord_RejectsUndisclosedDeath()
        {
            PersonId first = new PersonId("person.test.a");
            PersonId second = new PersonId("person.test.b");
            var states = new[]
            {
                new MissionParticipantState(first, true, false, false, false),
                new MissionParticipantState(second, true, false, true, true)
            };
            Assert.That(MissionRecord.TryCreate("run.test", "mission.test",
                MissionKind.TacticalInfiltration, MissionOperationMode.Delegated,
                new GameDate(1560, 5, 1), SharedMissionOutcome.SevereFailure, first,
                new[] { first, second }, new[] { second }, MissionPreparation.None,
                10, 10, 0, 50, 1, 0, 0, 0, 50, 50, null, 0, 0, 0, states,
                "chronicle.000001", out _, out string error), Is.False);
            Assert.That(error, Does.Contain("死亡"));

            Assert.That(MissionRecord.TryCreate("run.test", "mission.test",
                MissionKind.TacticalInfiltration, MissionOperationMode.Delegated,
                new GameDate(1560, 5, 1), SharedMissionOutcome.SevereFailure, first,
                new[] { first, second }, new[] { first, second }, MissionPreparation.None,
                10, 10, 0, 50, 1, 0, 0, 0, 50, 50, null, 0, 0, 0, states,
                "chronicle.000001", out MissionRecord record, out string accepted), Is.True,
                accepted);
            Assert.That(record.AnyParticipantDied, Is.True);
        }

        [Test]
        public void MissionHistory_RejectsDuplicateRunId()
        {
            var history = new MissionHistoryState();
            Assert.That(MissionRecord.TryCreate("run.dup", "mission.test",
                MissionKind.ShadowEscort, MissionOperationMode.Manual,
                new GameDate(1560, 5, 1), SharedMissionOutcome.SafeFailure,
                new PersonId("person.test.a"), new[] { new PersonId("person.test.a") },
                null, MissionPreparation.None, 0, 0, 0, 0, 1, 0, 0, 0, 50, 50, null,
                0, 0, 0,
                new[] { new MissionParticipantState(new PersonId("person.test.a"), true, false,
                    true, false) }, "chronicle.000001", out MissionRecord record,
                out string error), Is.True, error);
            Assert.That(history.TryAppend(record, out _), Is.True);
            Assert.That(history.TryAppend(record, out string duplicate), Is.False);
            Assert.That(duplicate, Does.Contain("重复"));
        }

        [Test]
        public void Request_RejectsMismatchedPreparationCostAndDuplicateParticipants()
        {
            PersonId first = new PersonId("person.test.a");
            PersonId second = new PersonId("person.test.b");
            Assert.That(MissionRequest.TryCreate("run.test", "mission.test",
                MissionKind.TacticalInfiltration, MissionOperationMode.Delegated,
                MissionPreparation.Scouting, new GameDate(1560, 5, 1), first,
                new[] { first, second }, new[] { first, second }, 10, 10, 0,
                TacticalMissionResolutionRules.Difficulty,
                TacticalMissionResolutionRules.BaseSupplyCost, 0, 55, 0,
                out _, out string costError), Is.False);
            Assert.That(costError, Does.Contain("准备成本"));
            PersonId[] participants = { first, first };
            Assert.That(MissionRequest.TryCreate("run.test", "mission.test",
                MissionKind.TacticalInfiltration, MissionOperationMode.Manual,
                MissionPreparation.None, new GameDate(1560, 5, 1), first,
                participants, new[] { first, first }, 10, 10, 0, 50, 2, 0, 0, 0,
                out _, out string duplicateError), Is.False);
            Assert.That(duplicateError, Does.Contain("重复"));
        }

        [Test]
        public void M05ManualSettlement_RequiresDueDateAndPaysExactlyOnce()
        {
            Assert.That(_session.TryAcceptCommission().Succeeded, Is.True);
            Assert.That(_session.TrySetCommissionMode(MissionOperationMode.Manual,
                out string manualError), Is.True, manualError);
            PersonId assignee = FirstEligibleCommissionAssignee();
            Assert.That(_session.TryAssignCommission(assignee).Succeeded, Is.True);
            Assert.That(_session.CommissionMode, Is.EqualTo(MissionOperationMode.Manual));

            Assert.That(_session.TrySettleCommissionManually().Succeeded, Is.False,
                "a commission before its due date cannot be settled manually.");
            for (int i = 0; i < ShadowEscortCommission.DurationDays; i++)
                Assert.That(_session.AdvanceOneDay(), Is.True);
            Assert.That(_session.IsCommissionDue, Is.True);
            Assert.That(_session.MissionHistory, Is.Empty,
                "manual play waits for the explicit settle action.");

            IndustryResources before = _session.Industry.Resources;
            MissionSettlementResult settlement = _session.TrySettleCommissionManually();
            Assert.That(settlement.Succeeded, Is.True, settlement.Message);
            Assert.That(_session.MissionHistory.Count, Is.EqualTo(1));
            MissionRecord record = _session.MissionHistory[0];
            Assert.That(record.Mode, Is.EqualTo(MissionOperationMode.Manual));
            Assert.That(record.RunId, Is.EqualTo(GameSession.CommissionRunId));
            Assert.That(record.ChronicleEntryId, Is.Not.Empty);
            Assert.That(_session.Industry.Resources.Funds,
                Is.EqualTo(before.Funds + record.FundsDelta));
            Assert.That(_session.Industry.Resources.Supplies,
                Is.EqualTo(before.Supplies + record.SuppliesDelta));
            Assert.That(_session.FamilyFame, Is.EqualTo(record.FameAfter));
            Assert.That(_session.Concealment, Is.EqualTo(record.ConcealmentAfter));

            IndustryResources settledResources = _session.Industry.Resources;
            int chronicleCount = _session.ChronicleEntries.Count;
            Assert.That(_session.TrySettleCommissionManually().Succeeded, Is.False,
                "a settled run cannot be settled twice.");
            Assert.That(_session.Industry.Resources, Is.EqualTo(settledResources));
            Assert.That(_session.ChronicleEntries.Count, Is.EqualTo(chronicleCount));
            Assert.That(_session.MissionHistory.Count, Is.EqualTo(1));
        }

        [Test]
        public void M05DelegatedSettlement_AutoSettlesOnceAtDueDate()
        {
            Assert.That(_session.TryAcceptCommission().Succeeded, Is.True);
            Assert.That(_session.TrySetCommissionMode(MissionOperationMode.Delegated,
                out string modeError), Is.True, modeError);
            PersonId assignee = FirstEligibleCommissionAssignee();
            Assert.That(_session.TryAssignCommission(assignee).Succeeded, Is.True);
            for (int i = 0; i < ShadowEscortCommission.DurationDays; i++)
                Assert.That(_session.AdvanceOneDay(), Is.True);

            Assert.That(_session.MissionHistory.Count, Is.EqualTo(1),
                "delegated play settles on the due day without a click.");
            MissionRecord record = _session.MissionHistory[0];
            Assert.That(record.Mode, Is.EqualTo(MissionOperationMode.Delegated));
            Assert.That(_session.TrySettleCommissionManually().Succeeded, Is.False,
                "the manual button is not a second entry for a delegated run.");

            Assert.That(_session.AdvanceOneDay(), Is.True);
            Assert.That(_session.MissionHistory.Count, Is.EqualTo(1),
                "a later day cannot repay or re-settle a finished run.");
        }

        [Test]
        public void M06Delegated_IsReproducibleForTheSameFrozenInputs()
        {
            using var second = new SessionScope(GameSession.NewGame());
            PersonId[] firstPair = TacticalPair(_session);
            PersonId[] secondPair = TacticalPair(second.Session);
            Assert.That(secondPair[0].Value, Is.EqualTo(firstPair[0].Value));
            Assert.That(secondPair[1].Value, Is.EqualTo(firstPair[1].Value));

            TacticalActionResult first = _session.TryDelegateTacticalMission(firstPair[0],
                firstPair[1], firstPair[0], MissionPreparation.None);
            TacticalActionResult same = second.Session.TryDelegateTacticalMission(secondPair[0],
                secondPair[1], secondPair[0], MissionPreparation.None);
            Assert.That(first.Succeeded, Is.True, first.Feedback);
            Assert.That(same.Succeeded, Is.True, same.Feedback);

            MissionRecord left = _session.MissionHistory[0];
            MissionRecord right = second.Session.MissionHistory[0];
            Assert.That(right.Score, Is.EqualTo(left.Score));
            Assert.That(right.Roll, Is.EqualTo(left.Roll));
            Assert.That(right.Outcome, Is.EqualTo(left.Outcome));
            Assert.That(second.Session.Industry.Resources, Is.EqualTo(_session.Industry.Resources));
            Assert.That(second.Session.FamilyFame, Is.EqualTo(_session.FamilyFame));
            Assert.That(second.Session.Concealment, Is.EqualTo(_session.Concealment));
            Assert.That(_session.IsTacticalMissionActive, Is.False,
                "delegated play resolves without a tactical board session.");
        }

        [Test]
        public void M06ManualWithdraw_UsesSharedSafeFailureAndCannotRepeat()
        {
            PersonId[] pair = TacticalPair(_session);
            TacticalActionResult start = _session.TryBeginTacticalMission(pair[0], pair[1],
                pair[0], MissionPreparation.None);
            Assert.That(start.Succeeded, Is.True, start.Feedback);
            Assert.That(_session.MissionHistory, Is.Empty,
                "starting a mission writes no terminal record.");

            IndustryResources before = _session.Industry.Resources;
            TacticalActionResult withdraw = _session.TryTacticalWithdraw();
            Assert.That(withdraw.Succeeded, Is.True, withdraw.Feedback);
            Assert.That(_session.MissionHistory.Count, Is.EqualTo(1));
            MissionRecord record = _session.MissionHistory[0];
            Assert.That(record.Outcome, Is.EqualTo(SharedMissionOutcome.SafeFailure));
            Assert.That(record.Mode, Is.EqualTo(MissionOperationMode.Manual));
            Assert.That(record.SuppliesDelta,
                Is.EqualTo(-TacticalMissionResolutionRules.BaseSupplyCost));
            Assert.That(_session.Industry.Resources.Supplies,
                Is.EqualTo(before.Supplies - TacticalMissionResolutionRules.BaseSupplyCost));
            Assert.That(_session.Concealment, Is.EqualTo(FamilyStandingState.InitialConcealment - 2));
            Assert.That(_session.TryTacticalWithdraw().Succeeded, Is.False);
            Assert.That(_session.MissionHistory.Count, Is.EqualTo(1));
        }

        /// <summary>
        /// M07 parity: the manual board result and the delegated auto-check settle the
        /// same frozen request through one contract. Withdrawn manual play and a seeded
        /// delegated safe failure must produce the same shared outcome, effects, life
        /// states and chronicle identity; only the resolution inputs, the mode field and
        /// the mode-specific chronicle wording are allowed to differ.
        /// </summary>
        [Test]
        public void M06ManualAndDelegated_SameFrozenInputs_ShareOutcomeAndChronicle()
        {
            using var delegated = new SessionScope(GameSession.NewGame());
            PersonId[] pair = TacticalPair(_session);
            PersonId[] delegatedPair = TacticalPair(delegated.Session);
            Assert.That(delegatedPair[0], Is.EqualTo(pair[0]));
            Assert.That(delegatedPair[1], Is.EqualTo(pair[1]));

            // Give both campaigns the same launch-time briefing snapshots so the frozen
            // numbers are real inputs instead of the fixed roster's zero values: two
            // Ninjutsu scores that make every band reachable, and the intelligence pool.
            SaveData manualSeed = WithFrozenTacticalBriefing(_session.CaptureSave(), pair[0],
                pair[1], FirstBriefingNinjutsu, SecondBriefingNinjutsu, BriefingIntelligence);
            Assert.That(_session.TryApplySave(manualSeed).Succeeded, Is.True);
            SaveData delegatedSeed = WithFrozenTacticalBriefing(delegated.Session.CaptureSave(),
                delegatedPair[0], delegatedPair[1], FirstBriefingNinjutsu, SecondBriefingNinjutsu,
                BriefingIntelligence);
            Assert.That(delegated.Session.TryApplySave(delegatedSeed).Succeeded, Is.True);

            TacticalActionResult manualStart = _session.TryBeginTacticalMission(pair[0], pair[1],
                pair[0], MissionPreparation.None);
            Assert.That(manualStart.Succeeded, Is.True, manualStart.Feedback);

            SaveData seeded = WithSeededMissionOutcome(delegated.Session.CaptureSave(),
                FirstBriefingNinjutsu, SecondBriefingNinjutsu, BriefingIntelligence,
                SharedMissionOutcome.SafeFailure);
            SaveOperationResult applied = delegated.Session.TryApplySave(seeded);
            Assert.That(applied.Succeeded, Is.True, applied.Message);

            TacticalActionResult delegatedStart =
                delegated.Session.TryDelegateTacticalMission(delegatedPair[0],
                    delegatedPair[1], delegatedPair[0], MissionPreparation.None);
            Assert.That(delegatedStart.Succeeded, Is.True, delegatedStart.Feedback);

            TacticalActionResult withdraw = _session.TryTacticalWithdraw();
            Assert.That(withdraw.Succeeded, Is.True, withdraw.Feedback);

            Assert.That(_session.MissionHistory.Count, Is.EqualTo(1));
            Assert.That(delegated.Session.MissionHistory.Count, Is.EqualTo(1));
            MissionRecord manual = _session.MissionHistory[0];
            MissionRecord auto = delegated.Session.MissionHistory[0];

            Assert.That(manual.Outcome, Is.EqualTo(SharedMissionOutcome.SafeFailure));
            Assert.That(auto.Outcome, Is.EqualTo(manual.Outcome),
                "the same frozen outcome must map to the same shared result.");
            Assert.That(manual.Mode, Is.EqualTo(MissionOperationMode.Manual));
            Assert.That(auto.Mode, Is.EqualTo(MissionOperationMode.Delegated));
            Assert.That(auto.Kind, Is.EqualTo(manual.Kind));
            Assert.That(auto.MissionId, Is.EqualTo(manual.MissionId));
            Assert.That(auto.CaptainId, Is.EqualTo(manual.CaptainId));
            Assert.That(auto.Participants, Is.EqualTo(manual.Participants));
            Assert.That(auto.DisclosedRiskParticipants,
                Is.EqualTo(manual.DisclosedRiskParticipants));
            Assert.That(auto.Preparation, Is.EqualTo(manual.Preparation));
            Assert.That(auto.FirstNinjutsu, Is.EqualTo(manual.FirstNinjutsu));
            Assert.That(auto.SecondNinjutsu, Is.EqualTo(manual.SecondNinjutsu));
            Assert.That(manual.FirstNinjutsu, Is.EqualTo(FirstBriefingNinjutsu));
            Assert.That(manual.SecondNinjutsu, Is.EqualTo(SecondBriefingNinjutsu));
            Assert.That(auto.FrozenIntelligence, Is.EqualTo(manual.FrozenIntelligence));
            Assert.That(manual.FrozenIntelligence, Is.EqualTo(BriefingIntelligence),
                "the briefing freezes the launch-time intelligence snapshot.");
            Assert.That(auto.Difficulty, Is.EqualTo(manual.Difficulty));
            Assert.That(auto.FundsDelta, Is.EqualTo(manual.FundsDelta));
            Assert.That(auto.SuppliesDelta, Is.EqualTo(manual.SuppliesDelta));
            Assert.That(auto.IntelligenceDelta, Is.EqualTo(manual.IntelligenceDelta));
            Assert.That(auto.FameBefore, Is.EqualTo(manual.FameBefore));
            Assert.That(auto.FameAfter, Is.EqualTo(manual.FameAfter));
            Assert.That(auto.ConcealmentBefore, Is.EqualTo(manual.ConcealmentBefore));
            Assert.That(auto.ConcealmentAfter, Is.EqualTo(manual.ConcealmentAfter));
            Assert.That(auto.RelationChanges, Is.EqualTo(manual.RelationChanges));
            Assert.That(auto.ParticipantStates, Is.EqualTo(manual.ParticipantStates));
            Assert.That(manual.Roll, Is.Zero,
                "manual resolution freezes the board result and does not roll a d100.");
            Assert.That(auto.Roll, Is.InRange(1, TacticalMissionResolutionRules.DieRollExclusiveMax));
            Assert.That(TacticalMissionResolutionRules.Classify(auto.Score),
                Is.EqualTo(auto.Outcome));
            Assert.That(delegated.Session.Industry.Resources,
                Is.EqualTo(_session.Industry.Resources));
            Assert.That(delegated.Session.FamilyFame, Is.EqualTo(_session.FamilyFame));
            Assert.That(delegated.Session.Concealment, Is.EqualTo(_session.Concealment));
            Assert.That(delegated.Session.RelationValue(MissionRelationIds.InterceptedDispatchTarget),
                Is.EqualTo(_session.RelationValue(MissionRelationIds.InterceptedDispatchTarget)));

            FamilyChronicleEntry manualRow = LastTacticalChronicle(_session);
            FamilyChronicleEntry autoRow = LastTacticalChronicle(delegated.Session);
            Assert.That(manualRow, Is.Not.Null);
            Assert.That(autoRow, Is.Not.Null);
            Assert.That(autoRow.EntryId, Is.EqualTo(manualRow.EntryId),
                "both entries are the first mission row of a fresh campaign.");
            Assert.That(autoRow.Sequence, Is.EqualTo(manualRow.Sequence));
            Assert.That(autoRow.Category, Is.EqualTo(manualRow.Category));
            Assert.That(autoRow.SubjectId, Is.EqualTo(manualRow.SubjectId));
            Assert.That(autoRow.SourceId, Is.EqualTo(manualRow.SourceId),
                "the chronicle source is the resolved mission action, not the entry point.");
            Assert.That(manualRow.SourceId, Is.EqualTo("withdrawn"));
            Assert.That(autoRow.Title, Is.EqualTo(manualRow.Title));
            Assert.That(autoRow.Year, Is.EqualTo(manualRow.Year));
            Assert.That(autoRow.Month, Is.EqualTo(manualRow.Month));
            Assert.That(autoRow.FromStage, Is.EqualTo(manualRow.FromStage));
            Assert.That(autoRow.ToStage, Is.EqualTo(manualRow.ToStage));
            Assert.That(manual.ChronicleEntryId, Is.EqualTo(manualRow.EntryId));
            Assert.That(auto.ChronicleEntryId, Is.EqualTo(autoRow.EntryId));
            Assert.That(ModeNeutralSummary(autoRow.Summary),
                Is.EqualTo(ModeNeutralSummary(manualRow.Summary)),
                "only the mode label and the manual board turn may differ in the summary.");
        }

        /// <summary>
        /// M07 success parity: a fully played manual extraction and a delegated check seeded
        /// to the same full success settle one frozen input to identical resources, standing,
        /// life states and chronicle data. Only the mode field and mode wording may differ.
        /// </summary>
        [Test]
        public void M06ManualAndDelegated_SameFrozenInputs_ShareSuccessAndChronicle()
        {
            using var delegated = new SessionScope(GameSession.NewGame());
            PersonId[] pair = TacticalPair(_session);
            PersonId[] delegatedPair = TacticalPair(delegated.Session);
            Assert.That(delegatedPair, Is.EqualTo(pair));

            // The frozen briefing is a real input for both entry points. The fixed roster
            // starts at zero Ninjutsu, so the success band needs explicit values.
            SaveData manualSeed = WithFrozenTacticalBriefing(_session.CaptureSave(), pair[0],
                pair[1], FirstBriefingNinjutsu, SecondBriefingNinjutsu, BriefingIntelligence);
            Assert.That(_session.TryApplySave(manualSeed).Succeeded, Is.True);
            SaveData delegatedSeed = WithFrozenTacticalBriefing(delegated.Session.CaptureSave(),
                delegatedPair[0], delegatedPair[1], FirstBriefingNinjutsu, SecondBriefingNinjutsu,
                BriefingIntelligence);
            Assert.That(delegated.Session.TryApplySave(delegatedSeed).Succeeded, Is.True);

            TacticalActionResult manualStart = _session.TryBeginTacticalMission(pair[0], pair[1],
                pair[0], MissionPreparation.None);
            Assert.That(manualStart.Succeeded, Is.True, manualStart.Feedback);
            PlayManualExtraction(_session);

            SaveData seeded = WithSeededMissionOutcome(delegated.Session.CaptureSave(),
                FirstBriefingNinjutsu, SecondBriefingNinjutsu, BriefingIntelligence,
                SharedMissionOutcome.FullSuccess);
            SaveOperationResult applied = delegated.Session.TryApplySave(seeded);
            Assert.That(applied.Succeeded, Is.True, applied.Message);

            TacticalActionResult delegatedStart =
                delegated.Session.TryDelegateTacticalMission(delegatedPair[0],
                    delegatedPair[1], delegatedPair[0], MissionPreparation.None);
            Assert.That(delegatedStart.Succeeded, Is.True, delegatedStart.Feedback);

            Assert.That(_session.MissionHistory.Count, Is.EqualTo(1));
            Assert.That(delegated.Session.MissionHistory.Count, Is.EqualTo(1));
            MissionRecord manual = _session.MissionHistory[0];
            MissionRecord auto = delegated.Session.MissionHistory[0];

            Assert.That(manual.Outcome, Is.EqualTo(SharedMissionOutcome.FullSuccess));
            Assert.That(auto.Outcome, Is.EqualTo(manual.Outcome));
            Assert.That(manual.Mode, Is.EqualTo(MissionOperationMode.Manual));
            Assert.That(auto.Mode, Is.EqualTo(MissionOperationMode.Delegated));
            Assert.That(manual.FrozenIntelligence, Is.EqualTo(BriefingIntelligence));
            Assert.That(auto.FrozenIntelligence, Is.EqualTo(manual.FrozenIntelligence));
            Assert.That(manual.FirstNinjutsu, Is.EqualTo(FirstBriefingNinjutsu));
            Assert.That(manual.SecondNinjutsu, Is.EqualTo(SecondBriefingNinjutsu));
            Assert.That(auto.FirstNinjutsu, Is.EqualTo(manual.FirstNinjutsu));
            Assert.That(auto.SecondNinjutsu, Is.EqualTo(manual.SecondNinjutsu));
            Assert.That(manual.FundsDelta,
                Is.EqualTo(TacticalMissionResolutionRules.SuccessFunds));
            Assert.That(manual.IntelligenceDelta,
                Is.EqualTo(TacticalMissionResolutionRules.SuccessIntelligence));
            Assert.That(manual.SuppliesDelta,
                Is.EqualTo(-TacticalMissionResolutionRules.BaseSupplyCost));
            Assert.That(auto.FundsDelta, Is.EqualTo(manual.FundsDelta));
            Assert.That(auto.SuppliesDelta, Is.EqualTo(manual.SuppliesDelta));
            Assert.That(auto.IntelligenceDelta, Is.EqualTo(manual.IntelligenceDelta));
            Assert.That(auto.FameBefore, Is.EqualTo(manual.FameBefore));
            Assert.That(auto.FameAfter, Is.EqualTo(manual.FameAfter));
            Assert.That(auto.ConcealmentBefore, Is.EqualTo(manual.ConcealmentBefore));
            Assert.That(auto.ConcealmentAfter, Is.EqualTo(manual.ConcealmentAfter));
            Assert.That(auto.RelationChanges, Is.EqualTo(manual.RelationChanges));
            Assert.That(auto.ParticipantStates, Is.EqualTo(manual.ParticipantStates));
            Assert.That(auto.DisclosedRiskParticipants,
                Is.EqualTo(manual.DisclosedRiskParticipants));
            Assert.That(delegated.Session.Industry.Resources,
                Is.EqualTo(_session.Industry.Resources));
            Assert.That(delegated.Session.FamilyFame, Is.EqualTo(_session.FamilyFame));
            Assert.That(delegated.Session.Concealment, Is.EqualTo(_session.Concealment));

            FamilyChronicleEntry manualRow = LastTacticalChronicle(_session);
            FamilyChronicleEntry autoRow = LastTacticalChronicle(delegated.Session);
            Assert.That(manualRow, Is.Not.Null);
            Assert.That(autoRow, Is.Not.Null);
            Assert.That(autoRow.EntryId, Is.EqualTo(manualRow.EntryId));
            Assert.That(autoRow.Sequence, Is.EqualTo(manualRow.Sequence));
            Assert.That(autoRow.Category, Is.EqualTo(manualRow.Category));
            Assert.That(autoRow.SubjectId, Is.EqualTo(manualRow.SubjectId));
            Assert.That(autoRow.SourceId, Is.EqualTo(manualRow.SourceId),
                "the chronicle source is the resolved mission action, not the entry point.");
            Assert.That(manualRow.SourceId, Is.EqualTo("succeeded"));
            Assert.That(autoRow.Title, Is.EqualTo(manualRow.Title));
            Assert.That(autoRow.Year, Is.EqualTo(manualRow.Year));
            Assert.That(autoRow.Month, Is.EqualTo(manualRow.Month));
            Assert.That(autoRow.FromStage, Is.EqualTo(manualRow.FromStage));
            Assert.That(autoRow.FromStage, Is.EqualTo((int)TacticalMissionStage.InProgress));
            Assert.That(autoRow.ToStage, Is.EqualTo(manualRow.ToStage));
            Assert.That(autoRow.ToStage, Is.EqualTo((int)TacticalMissionStage.Succeeded));
            Assert.That(manual.ChronicleEntryId, Is.EqualTo(manualRow.EntryId));
            Assert.That(auto.ChronicleEntryId, Is.EqualTo(autoRow.EntryId));
            Assert.That(ModeNeutralSummary(autoRow.Summary),
                Is.EqualTo(ModeNeutralSummary(manualRow.Summary)));
        }

        /// <summary>
        /// M07 costly-success parity end to end: a manual extraction after a guard reached
        /// alert and a delegated check seeded into the costly band settle one frozen input
        /// to the same result, cost and chronicle. Scouting pays base 2 + scouting 1 +
        /// costly 1 = 4 supplies; no preparation pays 2 + 1 = 3.
        /// </summary>
        [Test]
        public void M06ManualAndDelegated_CostlySuccess_ApplyTheSameCostsAndChronicle()
        {
            AssertCostlySuccessParity(MissionPreparation.Scouting, expectedSupplyCost: 4);
            AssertCostlySuccessParity(MissionPreparation.None, expectedSupplyCost: 3);
        }

        private void AssertCostlySuccessParity(MissionPreparation preparation,
            int expectedSupplyCost)
        {
            using var manual = new SessionScope(GameSession.NewGame());
            using var delegated = new SessionScope(GameSession.NewGame());
            PersonId[] pair = TacticalPair(manual.Session);
            PersonId[] delegatedPair = TacticalPair(delegated.Session);
            Assert.That(delegatedPair, Is.EqualTo(pair));

            // Freeze the same launch-time briefing in both campaigns. The fixed roster
            // starts at zero Ninjutsu, so the costly band is only reachable with explicit
            // values; the frozen request then carries identical numbers either way.
            SaveData manualSeed = WithFrozenTacticalBriefing(manual.Session.CaptureSave(), pair[0],
                pair[1], FirstBriefingNinjutsu, SecondBriefingNinjutsu, BriefingIntelligence);
            Assert.That(manual.Session.TryApplySave(manualSeed).Succeeded, Is.True);
            SaveData delegatedSeed = WithFrozenTacticalBriefing(delegated.Session.CaptureSave(),
                delegatedPair[0], delegatedPair[1], FirstBriefingNinjutsu, SecondBriefingNinjutsu,
                BriefingIntelligence);
            Assert.That(delegated.Session.TryApplySave(delegatedSeed).Succeeded, Is.True);

            TacticalActionResult manualStart = manual.Session.TryBeginTacticalMission(
                pair[0], pair[1], pair[0], preparation);
            Assert.That(manualStart.Succeeded, Is.True, manualStart.Feedback);
            PlayManualAlertedExtraction(manual.Session);
            Assert.That(manual.Session.TacticalMissionStatus.Outcome,
                Is.EqualTo(TacticalMissionOutcome.Succeeded),
                "the alerted extraction still has to end as a played success.");

            SaveData seeded = WithSeededMissionOutcome(delegated.Session.CaptureSave(),
                FirstBriefingNinjutsu, SecondBriefingNinjutsu, BriefingIntelligence,
                SharedMissionOutcome.CostlySuccess, preparation);
            SaveOperationResult applied = delegated.Session.TryApplySave(seeded);
            Assert.That(applied.Succeeded, Is.True, applied.Message);

            TacticalActionResult delegatedStart = delegated.Session.TryDelegateTacticalMission(
                delegatedPair[0], delegatedPair[1], delegatedPair[0], preparation);
            Assert.That(delegatedStart.Succeeded, Is.True, delegatedStart.Feedback);

            MissionRecord manualRecord = manual.Session.MissionHistory[0];
            MissionRecord autoRecord = delegated.Session.MissionHistory[0];
            Assert.That(manualRecord.Outcome, Is.EqualTo(SharedMissionOutcome.CostlySuccess),
                "an extraction after any guard alert maps to the costly success result.");
            Assert.That(autoRecord.Outcome, Is.EqualTo(SharedMissionOutcome.CostlySuccess));
            Assert.That(manualRecord.Mode, Is.EqualTo(MissionOperationMode.Manual));
            Assert.That(autoRecord.Mode, Is.EqualTo(MissionOperationMode.Delegated));
            Assert.That(autoRecord.MissionId, Is.EqualTo(manualRecord.MissionId));
            Assert.That(autoRecord.Date, Is.EqualTo(manualRecord.Date));
            Assert.That(manualRecord.Preparation, Is.EqualTo(preparation));
            Assert.That(autoRecord.Preparation, Is.EqualTo(preparation));
            Assert.That(manualRecord.CaptainId, Is.EqualTo(autoRecord.CaptainId));
            Assert.That(manualRecord.FirstNinjutsu, Is.EqualTo(FirstBriefingNinjutsu));
            Assert.That(manualRecord.SecondNinjutsu, Is.EqualTo(SecondBriefingNinjutsu));
            Assert.That(autoRecord.FirstNinjutsu, Is.EqualTo(manualRecord.FirstNinjutsu),
                "both entry points freeze the same briefing numbers.");
            Assert.That(autoRecord.SecondNinjutsu, Is.EqualTo(manualRecord.SecondNinjutsu));
            Assert.That(manualRecord.FrozenIntelligence, Is.EqualTo(BriefingIntelligence));
            Assert.That(autoRecord.FrozenIntelligence, Is.EqualTo(manualRecord.FrozenIntelligence));

            Assert.That(manualRecord.SuppliesDelta, Is.EqualTo(-expectedSupplyCost),
                "base cost plus the chosen preparation plus the costly extra, charged once.");
            Assert.That(autoRecord.SuppliesDelta, Is.EqualTo(manualRecord.SuppliesDelta));
            Assert.That(manualRecord.FundsDelta,
                Is.EqualTo(TacticalMissionResolutionRules.SuccessFunds));
            Assert.That(autoRecord.FundsDelta, Is.EqualTo(manualRecord.FundsDelta));
            Assert.That(manualRecord.IntelligenceDelta,
                Is.EqualTo(TacticalMissionResolutionRules.SuccessIntelligence));
            Assert.That(autoRecord.IntelligenceDelta, Is.EqualTo(manualRecord.IntelligenceDelta));
            Assert.That(manualRecord.FameAfter - manualRecord.FameBefore, Is.EqualTo(2),
                "the approved costly-success fame delta.");
            Assert.That(autoRecord.FameAfter, Is.EqualTo(manualRecord.FameAfter));
            Assert.That(manualRecord.ConcealmentAfter - manualRecord.ConcealmentBefore,
                Is.EqualTo(1), "the approved costly-success concealment delta.");
            Assert.That(autoRecord.ConcealmentAfter, Is.EqualTo(manualRecord.ConcealmentAfter));
            Assert.That(manualRecord.RelationChanges.Count, Is.EqualTo(1));
            Assert.That(manualRecord.RelationChanges[0].RelationId,
                Is.EqualTo(MissionRelationIds.InterceptedDispatchTarget));
            Assert.That(manualRecord.RelationChanges[0].After -
                        manualRecord.RelationChanges[0].Before, Is.EqualTo(2),
                "the approved costly-success relation delta.");
            Assert.That(autoRecord.RelationChanges, Is.EqualTo(manualRecord.RelationChanges));
            Assert.That(autoRecord.ParticipantStates,
                Is.EqualTo(manualRecord.ParticipantStates),
                "costly success causes no injury or death in either mode.");

            FamilyChronicleEntry manualRow = LastTacticalChronicle(manual.Session);
            FamilyChronicleEntry autoRow = LastTacticalChronicle(delegated.Session);
            Assert.That(manualRow, Is.Not.Null);
            Assert.That(autoRow, Is.Not.Null);
            Assert.That(manualRow.SourceId, Is.EqualTo("succeeded"));
            Assert.That(autoRow.SourceId, Is.EqualTo(manualRow.SourceId));
            Assert.That(manualRow.FromStage, Is.EqualTo((int)TacticalMissionStage.InProgress));
            Assert.That(manualRow.ToStage, Is.EqualTo((int)TacticalMissionStage.Succeeded));
            Assert.That(autoRow.FromStage, Is.EqualTo(manualRow.FromStage));
            Assert.That(autoRow.ToStage, Is.EqualTo(manualRow.ToStage));
            Assert.That(autoRow.EntryId, Is.EqualTo(manualRow.EntryId),
                "both entries are the first mission row of a fresh campaign.");
            Assert.That(autoRow.Sequence, Is.EqualTo(manualRow.Sequence));
            Assert.That(autoRow.Category, Is.EqualTo(manualRow.Category));
            Assert.That(autoRow.SubjectId, Is.EqualTo(manualRow.SubjectId));
            Assert.That(autoRow.Title, Is.EqualTo(manualRow.Title));
            Assert.That(autoRow.Year, Is.EqualTo(manualRow.Year));
            Assert.That(autoRow.Month, Is.EqualTo(manualRow.Month));
            Assert.That(manualRecord.ChronicleEntryId, Is.EqualTo(manualRow.EntryId));
            Assert.That(autoRecord.ChronicleEntryId, Is.EqualTo(autoRow.EntryId));
            Assert.That(ModeNeutralSummary(autoRow.Summary),
                Is.EqualTo(ModeNeutralSummary(manualRow.Summary)));
            Assert.That(manualRow.Summary, Does.Contain("截获密令目标势力"),
                "the chronicle must show the relationship label, not its stable ID.");
            Assert.That(manualRow.Summary,
                Does.Not.Contain(MissionRelationIds.InterceptedDispatchTarget));
            Assert.That(autoRow.Summary, Does.Contain("截获密令目标势力"));
            Assert.That(autoRow.Summary,
                Does.Not.Contain(MissionRelationIds.InterceptedDispatchTarget));
            Assert.That(manual.Session.Industry.Resources,
                Is.EqualTo(delegated.Session.Industry.Resources));

            // Exactly one settlement per run, and the second entry is refused.
            Assert.That(manual.Session.MissionHistory.Count, Is.EqualTo(1));
            Assert.That(delegated.Session.MissionHistory.Count, Is.EqualTo(1));
            Assert.That(manual.Session.TryTacticalExtract().Succeeded, Is.False);
            Assert.That(delegated.Session.TryDelegateTacticalMission(delegatedPair[0],
                delegatedPair[1], delegatedPair[0], preparation).Succeeded, Is.False);
            Assert.That(manual.Session.MissionHistory.Count, Is.EqualTo(1));
            Assert.That(delegated.Session.MissionHistory.Count, Is.EqualTo(1));
        }

        /// <summary>
        /// The settlement transaction opens before the severe-failure death checks, so a
        /// settlement that fails after those draws restores the Missions stream. The retry
        /// then draws the same values and settles exactly like a run that never failed.
        /// </summary>
        [Test]
        public void M06DelegatedSevereFailure_FailedSettlementRestoresTheStreamAndRetries()
        {
            using var control = new SessionScope(GameSession.NewGame());
            PersonId[] pair = TacticalPair(_session);
            PersonId[] controlPair = TacticalPair(control.Session);
            Assert.That(controlPair, Is.EqualTo(pair));
            SeedDelegatedOutcome(_session, SharedMissionOutcome.SevereFailure);
            SeedDelegatedOutcome(control.Session, SharedMissionOutcome.SevereFailure);

            string streamBefore = MissionsStreamState(_session);

            _session.TacticalChronicleAppendFaultInjection = ledger =>
                ledger.TryAppendMission(1560, 4, FamilyChronicleEntry.ShadowEscortCommissionId,
                    "test", (int)ShadowEscortStage.NotAccepted,
                    (int)ShadowEscortStage.Accepted, "测试", "推进序号。", out _, out _);
            TacticalActionResult failed;
            try
            {
                failed = _session.TryDelegateTacticalMission(pair[0], pair[1], pair[0],
                    MissionPreparation.None);
            }
            finally
            {
                _session.TacticalChronicleAppendFaultInjection = null;
            }

            Assert.That(failed.Succeeded, Is.False,
                "the rejected chronicle row fails the settlement after the death draws.");
            Assert.That(failed.Feedback, Does.Not.Contain("回滚不完整"),
                "a clean rollback must not claim the snapshot failed to restore.");
            Assert.That(_session.MissionHistory, Is.Empty);
            Assert.That(MissionsStreamState(_session), Is.EqualTo(streamBefore),
                "a failed post-draw settlement must restore the whole Missions stream.");

            TacticalActionResult retried = _session.TryDelegateTacticalMission(pair[0], pair[1],
                pair[0], MissionPreparation.None);
            Assert.That(retried.Succeeded, Is.True, retried.Feedback);
            TacticalActionResult clean = control.Session.TryDelegateTacticalMission(
                controlPair[0], controlPair[1], controlPair[0], MissionPreparation.None);
            Assert.That(clean.Succeeded, Is.True, clean.Feedback);

            MissionRecord afterRetry = _session.MissionHistory[0];
            MissionRecord controlRecord = control.Session.MissionHistory[0];
            Assert.That(afterRetry.Outcome, Is.EqualTo(SharedMissionOutcome.SevereFailure));
            Assert.That(afterRetry.Roll, Is.EqualTo(controlRecord.Roll),
                "the retry must reuse the restored draw.");
            Assert.That(afterRetry.Score, Is.EqualTo(controlRecord.Score));
            Assert.That(afterRetry.ParticipantStates,
                Is.EqualTo(controlRecord.ParticipantStates),
                "the death checks must replay from the restored stream.");
            Assert.That(afterRetry.FundsDelta, Is.EqualTo(controlRecord.FundsDelta));
            Assert.That(afterRetry.SuppliesDelta, Is.EqualTo(controlRecord.SuppliesDelta));
            Assert.That(afterRetry.IntelligenceDelta,
                Is.EqualTo(controlRecord.IntelligenceDelta));
            Assert.That(afterRetry.FameAfter, Is.EqualTo(controlRecord.FameAfter));
            Assert.That(afterRetry.ConcealmentAfter,
                Is.EqualTo(controlRecord.ConcealmentAfter));
            Assert.That(_session.Industry.Resources,
                Is.EqualTo(control.Session.Industry.Resources));
            FamilyChronicleEntry retriedRow = LastTacticalChronicle(_session);
            FamilyChronicleEntry controlRow = LastTacticalChronicle(control.Session);
            Assert.That(retriedRow, Is.Not.Null);
            Assert.That(controlRow, Is.Not.Null);
            Assert.That(retriedRow.SourceId, Is.EqualTo(controlRow.SourceId));
            Assert.That(retriedRow.ToStage, Is.EqualTo(controlRow.ToStage));
        }

        /// <summary>
        /// The manual terminal has no caller-side rollback: only the shared settlement can
        /// restore the Missions stream after a capture that already drew both disclosed
        /// death checks. The failed settlement must leave the board retryable and the
        /// stream, resources and chronicle exactly as they were before the attempt.
        /// </summary>
        [Test]
        public void M06ManualSevereFailure_FailedSettlementRestoresTheStreamWithoutACallerRollback()
        {
            PersonId[] pair = TacticalPair(_session);
            TacticalActionResult start = _session.TryBeginTacticalMission(pair[0], pair[1],
                pair[0], MissionPreparation.None);
            Assert.That(start.Succeeded, Is.True, start.Feedback);

            string streamBefore = MissionsStreamState(_session);
            IndustryResources resourcesBefore = _session.Industry.Resources;
            int chronicleBefore = _session.ChronicleEntries.Count;

            _session.TacticalChronicleAppendFaultInjection = ledger =>
                ledger.TryAppendMission(1560, 4, FamilyChronicleEntry.ShadowEscortCommissionId,
                    "test", (int)ShadowEscortStage.NotAccepted,
                    (int)ShadowEscortStage.Accepted, "测试", "推进序号。", out _, out _);
            TacticalActionResult capture;
            try
            {
                PersonId caught = _session.TacticalMissionStatus.Operatives[0].PersonId;
                MoveTactical(_session, caught, TacticalDirection.North);
                MoveTactical(_session, caught, TacticalDirection.North);
                MoveTactical(_session, caught, TacticalDirection.North);
                MoveTactical(_session, caught, TacticalDirection.North);
                for (int i = 0; i < 5; i++)
                    MoveTactical(_session, caught, TacticalDirection.East);
                capture = _session.TryTacticalMove(caught, TacticalDirection.East);
            }
            finally
            {
                _session.TacticalChronicleAppendFaultInjection = null;
            }

            Assert.That(capture.Succeeded, Is.False,
                "the guard catches the carrier and the injected chronicle row fails settlement.");
            Assert.That(_session.LastTacticalMessage, Does.Contain("编年史"));
            Assert.That(_session.LastTacticalMessage, Does.Not.Contain("回滚不完整"),
                "a clean rollback must not claim the snapshot failed to restore.");
            Assert.That(_session.IsTacticalMissionActive, Is.True,
                "the reverted terminal stays playable so the result can be retried.");
            Assert.That(_session.MissionHistory, Is.Empty);
            Assert.That(_session.Industry.Resources, Is.EqualTo(resourcesBefore));
            Assert.That(_session.ChronicleEntries.Count, Is.EqualTo(chronicleBefore));
            Assert.That(MissionsStreamState(_session), Is.EqualTo(streamBefore),
                "without a caller rollback the settlement itself must restore the draws.");

            // The rolled-back session is still consistent: the reopened terminal settles
            // once on a legal second attempt and writes exactly one record.
            Assert.That(_session.TryTacticalWithdraw().Succeeded, Is.True,
                _session.LastTacticalMessage);
            Assert.That(_session.MissionHistory.Count, Is.EqualTo(1));
            Assert.That(_session.MissionHistory[0].Outcome,
                Is.EqualTo(SharedMissionOutcome.SafeFailure));
        }

        /// <summary>
        /// A rejected rollback snapshot must be distinguishable from a valid one: the
        /// restore APIs return <c>false</c> and a null output, so the session helpers can
        /// assign only after success and never replace a live manager or ledger with a
        /// rejected snapshot.
        /// </summary>
        [Test]
        public void RollbackSnapshots_RejectedInputYieldsNoReplacement()
        {
            Assert.That(RandomStateManager.TryRestore(null, out RandomStateManager noManager),
                Is.False);
            Assert.That(noManager, Is.Null);

            RandomStateSaveData captured = RandomStateManager.Create(7UL).CaptureState();
            captured.streams[2].state0Hex = "NOT-HEX";
            Assert.That(RandomStateManager.TryRestore(captured, out RandomStateManager rejected),
                Is.False);
            Assert.That(rejected, Is.Null,
                "a rejected random snapshot must not look like a usable replacement.");

            Assert.That(HistoryLedger.TryRestore(new FamilyChronicleEntry[] { null },
                out HistoryLedger noLedger, out string ledgerError), Is.False);
            Assert.That(noLedger, Is.Null);
            Assert.That(ledgerError, Is.Not.Empty);
        }

        /// <summary>
        /// M07 death disclosure: a dead or unavailable SC-013 fixed heir must not leave
        /// the death decision with an empty candidate list while other eligible living
        /// members remain, and the fallback must be selectable.
        /// </summary>
        [Test]
        public void SuccessionCandidates_FallBackWhenTheFixedHeirIsDead()
        {
            SuccessionState state = _session.Succession;
            GameDate date = _session.WorldClock.CurrentDate;
            KillPerson(state, M2FixedScenario.MasatsunaId);

            // Keep the approved retirement gate unchanged: a dead fixed heir blocks a
            // retirement handoff instead of silently falling back to a collateral line.
            Assert.That(_session.BeginSuccessionRetirement().Succeeded, Is.False,
                "the fixed-heir retirement gate must stay closed for a dead heir.");
            Assert.That(state.Phase, Is.EqualTo(SuccessionPhase.None));
            Assert.That(state.HeadPersonId, Is.EqualTo(M2FixedScenario.HarunobuId));

            KillPerson(state, state.HeadPersonId);

            SuccessionResult begun = SuccessionService.BeginDeathSuccession(state, date);
            Assert.That(begun.Succeeded, Is.True, begun.Message);
            Assert.That(state.Phase, Is.EqualTo(SuccessionPhase.AwaitingSuccessionChoice));

            IReadOnlyList<SuccessionCandidate> normal =
                SuccessionService.GetCandidates(state, date);
            Assert.That(normal.Count, Is.GreaterThan(0));
            Assert.That(CandidateIds(_session.SuccessionCandidates),
                Is.EqualTo(CandidateIds(normal)),
                "a dead fixed heir must fall back to the normal eligible living list.");

            PersonId successor = _session.SuccessionCandidates[0].PersonId;
            SuccessionResult selected = _session.SelectSuccessionSuccessor(successor);
            Assert.That(selected.Succeeded, Is.True,
                "the fallback list must be usable, not only visible: " + selected.Message);
            if (_session.Succession.Phase == SuccessionPhase.AwaitingRegentChoice)
            {
                PersonId regent = _session.SuccessionRegentCandidates[0].PersonId;
                Assert.That(_session.SelectSuccessionRegent(regent).Succeeded, Is.True,
                    _session.LastSuccessionMessage);
            }
            Assert.That(_session.ConfirmSuccession().Succeeded, Is.True,
                _session.LastSuccessionMessage);
            Assert.That(_session.Succession.HeadPersonId, Is.EqualTo(successor));
        }

        /// <summary>
        /// The approved SC-013 line has no fixed heir after the third-generation head.
        /// That head's death must therefore offer the ordinary eligible living list
        /// instead of an empty fixed-heir filter.
        /// </summary>
        [Test]
        public void SuccessionCandidates_LastGenerationHeadUsesTheEligibleList()
        {
            SuccessionState state = _session.Succession;
            // Fixture: the third handoff leaves 信正 as the last-generation head.
            state.HeadPersonId = M2FixedScenario.NobumasaId;
            state.DesignatedHeirId = default;
            Assert.That(_session.BeginSuccessionRetirement().Succeeded, Is.False,
                "the last-generation finale gate still refuses retirement.");
            KillPerson(state, M2FixedScenario.NobumasaId);

            GameDate date = _session.WorldClock.CurrentDate;
            SuccessionResult begun = SuccessionService.BeginDeathSuccession(state, date);
            Assert.That(begun.Succeeded, Is.True, begun.Message);
            Assert.That(M2FixedScenario.ExpectedHeir(state), Is.EqualTo(default(PersonId)),
                "the last generation has no fixed heir.");

            IReadOnlyList<SuccessionCandidate> normal =
                SuccessionService.GetCandidates(state, date);
            Assert.That(normal.Count, Is.GreaterThan(0));
            Assert.That(CandidateIds(_session.SuccessionCandidates),
                Is.EqualTo(CandidateIds(normal)));
        }

        /// <summary>
        /// The crisis demo replaces the whole family snapshot. An active M06 operation
        /// holds two old-roster operatives in session state, so the swap must be refused
        /// without disturbing the live operation or the save/session integrity.
        /// </summary>
        [Test]
        public void CrisisDemo_IsRefusedWhileTheTacticalMissionIsActive()
        {
            _session.InitStandardDemo();
            PersonId[] pair = TacticalPair(_session);
            TacticalActionResult start = _session.TryBeginTacticalMission(pair[0], pair[1],
                pair[0], MissionPreparation.None);
            Assert.That(start.Succeeded, Is.True, start.Feedback);

            PersonId headBefore = _session.Succession.HeadPersonId;
            FamilyId familyBefore = _session.Succession.World.Family.Id;
            TacticalMissionSnapshot before = _session.TacticalMissionStatus;
            IndustryResources resourcesBefore = _session.Industry.Resources;
            int chronicleBefore = _session.ChronicleEntries.Count;

            Assert.That(_session.StartCrisisDemo(), Is.False,
                "an active infiltration holds old-roster operatives in session state.");
            Assert.That(_session.LastSuccessionMessage, Does.Contain("潜入"));
            Assert.That(_session.IsTacticalMissionActive, Is.True,
                "the refusal must not end the running operation.");
            Assert.That(_session.Succession.HeadPersonId, Is.EqualTo(headBefore));
            Assert.That(_session.Succession.World.Family.Id, Is.EqualTo(familyBefore),
                "the refusal must not replace the family snapshot.");
            TacticalMissionSnapshot after = _session.TacticalMissionStatus;
            Assert.That(after.Turn, Is.EqualTo(before.Turn));
            Assert.That(after.Stage, Is.EqualTo(before.Stage));
            Assert.That(after.Operatives[0].Position,
                Is.EqualTo(before.Operatives[0].Position));
            Assert.That(_session.Industry.Resources, Is.EqualTo(resourcesBefore));
            Assert.That(_session.ChronicleEntries.Count, Is.EqualTo(chronicleBefore));

            SaveData snapshot = _session.CaptureSave();
            Assert.That(_session.TryApplySave(snapshot).Succeeded, Is.False,
                "an active operation still refuses a save/load round trip.");
            Assert.That(_session.IsTacticalMissionActive, Is.True);
            Assert.That(_session.TacticalMissionStatus.Turn, Is.EqualTo(before.Turn));

            Assert.That(_session.TryTacticalWithdraw().Succeeded, Is.True,
                _session.LastTacticalMessage);
            // The exit is temporal, not a permanent lock: the settled operation no longer
            // owns old-roster people, so the same entry installs the four-person roster.
            Assert.That(_session.StartCrisisDemo(), Is.True, _session.LastSuccessionMessage);
            Assert.That(_session.Succession.World.PersonCount, Is.EqualTo(4));
            Assert.That(_session.Succession.World.Family.IsMember(
                new PersonId("person.akechi.masatsuna")), Is.False,
                "the replacement roster no longer contains the old adults.");
        }

        [Test]
        public void M05FullSuccess_AppliesAndRecordsTheSupplyReward()
        {
            int roll = CommissionRollFor(_session, succeed: true);
            AcceptAndAssignCommission(_session, MissionOperationMode.Manual, roll);
            IndustryResources before = _session.Industry.Resources;
            Assert.That(_session.AdvanceDays(ShadowEscortCommission.DurationDays), Is.True);
            Assert.That(_session.IsCommissionDue, Is.True);

            MissionSettlementResult settlement = _session.TrySettleCommissionManually();
            Assert.That(settlement.Succeeded, Is.True, settlement.Message);
            MissionRecord record = _session.MissionHistory[0];
            Assert.That(record.Outcome, Is.EqualTo(SharedMissionOutcome.FullSuccess));
            Assert.That(record.SuppliesDelta,
                Is.EqualTo(ShadowEscortCommission.RewardSuccessSupplies));
            Assert.That(_session.Industry.Resources.Supplies,
                Is.EqualTo(before.Supplies + ShadowEscortCommission.RewardSuccessSupplies));
            Assert.That(_session.Industry.Resources.Funds,
                Is.EqualTo(before.Funds + record.FundsDelta));
            string summary = LastCommissionChronicle(_session).Summary;
            Assert.That(summary,
                Does.Contain("、物资 +" + ShadowEscortCommission.RewardSuccessSupplies + "、情报"),
                "the settlement chronicle must record the actual supply reward.");
            Assert.That(summary, Does.Contain("尾张商队信用"),
                "the chronicle must show the relationship label, not its stable ID.");
            Assert.That(summary, Does.Not.Contain(MissionRelationIds.ShadowEscortMerchant));
        }

        [Test]
        public void M05PartialSuccess_AppliesAndRecordsTheSupplyReward()
        {
            int roll = CommissionRollFor(_session, succeed: false);
            AcceptAndAssignCommission(_session, MissionOperationMode.Manual, roll);
            IndustryResources before = _session.Industry.Resources;
            Assert.That(_session.AdvanceDays(ShadowEscortCommission.DurationDays), Is.True);

            MissionSettlementResult settlement = _session.TrySettleCommissionManually();
            Assert.That(settlement.Succeeded, Is.True, settlement.Message);
            MissionRecord record = _session.MissionHistory[0];
            Assert.That(record.Outcome, Is.EqualTo(SharedMissionOutcome.PartialSuccess));
            Assert.That(record.SuppliesDelta,
                Is.EqualTo(ShadowEscortCommission.RewardPartialSupplies));
            Assert.That(_session.Industry.Resources.Supplies,
                Is.EqualTo(before.Supplies + ShadowEscortCommission.RewardPartialSupplies));
            Assert.That(_session.Industry.Resources.Funds,
                Is.EqualTo(before.Funds + ShadowEscortCommission.RewardPartialFunds));
            Assert.That(LastCommissionChronicle(_session).Summary,
                Does.Contain("、物资 +" + ShadowEscortCommission.RewardPartialSupplies + "、情报"),
                "the settlement chronicle must record the actual supply reward.");
        }

        [Test]
        public void M05ManualAndDelegatedSettlement_ApplyTheSameReward()
        {
            int roll = CommissionRollFor(_session, succeed: true);
            using var delegated = new SessionScope(GameSession.NewGame());
            AcceptAndAssignCommission(_session, MissionOperationMode.Manual, roll);
            AcceptAndAssignCommission(delegated.Session, MissionOperationMode.Delegated, roll);

            Assert.That(_session.AdvanceDays(ShadowEscortCommission.DurationDays), Is.True);
            Assert.That(delegated.Session.AdvanceDays(ShadowEscortCommission.DurationDays),
                Is.True);
            Assert.That(_session.MissionHistory, Is.Empty,
                "manual play waits for the explicit settlement action.");
            Assert.That(delegated.Session.MissionHistory.Count, Is.EqualTo(1));

            MissionSettlementResult manual = _session.TrySettleCommissionManually();
            Assert.That(manual.Succeeded, Is.True, manual.Message);
            MissionRecord left = _session.MissionHistory[0];
            MissionRecord right = delegated.Session.MissionHistory[0];
            Assert.That(right.Mode, Is.EqualTo(MissionOperationMode.Delegated));
            Assert.That(left.Mode, Is.EqualTo(MissionOperationMode.Manual));
            Assert.That(right.Outcome, Is.EqualTo(left.Outcome));
            Assert.That(right.FundsDelta, Is.EqualTo(left.FundsDelta));
            Assert.That(right.SuppliesDelta, Is.EqualTo(left.SuppliesDelta));
            Assert.That(right.IntelligenceDelta, Is.EqualTo(left.IntelligenceDelta));
            Assert.That(delegated.Session.Industry.Resources,
                Is.EqualTo(_session.Industry.Resources),
                "the same frozen input must settle to the same resources either way.");
            Assert.That(left.SuppliesDelta,
                Is.EqualTo(ShadowEscortCommission.RewardSuccessSupplies));
        }

        /// <summary>
        /// Operation mode changes only how the player interacts with the run. A manual
        /// settlement clicked after the due date therefore records the same mission and
        /// chronicle date as delegated auto-settlement on the due day; time is not blocked
        /// and the extra clicked days stay ordinary campaign days.
        /// </summary>
        [Test]
        public void M05DelayedManualClick_SettlesOnTheCommissionDueDate()
        {
            int roll = CommissionRollFor(_session, succeed: true);
            using var delegated = new SessionScope(GameSession.NewGame());
            AcceptAndAssignCommission(_session, MissionOperationMode.Manual, roll);
            AcceptAndAssignCommission(delegated.Session, MissionOperationMode.Delegated, roll);

            Assert.That(_session.AdvanceDays(ShadowEscortCommission.DurationDays), Is.True);
            Assert.That(delegated.Session.AdvanceDays(ShadowEscortCommission.DurationDays),
                Is.True);
            Assert.That(delegated.Session.MissionHistory.Count, Is.EqualTo(1));
            Assert.That(_session.AdvanceDays(2), Is.True,
                "waiting after the due date is ordinary campaign time, not blocked.");
            Assert.That(_session.IsCommissionDue, Is.True);
            Assert.That(_session.MissionHistory, Is.Empty);

            MissionSettlementResult settlement = _session.TrySettleCommissionManually();
            Assert.That(settlement.Succeeded, Is.True, settlement.Message);

            var due = new GameDate(1560, 4, 4);
            MissionRecord manualRecord = _session.MissionHistory[0];
            MissionRecord autoRecord = delegated.Session.MissionHistory[0];
            Assert.That(manualRecord.Date, Is.EqualTo(due),
                "a delayed click records the commission due date, not the click date.");
            Assert.That(autoRecord.Date, Is.EqualTo(due));
            Assert.That(_session.WorldClock.CurrentDate.Day, Is.EqualTo(6),
                "the delayed click itself stays on the real calendar day.");
            FamilyChronicleEntry manualRow = LastCommissionChronicle(_session);
            FamilyChronicleEntry autoRow = LastCommissionChronicle(delegated.Session);
            Assert.That(manualRow, Is.Not.Null);
            Assert.That(autoRow, Is.Not.Null);
            Assert.That(manualRow.Year, Is.EqualTo(due.Year));
            Assert.That(manualRow.Month, Is.EqualTo(due.Month));
            Assert.That(autoRow.Year, Is.EqualTo(manualRow.Year));
            Assert.That(autoRow.Month, Is.EqualTo(manualRow.Month));
            Assert.That(manualRow.Summary, Does.Contain("（1560-04-04）"));
            Assert.That(autoRow.Summary, Does.Contain("（1560-04-04）"));
            Assert.That(autoRow.SourceId, Is.EqualTo(manualRow.SourceId));
            Assert.That(autoRecord.Outcome, Is.EqualTo(manualRecord.Outcome));
        }

        [Test]
        public void M05SupplyReward_ClampsAtTheCeilingAndRecordsTheAppliedDelta()
        {
            SaveData data = _session.CaptureSave();
            data.industry.supplies = IndustryResources.Maximum;
            Assert.That(_session.TryApplySave(data).Succeeded, Is.True);

            int roll = CommissionRollFor(_session, succeed: true);
            AcceptAndAssignCommission(_session, MissionOperationMode.Manual, roll);
            Assert.That(_session.AdvanceDays(ShadowEscortCommission.DurationDays), Is.True);

            IndustryResources capped = _session.Industry.Resources;
            MissionSettlementResult settlement = _session.TrySettleCommissionManually();
            Assert.That(settlement.Succeeded, Is.True, settlement.Message);
            MissionRecord record = _session.MissionHistory[0];
            Assert.That(record.Outcome, Is.EqualTo(SharedMissionOutcome.FullSuccess));
            Assert.That(record.SuppliesDelta, Is.Zero,
                "a capped supply pool reports the applied reward, which is zero.");
            Assert.That(_session.Industry.Resources.Supplies,
                Is.EqualTo(IndustryResources.Maximum));
            Assert.That(settlement.SuppliesDelta, Is.Zero);

            // Pin both clauses separately. The commission stores only the supply it
            // actually granted, so at the cap both the applied section and the reward
            // clause report the clamped +0; the "奖励 " prefix keeps each assertion on
            // the clause it names instead of matching one bare fragment twice.
            string summary = LastCommissionChronicle(_session).Summary;
            string funds = (record.FundsDelta >= 0 ? "+" : string.Empty) +
                           record.FundsDelta;
            Assert.That(summary, Does.Contain("｜资金 " + funds + "、物资 +0、情报 +0"),
                "the applied section must report the clamped supply delta.");
            Assert.That(summary, Does.Contain("奖励 资金 " + funds + "、物资 +0、情报 +0"),
                "the reward clause reports the applied reward, which is also zero at the cap.");
            Assert.That(summary, Does.Not.Contain("物资 +" +
                    ShadowEscortCommission.RewardSuccessSupplies),
                "the nominal reward must not be claimed when it was not granted.");
            Assert.That(_session.Industry.Resources.Supplies,
                Is.EqualTo(capped.Supplies));
        }

        [Test]
        public void Injury_BlocksMissionsAndClearsAtTheNextMonthBoundary()
        {
            PersonId assignee = FirstEligibleCommissionAssignee();
            _session.SetInjuredForTest(assignee, true);
            Assert.That(_session.IsPersonInjured(assignee), Is.True);

            Assert.That(_session.TryAcceptCommission().Succeeded, Is.True);
            ShadowEscortActionResult assign = _session.TryAssignCommission(assignee);
            Assert.That(assign.Succeeded, Is.False);
            Assert.That(assign.Feedback, Does.Contain("负伤"));

            PersonId[] pair = TacticalPair(_session);
            PersonId injuredForTactical = pair[0];
            _session.SetInjuredForTest(injuredForTactical, true);
            TacticalActionResult start = _session.TryBeginTacticalMission(pair[0], pair[1],
                pair[0], MissionPreparation.None);
            Assert.That(start.Succeeded, Is.False);
            Assert.That(start.Feedback, Does.Contain("负伤"));

            GameDate before = _session.WorldClock.CurrentDate;
            Assert.That(_session.TryAdvanceToNextMonth(), Is.True, _session.LastEventMessage);
            Assert.That(_session.LastMonthTurnOutcome, Is.EqualTo(MonthTurnOutcome.Succeeded));
            Assert.That(_session.WorldClock.CurrentDate.CompareTo(before), Is.GreaterThan(0),
                "the successful boundary must actually advance the calendar.");
            Assert.That(_session.WorldClock.CurrentDate.Month,
                Is.Not.EqualTo(before.Month),
                "recovery happens at a real month boundary, not before it.");
            Assert.That(_session.IsPersonInjured(assignee), Is.False,
                "the next month boundary restores every injured person.");
            Assert.That(_session.IsPersonInjured(injuredForTactical), Is.False);
        }

        [Test]
        public void MonthBoundary_DateOutOfRange_PreservesInjury()
        {
            Assert.That(_session.AdvanceDaysForLegacyFixtureSetup(160981), Is.True);
            Assert.That(_session.WorldClock.CurrentDate, Is.EqualTo(new GameDate(2000, 12, 31)));
            PersonId injured = FirstEligibleCommissionAssignee();
            _session.SetInjuredForTest(injured, true);

            Assert.That(_session.TryAdvanceToNextMonth(), Is.False);
            Assert.That(_session.LastMonthTurnOutcome,
                Is.EqualTo(MonthTurnOutcome.DateOutOfRange));
            Assert.That(_session.IsPersonInjured(injured), Is.True,
                "a month turn that cannot advance the date must not clear injuries.");
        }

        [Test]
        public void MonthBoundary_RejectedSettlement_PreservesInjury()
        {
            PersonId injured = FirstEligibleCommissionAssignee();
            _session.SetInjuredForTest(injured, true);
            GameDate date = _session.WorldClock.CurrentDate;
            Assert.That(IndustryStateForTest(_session).TrySetLastSettledMonthIndex(
                IndustryCalendar.MonthIndex(date) - 2, out _), Is.True);

            Assert.That(_session.TryAdvanceToNextMonth(), Is.False);
            Assert.That(_session.LastMonthTurnOutcome,
                Is.EqualTo(MonthTurnOutcome.IndustryStateInvalid));
            Assert.That(_session.WorldClock.CurrentDate, Is.EqualTo(date));
            Assert.That(_session.IsPersonInjured(injured), Is.True,
                "a rejected month transaction that leaves the date unchanged keeps the injury.");
        }

        [Test]
        public void MonthBoundary_TransactionFailure_RestoresInjury()
        {
            PersonId injured = FirstEligibleCommissionAssignee();
            _session.SetInjuredForTest(injured, true);
            GameDate date = _session.WorldClock.CurrentDate;
            _session.MonthSettlementFaultInjection = () =>
                throw new InvalidOperationException("injected month failure");
            try
            {
                Assert.That(_session.TryAdvanceToNextMonth(), Is.False);
            }
            finally
            {
                _session.MonthSettlementFaultInjection = null;
            }

            Assert.That(_session.LastMonthTurnOutcome,
                Is.EqualTo(MonthTurnOutcome.SettlementFailed));
            Assert.That(_session.WorldClock.CurrentDate, Is.EqualTo(date),
                "the clock must be restored after a commit-phase exception.");
            Assert.That(_session.IsPersonInjured(injured), Is.True,
                "the injury rollback must follow the restored date.");
        }

        [Test]
        public void MonthBoundary_CommittedMonthWithAlreadySettledIndustry_ClearsInjury()
        {
            PersonId injured = FirstEligibleCommissionAssignee();
            _session.SetInjuredForTest(injured, true);
            GameDate date = _session.WorldClock.CurrentDate;
            Assert.That(IndustryStateForTest(_session).TrySetLastSettledMonthIndex(
                IndustryCalendar.MonthIndex(date), out _), Is.True);

            Assert.That(_session.TryAdvanceToNextMonth(), Is.True, _session.LastEventMessage);
            Assert.That(_session.LastMonthTurnOutcome, Is.EqualTo(MonthTurnOutcome.Succeeded));
            Assert.That(_session.WorldClock.CurrentDate.Month, Is.Not.EqualTo(date.Month),
                "the month must actually commit even when industry reports AlreadySettled.");
            Assert.That(_session.IsPersonInjured(injured), Is.False,
                "a committed month clears injuries even when the industry plan reports failure.");
        }

        [Test]
        public void SaveLoad_RoundTripsStandingHistoryAndInjury()
        {
            Assert.That(_session.TryAcceptCommission().Succeeded, Is.True);
            Assert.That(_session.TrySetCommissionMode(MissionOperationMode.Delegated, out _),
                Is.True);
            PersonId assignee = FirstEligibleCommissionAssignee();
            Assert.That(_session.TryAssignCommission(assignee).Succeeded, Is.True);
            for (int i = 0; i < ShadowEscortCommission.DurationDays; i++)
                _session.AdvanceOneDay();
            PersonId injured = FirstTacticalOperative();
            _session.SetInjuredForTest(injured, true);

            SaveData saved = _session.CaptureSave();
            Assert.That(saved.familyStanding, Is.Not.Null);
            Assert.That(saved.injury, Is.Not.Null);
            Assert.That(saved.missionHistory, Has.Length.EqualTo(_session.MissionHistory.Count));

            GameSession restored = GameSession.NewGame();
            try
            {
                SaveOperationResult applied = restored.TryApplySave(saved);
                Assert.That(applied.Succeeded, Is.True, applied.Message);
                Assert.That(restored.FamilyFame, Is.EqualTo(_session.FamilyFame));
                Assert.That(restored.Concealment, Is.EqualTo(_session.Concealment));
                Assert.That(restored.MissionHistory.Count,
                    Is.EqualTo(_session.MissionHistory.Count));
                Assert.That(restored.MissionHistory[0].RunId,
                    Is.EqualTo(_session.MissionHistory[0].RunId));
                Assert.That(restored.MissionHistory[0].Outcome,
                    Is.EqualTo(_session.MissionHistory[0].Outcome));
                Assert.That(restored.IsPersonInjured(injured), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(restored.gameObject);
            }
        }

        [Test]
        public void Succession_DeathOfHeadWaitsForChoiceAndSavesInPhase()
        {
            SuccessionState state = SuccessionSample.CreateStandard();
            state.World.TryGetLife(state.HeadPersonId, out SuccessionPersonLife headLife);
            Assert.That(state.World.TrySetLife(state.HeadPersonId,
                new SuccessionPersonLife(headLife.BirthYear, false, false), out string killError),
                Is.True, killError);

            GameDate date = new GameDate(1561, 1, 1);
            SuccessionResult begun = SuccessionService.BeginDeathSuccession(state, date);
            Assert.That(begun.Succeeded, Is.True, begun.Message);
            Assert.That(state.Phase, Is.EqualTo(SuccessionPhase.AwaitingSuccessionChoice));
            Assert.That(SuccessionService.GetCandidates(state, date).Count,
                Is.GreaterThan(1), "multiple adult candidates must not be auto-picked.");

            Assert.That(SuccessionSaveData.Capture(state).ValidateForSave(date).Succeeded,
                Is.True, "a dead head is saveable only inside the death succession phase.");

            Assert.That(SuccessionService.ConfirmSuccession(state, date).Succeeded, Is.False,
                "the player must choose before the handoff confirms.");
            SuccessionCandidate candidate = SuccessionService.GetCandidates(state, date)[0];
            Assert.That(SuccessionService.SelectSuccessor(state, candidate.PersonId, date).Succeeded,
                Is.True);
            SuccessionResult confirmed = SuccessionService.ConfirmSuccession(state, date);
            Assert.That(confirmed.Succeeded, Is.True, confirmed.Message);
            Assert.That(state.HeadPersonId, Is.EqualTo(candidate.PersonId));
            Assert.That(SuccessionSaveData.Capture(state).ValidateForSave(date).Succeeded,
                Is.True);
        }

        [Test]
        public void CommissionAbandonment_HasNoSharedResultOrHistory()
        {
            Assert.That(IndustryResources.TryCreate(30, 12, 0, out IndustryResources resources,
                out string resourceError), Is.True, resourceError);
            var commission = new ShadowEscortCommission();
            PersonId assignee = new PersonId("person.test.assignee");
            GameDate date = new GameDate(1560, 4, 1);
            Assert.That(commission.TryAccept(LocationId.Kiyosu, FamilyCreedKind.RighteousnessFirst,
                resources, out _).Succeeded, Is.True);
            Assert.That(commission.TryAssign(assignee, false, false, true, date).Succeeded, Is.True);
            Assert.That(commission.TryAssignRunId(GameSession.CommissionRunId, out _), Is.True);

            Assert.That(commission.TryAbandon("执行人不可用", out ShadowEscortActionResult action),
                Is.True);
            Assert.That(action.Feedback, Does.Contain("执行人不可用"));
            Assert.That(commission.Stage, Is.EqualTo(ShadowEscortStage.Abandoned));
            Assert.That(commission.Outcome, Is.EqualTo(CommissionOutcome.None));
            Assert.That(commission.RunId, Is.Empty,
                "abandonment is not a shared result and keeps no run ID.");
            Assert.That(commission.RewardFunds, Is.Zero);
            Assert.That(commission.RewardSupplies, Is.Zero);

            ShadowEscortCommissionSaveData captured = commission.Capture();
            SaveData probe = _session.CaptureSave();
            probe.commission = captured;
            SaveOperationResult validated = SaveDataValidator.Validate(probe);
            Assert.That(validated.Succeeded, Is.True,
                "an abandoned row is a legal, saveable state: " + validated.Message);
            ShadowEscortCommission restored = ShadowEscortCommission.TryRestore(
                (ShadowEscortStage)captured.stage, captured.assigneePersonId,
                captured.durationDays, captured.elapsedDays, captured.startYear,
                captured.startMonth, captured.startDay, (CommissionOutcome)captured.outcome,
                captured.score, captured.roll, captured.rewardFunds, captured.rewardSupplies,
                captured.investedFunds, captured.lastFeedback, captured.runId, true,
                (MissionOperationMode)captured.mode, out string restoreError);
            Assert.That(restored, Is.Not.Null, restoreError);
            Assert.That(restored.Stage, Is.EqualTo(ShadowEscortStage.Abandoned));
        }

        private PersonId FirstEligibleCommissionAssignee() =>
            FirstEligibleCommissionAssignee(_session);

        private static PersonId FirstEligibleCommissionAssignee(GameSession session)
        {
            IReadOnlyList<CommissionAssigneeOption> options = session.CommissionAssignees;
            for (int i = 0; i < options.Count; i++)
                if (options[i].Eligible)
                    return options[i].PersonId;
            Assert.Fail("the fixed roster must offer an eligible commission assignee.");
            return default;
        }

        /// <summary>
        /// Chooses a roll that drives the fixed roster's zero-growth attributes plus the
        /// current creed bonus into the requested M05 band. The caller then freezes it into
        /// the Missions random stream so both manual and delegated play see one input.
        /// </summary>
        private static int CommissionRollFor(GameSession session, bool succeed)
        {
            PersonId assignee = FirstEligibleCommissionAssignee(session);
            Assert.That(session.Development.TryGetValues(assignee,
                out CharacterGrowthValues values), Is.True);
            FamilyCreedKind creed = session.CurrentFamilyCreed;
            int bonus = creed == FamilyCreedKind.RighteousnessFirst
                ? ShadowEscortCommission.RighteousnessScoreBonus
                : creed == FamilyCreedKind.MeritFirst
                    ? ShadowEscortCommission.MeritFirstScoreBonus
                    : 0;
            int baseScore = values.MartialArts + values.Management + values.Social + bonus;
            if (succeed)
                return Math.Min(ShadowEscortCommission.RollExclusiveMax - 1,
                    Math.Max(0, ShadowEscortCommission.SuccessThreshold - baseScore));
            int partialRoll = Math.Max(0,
                ShadowEscortCommission.PartialThreshold - baseScore);
            Assert.That(baseScore + partialRoll,
                Is.LessThan(ShadowEscortCommission.SuccessThreshold),
                "the fixed roster must be able to produce a partial escort result.");
            return partialRoll;
        }

        private static void AcceptAndAssignCommission(GameSession session,
            MissionOperationMode mode, int roll)
        {
            SaveData seeded = WithForcedMissionsRoll(session.CaptureSave(), roll);
            SaveOperationResult applied = session.TryApplySave(seeded);
            Assert.That(applied.Succeeded, Is.True, applied.Message);
            Assert.That(session.TryAcceptCommission().Succeeded, Is.True,
                session.LastCommissionMessage);
            Assert.That(session.TrySetCommissionMode(mode, out string modeError), Is.True,
                modeError);
            Assert.That(session.TryAssignCommission(
                FirstEligibleCommissionAssignee(session)).Succeeded, Is.True,
                session.LastCommissionMessage);
            Assert.That(session.CommissionMode, Is.EqualTo(mode));
        }

        /// <summary>
        /// Replaces only the Missions stream with a fresh stream whose very first draw is
        /// the requested roll, so the next settlement consumes a deterministic input.
        /// </summary>
        private static SaveData WithForcedMissionsRoll(SaveData data, int roll)
        {
            int missionsIndex = -1;
            for (int i = 0; i < data.randomState.streams.Length; i++)
                if (data.randomState.streams[i].streamId ==
                    RandomStateManager.StableId(RandomStreamKind.Missions))
                    missionsIndex = i;
            Assert.That(missionsIndex, Is.GreaterThanOrEqualTo(0),
                "the saved random state must contain the Missions stream.");

            for (ulong seed = 1; seed < 100000UL; seed++)
            {
                RandomStateManager probe = RandomStateManager.Create(seed);
                RandomStateSaveData candidate = probe.CaptureState();
                if (!probe.GetStream(RandomStreamKind.Missions).TryNextInt(
                        ShadowEscortCommission.RollExclusiveMax, out int preview) ||
                    preview != roll)
                    continue;
                data.randomState.streams[missionsIndex] = candidate.streams[missionsIndex];
                return data;
            }

            Assert.Fail("no Missions seed produces roll " + roll + ".");
            return data;
        }

        private static FamilyChronicleEntry LastCommissionChronicle(GameSession session)
        {
            IReadOnlyList<FamilyChronicleEntry> entries = session.ChronicleEntries;
            for (int i = entries.Count - 1; i >= 0; i--)
                if (entries[i].Category == ChronicleCategory.Mission &&
                    entries[i].SubjectId == FamilyChronicleEntry.ShadowEscortCommissionId)
                    return entries[i];
            return null;
        }

        private static FamilyChronicleEntry LastTacticalChronicle(GameSession session)
        {
            IReadOnlyList<FamilyChronicleEntry> entries = session.ChronicleEntries;
            for (int i = entries.Count - 1; i >= 0; i--)
                if (entries[i].Category == ChronicleCategory.Mission &&
                    entries[i].SubjectId == FamilyChronicleEntry.TacticalMissionId)
                    return entries[i];
            return null;
        }

        /// <summary>
        /// Removes the two values that legitimately depend on the entry point: the
        /// manual/delegated mode label and the manual board turn segment. Everything
        /// else in the shared settlement summary must be identical.
        /// </summary>
        private static string ModeNeutralSummary(string summary)
        {
            if (string.IsNullOrEmpty(summary))
                return string.Empty;
            string normalized = summary
                .Replace("｜" + MissionOutcomeTable.ModeName(MissionOperationMode.Manual) + "｜",
                    "｜｜")
                .Replace("｜" + MissionOutcomeTable.ModeName(MissionOperationMode.Delegated) + "｜",
                    "｜｜");
            int turn = normalized.IndexOf("｜回合 ", StringComparison.Ordinal);
            if (turn >= 0)
            {
                int end = normalized.IndexOf('｜', turn + 1);
                if (end > turn)
                    normalized = normalized.Remove(turn, end - turn);
            }
            return normalized;
        }

        private static void KillPerson(SuccessionState state, PersonId personId)
        {
            Assert.That(state.World.TryGetLife(personId, out SuccessionPersonLife life), Is.True,
                "the fixture requires a registered " + personId.Value);
            Assert.That(state.World.TrySetLife(personId,
                new SuccessionPersonLife(life.BirthYear, false, life.Retired),
                out string error), Is.True, error);
        }

        private static string[] CandidateIds(IReadOnlyList<SuccessionCandidate> candidates)
        {
            var ids = new string[candidates.Count];
            for (int i = 0; i < ids.Length; i++)
                ids[i] = candidates[i].PersonId.Value;
            return ids;
        }

        /// <summary>
        /// Freezes the launch-time briefing inputs both entry points share: the two
        /// operatives' Ninjutsu and the family intelligence pool. The fixed roster starts
        /// at zero growth, so without this fixture the delegated score cannot reach the
        /// success bands and the parity test would prove nothing.
        /// </summary>
        private static SaveData WithFrozenTacticalBriefing(SaveData data, PersonId first,
            PersonId second, int firstNinjutsu, int secondNinjutsu, int intelligence)
        {
            Assert.That(data, Is.Not.Null);
            Assert.That(data.industry, Is.Not.Null);
            Assert.That(data.development, Is.Not.Null);
            Assert.That(data.development.persons, Is.Not.Null);
            data.industry.intelligence = intelligence;
            bool firstFound = false;
            bool secondFound = false;
            for (int i = 0; i < data.development.persons.Length; i++)
            {
                CharacterDevelopmentPersonSaveEntry person = data.development.persons[i];
                if (person == null)
                    continue;
                if (person.personId == first.Value)
                {
                    person.ninjutsu = firstNinjutsu;
                    firstFound = true;
                }
                else if (person.personId == second.Value)
                {
                    person.ninjutsu = secondNinjutsu;
                    secondFound = true;
                }
            }

            Assert.That(firstFound, Is.True, "the first operative needs a growth row.");
            Assert.That(secondFound, Is.True, "the second operative needs a growth row.");
            return data;
        }

        /// <summary>
        /// Replaces only the Missions stream with one whose next draw classifies as the
        /// requested delegated outcome for the same frozen numbers. The manual entry then
        /// consumes a board result and the delegated entry a reproducible d100, while the
        /// rest of the frozen request stays identical.
        /// </summary>
        private static SaveData WithSeededMissionOutcome(SaveData data, int firstNinjutsu,
            int secondNinjutsu, int frozenIntelligence, SharedMissionOutcome wanted,
            MissionPreparation preparation = MissionPreparation.None)
        {
            int missionsIndex = -1;
            for (int i = 0; i < data.randomState.streams.Length; i++)
                if (data.randomState.streams[i].streamId ==
                    RandomStateManager.StableId(RandomStreamKind.Missions))
                    missionsIndex = i;
            Assert.That(missionsIndex, Is.GreaterThanOrEqualTo(0),
                "the saved random state must contain the Missions stream.");
            int preparationBonus = TacticalMissionResolutionRules.PreparationBonus(preparation);

            for (ulong seed = 1; seed < 100000UL; seed++)
            {
                RandomStateManager probe = RandomStateManager.Create(seed);
                RandomStateSaveData candidate = probe.CaptureState();
                if (!probe.GetStream(RandomStreamKind.Missions).TryNextInt(
                        TacticalMissionResolutionRules.DieRollExclusiveMax, out int zeroBased))
                    continue;
                int score = TacticalMissionResolutionRules.Score(firstNinjutsu, secondNinjutsu,
                    frozenIntelligence, preparationBonus, zeroBased + 1);
                if (TacticalMissionResolutionRules.Classify(score) != wanted)
                    continue;
                data.randomState.streams[missionsIndex] = candidate.streams[missionsIndex];
                return data;
            }

            Assert.Fail("no Missions seed produces a " + wanted + " delegated result.");
            return data;
        }

        /// <summary>
        /// Plays the authored concealment route: four north, five east, take the dispatch,
        /// then walk both operatives onto the two extraction cells. The caller starts the
        /// mission first.
        /// </summary>
        private static void PlayManualExtraction(GameSession session)
        {
            TacticalMissionSnapshot snapshot = session.TacticalMissionStatus;
            Assert.That(snapshot.IsActive, Is.True);
            PersonId carrier = snapshot.Operatives[0].PersonId;
            PersonId spotter = snapshot.Operatives[1].PersonId;

            MoveTactical(session, carrier, TacticalDirection.North);
            MoveTactical(session, carrier, TacticalDirection.North);
            MoveTactical(session, carrier, TacticalDirection.North);
            MoveTactical(session, carrier, TacticalDirection.North);
            MoveTactical(session, carrier, TacticalDirection.East);
            MoveTactical(session, carrier, TacticalDirection.East);
            MoveTactical(session, carrier, TacticalDirection.East);
            MoveTactical(session, carrier, TacticalDirection.East);
            MoveTactical(session, carrier, TacticalDirection.East);

            Assert.That(session.TacticalMissionStatus.IsActive, Is.True,
                "the authored route must stay concealed from both guards.");
            Assert.That(session.TryTacticalInteract(carrier).Succeeded, Is.True,
                session.LastTacticalMessage);

            MoveTactical(session, carrier, TacticalDirection.West);
            MoveTactical(session, carrier, TacticalDirection.South);
            MoveTactical(session, carrier, TacticalDirection.South);
            MoveTactical(session, carrier, TacticalDirection.South);
            MoveTactical(session, carrier, TacticalDirection.South);
            MoveTactical(session, carrier, TacticalDirection.West);
            MoveTactical(session, carrier, TacticalDirection.West);
            MoveTactical(session, spotter, TacticalDirection.West);
            MoveTactical(session, carrier, TacticalDirection.West);

            Assert.That(session.TryTacticalExtract().Succeeded, Is.True,
                session.LastTacticalMessage);
            Assert.That(session.TacticalMissionStatus.Stage,
                Is.EqualTo(TacticalMissionStage.Succeeded));
        }

        private static void MoveTactical(GameSession session, PersonId operative,
            TacticalDirection direction)
        {
            TacticalActionResult result = session.TryTacticalMove(operative, direction);
            Assert.That(result.Succeeded, Is.True,
                "move " + TacticalMissionScenario.DirectionName(direction) + " failed: " +
                result.Feedback + " | " + session.TacticalMissionStatus.LastEvent);
        }

        /// <summary>
        /// Plays the same authored approach, takes the dispatch, then steps north into the
        /// stationary guard's sight once and breaks contact west and south. The alert turns
        /// the extraction into the costly band; the carrier never shares a cell with a
        /// guard, so nothing can kill or injure the team.
        /// </summary>
        private static void PlayManualAlertedExtraction(GameSession session)
        {
            TacticalMissionSnapshot snapshot = session.TacticalMissionStatus;
            Assert.That(snapshot.IsActive, Is.True);
            PersonId carrier = snapshot.Operatives[0].PersonId;
            PersonId spotter = snapshot.Operatives[1].PersonId;

            MoveTactical(session, carrier, TacticalDirection.North);
            MoveTactical(session, carrier, TacticalDirection.North);
            MoveTactical(session, carrier, TacticalDirection.North);
            MoveTactical(session, carrier, TacticalDirection.North);
            MoveTactical(session, carrier, TacticalDirection.East);
            MoveTactical(session, carrier, TacticalDirection.East);
            MoveTactical(session, carrier, TacticalDirection.East);
            MoveTactical(session, carrier, TacticalDirection.East);
            MoveTactical(session, carrier, TacticalDirection.East);
            Assert.That(session.TryTacticalInteract(carrier).Succeeded, Is.True,
                session.LastTacticalMessage);
            Assert.That(session.TacticalMissionStatus.DispatchTaken, Is.True);

            // Expose the carrier at (5,5), then leave the guard's cone for good.
            MoveTactical(session, carrier, TacticalDirection.North);
            MoveTactical(session, carrier, TacticalDirection.West);
            MoveTactical(session, carrier, TacticalDirection.West);
            MoveTactical(session, carrier, TacticalDirection.South);
            MoveTactical(session, carrier, TacticalDirection.West);
            MoveTactical(session, carrier, TacticalDirection.South);
            MoveTactical(session, carrier, TacticalDirection.South);
            MoveTactical(session, carrier, TacticalDirection.South);
            MoveTactical(session, carrier, TacticalDirection.South);
            MoveTactical(session, spotter, TacticalDirection.West);
            MoveTactical(session, carrier, TacticalDirection.West);

            Assert.That(session.TryTacticalExtract().Succeeded, Is.True,
                session.LastTacticalMessage);
            Assert.That(session.TacticalMissionStatus.Stage,
                Is.EqualTo(TacticalMissionStage.Succeeded));
        }

        /// <summary>
        /// Replaces only the Missions stream with one whose next draw classifies as the
        /// requested delegated outcome for this session's frozen briefing numbers.
        /// </summary>
        private static void SeedDelegatedOutcome(GameSession session,
            SharedMissionOutcome wanted, MissionPreparation preparation = MissionPreparation.None)
        {
            PersonId[] pair = TacticalPair(session);
            Assert.That(session.Development.TryGetValues(pair[0],
                out CharacterGrowthValues firstValues), Is.True);
            Assert.That(session.Development.TryGetValues(pair[1],
                out CharacterGrowthValues secondValues), Is.True);
            SaveData seeded = WithSeededMissionOutcome(session.CaptureSave(),
                firstValues.Ninjutsu, secondValues.Ninjutsu,
                session.Industry.Resources.Intelligence, wanted, preparation);
            SaveOperationResult applied = session.TryApplySave(seeded);
            Assert.That(applied.Succeeded, Is.True, applied.Message);
        }

        /// <summary>Stable Missions stream state, so a rollback can be compared directly.</summary>
        private static string MissionsStreamState(GameSession session)
        {
            RandomStateSaveData state = session.CaptureSave().randomState;
            for (int i = 0; i < state.streams.Length; i++)
                if (state.streams[i].streamId ==
                    RandomStateManager.StableId(RandomStreamKind.Missions))
                    return state.streams[i].state0Hex + "|" + state.streams[i].state1Hex;
            Assert.Fail("the saved random state must contain the Missions stream.");
            return string.Empty;
        }

        private static IndustryState IndustryStateForTest(GameSession session)
        {
            FieldInfo field = typeof(GameSession).GetField("_industryState",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            IndustryState state = (IndustryState)field.GetValue(session);
            Assert.That(state, Is.Not.Null);
            return state;
        }

        private PersonId FirstTacticalOperative()
        {
            IReadOnlyList<CommissionAssigneeOption> options = _session.TacticalOperativeOptions;
            for (int i = 0; i < options.Count; i++)
                if (options[i].Eligible)
                    return options[i].PersonId;
            Assert.Fail("the fixed roster must offer an eligible tactical operative.");
            return default;
        }

        private static PersonId[] TacticalPair(GameSession session)
        {
            var pair = new List<PersonId>(2);
            IReadOnlyList<CommissionAssigneeOption> options = session.TacticalOperativeOptions;
            for (int i = 0; i < options.Count && pair.Count < 2; i++)
                if (options[i].Eligible)
                    pair.Add(options[i].PersonId);
            Assert.That(pair.Count, Is.EqualTo(2),
                "the fixed roster must offer two eligible operatives.");
            return pair.ToArray();
        }

        private sealed class SessionScope : System.IDisposable
        {
            public SessionScope(GameSession session) => Session = session;

            public GameSession Session { get; }

            public void Dispose()
            {
                if (Session != null)
                    Object.DestroyImmediate(Session.gameObject);
            }
        }
    }
}
