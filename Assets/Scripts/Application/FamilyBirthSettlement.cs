using System;
using System.Collections.Generic;
using SengokuChronicle.Domain;

namespace SengokuChronicle.Application
{
    /// <summary>Immutable facts of one due birth that the monthly transaction will apply.</summary>
    public readonly struct FamilyBirthFact
    {
        public FamilyBirthFact(PersonId newbornId, string displayName, PersonGender gender,
            PersonId gestatingParentId, PersonId otherParentId, GameDate birthDate,
            FamilyPregnancyRecord source, IReadOnlyList<PersonId> fullSiblingIds)
        {
            NewbornId = newbornId;
            DisplayName = displayName ?? string.Empty;
            Gender = gender;
            GestatingParentId = gestatingParentId;
            OtherParentId = otherParentId;
            BirthDate = birthDate;
            Source = source;
            FullSiblingIds = fullSiblingIds ?? Array.Empty<PersonId>();
        }

        public PersonId NewbornId { get; }
        public string DisplayName { get; }
        public PersonGender Gender { get; }
        public PersonId GestatingParentId { get; }
        public PersonId OtherParentId { get; }
        public GameDate BirthDate { get; }
        public FamilyPregnancyRecord Source { get; }
        /// <summary>Existing children of both parents; the transaction adds Sibling facts for each.</summary>
        public IReadOnlyList<PersonId> FullSiblingIds { get; }
    }

    /// <summary>One birth chronicle record prepared before the month commits.</summary>
    public readonly struct FamilyBirthChronicleFact
    {
        public FamilyBirthChronicleFact(PersonId newbornId, string title, string summary,
            GameDate date)
        {
            NewbornId = newbornId;
            Title = title ?? string.Empty;
            Summary = summary ?? string.Empty;
            Date = date;
        }

        public PersonId NewbornId { get; }
        public string Title { get; }
        public string Summary { get; }
        public GameDate Date { get; }
    }

    /// <summary>
    /// Immutable monthly birth plan: the pure pregnancy plan, every due newborn fact and
    /// the chronicle facts that may be appended. It owns no live state, so a failed month
    /// can discard the whole plan without leaving partial family mutations.
    /// </summary>
    public sealed class FamilyBirthSettlementPlan
    {
        // Internal so the sibling factory in FamilyBirthSettlement (same assembly) can
        // construct an immutable plan without exposing mutable construction publicly.
        internal FamilyBirthSettlementPlan(FamilyPregnancyMonthPlan pregnancyPlan,
            IReadOnlyList<FamilyBirthFact> births,
            IReadOnlyList<FamilyBirthChronicleFact> chronicles, string error)
        {
            PregnancyPlan = pregnancyPlan;
            Births = births;
            Chronicles = chronicles;
            Error = error ?? string.Empty;
        }

        public FamilyPregnancyMonthPlan PregnancyPlan { get; }
        public IReadOnlyList<FamilyBirthFact> Births { get; }
        public IReadOnlyList<FamilyBirthChronicleFact> Chronicles { get; }
        public string Error { get; }
        public bool Succeeded => string.IsNullOrEmpty(Error);
        public bool HasBirths => Births.Count > 0;
    }

    /// <summary>
    /// Pure monthly birth settlement. It delegates eligibility and progress to
    /// <see cref="FamilyPregnancyRules.PlanMonth"/>, then draws a 50/50 gender and a
    /// stable name for every due birth from the same deterministic WorldSimulation
    /// stream. No state is mutated here.
    /// </summary>
    public static class FamilyBirthSettlement
    {
        public const string ChronicleTitle = "家族新生";
        public const string ChronicleSourceId = FamilyChronicleEntry.BirthSourceId;

        public static FamilyBirthSettlementPlan Plan(SuccessionState succession,
            GameDate currentDate, GameDate nextDate, DeterministicRandomStream random)
        {
            if (succession == null || succession.World == null || random == null)
                return new FamilyBirthSettlementPlan(FamilyPregnancyMonthPlan.Empty,
                    Array.Empty<FamilyBirthFact>(), Array.Empty<FamilyBirthChronicleFact>(),
                    "月结家族状态为空，无法规划出生。");

            FamilyPregnancyMonthPlan pregnancyPlan =
                FamilyPregnancyRules.PlanMonth(succession, currentDate, nextDate, random);

            var births = new List<FamilyBirthFact>(pregnancyPlan.Completions.Count);
            var chronicles = new List<FamilyBirthChronicleFact>(
                pregnancyPlan.Completions.Count);
            var reservedIds = new HashSet<string>(StringComparer.Ordinal);
            var reservedNames = new HashSet<string>(StringComparer.Ordinal);

            for (int i = 0; i < pregnancyPlan.Completions.Count; i++)
            {
                FamilyPregnancyRecord record = pregnancyPlan.Completions[i];
                if (!succession.World.TryGetPerson(record.GestatingPersonId,
                        out PersonState gestating) ||
                    !succession.World.TryGetPerson(record.OtherParentId,
                        out PersonState otherParent))
                {
                    return Failed(pregnancyPlan,
                        "孕期记录引用了未登记人物，无法完成出生。");
                }

                PersonGender gender = random.NextInt(2) == 0
                    ? PersonGender.Male
                    : PersonGender.Female;
                FamilyBirthName name = FamilyBirthNaming.Create(succession,
                    record.GestatingPersonId, record.OtherParentId,
                    record.ConceptionDate, gender, reservedIds, reservedNames);
                if (!name.Succeeded)
                    return Failed(pregnancyPlan, name.Error);

                var siblingIds = new List<PersonId>();
                foreach (PersonState person in succession.World.Persons)
                {
                    if (person.Id == record.GestatingPersonId ||
                        person.Id == record.OtherParentId)
                        continue;
                    if (IsChildOf(succession, record.GestatingPersonId, person.Id) &&
                        IsChildOf(succession, record.OtherParentId, person.Id))
                        siblingIds.Add(person.Id);
                }

                var fact = new FamilyBirthFact(name.PersonId, name.DisplayName, gender,
                    record.GestatingPersonId, record.OtherParentId, nextDate, record,
                    siblingIds.AsReadOnly());
                births.Add(fact);
                chronicles.Add(new FamilyBirthChronicleFact(name.PersonId, ChronicleTitle,
                    BuildSummary(gestating.DisplayName, otherParent.DisplayName,
                        name.DisplayName, gender), nextDate));
            }

            return new FamilyBirthSettlementPlan(pregnancyPlan, births.AsReadOnly(),
                chronicles.AsReadOnly(), string.Empty);
        }

        private static bool IsChildOf(SuccessionState succession, PersonId parentId,
            PersonId childId)
        {
            var link = new KinshipLink(parentId, childId, KinshipType.ParentChild);
            return succession.World.Kinship.Contains(link);
        }

        private static string BuildSummary(string gestatingName, string otherParentName,
            string newbornName, PersonGender gender) =>
            (gestatingName ?? string.Empty) + "与" + (otherParentName ?? string.Empty) + "的" +
            (gender == PersonGender.Male ? "子" : "女") + (newbornName ?? string.Empty) + "出生。";

        private static FamilyBirthSettlementPlan Failed(FamilyPregnancyMonthPlan pregnancyPlan,
            string error) =>
            new FamilyBirthSettlementPlan(pregnancyPlan, Array.Empty<FamilyBirthFact>(),
                Array.Empty<FamilyBirthChronicleFact>(), error);
    }
}
