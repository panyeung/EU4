using System;
using System.Collections.Generic;
using SengokuChronicle.Domain;

namespace SengokuChronicle.Application
{
    /// <summary>
    /// Fixed, deterministic SC-022 marriage and ordinary-adoption demonstration.
    /// It is a separate family identity and title entry so the accepted SC-010,
    /// SC-013, SC-019, SC-020 and SC-021 samples keep their rosters, events and
    /// starting snapshots unchanged. Candidates are registered persons but not
    /// family members until the player commits a relationship.
    /// </summary>
    public static class SC022FamilyFormationScenario
    {
        public const string FamilyIdValue = "family.asaba.sc022";
        public const string FamilyName = "浅羽家";

        public const string HeadIdValue = "person.sc022.head";
        public const string SpouseIdValue = "person.sc022.spouse";
        public const string ChildIdValue = "person.sc022.child";
        public const string CollateralIdValue = "person.sc022.collateral";

        public const string MarriageCandidateOneIdValue = "person.sc022.candidate.saho";
        public const string MarriageCandidateTwoIdValue = "person.sc022.candidate.chihiro";
        public const string SiblingCandidateIdValue = "person.sc022.candidate.ume";
        public const string BloodAncestorIdValue = "person.sc022.candidate.sogen";
        public const string AdoptedChildCandidateIdValue = "person.sc022.candidate.yoichi";
        public const string ChildAdoptionCandidateIdValue = "person.sc022.candidate.senmatsu";
        public const string AdultAdoptionCandidateIdValue = "person.sc022.candidate.sakichi";

        public const string HeadName = "浅羽 政宗";
        public const string SpouseName = "浅羽 千代";
        public const string ChildName = "浅羽 小夜";
        public const string CollateralName = "浅羽 弦次郎";
        public const string MarriageCandidateOneName = "水无月 佐保";
        public const string MarriageCandidateTwoName = "白河 千寻";
        public const string SiblingCandidateName = "浅羽 梅";
        public const string BloodAncestorName = "浅羽 宗圆";
        public const string AdoptedChildCandidateName = "浅羽 与一";
        public const string ChildAdoptionCandidateName = "小野寺 千松";
        public const string AdultAdoptionCandidateName = "小野寺 佐吉";

        public const int HeadBirthYear = 1524;
        public const int SpouseBirthYear = 1528;
        public const int ChildBirthYear = 1550;
        public const int CollateralBirthYear = 1536;
        public const int MarriageCandidateOneBirthYear = 1538;
        public const int MarriageCandidateTwoBirthYear = 1540;
        public const int SiblingCandidateBirthYear = 1530;
        public const int BloodAncestorBirthYear = 1498;
        public const int AdoptedChildCandidateBirthYear = 1540;
        public const int ChildAdoptionCandidateBirthYear = 1552;
        public const int AdultAdoptionCandidateBirthYear = 1544;

        public static readonly FamilyId FamilyId = new FamilyId(FamilyIdValue);
        public static readonly PersonId HeadId = new PersonId(HeadIdValue);
        public static readonly PersonId SpouseId = new PersonId(SpouseIdValue);
        public static readonly PersonId ChildId = new PersonId(ChildIdValue);
        public static readonly PersonId CollateralId = new PersonId(CollateralIdValue);
        public static readonly PersonId MarriageCandidateOneId =
            new PersonId(MarriageCandidateOneIdValue);
        public static readonly PersonId MarriageCandidateTwoId =
            new PersonId(MarriageCandidateTwoIdValue);
        public static readonly PersonId SiblingCandidateId = new PersonId(SiblingCandidateIdValue);
        public static readonly PersonId BloodAncestorId = new PersonId(BloodAncestorIdValue);
        public static readonly PersonId AdoptedChildCandidateId =
            new PersonId(AdoptedChildCandidateIdValue);
        public static readonly PersonId ChildAdoptionCandidateId =
            new PersonId(ChildAdoptionCandidateIdValue);
        public static readonly PersonId AdultAdoptionCandidateId =
            new PersonId(AdultAdoptionCandidateIdValue);

        /// <summary>The approved SC-022 demo always starts at 1560-05-01.</summary>
        public static readonly GameDate StartDate = new GameDate(1560, 5, 1);

        private static readonly PersonId[] AllCandidateIds =
        {
            MarriageCandidateOneId, MarriageCandidateTwoId, SiblingCandidateId,
            BloodAncestorId, AdoptedChildCandidateId, ChildAdoptionCandidateId,
            AdultAdoptionCandidateId
        };

        private static readonly PersonId[] MarriageCandidateIds =
        {
            MarriageCandidateOneId, MarriageCandidateTwoId, SiblingCandidateId,
            BloodAncestorId, AdoptedChildCandidateId
        };

