using System;
using System.Collections.Generic;

namespace SengokuChronicle.Domain
{
    /// <summary>Mutable interpersonal value axes from GDD 3.3 (好感, 信任, 恩义). Relationships must not collapse into a single number.</summary>
    public enum InterpersonalAxis
    {
        Affection,
        Trust,
        Obligation
    }

    /// <summary>Snapshot of one directed person pair and its mutable values.</summary>
    public readonly struct InterpersonalRecord
    {
        public InterpersonalRecord(PersonId from, PersonId to, int affection, int trust, int obligation, bool betrayalRecorded)
        {
            From = from;
            To = to;
            Affection = affection;
            Trust = trust;
            Obligation = obligation;
            BetrayalRecorded = betrayalRecorded;
        }

        public PersonId From { get; }
        public PersonId To { get; }
        public int Affection { get; }
        public int Trust { get; }
        public int Obligation { get; }
        public bool BetrayalRecorded { get; }
    }

    /// <summary>Result of reading or changing one directed value axis; mirrors RelationshipResult for the player-to-Npc table.</summary>
    public readonly struct InterpersonalResult
    {
        public InterpersonalResult(PersonId from, PersonId to, InterpersonalAxis axis, int previousValue, int currentValue, bool succeeded, string error)
        {
            From = from;
            To = to;
            Axis = axis;
            PreviousValue = previousValue;
            CurrentValue = currentValue;
            Succeeded = succeeded;
            Error = error ?? string.Empty;
        }

        public PersonId From { get; }
        public PersonId To { get; }
        public InterpersonalAxis Axis { get; }
        public int PreviousValue { get; }
        public int CurrentValue { get; }
        public bool Succeeded { get; }
        public string Error { get; }
    }

    /// <summary>
    /// Directed mutable interpersonal state (GDD 11.3 RelationshipState, changing half).
    /// Values change over time and never rewrite the fixed structural facts in <see cref="KinshipGraph"/>.
    /// A→B and B→A are independent states, matching GDD 3.3's asymmetric sentiment.
    /// </summary>
    public sealed class InterpersonalState
    {
        // Same explicit range as CharacterRelationships (GDD 3.2: 明确范围, 避免无限膨胀).
        public const int MinimumValue = -100;
        public const int MaximumValue = 100;

        private sealed class PairKey : IEquatable<PairKey>
        {
            public PairKey(PersonId from, PersonId to) { From = from; To = to; }
            public PersonId From { get; }
            public PersonId To { get; }
            public bool Equals(PairKey other) => other != null && From == other.From && To == other.To;
            public override bool Equals(object obj) => obj is PairKey other && Equals(other);
            public override int GetHashCode() => From.GetHashCode() * 397 + To.GetHashCode();
        }

        private sealed class PairValues
        {
            public int Affection;
            public int Trust;
            public int Obligation;
            public bool BetrayalRecorded;
        }

        private readonly Dictionary<PairKey, PairValues> _pairs = new Dictionary<PairKey, PairValues>();

        public int RecordedPairCount => _pairs.Count;

        /// <summary>Reads one axis for a directed pair. Absent pairs read as the default zero value.</summary>
        public InterpersonalResult TryGet(PersonId from, PersonId to, InterpersonalAxis axis)
        {
            if (!ValidatePairAndAxis(from, to, axis, out string error))
                return new InterpersonalResult(from, to, axis, 0, 0, false, error);

            int value = 0;
            if (_pairs.TryGetValue(new PairKey(from, to), out PairValues values))
                value = Read(values, axis);
            return new InterpersonalResult(from, to, axis, value, value, true, string.Empty);
        }

