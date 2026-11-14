using System;
using System.Collections.Generic;
using NUnit.Framework;
using SengokuChronicle.Domain;

namespace SengokuChronicle.Tests.EditMode
{
    /// <summary>M08A Task 1: deterministic annual mortality and NPC marriage plans.</summary>
    public sealed class FamilyLifeCycleRulesTests
    {
        private static readonly GameDate December = new GameDate(1560, 12, 1);
        private static readonly GameDate January = new GameDate(1561, 1, 1);

        private static readonly PersonId Alpha = new PersonId("person.test.alpha");
        private static readonly PersonId Beta = new PersonId("person.test.beta");
        private static readonly PersonId Gamma = new PersonId("person.test.gamma");
        private static readonly PersonId Child = new PersonId("person.test.child");
        private static readonly PersonId Unknown = new PersonId("person.test.unknown");
        private static readonly PersonId Spouse = new PersonId("person.test.spouse");

        [TestCase(1560, 12, 1561, 1, true)]
        [TestCase(1560, 11, 1560, 12, false)]
        [TestCase(1561, 1, 1561, 2, false)]
        [TestCase(1560, 12, 1561, 2, false)]
        [TestCase(1560, 12, 1560, 1, false)]
        public void AnnualBoundary_IsExactlyDecemberToNextJanuary(int currentYear,
            int currentMonth, int nextYear, int nextMonth, bool expected)
        {
            var current = new GameDate(currentYear, currentMonth, 1);
            var next = new GameDate(nextYear, nextMonth, 1);

            Assert.That(FamilyLifeCycleRules.IsAnnualBoundary(current, next),
                Is.EqualTo(expected));
        }

        [TestCase(0, 5)]
        [TestCase(14, 5)]
        [TestCase(15, 5)]
        [TestCase(39, 5)]
        [TestCase(40, 10)]
        [TestCase(59, 10)]
        [TestCase(60, 40)]
        [TestCase(69, 40)]
        [TestCase(70, 120)]
        [TestCase(79, 120)]
        [TestCase(80, 250)]
        [TestCase(120, 250)]
        public void MortalityBandBoundaries_MatchTheApprovedPercentages(int age,
            int expectedPermille)
        {
            Assert.That(FamilyLifeCycleRules.MortalityPermilleForAge(age),
                Is.EqualTo(expectedPermille));
        }

        [Test]
        public void NonBoundaryMonth_ConsumesNoRandomValuesAndPlansNothing()
        {
            SuccessionState state = BuildFamily();
            var used = Stream(31UL);
            var control = Stream(31UL);

            Assert.That(FamilyLifeCycleRules.PlanNaturalDeaths(state,
                new GameDate(1561, 1, 1), new GameDate(1561, 2, 1), used), Is.Empty);
            Assert.That(FamilyLifeCycleRules.PlanNpcMarriages(state,
                new GameDate(1561, 1, 1), new GameDate(1561, 2, 1), used), Is.Empty);

            Assert.That(used.NextUInt64(), Is.EqualTo(control.NextUInt64()));
        }

        [Test]
        public void DeathCandidates_ConsumeOneRollEachInOrdinalIdOrder()
        {
            SuccessionState state = BuildOnly(Gamma, Alpha, Beta);

            const ulong seed = 77UL;
            IReadOnlyList<PersonId> deaths = FamilyLifeCycleRules.PlanNaturalDeaths(state,
                December, January, Stream(seed));

            var control = Stream(seed);
            var expected = new List<PersonId>();
            PersonId[] ordered = { Alpha, Beta, Gamma };
            for (int i = 0; i < ordered.Length; i++)
            {
                int roll = control.NextInt(FamilyLifeCycleRules.MortalityRollExclusiveMax);
                if (roll < FamilyLifeCycleRules.MortalityPermilleForAge(30))
                    expected.Add(ordered[i]);
            }

            Assert.That(deaths, Is.EqualTo(expected),
                "one roll per candidate, ordinal person-ID order");

            var replay = Stream(seed);
            Assert.That(FamilyLifeCycleRules.PlanNaturalDeaths(state, December, January,
                replay), Is.EqualTo(deaths), "same seed replays the same death set");
        }

