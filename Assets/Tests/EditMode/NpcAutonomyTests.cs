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

        private static void AssertCandidateReasons(NpcAutonomyEvaluation evaluation,
            string expected)
        {
            foreach (NpcActionCandidate candidate in evaluation.Candidates)
            {
                Assert.That(candidate.IsEligible, Is.False);
                Assert.That(candidate.Score, Is.Zero);
                Assert.That(candidate.Explanation, Does.Contain(expected));
            }
        }
    }
}