        /// <summary>Adds a delta to one axis, clamped at both bounds without overflow; other axes and kinship facts are untouched.</summary>
        public InterpersonalResult TryChange(PersonId from, PersonId to, InterpersonalAxis axis, int delta)
        {
            if (!ValidatePairAndAxis(from, to, axis, out string error))
                return new InterpersonalResult(from, to, axis, 0, 0, false, error);

            PairKey key = new PairKey(from, to);
            if (!_pairs.TryGetValue(key, out PairValues values))
            {
                values = new PairValues();
                _pairs.Add(key, values);
            }

            int current = Read(values, axis);
            long changed = (long)current + delta;
            int finalValue = changed < MinimumValue ? MinimumValue : changed > MaximumValue ? MaximumValue : (int)changed;
            Write(values, axis, finalValue);
            return new InterpersonalResult(from, to, axis, current, finalValue, true, string.Empty);
        }

        /// <summary>Marks that From considers To a betrayer (GDD 3.3 重要恩义与背叛). One-shot per directed pair; numeric axes are unaffected.</summary>
        public bool TryRecordBetrayal(PersonId from, PersonId to, out string error)
        {
            if (!ValidateEndpoint(from, to, out error))
                return false;

            PairKey key = new PairKey(from, to);
            if (!_pairs.TryGetValue(key, out PairValues values))
            {
                values = new PairValues();
                _pairs.Add(key, values);
            }
            if (values.BetrayalRecorded)
            {
                error = "Betrayal already recorded for this pair.";
                return false;
            }
            values.BetrayalRecorded = true;
            error = string.Empty;
            return true;
        }

        public bool IsBetrayalRecorded(PersonId from, PersonId to)
        {
            return !from.IsEmpty && !to.IsEmpty
                && _pairs.TryGetValue(new PairKey(from, to), out PairValues values)
                && values.BetrayalRecorded;
        }

        /// <summary>Deterministically ordered snapshot of every recorded pair, for validation and later persistence.</summary>
        public IReadOnlyList<InterpersonalRecord> Records
        {
            get
            {
                var snapshot = new List<InterpersonalRecord>(_pairs.Count);
                foreach (KeyValuePair<PairKey, PairValues> entry in _pairs)
                {
                    snapshot.Add(new InterpersonalRecord(
                        entry.Key.From, entry.Key.To,
                        entry.Value.Affection, entry.Value.Trust, entry.Value.Obligation, entry.Value.BetrayalRecorded));
                }
                snapshot.Sort((a, b) =>
                {
                    int byFrom = string.Compare(a.From.Value, b.From.Value, StringComparison.Ordinal);
                    return byFrom != 0 ? byFrom : string.Compare(a.To.Value, b.To.Value, StringComparison.Ordinal);
                });
                return snapshot.AsReadOnly();
            }
        }

        private static bool ValidatePairAndAxis(PersonId from, PersonId to, InterpersonalAxis axis, out string error)
        {
            if (!ValidateEndpoint(from, to, out error))
                return false;
            if (!Enum.IsDefined(typeof(InterpersonalAxis), axis))
            {
                error = "Interpersonal axis is undefined: " + (int)axis;
                return false;
            }
            error = string.Empty;
            return true;
        }

        private static bool ValidateEndpoint(PersonId from, PersonId to, out string error)
        {
            if (from.IsEmpty || to.IsEmpty)
            {
                error = "Interpersonal endpoints must not be empty.";
                return false;
            }
            if (from == to)
            {
                error = "Interpersonal state must relate two different persons.";
                return false;
            }
            error = string.Empty;
            return true;
        }

        private static int Read(PairValues values, InterpersonalAxis axis)
        {
            switch (axis)
            {
                case InterpersonalAxis.Affection: return values.Affection;
                case InterpersonalAxis.Trust: return values.Trust;
                default: return values.Obligation;
            }
        }

        private static void Write(PairValues values, InterpersonalAxis axis, int value)
        {
            switch (axis)
            {
                case InterpersonalAxis.Affection: values.Affection = value; break;
                case InterpersonalAxis.Trust: values.Trust = value; break;
                default: values.Obligation = value; break;
            }
        }
    }
}