        [Test]
        public void DeathCandidateSnapshot_IsOrderedAndSkipsDeadMembers()
        {
            SuccessionState state = BuildFamily();
            state.World.TrySetLife(Beta, new SuccessionPersonLife(December.Year - 30, false,
                false), out _);

            var ids = new List<string>();
            foreach (PersonId id in FamilyLifeCycleRules.CaptureDeathCandidateIds(state))
                ids.Add(id.Value);

            Assert.That(ids, Is.EqualTo(new[]
            {
                "person.test.alpha", "person.test.child", "person.test.gamma",
                "person.test.spouse", "person.test.unknown"
            }), "dead people are excluded, living members stay ordered");
        }

        [Test]
        public void MortalityRoll_UsesTheExactPermilleThreshold()
        {
            SuccessionState state = BuildOnly(Alpha);

            Assert.That(TryFindFirstRoll(out ulong dyingSeed, roll => roll == 4), Is.True);
            Assert.That(TryFindFirstRoll(out ulong survivorSeed, roll => roll == 5), Is.True);

            Assert.That(FamilyLifeCycleRules.PlanNaturalDeaths(state, December, January,
                Stream(dyingSeed)), Is.EqualTo(new[] { Alpha }));
            Assert.That(FamilyLifeCycleRules.PlanNaturalDeaths(state, December, January,
                Stream(survivorSeed)), Is.Empty);
        }

        [Test]
        public void EightiethBirthday_ReachesTheHighestMortalityBand()
        {
            PersonId candidate = new PersonId("person.test.alpha");
            var world = new SuccessionWorld(new FamilyId("family.test.lifecycle"), "测试家");
            Add(world, candidate, January.Year - 80, PersonGender.Male,
                FamilyMemberRole.Head, alive: true);
            var state = new SuccessionState(world) { HeadPersonId = candidate };

            Assert.That(state.World.TryGetLife(candidate, out SuccessionPersonLife life), Is.True);
            Assert.That(life.AgeAt(January), Is.EqualTo(80));
            Assert.That(FamilyLifeCycleRules.MortalityPermilleForAge(life.AgeAt(January)),
                Is.EqualTo(250));
        }

        [Test]
        public void NpcMarriage_CandidatesExcludeHeadMinorsUnknownGenderAndMarried()
        {
            SuccessionState state = BuildFamily();

            IReadOnlyList<NpcMarriageProposal> proposals =
                FamilyLifeCycleRules.PlanNpcMarriages(state, December, January,
                    FirstRollStream(roll => roll == 0));

            var ids = new List<string>();
            foreach (NpcMarriageProposal proposal in proposals)
                ids.Add(proposal.CandidateId.Value);

            Assert.That(ids, Is.EqualTo(new[] { "person.test.gamma" }),
                "the head, a minor, an unspecified gender and married people never marry");
        }

        [Test]
        public void NpcMarriage_WidowedAndOlderCandidatesRemainEligible()
        {
            PersonId widower = new PersonId("person.test.alpha");
            PersonId deceasedSpouse = new PersonId("person.test.late");
            var world = new SuccessionWorld(new FamilyId("family.test.lifecycle"), "测试家");
            Add(world, widower, January.Year - 71, PersonGender.Male,
                FamilyMemberRole.CollateralKin, alive: true);
            Add(world, deceasedSpouse, January.Year - 70, PersonGender.Female,
                FamilyMemberRole.Spouse, alive: false);
            Link(world, widower, deceasedSpouse, KinshipType.Spouse);
            var state = new SuccessionState(world);

            IReadOnlyList<NpcMarriageProposal> proposals =
                FamilyLifeCycleRules.PlanNpcMarriages(state, December, January,
                    FirstRollStream(roll => roll == 0));

            Assert.That(proposals, Has.Count.EqualTo(1));
            Assert.That(proposals[0].CandidateId, Is.EqualTo(widower));
            Assert.That(proposals[0].CandidateAge, Is.EqualTo(71),
                "marriage is not filtered by the fertility-age window");
        }

        [Test]
        public void NpcMarriage_UsesTheExactFifteenPercentThreshold()
        {
            SuccessionState state = BuildOnly(Alpha);

            Assert.That(TryFindFirstRoll(out ulong marrySeed, roll => roll == 14,
                FamilyLifeCycleRules.MarriageRollExclusiveMax), Is.True);
            Assert.That(TryFindFirstRoll(out ulong singleSeed, roll => roll == 15,
                FamilyLifeCycleRules.MarriageRollExclusiveMax), Is.True);

            Assert.That(FamilyLifeCycleRules.PlanNpcMarriages(state, December, January,
                Stream(marrySeed)), Has.Count.EqualTo(1));
            Assert.That(FamilyLifeCycleRules.PlanNpcMarriages(state, December, January,
                Stream(singleSeed)), Is.Empty);
        }

