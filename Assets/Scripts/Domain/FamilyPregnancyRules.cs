using System;
using System.Collections.Generic;

namespace SengokuChronicle.Domain
{
    /// <summary>
    /// One eligible formal spouse pair in stable order. The gestating person is always
    /// the female spouse and the other parent the male spouse; a pair with two people of
    /// the same or unspecified gender is never produced.
    /// </summary>
    public readonly struct FamilyPregnancyPair
    {
        public FamilyPregnancyPair(PersonId femaleId, PersonId maleId)
        {
            FemaleId = femaleId;
            MaleId = maleId;
        }

        public PersonId FemaleId { get; }
        public PersonId MaleId { get; }
    }

    /// <summary>Pure result of one month's pregnancy planning; planning never mutates state.</summary>
    public sealed class FamilyPregnancyMonthPlan
    {
        public static readonly FamilyPregnancyMonthPlan Empty = new FamilyPregnancyMonthPlan(
            Array.Empty<FamilyPregnancyRecord>(), Array.Empty<FamilyPregnancyRecord>(),
            Array.Empty<FamilyPregnancyRecord>());

        private FamilyPregnancyMonthPlan(
            IReadOnlyList<FamilyPregnancyRecord> starts,
            IReadOnlyList<FamilyPregnancyRecord> completions,
            IReadOnlyList<FamilyPregnancyRecord> cancellations)
        {
            Starts = starts;
            Completions = completions;
            Cancellations = cancellations;
        }

        /// <summary>New pregnancies in stable spouse-pair order.</summary>
        public IReadOnlyList<FamilyPregnancyRecord> Starts { get; }
        /// <summary>Pregnancies that reached their ninth step and produce a birth this month.</summary>
        public IReadOnlyList<FamilyPregnancyRecord> Completions { get; }
        /// <summary>Pregnancies cleared without a birth because the gestating parent died or left.</summary>
        public IReadOnlyList<FamilyPregnancyRecord> Cancellations { get; }

        public bool HasChanges =>
            Starts.Count > 0 || Completions.Count > 0 || Cancellations.Count > 0;

        internal static FamilyPregnancyMonthPlan Create(
            List<FamilyPregnancyRecord> starts, List<FamilyPregnancyRecord> completions,
            List<FamilyPregnancyRecord> cancellations) =>
            new FamilyPregnancyMonthPlan(starts.AsReadOnly(), completions.AsReadOnly(),
                cancellations.AsReadOnly());
    }

    /// <summary>
    /// Deterministic M04B pregnancy rules. Only formal spouse links from the existing
    /// kinship graph participate; the female must be 16–40 and the male 16–60 inclusive,
    /// both alive and current family members. Each eligible pair gets one independent
    /// 3% roll per month, consumed from the persisted WorldSimulation stream in stable
    /// pair order. Pairs that are already pregnant (including a pregnancy completing in
    /// the same settlement) consume no random values and cannot reconceive immediately.
    /// </summary>
    public static class FamilyPregnancyRules
    {
        public const int ConceptionPercent = 3;
        public const int MinimumFemaleAge = 16;
        public const int MaximumFemaleAge = 40;
        public const int MinimumMaleAge = 16;
        public const int MaximumMaleAge = 60;

        /// <summary>
        /// Plans one month transition. Existing pregnancies are cancelled or completed by
        /// the new month first; conception is then rolled only for eligible pairs that had
        /// no active pregnancy when the settlement began.
        /// </summary>
        public static FamilyPregnancyMonthPlan PlanMonth(SuccessionState state,
            GameDate currentDate, GameDate nextDate, DeterministicRandomStream random)
        {
            if (state == null || state.World == null || random == null ||
                !currentDate.IsValid || !nextDate.IsValid || nextDate <= currentDate)
                return FamilyPregnancyMonthPlan.Empty;

            var completions = new List<FamilyPregnancyRecord>();
            var cancellations = new List<FamilyPregnancyRecord>();
            var occupiedGestating = new HashSet<PersonId>();
            var occupiedPairs = new HashSet<string>(StringComparer.Ordinal);

            IReadOnlyList<FamilyPregnancyRecord> active = state.World.Pregnancies.Records;
            for (int i = 0; i < active.Count; i++)
            {
                FamilyPregnancyRecord record = active[i];
                occupiedGestating.Add(record.GestatingPersonId);
                occupiedPairs.Add(PairKey(record.GestatingPersonId, record.OtherParentId));

                if (!state.World.Family.IsMember(record.GestatingPersonId) ||
                    !state.World.IsLiving(record.GestatingPersonId))
                {
                    cancellations.Add(record);
                }
                else if (record.IsDueAt(nextDate))
                {
                    completions.Add(record);
                }
            }

            var starts = new List<FamilyPregnancyRecord>();
            IReadOnlyList<FamilyPregnancyPair> pairs = GetEligiblePairs(state, nextDate);
            for (int i = 0; i < pairs.Count; i++)
            {
                FamilyPregnancyPair pair = pairs[i];
                if (occupiedGestating.Contains(pair.FemaleId) ||
                    occupiedPairs.Contains(PairKey(pair.FemaleId, pair.MaleId)))
                    continue;

                if (!random.TryNextInt(100, out int roll) || roll >= ConceptionPercent)
                    continue;

                if (!FamilyPregnancyRecord.TryCreate(pair.FemaleId, pair.MaleId, nextDate,
                        FamilyPregnancyRecord.AddMonths(nextDate,
                            FamilyPregnancyRecord.GestationMonths),
                        out FamilyPregnancyRecord record, out _))
                    continue;

                starts.Add(record);
                occupiedGestating.Add(pair.FemaleId);
                occupiedPairs.Add(PairKey(pair.FemaleId, pair.MaleId));
            }

            return FamilyPregnancyMonthPlan.Create(starts, completions, cancellations);
        }

