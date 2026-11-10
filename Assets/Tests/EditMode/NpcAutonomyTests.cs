using System;
using System.Collections.Generic;
using NUnit.Framework;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;

namespace SengokuChronicle.Tests.EditMode
{
    /// <summary>
    /// M03 deterministic domain coverage: authored profiles, clamped needs, integer
    /// Utility scores, exclusions, stable tie-breaking and the approved durations.
    /// </summary>
    public sealed class NpcAutonomyTests
    {
        [Test]
        public void Profiles_AreTheTwoApprovedFixedPilots()
        {
            Assert.That(NpcAutonomyRules.Profiles.Count, Is.EqualTo(2));
            Assert.That(NpcAutonomyRules.MasatsunaId, Is.EqualTo(M2FixedScenario.MasatsunaId));
            Assert.That(NpcAutonomyRules.NobumasaId, Is.EqualTo(M2FixedScenario.NobumasaId));
            Assert.That(NpcAutonomyRules.Profiles[0].PersonId,
                Is.EqualTo(M2FixedScenario.MasatsunaId));
            Assert.That(NpcAutonomyRules.Profiles[1].PersonId,
                Is.EqualTo(M2FixedScenario.NobumasaId));
            Assert.That(NpcAutonomyRules.IsPilot(M2FixedScenario.HarunobuId), Is.False);
        }

        [Test]
        public void Profiles_UseApprovedWeightsAndInitialNeeds()
        {
            Assert.That(NpcAutonomyRules.TryGetProfile(M2FixedScenario.MasatsunaId,
                out NpcAutonomyProfile masatsuna), Is.True);
            Assert.That(masatsuna.RestWeight, Is.EqualTo(1));
            Assert.That(masatsuna.TrainingWeight, Is.EqualTo(2));
            Assert.That(masatsuna.WorkWeight, Is.EqualTo(1));
            Assert.That(NpcAutonomyRules.InitialNeedsFor(M2FixedScenario.MasatsunaId),
                Is.EqualTo(new NpcNeedValues(40, 75, 45)));

            Assert.That(NpcAutonomyRules.TryGetProfile(M2FixedScenario.NobumasaId,
                out NpcAutonomyProfile nobumasa), Is.True);
            Assert.That(nobumasa.RestWeight, Is.EqualTo(1));
            Assert.That(nobumasa.TrainingWeight, Is.EqualTo(1));
            Assert.That(nobumasa.WorkWeight, Is.EqualTo(2));
            Assert.That(NpcAutonomyRules.InitialNeedsFor(M2FixedScenario.NobumasaId),
                Is.EqualTo(new NpcNeedValues(45, 40, 75)));
        }

        [Test]
        public void NeedValues_GrowClampAndReduceTheMatchingNeedOnly()
        {
            var needs = new NpcNeedValues(98, 99, 100);
            NpcNeedValues grown = needs.WithDailyGrowth();
            Assert.That(grown, Is.EqualTo(new NpcNeedValues(100, 100, 100)));

            Assert.That(new NpcNeedValues(10, 20, 30).WithCompletedAction(DailyActionKind.Rest),
                Is.EqualTo(new NpcNeedValues(0, 20, 30)));
            Assert.That(new NpcNeedValues(10, 20, 30).WithCompletedAction(DailyActionKind.Training),
                Is.EqualTo(new NpcNeedValues(10, 0, 30)));
            Assert.That(new NpcNeedValues(10, 20, 30).WithCompletedAction(DailyActionKind.Work),
                Is.EqualTo(new NpcNeedValues(10, 20, 0)));
            Assert.That(new NpcNeedValues(10, 20, 30).WithCompletedAction(DailyActionKind.Travel),
                Is.EqualTo(new NpcNeedValues(10, 20, 30)));
        }

