using System;
using System.Collections.Generic;

namespace SengokuChronicle.Domain
{
    /// <summary>
    /// Structural kinship kinds from GDD 3.3. These are fixed facts, not changing values:
    /// 父母与子女 (ParentChild), 兄弟姐妹 (Sibling), 婚姻 (Spouse), 收养 (Adoption).
    /// 师徒/家臣主君 membership is expressed through <see cref="FamilyMemberRole"/>;
    /// 好感/信任/恩义 mutable values are expressed through <see cref="InterpersonalState"/>.
    /// </summary>
    public enum KinshipType
    {
        /// <summary>Directed: parent to biological child. Permanent blood fact.</summary>
        ParentChild,
        /// <summary>Symmetric: permanent blood fact between siblings.</summary>
        Sibling,
        /// <summary>Symmetric: marriage fact between two persons.</summary>
        Spouse,
        /// <summary>Directed: adoptive parent to adopted child. Structural legal fact.</summary>
        Adoption
    }

    /// <summary>One fixed structural fact between two persons. Immutable once created.</summary>
    public readonly struct KinshipLink : IEquatable<KinshipLink>
    {
        public KinshipLink(PersonId from, PersonId to, KinshipType type)
        {
            From = from;
            To = to;
            Type = type;
        }

        public PersonId From { get; }
        public PersonId To { get; }
        public KinshipType Type { get; }

        /// <summary>Symmetric kinds connect both directions with one fact; directed kinds (parent→child, adopter→adopted) do not.</summary>
        public bool IsSymmetric => Type == KinshipType.Sibling || Type == KinshipType.Spouse;

        public KinshipLink Reversed() => new KinshipLink(To, From, Type);

        /// <summary>Structural facts never relate a person to themselves and require two known IDs and a defined kind.</summary>
        public static bool TryCreate(PersonId from, PersonId to, KinshipType type, out KinshipLink link, out string error)
        {
            link = new KinshipLink(from, to, type);
            if (from.IsEmpty || to.IsEmpty)
            {
                error = "Kinship endpoints must not be empty.";
                return false;
            }
            if (from == to)
            {
                error = "Kinship endpoints must be two different persons.";
                return false;
            }
            if (!Enum.IsDefined(typeof(KinshipType), type))
            {
                error = "Kinship type is undefined: " + (int)type;
                return false;
            }
            error = string.Empty;
            return true;
        }

        public bool Equals(KinshipLink other) => From == other.From && To == other.To && Type == other.Type;
        public override bool Equals(object obj) => obj is KinshipLink other && Equals(other);
        public override int GetHashCode()
        {
            int hash = From.GetHashCode();
            hash = hash * 397 + To.GetHashCode();
            hash = hash * 397 + (int)Type;
            return hash;
        }
        public override string ToString() => From + " -[" + Type + "]- " + To;
    }

    /// <summary>
    /// Append-only ledger of structural kinship facts (GDD 11.3 RelationshipState, structural half).
    /// Facts are permanent: the graph intentionally exposes no removal or mutation API, so blood,
    /// marriage and adoption records survive every later membership or sentiment change.
    /// </summary>
    public sealed class KinshipGraph
    {
        private readonly List<KinshipLink> _links = new List<KinshipLink>();
        private readonly HashSet<KinshipLink> _exact = new HashSet<KinshipLink>();
        // Reverse index of symmetric kinds so A-Sibling-B and B-Sibling-A are one fact.
        private readonly HashSet<KinshipLink> _symmetricReverses = new HashSet<KinshipLink>();

        public int Count => _links.Count;

        public IReadOnlyList<KinshipLink> Links => _links.AsReadOnly();

        public bool Contains(KinshipLink link)
        {
            return _exact.Contains(link) || (link.IsSymmetric && _symmetricReverses.Contains(link));
        }

        /// <summary>Registers one structural fact. Rejects malformed links and duplicates (including the mirror of a symmetric fact) without changing state.</summary>
        public bool TryAdd(KinshipLink link, out string error)
        {
            if (!KinshipLink.TryCreate(link.From, link.To, link.Type, out _, out string validationError))
            {
                error = validationError;
                return false;
            }
            if (_exact.Contains(link) || (link.IsSymmetric && _symmetricReverses.Contains(link)))
            {
                error = "Kinship link already recorded: " + link;
                return false;
            }
            _links.Add(link);
            _exact.Add(link);
            if (link.IsSymmetric)
                _symmetricReverses.Add(link.Reversed());
            error = string.Empty;
            return true;
        }

        /// <summary>All recorded facts that mention one person, either as From or as To.</summary>
        public IReadOnlyList<KinshipLink> LinksFor(PersonId personId)
        {
            var found = new List<KinshipLink>();
            for (int i = 0; i < _links.Count; i++)
            {
                if (_links[i].From == personId || _links[i].To == personId)
                    found.Add(_links[i]);
            }
            return found.AsReadOnly();
        }
    }
}