        /// <summary>
        /// Stable eligible pairs for one date. Duplicate kinship facts are impossible in
        /// <see cref="KinshipGraph"/>; the extra set keeps this method total even if the
        /// graph is restored from an older malformed snapshot.
        /// </summary>
        public static IReadOnlyList<FamilyPregnancyPair> GetEligiblePairs(SuccessionState state,
            GameDate date)
        {
            var pairs = new List<FamilyPregnancyPair>();
            if (state == null || state.World == null || !date.IsValid)
                return pairs.AsReadOnly();

            var seen = new HashSet<string>(StringComparer.Ordinal);
            IReadOnlyList<KinshipLink> links = state.World.Kinship.Links;
            for (int i = 0; i < links.Count; i++)
            {
                KinshipLink link = links[i];
                if (link.Type != KinshipType.Spouse)
                    continue;

                if (!TryResolvePair(state, link.From, link.To, date,
                        out FamilyPregnancyPair pair))
                    continue;

                string key = PairKey(pair.FemaleId, pair.MaleId);
                if (seen.Add(key))
                    pairs.Add(pair);
            }

            pairs.Sort((a, b) =>
            {
                int byFemale = string.CompareOrdinal(a.FemaleId.Value ?? string.Empty,
                    b.FemaleId.Value ?? string.Empty);
                if (byFemale != 0)
                    return byFemale;
                return string.CompareOrdinal(a.MaleId.Value ?? string.Empty,
                    b.MaleId.Value ?? string.Empty);
            });
            return pairs.AsReadOnly();
        }

        /// <summary>Human-readable roster status: <c>怀孕 · 第 n/9 月 · 预计 YYYY-MM</c>.</summary>
        public static string StatusText(FamilyPregnancyRecord record, GameDate date) =>
            "怀孕 · 第 " + record.ProgressMonth(date) + "/9 月 · 预计 " +
            record.DueDate.Year.ToString("D4") + "-" +
            record.DueDate.Month.ToString("D2");

        private static bool TryResolvePair(SuccessionState state, PersonId a, PersonId b,
            GameDate date, out FamilyPregnancyPair pair)
        {
            pair = default;
            if (a == b || a.IsEmpty || b.IsEmpty)
                return false;
            if (!state.World.Family.IsMember(a) || !state.World.Family.IsMember(b))
                return false;
            if (!state.World.TryGetPerson(a, out PersonState personA) ||
                !state.World.TryGetPerson(b, out PersonState personB))
                return false;
            if (!state.World.TryGetLife(a, out SuccessionPersonLife lifeA) ||
                !state.World.TryGetLife(b, out SuccessionPersonLife lifeB))
                return false;
            if (!lifeA.Alive || !lifeB.Alive)
                return false;

            PersonState female;
            PersonState male;
            SuccessionPersonLife femaleLife;
            SuccessionPersonLife maleLife;
            if (personA.Gender == PersonGender.Female && personB.Gender == PersonGender.Male)
            {
                female = personA;
                male = personB;
                femaleLife = lifeA;
                maleLife = lifeB;
            }
            else if (personA.Gender == PersonGender.Male && personB.Gender == PersonGender.Female)
            {
                female = personB;
                male = personA;
                femaleLife = lifeB;
                maleLife = lifeA;
            }
            else
            {
                return false;
            }

            int femaleAge = femaleLife.AgeAt(date);
            int maleAge = maleLife.AgeAt(date);
            if (femaleAge < MinimumFemaleAge || femaleAge > MaximumFemaleAge)
                return false;
            if (maleAge < MinimumMaleAge || maleAge > MaximumMaleAge)
                return false;

            pair = new FamilyPregnancyPair(female.Id, male.Id);
            return true;
        }

        private static string PairKey(PersonId a, PersonId b)
        {
            string first = a.Value ?? string.Empty;
            string second = b.Value ?? string.Empty;
            if (string.CompareOrdinal(first, second) > 0)
            {
                string swap = first;
                first = second;
                second = swap;
            }
            return first.Length.ToString(System.Globalization.CultureInfo.InvariantCulture) +
                   ":" + first + "|" +
                   second.Length.ToString(System.Globalization.CultureInfo.InvariantCulture) +
                   ":" + second;
        }
    }
}
