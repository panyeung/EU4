using SengokuChronicle.Domain;

namespace SengokuChronicle.Application
{
    /// <summary>
    /// Stable scenario routing for the child-education screens. SC-020 keeps its exact
    /// Aoba values, SC-023 supplies its own three-generation values, and every other
    /// scenario falls back to neutral values without borrowing SC-020's identifiers.
    /// </summary>
    public static class EducationScenarioProvider
    {
        public const int MinimumLearnerAge = EducationYearPlan.MinimumAgeYear;
        public const int MaximumLearnerAge = EducationYearPlan.MaximumAgeYear;

        /// <summary>True for the two scenarios that own a child-education plan.</summary>
        public static bool IsActive(SuccessionState state) =>
            SC020EducationScenario.IsActive(state) || SC023SeclusionScenario.IsActive(state);

        public static bool IsLearner(SuccessionState state, PersonId personId) =>
            (SC020EducationScenario.IsActive(state) && SC020EducationScenario.IsChild(personId)) ||
            (SC023SeclusionScenario.IsActive(state) && SC023SeclusionScenario.IsChild(personId));

        public static bool HasAptitude(SuccessionState state, PersonId personId) =>
            IsLearner(state, personId);

        public static GrowthAttribute AptitudeOf(SuccessionState state, PersonId personId)
        {
            if (SC020EducationScenario.IsActive(state) &&
                SC020EducationScenario.IsChild(personId))
                return SC020EducationScenario.ChildAptitude;
            if (SC023SeclusionScenario.IsActive(state) && SC023SeclusionScenario.IsChild(personId))
                return SC023SeclusionScenario.ChildAptitude;
            return default;
        }

        public static bool IsSupportiveEnvironment(SuccessionState state, PersonId personId)
        {
            if (SC020EducationScenario.IsActive(state) &&
                SC020EducationScenario.IsChild(personId))
                return SC020EducationScenario.SupportiveEnvironment;
            return SC023SeclusionScenario.IsActive(state) &&
                   SC023SeclusionScenario.IsChild(personId) &&
                   SC023SeclusionScenario.SupportiveEnvironment;
        }

        /// <summary>Fixed starting quintet for the scenario child; all other people start at zero.</summary>
        public static CharacterGrowthValues InitialValuesFor(SuccessionState state, PersonId personId)
        {
            if (SC020EducationScenario.IsActive(state) &&
                SC020EducationScenario.IsChild(personId))
                return CharacterGrowthValues.CreateClamped(SC020EducationScenario.ChildNinjutsu,
                    SC020EducationScenario.ChildMartialArts, SC020EducationScenario.ChildScholarship,
                    SC020EducationScenario.ChildManagement, SC020EducationScenario.ChildSocial);
            if (SC023SeclusionScenario.IsActive(state) && SC023SeclusionScenario.IsChild(personId))
                return CharacterGrowthValues.CreateClamped(SC023SeclusionScenario.ChildNinjutsu,
                    SC023SeclusionScenario.ChildMartialArts, SC023SeclusionScenario.ChildScholarship,
                    SC023SeclusionScenario.ChildManagement, SC023SeclusionScenario.ChildSocial);
            return CharacterGrowthValues.CreateClamped(0, 0, 0, 0, 0);
        }

        public static MentorSpecialty SpecialtyOf(SuccessionState state, PersonId personId)
        {
            if (SC020EducationScenario.IsActive(state))
                return SC020EducationScenario.SpecialtyOf(personId);
            if (SC023SeclusionScenario.IsActive(state))
                return SC023SeclusionScenario.SpecialtyOf(personId);
            return MentorSpecialty.None;
        }

        /// <summary>
        /// Human-readable role shown next to a mentor. SC-020 keeps its compact existing
        /// label; SC-023 names the retired former head without exposing raw IDs.
        /// </summary>
        public static string MentorRoleLabel(SuccessionState state, PersonId personId)
        {
            if (!SC023SeclusionScenario.IsActive(state) || state.World == null ||
                !state.World.Family.TryGetRole(personId, out FamilyMemberRole role) ||
                !state.World.TryGetLife(personId, out SuccessionPersonLife life))
                return string.Empty;

            if (life.Retired && role == FamilyMemberRole.DirectKin)
                return "隐居家主／前任家主";
            if (personId == state.HeadPersonId && role == FamilyMemberRole.Head)
                return "现任家主";
            switch (role)
            {
                case FamilyMemberRole.DirectKin: return "直系亲族";
                case FamilyMemberRole.CollateralKin: return "旁系亲族";
                case FamilyMemberRole.Spouse: return "配偶";
                case FamilyMemberRole.AdoptedChild: return "养子/养女";
                case FamilyMemberRole.Retainer: return "家臣";
                case FamilyMemberRole.Disciple: return "弟子";
                case FamilyMemberRole.Guest: return "客将";
                default: return string.Empty;
            }
        }

        /// <summary>Scenario-specific actionable text for an invalid mentor choice.</summary>
        public static string MentorRequirementMessage(SuccessionState state) =>
            SC023SeclusionScenario.IsActive(state)
                ? "导师必须是存活、成年的家族成员；隐居的前任家主可以担任导师，且不能是培养对象本人。"
                : "请选择一位存活且未退休的成年家族导师。";
    }
}
