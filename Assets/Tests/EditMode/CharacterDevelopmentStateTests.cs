using NUnit.Framework;
using SengokuChronicle.Domain;

namespace SengokuChronicle.Tests.EditMode
{
    /// <summary>SC-020 Task 1: per-person plans, one-shot settlement and clone isolation.</summary>
    public sealed class CharacterDevelopmentStateTests
    {
        private static readonly PersonId ChildId = new PersonId("person.sc020.child");
        private static readonly PersonId MentorId = new PersonId("person.sc020.spouse");

        private static CharacterDevelopmentState CreateState()
        {
            CharacterGrowthValues.TryCreate(15, 12, 20, 10, 13, out CharacterGrowthValues values, out _);
            CharacterDevelopmentState state = CharacterDevelopmentState.CreateEmpty();
            Assert.That(state.TryAddPerson(ChildId, values, out string error), Is.True, error);
            return state;
        }

        private static EducationYearPlan Plan(EducationDirection direction, PersonId mentor) =>
            EducationYearPlan.TryCreate(ChildId, 15, direction, mentor, out EducationYearPlan plan, out _)
                ? plan
                : throw new AssertionException("test plan must be valid");

        [Test]
        public void SetPlan_ReplacesWithoutChangingValues()
        {
            CharacterDevelopmentState state = CreateState();
            Assert.That(state.TrySetPlan(Plan(EducationDirection.MartialArts,
                new PersonId("person.sc020.head")), out string error), Is.True, error);
            Assert.That(state.TrySetPlan(Plan(EducationDirection.Scholarship, MentorId), out error),
                Is.True, error);

            Assert.That(state.TryGetValues(ChildId, out CharacterGrowthValues values), Is.True);
            Assert.That(values.Scholarship, Is.EqualTo(20));
            Assert.That(state.TryGetPlan(ChildId, 15, out EducationYearPlan stored), Is.True);
            Assert.That(stored.Direction, Is.EqualTo(EducationDirection.Scholarship));
            Assert.That(state.TryValidate(out error), Is.True, error);
        }

        [Test]
        public void SettleYear_AppliesOnceAndRejectsDuplicates()
        {
            CharacterDevelopmentState state = CreateState();
            Assert.That(state.TrySetPlan(Plan(EducationDirection.Scholarship, MentorId), out _), Is.True);

            Assert.That(state.TrySettleYear(ChildId, 15, true, GrowthAttribute.Scholarship,
                MentorSpecialty.Scholarship, true, false,
                out EducationYearResult result, out string error), Is.True, error);
            Assert.That(result.After.Scholarship, Is.EqualTo(29));
            Assert.That(state.TryGetValues(ChildId, out CharacterGrowthValues values), Is.True);
            Assert.That(values.Scholarship, Is.EqualTo(29));

            Assert.That(state.TrySettleYear(ChildId, 15, true, GrowthAttribute.Scholarship,
                MentorSpecialty.Scholarship, true, false,
                out _, out string duplicate), Is.False);
            Assert.That(duplicate, Does.Contain("已经结算"));
            Assert.That(state.TryGetValues(ChildId, out values), Is.True);
            Assert.That(values.Scholarship, Is.EqualTo(29), "a rejected duplicate must not grant growth");
        }

        [Test]
        public void UnplannedYear_UsesFreeGrowth()
        {
            CharacterDevelopmentState state = CreateState();

            Assert.That(state.TrySettleYear(ChildId, 15, true, GrowthAttribute.Scholarship,
                MentorSpecialty.None, true, false,
                out EducationYearResult result, out string error), Is.True, error);

            Assert.That(result.Direction, Is.EqualTo(EducationDirection.FreeGrowth));
            Assert.That(result.After.Ninjutsu, Is.EqualTo(16));
            Assert.That(result.After.Scholarship, Is.EqualTo(21));
        }