        [TestCase(-1, 0, 0)]
        [TestCase(0, 101, 0)]
        [TestCase(0, 0, -5)]
        public void NeedValues_RejectOutOfRangeInput(int rest, int learning, int duty)
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new NpcNeedValues(rest, learning, duty));
        }

        [TestCase(100, 0, 0, DailyActionKind.Rest)]
        [TestCase(0, 100, 0, DailyActionKind.Training)]
        [TestCase(0, 0, 100, DailyActionKind.Work)]
        public void Evaluate_EachActionMatchesItsOwnNeed(int rest, int learning, int duty,
            DailyActionKind expected)
        {
            NpcAutonomyEvaluation evaluation = NpcAutonomyRules.Evaluate(
                M2FixedScenario.MasatsunaId, new NpcNeedValues(rest, learning, duty),
                true, true, false);

            Assert.That(evaluation.HasSelection, Is.True);
            Assert.That(evaluation.Selected.Kind, Is.EqualTo(expected));
            Assert.That(evaluation.Candidates.Count, Is.EqualTo(3));
            Assert.That(evaluation.Candidates[0].Kind, Is.EqualTo(DailyActionKind.Rest));
            Assert.That(evaluation.Candidates[1].Kind, Is.EqualTo(DailyActionKind.Training));
            Assert.That(evaluation.Candidates[2].Kind, Is.EqualTo(DailyActionKind.Work));
        }

        [Test]
        public void Evaluate_TieBreaksByActionKindAscending()
        {
            // Nobumasa's 1/1/2 weights make 50/50/25 score 50 for every candidate.
            NpcAutonomyEvaluation evaluation = NpcAutonomyRules.Evaluate(
                M2FixedScenario.NobumasaId, new NpcNeedValues(50, 50, 25), true, true, false);

            Assert.That(evaluation.HasSelection, Is.True);
            Assert.That(evaluation.Selected.Kind, Is.EqualTo(DailyActionKind.Rest),
                "equal scores must choose the lowest DailyActionKind value");
            Assert.That(evaluation.Candidates[0].Score, Is.EqualTo(50));
            Assert.That(evaluation.Candidates[1].Score, Is.EqualTo(50));
            Assert.That(evaluation.Candidates[2].Score, Is.EqualTo(50));
        }

        [Test]
        public void Evaluate_ExcludesUnregisteredUnknownLocationAndActivePilots()
        {
            NpcAutonomyEvaluation unregistered = NpcAutonomyRules.Evaluate(
                M2FixedScenario.HarunobuId, new NpcNeedValues(100, 100, 100), false, true, false);
            Assert.That(unregistered.HasSelection, Is.False);
            AssertCandidateReasons(unregistered, "未登记");

            NpcAutonomyEvaluation unknown = NpcAutonomyRules.Evaluate(
                M2FixedScenario.MasatsunaId, new NpcNeedValues(100, 100, 100), true, false, false);
            Assert.That(unknown.HasSelection, Is.False);
            AssertCandidateReasons(unknown, "地点未知");

            NpcAutonomyEvaluation active = NpcAutonomyRules.Evaluate(
                M2FixedScenario.MasatsunaId, new NpcNeedValues(100, 100, 100), true, true, true);
            Assert.That(active.HasSelection, Is.False);
            AssertCandidateReasons(active, "进行中");
        }

        [Test]
        public void Evaluate_IsIdenticalForIdenticalInputs()
        {
            var needs = new NpcNeedValues(40, 75, 45);
            NpcAutonomyEvaluation first = NpcAutonomyRules.Evaluate(
                M2FixedScenario.MasatsunaId, needs, true, true, false);
            NpcAutonomyEvaluation second = NpcAutonomyRules.Evaluate(
                M2FixedScenario.MasatsunaId, needs, true, true, false);

            Assert.That(second.Selected.Kind, Is.EqualTo(first.Selected.Kind));
            Assert.That(second.Selected.Score, Is.EqualTo(first.Selected.Score));
            Assert.That(second.Summary, Is.EqualTo(first.Summary));
            for (int i = 0; i < first.Candidates.Count; i++)
            {
                Assert.That(second.Candidates[i].Kind, Is.EqualTo(first.Candidates[i].Kind));
                Assert.That(second.Candidates[i].Score, Is.EqualTo(first.Candidates[i].Score));
                Assert.That(second.Candidates[i].IsEligible,
                    Is.EqualTo(first.Candidates[i].IsEligible));
            }
        }

        [TestCase(DailyActionKind.Rest, 1)]
        [TestCase(DailyActionKind.Training, 2)]
        [TestCase(DailyActionKind.Work, 3)]
        public void PlannedDuration_IsApproved(DailyActionKind kind, int expected)
        {
            Assert.That(NpcAutonomyRules.PlannedDuration(kind), Is.EqualTo(expected));
        }

        [Test]
        public void State_KeepsOnlyTheTwoPilotRowsInOrdinalOrder()
        {
            var roster = new[]
            {
                M2FixedScenario.NobumasaId,
                M2FixedScenario.HarunobuId,
                M2FixedScenario.MasatsunaId
            };
            NpcAutonomyState state = NpcAutonomyState.CreateForRoster(roster);

            Assert.That(state.Persons.Count, Is.EqualTo(2));
            Assert.That(state.Persons[0].PersonId, Is.EqualTo(M2FixedScenario.MasatsunaId));
            Assert.That(state.Persons[1].PersonId, Is.EqualTo(M2FixedScenario.NobumasaId));
            Assert.That(state.Contains(M2FixedScenario.HarunobuId), Is.False);
        }

        [Test]
        public void DecisionLog_IsBoundedAndReturnedNewestFirst()
        {
            NpcAutonomyState state = NpcAutonomyState.CreateForRoster(
                new[] { M2FixedScenario.MasatsunaId });
            for (int i = 0; i < NpcAutonomyState.MaxDecisionRecords + 3; i++)
            {
                state.RecordDecision(new NpcAutonomyDecisionRecord(
                    new GameDate(1560, 4, 1 + i), M2FixedScenario.MasatsunaId,
                    NpcAutonomyCommandOutcomeKind.Accepted, DailyActionKind.Training, true,
                    NpcAutonomyTrigger.DailyCheck, "记录 " + i));
            }

            Assert.That(state.Decisions.Count,
                Is.EqualTo(NpcAutonomyState.MaxDecisionRecords));
            Assert.That(state.Decisions[0].Reason, Does.EndWith(
                (NpcAutonomyState.MaxDecisionRecords + 2).ToString()));
        }

        [Test]
        public void Evaluate_ExcludedCandidatesKeepDiagnosticScoresButNeverCompete()
        {
            NpcAutonomyEvaluation active = NpcAutonomyRules.Evaluate(
                M2FixedScenario.MasatsunaId, new NpcNeedValues(40, 75, 45), true, true, true);

            Assert.That(active.HasSelection, Is.False);
            Assert.That(active.Candidates[0].Score, Is.EqualTo(40));
            Assert.That(active.Candidates[1].Score, Is.EqualTo(150));
            Assert.That(active.Candidates[2].Score, Is.EqualTo(45));
            foreach (NpcActionCandidate candidate in active.Candidates)
            {
                Assert.That(candidate.IsEligible, Is.False);
                Assert.That(candidate.Explanation, Does.Contain("进行中"));
                Assert.That(candidate.Explanation, Does.Contain("当前分数"));
            }
        }

        [TestCase(NpcAutonomyCommandOutcomeKind.Accepted, "接受", "执行中")]
        [TestCase(NpcAutonomyCommandOutcomeKind.Deferred, "延期", "空闲")]
        [TestCase(NpcAutonomyCommandOutcomeKind.Rejected, "拒绝", "空闲")]
        [TestCase(NpcAutonomyCommandOutcomeKind.Interrupted, "中断", "被中断")]
        public void DecisionRecord_ExposesOutcomeStatusAndReason(
            NpcAutonomyCommandOutcomeKind outcome, string expectedOutcome,
            string expectedStatus)
        {
            var record = new NpcAutonomyDecisionRecord(new GameDate(1560, 4, 2),
                M2FixedScenario.MasatsunaId, outcome, DailyActionKind.Training, true,
                NpcAutonomyTrigger.DailyCheck, "测试原因。");

            Assert.That(record.OutcomeName, Is.EqualTo(expectedOutcome));
            Assert.That(record.StatusName, Is.EqualTo(expectedStatus));
            Assert.That(record.TriggerName, Is.EqualTo("每日检查"));
            Assert.That(record.Reason, Is.EqualTo("测试原因。"));
            Assert.That(record.HasAction, Is.True);
            Assert.That(record.Action, Is.EqualTo(DailyActionKind.Training));
        }

        [TestCase(NpcAutonomyTrigger.Initialization, "首次初始化")]
        [TestCase(NpcAutonomyTrigger.ActionCompleted, "行动完成")]
        [TestCase(NpcAutonomyTrigger.ActionInterrupted, "行动中断")]
        [TestCase(NpcAutonomyTrigger.EligibilityChanged, "资格变化")]
        [TestCase(NpcAutonomyTrigger.DailyCheck, "每日检查")]
        public void TriggerNames_AreStableChineseLabels(NpcAutonomyTrigger trigger,
            string expected)
        {
            Assert.That(NpcAutonomyRules.TriggerName(trigger), Is.EqualTo(expected));
        }

        [Test]
        public void RecordMemory_ReplacesOnlySameKindAndKeepsStableOrder()
        {
            NpcAutonomyState state = NpcAutonomyState.CreateForRoster(
                new[] { M2FixedScenario.MasatsunaId });
            GameDate first = new GameDate(1560, 4, 1);
            GameDate second = new GameDate(1560, 4, 2);
            GameDate third = new GameDate(1560, 4, 3);

            Assert.That(state.RecordMemory(M2FixedScenario.MasatsunaId,
                NpcActionMemoryKind.Completed, DailyActionKind.Rest, first, string.Empty),
                Is.True);
            Assert.That(state.RecordMemory(M2FixedScenario.MasatsunaId,
                NpcActionMemoryKind.Interrupted, DailyActionKind.Work, second, "首次中断。"),
                Is.True);
            Assert.That(state.RecordMemory(M2FixedScenario.MasatsunaId,
                NpcActionMemoryKind.Completed, DailyActionKind.Training, third, string.Empty),
                Is.True);

            NpcAutonomyPersonState person = state.Persons[0];
            Assert.That(person.Memories, Has.Count.EqualTo(2),
                "one completed and one interrupted memory are kept, never three");
            Assert.That(person.Memories[0].Kind, Is.EqualTo(NpcActionMemoryKind.Completed),
                "completed memory is always first");
            Assert.That(person.Memories[0].Action, Is.EqualTo(DailyActionKind.Training),
                "a newer completed result replaces the older completed result");
            Assert.That(person.Memories[0].Date, Is.EqualTo(third));
            Assert.That(person.Memories[1].Kind, Is.EqualTo(NpcActionMemoryKind.Interrupted));
            Assert.That(person.Memories[1].Action, Is.EqualTo(DailyActionKind.Work),
                "replacing completed memory must not overwrite interrupted memory");
            Assert.That(person.Memories[1].Reason, Is.EqualTo("首次中断。"));
        }

        [Test]
        public void Evaluate_CompletedMemoryAddsOnlyToSameAction()
        {
            var needs = new NpcNeedValues(40, 75, 45);
            var memory = new NpcActionMemory(M2FixedScenario.MasatsunaId,
                NpcActionMemoryKind.Completed, DailyActionKind.Training,
                new GameDate(1560, 4, 1), string.Empty);

            NpcAutonomyEvaluation evaluation = NpcAutonomyRules.Evaluate(
                M2FixedScenario.MasatsunaId, needs, new[] { memory },
                new GameDate(1560, 4, 10), true, true, false);

            Assert.That(evaluation.Candidates[0].BaseScore, Is.EqualTo(40));
            Assert.That(evaluation.Candidates[0].MemoryAdjustment, Is.Zero);
            Assert.That(evaluation.Candidates[0].FinalScore, Is.EqualTo(40));
            Assert.That(evaluation.Candidates[1].BaseScore, Is.EqualTo(150));
            Assert.That(evaluation.Candidates[1].MemoryAdjustment,
                Is.EqualTo(NpcAutonomyRules.CompletedMemoryAdjustment));
            Assert.That(evaluation.Candidates[1].FinalScore, Is.EqualTo(162));
            Assert.That(evaluation.Candidates[2].BaseScore, Is.EqualTo(45));
            Assert.That(evaluation.Candidates[2].MemoryAdjustment, Is.Zero);
            Assert.That(evaluation.Selected.Kind, Is.EqualTo(DailyActionKind.Training));
            Assert.That(evaluation.Selected.Explanation, Does.Contain("基础分数 150"));
            Assert.That(evaluation.Selected.Explanation, Does.Contain("记忆修正 +12"));
            Assert.That(evaluation.Selected.Explanation, Does.Contain("最终分数 162"));
            Assert.That(evaluation.Selected.Explanation, Does.Contain("完成记忆"));
            Assert.That(evaluation.Selected.Explanation, Does.Contain("1560-04-01"));
        }

        [Test]
        public void Evaluate_InterruptedMemorySubtractsOnlyFromSameAction()
        {
            var needs = new NpcNeedValues(40, 75, 45);
            var memory = new NpcActionMemory(M2FixedScenario.MasatsunaId,
                NpcActionMemoryKind.Interrupted, DailyActionKind.Work,
                new GameDate(1560, 4, 1), "行动被中断。");

            NpcAutonomyEvaluation evaluation = NpcAutonomyRules.Evaluate(
                M2FixedScenario.MasatsunaId, needs, new[] { memory },
                new GameDate(1560, 4, 10), true, true, false);

            Assert.That(evaluation.Candidates[0].FinalScore, Is.EqualTo(40));
            Assert.That(evaluation.Candidates[1].FinalScore, Is.EqualTo(150));
            Assert.That(evaluation.Candidates[2].BaseScore, Is.EqualTo(45));
            Assert.That(evaluation.Candidates[2].MemoryAdjustment,
                Is.EqualTo(NpcAutonomyRules.InterruptedMemoryAdjustment));
            Assert.That(evaluation.Candidates[2].FinalScore, Is.EqualTo(33));
            Assert.That(evaluation.Selected.Kind, Is.EqualTo(DailyActionKind.Training));

            var completedAndInterrupted = new[]
            {
                new NpcActionMemory(M2FixedScenario.MasatsunaId,
                    NpcActionMemoryKind.Completed, DailyActionKind.Work,
                    new GameDate(1560, 4, 1), string.Empty),
                memory
            };
            NpcAutonomyEvaluation mixed = NpcAutonomyRules.Evaluate(
                M2FixedScenario.MasatsunaId, needs, completedAndInterrupted,
                new GameDate(1560, 4, 10), true, true, false);
            Assert.That(mixed.Candidates[2].MemoryAdjustment, Is.Zero,
                "a completed and an interrupted memory of the same action cancel out");
            Assert.That(mixed.Candidates[2].FinalScore, Is.EqualTo(45));
        }

        [Test]
        public void MemoryEffect_IsActiveThroughDay30AndExpiredOnDay31()
        {
            var memory = new NpcActionMemory(M2FixedScenario.NobumasaId,
                NpcActionMemoryKind.Completed, DailyActionKind.Work,
                new GameDate(1560, 4, 1), string.Empty);

            Assert.That(NpcAutonomyRules.IsMemoryActive(memory, new GameDate(1560, 4, 30)),
                Is.True);
            Assert.That(NpcAutonomyRules.IsMemoryActive(memory, new GameDate(1560, 5, 1)),
                Is.True, "the 30th following day still applies across a month boundary");
            Assert.That(NpcAutonomyRules.IsMemoryActive(memory, new GameDate(1560, 5, 2)),
                Is.False, "the 31st following day no longer affects scoring");

            NpcAutonomyEvaluation expired = NpcAutonomyRules.Evaluate(
                M2FixedScenario.NobumasaId, new NpcNeedValues(45, 40, 75),
                new[] { memory }, new GameDate(1560, 5, 2), true, true, false);
            Assert.That(expired.Candidates[2].MemoryAdjustment, Is.Zero);
            Assert.That(expired.Candidates[2].Explanation, Does.Contain("已过期"));
        }

        [Test]
        public void MemoryEffect_HandlesLeapAndDateMaximumWithoutOverflow()
        {
            var leap = new NpcActionMemory(M2FixedScenario.MasatsunaId,
                NpcActionMemoryKind.Completed, DailyActionKind.Rest,
                new GameDate(1560, 2, 1), string.Empty);
            Assert.That(GameDate.TryAddDays(leap.Date, 30, out GameDate leapBoundary));
            Assert.That(leapBoundary, Is.EqualTo(new GameDate(1560, 3, 2)));
            Assert.That(NpcAutonomyRules.IsMemoryActive(leap, leapBoundary), Is.True);
            Assert.That(GameDate.TryNext(leapBoundary, out GameDate leapExpired));
            Assert.That(NpcAutonomyRules.IsMemoryActive(leap, leapExpired), Is.False);

            var atMaximum = new NpcActionMemory(M2FixedScenario.MasatsunaId,
                NpcActionMemoryKind.Interrupted, DailyActionKind.Training,
                new GameDate(GameDate.MaxYear, 12, 31), "范围上限。");
            Assert.That(NpcAutonomyRules.IsMemoryActive(atMaximum,
                new GameDate(GameDate.MaxYear, 12, 31)), Is.True,
                "the supported maximum date must not overflow a 30-day window");

            var nearMaximum = new NpcActionMemory(M2FixedScenario.MasatsunaId,
                NpcActionMemoryKind.Completed, DailyActionKind.Rest,
                new GameDate(GameDate.MaxYear, 12, 2), string.Empty);
            Assert.That(NpcAutonomyRules.IsMemoryActive(nearMaximum,
                new GameDate(GameDate.MaxYear, 12, 31)), Is.True,
                "remaining supported days still count inside the 30-day window");
        }

        private static void AssertCandidateReasons(NpcAutonomyEvaluation evaluation,
            string expected)
        {
            foreach (NpcActionCandidate candidate in evaluation.Candidates)
            {
                Assert.That(candidate.IsEligible, Is.False);
                Assert.That(candidate.Score, Is.GreaterThanOrEqualTo(0));
                Assert.That(candidate.Explanation, Does.Contain(expected));
            }
        }
    }
}
