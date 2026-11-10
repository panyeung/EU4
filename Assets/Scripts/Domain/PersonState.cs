using System;

namespace SengokuChronicle.Domain
{
    /// <summary>
    /// Stable person fact for the M04B birth simulation. Only <see cref="Male"/> and
    /// <see cref="Female"/> can start a pregnancy; <see cref="Unspecified"/> people
    /// remain fully playable but are never inferred from a name or relationship.
    /// Numeric values are part of the save compatibility contract (v13).
    /// </summary>
    public enum PersonGender
    {
        Unspecified = 0,
        Male = 1,
        Female = 2
    }

    /// <summary>
    /// Runtime identity of one person. The PersonId is permanent (GDD 3.1): renaming,
    /// marriage or succession change the display data only, never the identifier.
    /// Family membership lives in <see cref="FamilyState"/>; kinship in <see cref="KinshipGraph"/>.
    /// </summary>
    public sealed class PersonState
    {
        public PersonState(PersonId id, string displayName,
            PersonGender gender = PersonGender.Unspecified)
        {
            Id = id;
            _displayName = displayName ?? string.Empty;
            Gender = gender;
        }

        /// <summary>Stable identifier; cannot be reassigned.</summary>
        public PersonId Id { get; }

        /// <summary>Stable gender fact; never changes through marriage or succession.</summary>
        public PersonGender Gender { get; }

        private string _displayName;
        public string DisplayName => _displayName;

        /// <summary>Renames the person while keeping the permanent ID. Blank names are rejected and leave state untouched.</summary>
        public bool TryRename(string newName, out string error)
        {
            if (string.IsNullOrWhiteSpace(newName))
            {
                error = "Display name must not be empty.";
                return false;
            }
            _displayName = newName;
            error = string.Empty;
            return true;
        }

        public override string ToString() => Id + " " + _displayName;
    }
}
