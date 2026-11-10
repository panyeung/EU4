using NUnit.Framework;
using SengokuChronicle.Domain;

namespace SengokuChronicle.Tests.EditMode
{
    /// <summary>M04B Task 2: deterministic eligibility, monthly rolls and gestation.</summary>
    public sealed class FamilyPregnancyRulesTests
    {
        private static readonly GameDate Current = new GameDate(1560, 1, 1);
        private static readonly GameDate Next = new GameDate(1560, 2, 1);
        private static readonly PersonId Male = new PersonId("person.test.husband");
        private static readonly PersonId Wife = new PersonId("person.test.wife");
        private static readonly PersonId WifeTwo = new PersonId("person.test.wife2");

        [TestCase(15, false)]
        [TestCase(16, true)]
        [TestCase(40, true)]
        [TestCase(41, false)]
        public void FemaleAgeBounds_AreInclusive(int age, bool eligible)
        {
            SuccessionState state = BuildPair(age, 30, PersonGender.Female,
                PersonGender.Male);

            Assert.That(FamilyPregnancyRules.GetEligiblePairs(state, Current).Count,
                Is.EqualTo(eligible ? 1 : 0));
        }

        [TestCase(15, false)]
        [TestCase(16, true)]
        [TestCase(60, true)]
        [TestCase(61, false)]
        public void MaleAgeBounds_AreInclusive(int age, bool eligible)
        {
            SuccessionState state = BuildPair(30, age, PersonGender.Female,
                PersonGender.Male);

            Assert.That(FamilyPregnancyRules.GetEligiblePairs(state, Current).Count,
                Is.EqualTo(eligible ? 1 : 0));
        }

        [Test]
        public void GenderAndLifeFacts_ControlEligibility()
        {
            Assert.That(FamilyPregnancyRules.GetEligiblePairs(
                BuildPair(30, 30, PersonGender.Unspecified, PersonGender.Male),
                Current).Count, Is.Zero, "unknown gender is never inferred");
            Assert.That(FamilyPregnancyRules.GetEligiblePairs(
                BuildPair(30, 30, PersonGender.Female, PersonGender.Female),
                Current).Count, Is.Zero);
            Assert.That(FamilyPregnancyRules.GetEligiblePairs(
                BuildPair(30, 30, PersonGender.Male, PersonGender.Male),
                Current).Count, Is.Zero);

            SuccessionState dead = BuildPair(30, 30, PersonGender.Female,
                PersonGender.Male);
            dead.World.TrySetLife(Wife, new SuccessionPersonLife(1530, false, false), out _);
            Assert.That(FamilyPregnancyRules.GetEligiblePairs(dead, Current).Count, Is.Zero);

            SuccessionState nonMember = BuildPair(30, 30, PersonGender.Female,
                PersonGender.Male, addWifeAsMember: false);
            Assert.That(FamilyPregnancyRules.GetEligiblePairs(nonMember, Current).Count,
                Is.Zero);
        }

        [Test]
        public void EligiblePair_ConceptionIsDeterministicAndUsesWorldStream()
        {
            SuccessionState state = BuildPair(30, 30, PersonGender.Female,
                PersonGender.Male);

            Assert.That(TryFindSeed(state, Current, Next, 1,
                out ulong seed, out FamilyPregnancyMonthPlan first), Is.True,
                "a 3% roll must succeed for some deterministic seed");

            FamilyPregnancyMonthPlan second = FamilyPregnancyRules.PlanMonth(state, Current,
                Next, RandomStateManager.Create(seed)
                    .GetStream(RandomStreamKind.WorldSimulation));

            Assert.That(second.Starts, Has.Count.EqualTo(1));
            Assert.That(second.Starts[0], Is.EqualTo(first.Starts[0]));
            Assert.That(first.Starts[0].GestatingPersonId, Is.EqualTo(Wife));
            Assert.That(first.Starts[0].OtherParentId, Is.EqualTo(Male));
            Assert.That(first.Starts[0].ConceptionDate, Is.EqualTo(Next));
            Assert.That(first.Starts[0].DueDate, Is.EqualTo(new GameDate(1560, 11, 1)));
        }

        [Test]
        public void IneligiblePairs_ConsumeNoRandomValues()
        {
            SuccessionState tooYoung = BuildPair(15, 30, PersonGender.Female,
                PersonGender.Male);
            var used = RandomStateManager.Create(91UL)
                .GetStream(RandomStreamKind.WorldSimulation);
            var control = RandomStateManager.Create(91UL)
                .GetStream(RandomStreamKind.WorldSimulation);

            FamilyPregnancyRules.PlanMonth(tooYoung, Current, Next, used);

            Assert.That(used.NextUInt64(), Is.EqualTo(control.NextUInt64()));
        }

