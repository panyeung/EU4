using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;
using SengokuChronicle.Editor;
using SengokuChronicle.Events;
using SengokuChronicle.Persistence;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace SengokuChronicle.Tests.EditMode
{
    /// <summary>
    /// M08B Task 4: the long-term runner must drive real SC-013 sessions, preserve every
    /// stop reason, expose GDD 14.2 metrics with explicit denominators, and serialize
    /// normalized artifacts byte-identically for the same seed range.
    /// </summary>
    public sealed class M08BLongTermSimulationTests
    {
        private static readonly string[] NewSc018EventIds =
        {
            "event.sc018.sibling-duty",
            "event.sc018.child-study",
            "event.sc018.parent-care",
            "event.sc018.heirloom",
            "event.sc018.spouse-visit",
            "event.sc018.adoption-welcome",
            "event.sc018.family-assembly"
        };

        private string _root;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(M08BLongTermSimulationRunner.DefaultOutputRoot, "tests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_root);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_root))
                Directory.Delete(_root, true);
        }

        [Test]
        public void RunSeed_IsolatesSeedsAndRepeatsIdenticalRows()
        {
            int[] years = { 1 };
            M08BSeedResult first = M08BLongTermSimulationRunner.RunSeed(41UL, 1, years);
            M08BSeedResult other = M08BLongTermSimulationRunner.RunSeed(42UL, 1, years);
            M08BSeedResult repeat = M08BLongTermSimulationRunner.RunSeed(41UL, 1, years);

            Assert.That(first.InitialRandomStateHex, Is.Not.Empty);
            Assert.That(first.InitialRandomStateHex, Is.EqualTo(repeat.InitialRandomStateHex));
            Assert.That(first.InitialRandomStateHex, Is.Not.EqualTo(other.InitialRandomStateHex),
                "different seeds must never share the captured initial random state");

            string firstCsv = M08BLongTermSimulationRunner.SerializeSeedsCsv(
                new[] { first }, years);
            string repeatCsv = M08BLongTermSimulationRunner.SerializeSeedsCsv(
                new[] { repeat }, years);
            Assert.That(repeatCsv, Is.EqualTo(firstCsv),
                "the same seed must serialize byte-identically even after another seed ran");
        }

        [Test]
        public void EventResolverChoosesSmallestAvailableOptionId()
        {
            var holder = new GameObject("M08BEventResolverTest");
            try
            {
                GameSession session = holder.AddComponent<GameSession>();
                session.InitNewGame(123UL);
                for (int month = 0; month < 12 && string.IsNullOrEmpty(session.PendingEventId); month++)
                {
                    bool advanced = session.TryAdvanceToNextMonth();
                    if (!advanced)
                        break;
                }

                string eventId = session.PendingEventId;
                Assert.That(eventId, Is.Not.Empty, "a real SC-013 run must queue an event within a year");

                EventDefinitionData definition = session.EventCatalog.events
                    .First(item => item.eventId == eventId);
                string expected = definition.options
                    .Where(option =>
                    {
                        bool found = session.TryGetEventOptionAvailability(eventId, option.optionId,
                            out bool enabled, out _);
                        return found && enabled;
                    })
                    .Select(option => option.optionId)
                    .OrderBy(value => value, StringComparer.Ordinal)
                    .First();

                var choices = new List<string>();
                Assert.That(M08BLongTermSimulationRunner.TryResolvePendingEvents(session, choices,
                    out string failure), Is.True, failure);
                Assert.That(choices, Has.Count.EqualTo(1));
                Assert.That(choices[0], Is.EqualTo(eventId + "=" + expected));
                Assert.That(session.PendingEventId, Is.Empty);
            }
            finally
            {
                Object.DestroyImmediate(holder);
            }
        }

        [Test]
        public void DeathSuccessionIsResolvedByTheApprovedPolicyAndTheRunContinues()
        {
            var holder = new GameObject("M08BPendingSuccessionTest");
            try
            {
                GameSession session = holder.AddComponent<GameSession>();
                session.InitNewGame(7UL);
                PersonId head = session.Succession.HeadPersonId;
                Assert.That(session.Succession.World.TryGetLife(head, out SuccessionPersonLife life),
                    Is.True);
                Assert.That(session.Succession.World.TrySetLife(head,
                    new SuccessionPersonLife(life.BirthYear, false, life.Retired), out string lifeError),
                    Is.True, lifeError);
                SuccessionResult result = SuccessionService.BeginDeathSuccession(
                    session.Succession, session.WorldClock.CurrentDate);
                Assert.That(result.Succeeded, Is.True, result.Message);
                Assert.That(session.Succession.IsPending, Is.True);

                M08BSeedResult run = M08BLongTermSimulationRunner.RunExistingSession(
                    session, 7UL, 1, new[] { 1 });

                Assert.That(session.Succession.IsPending, Is.False,
                    "the approved policy must settle a pending succession");
                Assert.That(session.Succession.SuccessionCount, Is.EqualTo(1));
                Assert.That(session.Succession.HeadPersonId, Is.Not.EqualTo(head));
                Assert.That(run.SuccessionDecisions.Any(decision =>
                    decision.StartsWith("successor=", StringComparison.Ordinal)), Is.True);
                Assert.That(run.Termination, Is.Not.EqualTo(M08BTerminationReason.PendingSuccession),
                    "a resolved succession must not be reported as a blocking stop");
                Assert.That(run.ProgressionDeadlocks, Is.EqualTo(0));
            }
            finally
            {
                Object.DestroyImmediate(holder);
            }
        }

        [Test]
        public void CrisisDecisionKeepsThePendingSuccessionStop()
        {
            var holder = new GameObject("M08BCrisisDecisionTest");
            try
            {
                GameSession session = holder.AddComponent<GameSession>();
                session.InitNewGame(7UL);
                Assert.That(session.StartCrisisDemoScenario(), Is.True);
                PersonId crisisHead = session.Succession.HeadPersonId;
                Assert.That(session.Succession.World.TryGetLife(crisisHead,
                    out SuccessionPersonLife crisisLife), Is.True);
                Assert.That(session.Succession.World.TrySetLife(crisisHead,
                    new SuccessionPersonLife(crisisLife.BirthYear, false, crisisLife.Retired),
                    out string crisisError), Is.True, crisisError);
                Assert.That(SuccessionService.BeginDeathSuccession(session.Succession,
                    session.WorldClock.CurrentDate).Succeeded, Is.True);
                Assert.That(session.Succession.Phase,
                    Is.EqualTo(SuccessionPhase.CrisisDecision));

                M08BSeedResult run = M08BLongTermSimulationRunner.RunExistingSession(
                    session, 7UL, 1, new[] { 1 });

                Assert.That(run.Termination, Is.EqualTo(M08BTerminationReason.PendingSuccession));
                Assert.That(run.TerminationDetail, Does.Contain("CrisisDecision"));
                Assert.That(run.ProgressionDeadlocks, Is.EqualTo(0),
                    "a rule-required succession decision is a stop category, not a deadlock");
                Assert.That(run.EventDeadlocks, Is.EqualTo(0));
                Assert.That(run.SuccessionDecisions, Is.Empty,
                    "the simulation must never accept a crisis adoption");
                Assert.That(session.Succession.Phase,
                    Is.EqualTo(SuccessionPhase.CrisisDecision));
            }
            finally
            {
                Object.DestroyImmediate(holder);
            }
        }

        [Test]
        public void ExtinctionKeepsItsOwnStopCategory()
        {
            var extinctHolder = new GameObject("M08BExtinctionTest");
            try
            {
                GameSession session = extinctHolder.AddComponent<GameSession>();
                session.InitNewGame(7UL);
                foreach (FamilyMembership membership in session.Succession.World.Family.Members)
                {
                    Assert.That(session.Succession.World.TryGetLife(membership.PersonId,
                        out SuccessionPersonLife life), Is.True);
                    Assert.That(session.Succession.World.TrySetLife(membership.PersonId,
                        new SuccessionPersonLife(life.BirthYear, false, life.Retired), out string error),
                        Is.True, error);
                }
                SuccessionResult result = SuccessionService.BeginDeathSuccession(
                    session.Succession, session.WorldClock.CurrentDate);
                Assert.That(result.Succeeded, Is.True, result.Message);
                Assert.That(session.IsFamilyExtinct, Is.True);

                M08BSeedResult run = M08BLongTermSimulationRunner.RunExistingSession(
                    session, 7UL, 1, new[] { 1 });
                Assert.That(run.Termination, Is.EqualTo(M08BTerminationReason.Extinction));
                Assert.That(run.ProgressionDeadlocks, Is.EqualTo(0));
            }
            finally
            {
                Object.DestroyImmediate(extinctHolder);
            }
        }

        [Test]
        public void SuccessionChoicePrefersTheValidDesignatedHeir()
        {
            var designated = new SuccessionCandidate(new PersonId("person.c"), "C", 30, true,
                FamilyMemberRole.DirectKin, true);
            var candidates = new[]
            {
                new SuccessionCandidate(new PersonId("person.a"), "A", 20, true,
                    FamilyMemberRole.CollateralKin, false),
                new SuccessionCandidate(new PersonId("person.b"), "B", 40, true,
                    FamilyMemberRole.DirectKin, false),
                designated
            };

            Assert.That(M08BLongTermSimulationRunner.TryChooseSuccessor(candidates,
                new PersonId("person.c"), out SuccessionCandidate chosen), Is.True);
            Assert.That(chosen.PersonId, Is.EqualTo(new PersonId("person.c")),
                "a valid designated heir wins even when it is not the lowest stable ID");

            Assert.That(M08BLongTermSimulationRunner.TryChooseSuccessor(candidates,
                new PersonId("person.retired"), out chosen), Is.True);
            Assert.That(chosen.PersonId, Is.EqualTo(new PersonId("person.a")),
                "an invalid designation falls back to the lowest stable ID");
        }

        [Test]
        public void SuccessionFallbackUsesTheLowestStablePersonIdRegardlessOfOrder()
        {
            var first = new SuccessionCandidate(new PersonId("person.a"), "A", 20, true,
                FamilyMemberRole.DirectKin, false);
            var second = new SuccessionCandidate(new PersonId("person.b"), "B", 30, true,
                FamilyMemberRole.DirectKin, false);
            var third = new SuccessionCandidate(new PersonId("person.c"), "C", 40, true,
                FamilyMemberRole.DirectKin, false);

            Assert.That(M08BLongTermSimulationRunner.TryChooseSuccessor(
                new[] { first, second, third }, default, out SuccessionCandidate forward), Is.True);
            Assert.That(M08BLongTermSimulationRunner.TryChooseSuccessor(
                new[] { third, second, first }, default, out SuccessionCandidate reversed), Is.True);
            Assert.That(M08BLongTermSimulationRunner.TryChooseSuccessor(
                new[] { second, third, first }, default, out SuccessionCandidate shuffled), Is.True);

            Assert.That(forward.PersonId, Is.EqualTo(new PersonId("person.a")));
            Assert.That(reversed.PersonId, Is.EqualTo(forward.PersonId),
                "the pick must not depend on enumeration order");
            Assert.That(shuffled.PersonId, Is.EqualTo(forward.PersonId));
            Assert.That(M08BLongTermSimulationRunner.TryChooseSuccessor(
                Array.Empty<SuccessionCandidate>(), default, out _), Is.False);
        }

        [Test]
        public void SuccessionPolicyAdvancesThroughMultipleGenerations()
        {
            var holder = new GameObject("M08BMultiGenerationTest");
            try
            {
                GameSession session = holder.AddComponent<GameSession>();
                session.InitNewGame(7UL);
                var decisions = new List<string>();
                var heads = new List<PersonId> { session.Succession.HeadPersonId };

                for (int generation = 1; generation <= 2; generation++)
                {
                    PersonId head = session.Succession.HeadPersonId;
                    Assert.That(session.Succession.World.TryGetLife(head,
                        out SuccessionPersonLife life), Is.True);
                    Assert.That(session.Succession.World.TrySetLife(head,
                        new SuccessionPersonLife(life.BirthYear, false, life.Retired),
                        out string lifeError), Is.True, lifeError);
                    SuccessionResult started = SuccessionService.BeginDeathSuccession(
                        session.Succession, session.WorldClock.CurrentDate);
                    Assert.That(started.Succeeded, Is.True, started.Message);

                    Assert.That(M08BLongTermSimulationRunner.TryResolveSuccession(session,
                        decisions, out string failure), Is.True, failure);
                    Assert.That(session.Succession.Phase, Is.EqualTo(SuccessionPhase.None));
                    Assert.That(session.Succession.SuccessionCount, Is.EqualTo(generation));
                    Assert.That(heads.Any(existing =>
                            existing == session.Succession.HeadPersonId), Is.False,
                        "each generation must hand over to a new head");
                    heads.Add(session.Succession.HeadPersonId);
                }

                Assert.That(decisions.Count(decision =>
                    decision.StartsWith("successor=", StringComparison.Ordinal)), Is.EqualTo(2));
            }
            finally
            {
                Object.DestroyImmediate(holder);
            }
        }

        [Test]
        public void MinorHeirUsesTheLowestStableIdRegentAndCompletesRegency()
        {
            var holder = new GameObject("M08BRegencyPolicyTest");
            try
            {
                GameSession session = holder.AddComponent<GameSession>();
                session.InitNewGame(7UL);
                PersonId heir = session.Succession.DesignatedHeirId;
                Assert.That(heir.IsEmpty, Is.False, "SC-013 must designate an heir");
                Assert.That(session.Succession.World.TryGetLife(heir,
                    out SuccessionPersonLife heirLife), Is.True);
                Assert.That(session.Succession.World.TrySetLife(heir,
                    new SuccessionPersonLife(session.WorldClock.CurrentDate.Year - 5, true, false),
                    out string heirError), Is.True, heirError);

                string expectedRegent = session.SuccessionRegentCandidates
                    .Select(candidate => candidate.PersonId.Value)
                    .OrderBy(value => value, StringComparer.Ordinal)
                    .First();
                PersonId head = session.Succession.HeadPersonId;
                Assert.That(session.Succession.World.TryGetLife(head,
                    out SuccessionPersonLife headLife), Is.True);
                Assert.That(session.Succession.World.TrySetLife(head,
                    new SuccessionPersonLife(headLife.BirthYear, false, headLife.Retired),
                    out string headError), Is.True, headError);
                Assert.That(SuccessionService.BeginDeathSuccession(session.Succession,
                    session.WorldClock.CurrentDate).Succeeded, Is.True);

                var decisions = new List<string>();
                Assert.That(M08BLongTermSimulationRunner.TryResolveSuccession(session, decisions,
                    out string failure), Is.True, failure);
                Assert.That(session.Succession.Phase, Is.EqualTo(SuccessionPhase.Regency));
                Assert.That(session.Succession.HeadPersonId, Is.EqualTo(heir));
                Assert.That(session.Succession.PendingRegentId.Value, Is.EqualTo(expectedRegent),
                    "the regent must be the lowest stable PersonId candidate");
                Assert.That(decisions.Any(decision =>
                    decision.StartsWith("regent=", StringComparison.Ordinal)), Is.True);

                int guard = 0;
                while (!session.Succession.IsRegencyHandoffDue(session.WorldClock.CurrentDate) &&
                       guard++ < 9000)
                    Assert.That(session.WorldClock.AdvanceOneDay(), Is.True);
                Assert.That(session.Succession.IsRegencyHandoffDue(session.WorldClock.CurrentDate),
                    Is.True, "the minor head must reach adulthood inside the guarded window");

                Assert.That(M08BLongTermSimulationRunner.TryResolveSuccession(session, decisions,
                    out failure), Is.True, failure);
                Assert.That(session.Succession.Phase, Is.EqualTo(SuccessionPhase.None));
                Assert.That(session.Succession.RegencyCompleted, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(holder);
            }
        }

        [Test]
        public void ClassifyStopPreservesEveryTerminationCategory()
        {
            Assert.That(M08BLongTermSimulationRunner.ClassifyStop(true, false, false, false, false, 0),
                Is.EqualTo(M08BTerminationReason.Completed));
            Assert.That(M08BLongTermSimulationRunner.ClassifyStop(false, true, false, false, false, 0),
                Is.EqualTo(M08BTerminationReason.Extinction));
            Assert.That(M08BLongTermSimulationRunner.ClassifyStop(false, false, true, false, false, 0),
                Is.EqualTo(M08BTerminationReason.PendingSuccession));
            Assert.That(M08BLongTermSimulationRunner.ClassifyStop(false, false, false, true, false, 0),
                Is.EqualTo(M08BTerminationReason.UnresolvedBlockingEvent));
            Assert.That(M08BLongTermSimulationRunner.ClassifyStop(false, false, false, false, true, 0),
                Is.EqualTo(M08BTerminationReason.DateOverflow));
            Assert.That(M08BLongTermSimulationRunner.ClassifyStop(false, false, false, false, false,
                    M08BLongTermSimulationRunner.MaxConsecutiveBlockedAttempts),
                Is.EqualTo(M08BTerminationReason.Timeout));
        }

        [Test]
        public void RealRunCapturesCheckpointMetricsAndWealth()
        {
            M08BSeedResult result = null;
            for (ulong seed = 1; seed <= 8 && result == null; seed++)
            {
                M08BSeedResult candidate = M08BLongTermSimulationRunner.RunSeed(seed, 1, new[] { 1 });
                M08BCheckpointResult checkpoint = candidate.Checkpoints.Single(item => item.Year == 1);
                if (checkpoint.Reached && checkpoint.Alive)
                    result = candidate;
            }

            Assert.That(result, Is.Not.Null, "at least one one-year seed must still be alive");
            M08BCheckpointResult captured = result.Checkpoints.Single(item => item.Year == 1);
            Assert.That(captured.MonthsRequired, Is.EqualTo(12));
            Assert.That(captured.LivingMembers, Is.GreaterThan(0));
            Assert.That(captured.MeanLivingAgeTenths, Is.GreaterThan(0));
            Assert.That(captured.FoodPriceIndexPermille,
                Is.InRange(MarketState.MinimumPriceIndexPermille, MarketState.MaximumPriceIndexPermille));
            Assert.That(captured.Wealth, Is.EqualTo(M08BLongTermSimulationRunner.ComputeWealth(
                captured.Funds, captured.FoodInventory, captured.FoodPriceIndexPermille,
                captured.Supplies, captured.MaterialsPriceIndexPermille)));
            Assert.That(captured.EventCount, Is.EqualTo(result.EventsTriggered));
            Assert.That(captured.EventsPer1000Months, Is.EqualTo(
                M08BLongTermSimulationRunner.RoundHalfUp((long)captured.EventCount * 1000L, 12)));
        }

        [Test]
        public void SummaryUsesNearestRankWealthQuantilesAndCountsEmptyCheckpoints()
        {
            int[] years = { 10, 25 };
            var seeds = new List<M08BSeedResult>
            {
                SyntheticSeed(1, 10, true, 1, 100),
                SyntheticSeed(2, 10, true, 2, 200),
                SyntheticSeed(3, 10, true, 3, 300),
                SyntheticSeed(4, 10, false, 0, 0),
                SyntheticSeed(5, 10, false, 0, 0)
            };
            seeds[0].Checkpoints[0].EconomicCollapse = true;
            seeds[1].Checkpoints[0].ExtremeInflation = true;

            string json = M08BLongTermSimulationRunner.BuildSummaryJson(seeds, 1, 5, 100, years);

            StringAssert.Contains("\"reached\":5", json);
            StringAssert.Contains("\"notReached\":0", json);
            StringAssert.Contains("\"emptyCheckpoints\":2", json);
            StringAssert.Contains(
                "\"wealth\":{\"sample\":3,\"min\":100,\"max\":300,\"mean\":200," +
                "\"p10\":100,\"p25\":100,\"p50\":200,\"p75\":300,\"p90\":300}", json);
            StringAssert.Contains(
                "\"population\":{\"sample\":5,\"min\":0,\"max\":3,\"mean\":1," +
                "\"p10\":0,\"p25\":0,\"p50\":1,\"p75\":2,\"p90\":3", json);
            StringAssert.Contains("\"members\":0,\"count\":2", json);
            StringAssert.Contains(
                "\"economicCollapse\":{\"numerator\":1,\"denominator\":5,\"permille\":200}", json);
            StringAssert.Contains(
                "\"extremeInflation\":{\"numerator\":1,\"denominator\":5,\"permille\":200}", json);
            StringAssert.Contains("\"year\":25,\"monthsRequired\":300,\"reached\":0,\"notReached\":5",
                json);
            StringAssert.Contains("\"denominator\":5", json);
        }

        [Test]
        public void CsvAndJsonSerializationIsStableAndRoundTrips()
        {
            int[] years = { 1 };
            var seeds = new List<M08BSeedResult>
            {
                M08BLongTermSimulationRunner.RunSeed(3UL, 1, years),
                M08BLongTermSimulationRunner.RunSeed(4UL, 1, years)
            };

            string csv = M08BLongTermSimulationRunner.SerializeSeedsCsv(seeds, years);
            Assert.That(M08BLongTermSimulationRunner.SerializeSeedsCsv(seeds, years),
                Is.EqualTo(csv));
            string checkpointCsv = M08BLongTermSimulationRunner.SerializeCheckpointsCsv(seeds, years);
            Assert.That(M08BLongTermSimulationRunner.SerializeCheckpointsCsv(seeds, years),
                Is.EqualTo(checkpointCsv));
            string json = M08BLongTermSimulationRunner.BuildSummaryJson(seeds, 3, 2, 1, years);
            Assert.That(M08BLongTermSimulationRunner.BuildSummaryJson(seeds, 3, 2, 1, years),
                Is.EqualTo(json));
            Assert.That(csv, Does.Not.Contain("duration"));
            Assert.That(json, Does.Not.Contain("duration"));
            Assert.That(json, Does.Not.Contain("utcStart"));

            Assert.That(M08BLongTermSimulationRunner.TryParseSeedsCsv(csv, 3, 2, 1, years,
                out List<M08BSeedResult> parsed, out string error), Is.True, error);
            Assert.That(parsed, Has.Count.EqualTo(2));
            Assert.That(parsed.Select(item => item.Seed), Is.EqualTo(new ulong[] { 3, 4 }));
            Assert.That(M08BLongTermSimulationRunner.SerializeSeedsCsv(parsed, years),
                Is.EqualTo(csv));
        }

        [Test]
        public void BatchResumeKeepsCompletedRowsAndNeverRewritesAnotherRange()
        {
            int[] years = { 1 };
            M08BLongTermSimulationRunner.RunBatch(1, 2, 1, _root, years);
            string seedsPath = Path.Combine(_root, "seeds-1-2-1y.csv");
            string summaryPath = Path.Combine(_root, "summary-1-2-1y.json");
            string firstCsv = File.ReadAllText(seedsPath);
            string firstSummary = File.ReadAllText(summaryPath);

            M08BLongTermSimulationRunner.RunBatch(1, 2, 1, _root, years);
            Assert.That(File.ReadAllText(seedsPath), Is.EqualTo(firstCsv),
                "rerunning a completed range must not change normalized rows");
            Assert.That(File.ReadAllText(summaryPath), Is.EqualTo(firstSummary));
            StringAssert.Contains("reused=2 ran=0", File.ReadAllText(
                Path.Combine(_root, "batch-1-2-1y.log")));

            M08BLongTermSimulationRunner.RunBatch(1, 3, 1, _root, years);
            StringAssert.Contains("reused=2 ran=1", File.ReadAllText(
                Path.Combine(_root, "batch-1-3-1y.log")));
            string[] before = firstCsv.Split('\n');
            string[] after = File.ReadAllText(Path.Combine(_root, "seeds-1-3-1y.csv")).Split('\n');
            Assert.That(after[0], Is.EqualTo(before[0]));
            Assert.That(after[1], Is.EqualTo(before[1]));
            Assert.That(after[2], Is.EqualTo(before[2]));
            Assert.That(File.ReadAllText(seedsPath), Is.EqualTo(firstCsv),
                "a wider range must not overwrite the completed narrower range");
        }

        [Test]
        public void EventFrequencyRatioUsesObservedEventsAndMonths()
        {
            int[] years = { 1 };
            M08BCheckpointResult checkpoint = null;
            M08BSeedResult result = null;
            for (ulong seed = 1; seed <= 8 && checkpoint == null; seed++)
            {
                M08BSeedResult candidate = M08BLongTermSimulationRunner.RunSeed(seed, 1, years);
                M08BCheckpointResult candidateCheckpoint =
                    candidate.Checkpoints.Single(item => item.Year == 1);
                if (candidateCheckpoint.Reached)
                {
                    result = candidate;
                    checkpoint = candidateCheckpoint;
                }
            }

            Assert.That(checkpoint, Is.Not.Null, "at least one one-year seed must reach the checkpoint");
            Assert.That(checkpoint.EventCount, Is.EqualTo(result.EventsTriggered));
            Assert.That(checkpoint.EventsPer1000Months,
                Is.EqualTo(M08BLongTermSimulationRunner.RoundHalfUp(
                    (long)checkpoint.EventCount * 1000L, checkpoint.MonthsRequired)));
        }

        [Test]
        public void PercentileNearestRankMatchesTheDocumentedLabel()
        {
            var three = new[] { 100, 200, 300 };
            Assert.That(M08BLongTermSimulationRunner.PercentileNearestRank(three, 0),
                Is.EqualTo(100), "p0 is the minimum");
            Assert.That(M08BLongTermSimulationRunner.PercentileNearestRank(three, 10),
                Is.EqualTo(100));
            Assert.That(M08BLongTermSimulationRunner.PercentileNearestRank(three, 25),
                Is.EqualTo(100), "rank = ceil(3 * 25 / 100) = 1");
            Assert.That(M08BLongTermSimulationRunner.PercentileNearestRank(three, 50),
                Is.EqualTo(200), "rank = ceil(3 * 50 / 100) = 2");
            Assert.That(M08BLongTermSimulationRunner.PercentileNearestRank(three, 75),
                Is.EqualTo(300), "rank = ceil(3 * 75 / 100) = 3");
            Assert.That(M08BLongTermSimulationRunner.PercentileNearestRank(three, 100),
                Is.EqualTo(300), "p100 is the maximum");

            var five = new[] { 0, 0, 1, 2, 3 };
            for (int percentile = 0; percentile <= 100; percentile++)
            {
                int rank = (five.Length * percentile + 99) / 100;
                rank = Math.Max(1, Math.Min(five.Length, rank));
                Assert.That(M08BLongTermSimulationRunner.PercentileNearestRank(five, percentile),
                    Is.EqualTo(five[rank - 1]),
                    "percentile " + percentile + " must follow the documented nearest-rank rule");
            }

            Assert.That(M08BLongTermSimulationRunner.PercentileNearestRank(new[] { 7 }, 42),
                Is.EqualTo(7));
            Assert.Throws<ArgumentException>(() =>
                M08BLongTermSimulationRunner.PercentileNearestRank(Array.Empty<int>(), 50));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                M08BLongTermSimulationRunner.PercentileNearestRank(three, 101));

            string json = M08BLongTermSimulationRunner.BuildSummaryJson(
                new[] { SyntheticSeed(1, 10, true, 1, 100) }, 1, 1, 100, new[] { 10 });
            StringAssert.Contains("rank = ceil(n * p / 100)", json,
                "the reported percentile definition must name the implemented rule");
        }

        [Test]
        public void RunSeed_LeavesAnAlreadyDirtyIsolatedSceneUntouched()
        {
            // The Test Runner owns an untitled, disposable scene. Dirty it deliberately when it
            // is untitled so this proves the already-dirty case; never touch a saved scene.
            Scene active = SceneManager.GetActiveScene();
            Assert.That(active.IsValid(), Is.True, "the test runner must have an active scene");
            bool activeWasDirty = active.isDirty;
            GameObject activeMarker = null;
            if (!activeWasDirty && string.IsNullOrEmpty(active.path))
            {
                activeMarker = new GameObject("M08BSceneIsolationMarker");
                SceneManager.MoveGameObjectToScene(activeMarker, active);
            }
            bool dirtyBefore = active.isDirty;
            int sceneCountBefore = SceneManager.sceneCount;

            Scene isolated = EditorSceneManager.NewPreviewScene();
            try
            {
                var previewMarker = new GameObject("M08BPreviewIsolationMarker");
                SceneManager.MoveGameObjectToScene(previewMarker, isolated);

                M08BSeedResult result = M08BLongTermSimulationRunner.RunSeed(5UL, 1, new[] { 1 });

                Assert.That(result, Is.Not.Null);
                Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(active),
                    "a seed run must not switch the active scene");
                Assert.That(active.isDirty, Is.EqualTo(dirtyBefore),
                    "an already-dirty active scene must keep its dirty state");
                Assert.That(active.GetRootGameObjects().Any(root =>
                        root.name == "M08BLongTermSimulation"), Is.False,
                    "the simulation holder must never be added to the active scene");
                if (activeMarker != null)
                    Assert.That(active.GetRootGameObjects().Any(root =>
                            root.name == "M08BSceneIsolationMarker"), Is.True,
                        "the run must not remove or replace active scene content");
                Assert.That(isolated.GetRootGameObjects().Any(root =>
                        root.name == "M08BPreviewIsolationMarker"), Is.True,
                    "the run must not touch the isolated preview scene content");
                Assert.That(isolated.GetRootGameObjects().Any(root =>
                        root.name == "M08BLongTermSimulation"), Is.False,
                    "the simulation holder must be destroyed with its own preview scene");
            }
            finally
            {
                if (activeMarker != null)
                    Object.DestroyImmediate(activeMarker);
                if (isolated.IsValid())
                    EditorSceneManager.ClosePreviewScene(isolated);
            }

            Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(active));
            Assert.That(SceneManager.sceneCount, Is.EqualTo(sceneCountBefore));
            Assert.That(active.isDirty, Is.EqualTo(dirtyBefore),
                "the fixture must restore the prior scene state without saving or reloading");
        }

        [Test]
        public void RunExistingSession_MapsRealDateOverflowToDateOverflow()
        {
            var holder = new GameObject("M08BDateOverflowTest");
            try
            {
                GameSession session = holder.AddComponent<GameSession>();
                session.InitNewGame(11UL);
                Assert.That(session.AdvanceDaysForLegacyFixtureSetup(160981), Is.True);
                Assert.That(session.WorldClock.CurrentDate, Is.EqualTo(new GameDate(2000, 12, 31)));

                M08BSeedResult run = M08BLongTermSimulationRunner.RunExistingSession(
                    session, 11UL, 1, new[] { 1 });

                Assert.That(run.Termination, Is.EqualTo(M08BTerminationReason.DateOverflow));
                Assert.That(run.TerminationDetail, Does.Contain("DateOutOfRange"));
                Assert.That(run.MonthsSimulated, Is.EqualTo(0));
                Assert.That(run.Checkpoints.Single().Reached, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(holder);
            }
        }

        [Test]
        public void RunExistingSession_ReportsTimeoutForRepeatedNonTerminalRefusals()
        {
            var holder = new GameObject("M08BTimeoutTest");
            try
            {
                GameSession session = holder.AddComponent<GameSession>();
                session.InitNewGame(13UL);
                IndustryState state = IndustryStateOf(session);
                Assert.That(state.TrySetLastSettledMonthIndex(
                    IndustryCalendar.MonthIndex(session.WorldClock.CurrentDate) - 2, out string error),
                    Is.True, error);

                M08BSeedResult run = M08BLongTermSimulationRunner.RunExistingSession(
                    session, 13UL, 1, new[] { 1 });

                Assert.That(run.Termination, Is.EqualTo(M08BTerminationReason.Timeout));
                Assert.That(run.TerminationDetail, Does.Contain(
                    M08BLongTermSimulationRunner.MaxConsecutiveBlockedAttempts + " consecutive"));
                Assert.That(run.MonthsSimulated, Is.EqualTo(0));
                Assert.That(session.WorldClock.CurrentDate, Is.EqualTo(WorldClock.NewGameDate),
                    "a refused advance must never move the clock");
            }
            finally
            {
                Object.DestroyImmediate(holder);
            }
        }

        [Test]
        public void UnexpectedSimulatorExceptionIsSimulatorFaultNotTimeout()
        {
            M08BSeedResult fault = M08BLongTermSimulationRunner.CreateAbortedSeedResult(21UL, 1,
                new[] { 1 }, new InvalidOperationException("boom"));

            Assert.That(fault.Termination, Is.EqualTo(M08BTerminationReason.SimulatorFault));
            Assert.That(fault.Termination, Is.Not.EqualTo(M08BTerminationReason.Timeout),
                "a harness exception is not a rule-level timeout");
            Assert.That(fault.TerminationDetail, Does.Contain("InvalidOperationException"));
            Assert.That(fault.MonthsSimulated, Is.EqualTo(0));
            Assert.That(fault.Checkpoints.Single().Reached, Is.False);
            Assert.That(fault.MaxFoodPriceIndexPermille,
                Is.GreaterThanOrEqualTo(fault.MinFoodPriceIndexPermille),
                "an aborted row must never serialize a max below its min");
            Assert.That(fault.MinFoodPriceIndexPermille,
                Is.GreaterThanOrEqualTo(MarketState.MinimumPriceIndexPermille));
            Assert.That(fault.MaxMaterialsPriceIndexPermille,
                Is.GreaterThanOrEqualTo(fault.MinMaterialsPriceIndexPermille));
            Assert.That(fault.MaxCompositePriceIndexPermille,
                Is.GreaterThanOrEqualTo(MarketState.MinimumPriceIndexPermille));
            Assert.That(fault.MinFoodPriceIndexPermille,
                Is.EqualTo(MarketState.BaselinePriceIndexPermille),
                "no month settled, so the only truthful observation is the new-game baseline");
            Assert.That(fault.MaxFoodPriceIndexPermille,
                Is.EqualTo(MarketState.BaselinePriceIndexPermille));

            string csv = M08BLongTermSimulationRunner.SerializeSeedsCsv(new[] { fault }, new[] { 1 });
            StringAssert.Contains("SimulatorFault", csv);
            Assert.That(M08BLongTermSimulationRunner.TryParseSeedsCsv(csv, 21, 1, 1, new[] { 1 },
                out List<M08BSeedResult> parsed, out string error), Is.True, error);
            Assert.That(parsed.Single().Termination,
                Is.EqualTo(M08BTerminationReason.SimulatorFault));
        }

        [Test]
        public void PersistedRelationshipIntegrityGuardCountsInvariantViolations()
        {
            var save = new SaveData
            {
                relationships = new[]
                {
                    new RelationshipSaveEntry { characterId = string.Empty, value = 0 },
                    new RelationshipSaveEntry { characterId = "npc.known", value = 500 }
                },
                familyRelationships = new[]
                {
                    null,
                    new FamilyRelationshipSaveEntry
                    {
                        headPersonId = "person.a", memberPersonId = "person.a", value = 0
                    },
                    new FamilyRelationshipSaveEntry
                    {
                        headPersonId = "person.a", memberPersonId = "person.b", value = 0
                    },
                    new FamilyRelationshipSaveEntry
                    {
                        headPersonId = "person.a", memberPersonId = "person.b", value = 10
                    },
                    new FamilyRelationshipSaveEntry
                    {
                        headPersonId = "person.a", memberPersonId = "person.c", value = 10
                    }
                }
            };

            Assert.That(M08BLongTermSimulationRunner.CountPersistedRelationshipViolations(save),
                Is.EqualTo(5),
                "empty id, out-of-range value, null entry, self pair and duplicate pair each count once");
            Assert.That(M08BLongTermSimulationRunner.CountPersistedRelationshipViolations(null),
                Is.EqualTo(0));
            Assert.That(M08BLongTermSimulationRunner.CountPersistedRelationshipViolations(
                new SaveData()), Is.EqualTo(0), "a missing array is not a violation");
        }

        [Test]
        public void FamilyRelationshipAnomalyMetricCountsHostileHeadRelations()
        {
            var save = new SaveData
            {
                familyRelationships = new[]
                {
                    // Current head -> living member at the documented hostile bound.
                    new FamilyRelationshipSaveEntry
                    {
                        headPersonId = "person.head", memberPersonId = "person.living", value = -100
                    },
                    // Current head -> living member with an ordinary value.
                    new FamilyRelationshipSaveEntry
                    {
                        headPersonId = "person.head", memberPersonId = "person.other", value = 10
                    },
                    // Historical head: not sampled by the current-head metric.
                    new FamilyRelationshipSaveEntry
                    {
                        headPersonId = "person.oldhead", memberPersonId = "person.living", value = -100
                    },
                    // Dead member: not a living-member relation.
                    new FamilyRelationshipSaveEntry
                    {
                        headPersonId = "person.head", memberPersonId = "person.dead", value = -100
                    }
                }
            };
            var living = new HashSet<string> { "person.head", "person.living", "person.other" };

            int anomalies = M08BLongTermSimulationRunner.CountFamilyRelationshipAnomalies(save,
                "person.head", living, out int sampled);

            Assert.That(anomalies, Is.EqualTo(1),
                "only a hostile current-head to living-member relation is a GDD family relationship anomaly");
            Assert.That(sampled, Is.EqualTo(2), "the metric reports its real denominator");
            Assert.That(M08BLongTermSimulationRunner.CountPersistedRelationshipViolations(save),
                Is.EqualTo(0),
                "the GDD metric must not be the constant-by-construction integrity proxy");
        }

        [Test]
        public void FamilyRelationshipAnomalyMetricReportsAnEmptySampleAsUnavailable()
        {
            int anomalies = M08BLongTermSimulationRunner.CountFamilyRelationshipAnomalies(
                new SaveData(), "person.head", new HashSet<string>(), out int sampled);

            Assert.That(anomalies, Is.EqualTo(0));
            Assert.That(sampled, Is.EqualTo(0),
                "a zero sample must be visible so the metric reads as unavailable, not healthy");
        }

        [Test]
        public void SummaryReportsPeakFamilyRelationshipAnomaliesWithoutDoubleCounting()
        {
            M08BSeedResult checkpointOnly = SyntheticSeed(1, 10, true, 1, 100);
            checkpointOnly.Checkpoints[0].FamilyRelationshipAnomalies = 2;
            checkpointOnly.Checkpoints[0].FamilyRelationshipsSampled = 3;
            checkpointOnly.PeakFamilyRelationshipAnomalies = 2;
            checkpointOnly.FinalFamilyRelationshipAnomalies = 0;
            checkpointOnly.FamilyRelationshipsSampled = 3;

            M08BSeedResult finalOnly = SyntheticSeed(2, 10, true, 1, 100);
            finalOnly.Checkpoints[0].FamilyRelationshipAnomalies = 0;
            finalOnly.Checkpoints[0].FamilyRelationshipsSampled = 2;
            finalOnly.PeakFamilyRelationshipAnomalies = 3;
            finalOnly.FinalFamilyRelationshipAnomalies = 3;
            finalOnly.FamilyRelationshipsSampled = 2;

            string json = M08BLongTermSimulationRunner.BuildSummaryJson(
                new[] { checkpointOnly, finalOnly }, 1, 2, 100, new[] { 10 });

            StringAssert.Contains(
                "\"familyRelationshipAnomalySeeds\":{\"numerator\":2,\"denominator\":2,\"permille\":1000}",
                json);
            StringAssert.Contains("\"peakFamilyRelationshipAnomalies\":5", json);
            StringAssert.Contains("\"sampled\":5", json);
            Assert.That(json, Does.Not.Contain("totalRelationshipAnomalies"),
                "summing a checkpoint count and a final count would double-count the same anomaly");
        }

        [Test]
        public void CheckpointCollapseFlagsDoNotInheritTheRunLevelFlags()
        {
            M08BSeedResult seed = SyntheticSeed(1, 10, true, 1, 100);
            seed.FamineCollapse = true;
            seed.IndustryCollapse = false;
            seed.EconomicCollapse = true;
            seed.ExtremeInflation = true;
            seed.Checkpoints[0].FamineCollapse = false;
            seed.Checkpoints[0].EconomicCollapse = false;
            seed.Checkpoints[0].ExtremeInflation = false;

            string json = M08BLongTermSimulationRunner.BuildSummaryJson(
                new[] { seed }, 1, 1, 100, new[] { 10 });
            int checkpointsIndex = json.IndexOf("\"checkpoints\"", StringComparison.Ordinal);
            Assert.That(checkpointsIndex, Is.GreaterThan(0));
            string checkpointSection = json.Substring(checkpointsIndex);
            StringAssert.Contains(
                "\"economicCollapse\":{\"numerator\":0,\"denominator\":1,\"permille\":0}",
                checkpointSection,
                "the checkpoint must report its own collapse state, not the final run state");
            StringAssert.Contains(
                "\"extremeInflation\":{\"numerator\":0,\"denominator\":1,\"permille\":0}",
                checkpointSection);
            StringAssert.Contains(
                "\"economicCollapse\":{\"numerator\":1,\"denominator\":1,\"permille\":1000}", json,
                "the seed-level rate still reports that the run collapsed");

            string checkpointCsv = M08BLongTermSimulationRunner.SerializeCheckpointsCsv(
                new[] { seed }, new[] { 10 });
            Assert.That(CsvField(checkpointCsv, "famineCollapse"), Is.EqualTo("0"));
            Assert.That(CsvField(checkpointCsv, "economicCollapse"), Is.EqualTo("0"));
            Assert.That(CsvField(checkpointCsv, "extremeInflation"), Is.EqualTo("0"));

            string seedsCsv = M08BLongTermSimulationRunner.SerializeSeedsCsv(
                new[] { seed }, new[] { 10 });
            Assert.That(CsvField(seedsCsv, "famineCollapse"), Is.EqualTo("1"));
            Assert.That(CsvField(seedsCsv, "y10_famineCollapse"), Is.EqualTo("0"));
            Assert.That(CsvField(seedsCsv, "y10_extremeInflation"), Is.EqualTo("0"));
        }

        [Test]
        public void BatchLogDescribesOnlyTheCurrentRun()
        {
            int[] years = { 1 };
            M08BLongTermSimulationRunner.RunBatch(1, 2, 1, _root, years);
            string logPath = Path.Combine(_root, "batch-1-2-1y.log");
            StringAssert.Contains("ran=2", File.ReadAllText(logPath));

            M08BLongTermSimulationRunner.RunBatch(1, 2, 1, _root, years);
            string log = File.ReadAllText(logPath);

            Assert.That(Occurrences(log, "M08B LONG-TERM SIMULATION BATCH LOG"),
                Is.EqualTo(1), "a second run must replace the log, not append to it");
            StringAssert.Contains("reused=2 ran=0", log);
            Assert.That(log, Does.Not.Contain("ran=2"),
                "counters from an earlier run must not survive into this run's log");
            StringAssert.Contains("utcEnd:", log);
            StringAssert.Contains("schema=m08b.long-term-simulation/v6", log);
            StringAssert.Contains("ruleInputFileCount=", log);
        }

        [Test]
        public void BatchReuseRequiresMatchingCompanionProvenance()
        {
            int[] years = { 1 };
            M08BLongTermSimulationRunner.RunBatch(1, 2, 1, _root, years);
            string seedsPath = Path.Combine(_root, "seeds-1-2-1y.csv");
            string summaryPath = Path.Combine(_root, "summary-1-2-1y.json");
            string logPath = Path.Combine(_root, "batch-1-2-1y.log");
            string summary = File.ReadAllText(summaryPath);
            string sourceSha = M08BLongTermSimulationRunner.ComputeSourceSha256();
            Assert.That(sourceSha, Has.Length.EqualTo(64));
            StringAssert.Contains(sourceSha, summary);

            M08BLongTermSimulationRunner.RunBatch(1, 2, 1, _root, years);
            StringAssert.Contains("reused=2 ran=0", File.ReadAllText(logPath));

            File.WriteAllText(summaryPath, summary.Replace(sourceSha, new string('a', 64)));
            M08BLongTermSimulationRunner.RunBatch(1, 2, 1, _root, years);
            string staleSourceLog = File.ReadAllText(logPath);
            StringAssert.Contains("reused=0 ran=2", staleSourceLog);
            StringAssert.Contains("sourceSha256 does not match", staleSourceLog);

            File.WriteAllText(summaryPath, summary.Replace("m08b.long-term-simulation/v6",
                "m08b.long-term-simulation/v5"));
            M08BLongTermSimulationRunner.RunBatch(1, 2, 1, _root, years);
            StringAssert.Contains("reused=0 ran=2", File.ReadAllText(logPath));
            StringAssert.Contains("schema", File.ReadAllText(logPath));

            File.WriteAllText(summaryPath, summary.Replace("\"checkpointYears\": [1]",
                "\"checkpointYears\": [10]"));
            M08BLongTermSimulationRunner.RunBatch(1, 2, 1, _root, years);
            StringAssert.Contains("reused=0 ran=2", File.ReadAllText(logPath));
            StringAssert.Contains("checkpoint set is incompatible", File.ReadAllText(logPath));

            File.WriteAllText(summaryPath, summary.Replace(
                "\"firstSeed\":1,\"count\":2,\"years\":1", "\"firstSeed\":1,\"count\":3,\"years\":1"));
            M08BLongTermSimulationRunner.RunBatch(1, 2, 1, _root, years);
            StringAssert.Contains("reused=0 ran=2", File.ReadAllText(logPath));
            StringAssert.Contains("range does not describe this file", File.ReadAllText(logPath));

            File.Delete(summaryPath);
            M08BLongTermSimulationRunner.RunBatch(1, 2, 1, _root, years);
            StringAssert.Contains("reused=0 ran=2", File.ReadAllText(logPath));
            StringAssert.Contains("companion summary is missing", File.ReadAllText(logPath));
            Assert.That(File.Exists(seedsPath), Is.True);
        }

        [Test]
        public void FaultedRowsAreNeverReusedFromCsv()
        {
            int[] years = { 1 };
            M08BLongTermSimulationRunner.RunBatch(1, 2, 1, _root, years);
            string seedsPath = Path.Combine(_root, "seeds-1-2-1y.csv");
            string logPath = Path.Combine(_root, "batch-1-2-1y.log");
            Assert.That(M08BLongTermSimulationRunner.TryParseSeedsCsv(File.ReadAllText(seedsPath),
                1, 2, 1, years, out List<M08BSeedResult> rows, out string parseError), Is.True,
                parseError);
            rows[0].Termination = M08BTerminationReason.SimulatorFault;
            File.WriteAllText(seedsPath,
                M08BLongTermSimulationRunner.SerializeSeedsCsv(rows, years));

            M08BLongTermSimulationRunner.RunBatch(1, 2, 1, _root, years);
            string log = File.ReadAllText(logPath);
            StringAssert.Contains("faulted rows re-run", log);
            StringAssert.Contains("reused=1 ran=1", log);
        }

        [Test]
        public void RuleInputHashCoversTheSimulationCallPath()
        {
            IReadOnlyList<string> paths = M08BLongTermSimulationRunner.RuleInputRelativePaths;
            foreach (string required in new[]
                     {
                         "Assets/Scripts/Editor/M08BLongTermSimulationRunner.cs",
                         "Assets/Scripts/Application/GameSession.cs",
                         "Assets/Scripts/Application/GameSession.M08A.cs",
                         "Assets/Scripts/Application/GameSession.M08B.cs",
                         "Assets/Scripts/Domain/MarketSettlement.cs",
                         "Assets/Scripts/Domain/IndustrySettlement.cs",
                         "Assets/Scripts/Domain/FamilyLifeCycleRules.cs",
                         "Assets/Scripts/Application/FamilyBirthSettlement.cs",
                         "Assets/Scripts/Application/FamilyMarriageSettlement.cs",
                         "Assets/Scripts/Domain/NpcAutonomy.cs",
                         "Assets/Scripts/Domain/SuccessionService.cs",
                         "Assets/Scripts/Events/EventEngine.cs",
                         "Assets/Resources/Events/event-catalog-v1.json",
                         "Assets/Resources/Localization/event-text-zh-v1.json"
                     })
            {
                Assert.That(paths, Does.Contain(required), "rule input missing: " + required);
            }

            string ruleInput = M08BLongTermSimulationRunner.ComputeRuleInputSha256();
            Assert.That(ruleInput, Has.Length.EqualTo(64),
                "every listed rule input must exist, so the dependency hash is provable");
            Assert.That(ruleInput,
                Is.EqualTo(M08BLongTermSimulationRunner.ComputeRuleInputSha256()),
                "the dependency hash must be deterministic");
            Assert.That(ruleInput,
                Is.Not.EqualTo(M08BLongTermSimulationRunner.ComputeSourceSha256()),
                "the dependency hash must cover more than the runner file");

            string summary = M08BLongTermSimulationRunner.BuildSummaryJson(
                new[] { SyntheticSeed(1, 10, true, 1, 100) }, 1, 1, 100, new[] { 10 });
            StringAssert.Contains("\"ruleInputFileCount\":" + paths.Count, summary);
            StringAssert.Contains("\"ruleInputSha256\":\"" + ruleInput + "\"", summary);
        }

        [Test]
        public void ChangingOneRuleInputInvalidatesReusableRows()
        {
            string root = Path.Combine(_root, "rule-inputs");
            const string runnerPath = "Assets/Scripts/Editor/M08BLongTermSimulationRunner.cs";
            const string rulePath = "Assets/Scripts/Domain/MarketSettlement.cs";
            WriteFile(root, runnerPath, "// runner body");
            WriteFile(root, rulePath, "// rule body v1");
            var paths = new[] { runnerPath, rulePath };

            string before = M08BLongTermSimulationRunner.ComputeRuleInputSha256(root, paths);
            Assert.That(before, Has.Length.EqualTo(64));

            // The runner source is unchanged; only a settlement rule input changes.
            WriteFile(root, rulePath, "// rule body v2");
            string after = M08BLongTermSimulationRunner.ComputeRuleInputSha256(root, paths);
            Assert.That(after, Is.Not.EqualTo(before),
                "changing a rule input must change the dependency hash");
            Assert.That(M08BLongTermSimulationRunner.ComputeRuleInputSha256(root,
                    new[] { runnerPath }), Is.Not.EqualTo(after),
                "the hash covers the whole dependency set, not just the runner");

            File.Delete(Path.Combine(root, rulePath.Replace('/',
                Path.DirectorySeparatorChar)));
            Assert.That(M08BLongTermSimulationRunner.ComputeRuleInputSha256(root, paths),
                Is.Empty, "a missing rule input cannot be proven and must reject reuse");

            int[] years = { 1 };
            M08BLongTermSimulationRunner.RunBatch(1, 2, 1, _root, years);
            string summaryPath = Path.Combine(_root, "summary-1-2-1y.json");
            string logPath = Path.Combine(_root, "batch-1-2-1y.log");
            string summary = File.ReadAllText(summaryPath);
            string ruleInputSha = M08BLongTermSimulationRunner.ComputeRuleInputSha256();
            Assert.That(ruleInputSha, Has.Length.EqualTo(64));
            StringAssert.Contains(ruleInputSha, summary);

            // Runner hash stays valid; only the rule-input provenance is stale.
            File.WriteAllText(summaryPath, summary.Replace(ruleInputSha, new string('b', 64)));
            M08BLongTermSimulationRunner.RunBatch(1, 2, 1, _root, years);
            string log = File.ReadAllText(logPath);
            StringAssert.Contains("reused=0 ran=2", log);
            StringAssert.Contains("ruleInputSha256 does not match", log);
            StringAssert.Contains(M08BLongTermSimulationRunner.ComputeSourceSha256(), log);
        }

        [Test]
        public void RuleInputManifestCoversEveryRuntimeRuleDirectory()
        {
            IReadOnlyList<string> paths = M08BLongTermSimulationRunner.RuleInputRelativePaths;
            string projectRoot = Path.GetFullPath(Path.Combine(
                UnityEngine.Application.dataPath, ".."));
            string[] roots =
            {
                "Assets/Scripts/Application",
                "Assets/Scripts/Domain",
                "Assets/Scripts/Events",
                "Assets/Scripts/Persistence",
                "Assets/Resources/Events",
                "Assets/Resources/Localization"
            };

            foreach (string root in roots)
            {
                string directory = Path.Combine(projectRoot, root.Replace('/',
                    Path.DirectorySeparatorChar));
                Assert.That(Directory.Exists(directory), Is.True, root);
                foreach (string file in Directory.GetFiles(directory, "*",
                             SearchOption.AllDirectories))
                {
                    string extension = Path.GetExtension(file);
                    if (!string.Equals(extension, ".cs", StringComparison.OrdinalIgnoreCase) &&
                        !string.Equals(extension, ".json", StringComparison.OrdinalIgnoreCase))
                        continue;
                    string relative = Path.GetRelativePath(projectRoot, file).Replace('\\', '/');
                    Assert.That(paths, Does.Contain(relative),
                        "the manifest must cover every rule input, including " + relative);
                }
            }

            Assert.That(paths, Does.Contain(
                "Assets/Scripts/Editor/M08BLongTermSimulationRunner.cs"));
            Assert.That(paths, Does.Contain(
                "Assets/Scripts/Application/FamilyBirthSettlement.cs"));
            Assert.That(paths, Does.Contain(
                "Assets/Scripts/Application/FamilyMarriageSettlement.cs"));
            Assert.That(paths, Does.Contain("Assets/Scripts/Domain/NpcAutonomy.cs"));
            Assert.That(paths.Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(paths.Count));

            for (int i = 1; i < paths.Count; i++)
                Assert.That(string.CompareOrdinal(paths[i - 1], paths[i]), Is.LessThan(0),
                    "the manifest must stay in stable ordinal order");

            foreach (string path in paths)
            {
                Assert.That(path.StartsWith("Assets/", StringComparison.Ordinal), Is.True, path);
                Assert.That(path, Does.Not.Contain("Library/"));
                Assert.That(path, Does.Not.Contain("Temp/"));
                Assert.That(path, Does.Not.EndWith(".meta"));
            }
        }

        [Test]
        public void RuleInputManifestNoticesNewlyAddedFilesAndOmittedRuleInputs()
        {
            string root = Path.Combine(_root, "manifest-sweep");
            const string runnerPath = "Assets/Scripts/Editor/M08BLongTermSimulationRunner.cs";
            const string birthPath = "Assets/Scripts/Application/FamilyBirthSettlement.cs";
            const string npcPath = "Assets/Scripts/Domain/NpcAutonomy.cs";
            const string marriagePath = "Assets/Scripts/Application/FamilyMarriageSettlement.cs";
            WriteFile(root, runnerPath, "// runner body");
            WriteFile(root, birthPath, "// birth plan v1");
            WriteFile(root, npcPath, "// npc autonomy v1");

            IReadOnlyList<string> first = M08BLongTermSimulationRunner.BuildRuleInputPaths(root);
            CollectionAssert.Contains(first, runnerPath);
            CollectionAssert.Contains(first, birthPath);
            CollectionAssert.Contains(first, npcPath);
            string firstHash = M08BLongTermSimulationRunner.ComputeRuleInputSha256(root, first);
            Assert.That(firstHash, Has.Length.EqualTo(64));

            // A settlement file that did not exist when the rows ran joins the manifest, so
            // reuse cannot survive a newly added rule input either.
            WriteFile(root, marriagePath, "// marriage plan v1");
            IReadOnlyList<string> second = M08BLongTermSimulationRunner.BuildRuleInputPaths(root);
            CollectionAssert.Contains(second, marriagePath);
            Assert.That(second.Count, Is.EqualTo(first.Count + 1));
            string secondHash = M08BLongTermSimulationRunner.ComputeRuleInputSha256(root, second);
            Assert.That(secondHash, Is.Not.EqualTo(firstHash));

            // The runner source is byte-identical; only a previously omitted rule input changes.
            Assert.That(File.ReadAllText(Path.Combine(root, runnerPath.Replace('/',
                Path.DirectorySeparatorChar))), Is.EqualTo("// runner body"));
            WriteFile(root, npcPath, "// npc autonomy v2");
            string thirdHash = M08BLongTermSimulationRunner.ComputeRuleInputSha256(root,
                M08BLongTermSimulationRunner.BuildRuleInputPaths(root));
            Assert.That(thirdHash, Is.Not.EqualTo(secondHash),
                "changing a previously omitted rule input must invalidate reuse");
        }

        [Test]
        public void ExtinctionDuringSuccessionResolutionKeepsTheExtinctionCategory()
        {
            var holder = new GameObject("M08BInSuccessionExtinctionTest");
            try
            {
                GameSession session = holder.AddComponent<GameSession>();
                session.InitNewGame(7UL);
                GameDate date = session.WorldClock.CurrentDate;

                // Leave one living minor candidate and no adult sponsor: selecting that minor
                // has no lawful regent and no crisis adoption, so the resolution itself ends
                // the family after the decision has already started.
                foreach (FamilyMembership membership in session.Succession.World.Family.Members)
                {
                    PersonId personId = membership.PersonId;
                    if (personId == M2FixedScenario.NobumasaId)
                        continue;
                    Assert.That(session.Succession.World.TryGetLife(personId,
                        out SuccessionPersonLife life), Is.True);
                    Assert.That(session.Succession.World.TrySetLife(personId,
                        new SuccessionPersonLife(life.BirthYear, false, life.Retired), out string error),
                        Is.True, error);
                }
                Assert.That(session.Succession.World.TryGetLife(M2FixedScenario.NobumasaId,
                    out SuccessionPersonLife heirLife), Is.True);
                Assert.That(session.Succession.World.TrySetLife(M2FixedScenario.NobumasaId,
                    new SuccessionPersonLife(date.Year - 5, true, false), out string heirError),
                    Is.True, heirError);

                SuccessionResult started = SuccessionService.BeginDeathSuccession(
                    session.Succession, date);
                Assert.That(started.Succeeded, Is.True, started.Message);
                Assert.That(session.Succession.Phase,
                    Is.EqualTo(SuccessionPhase.AwaitingSuccessionChoice));
                Assert.That(session.IsFamilyExtinct, Is.False,
                    "the family is not extinct when the decision starts");

                M08BSeedResult run = M08BLongTermSimulationRunner.RunExistingSession(
                    session, 7UL, 1, new[] { 1 });

                Assert.That(session.IsFamilyExtinct, Is.True,
                    "resolving the only candidate without a lawful regent ends the family");
                Assert.That(run.Termination, Is.EqualTo(M08BTerminationReason.Extinction),
                    "extinction reached during succession resolution must keep the Extinction category");
                Assert.That(run.TerminationDetail, Does.Contain("became extinct"));
                Assert.That(run.ProgressionDeadlocks, Is.EqualTo(0));
                Assert.That(run.EventDeadlocks, Is.EqualTo(0));
                Assert.That(run.FinalFamilyExtinct, Is.True,
                    "the stopped family must be recorded as extinct even though its roster rows remain");
                Assert.That(run.FinalLivingMembers, Is.GreaterThan(0),
                    "an extinct family keeps its remaining roster rows, which is why a living-member filter alone misreads it");
                Assert.That(run.FinalHeirGap, Is.False,
                    "an extinct family is not a live family with an heir gap");
            }
            finally
            {
                Object.DestroyImmediate(holder);
            }
        }

        [Test]
        public void EventDeadlocksAreTheOnlyProgressionDeadlocks()
        {
            var holder = new GameObject("M08BEventDeadlockCounterTest");
            try
            {
                GameSession session = holder.AddComponent<GameSession>();
                session.InitNewGame(123UL);
                for (int month = 0; month < 24 && string.IsNullOrEmpty(session.PendingEventId); month++)
                    Assert.That(session.TryAdvanceToNextMonth(), Is.True, session.LastEventMessage);

                string eventId = session.PendingEventId;
                Assert.That(eventId, Is.Not.Empty, "a real SC-013 run must queue an event");

                // Make every option unresolvable through the public conditions path: the runner
                // must then report one blocking event and one progression deadlock, and a plain
                // pending succession must report neither.
                EventDefinitionData definition = session.EventCatalog.events
                    .First(item => item.eventId == eventId);
                foreach (EventOptionData option in definition.options)
                {
                    option.requirements = new[]
                    {
                        new EventRequirementData
                        {
                            type = nameof(EventRequirementKind.RelationshipAtLeast),
                            targetCharacterId = CharacterId.KurodaIori.Value,
                            minimumValue = 999,
                            reason = "forced test requirement"
                        }
                    };
                }

                M08BSeedResult blocked = M08BLongTermSimulationRunner.RunExistingSession(
                    session, 123UL, 1, new[] { 1 });

                Assert.That(blocked.Termination,
                    Is.EqualTo(M08BTerminationReason.UnresolvedBlockingEvent));
                Assert.That(blocked.EventDeadlocks, Is.EqualTo(1));
                Assert.That(blocked.ProgressionDeadlocks, Is.EqualTo(1),
                    "a still-blocking event is the only progression deadlock");

                var crisisHolder = new GameObject("M08BPendingSuccessionCounterTest");
                try
                {
                    GameSession crisis = crisisHolder.AddComponent<GameSession>();
                    crisis.InitNewGame(7UL);
                    Assert.That(crisis.StartCrisisDemoScenario(), Is.True);
                    PersonId crisisHead = crisis.Succession.HeadPersonId;
                    Assert.That(crisis.Succession.World.TryGetLife(crisisHead,
                        out SuccessionPersonLife crisisLife), Is.True);
                    Assert.That(crisis.Succession.World.TrySetLife(crisisHead,
                        new SuccessionPersonLife(crisisLife.BirthYear, false, crisisLife.Retired),
                        out string crisisError), Is.True, crisisError);
                    Assert.That(SuccessionService.BeginDeathSuccession(crisis.Succession,
                        crisis.WorldClock.CurrentDate).Succeeded, Is.True);

                    M08BSeedResult pending = M08BLongTermSimulationRunner.RunExistingSession(
                        crisis, 7UL, 1, new[] { 1 });

                    Assert.That(pending.Termination,
                        Is.EqualTo(M08BTerminationReason.PendingSuccession));
                    Assert.That(pending.ProgressionDeadlocks, Is.EqualTo(0),
                        "a rule-required pending succession is not a deadlock");
                    Assert.That(pending.EventDeadlocks, Is.EqualTo(0));
                }
                finally
                {
                    Object.DestroyImmediate(crisisHolder);
                }
            }
            finally
            {
                Object.DestroyImmediate(holder);
            }
        }

        [Test]
        public void PendingFamilyEventIsChosenBeforeTheYearBoundarySuccession()
        {
            var holder = new GameObject("M08BOldHeadEventChoiceTest");
            try
            {
                GameSession session = holder.AddComponent<GameSession>();
                session.InitNewGame(7UL);
                EnqueuePendingEvent(session, "event.sc018.heirloom");
                PersonId oldHead = session.Succession.HeadPersonId;
                Assert.That(oldHead, Is.EqualTo(M2FixedScenario.HarunobuId));

                // Reproduce the year boundary: the month turn offered the event first and then
                // applied the head's death, so the queue and the succession decision coexist and
                // the outgoing head is still the current head.
                Assert.That(session.Succession.World.TryGetLife(oldHead,
                    out SuccessionPersonLife headLife), Is.True);
                Assert.That(session.Succession.World.TrySetLife(oldHead,
                    new SuccessionPersonLife(headLife.BirthYear, false, headLife.Retired),
                    out string headError), Is.True, headError);
                SuccessionResult started = SuccessionService.BeginDeathSuccession(
                    session.Succession, session.WorldClock.CurrentDate);
                Assert.That(started.Succeeded, Is.True, started.Message);
                Assert.That(session.Succession.IsPending, Is.True);
                Assert.That(session.Succession.HeadPersonId, Is.EqualTo(oldHead),
                    "the outgoing head stays current until the handoff is confirmed");
                Assert.That(session.PendingEventId, Is.EqualTo("event.sc018.heirloom"));

                M08BSeedResult run = M08BLongTermSimulationRunner.RunExistingSession(
                    session, 7UL, 1, new[] { 1 });

                Assert.That(run.EventDeadlocks, Is.EqualTo(0),
                    "the pending scene must resolve instead of blocking the calendar");
                Assert.That(run.Termination, Is.Not.EqualTo(
                    M08BTerminationReason.UnresolvedBlockingEvent));
                Assert.That(run.EventChoices, Does.Contain(
                        "event.sc018.heirloom=option.sc018.heirloom.keep-in-main-hall"),
                    "the stable-order resolver must commit the pending option");
                Assert.That(session.TryGetFamilyRelationship(oldHead,
                        M2FixedScenario.TokihimeId).CurrentValue,
                    Is.EqualTo(-5),
                    "the effect must apply to the outgoing head's pair, not be dropped or retargeted");
                Assert.That(session.TryGetFamilyRelationship(oldHead,
                        M2FixedScenario.MasatsunaId).CurrentValue, Is.EqualTo(5));
                Assert.That(run.SuccessionDecisions, Is.Not.Empty,
                    "the pending succession must still settle after the choice");
                Assert.That(session.Succession.IsPending, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(holder);
            }
        }

        [Test]
        public void FamilyOptionIsNotAdvertisedWhenItsTargetIsTheCurrentHead()
        {
            var holder = new GameObject("M08BHeadTargetAvailabilityTest");
            try
            {
                GameSession session = holder.AddComponent<GameSession>();
                session.InitNewGame(7UL);
                PersonId head = session.Succession.HeadPersonId;
                Assert.That(session.Succession.World.TryGetLife(head,
                    out SuccessionPersonLife headLife), Is.True);
                Assert.That(session.Succession.World.TrySetLife(head,
                    new SuccessionPersonLife(headLife.BirthYear, false, headLife.Retired),
                    out string headError), Is.True, headError);
                Assert.That(SuccessionService.BeginDeathSuccession(session.Succession,
                    session.WorldClock.CurrentDate).Succeeded, Is.True);
                Assert.That(session.SelectSuccessionSuccessor(M2FixedScenario.MasatsunaId).Succeeded,
                    Is.True);
                Assert.That(session.ConfirmSuccession().Succeeded, Is.True);
                Assert.That(session.Succession.HeadPersonId,
                    Is.EqualTo(M2FixedScenario.MasatsunaId));

                bool found = session.TryGetEventOptionAvailability("event.sc018.heirloom",
                    "option.sc018.heirloom.keep-in-main-hall", out bool enabled, out string reason);
                Assert.That(found, Is.True, reason);
                Assert.That(enabled, Is.False,
                    "an option whose family target is the current head must not be advertised");
                Assert.That(reason, Does.Contain("家主本人"));

                Assert.That(session.TryGetEventOptionAvailability("event.sc018.parent-care",
                    "option.sc018.parent-care.nurse-personally", out bool stillEnabled,
                    out reason), Is.True, reason);
                Assert.That(stillEnabled, Is.True,
                    "an option whose target is still another family member stays available: " + reason);
            }
            finally
            {
                Object.DestroyImmediate(holder);
            }
        }

        [Test]
        public void SceneWhoseOnlyTargetsAreTheHeadIsNotOffered()
        {
            var text = JsonUtility.FromJson<EventTextCatalog>(
                File.ReadAllText("Assets/Resources/Localization/event-text-zh-v1.json"));
            var engine = new EventEngine(LoadCatalog(), text);
            Assert.That(engine.IsReady, Is.True, engine.ValidationMessage);

            PersonId[] members =
            {
                M2FixedScenario.HarunobuId, M2FixedScenario.ChieId,
                M2FixedScenario.MasatsunaId, M2FixedScenario.NobumasaId,
                M2FixedScenario.TokihimeId, M2FixedScenario.SakonId
            };
            var headContext = new EventRuleContext(2, 2, EventScenarioIds.Sc013,
                M2FixedScenario.TokihimeId, members);
            IReadOnlyList<string> eligible = engine.GetEligibleEventIds(
                new GameDate(1560, 5, 1), new CharacterRelationships(), new EventRuntimeState(),
                headContext);

            Assert.That(eligible, Has.No.Member("event.sc018.sibling-duty"),
                "every option targets Tokihime, who is now the head");
            Assert.That(eligible, Has.No.Member("event.sc018.adoption-welcome"),
                "every option targets Tokihime, who is now the head");
            Assert.That(eligible, Has.Member("event.sc018.parent-care"));
            Assert.That(eligible, Has.Member("event.sc018.spouse-visit"));

            // A head-only roster is a complete family context, not a missing one: every
            // family-target option is invalid, so the family scenes are excluded while
            // neutral events stay eligible.
            var headOnly = new EventRuleContext(0, 0, EventScenarioIds.Sc013,
                M2FixedScenario.HarunobuId, new[] { M2FixedScenario.HarunobuId });
            IReadOnlyList<string> headOnlyEligible = engine.GetEligibleEventIds(
                new GameDate(1560, 5, 1), new CharacterRelationships(), new EventRuntimeState(),
                headOnly);
            foreach (string familyEventId in NewSc018EventIds)
                Assert.That(headOnlyEligible, Has.No.Member(familyEventId),
                    "a scene whose every option targets the only member must not be offered");
            Assert.That(headOnlyEligible, Has.Member("event.rainy-arrival"),
                "neutral events must stay eligible");
            Assert.That(headOnlyEligible, Has.Member(SC013EventIds.PledgeEventId));

            // An empty but non-null roster behaves the same way; only a missing head makes
            // the family context unavailable and leaves the eligibility unchanged.
            var emptyRoster = new EventRuleContext(0, 0, EventScenarioIds.Sc013,
                M2FixedScenario.HarunobuId, new PersonId[0]);
            IReadOnlyList<string> emptyRosterEligible = engine.GetEligibleEventIds(
                new GameDate(1560, 5, 1), new CharacterRelationships(), new EventRuntimeState(),
                emptyRoster);
            foreach (string familyEventId in NewSc018EventIds)
                Assert.That(emptyRosterEligible, Has.No.Member(familyEventId));
            Assert.That(emptyRosterEligible, Has.Member("event.rainy-arrival"));

            IReadOnlyList<string> noContext = engine.GetEligibleEventIds(
                new GameDate(1560, 5, 1), new CharacterRelationships(), new EventRuntimeState(),
                EventRuleContext.Default);
            Assert.That(noContext, Has.Member("event.rainy-arrival"),
                "without a head the family gate is unavailable, not negative");
            Assert.That(noContext, Has.No.Member("event.sc018.parent-care"),
                "scenario-tagged rows need the matching scenario; the missing head is not the reason");
        }

        [Test]
        public void PreviouslyDeadlockingSc018SeedsResolveWithoutUnresolvedEventStops()
        {
            ulong[] previouslyDeadlocking = { 40UL, 60UL, 83UL, 93UL, 120UL, 136UL };
            foreach (ulong seed in previouslyDeadlocking)
            {
                M08BSeedResult run = M08BLongTermSimulationRunner.RunSeed(seed, 1, new[] { 1 });
                Assert.That(run.TerminationDetail, Does.Not.Contain("家主本人"),
                    "seed " + seed + " must not reject the head-targeted SC-018 option");
                Assert.That(run.EventDeadlocks, Is.EqualTo(0),
                    "seed " + seed + " must not deadlock on a pending SC-018 scene");
                Assert.That(run.MonthsSimulated, Is.GreaterThan(9),
                    "seed " + seed + " must advance past the month that used to deadlock");
            }
        }

        [Test]
        public void HeirGapAtStopExcludesExtinctFamiliesFromBothSides()
        {
            M08BSeedResult live = SyntheticSeed(1, 10, true, 2, 100);
            live.Termination = M08BTerminationReason.PendingSuccession;
            live.FinalLivingMembers = 2;
            live.FinalFamilyExtinct = false;
            live.FinalHeirGap = true;

            // The reported shape: extinct during succession, roster rows still living.
            M08BSeedResult extinct = SyntheticSeed(2, 10, true, 3, 100);
            extinct.Termination = M08BTerminationReason.Extinction;
            extinct.FinalLivingMembers = 3;
            extinct.FinalFamilyExtinct = true;
            extinct.FinalHeirGap = false;

            M08BSeedResult emptied = SyntheticSeed(3, 10, true, 0, 0);
            emptied.Termination = M08BTerminationReason.Completed;
            emptied.FinalLivingMembers = 0;
            emptied.FinalFamilyExtinct = false;
            emptied.FinalHeirGap = false;

            var seeds = new[] { live, extinct, emptied };
            string json = M08BLongTermSimulationRunner.BuildSummaryJson(seeds, 1, 3, 100,
                new[] { 10 });
            StringAssert.Contains(
                "\"heirGapAtStop\":{\"numerator\":1,\"denominator\":1,\"permille\":1000}", json,
                "numerator and denominator must both exclude the extinct family and the empty family");

            string csv = M08BLongTermSimulationRunner.SerializeSeedsCsv(seeds, new[] { 10 });
            Assert.That(csv.Split('\n')[0], Does.Contain("finalFamilyExtinct"));
            Assert.That(M08BLongTermSimulationRunner.TryParseSeedsCsv(csv, 1, 3, 100, new[] { 10 },
                out List<M08BSeedResult> parsed, out string error), Is.True, error);
            Assert.That(parsed.Single(item => item.Seed == 2UL).FinalFamilyExtinct, Is.True,
                "the extinct flag must survive the normalized round trip");
            Assert.That(parsed.Single(item => item.Seed == 1UL).FinalFamilyExtinct, Is.False);
        }

        [Test]
        public void ConfirmingSuccessionIsBlockedWhileAFamilyEventIsPending()
        {
            var holder = new GameObject("M08BPendingEventSuccessionGateTest");
            try
            {
                GameSession session = holder.AddComponent<GameSession>();
                session.InitNewGame(7UL);
                EnqueuePendingEvent(session, "event.sc018.heirloom");
                PersonId outgoingHead = session.Succession.HeadPersonId;
                Assert.That(outgoingHead, Is.EqualTo(M2FixedScenario.HarunobuId));

                // The year boundary queues the scene and kills the head in the same month turn.
                Assert.That(session.Succession.World.TryGetLife(outgoingHead,
                    out SuccessionPersonLife headLife), Is.True);
                Assert.That(session.Succession.World.TrySetLife(outgoingHead,
                    new SuccessionPersonLife(headLife.BirthYear, false, headLife.Retired),
                    out string headError), Is.True, headError);
                Assert.That(SuccessionService.BeginDeathSuccession(session.Succession,
                    session.WorldClock.CurrentDate).Succeeded, Is.True);

                // Selecting the fixed heir first is allowed: it changes no head.
                Assert.That(session.SelectSuccessionSuccessor(M2FixedScenario.MasatsunaId).Succeeded,
                    Is.True);

                // Confirming now would hand the head to 正綱 and leave every heirloom option
                // with no counterpart, which is exactly the stranded state the review reported.
                SuccessionResult blocked = session.ConfirmSuccession();
                Assert.That(blocked.Succeeded, Is.False);
                Assert.That(blocked.Outcome,
                    Is.EqualTo(SuccessionOutcome.BlockedByPendingEvent));
                Assert.That(blocked.Message, Does.Contain("事件"));
                Assert.That(session.LastSuccessionMessage, Does.Contain("事件"),
                    "the succession panel renders this message in its feedback line");
                Assert.That(session.Succession.HeadPersonId, Is.EqualTo(outgoingHead),
                    "a refused handoff must not change the current head");
                Assert.That(session.PendingEventId, Is.EqualTo("event.sc018.heirloom"));

                // The scene stays selectable under the outgoing head, so the player is not stuck.
                Assert.That(session.TryGetEventOptionAvailability("event.sc018.heirloom",
                        "option.sc018.heirloom.keep-in-main-hall", out bool enabled,
                        out string reason), Is.True, reason);
                Assert.That(enabled, Is.True, reason);

                EventChoiceResult choice = session.TryChooseEventOption("event.sc018.heirloom",
                    "option.sc018.heirloom.keep-in-main-hall",
                    session.CreateEventResultId("event.sc018.heirloom",
                        "option.sc018.heirloom.keep-in-main-hall"));
                Assert.That(choice.Succeeded, Is.True, choice.Message);
                Assert.That(session.TryGetFamilyRelationship(outgoingHead,
                    M2FixedScenario.TokihimeId).CurrentValue, Is.EqualTo(-5),
                    "the effect stays on the outgoing head's pair, never retargeted");

                // Once the queue is clear the same handoff proceeds normally.
                Assert.That(session.ConfirmSuccession().Succeeded, Is.True);
                Assert.That(session.Succession.HeadPersonId,
                    Is.EqualTo(M2FixedScenario.MasatsunaId));
            }
            finally
            {
                Object.DestroyImmediate(holder);
            }
        }

        [Test]
        public void RetirementIsUnavailableWhileAFamilyEventIsPending()
        {
            var holder = new GameObject("M08BPendingEventRetirementGateTest");
            try
            {
                GameSession session = holder.AddComponent<GameSession>();
                session.InitNewGame(7UL);
                EnqueuePendingEvent(session, "event.sc018.parent-care");

                Assert.That(session.CanBeginSuccessionRetirement, Is.False,
                    "the succession panel disables the retire button from this property");
                Assert.That(session.SuccessionRetirementBlockReason, Does.Contain("事件"),
                    "the panel must explain why retirement is unavailable, not just disable it");
                SuccessionResult blocked = session.BeginSuccessionRetirement();
                Assert.That(blocked.Succeeded, Is.False);
                Assert.That(blocked.Outcome,
                    Is.EqualTo(SuccessionOutcome.BlockedByPendingEvent));
                Assert.That(session.Succession.Phase, Is.EqualTo(SuccessionPhase.None));
                Assert.That(session.PendingEventId, Is.EqualTo("event.sc018.parent-care"));
            }
            finally
            {
                Object.DestroyImmediate(holder);
            }
        }

        [Test]
        public void DefaultRunStaysUnmanaged()
        {
            var holder = new GameObject("M08BDefaultUnmanagedTest");
            try
            {
                GameSession session = holder.AddComponent<GameSession>();
                session.InitNewGame(7UL);

                M08BSeedResult run = M08BLongTermSimulationRunner.RunExistingSession(
                    session, 7UL, 1, new[] { 1 });

                Assert.That(run.PolicyId,
                    Is.EqualTo(M08BLongTermSimulationRunner.UnmanagedPolicyId));
                Assert.That(run.FarmAppointments, Is.Zero,
                    "the default path must never appoint an industry");
                Assert.That(run.FarmVacancies, Is.Zero,
                    "the default path must not even inspect the industry posts");
                Assert.That(run.FinalFarmAssignee, Is.Empty);
                Assert.That(run.IndustryDecisions, Is.Empty);
                Assert.That(FarmAssigneeOf(session).IsEmpty, Is.True,
                    "the real session must end with Farm vacant");
                Assert.That(session.Industry.Slot(IndustryCatalog.TradingHouseId).AssigneeId.IsEmpty,
                    Is.True, "Trading House must stay vacant");
                Assert.That(run.Termination, Is.Not.EqualTo(M08BTerminationReason.SimulatorFault));
                Assert.That(run.ProgressionDeadlocks, Is.Zero);
            }
            finally
            {
                Object.DestroyImmediate(holder);
            }
        }

        [Test]
        public void FarmManagedV1PicksTheLowestOrdinalLegalCandidate()
        {
            var primaryHolder = new GameObject("M08BFarmPolicyPrimary");
            var probeHolder = new GameObject("M08BFarmPolicyProbe");
            try
            {
                GameSession session = primaryHolder.AddComponent<GameSession>();
                session.InitNewGame(7UL);
                GameSession probe = probeHolder.AddComponent<GameSession>();
                probe.InitNewGame(7UL);

                // Oracle: the same real command, enumerated literally in ordinal ID order on an
                // identical session. Every rejected candidate before the success is recorded.
                var refusedBeforeSuccess = new List<PersonId>();
                PersonId expected = default;
                foreach (PersonId candidate in LivingMembersByOrdinalId(session))
                {
                    IndustryAssignmentResult attempt =
                        probe.TryAssignIndustry(IndustryCatalog.FarmId, candidate);
                    if (attempt.Succeeded)
                    {
                        expected = candidate;
                        break;
                    }
                    refusedBeforeSuccess.Add(candidate);
                }
                Assert.That(expected.IsEmpty, Is.False,
                    "the standard SC-013 roster must offer at least one legal Farm candidate");

                var appointments = new List<string>();
                bool appointed = M08BLongTermSimulationRunner.TryApplyFarmManagement(session,
                    appointments, out bool farmWasVacant, out string detail);

                Assert.That(appointed, Is.True, detail);
                Assert.That(farmWasVacant, Is.True);
                Assert.That(FarmAssigneeOf(session), Is.EqualTo(expected),
                    "the policy must take the first candidate the real rules accept");
                Assert.That(appointments, Is.EqualTo(new[] { "farm=" + expected.Value }));
                foreach (PersonId refused in refusedBeforeSuccess)
                {
                    Assert.That(session.Succession.World.IsLiving(refused), Is.True);
                    Assert.That(IndustryService.CanWork(session.Succession, refused,
                            session.WorldClock.CurrentDate, out _), Is.False,
                        "a candidate skipped by the ordinal scan must be genuinely ineligible");
                }
                Assert.That(session.Industry.Slot(IndustryCatalog.TradingHouseId).AssigneeId.IsEmpty,
                    Is.True, "the policy must never staff the Trading House");
            }
            finally
            {
                Object.DestroyImmediate(primaryHolder);
                Object.DestroyImmediate(probeHolder);
            }
        }

        [Test]
        public void FarmManagedV1NeverReplacesAValidAssignment()
        {
            var holder = new GameObject("M08BFarmPolicyNoReplaceTest");
            try
            {
                GameSession session = holder.AddComponent<GameSession>();
                session.InitNewGame(7UL);
                PersonId keeper = LivingMembersByOrdinalId(session)
                    .First(personId => IndustryService.CanWork(session.Succession, personId,
                        session.WorldClock.CurrentDate, out _));
                Assert.That(session.TryAssignIndustry(IndustryCatalog.FarmId, keeper).Succeeded,
                    Is.True);

                var appointments = new List<string>();
                bool appointed = M08BLongTermSimulationRunner.TryApplyFarmManagement(session,
                    appointments, out bool farmWasVacant, out string detail);

                Assert.That(appointed, Is.False, detail);
                Assert.That(farmWasVacant, Is.False,
                    "an occupied post is not a vacancy the policy may fill");
                Assert.That(appointments, Is.Empty);
                Assert.That(FarmAssigneeOf(session), Is.EqualTo(keeper),
                    "a valid appointment must never be replaced or reassigned");
            }
            finally
            {
                Object.DestroyImmediate(holder);
            }
        }

        [Test]
        public void FarmManagedV1RefillsOnlyAfterRealSessionRulesVacateThePost()
        {
            var holder = new GameObject("M08BFarmPolicyRefillTest");
            try
            {
                GameSession session = holder.AddComponent<GameSession>();
                session.InitNewGame(7UL);
                PersonId head = session.Succession.HeadPersonId;
                Assert.That(session.TryAssignIndustry(IndustryCatalog.FarmId, head).Succeeded, Is.True,
                    "the head must be a legal Farm candidate for this scenario");

                var beforeVacancy = new List<string>();
                Assert.That(M08BLongTermSimulationRunner.TryApplyFarmManagement(session,
                    beforeVacancy, out bool vacantBefore, out _), Is.False);
                Assert.That(vacantBefore, Is.False);
                Assert.That(FarmAssigneeOf(session), Is.EqualTo(head));

                Assert.That(session.Succession.World.TryGetLife(head,
                    out SuccessionPersonLife life), Is.True);
                Assert.That(session.Succession.World.TrySetLife(head,
                    new SuccessionPersonLife(life.BirthYear, false, life.Retired),
                    out string lifeError), Is.True, lifeError);
                Assert.That(SuccessionService.BeginDeathSuccession(session.Succession,
                    session.WorldClock.CurrentDate).Succeeded, Is.True);
                var decisions = new List<string>();
                Assert.That(M08BLongTermSimulationRunner.TryResolveSuccession(session, decisions,
                    out string failure), Is.True, failure);

                Assert.That(FarmAssigneeOf(session).IsEmpty, Is.True,
                    "the real succession rules must vacate the dead head's post first");
                Assert.That(session.CanEditIndustryAssignments, Is.True,
                    "the refill needs a management-stable point");

                var appointments = new List<string>();
                bool refilled = M08BLongTermSimulationRunner.TryApplyFarmManagement(session,
                    appointments, out bool farmWasVacant, out string detail);

                Assert.That(refilled, Is.True, detail);
                Assert.That(farmWasVacant, Is.True);
                Assert.That(appointments.Count, Is.EqualTo(1));
                Assert.That(FarmAssigneeOf(session), Is.Not.EqualTo(head));
                Assert.That(FarmAssigneeOf(session).IsEmpty, Is.False);
                Assert.That(session.Succession.World.IsLiving(FarmAssigneeOf(session)), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(holder);
            }
        }

        [Test]
        public void FarmManagedV1LeavesThePostVacantWhenNobodyIsEligible()
        {
            var holder = new GameObject("M08BFarmPolicyNoCandidateTest");
            try
            {
                GameSession session = holder.AddComponent<GameSession>();
                session.InitNewGame(7UL);
                foreach (PersonId member in LivingMembersByOrdinalId(session))
                {
                    Assert.That(session.Succession.World.TryGetLife(member,
                        out SuccessionPersonLife life), Is.True);
                    Assert.That(session.Succession.World.TrySetLife(member,
                        new SuccessionPersonLife(life.BirthYear, life.Alive, true),
                        out string retireError), Is.True, retireError);
                }

                var appointments = new List<string>();
                bool appointed = M08BLongTermSimulationRunner.TryApplyFarmManagement(session,
                    appointments, out bool farmWasVacant, out string detail);

                Assert.That(appointed, Is.False, detail);
                Assert.That(farmWasVacant, Is.True);
                Assert.That(appointments, Is.Empty);
                Assert.That(FarmAssigneeOf(session).IsEmpty, Is.True,
                    "no accepted candidate must leave the post vacant");
            }
            finally
            {
                Object.DestroyImmediate(holder);
            }
        }

        [Test]
        public void FarmManagedV1ConsumesNoRandomState()
        {
            var holder = new GameObject("M08BFarmPolicyRandomTest");
            try
            {
                GameSession session = holder.AddComponent<GameSession>();
                session.InitNewGame(7UL);
                Assert.That(session.CanEditIndustryAssignments, Is.True,
                    "a fresh new game must be a management-stable point");
                string before = JsonUtility.ToJson(session.CaptureSave().randomState);

                var appointments = new List<string>();
                Assert.That(M08BLongTermSimulationRunner.TryApplyFarmManagement(session,
                    appointments, out _, out string detail), Is.True, detail);

                string after = JsonUtility.ToJson(session.CaptureSave().randomState);
                Assert.That(after, Is.EqualTo(before),
                    "the industry policy must not read or advance the random streams");
            }
            finally
            {
                Object.DestroyImmediate(holder);
            }
        }

        [Test]
        public void FarmManagedRunAppointsTheFarmWhileTheUnmanagedRunStaysVacant()
        {
            int[] years = { 1 };
            M08BSeedResult unmanaged = M08BLongTermSimulationRunner.RunSeed(7UL, 1, years,
                M08BIndustryPolicy.Unmanaged);
            M08BSeedResult managed = M08BLongTermSimulationRunner.RunSeed(7UL, 1, years,
                M08BIndustryPolicy.FarmManagedV1);

            Assert.That(unmanaged.PolicyId,
                Is.EqualTo(M08BLongTermSimulationRunner.UnmanagedPolicyId));
            Assert.That(managed.PolicyId,
                Is.EqualTo(M08BLongTermSimulationRunner.FarmManagedV1PolicyId));
            Assert.That(unmanaged.FarmAppointments, Is.Zero);
            Assert.That(unmanaged.FinalFarmAssignee, Is.Empty);
            Assert.That(managed.FarmAppointments, Is.GreaterThanOrEqualTo(1),
                "a fresh SC-013 family must staff the Farm at its first stable point");
            Assert.That(managed.FarmVacancies, Is.GreaterThanOrEqualTo(managed.FarmAppointments),
                "every appointment needs a vacancy, so vacancies cannot trail appointments");
            Assert.That(managed.FinalFarmAssignee, Is.Not.Empty);
            Assert.That(managed.FarmAppointments, Is.EqualTo(managed.IndustryDecisions.Count),
                "every counted appointment needs exactly one recorded decision");
            Assert.That(managed.IndustryDecisions.All(decision =>
                    decision.StartsWith("farm=", StringComparison.Ordinal)), Is.True);
        }

        [Test]
        public void FarmManagedPolicyIsDeterministicForTheSameSeed()
        {
            int[] years = { 1 };
            M08BSeedResult first = M08BLongTermSimulationRunner.RunSeed(7UL, 1, years,
                M08BIndustryPolicy.FarmManagedV1);
            M08BSeedResult repeat = M08BLongTermSimulationRunner.RunSeed(7UL, 1, years,
                M08BIndustryPolicy.FarmManagedV1);

            Assert.That(M08BLongTermSimulationRunner.SerializeSeedsCsv(new[] { repeat }, years),
                Is.EqualTo(M08BLongTermSimulationRunner.SerializeSeedsCsv(new[] { first }, years)),
                "the same seed and policy must serialize byte-identically");
            Assert.That(repeat.IndustryDecisions, Is.EqualTo(first.IndustryDecisions),
                "the same stable points must make the same appointments in the same order");
            Assert.That(repeat.Termination, Is.EqualTo(first.Termination));
            Assert.That(repeat.FarmAppointments, Is.EqualTo(first.FarmAppointments));
        }

        [Test]
        public void PolicyRunsWriteDistinctPolicyLabeledArtifactsAndNeverShareRows()
        {
            int[] years = { 1 };
            string unmanagedRoot = Path.Combine(_root, "unmanaged");
            string managedRoot = Path.Combine(_root, "farm-managed-v1");
            Directory.CreateDirectory(unmanagedRoot);
            Directory.CreateDirectory(managedRoot);

            M08BLongTermSimulationRunner.RunBatch(1, 1, 1, unmanagedRoot, years,
                M08BIndustryPolicy.Unmanaged);
            M08BLongTermSimulationRunner.RunBatch(1, 1, 1, managedRoot, years,
                M08BIndustryPolicy.FarmManagedV1);

            string unmanagedSeeds = File.ReadAllText(Path.Combine(unmanagedRoot, "seeds-1-1-1y.csv"));
            string managedSeeds = File.ReadAllText(Path.Combine(managedRoot, "seeds-1-1-1y.csv"));
            Assert.That(unmanagedSeeds, Is.Not.EqualTo(managedSeeds));
            Assert.That(CsvField(unmanagedSeeds, "policy"),
                Is.EqualTo(M08BLongTermSimulationRunner.UnmanagedPolicyId));
            Assert.That(CsvField(managedSeeds, "policy"),
                Is.EqualTo(M08BLongTermSimulationRunner.FarmManagedV1PolicyId));
            StringAssert.Contains("\"policy\":\"farm-managed-v1\"",
                File.ReadAllText(Path.Combine(managedRoot, "summary-1-1-1y.json")));
            StringAssert.Contains("\"policy\":\"unmanaged\"",
                File.ReadAllText(Path.Combine(unmanagedRoot, "summary-1-1-1y.json")));
            StringAssert.Contains("policy=farm-managed-v1",
                File.ReadAllText(Path.Combine(managedRoot, "batch-1-1-1y.log")));
            StringAssert.Contains("policy=unmanaged",
                File.ReadAllText(Path.Combine(unmanagedRoot, "batch-1-1-1y.log")));

            // Each policy root is independently resumable.
            M08BLongTermSimulationRunner.RunBatch(1, 1, 1, managedRoot, years,
                M08BIndustryPolicy.FarmManagedV1);
            StringAssert.Contains("reused=1 ran=0",
                File.ReadAllText(Path.Combine(managedRoot, "batch-1-1-1y.log")));

            // A summary written by the other policy can never be reused.
            string managedSeedsBefore = File.ReadAllText(
                Path.Combine(managedRoot, "seeds-1-1-1y.csv"));
            M08BLongTermSimulationRunner.RunBatch(1, 1, 1, unmanagedRoot, years,
                M08BIndustryPolicy.FarmManagedV1);
            string rejectionLog = File.ReadAllText(Path.Combine(unmanagedRoot, "batch-1-1-1y.log"));
            StringAssert.Contains("reused=0 ran=1", rejectionLog);
            StringAssert.Contains("policy unmanaged does not match farm-managed-v1", rejectionLog);
            Assert.That(managedSeeds, Is.EqualTo(managedSeedsBefore));
            Assert.That(M08BLongTermSimulationRunner.PolicyOutputRoot(
                    M08BIndustryPolicy.Unmanaged),
                Is.Not.EqualTo(M08BLongTermSimulationRunner.PolicyOutputRoot(
                    M08BIndustryPolicy.FarmManagedV1)));
            Assert.That(M08BLongTermSimulationRunner.PolicyOutputRoot(
                    M08BIndustryPolicy.FarmManagedV1),
                Does.EndWith("farm-managed-v1"));
        }

        [Test]
        public void DefaultRunRootsTargetPolicyDirectoriesAndNeverTheHistoricalRoot()
        {
            string historicalRoot = Path.GetFullPath(
                M08BLongTermSimulationRunner.DefaultOutputRoot);

            foreach (M08BIndustryPolicy policy in Enum.GetValues(typeof(M08BIndustryPolicy)))
            {
                string expected = Path.GetFullPath(
                    M08BLongTermSimulationRunner.PolicyOutputRoot(policy));
                Assert.That(
                    Path.GetFullPath(M08BLongTermSimulationRunner.ResolveArtifactRoot(null, policy)),
                    Is.EqualTo(expected),
                    "a null root must resolve to the policy directory for " + policy);
                Assert.That(
                    Path.GetFullPath(M08BLongTermSimulationRunner.ResolveArtifactRoot("   ", policy)),
                    Is.EqualTo(expected),
                    "a blank root must resolve to the policy directory for " + policy);
                Assert.That(expected, Is.Not.EqualTo(historicalRoot),
                    "a default run must never target the historical artifact root");
                Assert.That(Path.GetDirectoryName(expected), Is.EqualTo(historicalRoot),
                    "policy directories stay directly below the historical root");
            }

            Assert.That(
                Path.GetFullPath(M08BLongTermSimulationRunner.ResolveArtifactRoot(
                    null, M08BIndustryPolicy.Unmanaged)),
                Is.EqualTo(Path.Combine(historicalRoot,
                    M08BLongTermSimulationRunner.UnmanagedPolicyId)),
                "the no-policy default is the unmanaged directory");

            // The ignored Logs tool passed DefaultOutputRoot explicitly, so an explicit legacy
            // root must be rejected in every spelling before any write happens.
            string[] legacyRootSpellings =
            {
                historicalRoot,
                M08BLongTermSimulationRunner.DefaultOutputRoot,
                historicalRoot + Path.DirectorySeparatorChar,
                historicalRoot + Path.AltDirectorySeparatorChar,
                Path.Combine(historicalRoot, "."),
                historicalRoot.ToUpperInvariant()
            };
            List<string> entriesBefore = Directory.EnumerateFileSystemEntries(historicalRoot)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToList();
            foreach (string spelling in legacyRootSpellings)
            {
                Assert.Throws<ArgumentException>(() =>
                        M08BLongTermSimulationRunner.ResolveArtifactRoot(spelling,
                            M08BIndustryPolicy.FarmManagedV1),
                    "an explicit legacy root must be rejected: " + spelling);
            }
            Assert.That(Directory.EnumerateFileSystemEntries(historicalRoot)
                    .OrderBy(path => path, StringComparer.Ordinal),
                Is.EqualTo(entriesBefore),
                "rejecting the legacy root must not write or create anything there");

            Assert.That(
                M08BLongTermSimulationRunner.ResolveArtifactRoot(_root,
                    M08BIndustryPolicy.FarmManagedV1),
                Is.EqualTo(_root),
                "an explicitly supplied output root stays functional");
        }

        private static PersonId FarmAssigneeOf(GameSession session) =>
            session.Industry.Slot(IndustryCatalog.FarmId).AssigneeId;

        private static IEnumerable<PersonId> LivingMembersByOrdinalId(GameSession session)
        {
            return session.Succession.World.Family.Members
                .Select(member => member.PersonId)
                .Where(personId => session.Succession.World.IsLiving(personId))
                .OrderBy(personId => personId.Value ?? string.Empty, StringComparer.Ordinal);
        }

        private static EventCatalogData LoadCatalog()
        {
            return JsonUtility.FromJson<EventCatalogData>(
                File.ReadAllText("Assets/Resources/Events/event-catalog-v1.json"));
        }

        private static void EnqueuePendingEvent(GameSession session, string eventId)
        {
            FieldInfo field = typeof(GameSession).GetField("_eventRuntimeState",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            object runtime = field.GetValue(session);
            Assert.That(runtime, Is.Not.Null);
            MethodInfo enqueue = runtime.GetType().GetMethod("Enqueue",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(enqueue, Is.Not.Null);
            enqueue.Invoke(runtime, new object[] { eventId });
            Assert.That(session.PendingEventId, Is.EqualTo(eventId));
        }

        private static void WriteFile(string root, string relativePath, string content)
        {
            string fullPath = Path.Combine(root, relativePath.Replace('/',
                Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            File.WriteAllText(fullPath, content);
        }

        private static IndustryState IndustryStateOf(GameSession session)
        {
            FieldInfo field = typeof(GameSession).GetField("_industryState",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            return (IndustryState)field.GetValue(session);
        }

        private static string CsvField(string csv, string column)
        {
            string[] lines = csv.Replace("\r\n", "\n").Split('\n');
            string[] header = lines[0].Split(',');
            int index = Array.IndexOf(header, column);
            Assert.That(index, Is.GreaterThanOrEqualTo(0), "missing column " + column);
            return lines[1].Split(',')[index];
        }

        private static int Occurrences(string text, string value)
        {
            int count = 0;
            int index = 0;
            while ((index = text.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
            {
                count++;
                index += value.Length;
            }
            return count;
        }

        private static M08BSeedResult SyntheticSeed(ulong seed, int year, bool alive,
            int livingMembers, int wealth)
        {
            var result = new M08BSeedResult
            {
                Seed = seed,
                SeedHex = "0x" + seed.ToString("X16"),
                Termination = M08BTerminationReason.Completed,
                MonthsSimulated = 120,
                EventsTriggered = 2,
                FinalLivingMembers = livingMembers
            };
            result.Checkpoints.Add(new M08BCheckpointResult
            {
                Year = year,
                MonthsRequired = year * 12,
                Reached = true,
                Alive = alive,
                LivingMembers = livingMembers,
                MeanLivingAgeTenths = alive ? 300 : 0,
                Funds = wealth,
                FoodInventory = 0,
                Supplies = 0,
                FoodPriceIndexPermille = 1000,
                MaterialsPriceIndexPermille = 1000,
                CompositePriceIndexPermille = 1000,
                Wealth = wealth,
                EventCount = 2,
                EventsPer1000Months = 16
            });
            result.Checkpoints.Add(new M08BCheckpointResult
            {
                Year = 25,
                MonthsRequired = 300,
                Reached = false
            });
            return result;
        }
    }
}
