using System.Collections.Generic;
using NUnit.Framework;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;

namespace SengokuChronicle.Tests.EditMode
{
    /// <summary>M08A Task 2: stable spouse identity and complete family registration.</summary>
    public sealed class FamilyMarriageSettlementTests
    {
        private static readonly GameDate Date = new GameDate(1561, 1, 1);
        private static readonly PersonId Candidate = new PersonId("person.test.candidate");
        private static readonly PersonId SecondCandidate =
            new PersonId("person.test.candidate2");

        [Test]
        public void Plan_IsDeterministicForTheSameStateAndDate()
        {
            SuccessionState state = BuildFamily(Candidate);
            var proposals = new[]
            {
                new NpcMarriageProposal(Candidate, PersonGender.Male, 30,
                    Date.Year - 30)
            };

            FamilyMarriagePlan first = FamilyMarriageSettlement.Plan(state, proposals, Date);
            FamilyMarriagePlan second = FamilyMarriageSettlement.Plan(state, proposals, Date);

            Assert.That(first.Succeeded, Is.True, first.Error);
            Assert.That(first.Marriages, Has.Count.EqualTo(1));
            Assert.That(second.Marriages[0].SpouseId, Is.EqualTo(first.Marriages[0].SpouseId));
            Assert.That(second.Marriages[0].SpouseDisplayName,
                Is.EqualTo(first.Marriages[0].SpouseDisplayName));
            Assert.That(first.Marriages[0].SpouseId.Value,
                Is.EqualTo("person.marriage.person-test-candidate.1561"));
        }

        [Test]
        public void Plan_NeverMutatesTheSourceState()
        {
            SuccessionState state = BuildFamily(Candidate);
            int persons = state.World.PersonCount;
            int links = state.World.Kinship.Count;

            FamilyMarriageSettlement.Plan(state, Proposals(), Date);

            Assert.That(state.World.PersonCount, Is.EqualTo(persons));
            Assert.That(state.World.Kinship.Count, Is.EqualTo(links));
        }

        [Test]
        public void Plan_GeneratesOppositeGenderAdultsWithABoundedAgeGap()
        {
            SuccessionState state = BuildFamily(Candidate, SecondCandidate);
            var proposals = new[]
            {
                new NpcMarriageProposal(Candidate, PersonGender.Male, 30, Date.Year - 30),
                new NpcMarriageProposal(SecondCandidate, PersonGender.Female, 17,
                    Date.Year - 17)
            };

            FamilyMarriagePlan plan = FamilyMarriageSettlement.Plan(state, proposals, Date);

            Assert.That(plan.Succeeded, Is.True, plan.Error);
            Assert.That(plan.Marriages[0].SpouseGender, Is.EqualTo(PersonGender.Female));
            Assert.That(plan.Marriages[1].SpouseGender, Is.EqualTo(PersonGender.Male));

            for (int i = 0; i < plan.Marriages.Count; i++)
            {
                int spouseAge = Date.Year - plan.Marriages[i].SpouseBirthYear;
                Assert.That(spouseAge, Is.GreaterThanOrEqualTo(state.AdulthoodAge),
                    "a generated spouse is always an adult");
                int gap = spouseAge - proposals[i].CandidateAge;
                Assert.That(gap, Is.InRange(-FamilyLifeCycleRules.MaximumSpouseAgeGap,
                    FamilyLifeCycleRules.MaximumSpouseAgeGap), "age gap stays within five years");
            }

            Assert.That(FamilyMarriageSettlement.SpouseAgeFor(proposals[1],
                state.AdulthoodAge), Is.GreaterThanOrEqualTo(state.AdulthoodAge),
                "the adult boundary is clamped even for the youngest candidate");
        }

