using System;
using System.Collections.Generic;
using SengokuChronicle.Domain;

namespace SengokuChronicle.Presentation
{
    /// <summary>Where one family member is displayed in the read-only family graph.</summary>
    public enum FamilyTreeMemberSection
    {
        /// <summary>Family member with at least one visible structural kinship link.</summary>
        Kinship = 0,
        /// <summary>Family member without a visible kinship link; still part of the family.</summary>
        UnconnectedKin = 1,
        /// <summary>Retainer, disciple or guest; never drawn as blood or marriage kin.</summary>
        Household = 2
    }

    /// <summary>
    /// Immutable display data for one person. It is derived from <see cref="SuccessionWorld"/>
    /// and never owns or mutates the underlying person, membership or kinship facts.
    /// </summary>
    public readonly struct FamilyTreePersonNode
    {
        public FamilyTreePersonNode(PersonId personId, string displayName, FamilyMemberRole role,
            int age, bool hasAge, bool hasLife, bool isAlive, bool isRetired, bool isHead,
            bool isDesignatedHeir, bool isDesignatedHeirValid, FamilyTreeMemberSection section)
        {
            PersonId = personId;
            DisplayName = displayName ?? string.Empty;
            Role = role;
            Age = age;
            HasAge = hasAge;
            HasLife = hasLife;
            IsAlive = isAlive;
            IsRetired = isRetired;
            IsHead = isHead;
            IsDesignatedHeir = isDesignatedHeir;
            IsDesignatedHeirValid = isDesignatedHeirValid;
            Section = section;
        }

        public PersonId PersonId { get; }
        public string DisplayName { get; }
        public FamilyMemberRole Role { get; }
        /// <summary>Age at the requested date, or 0 when no birth year is available.</summary>
        public int Age { get; }
        public bool HasAge { get; }
        /// <summary>True when a life record exists, even if it cannot supply an age.</summary>
        public bool HasLife { get; }
        /// <summary>Only meaningful when <see cref="HasLife"/> is true.</summary>
        public bool IsAlive { get; }
        /// <summary>Only meaningful when <see cref="HasLife"/> is true.</summary>
        public bool IsRetired { get; }
        public bool IsHead { get; }
        public bool IsDesignatedHeir { get; }
        public bool IsDesignatedHeirValid { get; }
        public FamilyTreeMemberSection Section { get; }

        public bool IsKin => Section == FamilyTreeMemberSection.Kinship;
        public bool IsUnconnectedKin => Section == FamilyTreeMemberSection.UnconnectedKin;
        public bool IsHousehold => Section == FamilyTreeMemberSection.Household;

        public string RoleName
        {
            get
            {
                switch (Role)
                {
                    case FamilyMemberRole.Head: return "家主";
                    case FamilyMemberRole.Spouse: return "配偶";
                    case FamilyMemberRole.DirectKin: return "直系亲族";
                    case FamilyMemberRole.CollateralKin: return "旁系亲族";
                    case FamilyMemberRole.AdoptedChild: return "养子/养女";
                    case FamilyMemberRole.Retainer: return "家臣";
                    case FamilyMemberRole.Disciple: return "弟子";
                    case FamilyMemberRole.Guest: return "客将";
                    default: return Role.ToString();
                }
            }
        }
    }

    /// <summary>
    /// One visible structural kinship fact between two displayed family members.
    /// The direction of <see cref="KinshipType.ParentChild"/> and
    /// <see cref="KinshipType.Adoption"/> is the recorded fact's own direction
    /// (parent/adopter to child); symmetric kinds carry no direction.
    /// </summary>
    public readonly struct FamilyTreeRelationEdge
    {
        public FamilyTreeRelationEdge(PersonId from, PersonId to, KinshipType type)
        {
            From = from;
            To = to;
            Type = type;
        }

        public PersonId From { get; }
        public PersonId To { get; }
        public KinshipType Type { get; }

        public bool IsSymmetric => Type == KinshipType.Sibling || Type == KinshipType.Spouse;

        public string TypeName
        {
            get
            {
                switch (Type)
                {
                    case KinshipType.ParentChild: return "亲子";
                    case KinshipType.Adoption: return "收养";
                    case KinshipType.Sibling: return "手足";
                    case KinshipType.Spouse: return "配偶";
                    default: return Type.ToString();
                }
            }
        }
    }