        [Test]
        public void AlreadyPregnantPair_ConsumesNoRandomValues()
        {
            SuccessionState state = BuildPair(30, 30, PersonGender.Female,
                PersonGender.Male);
            state.World.Pregnancies.TryAdd(Record(Wife, Male, new GameDate(1560, 1, 1)),
                out string error);
            Assert.That(error, Is.Empty);

            var used = RandomStateManager.Create(55UL)
                .GetStream(RandomStreamKind.WorldSimulation);
            var control = RandomStateManager.Create(55UL)
                .GetStream(RandomStreamKind.WorldSimulation);

            FamilyPregnancyMonthPlan plan = FamilyPregnancyRules.PlanMonth(state, Current,
                Next, used);

            Assert.That(plan.Starts, Is.Empty);
            Assert.That(used.NextUInt64(), Is.EqualTo(control.NextUInt64()));
        }

        [Test]
        public void NineMonthlySteps_CompleteExactlyOnTheNinthAdvance()
        {
            SuccessionState state = BuildPair(30, 30, PersonGender.Female,
                PersonGender.Male);
            state.World.Pregnancies.TryAdd(Record(Wife, Male, new GameDate(1560, 1, 1)),
                out string error);
            Assert.That(error, Is.Empty);

            for (int month = 1; month <= 8; month++)
            {
                GameDate current = new GameDate(1560, month, 1);
                GameDate next = FamilyPregnancyRecord.AddMonths(current, 1);
                FamilyPregnancyMonthPlan plan = FamilyPregnancyRules.PlanMonth(state, current,
                    next, RandomStateManager.Create(1UL)
                        .GetStream(RandomStreamKind.WorldSimulation));
                Assert.That(plan.Completions, Is.Empty, "month " + month);
            }

            FamilyPregnancyMonthPlan ninth = FamilyPregnancyRules.PlanMonth(state,
                new GameDate(1560, 9, 1), new GameDate(1560, 10, 1),
                RandomStateManager.Create(1UL)
                    .GetStream(RandomStreamKind.WorldSimulation));
            Assert.That(ninth.Completions, Has.Count.EqualTo(1));
        }

        [Test]
        public void DecemberToJanuaryDueDate_CompletesOnce()
        {
            SuccessionState state = BuildPair(30, 30, PersonGender.Female,
                PersonGender.Male);
            state.World.Pregnancies.TryAdd(Record(Wife, Male, new GameDate(1560, 5, 1)),
                out string error);
            Assert.That(error, Is.Empty);

            FamilyPregnancyMonthPlan plan = FamilyPregnancyRules.PlanMonth(state,
                new GameDate(1561, 1, 1), new GameDate(1561, 2, 1),
                RandomStateManager.Create(1UL)
                    .GetStream(RandomStreamKind.WorldSimulation));

            Assert.That(plan.Completions, Has.Count.EqualTo(1));
            Assert.That(plan.Completions[0].DueDate, Is.EqualTo(new GameDate(1561, 2, 1)));
        }

        [Test]
        public void Polygamy_AllowsEachWifeOnePregnancyButNotTheSamePairTwice()
        {
            SuccessionState state = BuildPolygamousFamily();
            Assert.That(TryFindSeedWithBothWives(state, out ulong seed,
                out FamilyPregnancyMonthPlan both), Is.True,
                "two independent 3% rolls must both succeed for some seed");
            Assert.That(both.Starts, Has.Count.EqualTo(2));

            FamilyPregnancyMonthPlan replay = FamilyPregnancyRules.PlanMonth(state, Current,
                Next, RandomStateManager.Create(seed)
                    .GetStream(RandomStreamKind.WorldSimulation));
            Assert.That(replay.Starts, Is.EqualTo(both.Starts));

            state.World.Pregnancies.TryAdd(Record(Wife, Male, Current), out string error);
            Assert.That(error, Is.Empty);
            Assert.That(TryFindSeedWhereOnlyWifeTwoConceives(state, out _), Is.True,
                "the second wife must remain independently eligible");
        }

        [Test]
        public void DuePregnancy_DoesNotReconceiveInTheSameSettlement()
        {
            SuccessionState state = BuildPair(30, 30, PersonGender.Female,
                PersonGender.Male);
            GameDate current = new GameDate(1560, 9, 1);
            GameDate next = new GameDate(1560, 10, 1);
            FamilyPregnancyRecord due = Record(Wife, Male, new GameDate(1560, 1, 1));
            state.World.Pregnancies.TryAdd(due, out string error);
            Assert.That(error, Is.Empty);

            for (ulong seed = 1; seed <= 50; seed++)
            {
                FamilyPregnancyMonthPlan plan = FamilyPregnancyRules.PlanMonth(state,
                    current, next, RandomStateManager.Create(seed)
                        .GetStream(RandomStreamKind.WorldSimulation));
                Assert.That(plan.Completions, Has.Count.EqualTo(1));
                Assert.That(plan.Starts, Is.Empty, "seed " + seed);
            }
        }

