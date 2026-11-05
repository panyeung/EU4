using System;
using System.Collections.Generic;
using SengokuChronicle.Domain;

namespace SengokuChronicle.Application
{
    /// <summary>
    /// Fixed, deterministic SC-020 child-education demonstration. It is deliberately a
    /// separate family identity and entry point: the accepted SC-013/SC-019 samples keep
    /// their rosters, events and industries unchanged.
    /// </summary>
    public static class SC020EducationScenario
    {
        public const string FamilyIdValue = "family.aoba.sc020";
        public const string FamilyName = "青叶家";

        public const string HeadIdValue = "person.sc020.head";
        public const string SpouseIdValue = "person.sc020.spouse";
        public const string ChildIdValue = "person.sc020.child";

        public const string HeadName = "青叶 宗一郎";
        public const string SpouseName = "青叶 小夜";
        public const string ChildName = "青叶 千景";

        public const int HeadBirthYear = 1520;
        public const int SpouseBirthYear = 1522;
        public const int ChildBirthYear = 1545;

        public const int ChildNinjutsu = 15;
        public const int ChildMartialArts = 12;
        public const int ChildScholarship = 20;
        public const int ChildManagement = 10;
        public const int ChildSocial = 13;

        public static readonly FamilyId FamilyId = new FamilyId(FamilyIdValue);
        public static readonly PersonId HeadId = new PersonId(HeadIdValue);
        public static readonly PersonId SpouseId = new PersonId(SpouseIdValue);
        public static readonly PersonId ChildId = new PersonId(ChildIdValue);

        /// <summary>The approved SC-020 demo always starts at 1560-01-01.</summary>
        public static readonly GameDate StartDate = new GameDate(1560, 1, 1);

        /// <summary>The child is fixed as scholarship-gifted in this demonstration.</summary>
        public static GrowthAttribute ChildAptitude => GrowthAttribute.Scholarship;

        /// <summary>The Aoba home environment is fixed as supportive in this demonstration.</summary>
        public static bool SupportiveEnvironment => true;

        public const int MinimumLearnerAge = 0;
        public const int MaximumLearnerAge = 15;

        public static SuccessionState CreateSuccession()
        {
            var world = new SuccessionWorld(FamilyId, FamilyName);
            Add(world, HeadId, HeadName, HeadBirthYear, FamilyMemberRole.Head);
            Add(world, SpouseId, SpouseName, SpouseBirthYear, FamilyMemberRole.Spouse);
            Add(world, ChildId, ChildName, ChildBirthYear, FamilyMemberRole.DirectKin);

            Link(world, HeadId, SpouseId, KinshipType.Spouse);
            Link(world, HeadId, ChildId, KinshipType.ParentChild);
            Link(world, SpouseId, ChildId, KinshipType.ParentChild);

            return new SuccessionState(world)
            {
                HeadPersonId = HeadId
            };
        }

        /// <summary>True only for the fixed SC-020 Aoba family identity.</summary>
        public static bool IsActive(SuccessionState state) =>
            state != null && state.World != null && state.World.Family.Id == FamilyId;

        /// <summary>Fixed SC-020 growth state: the child starts at the approved values.</summary>
        public static CharacterDevelopmentState CreateDevelopmentState()
        {
            var development = CharacterDevelopmentState.CreateEmpty();
            AddDevelopment(development, HeadId, 0, 0, 0, 0, 0);
            AddDevelopment(development, SpouseId, 0, 0, 0, 0, 0);
            AddDevelopment(development, ChildId, ChildNinjutsu, ChildMartialArts,
                ChildScholarship, ChildManagement, ChildSocial);
            return development;
        }

        /// <summary>Fixed mentor specialty for the two approved SC-020 adults.</summary>
        public static MentorSpecialty SpecialtyOf(PersonId personId)
        {
            if (personId == HeadId)
                return MentorSpecialty.MartialArts;
            if (personId == SpouseId)
                return MentorSpecialty.Scholarship;
            return MentorSpecialty.None;
        }

        public static string MentorName(PersonId personId)
        {
            if (personId == HeadId)
                return HeadName;
            if (personId == SpouseId)
                return SpouseName;
            if (personId == ChildId)
                return ChildName;
            return string.Empty;
        }

        /// <summary>True when this person is the fixed SC-020 demonstration child.</summary>
        public static bool IsChild(PersonId personId) => personId == ChildId;

        private static void Add(SuccessionWorld world, PersonId id, string name, int birthYear,
            FamilyMemberRole role)
        {
            if (!world.TryAddPerson(new PersonState(id, name),
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
