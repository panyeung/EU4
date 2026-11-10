using System;
using System.Collections.Generic;

namespace SengokuChronicle.Domain
{
    /// <summary>
    /// One active pregnancy fact. The gestating person is the stable key; the other
    /// parent is retained even if that parent dies later so the confirmed parent-child
    /// fact survives the birth. Conception and due dates use the game calendar, and a
    /// valid pregnancy always spans exactly nine monthly steps.
    /// </summary>
    public readonly struct FamilyPregnancyRecord : IEquatable<FamilyPregnancyRecord>
    {
        public const int GestationMonths = 9;

        private FamilyPregnancyRecord(PersonId gestatingPersonId, PersonId otherParentId,
            GameDate conceptionDate, GameDate dueDate)
        {
            GestatingPersonId = gestatingPersonId;
            OtherParentId = otherParentId;
            ConceptionDate = conceptionDate;
            DueDate = dueDate;
        }

        /// <summary>The female spouse who carries the pregnancy; also the stable sort key.</summary>
        public PersonId GestatingPersonId { get; }
        /// <summary>The male spouse whose confirmed parentage is retained.</summary>
        public PersonId OtherParentId { get; }
        public GameDate ConceptionDate { get; }
        public GameDate DueDate { get; }

        public bool IsValid =>
            !GestatingPersonId.IsEmpty && !OtherParentId.IsEmpty &&
            GestatingPersonId != OtherParentId &&
            ConceptionDate.IsValid && DueDate.IsValid &&
            DueDate == AddMonths(ConceptionDate, GestationMonths);

        /// <summary>
        /// Creates one validated record. A due date that is not exactly nine monthly
        /// steps after conception is rejected instead of being silently normalized.
        /// </summary>
        public static bool TryCreate(PersonId gestatingPersonId, PersonId otherParentId,
            GameDate conceptionDate, GameDate dueDate, out FamilyPregnancyRecord record,
            out string error)
        {
            record = default;
            error = string.Empty;
            if (gestatingPersonId.IsEmpty || otherParentId.IsEmpty)
            {
                error = "孕期双方人物 ID 不能为空。";
                return false;
            }
            if (gestatingPersonId == otherParentId)
            {
                error = "孕期双方不能是同一人物。";
                return false;
            }
            if (!conceptionDate.IsValid || !dueDate.IsValid)
            {
                error = "孕期受孕日或预产日无效。";
                return false;
            }
            if (dueDate != AddMonths(conceptionDate, GestationMonths))
            {
                error = "预产日必须是受孕日之后九个整月。";
                return false;
            }

            record = new FamilyPregnancyRecord(gestatingPersonId, otherParentId,
                conceptionDate, dueDate);
            return true;
        }

        /// <summary>
        /// Adds whole months to a date, clamping the day to the target month length.
        /// Month advances always land on day 1, so the clamp only protects restored or
        /// programmatic dates.
        /// </summary>
        public static GameDate AddMonths(GameDate date, int months)
        {
            int totalMonths = (date.Year * 12) + (date.Month - 1) + months;
            int year = totalMonths / 12;
            int month = (totalMonths % 12) + 1;
            int day = Math.Min(date.Day, GameDate.DaysInMonth(year, month));
            return new GameDate(year, month, day);
        }

        /// <summary>Whole months elapsed since conception; negative before conception.</summary>
        public int CompletedMonths(GameDate date) =>
            ((date.Year * 12) + date.Month) - ((ConceptionDate.Year * 12) + ConceptionDate.Month);

        /// <summary>
        /// Displayed progress 1..9. The conception month is month 1 and the month
        /// before the due date is month 9; the due month itself resolves to a birth.
        /// </summary>
        public int ProgressMonth(GameDate date)
        {
            int month = CompletedMonths(date) + 1;
            if (month < 1)
                return 1;
            return month > GestationMonths ? GestationMonths : month;
        }

        public bool IsDueAt(GameDate date) => date >= DueDate;

        public bool Equals(FamilyPregnancyRecord other) =>
            GestatingPersonId == other.GestatingPersonId &&
            OtherParentId == other.OtherParentId &&
            ConceptionDate == other.ConceptionDate &&
            DueDate == other.DueDate;

        public override bool Equals(object obj) =>
            obj is FamilyPregnancyRecord other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = GestatingPersonId.GetHashCode();
                hash = (hash * 397) ^ OtherParentId.GetHashCode();
                hash = (hash * 397) ^ ConceptionDate.GetHashCode();
                hash = (hash * 397) ^ DueDate.GetHashCode();
                return hash;
            }
        }

        public override string ToString() =>
            GestatingPersonId + " + " + OtherParentId + " (" + ConceptionDate + " → " +
            DueDate + ")";
    }

    /// <summary>
    /// Ordered, validated collection of active pregnancies. At most one pregnancy may
    /// be carried by a person and at most one may exist for a spouse pair; the collection
    /// is keyed and enumerated by ordinal gestating-person ID, so capture, validation and
    /// replay all share one stable order.
    /// </summary>
    public sealed class FamilyPregnancyState
    {
        private readonly SortedDictionary<string, FamilyPregnancyRecord> _records =
            new SortedDictionary<string, FamilyPregnancyRecord>(StringComparer.Ordinal);

        public int Count => _records.Count;

        /// <summary>Read-only snapshot in stable ordinal gestating-person order.</summary>
        public IReadOnlyList<FamilyPregnancyRecord> Records
        {
            get
            {
                var snapshot = new List<FamilyPregnancyRecord>(_records.Count);
                foreach (FamilyPregnancyRecord record in _records.Values)
                    snapshot.Add(record);
                return snapshot.AsReadOnly();
            }
        }

        public bool TryGet(PersonId gestatingPersonId, out FamilyPregnancyRecord record)
        {
            record = default;
            return !gestatingPersonId.IsEmpty &&
                   _records.TryGetValue(gestatingPersonId.Value, out record);
        }

        /// <summary>True when either endpoint order matches one active pair.</summary>
        public bool ContainsPair(PersonId parentA, PersonId parentB)
        {
            foreach (FamilyPregnancyRecord record in _records.Values)
            {
                if ((record.GestatingPersonId == parentA && record.OtherParentId == parentB) ||
                    (record.GestatingPersonId == parentB && record.OtherParentId == parentA))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Adds one validated record. A duplicate female or duplicate pair is rejected
        /// without changing the collection.
        /// </summary>
        public bool TryAdd(FamilyPregnancyRecord record, out string error)
        {
            if (!record.IsValid)
            {
                error = "孕期记录无效。";
                return false;
            }
            if (_records.ContainsKey(record.GestatingPersonId.Value))
            {
                error = "同一女性最多只能有一条未完成孕期：" + record.GestatingPersonId.Value + "。";
                return false;
            }
            if (ContainsPair(record.GestatingPersonId, record.OtherParentId))
            {
                error = "同一配偶对最多只能有一条未完成孕期。";
                return false;
            }

            _records.Add(record.GestatingPersonId.Value, record);
            error = string.Empty;
            return true;
        }

        /// <summary>Removes one record during a planned completion, cancellation or rollback.</summary>
        internal bool TryRemove(PersonId gestatingPersonId, out string error)
        {
            if (gestatingPersonId.IsEmpty ||
                !_records.Remove(gestatingPersonId.Value))
            {
                error = "没有可移除的孕期记录：" + gestatingPersonId.Value + "。";
                return false;
            }
            error = string.Empty;
            return true;
        }

        internal bool TryRemove(FamilyPregnancyRecord record, out string error)
        {
            if (record.GestatingPersonId.IsEmpty ||
                !_records.TryGetValue(record.GestatingPersonId.Value,
                    out FamilyPregnancyRecord current) || !current.Equals(record))
            {
                error = "孕期记录已变化，无法按原记录移除。";
                return false;
            }
            return TryRemove(record.GestatingPersonId, out error);
        }
    }
}
