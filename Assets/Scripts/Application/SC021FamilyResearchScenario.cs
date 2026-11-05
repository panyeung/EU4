using System;
using SengokuChronicle.Domain;

namespace SengokuChronicle.Application
{
    /// <summary>
    /// Fixed, deterministic SC-021 family-research demonstration. It is a separate
    /// family identity and title entry so the accepted SC-010/SC-013/SC-019/SC-020
    /// samples keep their rosters, events and starting snapshots unchanged.
    /// </summary>
    public static class SC021FamilyResearchScenario
    {
        public const string FamilyIdValue = "family.kageyama.sc021";
        public const string FamilyName = "影山家";

        public const string HeadIdValue = "person.sc021.head";
        public const string SpouseIdValue = "person.sc021.spouse";
        public const string HeirIdValue = "person.sc021.heir";

        public const string HeadName = "影山 宗次";
        public const string SpouseName = "影山 奈绪";
        public const string HeirName = "影山 小太郎";

        public const int HeadBirthYear = 1520;
        public const int SpouseBirthYear = 1522;
        public const int HeirBirthYear = 1542;

        public const int OpeningFunds = 2;
        public const int OpeningSupplies = 2;
        public const int OpeningIntelligence = 0;

        public static readonly FamilyId FamilyId = new FamilyId(FamilyIdValue);
        public static readonly PersonId HeadId = new PersonId(HeadIdValue);
        public static readonly PersonId SpouseId = new PersonId(SpouseIdValue);
        public static readonly PersonId HeirId = new PersonId(HeirIdValue);

        /// <summary>The approved SC-021 demo always starts at 1560-01-01.</summary>
        public static readonly GameDate StartDate = new GameDate(1560, 1, 1);

        public static SuccessionState CreateSuccession()
        {
            var world = new SuccessionWorld(FamilyId, FamilyName);
            Add(world, HeadId, HeadName, HeadBirthYear, FamilyMemberRole.Head);
            Add(world, SpouseId, SpouseName, SpouseBirthYear, FamilyMemberRole.Spouse);
            Add(world, HeirId, HeirName, HeirBirthYear, FamilyMemberRole.DirectKin);

            Link(world, HeadId, SpouseId, KinshipType.Spouse);
            Link(world, HeadId, HeirId, KinshipType.ParentChild);
            Link(world, SpouseId, HeirId, KinshipType.ParentChild);

            return new SuccessionState(world)
            {
                HeadPersonId = HeadId,
                DesignatedHeirId = HeirId
            };
        }

        /// <summary>
        /// Exactly the two-month research budget. With no industry appointments the
        /// first month costs 1/1 and the second costs the remaining 1/1.
        /// </summary>
        public static IndustryState CreateIndustryState()
        {
            if (!IndustryResources.TryCreate(OpeningFunds, OpeningSupplies, OpeningIntelligence,
                    out IndustryResources resources, out string error))
                throw new InvalidOperationException(error);
            return IndustryState.CreateInitial(StartDate, resources);
        }

        /// <summary>True only for the fixed SC-021 Kageyama family identity.</summary>
        public static bool IsActive(SuccessionState state) =>
            state != null && state.World != null && state.World.Family.Id == FamilyId;

        public static string PersonName(PersonId personId)
        {
            if (personId == HeadId)
                return HeadName;
            if (personId == SpouseId)
                return SpouseName;
            if (personId == HeirId)
                return HeirName;
            return string.Empty;
        }

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
    }
}