        [Test]
        public void NpcMarriage_PlansAreStableAndNeverDuplicateCandidates()
        {
            SuccessionState state = BuildOnly(Alpha, Beta);
            var seen = new HashSet<string>(StringComparer.Ordinal);

            IReadOnlyList<NpcMarriageProposal> first =
                FamilyLifeCycleRules.PlanNpcMarriages(state, December, January,
                    AllMarryStream(2));
            IReadOnlyList<NpcMarriageProposal> second =
                FamilyLifeCycleRules.PlanNpcMarriages(state, December, January,
                    AllMarryStream(2));

            Assert.That(first, Has.Count.EqualTo(2));
            Assert.That(second.Count, Is.EqualTo(first.Count));
            for (int i = 0; i < first.Count; i++)
            {
                Assert.That(second[i].CandidateId, Is.EqualTo(first[i].CandidateId));
                Assert.That(second[i].CandidateGender, Is.EqualTo(first[i].CandidateGender));
                Assert.That(second[i].CandidateAge, Is.EqualTo(first[i].CandidateAge));
                Assert.That(seen.Add(first[i].CandidateId.Value), Is.True,
                    "each person is proposed at most once per annual settlement");
            }
        }

        [Test]
        public void NpcMarriage_ASettlementStartSnapshotDefersTheNewWidowToALaterYear()
        {
            SuccessionState state = BuildFamily();
            IReadOnlyList<PersonId> snapshot =
                FamilyLifeCycleRules.CaptureMarriageCandidateIds(state, January);

            Assert.That(snapshot, Is.EqualTo(new[] { Gamma }),
                "Beta still has a living spouse when the settlement begins");
            Assert.That(state.World.TrySetLife(Spouse,
                new SuccessionPersonLife(December.Year - 28, false, false), out _), Is.True);

            IReadOnlyList<NpcMarriageProposal> sameSettlement =
                FamilyLifeCycleRules.PlanNpcMarriages(snapshot, state, December, January,
                    FirstRollStream(roll => roll == 0));

            Assert.That(CandidateIdsOf(sameSettlement), Does.Not.Contain(Beta.Value),
                "the settlement that widowed Beta does not remarry him");
            Assert.That(CandidateIdsOf(sameSettlement), Is.EqualTo(new[] { Gamma.Value }));
            Assert.That(FamilyLifeCycleRules.CaptureMarriageCandidateIds(state, January),
                Does.Contain(Beta),
                "only the frozen start snapshot decides this settlement");

            var nextJanuary = new GameDate(1562, 1, 1);
            IReadOnlyList<NpcMarriageProposal> nextYear =
                FamilyLifeCycleRules.PlanNpcMarriages(state, new GameDate(1561, 12, 1),
                    nextJanuary, FirstRollStream(roll => roll == 0));

            Assert.That(CandidateIdsOf(nextYear), Does.Contain(Beta.Value),
                "the widower is eligible at a later annual settlement");
        }

        [Test]
        public void Plans_NeverMutateTheSourceState()
        {
            SuccessionState state = BuildFamily();
            int persons = state.World.PersonCount;
            int members = state.World.Family.MemberCount;
            int links = state.World.Kinship.Count;

            FamilyLifeCycleRules.PlanNaturalDeaths(state, December, January, Stream(5UL));
            FamilyLifeCycleRules.PlanNpcMarriages(state, December, January, Stream(5UL));

            Assert.That(state.World.PersonCount, Is.EqualTo(persons));
            Assert.That(state.World.Family.MemberCount, Is.EqualTo(members));
            Assert.That(state.World.Kinship.Count, Is.EqualTo(links));
        }

        // ---- fixtures -------------------------------------------------------

        private static IReadOnlyList<string> CandidateIdsOf(
            IReadOnlyList<NpcMarriageProposal> proposals)
        {
            var ids = new List<string>(proposals.Count);
            foreach (NpcMarriageProposal proposal in proposals)
                ids.Add(proposal.CandidateId.Value);
            return ids;
        }

