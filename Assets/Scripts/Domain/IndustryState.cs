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

    /// <summary>One industry slot assignment; an empty person ID means the post is vacant.</summary>
    public readonly struct IndustryAssignment
    {
        public IndustryAssignment(string industryId, PersonId personId)
        {
            IndustryId = industryId ?? string.Empty;
            PersonId = personId;
        }

        public string IndustryId { get; }
        public PersonId PersonId { get; }
    }

    /// <summary>
    /// Mutable SC-019 industry state: resources, the last settled month and the two
    /// fixed slots. All slot mutation is validated here; person eligibility belongs
    /// to <see cref="IndustryService"/>.
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
            _assignments = new IndustryAssignment[IndustryCatalog.All.Count];
            for (int i = 0; i < _assignments.Length; i++)
                _assignments[i] = new IndustryAssignment(IndustryCatalog.All[i].Id, default);
        }

        public IndustryResources Resources => _resources;
        public int LastSettledMonthIndex => _lastSettledMonthIndex;
        public IReadOnlyList<IndustryAssignment> Assignments => Array.AsReadOnly(_assignments);

        /// <summary>New game: 30 funds, 12 supplies, 0 intelligence, both posts vacant.</summary>
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

        public bool TryGetAssignee(string industryId, out PersonId personId)
        {
            int index = IndexOf(industryId);
            if (index < 0)
            {
                personId = default;
                return false;
            }

            personId = _assignments[index].PersonId;
            return true;
        }

        public bool TrySetAssignee(string industryId, PersonId personId, out string error)
        {
            int index = IndexOf(industryId);
            if (index < 0)
            {
                error = "未知产业：" + (industryId ?? "(null)") + "。";
                return false;
            }

            _assignments[index] = new IndustryAssignment(_assignments[index].IndustryId, personId);
            error = string.Empty;
            return true;
        }

        public bool TryClearAssignee(string industryId, out string error) =>
            TrySetAssignee(industryId, default, out error);

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

        private int IndexOf(string industryId)
        {
            if (string.IsNullOrEmpty(industryId))
                return -1;
            for (int i = 0; i < _assignments.Length; i++)
                if (string.Equals(_assignments[i].IndustryId, industryId, StringComparison.Ordinal))
                    return i;
            return -1;
        }
    }
}
