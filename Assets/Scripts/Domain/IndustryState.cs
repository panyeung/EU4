using System;
using System.Collections.Generic;

namespace SengokuChronicle.Domain
{
    /// <summary>Non-negative capped funds/supplies/intelligence triple.</summary>
    public readonly struct IndustryResources : IEquatable<IndustryResources>
    {
        public const int Minimum = 0;
        public const int Maximum = 999999;

        private IndustryResources(int funds, int supplies, int intelligence)
        {
            Funds = funds;
            Supplies = supplies;
            Intelligence = intelligence;
        }

        public int Funds { get; }
        public int Supplies { get; }
        public int Intelligence { get; }

        public static bool TryCreate(int funds, int supplies, int intelligence,
            out IndustryResources resources, out string error)
        {
            resources = default;
            if (funds < Minimum || funds > Maximum)
            {
                error = "资金超出允许范围 0..999999。";
                return false;
            }
            if (supplies < Minimum || supplies > Maximum)
            {
                error = "物资超出允许范围 0..999999。";
                return false;
            }
            if (intelligence < Minimum || intelligence > Maximum)
            {
                error = "情报超出允许范围 0..999999。";
                return false;
            }

            resources = new IndustryResources(funds, supplies, intelligence);
            error = string.Empty;
            return true;
        }

        public bool Equals(IndustryResources other) =>
            Funds == other.Funds && Supplies == other.Supplies && Intelligence == other.Intelligence;

        public override bool Equals(object obj) => obj is IndustryResources other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = Funds;
                hash = (hash * 397) ^ Supplies;
                hash = (hash * 397) ^ Intelligence;
                return hash;
            }
        }

        public static bool operator ==(IndustryResources a, IndustryResources b) => a.Equals(b);
        public static bool operator !=(IndustryResources a, IndustryResources b) => !a.Equals(b);

        public override string ToString() =>
            Funds.ToString(System.Globalization.CultureInfo.InvariantCulture) + "/" +
            Supplies.ToString(System.Globalization.CultureInfo.InvariantCulture) + "/" +
            Intelligence.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// One industry post assignment. An industry owns <see cref="IndustryDefinition.CapacityPosts"/>
    /// consecutive posts; an empty person ID means that post is vacant.
    /// </summary>
    public readonly struct IndustryAssignment
    {
        public IndustryAssignment(string industryId, int postIndex, PersonId personId)
        {
            IndustryId = industryId ?? string.Empty;
            PostIndex = postIndex < 0 ? 0 : postIndex;
            PersonId = personId;
        }

        public string IndustryId { get; }
        public int PostIndex { get; }
        public PersonId PersonId { get; }
    }

    /// <summary>
    /// Mutable SC-019 industry state: resources, the last settled month and every fixed
    /// post (the farm owns two, the trading house one). All slot mutation is validated
    /// here; person eligibility belongs to <see cref="IndustryService"/>.
    /// </summary>
    public sealed class IndustryState
    {
        private readonly IndustryAssignment[] _assignments;
        private IndustryResources _resources;
        private int _lastSettledMonthIndex;

        private IndustryState(IndustryResources resources, int lastSettledMonthIndex)
        {
            _resources = resources;
            _lastSettledMonthIndex = lastSettledMonthIndex;
            _assignments = new IndustryAssignment[IndustryCatalog.TotalPosts];
            int index = 0;
            foreach (IndustryDefinition definition in IndustryCatalog.All)
                for (int post = 0; post < definition.CapacityPosts; post++)
                    _assignments[index++] = new IndustryAssignment(definition.Id, post, default);
        }

        public IndustryResources Resources => _resources;
        public int LastSettledMonthIndex => _lastSettledMonthIndex;
        public IReadOnlyList<IndustryAssignment> Assignments => Array.AsReadOnly(_assignments);

        /// <summary>New game: 30 funds, 12 supplies, 0 intelligence, every post vacant.</summary>
        public static IndustryState CreateInitial(GameDate date) =>
            CreateInitial(date, null);

        /// <summary>
        /// Opening state with caller-supplied resources. A null value keeps the standard
        /// 30/12/0 opening; fixed demonstrations use this to constrain their budget.
        /// </summary>
        public static IndustryState CreateInitial(GameDate date, IndustryResources? opening)
        {
            IndustryResources resources;
            if (opening.HasValue)
                resources = opening.Value;
            else
                IndustryResources.TryCreate(30, 12, 0, out resources, out _);
            return new IndustryState(resources, IndustryCalendar.MonthIndex(date) - 1);
        }

        public IndustryState Clone()
        {
            var clone = new IndustryState(_resources, _lastSettledMonthIndex);
            for (int i = 0; i < _assignments.Length; i++)
                clone._assignments[i] = _assignments[i];
            return clone;
        }

        /// <summary>Post count of a known industry; zero for an unknown ID.</summary>
        public static int PostCount(string industryId) =>
            IndustryCatalog.TryGet(industryId, out IndustryDefinition definition)
                ? definition.CapacityPosts
                : 0;

        /// <summary>First post's assignee, the historical single-post accessor.</summary>
        public bool TryGetAssignee(string industryId, out PersonId personId) =>
            TryGetAssignee(industryId, 0, out personId);

        public bool TryGetAssignee(string industryId, int postIndex, out PersonId personId)
        {
            int index = IndexOf(industryId, postIndex);
            if (index < 0)
            {
                personId = default;
                return false;
            }

            personId = _assignments[index].PersonId;
            return true;
        }

        /// <summary>Addresses the first post; multi-post industries set every post separately.</summary>
        public bool TrySetAssignee(string industryId, PersonId personId, out string error) =>
            TrySetAssignee(industryId, 0, personId, out error);

        public bool TrySetAssignee(string industryId, int postIndex, PersonId personId,
            out string error)
        {
            int index = IndexOf(industryId, postIndex);
            if (index < 0)
            {
                error = "未知产业或岗位：" + (industryId ?? "(null)") + "#" + postIndex + "。";
                return false;
            }

            _assignments[index] = new IndustryAssignment(_assignments[index].IndustryId, postIndex,
                personId);
            error = string.Empty;
            return true;
        }

        /// <summary>Clears one post; the single-argument form clears every post of the industry.</summary>
        public bool TryClearAssignee(string industryId, int postIndex, out string error) =>
            TrySetAssignee(industryId, postIndex, default, out error);

        public bool TryClearAssignee(string industryId, out string error)
        {
            int postCount = PostCount(industryId);
            if (postCount <= 0)
            {
                error = "未知产业：" + (industryId ?? "(null)") + "。";
                return false;
            }

            for (int post = 0; post < postCount; post++)
                _assignments[IndexOf(industryId, post)] =
                    new IndustryAssignment(industryId, post, default);
            error = string.Empty;
            return true;
        }

        public bool TrySetResources(IndustryResources resources, out string error)
        {
            _resources = resources;
            error = string.Empty;
            return true;
        }

        public bool TrySetLastSettledMonthIndex(int monthIndex, out string error)
        {
            if (monthIndex < 0)
            {
                error = "已结算月份索引不能为负。";
                return false;
            }

            _lastSettledMonthIndex = monthIndex;
            error = string.Empty;
            return true;
        }

        private int IndexOf(string industryId, int postIndex)
        {
            if (string.IsNullOrEmpty(industryId) || postIndex < 0)
                return -1;
            for (int i = 0; i < _assignments.Length; i++)
                if (_assignments[i].PostIndex == postIndex &&
                    string.Equals(_assignments[i].IndustryId, industryId, StringComparison.Ordinal))
                    return i;
            return -1;
        }
    }
}