        /// <summary>
        /// Alpha is the head (male, 30). Beta is a married adult male; Spouse is his
        /// living adult wife. Gamma is a single adult woman. Child is a minor. Unknown
        /// has an unspecified gender. Only Gamma is an eligible autonomous-marriage
        /// candidate; everyone alive is a mortality candidate.
        /// </summary>
        private static SuccessionState BuildFamily()
        {
            var world = new SuccessionWorld(new FamilyId("family.test.lifecycle"), "测试家");
            Add(world, Alpha, December.Year - 30, PersonGender.Male, FamilyMemberRole.Head,
                alive: true);
            Add(world, Beta, December.Year - 30, PersonGender.Male,
                FamilyMemberRole.CollateralKin, alive: true);
            Add(world, Spouse, December.Year - 28, PersonGender.Female,
                FamilyMemberRole.Spouse, alive: true);
            Add(world, Gamma, December.Year - 25, PersonGender.Female,
                FamilyMemberRole.DirectKin, alive: true);
            Add(world, Child, December.Year - 8, PersonGender.Male,
                FamilyMemberRole.DirectKin, alive: true);
            Add(world, Unknown, December.Year - 40, PersonGender.Unspecified,
                FamilyMemberRole.Retainer, alive: true);
            Link(world, Beta, Spouse, KinshipType.Spouse);
            return new SuccessionState(world) { HeadPersonId = Alpha };
        }

        /// <summary>Minimal family with exactly the supplied members and no head.</summary>
        private static SuccessionState BuildOnly(params PersonId[] ids)
        {
            var world = new SuccessionWorld(new FamilyId("family.test.lifecycle"), "测试家");
            foreach (PersonId id in ids)
            {
                int birthYear = id == Spouse || id == Gamma
                    ? December.Year - 25
                    : December.Year - 30;
                PersonGender gender = id == Spouse || id == Gamma
                    ? PersonGender.Female
                    : PersonGender.Male;
                Add(world, id, birthYear, gender, FamilyMemberRole.DirectKin, alive: true);
            }
            return new SuccessionState(world);
        }

        private static void Add(SuccessionWorld world, PersonId id, int birthYear,
            PersonGender gender, FamilyMemberRole role, bool alive)
        {
            Assert.That(world.TryAddPerson(new PersonState(id, id.Value, gender),
                new SuccessionPersonLife(birthYear, alive, false), out string error), Is.True,
                error);
            Assert.That(world.TryAddMember(id, role, out error), Is.True, error);
        }

        private static void Link(SuccessionWorld world, PersonId from, PersonId to,
            KinshipType type)
        {
            Assert.That(KinshipLink.TryCreate(from, to, type, out KinshipLink link,
                out string error), Is.True, error);
            Assert.That(world.TryAddKinship(link, out error), Is.True, error);
        }

        private static DeterministicRandomStream Stream(ulong seed) =>
            RandomStateManager.Create(seed).GetStream(RandomStreamKind.WorldSimulation);

        /// <summary>A WorldSimulation stream whose first roll satisfies the predicate.</summary>
        private static DeterministicRandomStream FirstRollStream(Func<int, bool> matches,
            int exclusiveMax = FamilyLifeCycleRules.MarriageRollExclusiveMax)
        {
            Assert.That(TryFindFirstRoll(out ulong seed, matches, exclusiveMax), Is.True,
                "no deterministic seed produced the expected first roll");
            return Stream(seed);
        }

        /// <summary>A stream whose first <paramref name="count"/> 100-sided rolls all marry.</summary>
        private static DeterministicRandomStream AllMarryStream(int count)
        {
            for (ulong candidate = 1; candidate <= 200000; candidate++)
            {
                DeterministicRandomStream stream = Stream(candidate);
                bool all = true;
                for (int i = 0; i < count; i++)
                {
                    if (!stream.TryNextInt(FamilyLifeCycleRules.MarriageRollExclusiveMax,
                            out int roll) || roll >= FamilyLifeCycleRules.MarriagePercent)
                    {
                        all = false;
                        break;
                    }
                }
                if (all)
                    return Stream(candidate);
            }
            Assert.Fail("no deterministic seed produced " + count + " consecutive marriage rolls");
            return null;
        }

        private static bool TryFindFirstRoll(out ulong seed, Func<int, bool> firstRollMatches,
            int exclusiveMax = FamilyLifeCycleRules.MortalityRollExclusiveMax)
        {
            for (ulong candidate = 1; candidate <= 200000; candidate++)
            {
                DeterministicRandomStream stream = Stream(candidate);
                if (!stream.TryNextInt(exclusiveMax, out int roll))
                    continue;
                if (!firstRollMatches(roll))
                    continue;
                seed = candidate;
                return true;
            }
            seed = 0;
            return false;
        }
    }
}
