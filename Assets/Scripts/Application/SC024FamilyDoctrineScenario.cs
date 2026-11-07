using System;
using SengokuChronicle.Domain;

namespace SengokuChronicle.Application
{
    /// <summary>
    /// Fixed, deterministic SC-024 family-creed demonstration. It deliberately reuses
    /// the approved six-person SC-013 roster, birth years, stable person IDs and start
    /// date, then adds one SC-024-only fourth-generation minor heir so the
    /// <c>培养继承人</c> ambition has a real education target. The ordinary SC-013
    /// scenario keeps its six people and every gate unchanged.
    /// </summary>
    public static class SC024FamilyDoctrineScenario
    {
        public const string ScenarioId = "sc024";

        /// <summary>Stable ID for the SC-024-only fourth-generation minor heir.</summary>
        public const string HeirIdValue = "person.sc024.nobumasa-heir";
        public const string HeirName = "明智 景继";
        public const int HeirBirthYear = 1550;
        public const int HeirStartAge = 10;
        public const string HeirGenerationLabel = "第四代";

        public static readonly PersonId HeirId = new PersonId(HeirIdValue);

        /// <summary>
        /// The approved SC-013 roster reused without copying people or kinship, plus the
        /// SC-024-only child linked directly from third-generation Nobumasa. Masatsuna
        /// remains the opening designated heir.
        /// </summary>
        public static SuccessionState CreateSuccession()
        {
            SuccessionState state = M2FixedScenario.CreateSuccession();
            Add(state.World, HeirId, HeirName, HeirBirthYear, FamilyMemberRole.DirectKin);
            Link(state.World, M2FixedScenario.NobumasaId, HeirId, KinshipType.ParentChild);
            return state;
        }

        /// <summary>Same opening date as the fixed M2/SC-013 new game.</summary>
        public static GameDate StartDate => WorldClock.NewGameDate;

        /// <summary>True only for the reused fixed Akechi family identity.</summary>
        public static bool IsActive(SuccessionState state) => M2FixedScenario.IsActive(state);

        /// <summary>True only when the session owns the explicit SC-024 event route.</summary>
        public static bool IsRoute(string eventScenarioId) =>
            string.Equals(eventScenarioId, ScenarioId, StringComparison.Ordinal);

        /// <summary>
        /// True when the reused family contains the SC-024-only child. The child is never
        /// added by <see cref="M2FixedScenario.CreateSuccession"/>, so this stays SC-024-only.
        /// </summary>
        public static bool HasHeir(SuccessionState state) =>
            state != null && state.World != null && state.World.TryGetPerson(HeirId, out _);

        /// <summary>True when the given person is the SC-024-only fourth-generation heir.</summary>
        public static bool IsHeir(PersonId personId) => personId == HeirId;

        /// <summary>Human-readable generation label for the SC-024-only heir.</summary>
        public static bool TryGetGenerationLabel(PersonId personId, out string label)
        {
            if (personId == HeirId)
            {
                label = HeirGenerationLabel;
                return true;
            }

            label = string.Empty;
            return false;
        }

        /// <summary>The child becomes Nobumasa's expected heir only once Nobumasa is head.</summary>
        public static PersonId ExpectedHeir(SuccessionState state) =>
            state != null && state.HeadPersonId == M2FixedScenario.NobumasaId && HasHeir(state)
                ? HeirId
                : default;

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