    /// <summary>
    /// Read-only snapshot of every valid family member and every visible structural
    /// kinship edge. Persons and edges are sorted by stable IDs and type, so callers
    /// cannot make the display depend on insertion or iteration order. No method
    /// mutates succession, membership or kinship state.
    /// </summary>
    public sealed class FamilyTreeGraphSnapshot
    {
        private readonly List<FamilyTreePersonNode> _persons =
            new List<FamilyTreePersonNode>();
        private readonly List<FamilyTreeRelationEdge> _relations =
            new List<FamilyTreeRelationEdge>();
        private readonly List<FamilyTreePersonNode> _kinshipMembers =
            new List<FamilyTreePersonNode>();
        private readonly List<FamilyTreePersonNode> _unconnectedKin =
            new List<FamilyTreePersonNode>();
        private readonly List<FamilyTreePersonNode> _householdMembers =
            new List<FamilyTreePersonNode>();
        private readonly Dictionary<string, int> _indexById =
            new Dictionary<string, int>(StringComparer.Ordinal);

        public FamilyTreeGraphSnapshot(string familyName,
            IEnumerable<FamilyTreePersonNode> persons,
            IEnumerable<FamilyTreeRelationEdge> relations)
        {
            FamilyName = familyName ?? string.Empty;
            if (persons != null)
            {
                foreach (FamilyTreePersonNode person in persons)
                {
                    string key = person.PersonId.Value ?? string.Empty;
                    if (string.IsNullOrEmpty(key) || _indexById.ContainsKey(key))
                        continue;
                    _indexById.Add(key, _persons.Count);
                    _persons.Add(person);
                }
            }

            _persons.Sort(ComparePersons);
            _indexById.Clear();
            for (int i = 0; i < _persons.Count; i++)
                _indexById.Add(_persons[i].PersonId.Value, i);

            var seen = new HashSet<string>(StringComparer.Ordinal);
            if (relations != null)
            {
                foreach (FamilyTreeRelationEdge relation in relations)
                {
                    if (relation.From == relation.To)
                        continue;
                    if (!Contains(relation.From) || !Contains(relation.To))
                        continue;
                    // Never draw a family link to a retainer, disciple or guest.
                    if (IsHousehold(relation.From) || IsHousehold(relation.To))
                        continue;
                    string key = EdgeKey(relation.From, relation.To, relation.Type);
                    if (seen.Contains(key))
                        continue;
                    if (relation.IsSymmetric)
                    {
                        string reverse = EdgeKey(relation.To, relation.From, relation.Type);
                        if (seen.Contains(reverse))
                            continue;
                        seen.Add(reverse);
                    }
                    seen.Add(key);
                    _relations.Add(relation);
                }
            }

            _relations.Sort(CompareRelations);
            for (int i = 0; i < _persons.Count; i++)
            {
                FamilyTreePersonNode person = _persons[i];
                switch (person.Section)
                {
                    case FamilyTreeMemberSection.Household:
                        _householdMembers.Add(person);
                        break;
                    case FamilyTreeMemberSection.UnconnectedKin:
                        _unconnectedKin.Add(person);
                        break;
                    default:
                        _kinshipMembers.Add(person);
                        break;
                }
            }
        }

        public string FamilyName { get; }
        public IReadOnlyList<FamilyTreePersonNode> Persons => _persons.AsReadOnly();
        public IReadOnlyList<FamilyTreeRelationEdge> Relations => _relations.AsReadOnly();
        public IReadOnlyList<FamilyTreePersonNode> KinshipMembers => _kinshipMembers.AsReadOnly();
        public IReadOnlyList<FamilyTreePersonNode> UnconnectedKin => _unconnectedKin.AsReadOnly();
        public IReadOnlyList<FamilyTreePersonNode> HouseholdMembers => _householdMembers.AsReadOnly();
        public int Count => _persons.Count;

        public bool Contains(PersonId personId)
        {
            return personId.Value != null && _indexById.ContainsKey(personId.Value);
        }

        public bool TryGetPerson(PersonId personId, out FamilyTreePersonNode node)
        {
            if (personId.Value != null && _indexById.TryGetValue(personId.Value, out int index))
            {
                node = _persons[index];
                return true;
            }
            node = default;
            return false;
        }

        private bool IsHousehold(PersonId personId)
        {
            return TryGetPerson(personId, out FamilyTreePersonNode node) && node.IsHousehold;
        }

        private static string EdgeKey(PersonId from, PersonId to, KinshipType type)
        {
            return (from.Value ?? string.Empty) + "\u001f" + (to.Value ?? string.Empty) +
                   "\u001f" + (int)type;
        }

        private static int ComparePersons(FamilyTreePersonNode a, FamilyTreePersonNode b)
        {
            return string.Compare(a.PersonId.Value ?? string.Empty,
                b.PersonId.Value ?? string.Empty, StringComparison.Ordinal);
        }

        private static int CompareRelations(FamilyTreeRelationEdge a, FamilyTreeRelationEdge b)
        {
            int from = string.Compare(a.From.Value ?? string.Empty, b.From.Value ?? string.Empty,
                StringComparison.Ordinal);
            if (from != 0)
                return from;
            int to = string.Compare(a.To.Value ?? string.Empty, b.To.Value ?? string.Empty,
                StringComparison.Ordinal);
            if (to != 0)
                return to;
            return ((int)a.Type).CompareTo((int)b.Type);
        }
    }

