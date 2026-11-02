using System;

namespace SengokuChronicle.Domain
{
    /// <summary>Permanent person identifier (GDD 3.1). Stable across rename, marriage, inheritance and scene changes; never re-issued.</summary>
    public readonly struct PersonId : IEquatable<PersonId>
    {
        private readonly string _value;

        public PersonId(string value) { _value = value; }

        public string Value => _value;

        /// <summary>True when the identifier carries no usable text (GDD 3.1 permanent IDs must be non-empty).</summary>
        public bool IsEmpty => string.IsNullOrWhiteSpace(_value);

        public static bool TryParse(string value, out PersonId id)
        {
            id = new PersonId(value);
            return !id.IsEmpty;
        }

        public bool Equals(PersonId other) => string.Equals(_value, other._value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is PersonId other && Equals(other);
        public override int GetHashCode() => _value == null ? 0 : StringComparer.Ordinal.GetHashCode(_value);
        public override string ToString() => _value ?? string.Empty;

        public static bool operator ==(PersonId left, PersonId right) => left.Equals(right);
        public static bool operator !=(PersonId left, PersonId right) => !left.Equals(right);
    }

    /// <summary>Permanent family identifier. Stable across leadership change and generational succession (GDD 3.1, 4.x).</summary>
    public readonly struct FamilyId : IEquatable<FamilyId>
    {
        private readonly string _value;

        public FamilyId(string value) { _value = value; }

        public string Value => _value;

        public bool IsEmpty => string.IsNullOrWhiteSpace(_value);

        public static bool TryParse(string value, out FamilyId id)
        {
            id = new FamilyId(value);
            return !id.IsEmpty;
        }

        public bool Equals(FamilyId other) => string.Equals(_value, other._value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is FamilyId other && Equals(other);
        public override int GetHashCode() => _value == null ? 0 : StringComparer.Ordinal.GetHashCode(_value);
        public override string ToString() => _value ?? string.Empty;

        public static bool operator ==(FamilyId left, FamilyId right) => left.Equals(right);
        public static bool operator !=(FamilyId left, FamilyId right) => !left.Equals(right);
    }
}
