using System;
using System.Globalization;

namespace SengokuChronicle.Domain
{
    /// <summary>Five growth attributes shared by education and future M3 growth.</summary>
    public enum GrowthAttribute
    {
        Ninjutsu = 0,
        MartialArts = 1,
        Scholarship = 2,
        Management = 3,
        Social = 4
    }

    /// <summary>One annual education direction; free growth has no attribute or mentor.</summary>
    public enum EducationDirection
    {
        Ninjutsu = 0,
        MartialArts = 1,
        Scholarship = 2,
        Management = 3,
        Social = 4,
        FreeGrowth = 5
    }

    /// <summary>Fixed mentor specialty used by the deterministic SC-020 rules.</summary>
    public enum MentorSpecialty
    {
        None = 0,
        Ninjutsu = 1,
        MartialArts = 2,
        Scholarship = 3,
        Management = 4,
        Social = 5
    }

    /// <summary>
    /// Immutable 0..100 growth attribute quintet. Construction rejects out-of-range
    /// values; every applied gain is clamped so no caller can store an illegal value.
    /// </summary>
    public readonly struct CharacterGrowthValues : IEquatable<CharacterGrowthValues>
    {
        public const int Minimum = 0;
        public const int Maximum = 100;

        private CharacterGrowthValues(int ninjutsu, int martialArts, int scholarship,
            int management, int social)
        {
            Ninjutsu = ninjutsu;
            MartialArts = martialArts;
            Scholarship = scholarship;
            Management = management;
            Social = social;
        }

        public int Ninjutsu { get; }
        public int MartialArts { get; }
        public int Scholarship { get; }
        public int Management { get; }
        public int Social { get; }

        public static bool TryCreate(int ninjutsu, int martialArts, int scholarship,
            int management, int social, out CharacterGrowthValues values, out string error)
        {
            values = default;
            if (!InRange(ninjutsu))
            {
                error = "忍术超出允许范围 0..100。";
                return false;
            }
            if (!InRange(martialArts))
            {
                error = "武艺超出允许范围 0..100。";
                return false;
            }
            if (!InRange(scholarship))
            {
                error = "学问超出允许范围 0..100。";
                return false;
            }
            if (!InRange(management))
            {
                error = "经营超出允许范围 0..100。";
                return false;
            }
            if (!InRange(social))
            {
                error = "社交超出允许范围 0..100。";
                return false;
            }

            values = new CharacterGrowthValues(ninjutsu, martialArts, scholarship, management, social);
            error = string.Empty;
            return true;
        }

        public static CharacterGrowthValues CreateClamped(int ninjutsu, int martialArts, int scholarship,
            int management, int social) =>
            new CharacterGrowthValues(Clamp(ninjutsu), Clamp(martialArts), Clamp(scholarship),
                Clamp(management), Clamp(social));

        public static bool IsDefined(GrowthAttribute attribute) =>
            Enum.IsDefined(typeof(GrowthAttribute), attribute);

        public int Get(GrowthAttribute attribute)
        {
            switch (attribute)
            {
                case GrowthAttribute.Ninjutsu: return Ninjutsu;
                case GrowthAttribute.MartialArts: return MartialArts;
                case GrowthAttribute.Scholarship: return Scholarship;
                case GrowthAttribute.Management: return Management;
                case GrowthAttribute.Social: return Social;
                default: throw new ArgumentOutOfRangeException(nameof(attribute));
            }
        }

        /// <summary>Returns a copy with one attribute adjusted and clamped to 0..100.</summary>
        public CharacterGrowthValues Apply(GrowthAttribute attribute, int delta)
        {
            if (!IsDefined(attribute))
                throw new ArgumentOutOfRangeException(nameof(attribute));
            switch (attribute)
            {
                case GrowthAttribute.Ninjutsu: return With(ninjutsu: Clamp(Ninjutsu + delta));
                case GrowthAttribute.MartialArts: return With(martialArts: Clamp(MartialArts + delta));
                case GrowthAttribute.Scholarship: return With(scholarship: Clamp(Scholarship + delta));
                case GrowthAttribute.Management: return With(management: Clamp(Management + delta));
                case GrowthAttribute.Social: return With(social: Clamp(Social + delta));
                default: throw new ArgumentOutOfRangeException(nameof(attribute));
            }
        }

        /// <summary>Applies the same delta to every attribute, clamping each independently.</summary>
        public CharacterGrowthValues ApplyToAll(int delta) =>
            new CharacterGrowthValues(Clamp(Ninjutsu + delta), Clamp(MartialArts + delta),
                Clamp(Scholarship + delta), Clamp(Management + delta), Clamp(Social + delta));

        private CharacterGrowthValues With(int? ninjutsu = null, int? martialArts = null,
            int? scholarship = null, int? management = null, int? social = null) =>
            new CharacterGrowthValues(ninjutsu ?? Ninjutsu, martialArts ?? MartialArts,
                scholarship ?? Scholarship, management ?? Management, social ?? Social);

        private static bool InRange(int value) => value >= Minimum && value <= Maximum;

        private static int Clamp(int value) => value < Minimum ? Minimum : value > Maximum ? Maximum : value;

        public bool Equals(CharacterGrowthValues other) =>
            Ninjutsu == other.Ninjutsu && MartialArts == other.MartialArts &&
            Scholarship == other.Scholarship && Management == other.Management && Social == other.Social;

        public override bool Equals(object obj) => obj is CharacterGrowthValues other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = Ninjutsu;
                hash = (hash * 397) ^ MartialArts;
                hash = (hash * 397) ^ Scholarship;
                hash = (hash * 397) ^ Management;
                hash = (hash * 397) ^ Social;
                return hash;
            }
        }

        public static bool operator ==(CharacterGrowthValues a, CharacterGrowthValues b) => a.Equals(b);
        public static bool operator !=(CharacterGrowthValues a, CharacterGrowthValues b) => !a.Equals(b);

        public override string ToString() =>
            "忍术 " + Ninjutsu.ToString(CultureInfo.InvariantCulture) +
            "／武艺 " + MartialArts.ToString(CultureInfo.InvariantCulture) +
            "／学问 " + Scholarship.ToString(CultureInfo.InvariantCulture) +
            "／经营 " + Management.ToString(CultureInfo.InvariantCulture) +
            "／社交 " + Social.ToString(CultureInfo.InvariantCulture);

        /// <summary>Compact "15/12/20/10/13" form used by previews and diagnostics.</summary>
        public string ToCompactString() =>
            Ninjutsu.ToString(CultureInfo.InvariantCulture) + "/" +
            MartialArts.ToString(CultureInfo.InvariantCulture) + "/" +
            Scholarship.ToString(CultureInfo.InvariantCulture) + "/" +
            Management.ToString(CultureInfo.InvariantCulture) + "/" +
            Social.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// One pending annual plan for a single (person, age year). Free growth requires an
    /// empty mentor; every directed plan requires a non-empty mentor ID. Whether that
    /// mentor is currently eligible is a session-level rule.
    /// </summary>
    public readonly struct EducationYearPlan
    {
        public const int MinimumAgeYear = 0;
        public const int MaximumAgeYear = 15;

        private EducationYearPlan(PersonId learner, int ageYear, EducationDirection direction,
            PersonId mentor)
        {
            Learner = learner;
            AgeYear = ageYear;
            Direction = direction;
            Mentor = mentor;
        }

        public PersonId Learner { get; }
        public int AgeYear { get; }
        public EducationDirection Direction { get; }
        /// <summary>Empty exactly when <see cref="Direction"/> is FreeGrowth.</summary>
        public PersonId Mentor { get; }

        public bool IsFreeGrowth => Direction == EducationDirection.FreeGrowth;

        public static bool TryCreate(PersonId learner, int ageYear, EducationDirection direction,
            PersonId mentor, out EducationYearPlan plan, out string error)
        {
            plan = default;
            if (learner.IsEmpty)
            {
                error = "培养对象 ID 为空。";
                return false;
            }
            if (ageYear < MinimumAgeYear || ageYear > MaximumAgeYear)
            {
                error = "培养年度必须在 0..15 岁之间。";
                return false;
            }
            if (!Enum.IsDefined(typeof(EducationDirection), direction))
            {
                error = "培养方向无效。";
                return false;
            }
            bool free = direction == EducationDirection.FreeGrowth;
            if (free && !mentor.IsEmpty)
            {
                error = "自由成长不能指定导师。";
                return false;
            }
            if (!free && mentor.IsEmpty)
            {
                error = "定向培养必须指定导师。";
                return false;
            }

            plan = new EducationYearPlan(learner, ageYear, direction, free ? default : mentor);
            error = string.Empty;
            return true;
        }

        public override string ToString() =>
            Learner.Value + "@" + AgeYear.ToString(CultureInfo.InvariantCulture) + ":" + Direction;
    }

    /// <summary>
    /// Immutable outcome of one settled age year. It carries the exact before/after
    /// quintets and a short Chinese reason; learner and age year are the state keys.
    /// </summary>
    public readonly struct EducationYearResult
    {
        public EducationYearResult(EducationDirection direction, PersonId mentor,
            CharacterGrowthValues before, CharacterGrowthValues after, string explanation,
            bool usedFreeGrowthFallback)
        {
            Direction = direction;
            Mentor = mentor;
            Before = before;
            After = after;
            Explanation = explanation ?? string.Empty;
            UsedFreeGrowthFallback = usedFreeGrowthFallback;
        }

        public EducationDirection Direction { get; }
        public PersonId Mentor { get; }
        public CharacterGrowthValues Before { get; }
        public CharacterGrowthValues After { get; }
        public string Explanation { get; }
        /// <summary>True when an invalid stored mentor was replaced by free growth at settlement.</summary>
        public bool UsedFreeGrowthFallback { get; }

        public CharacterGrowthValues Delta =>
            CharacterGrowthValues.CreateClamped(
                After.Ninjutsu - Before.Ninjutsu, After.MartialArts - Before.MartialArts,
                After.Scholarship - Before.Scholarship, After.Management - Before.Management,
                After.Social - Before.Social);

        public int DeltaOf(GrowthAttribute attribute)
        {
            if (!CharacterGrowthValues.IsDefined(attribute))
                throw new ArgumentOutOfRangeException(nameof(attribute));
            return After.Get(attribute) - Before.Get(attribute);
        }
    }

    /// <summary>
    /// Pure deterministic SC-020 growth rules. No random stream, Unity type or save type
    /// is read here; the fixed demonstration values come from the scenario layer.
    /// </summary>
    public static class CharacterGrowthRules
    {
        public const int DirectedBaseGain = 4;
        public const int AptitudeBonus = 2;
        public const int MentorBonus = 2;
        public const int SupportiveEnvironmentBonus = 1;
        public const int FreeGrowthGain = 1;

        public static bool TryGetAttribute(EducationDirection direction, out GrowthAttribute attribute)
        {
            switch (direction)
            {
                case EducationDirection.Ninjutsu: attribute = GrowthAttribute.Ninjutsu; return true;
                case EducationDirection.MartialArts: attribute = GrowthAttribute.MartialArts; return true;
                case EducationDirection.Scholarship: attribute = GrowthAttribute.Scholarship; return true;
                case EducationDirection.Management: attribute = GrowthAttribute.Management; return true;
                case EducationDirection.Social: attribute = GrowthAttribute.Social; return true;
                default:
                    attribute = default;
                    return false;
            }
        }

        public static bool TryGetAttribute(MentorSpecialty specialty, out GrowthAttribute attribute)
        {
            switch (specialty)
            {
                case MentorSpecialty.Ninjutsu: attribute = GrowthAttribute.Ninjutsu; return true;
                case MentorSpecialty.MartialArts: attribute = GrowthAttribute.MartialArts; return true;
                case MentorSpecialty.Scholarship: attribute = GrowthAttribute.Scholarship; return true;
                case MentorSpecialty.Management: attribute = GrowthAttribute.Management; return true;
                case MentorSpecialty.Social: attribute = GrowthAttribute.Social; return true;
                default:
                    attribute = default;
                    return false;
            }
        }

        /// <summary>
        /// Resolves one education year. Directed growth adds +4 to the matching attribute,
        /// +2 when the child's aptitude matches, +2 when the mentor's specialty matches and
        /// +1 in a supportive home; free growth adds +1 to every attribute. Values clamp
        /// to 0..100. Invalid enum values throw; callers with untrusted input use TryResolve.
        /// </summary>
        public static EducationYearResult Resolve(CharacterGrowthValues before,
            EducationDirection direction, GrowthAttribute childAptitude, MentorSpecialty mentor,
            bool supportiveEnvironment) =>
            Resolve(before, direction, true, childAptitude, mentor, supportiveEnvironment);

        /// <summary>
        /// Resolves one education year with an explicit aptitude-match flag. The fixed
        /// SC-020 child has a defined aptitude; other scenarios have none and pass false
        /// so the +2 aptitude bonus never applies by accident.
        /// </summary>
        public static EducationYearResult Resolve(CharacterGrowthValues before,
            EducationDirection direction, bool hasAptitude, GrowthAttribute childAptitude,
            MentorSpecialty mentor, bool supportiveEnvironment)
        {
            if (!Enum.IsDefined(typeof(EducationDirection), direction))
                throw new ArgumentOutOfRangeException(nameof(direction));
            if (hasAptitude && !CharacterGrowthValues.IsDefined(childAptitude))
                throw new ArgumentOutOfRangeException(nameof(childAptitude));
            if (!Enum.IsDefined(typeof(MentorSpecialty), mentor))
                throw new ArgumentOutOfRangeException(nameof(mentor));

            if (direction == EducationDirection.FreeGrowth)
            {
                CharacterGrowthValues freeAfter = before.ApplyToAll(FreeGrowthGain);
                return new EducationYearResult(direction, default, before, freeAfter,
                    "自由成长：五项能力各 +" + FreeGrowthGain.ToString(CultureInfo.InvariantCulture) + "。",
                    false);
            }

            TryGetAttribute(direction, out GrowthAttribute directionAttribute);
            int gain = DirectedBaseGain;
            var parts = new System.Text.StringBuilder(96);
            parts.Append(DirectionName(direction)).Append("培养：基础 +")
                .Append(DirectedBaseGain.ToString(CultureInfo.InvariantCulture));

            if (directionAttribute == childAptitude && hasAptitude)
            {
                gain += AptitudeBonus;
                parts.Append("，资质契合 +").Append(AptitudeBonus.ToString(CultureInfo.InvariantCulture));
            }

            if (TryGetAttribute(mentor, out GrowthAttribute mentorAttribute) &&
                mentorAttribute == directionAttribute)
            {
                gain += MentorBonus;
                parts.Append("，导师专长 +").Append(MentorBonus.ToString(CultureInfo.InvariantCulture));
            }

            if (supportiveEnvironment)
            {
                gain += SupportiveEnvironmentBonus;
                parts.Append("，家庭环境有利 +")
                    .Append(SupportiveEnvironmentBonus.ToString(CultureInfo.InvariantCulture));
            }

            parts.Append("，共 +").Append(gain.ToString(CultureInfo.InvariantCulture)).Append('。');

            CharacterGrowthValues after = before.Apply(directionAttribute, gain);
            return new EducationYearResult(direction, default, before, after, parts.ToString(), false);
        }

        /// <summary>Non-throwing wrapper used by application code and panels.</summary>
        public static bool TryResolve(CharacterGrowthValues before, EducationDirection direction,
            GrowthAttribute childAptitude, MentorSpecialty mentor, bool supportiveEnvironment,
            out EducationYearResult result, out string error) =>
            TryResolve(before, direction, true, childAptitude, mentor, supportiveEnvironment,
                out result, out error);

        /// <summary>Non-throwing overload with an explicit aptitude-match flag.</summary>
        public static bool TryResolve(CharacterGrowthValues before, EducationDirection direction,
            bool hasAptitude, GrowthAttribute childAptitude, MentorSpecialty mentor,
            bool supportiveEnvironment, out EducationYearResult result, out string error)
        {
            if (!Enum.IsDefined(typeof(EducationDirection), direction))
            {
                result = default;
                error = "培养方向无效。";
                return false;
            }
            if (hasAptitude && !CharacterGrowthValues.IsDefined(childAptitude))
            {
                result = default;
                error = "孩子资质无效。";
                return false;
            }
            if (!Enum.IsDefined(typeof(MentorSpecialty), mentor))
            {
                result = default;
                error = "导师专长无效。";
                return false;
            }

            result = Resolve(before, direction, hasAptitude, childAptitude, mentor,
                supportiveEnvironment);
            error = string.Empty;
            return true;
        }

        public static string DirectionName(EducationDirection direction)
        {
            switch (direction)
            {
                case EducationDirection.Ninjutsu: return "忍术";
                case EducationDirection.MartialArts: return "武艺";
                case EducationDirection.Scholarship: return "学问";
                case EducationDirection.Management: return "经营";
                case EducationDirection.Social: return "社交";
                case EducationDirection.FreeGrowth: return "自由成长";
                default: return direction.ToString();
            }
        }

        public static string AttributeName(GrowthAttribute attribute)
        {
            switch (attribute)
            {
                case GrowthAttribute.Ninjutsu: return "忍术";
                case GrowthAttribute.MartialArts: return "武艺";
                case GrowthAttribute.Scholarship: return "学问";
                case GrowthAttribute.Management: return "经营";
                case GrowthAttribute.Social: return "社交";
                default: return attribute.ToString();
            }
        }

        public static string SpecialtyName(MentorSpecialty specialty)
        {
            switch (specialty)
            {
                case MentorSpecialty.Ninjutsu: return "忍术";
                case MentorSpecialty.MartialArts: return "武艺";
                case MentorSpecialty.Scholarship: return "学问";
                case MentorSpecialty.Management: return "经营";
                case MentorSpecialty.Social: return "社交";
                case MentorSpecialty.None: return "无";
                default: return specialty.ToString();
            }
        }
    }
}