    /// <summary>
    /// Derives the display graph from real session state. Membership comes from
    /// <see cref="SuccessionWorld.Family"/>, names/life from <see cref="SuccessionWorld"/>,
    /// and structural facts from <see cref="SuccessionWorld.Kinship"/>. Nothing is
    /// hard-coded to SC-013 names, IDs, counts or generations.
    /// </summary>
    public static class FamilyTreeGraphBuilder
    {
        public static FamilyTreeGraphSnapshot Build(SuccessionState state, GameDate date)
        {
            if (state == null || state.World == null)
                return new FamilyTreeGraphSnapshot(string.Empty,
                    Array.Empty<FamilyTreePersonNode>(), Array.Empty<FamilyTreeRelationEdge>());

            SuccessionWorld world = state.World;
            IReadOnlyList<FamilyMembership> members = world.Family.Members;
            var persons = new List<FamilyTreePersonNode>(members.Count);
            var roles = new Dictionary<PersonId, FamilyMemberRole>();
            var linked = new HashSet<PersonId>();
            for (int i = 0; i < members.Count; i++)
            {
                FamilyMembership membership = members[i];
                if (!world.TryGetPerson(membership.PersonId, out PersonState person))
                    continue;
                roles[membership.PersonId] = membership.Role;
                persons.Add(CreatePerson(state, person, membership.Role, date,
                    FamilyTreeMemberSection.UnconnectedKin));
            }

            // A link only counts when both endpoints are displayable kin. Edges to a
            // removed person or to a retainer/disciple/guest are ignored, but the
            // remaining member is never dropped from the roster.
            IReadOnlyList<KinshipLink> links = world.Kinship.Links;
            for (int i = 0; i < links.Count; i++)
            {
                KinshipLink link = links[i];
                if (!roles.ContainsKey(link.From) || !roles.ContainsKey(link.To))
                    continue;
                if (!IsKinRole(roles[link.From]) || !IsKinRole(roles[link.To]))
                    continue;
                linked.Add(link.From);
                linked.Add(link.To);
            }

            for (int i = 0; i < persons.Count; i++)
            {
                FamilyTreePersonNode node = persons[i];
                if (node.IsHousehold)
                    continue;
                if (linked.Contains(node.PersonId))
                    persons[i] = WithSection(node, FamilyTreeMemberSection.Kinship);
            }

            var relations = new List<FamilyTreeRelationEdge>(links.Count);
            for (int i = 0; i < links.Count; i++)
            {
                KinshipLink link = links[i];
                if (!roles.ContainsKey(link.From) || !roles.ContainsKey(link.To))
                    continue;
                if (!IsKinRole(roles[link.From]) || !IsKinRole(roles[link.To]))
                    continue;
                relations.Add(new FamilyTreeRelationEdge(link.From, link.To, link.Type));
            }

            return new FamilyTreeGraphSnapshot(world.Family.DisplayName, persons, relations);
        }

        private static FamilyTreePersonNode CreatePerson(SuccessionState state, PersonState person,
            FamilyMemberRole role, GameDate date, FamilyTreeMemberSection section)
        {
            bool hasLife = state.World.TryGetLife(person.Id, out SuccessionPersonLife life);
            bool hasAge = hasLife && life.BirthYear > 0 && date.IsValid;
            int age = hasAge ? life.AgeAt(date) : 0;
            bool isHousehold = !IsKinRole(role);
            bool isDesignatedHeir = !state.DesignatedHeirId.IsEmpty &&
                                    state.DesignatedHeirId == person.Id;
            bool isValidHeir = isDesignatedHeir &&
                               SuccessionService.IsEligibleSuccessor(state, person.Id, date);
            return new FamilyTreePersonNode(person.Id, person.DisplayName, role, age, hasAge,
                hasLife, hasLife && life.Alive, hasLife && life.Retired, state.HeadPersonId == person.Id,
                isDesignatedHeir, isValidHeir,
                isHousehold ? FamilyTreeMemberSection.Household : section);
        }

        private static FamilyTreePersonNode WithSection(FamilyTreePersonNode node,
            FamilyTreeMemberSection section)
        {
            return new FamilyTreePersonNode(node.PersonId, node.DisplayName, node.Role, node.Age,
                node.HasAge, node.HasLife, node.IsAlive, node.IsRetired, node.IsHead,
                node.IsDesignatedHeir, node.IsDesignatedHeirValid, section);
        }

        private static bool IsKinRole(FamilyMemberRole role)
        {
            return role == FamilyMemberRole.Head ||
                   role == FamilyMemberRole.Spouse ||
                   role == FamilyMemberRole.DirectKin ||
                   role == FamilyMemberRole.CollateralKin ||
                   role == FamilyMemberRole.AdoptedChild;
        }
    }
}
