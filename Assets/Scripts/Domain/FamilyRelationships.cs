using System;
using System.Collections.Generic;

namespace SengokuChronicle.Domain
{
    /// <summary>
    /// Result of reading or changing one ordered (current head, family member)
    /// relationship value. The pair is explicit because the same family member has a
    /// different value for each head; old-head records remain addressable after a
    /// succession.
    /// </summary>
    public readonly struct FamilyRelationshipResult
    {
        public FamilyRelationshipResult(PersonId headId, PersonId memberId, int previousValue,
            int currentValue, bool succeeded, string error)
        {
            HeadId = headId;
            MemberId = memberId;
            PreviousValue = previousValue;
            CurrentValue = currentValue;
            Succeeded = succeeded;
            Error = error ?? string.Empty;
        }

        public PersonId HeadId { get; }
        public PersonId MemberId { get; }
        public int PreviousValue { get; }
        public int CurrentValue { get; }
        public bool Succeeded { get; }
        public string Error { get; }
    }

    /// <summary>One persisted ordered (current head, family member) relationship value.</summary>
    public readonly struct FamilyRelationshipEntry
    {
        public FamilyRelationshipEntry(PersonId headId, PersonId memberId, int value)
        {
            HeadId = headId;
            MemberId = memberId;
            Value = value;
        }

        public PersonId HeadId { get; }
        public PersonId MemberId { get; }
        public int Value { get; }
    }

    /// <summary>
    /// Stable key for one ordered (current head, family member) pair. A family member
    /// can have different values under different heads; self-pairs are never valid.
    /// </summary>
    public readonly struct FamilyRelationshipKey : IEquatable<FamilyRelationshipKey>
    {
        public FamilyRelationshipKey(PersonId headId, PersonId memberId)
        {
            HeadId = headId;
            MemberId = memberId;
        }

        public PersonId HeadId { get; }
        public PersonId MemberId { get; }

        public bool IsValid => !HeadId.IsEmpty && !MemberId.IsEmpty && HeadId != MemberId;

        public bool Equals(FamilyRelationshipKey other) =>
            HeadId == other.HeadId && MemberId == other.MemberId;

        public override bool Equals(object obj) => obj is FamilyRelationshipKey other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                return (HeadId.GetHashCode() * 397) ^ MemberId.GetHashCode();
            }
        }

        public override string ToString() => HeadId.Value + " -> " + MemberId.Value;
    }

    /// <summary>
    /// Runtime store for the values of ordered (current head, family member) pairs.
    /// Pair order identifies the two people; the localized event text defines the
    /// narrative meaning of each value. It is intentionally separate from
    /// <see cref="CharacterRelationships"/>; NPC behavior and persistence remain
    /// unchanged. Values clamp to the same [-100, 100] range. No value is inferred
    /// from kinship, and a new head starts with no copied values.
    /// </summary>
    public sealed class FamilyRelationships
    {
        public const int MinimumValue = -100;
        public const int MaximumValue = 100;

        private readonly Dictionary<FamilyRelationshipKey, int> _values =
            new Dictionary<FamilyRelationshipKey, int>();

        public static FamilyRelationships CreateEmpty() => new FamilyRelationships();

        public FamilyRelationshipResult TryGet(PersonId headId, PersonId memberId)
        {
            if (!TryValidatePair(headId, memberId, out string error))
                return new FamilyRelationshipResult(headId, memberId, 0, 0, false, error);

            var key = new FamilyRelationshipKey(headId, memberId);
            int value = _values.TryGetValue(key, out int stored) ? stored : 0;
            return new FamilyRelationshipResult(headId, memberId, value, value, true, string.Empty);
        }

        public FamilyRelationshipResult TryChange(PersonId headId, PersonId memberId, int delta)
        {
            if (!TryValidatePair(headId, memberId, out string error))
                return new FamilyRelationshipResult(headId, memberId, 0, 0, false, error);

            var key = new FamilyRelationshipKey(headId, memberId);
            int current = _values.TryGetValue(key, out int stored) ? stored : 0;
            long changed = (long)current + delta;
            int final = changed < MinimumValue ? MinimumValue : changed > MaximumValue ? MaximumValue : (int)changed;
            _values[key] = final;
            return new FamilyRelationshipResult(headId, memberId, current, final, true, string.Empty);
        }

        /// <summary>Snapshot in stable head/member order for save capture and tests.</summary>
        public IReadOnlyList<FamilyRelationshipEntry> Entries
        {
            get
            {
                var entries = new List<FamilyRelationshipEntry>(_values.Count);
                foreach (KeyValuePair<FamilyRelationshipKey, int> pair in _values)
                    entries.Add(new FamilyRelationshipEntry(pair.Key.HeadId, pair.Key.MemberId, pair.Value));
                entries.Sort((left, right) =>
                {
                    int head = string.CompareOrdinal(left.HeadId.Value, right.HeadId.Value);
                    return head != 0
                        ? head
                        : string.CompareOrdinal(left.MemberId.Value, right.MemberId.Value);
                });
                return entries.AsReadOnly();
            }
        }

        private static bool TryValidatePair(PersonId headId, PersonId memberId, out string error)
        {
            if (headId.IsEmpty)
            {
                error = "家庭关系的主体家主 ID 为空。";
                return false;
            }
            if (memberId.IsEmpty)
            {
                error = "家庭关系的家人 ID 为空。";
                return false;
            }
            if (headId == memberId)
            {
                error = "家庭关系不能指向家主本人。";
                return false;
            }

            error = string.Empty;
            return true;
        }
    }
}