        private static readonly PersonId[] AdoptionCandidateIds =
        {
            ChildAdoptionCandidateId, AdultAdoptionCandidateId, MarriageCandidateOneId,
            MarriageCandidateTwoId, SiblingCandidateId, BloodAncestorId,
            AdoptedChildCandidateId
        };

        public static IReadOnlyList<PersonId> AllScenarioCandidates =>
            Array.AsReadOnly(AllCandidateIds);

        public static IReadOnlyList<PersonId> MarriageCandidates =>
            Array.AsReadOnly(MarriageCandidateIds);

        public static IReadOnlyList<PersonId> AdoptionCandidates =>
            Array.AsReadOnly(AdoptionCandidateIds);

        public static SuccessionState CreateSuccession()
        {
            var world = new SuccessionWorld(FamilyId, FamilyName);
            AddMember(world, HeadId, HeadName, HeadBirthYear, FamilyMemberRole.Head);
            AddMember(world, SpouseId, SpouseName, SpouseBirthYear, FamilyMemberRole.Spouse);
            AddMember(world, ChildId, ChildName, ChildBirthYear, FamilyMemberRole.DirectKin);
            AddMember(world, CollateralId, CollateralName, CollateralBirthYear,
                FamilyMemberRole.CollateralKin);

            AddCandidate(world, MarriageCandidateOneId, MarriageCandidateOneName,
                MarriageCandidateOneBirthYear);
            AddCandidate(world, MarriageCandidateTwoId, MarriageCandidateTwoName,
                MarriageCandidateTwoBirthYear);
            AddCandidate(world, SiblingCandidateId, SiblingCandidateName,
                SiblingCandidateBirthYear);
            AddCandidate(world, BloodAncestorId, BloodAncestorName, BloodAncestorBirthYear);
            AddCandidate(world, AdoptedChildCandidateId, AdoptedChildCandidateName,
                AdoptedChildCandidateBirthYear);
            AddCandidate(world, ChildAdoptionCandidateId, ChildAdoptionCandidateName,
                ChildAdoptionCandidateBirthYear);
            AddCandidate(world, AdultAdoptionCandidateId, AdultAdoptionCandidateName,
                AdultAdoptionCandidateBirthYear);

            Link(world, HeadId, SpouseId, KinshipType.Spouse);
            Link(world, HeadId, ChildId, KinshipType.ParentChild);
            Link(world, SpouseId, ChildId, KinshipType.ParentChild);
            Link(world, HeadId, CollateralId, KinshipType.Sibling);
            // Existing non-member facts prove the direct-line and cycle rejections
            // without relying on the command to create them first.
            Link(world, BloodAncestorId, HeadId, KinshipType.ParentChild);
            Link(world, HeadId, SiblingCandidateId, KinshipType.Sibling);
            Link(world, HeadId, AdoptedChildCandidateId, KinshipType.Adoption);

            return new SuccessionState(world)
            {
                HeadPersonId = HeadId,
                DesignatedHeirId = ChildId
            };
        }

        /// <summary>True only for the fixed SC-022 Asaba family identity.</summary>
        public static bool IsActive(SuccessionState state) =>
            state != null && state.World != null && state.World.Family.Id == FamilyId;

        public static bool IsScenarioCandidate(PersonId personId)
        {
            for (int i = 0; i < AllCandidateIds.Length; i++)
                if (AllCandidateIds[i] == personId)
                    return true;
            return false;
        }

        public static string PersonName(PersonId personId)
        {
            if (personId == HeadId) return HeadName;
            if (personId == SpouseId) return SpouseName;
            if (personId == ChildId) return ChildName;
            if (personId == CollateralId) return CollateralName;
            if (personId == MarriageCandidateOneId) return MarriageCandidateOneName;
            if (personId == MarriageCandidateTwoId) return MarriageCandidateTwoName;
            if (personId == SiblingCandidateId) return SiblingCandidateName;
            if (personId == BloodAncestorId) return BloodAncestorName;
            if (personId == AdoptedChildCandidateId) return AdoptedChildCandidateName;
            if (personId == ChildAdoptionCandidateId) return ChildAdoptionCandidateName;
            if (personId == AdultAdoptionCandidateId) return AdultAdoptionCandidateName;
            return string.Empty;
        }

        private static void AddMember(SuccessionWorld world, PersonId id, string name, int birthYear,
            FamilyMemberRole role)
        {
            if (!world.TryAddPerson(new PersonState(id, name),
                    new SuccessionPersonLife(birthYear, true, false), out string error))
                throw new InvalidOperationException(error);
            if (!world.TryAddMember(id, role, out error))
                throw new InvalidOperationException(error);
        }

        private static void AddCandidate(SuccessionWorld world, PersonId id, string name,
            int birthYear)
        {
            if (!world.TryAddPerson(new PersonState(id, name),
                    new SuccessionPersonLife(birthYear, true, false), out string error))
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
