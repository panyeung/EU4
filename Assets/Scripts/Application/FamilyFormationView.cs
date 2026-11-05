using System;
using System.Collections.Generic;
using SengokuChronicle.Domain;

namespace SengokuChronicle.Application
{
    /// <summary>Explicit result code for one family-formation command; never a silent failure.</summary>
    public enum FamilyFormationOutcome
    {
        Succeeded = 0,
        InvalidState = 1,
        Blocked = 2,
        InvalidInitiator = 3,
        InvalidCandidate = 4,
        SelfRelation = 5,
        AlreadyMember = 6,
        CandidateNotEligible = 7,
        DuplicateRelation = 8,
        DirectLineRelation = 9,
        SpouseRelation = 10,
        AdoptionCycle = 11,
        ChronicleFailed = 12
    }

    public readonly struct FamilyFormationCommandResult
    {
        public FamilyFormationCommandResult(bool succeeded, FamilyFormationOutcome outcome,
            string message, PersonId initiatorId, PersonId candidateId)
        {
            Succeeded = succeeded;
            Outcome = outcome;
            Message = message ?? string.Empty;
            InitiatorId = initiatorId;
            CandidateId = candidateId;
        }

        public bool Succeeded { get; }
        public FamilyFormationOutcome Outcome { get; }
        public string Message { get; }
        public PersonId InitiatorId { get; }
        public PersonId CandidateId { get; }

        public static FamilyFormationCommandResult Failure(FamilyFormationOutcome outcome,
            string message, PersonId initiatorId, PersonId candidateId) =>
            new FamilyFormationCommandResult(false, outcome, message, initiatorId, candidateId);
    }

    /// <summary>One living adult family member who may start a marriage or adoption.</summary>
    public readonly struct FamilyFormationMemberOption
    {
        public FamilyFormationMemberOption(PersonId personId, string displayName, int age,
            string spouseLine)
        {
            PersonId = personId;
            DisplayName = displayName ?? string.Empty;
            Age = age;
            SpouseLine = spouseLine ?? string.Empty;
        }

        public PersonId PersonId { get; }
        public string DisplayName { get; }
        public int Age { get; }
        /// <summary>Readable list of existing formal spouses, or an empty string.</summary>
        public string SpouseLine { get; }

        public string Label => DisplayName + "（" + Age + "岁）";
    }

    /// <summary>One registered, non-member scenario candidate.</summary>
    public readonly struct FamilyFormationCandidateOption
    {
        public FamilyFormationCandidateOption(PersonId personId, string displayName, int age)
        {
            PersonId = personId;
            DisplayName = displayName ?? string.Empty;
            Age = age;
        }

        public PersonId PersonId { get; }
        public string DisplayName { get; }
        public int Age { get; }

        public string Label => DisplayName + "（" + Age + "岁）";
    }

    /// <summary>
    /// Read-only SC-022 projection for the family-formation panel. All eligibility
    /// decisions live in the Domain rules and the session command; this type only
    /// filters the fixed scenario candidate list for display.
    /// </summary>
    public sealed class FamilyFormationView
    {
        private SuccessionState _succession;

        public bool CanEdit { get; private set; }
        public string BlockReason { get; private set; } = string.Empty;
        public string LastMessage { get; private set; } = string.Empty;
        public bool IsScenarioActive { get; private set; }
        public IReadOnlyList<FamilyFormationMemberOption> Members { get; private set; } =
            Array.Empty<FamilyFormationMemberOption>();
        public IReadOnlyList<FamilyFormationCandidateOption> MarriageCandidates { get; private set; } =
            Array.Empty<FamilyFormationCandidateOption>();
        public IReadOnlyList<FamilyFormationCandidateOption> AdoptionCandidates { get; private set; } =
            Array.Empty<FamilyFormationCandidateOption>();

