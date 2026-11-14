using System;
using System.Collections.Generic;
using System.Globalization;
using SengokuChronicle.Domain;

namespace SengokuChronicle.Application
{
    /// <summary>One fully-resolved autonomous marriage that the monthly transaction applies.</summary>
    public readonly struct FamilyMarriageFact
    {
        public FamilyMarriageFact(PersonId candidateId, string candidateName,
            PersonId spouseId, string spouseDisplayName, PersonGender spouseGender,
            int spouseBirthYear, int candidateBirthYear, GameDate date)
        {
            CandidateId = candidateId;
            CandidateName = candidateName ?? string.Empty;
            SpouseId = spouseId;
            SpouseDisplayName = spouseDisplayName ?? string.Empty;
            SpouseGender = spouseGender;
            SpouseBirthYear = spouseBirthYear;
            CandidateBirthYear = candidateBirthYear;
            Date = date;
        }

        public PersonId CandidateId { get; }
        public string CandidateName { get; }
        public PersonId SpouseId { get; }
        public string SpouseDisplayName { get; }
        public PersonGender SpouseGender { get; }
        public int SpouseBirthYear { get; }
        public int CandidateBirthYear { get; }
        public GameDate Date { get; }
    }

    /// <summary>One marriage chronicle record prepared before the month commits.</summary>
    public readonly struct FamilyMarriageChronicleFact
    {
        public FamilyMarriageChronicleFact(PersonId candidateId, PersonId spouseId, string title,
            string summary, GameDate date)
        {
            CandidateId = candidateId;
            SpouseId = spouseId;
            Title = title ?? string.Empty;
            Summary = summary ?? string.Empty;
            Date = date;
        }

        public PersonId CandidateId { get; }
        public PersonId SpouseId { get; }
        public string Title { get; }
        public string Summary { get; }
        public GameDate Date { get; }
    }

    /// <summary>
    /// Immutable marriage plan. It owns no live state: the monthly transaction can
    /// discard it, or apply it to a cloned succession snapshot, without leaving a
    /// half-registered spouse behind.
    /// </summary>
    public sealed class FamilyMarriagePlan
    {
        public static readonly FamilyMarriagePlan Empty = new FamilyMarriagePlan(
            Array.Empty<FamilyMarriageFact>(), Array.Empty<FamilyMarriageChronicleFact>(),
            string.Empty);

        internal FamilyMarriagePlan(IReadOnlyList<FamilyMarriageFact> marriages,
            IReadOnlyList<FamilyMarriageChronicleFact> chronicles, string error)
        {
            Marriages = marriages;
            Chronicles = chronicles;
            Error = error ?? string.Empty;
        }

        public IReadOnlyList<FamilyMarriageFact> Marriages { get; }
        public IReadOnlyList<FamilyMarriageChronicleFact> Chronicles { get; }
        public string Error { get; }
        public bool Succeeded => string.IsNullOrEmpty(Error);
        public bool HasChanges => Marriages.Count > 0;
    }

    /// <summary>
    /// M08A autonomous-marriage settlement. Planning turns pure
    /// <see cref="NpcMarriageProposal"/> facts into stable person IDs, original names,
    /// the opposite gender, a deterministic adult birth year, the formal spouse edge
    /// and the chronicle facts. Nothing is mutated during planning. The apply step
    /// reuses the existing person, membership, kinship and growth registration paths;
    /// it never creates an M03 autonomy pilot or a personal relationship value.
    /// </summary>
    public static class FamilyMarriageSettlement
    {
        public const string ChronicleTitle = "自主婚姻";
        public const string ChronicleSourceId = FamilyChronicleEntry.MarriageSourceId;

        /// <summary>
        /// Resolves the ordered proposals into marriage facts. IDs and names are made
        /// collision-free across the whole plan, so two proposals can never generate the
        /// same person. A spouse is always the opposite gender and at least the family's
        /// adulthood age; the preferred age gap is at most five years, clamped at the
        /// adult boundary.
        /// </summary>
        public static FamilyMarriagePlan Plan(SuccessionState state,
            IReadOnlyList<NpcMarriageProposal> proposals, GameDate date)
        {
            if (state == null || state.World == null)
                return new FamilyMarriagePlan(Array.Empty<FamilyMarriageFact>(),
                    Array.Empty<FamilyMarriageChronicleFact>(), "家族状态为空，无法规划婚姻。");
            if (proposals == null || proposals.Count == 0 || !date.IsValid)
                return FamilyMarriagePlan.Empty;

            var reservedIds = new HashSet<string>(StringComparer.Ordinal);
            var reservedNames = new HashSet<string>(StringComparer.Ordinal);
            var marriages = new List<FamilyMarriageFact>(proposals.Count);
            var chronicles = new List<FamilyMarriageChronicleFact>(proposals.Count);
            var plannedCandidates = new HashSet<PersonId>();

            for (int i = 0; i < proposals.Count; i++)
            {
                NpcMarriageProposal proposal = proposals[i];
                if (proposal.CandidateId.IsEmpty || !plannedCandidates.Add(proposal.CandidateId))
                    return Failed("婚姻提案重复或缺少候选人。");
                if (proposal.CandidateGender != PersonGender.Male &&
                    proposal.CandidateGender != PersonGender.Female)
                    return Failed("婚姻提案的候选人性别无效。");
                if (!state.World.TryGetPerson(proposal.CandidateId, out PersonState candidate))
                    return Failed("婚姻候选人不是已登记人物。");

                PersonGender spouseGender = proposal.CandidateGender == PersonGender.Male
                    ? PersonGender.Female
                    : PersonGender.Male;
                FamilyBirthName spouse = FamilyBirthNaming.CreateSpouse(state,
                    proposal.CandidateId, date, spouseGender, reservedIds, reservedNames);
                if (!spouse.Succeeded)
                    return Failed(spouse.Error);

                int spouseAge = SpouseAgeFor(proposal, state.AdulthoodAge);
                int spouseBirthYear = date.Year - spouseAge;
                if (spouseBirthYear <= 0)
                    return Failed("生成配偶的生年无效。");

                var fact = new FamilyMarriageFact(proposal.CandidateId,
                    candidate.DisplayName, spouse.PersonId, spouse.DisplayName, spouseGender,
                    spouseBirthYear, proposal.CandidateBirthYear, date);
                marriages.Add(fact);
                chronicles.Add(new FamilyMarriageChronicleFact(proposal.CandidateId,
                    spouse.PersonId, ChronicleTitle, BuildSummary(candidate.DisplayName,
                        spouse.DisplayName), date));
            }

            return new FamilyMarriagePlan(marriages.AsReadOnly(), chronicles.AsReadOnly(),
                string.Empty);
        }

