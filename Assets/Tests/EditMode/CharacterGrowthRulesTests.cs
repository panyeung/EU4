using NUnit.Framework;
using SengokuChronicle.Domain;

namespace SengokuChronicle.Tests.EditMode
{
    /// <summary>SC-020 Task 1: fixed, deterministic education growth formulas.</summary>
    public sealed class CharacterGrowthRulesTests
    {
        private static CharacterGrowthValues DemoValues()
        {
            Assert.That(CharacterGrowthValues.TryCreate(15, 12, 20, 10, 13,
                out CharacterGrowthValues values, out string error), Is.True, error);
            return values;
        }

        [Test]
        public void ScholarshipAptitudeAndSpouseMentor_GrowsScholarshipByNine()
        {
            EducationYearResult result = CharacterGrowthRules.Resolve(DemoValues(),
                EducationDirection.Scholarship, GrowthAttribute.Scholarship,
                MentorSpecialty.Scholarship, true);

            Assert.That(result.Before.Scholarship, Is.EqualTo(20));
            Assert.That(result.After.Scholarship, Is.EqualTo(29));
            Assert.That(result.DeltaOf(GrowthAttribute.Scholarship), Is.EqualTo(9));
            Assert.That(result.After.Ninjutsu, Is.EqualTo(15));
            Assert.That(result.Explanation, Does.Contain("共 +9"));
        }

        [Test]
        public void ScholarshipAptitudeAndMartialMentor_GrowsScholarshipBySeven()
        {
            EducationYearResult result = CharacterGrowthRules.Resolve(DemoValues(),
                EducationDirection.Scholarship, GrowthAttribute.Scholarship,
                MentorSpecialty.MartialArts, true);

            Assert.That(result.After.Scholarship, Is.EqualTo(27));
            Assert.That(result.DeltaOf(GrowthAttribute.Scholarship), Is.EqualTo(7));
        }

        [Test]
        public void FreeGrowth_AddsOneToEveryAttributeAndIgnoresMentor()
        {
            EducationYearResult result = CharacterGrowthRules.Resolve(DemoValues(),
                EducationDirection.FreeGrowth, GrowthAttribute.Scholarship,
                MentorSpecialty.Scholarship, true);

            Assert.That(result.After.Ninjutsu, Is.EqualTo(16));
            Assert.That(result.After.MartialArts, Is.EqualTo(13));
            Assert.That(result.After.Scholarship, Is.EqualTo(21));
            Assert.That(result.After.Management, Is.EqualTo(11));
            Assert.That(result.After.Social, Is.EqualTo(14));
            Assert.That(result.Explanation, Does.Contain("自由成长"));
        }

        [Test]
        public void Gains_ClampAtOneHundred()
        {
            Assert.That(CharacterGrowthValues.TryCreate(15, 12, 99, 10, 13,
                out CharacterGrowthValues values, out _), Is.True);

            EducationYearResult result = CharacterGrowthRules.Resolve(values,
                EducationDirection.Scholarship, GrowthAttribute.Scholarship,
                MentorSpecialty.Scholarship, true);

            Assert.That(result.After.Scholarship, Is.EqualTo(100));
            Assert.That(result.DeltaOf(GrowthAttribute.Scholarship), Is.EqualTo(1));
        }

        [TestCase(-1, 0, 0, 0, 0)]
        [TestCase(0, 101, 0, 0, 0)]
        [TestCase(0, 0, -5, 0, 0)]
        [TestCase(0, 0, 0, 1000, 0)]
        [TestCase(0, 0, 0, 0, 1000)]
        public void InvalidInitialValues_AreRejected(int ninjutsu, int martialArts, int scholarship,
            int management, int social)
        {
            Assert.That(CharacterGrowthValues.TryCreate(ninjutsu, martialArts, scholarship, management,
                social, out _, out string error), Is.False);
            Assert.That(error, Is.Not.Empty);
        }

        [Test]
        public void InvalidDirection_IsRejectedWithoutChangingValues()
        {
            CharacterGrowthValues before = DemoValues();
            Assert.That(CharacterGrowthRules.TryResolve(before, (EducationDirection)99,
                GrowthAttribute.Scholarship, MentorSpecialty.Scholarship, true,
                out _, out string error), Is.False);
            Assert.That(error, Does.Contain("方向"));
        }

        [Test]
        public void Plan_RequiresMentorForDirectedGrowthAndRejectsOneForFreeGrowth()
        {
            PersonId learner = new PersonId("person.sc020.child");
            Assert.That(EducationYearPlan.TryCreate(learner, 15, EducationDirection.Scholarship,
                default, out _, out string missingMentor), Is.False);
            Assert.That(missingMentor, Does.Contain("导师"));

            Assert.That(EducationYearPlan.TryCreate(learner, 15, EducationDirection.FreeGrowth,
                new PersonId("person.sc020.spouse"), out _, out string freeMentor), Is.False);
            Assert.That(freeMentor, Does.Contain("自由成长"));

            Assert.That(EducationYearPlan.TryCreate(learner, 16, EducationDirection.FreeGrowth,
                default, out _, out string age), Is.False);
            Assert.That(age, Does.Contain("0..15"));
        }

        [Test]
        public void DirectionNames_AreLocalizedAndStable()
        {
            Assert.That(CharacterGrowthRules.DirectionName(EducationDirection.Scholarship),
                Is.EqualTo("学问"));
            Assert.That(CharacterGrowthRules.DirectionName(EducationDirection.FreeGrowth),
                Is.EqualTo("自由成长"));
            Assert.That(CharacterGrowthRules.SpecialtyName(MentorSpecialty.MartialArts),
                Is.EqualTo("武艺"));
        }
    }
}