        public static FamilyFormationView Build(SuccessionState succession, GameDate date,
            bool canEdit, string blockReason, string lastMessage)
        {
            var view = new FamilyFormationView
            {
                _succession = succession,
                CanEdit = canEdit,
                BlockReason = blockReason ?? string.Empty,
                LastMessage = lastMessage ?? string.Empty,
                IsScenarioActive = SC022FamilyFormationScenario.IsActive(succession)
            };
            if (succession == null || succession.World == null)
                return view;

            view.Members = BuildMembers(succession, date);
            view.MarriageCandidates = BuildCandidates(succession, date,
                SC022FamilyFormationScenario.MarriageCandidates, adultOnly: true);
            view.AdoptionCandidates = BuildCandidates(succession, date,
                SC022FamilyFormationScenario.AdoptionCandidates, adultOnly: false);
            return view;
        }

        /// <summary>
        /// Eligible adult marriage candidates after removing the selected member's
        /// direct blood/adoptive line. Siblings remain eligible by the approved rule.
        /// </summary>
        public IReadOnlyList<FamilyFormationCandidateOption> MarriageCandidatesFor(
            PersonId memberId)
        {
            if (memberId.IsEmpty || _succession == null || _succession.World == null ||
                !_succession.World.Family.IsMember(memberId))
                return Array.Empty<FamilyFormationCandidateOption>();

            var filtered = new List<FamilyFormationCandidateOption>();
            foreach (FamilyFormationCandidateOption option in MarriageCandidates)
            {
                if (HasSpouseLink(memberId, option.PersonId))
                    continue;
                if (FamilyFormationRules.IsDirectLineRelation(_succession.World.Kinship, memberId,
                        option.PersonId))
                    continue;
                filtered.Add(option);
            }
            return filtered.AsReadOnly();
        }

        /// <summary>
        /// Eligible adoption candidates after removing the adopter themself, a formal
        /// spouse, an existing same-direction adoption edge, and any candidate that
        /// would close a directed parent/child cycle.
        /// </summary>
        public IReadOnlyList<FamilyFormationCandidateOption> AdoptionCandidatesFor(
            PersonId adopterId)
        {
            if (adopterId.IsEmpty || _succession == null || _succession.World == null ||
                !_succession.World.Family.IsMember(adopterId))
                return Array.Empty<FamilyFormationCandidateOption>();

            var filtered = new List<FamilyFormationCandidateOption>();
            foreach (FamilyFormationCandidateOption option in AdoptionCandidates)
            {
                if (option.PersonId == adopterId)
                    continue;
                if (HasSpouseLink(adopterId, option.PersonId))
                    continue;
                if (HasAdoptionLink(adopterId, option.PersonId))
                    continue;
                if (FamilyFormationRules.WouldCreateAdoptionCycle(_succession.World.Kinship,
                        adopterId, option.PersonId))
                    continue;
                filtered.Add(option);
            }
            return filtered.AsReadOnly();
        }

        public string RelationNote(PersonId memberId, PersonId candidateId)
        {
            if (_succession == null || _succession.World == null)
                return string.Empty;
            if (memberId == candidateId)
                return "本人";
            if (HasSpouseLink(memberId, candidateId))
                return "正式配偶";
            if (FamilyFormationRules.IsDirectLineRelation(_succession.World.Kinship, memberId,
                    candidateId))
                return "直系血亲/养亲";
            if (HasSiblingLink(memberId, candidateId))
                return "兄弟姐妹";
            if (HasAdoptionLink(memberId, candidateId) ||
                HasAdoptionLink(candidateId, memberId))
                return "收养关系";
            return "无直系关系";
        }

        public string SpouseNamesFor(PersonId personId)
        {
            if (_succession == null || _succession.World == null || personId.IsEmpty)
                return string.Empty;
            var names = new List<string>();
            foreach (KinshipLink link in _succession.World.Kinship.LinksFor(personId))
            {
                if (link.Type != KinshipType.Spouse)
                    continue;
                PersonId other = link.From == personId ? link.To : link.From;
                string name = PersonDisplayName(other);
                if (!string.IsNullOrEmpty(name))
                    names.Add(name);
            }
            return string.Join("、", names);
        }