        [Test]
        public void Plan_ResolvesIdsAndNamesAgainstExistingConflicts()
        {
            SuccessionState state = BuildFamily(Candidate);
            FamilyMarriagePlan baseline = FamilyMarriageSettlement.Plan(state, Proposals(), Date);
            FamilyMarriageFact baselineFact = baseline.Marriages[0];

            var occupied = new PersonState(new PersonId(baselineFact.SpouseId.Value),
                baselineFact.SpouseDisplayName, PersonGender.Female);
            Assert.That(state.World.TryAddPerson(occupied,
                new SuccessionPersonLife(Date.Year - 30, true, false), out string error),
                Is.True, error);
            Assert.That(state.World.TryAddMember(occupied.Id, FamilyMemberRole.Guest,
                out error), Is.True, error);

            FamilyMarriagePlan conflicted = FamilyMarriageSettlement.Plan(state, Proposals(), Date);

            Assert.That(conflicted.Succeeded, Is.True, conflicted.Error);
            Assert.That(conflicted.Marriages[0].SpouseId, Is.Not.EqualTo(baselineFact.SpouseId));
            Assert.That(conflicted.Marriages[0].SpouseDisplayName,
                Is.Not.EqualTo(baselineFact.SpouseDisplayName),
                "a display name already in use receives a numeric suffix");
        }

        [Test]
        public void Plan_PreservesProposalOrderAndKeepsNamesUniqueWithinThePlan()
        {
            SuccessionState state = BuildFamily(Candidate, SecondCandidate);
            var proposals = new[]
            {
                new NpcMarriageProposal(Candidate, PersonGender.Male, 30, Date.Year - 30),
                new NpcMarriageProposal(SecondCandidate, PersonGender.Male, 30,
                    Date.Year - 30)
            };

            FamilyMarriagePlan plan = FamilyMarriageSettlement.Plan(state, proposals, Date);

            Assert.That(plan.Marriages[0].CandidateId, Is.EqualTo(Candidate));
            Assert.That(plan.Marriages[1].CandidateId, Is.EqualTo(SecondCandidate));
            Assert.That(plan.Marriages[0].SpouseDisplayName,
                Is.Not.EqualTo(plan.Marriages[1].SpouseDisplayName));
        }

        [Test]
        public void Apply_RegistersACompleteLivingSpouse()
        {
            SuccessionState state = BuildFamily(Candidate);
            CharacterDevelopmentState development = CreateDevelopment(state);
            FamilyMarriagePlan plan = FamilyMarriageSettlement.Plan(state, Proposals(), Date);

            Assert.That(FamilyMarriageSettlement.TryApply(state, plan, ref development,
                out string error), Is.True, error);

            FamilyMarriageFact fact = plan.Marriages[0];
            Assert.That(state.World.Family.IsMember(fact.SpouseId), Is.True);
            Assert.That(state.World.Family.TryGetRole(fact.SpouseId, out FamilyMemberRole role),
                Is.True);
            Assert.That(role, Is.EqualTo(FamilyMemberRole.Spouse));
            Assert.That(state.World.Kinship.Contains(new KinshipLink(Candidate, fact.SpouseId,
                KinshipType.Spouse)), Is.True);
            Assert.That(state.World.Kinship.Contains(new KinshipLink(fact.SpouseId, Candidate,
                KinshipType.Spouse)), Is.True, "the spouse edge reads symmetrically");
            Assert.That(state.World.TryGetLife(fact.SpouseId, out SuccessionPersonLife life),
                Is.True);
            Assert.That(life.Alive, Is.True);
            Assert.That(life.AgeAt(Date), Is.EqualTo(Date.Year - fact.SpouseBirthYear));
            Assert.That(development.Contains(fact.SpouseId), Is.True);
            Assert.That(development.TryGetValues(fact.SpouseId, out CharacterGrowthValues values),
                Is.True);
            Assert.That(values, Is.EqualTo(CharacterGrowthValues.CreateClamped(0, 0, 0, 0, 0)));

            Assert.That(plan.Chronicles, Has.Count.EqualTo(1));
            Assert.That(plan.Chronicles[0].Title, Is.EqualTo("自主婚姻"));
            Assert.That(plan.Chronicles[0].Summary, Does.Contain(fact.SpouseDisplayName));
        }