        private static SuccessionState BuildPolygamousFamily()
        {
            var world = new SuccessionWorld(new FamilyId("family.test.polygamy"), "测试家");
            Add(world, Male, 1530, PersonGender.Male, FamilyMemberRole.Head);
            Add(world, Wife, 1530, PersonGender.Female, FamilyMemberRole.Spouse);
            Add(world, WifeTwo, 1532, PersonGender.Female, FamilyMemberRole.Spouse);
            Link(world, Male, Wife, KinshipType.Spouse);
            Link(world, Male, WifeTwo, KinshipType.Spouse);
            return new SuccessionState(world)
            {
                HeadPersonId = Male,
                DesignatedHeirId = default
            };
        }

        private static SuccessionState BuildPair(int femaleAge, int maleAge,
            PersonGender femaleGender, PersonGender maleGender,
            bool addWifeAsMember = true)
        {
            var world = new SuccessionWorld(new FamilyId("family.test.rules"), "测试家");
            Add(world, Male, 1560 - maleAge, maleGender, FamilyMemberRole.Head);
            Add(world, Wife, 1560 - femaleAge, femaleGender, FamilyMemberRole.Spouse,
                addWifeAsMember);
            Link(world, Male, Wife, KinshipType.Spouse);
            return new SuccessionState(world) { HeadPersonId = Male };
        }

        private static void Add(SuccessionWorld world, PersonId id, int birthYear,
            PersonGender gender, FamilyMemberRole role, bool asMember = true)
        {
            Assert.That(world.TryAddPerson(new PersonState(id, id.Value, gender),
                new SuccessionPersonLife(birthYear, true, false), out string error), Is.True,
                error);
            if (asMember)
                Assert.That(world.TryAddMember(id, role, out error), Is.True, error);
        }

        private static void Link(SuccessionWorld world, PersonId from, PersonId to,
            KinshipType type)
        {
            Assert.That(KinshipLink.TryCreate(from, to, type, out KinshipLink link,
                out string error), Is.True, error);
            Assert.That(world.TryAddKinship(link, out error), Is.True, error);
        }

        private static FamilyPregnancyRecord Record(PersonId mother, PersonId father,
            GameDate conception)
        {
            Assert.That(FamilyPregnancyRecord.TryCreate(mother, father, conception,
                FamilyPregnancyRecord.AddMonths(conception,
                    FamilyPregnancyRecord.GestationMonths),
                out FamilyPregnancyRecord record, out string error), Is.True, error);
            return record;
        }

        private static bool TryFindSeed(SuccessionState state, GameDate current,
            GameDate next, int minimumStarts, out ulong seed,
            out FamilyPregnancyMonthPlan plan)
        {
            for (ulong candidate = 1; candidate <= 100000; candidate++)
            {
                plan = FamilyPregnancyRules.PlanMonth(state, current, next,
                    RandomStateManager.Create(candidate)
                        .GetStream(RandomStreamKind.WorldSimulation));
                if (plan.Starts.Count >= minimumStarts)
                {
                    seed = candidate;
                    return true;
                }
            }
            seed = 0;
            plan = FamilyPregnancyMonthPlan.Empty;
            return false;
        }

        private static bool TryFindSeedWithBothWives(SuccessionState state, out ulong seed,
            out FamilyPregnancyMonthPlan plan)
        {
            for (ulong candidate = 1; candidate <= 200000; candidate++)
            {
                plan = FamilyPregnancyRules.PlanMonth(state, Current, Next,
                    RandomStateManager.Create(candidate)
                        .GetStream(RandomStreamKind.WorldSimulation));
                if (plan.Starts.Count == 2)
                {
                    seed = candidate;
                    return true;
                }
            }
            seed = 0;
            plan = FamilyPregnancyMonthPlan.Empty;
            return false;
        }

        private static bool TryFindSeedWhereOnlyWifeTwoConceives(SuccessionState state,
            out FamilyPregnancyMonthPlan plan)
        {
            for (ulong candidate = 1; candidate <= 100000; candidate++)
            {
                plan = FamilyPregnancyRules.PlanMonth(state, Current, Next,
                    RandomStateManager.Create(candidate)
                        .GetStream(RandomStreamKind.WorldSimulation));
                if (plan.Starts.Count == 1 &&
                    plan.Starts[0].GestatingPersonId == WifeTwo)
                    return true;
            }
            plan = FamilyPregnancyMonthPlan.Empty;
            return false;
        }
    }
}
