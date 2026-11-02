using System;

namespace SengokuChronicle.Domain
{
    /// <summary>
    /// Runtime identity of one person. The PersonId is permanent (GDD 3.1): renaming,
    /// marriage or succession change the display data only, never the identifier.
    /// Family membership lives in <see cref="FamilyState"/>; kinship in <see cref="KinshipGraph"/>.
    /// </summary>
    public sealed class PersonState
    {
        public PersonState(PersonId id, string displayName)
        {
            Id = id;
            _displayName = displayName ?? string.Empty;
        }

        /// <summary>Stable identifier; cannot be reassigned.</summary>
        public PersonId Id { get; }

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