        /// <summary>
        /// Deterministic spouse age: the candidate's settlement age plus a stable offset
        /// in [-5, +5] derived from the permanent candidate ID and the settlement year.
        /// No random value is consumed, so marriage never changes the mortality stream.
        /// The result never falls below the family's adulthood age.
        /// </summary>
        public static int SpouseAgeFor(NpcMarriageProposal proposal, int adulthoodAge)
        {
            int minimum = Math.Max(1, adulthoodAge);
            int span = (FamilyLifeCycleRules.MaximumSpouseAgeGap * 2) + 1;
            int offset = (int)(StableHash((proposal.CandidateId.Value ?? string.Empty) + "|" +
                                          proposal.CandidateBirthYear.ToString(
                                              CultureInfo.InvariantCulture)) % (uint)span) -
                         FamilyLifeCycleRules.MaximumSpouseAgeGap;
            int age = proposal.CandidateAge + offset;
            return age < minimum ? minimum : age;
        }

        /// <summary>
        /// Applies one marriage plan to a working succession snapshot. Every spouse is
        /// registered as a living <see cref="FamilyMemberRole.Spouse"/> with one
        /// symmetric formal spouse edge and one zero growth row. The caller owns daily
        /// action rows (the month commit already reconciles the roster) and must discard
        /// the snapshot on any failure.
        /// </summary>
        public static bool TryApply(SuccessionState succession, FamilyMarriagePlan plan,
            ref CharacterDevelopmentState development, out string error)
        {
            error = string.Empty;
            if (succession == null || succession.World == null || plan == null)
            {
                error = "婚姻结算状态为空。";
                return false;
            }
            if (!plan.Succeeded)
            {
                error = plan.Error;
                return false;
            }
            if (!plan.HasChanges)
                return true;

            development = development != null
                ? development.Clone()
                : CharacterDevelopmentState.CreateEmpty();

            foreach (FamilyMarriageFact marriage in plan.Marriages)
            {
                if (!succession.World.TryGetPerson(marriage.CandidateId, out _))
                {
                    error = "婚姻候选人不是已登记人物：" + marriage.CandidateId.Value;
                    return false;
                }
                if (succession.World.TryGetPerson(marriage.SpouseId, out _))
                {
                    error = "生成配偶的 ID 已存在：" + marriage.SpouseId.Value;
                    return false;
                }

                var person = new PersonState(marriage.SpouseId, marriage.SpouseDisplayName,
                    marriage.SpouseGender);
                if (!succession.World.TryAddPerson(person,
                        new SuccessionPersonLife(marriage.SpouseBirthYear, true, false),
                        out error))
                    return false;
                if (!succession.World.TryAddMember(marriage.SpouseId, FamilyMemberRole.Spouse,
                        out error))
                    return false;
                if (!KinshipLink.TryCreate(marriage.CandidateId, marriage.SpouseId,
                        KinshipType.Spouse, out KinshipLink link, out error))
                    return false;
                if (succession.World.Kinship.Contains(link))
                {
                    error = "婚姻关系已存在。";
                    return false;
                }
                if (!succession.World.TryAddKinship(link, out error))
                    return false;
                if (!development.TryAddPerson(marriage.SpouseId,
                        CharacterGrowthValues.CreateClamped(0, 0, 0, 0, 0), out error))
                    return false;
            }

            return true;
        }

        private static string BuildSummary(string candidateName, string spouseName) =>
            "「" + (candidateName ?? string.Empty) + "」与「" + (spouseName ?? string.Empty) +
            "」结为配偶；「" + (spouseName ?? string.Empty) + "」以配偶身份加入家族。";

        private static FamilyMarriagePlan Failed(string error) =>
            new FamilyMarriagePlan(Array.Empty<FamilyMarriageFact>(),
                Array.Empty<FamilyMarriageChronicleFact>(), error);

        private static uint StableHash(string value)
        {
            // FNV-1a: independent of the save seed so a reloaded or replayed settlement
            // produces the same spouse age and name.
            uint hash = 2166136261u;
            if (value != null)
            {
                foreach (char character in value)
                {
                    hash ^= character;
                    hash *= 16777619u;
                }
            }
            return hash;
        }
    }
}
