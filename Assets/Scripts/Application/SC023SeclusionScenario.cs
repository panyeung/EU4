using System;
using SengokuChronicle.Domain;

namespace SengokuChronicle.Application
{
    /// <summary>
    /// Fixed, deterministic SC-023 seclusion demonstration. A three-generation family
    /// starts with the old head still in office and an adult heir designated: the player
    /// performs the retirement handoff, keeps the old head as a retired direct kin, and
    /// then chooses that former head as the grandchild's annual mentor. It is a separate
    /// family identity and title entry, so SC-020 and every earlier scenario keep their
    /// rosters, values and mentor rules unchanged.
    /// </summary>
    public static class SC023SeclusionScenario
    {
        public const string FamilyIdValue = EducationMentorRules.SC023SeclusionFamilyIdValue;
        public const string FamilyName = "橘家";

        public const string ElderIdValue = "person.sc023.elder";
        public const string HeirIdValue = "person.sc023.heir";
        public const string ChildIdValue = "person.sc023.child";

        public const string ElderName = "橘 宗严";
        public const string HeirName = "橘 赖久";
        public const string ChildName = "橘 千代";

        public const int ElderBirthYear = 1498;
        public const int HeirBirthYear = 1528;
        public const int ChildBirthYear = 1550;

        public const int ChildNinjutsu = 10;
        public const int ChildMartialArts = 12;
        public const int ChildScholarship = 18;
        public const int ChildManagement = 11;
        public const int ChildSocial = 13;

        public static readonly FamilyId FamilyId = new FamilyId(FamilyIdValue);
        public static readonly PersonId ElderId = new PersonId(ElderIdValue);
        public static readonly PersonId HeirId = new PersonId(HeirIdValue);
        public static readonly PersonId ChildId = new PersonId(ChildIdValue);

        /// <summary>Recent start so the December-to-January settlement is one month away.</summary>
        public static readonly GameDate StartDate = new GameDate(1560, 12, 1);

        /// <summary>The grandchild is fixed as scholarship-gifted in this demonstration.</summary>
        public static GrowthAttribute ChildAptitude => GrowthAttribute.Scholarship;

        /// <summary>The Tachibana home environment is fixed as supportive.</summary>
        public static bool SupportiveEnvironment => true;

        public static SuccessionState CreateSuccession()
        {
            var world = new SuccessionWorld(FamilyId, FamilyName);
            Add(world, ElderId, ElderName, ElderBirthYear, FamilyMemberRole.Head);
            Add(world, HeirId, HeirName, HeirBirthYear, FamilyMemberRole.DirectKin);
            Add(world, ChildId, ChildName, ChildBirthYear, FamilyMemberRole.DirectKin);

            Link(world, ElderId, HeirId, KinshipType.ParentChild);
            Link(world, HeirId, ChildId, KinshipType.ParentChild);

            return new SuccessionState(world)
            {
                HeadPersonId = ElderId,
                DesignatedHeirId = HeirId
            };
        }

        /// <summary>True only for the fixed SC-023 Tachibana family identity.</summary>
        public static bool IsActive(SuccessionState state) =>
            state != null && state.World != null && state.World.Family.Id == FamilyId;

        /// <summary>Fixed SC-023 growth state: only the grandchild starts with values.</summary>
        public static CharacterDevelopmentState CreateDevelopmentState()
        {
            var development = CharacterDevelopmentState.CreateEmpty();
            AddDevelopment(development, ElderId, 0, 0, 0, 0, 0);
            AddDevelopment(development, HeirId, 0, 0, 0, 0, 0);
            AddDevelopment(development, ChildId, ChildNinjutsu, ChildMartialArts,
                ChildScholarship, ChildManagement, ChildSocial);
            return development;
        }

        /// <summary>Fixed mentor specialty for the retired former head.</summary>
        public static MentorSpecialty SpecialtyOf(PersonId personId) =>
            personId == ElderId ? MentorSpecialty.Scholarship : MentorSpecialty.None;

        public static string MentorName(PersonId personId)
        {
            if (personId == ElderId)
                return ElderName;
            if (personId == HeirId)
                return HeirName;
            if (personId == ChildId)
                return ChildName;
            return string.Empty;
        }

        /// <summary>True when this person is the fixed SC-023 demonstration child.</summary>
        public static bool IsChild(PersonId personId) => personId == ChildId;

        private static void Add(SuccessionWorld world, PersonId id, string name, int birthYear,
            FamilyMemberRole role)
        {
            if (!world.TryAddPerson(new PersonState(id, name,
                    PersonGenderCatalog.GenderFor(id)),
                    new SuccessionPersonLife(birthYear, true, false), out string error))
                throw new InvalidOperationException(error);
            if (!world.TryAddMember(id, role, out error))
                throw new InvalidOperationException(error);
        }

        private static void Link(SuccessionWorld world, PersonId from, PersonId to, KinshipType type)
        {
            if (!KinshipLink.TryCreate(from, to, type, out KinshipLink link, out string error))
                throw new InvalidOperationException(error);
            if (!world.TryAddKinship(link, out error))
                throw new InvalidOperationException(error);
        }

        private static void AddDevelopment(CharacterDevelopmentState development, PersonId id,
            int ninjutsu, int martialArts, int scholarship, int management, int social)
        {
            if (!CharacterGrowthValues.TryCreate(ninjutsu, martialArts, scholarship, management,
                    social, out CharacterGrowthValues values, out string error))
                throw new InvalidOperationException(error);
            if (!development.TryAddPerson(id, values, out error))
                throw new InvalidOperationException(error);
        }
    }
}