        public string MemberDisplayName(PersonId personId) => PersonDisplayName(personId);

        public string CandidateDisplayName(PersonId personId)
        {
            if (_succession != null && _succession.World != null &&
                _succession.World.TryGetPerson(personId, out PersonState person) &&
                !string.IsNullOrWhiteSpace(person.DisplayName))
                return person.DisplayName;
            return SC022FamilyFormationScenario.PersonName(personId);
        }

        private static IReadOnlyList<FamilyFormationMemberOption> BuildMembers(
            SuccessionState succession, GameDate date)
        {
            var members = new List<FamilyFormationMemberOption>();
            foreach (FamilyMembership membership in succession.World.Family.Members)
            {
                if (!succession.World.TryGetPerson(membership.PersonId, out PersonState person))
                    continue;
                if (!succession.World.TryGetLife(membership.PersonId,
                        out SuccessionPersonLife life) || !life.Alive)
                    continue;
                int age = life.AgeAt(date);
                if (age < succession.AdulthoodAge)
                    continue;
                var spouseNames = new List<string>();
                foreach (KinshipLink link in succession.World.Kinship.LinksFor(membership.PersonId))
                {
                    if (link.Type != KinshipType.Spouse)
                        continue;
                    PersonId other = link.From == membership.PersonId ? link.To : link.From;
                    if (succession.World.TryGetPerson(other, out PersonState spouse) &&
                        !string.IsNullOrWhiteSpace(spouse.DisplayName))
                        spouseNames.Add(spouse.DisplayName);
                }
                string spouseLine = spouseNames.Count == 0
                    ? "现有配偶：无"
                    : "现有配偶：" + string.Join("、", spouseNames);
                members.Add(new FamilyFormationMemberOption(membership.PersonId,
                    person.DisplayName, age, spouseLine));
            }
            return members.AsReadOnly();
        }

        private static IReadOnlyList<FamilyFormationCandidateOption> BuildCandidates(
            SuccessionState succession, GameDate date, IReadOnlyList<PersonId> scenarioCandidates,
            bool adultOnly)
        {
            var candidates = new List<FamilyFormationCandidateOption>();
            foreach (PersonId personId in scenarioCandidates)
            {
                if (succession.World.Family.IsMember(personId))
                    continue;
                if (!succession.World.TryGetPerson(personId, out PersonState person))
                    continue;
                if (!succession.World.TryGetLife(personId, out SuccessionPersonLife life) ||
                    !life.Alive)
                    continue;
                int age = life.AgeAt(date);
                if (adultOnly && age < succession.AdulthoodAge)
                    continue;
                candidates.Add(new FamilyFormationCandidateOption(personId,
                    string.IsNullOrWhiteSpace(person.DisplayName)
                        ? SC022FamilyFormationScenario.PersonName(personId)
                        : person.DisplayName,
                    age));
            }
            return candidates.AsReadOnly();
        }

        private bool HasSpouseLink(PersonId left, PersonId right) =>
            _succession != null && _succession.World != null &&
            _succession.World.Kinship.Contains(new KinshipLink(left, right, KinshipType.Spouse));

        private bool HasSiblingLink(PersonId left, PersonId right) =>
            _succession != null && _succession.World != null &&
            _succession.World.Kinship.Contains(new KinshipLink(left, right, KinshipType.Sibling));

        private bool HasAdoptionLink(PersonId from, PersonId to) =>
            _succession != null && _succession.World != null &&
            _succession.World.Kinship.Contains(new KinshipLink(from, to, KinshipType.Adoption));

        private string PersonDisplayName(PersonId personId)
        {
            if (_succession != null && _succession.World != null &&
                _succession.World.TryGetPerson(personId, out PersonState person) &&
                !string.IsNullOrWhiteSpace(person.DisplayName))
                return person.DisplayName;
            return SC022FamilyFormationScenario.PersonName(personId);
        }
    }
}