        [Test]
        public void MissingMentor_FallsBackToFreeGrowthAtSettlement()
        {
            CharacterDevelopmentState state = CreateState();
            Assert.That(state.TrySetPlan(Plan(EducationDirection.Scholarship, MentorId), out _), Is.True);

            Assert.That(state.TrySettleYear(ChildId, 15, true, GrowthAttribute.Scholarship,
                MentorSpecialty.Scholarship, true, mentorUnavailable: true,
                out EducationYearResult result, out string error), Is.True, error);

            Assert.That(result.Direction, Is.EqualTo(EducationDirection.FreeGrowth));
            Assert.That(result.UsedFreeGrowthFallback, Is.True);
            Assert.That(result.Explanation, Does.Contain("导师"));
            Assert.That(result.After.Scholarship, Is.EqualTo(21));
        }

        [Test]
        public void UnknownPersonOrInvalidAge_IsRejectedWithoutMutation()
        {
            CharacterDevelopmentState state = CreateState();
            Assert.That(state.TrySettleYear(new PersonId("person.unknown"), 15, true,
                GrowthAttribute.Scholarship, MentorSpecialty.Scholarship, true, false,
                out _, out string unknown), Is.False);
            Assert.That(unknown, Does.Contain("成长记录"));
            Assert.That(state.TrySettleYear(ChildId, 16, true, GrowthAttribute.Scholarship,
                MentorSpecialty.Scholarship, true, false, out _, out string age), Is.False);
            Assert.That(age, Does.Contain("0..15"));
            Assert.That(state.TryGetValues(ChildId, out CharacterGrowthValues values), Is.True);
            Assert.That(values.Scholarship, Is.EqualTo(20));
        }

        [Test]
        public void Clone_IsIndependent()
        {
            CharacterDevelopmentState state = CreateState();
            CharacterDevelopmentState clone = state.Clone();
            Assert.That(clone.TrySetPlan(Plan(EducationDirection.Scholarship, MentorId), out _), Is.True);

            Assert.That(state.TryGetPlan(ChildId, 15, out _), Is.False,
                "mutating a clone must not touch the source");
            Assert.That(clone.TrySettleYear(ChildId, 15, true, GrowthAttribute.Scholarship,
                MentorSpecialty.Scholarship, true, false, out _, out _), Is.True);
            Assert.That(state.TryGetValues(ChildId, out CharacterGrowthValues source), Is.True);
            Assert.That(source.Scholarship, Is.EqualTo(20));
        }

        [Test]
        public void SettledYear_CannotBeReplanned()
        {
            CharacterDevelopmentState state = CreateState();
            Assert.That(state.TrySettleYear(ChildId, 15, true, GrowthAttribute.Scholarship,
                MentorSpecialty.Scholarship, true, false, out _, out _), Is.True);

            Assert.That(state.TrySetPlan(Plan(EducationDirection.Scholarship, MentorId), out string error),
                Is.False);
            Assert.That(error, Does.Contain("已结算"));
        }

        [Test]
        public void RestoredYear_RoundTripsAndValidates()
        {
            CharacterDevelopmentState state = CreateState();
            Assert.That(state.TrySetPlan(Plan(EducationDirection.Scholarship, MentorId), out _), Is.True);
            Assert.That(state.TrySettleYear(ChildId, 15, true, GrowthAttribute.Scholarship,
                MentorSpecialty.Scholarship, true, false,
                out EducationYearResult result, out _), Is.True);

            CharacterDevelopmentState restored = CharacterDevelopmentState.CreateEmpty();
            Assert.That(restored.TryAddPerson(ChildId, result.Before, out _), Is.True);
            Assert.That(restored.TryRestorePlan(Plan(EducationDirection.Scholarship, MentorId), out _), Is.True);
            Assert.That(restored.TryRestoreSettledYear(ChildId, 15, result, out string error), Is.True, error);
            Assert.That(restored.TryGetValues(ChildId, out CharacterGrowthValues values), Is.True);
            Assert.That(values.Scholarship, Is.EqualTo(29));
            Assert.That(restored.TryValidate(out error), Is.True, error);
        }
    }
}
