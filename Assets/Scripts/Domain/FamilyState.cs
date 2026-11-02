using System;
using System.Collections.Generic;

namespace SengokuChronicle.Domain
{
    /// <summary>Family identity categories from GDD 3.1 (当代家主/配偶/直系亲族/旁系亲族/养子及养女/外姓家臣/弟子/客将和临时雇员).</summary>
    public enum FamilyMemberRole
    {
        Head,
        Spouse,
        DirectKin,
        CollateralKin,
        AdoptedChild,
        Retainer,
        Disciple,
        Guest
    }

    /// <summary>One person's membership record inside one family.</summary>
    public readonly struct FamilyMembership
    {
        public FamilyMembership(PersonId personId, FamilyMemberRole role)
        {
            PersonId = personId;
            Role = role;
        }

        public PersonId PersonId { get; }
        public FamilyMemberRole Role { get; }
    }

    /// <summary>
    /// Mutable family roster (GDD 3.1, 11.3 FamilyState). The FamilyId is permanent; membership
    /// and roles change over a lifetime, but a person's own ID never changes when moving families.
    /// </summary>
    public sealed class FamilyState
    {
        private readonly Dictionary<PersonId, FamilyMemberRole> _members = new Dictionary<PersonId, FamilyMemberRole>();
        private string _displayName;

        public FamilyState(FamilyId id, string displayName)
        {
            Id = id;
            _displayName = displayName ?? string.Empty;
        }

        /// <summary>Stable identifier; cannot be reassigned.</summary>
        public FamilyId Id { get; }

        public string DisplayName => _displayName;

        public int MemberCount => _members.Count;

        public bool IsMember(PersonId personId) => _members.ContainsKey(personId);

        public bool TryGetRole(PersonId personId, out FamilyMemberRole role) => _members.TryGetValue(personId, out role);

        /// <summary>Read-only snapshot of membership sorted by person ID for deterministic validation output.</summary>
        public IReadOnlyList<FamilyMembership> Members
        {
            get
            {
                var snapshot = new List<FamilyMembership>(_members.Count);
                foreach (KeyValuePair<PersonId, FamilyMemberRole> entry in _members)
                    snapshot.Add(new FamilyMembership(entry.Key, entry.Value));
                snapshot.Sort((a, b) => string.Compare(a.PersonId.Value, b.PersonId.Value, StringComparison.Ordinal));
                return snapshot.AsReadOnly();
            }
        }

        public bool TryRename(string newName, out string error)
        {
            if (string.IsNullOrWhiteSpace(newName))
            {
                error = "Family display name must not be empty.";
                return false;
            }
            _displayName = newName;
            error = string.Empty;
            return true;
        }

        /// <summary>Adds a member. Blank person IDs, undefined roles and repeated memberships are rejected without changing state.</summary>
        public bool TryAddMember(PersonId personId, FamilyMemberRole role, out string error)
        {
            if (personId.IsEmpty)
            {
                error = "Member person ID must not be empty.";
                return false;
            }
            if (!Enum.IsDefined(typeof(FamilyMemberRole), role))
            {
                error = "Family member role is undefined: " + (int)role;
                return false;
            }
            if (_members.ContainsKey(personId))
            {
                error = "Person is already a member of this family.";
                return false;
            }
            _members.Add(personId, role);
            error = string.Empty;
            return true;
        }

        /// <summary>Changes an existing member's role (e.g. adoption or promotion); unknown members are rejected.</summary>
        public bool TrySetRole(PersonId personId, FamilyMemberRole role, out string error)
        {
            if (!Enum.IsDefined(typeof(FamilyMemberRole), role))
            {
                error = "Family member role is undefined: " + (int)role;
                return false;
            }
            if (!_members.ContainsKey(personId))
            {
                error = "Person is not a member of this family.";
                return false;
            }
            _members[personId] = role;
            error = string.Empty;
            return true;
        }

        /// <summary>Removes a membership (death or departure is handled by later milestones); unknown members are rejected.</summary>
        public bool TryRemoveMember(PersonId personId, out string error)
        {
            if (!_members.Remove(personId))
            {
                error = "Person is not a member of this family.";
                return false;
            }
            error = string.Empty;
            return true;
        }

        public override string ToString() => Id + " " + _displayName;
    }
}