        [Test]
        public void Apply_AddsNoPregnancyAndOnlyNextMonthMayConceive()
        {
            SuccessionState state = BuildFamily(Candidate);
            CharacterDevelopmentState development = CreateDevelopment(state);
            FamilyMarriagePlan plan = FamilyMarriageSettlement.Plan(state, Proposals(), Date);
            Assert.That(FamilyMarriageSettlement.TryApply(state, plan, ref development,
                out string error), Is.True, error);

            Assert.That(state.World.Pregnancies.Count, Is.Zero,
                "registering the marriage never replays or starts the birth settlement");

            IReadOnlyList<FamilyPregnancyPair> pairs =
                FamilyPregnancyRules.GetEligiblePairs(state, Date);
            Assert.That(pairs, Has.Count.EqualTo(1), "the new adult couple is fertile next month");

            FamilyPregnancyMonthPlan nextMonth = FamilyPregnancyRules.PlanMonth(state, Date,
                new GameDate(1561, 2, 1), FirstRollStream(0));
            Assert.That(nextMonth.Starts, Has.Count.EqualTo(1));
            Assert.That(nextMonth.Starts[0].GestatingPersonId,
                Is.EqualTo(plan.Marriages[0].SpouseId));
        }

        [Test]
        public void Apply_KeepsAnOlderCoupleMarriedWithoutFertilityEligibility()
        {
            SuccessionState state = BuildFamily(Candidate);
            CharacterDevelopmentState development = CreateDevelopment(state);
            var proposals = new[]
            {
                new NpcMarriageProposal(Candidate, PersonGender.Male, 70, Date.Year - 70)
            };
            FamilyMarriagePlan plan = FamilyMarriageSettlement.Plan(state, proposals, Date);
            Assert.That(FamilyMarriageSettlement.TryApply(state, plan, ref development,
                out string error), Is.True, error);

            Assert.That(state.World.IsLiving(plan.Marriages[0].SpouseId), Is.True);
            Assert.That(FamilyPregnancyRules.GetEligiblePairs(state, Date), Is.Empty,
                "a couple outside the M04B fertility window is still formally married");
        }

        // ---- fixtures -------------------------------------------------------

        private static IReadOnlyList<NpcMarriageProposal> Proposals() => new[]
        {
            new NpcMarriageProposal(Candidate, PersonGender.Male, 30, Date.Year - 30)
        };

        private static SuccessionState BuildFamily(params PersonId[] candidates)
        {
            var world = new SuccessionWorld(new FamilyId("family.test.marriage"), "测试家");
            Add(world, new PersonId("person.test.head"), Date.Year - 45, PersonGender.Male,
                FamilyMemberRole.Head);
            foreach (PersonId id in candidates)
                Add(world, id, Date.Year - 30, id == SecondCandidate
                    ? PersonGender.Female
                    : PersonGender.Male, FamilyMemberRole.DirectKin);
            return new SuccessionState(world)
            {
                HeadPersonId = new PersonId("person.test.head")
            };
        }

        private static void Add(SuccessionWorld world, PersonId id, int birthYear,
            PersonGender gender, FamilyMemberRole role)
        {
            Assert.That(world.TryAddPerson(new PersonState(id, id.Value, gender),
                new SuccessionPersonLife(birthYear, true, false), out string error), Is.True,
                error);
            Assert.That(world.TryAddMember(id, role, out error), Is.True, error);
        }

        private static CharacterDevelopmentState CreateDevelopment(SuccessionState state)
        {
            CharacterDevelopmentState development = CharacterDevelopmentState.CreateEmpty();
            foreach (PersonState person in state.World.Persons)
                development.TryAddPerson(person.Id,
                    CharacterGrowthValues.CreateClamped(0, 0, 0, 0, 0), out _);
            return development;
        }

        private static DeterministicRandomStream FirstRollStream(int value)
        {
            for (ulong seed = 1; seed <= 200000; seed++)
            {
                DeterministicRandomStream stream =
                    RandomStateManager.Create(seed).GetStream(RandomStreamKind.WorldSimulation);
                if (stream.TryNextInt(100, out int roll) && roll == value)
                    return RandomStateManager.Create(seed)
                        .GetStream(RandomStreamKind.WorldSimulation);
            }
            Assert.Fail("no deterministic seed produced the expected first roll");
            return null;
        }
    }
}
