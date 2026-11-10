using System;
using System.Collections.Generic;
using System.Globalization;
using SengokuChronicle.Domain;

namespace SengokuChronicle.Application
{
    /// <summary>Stable result of one newborn identity request.</summary>
    public readonly struct FamilyBirthName
    {
        private FamilyBirthName(bool succeeded, PersonId personId, string displayName,
            string error)
        {
            Succeeded = succeeded;
            PersonId = personId;
            DisplayName = displayName ?? string.Empty;
            Error = error ?? string.Empty;
        }

        public bool Succeeded { get; }
        public PersonId PersonId { get; }
        public string DisplayName { get; }
        public string Error { get; }

        public static FamilyBirthName Failure(string error) =>
            new FamilyBirthName(false, default, string.Empty, error);

        public static FamilyBirthName Success(PersonId personId, string displayName) =>
            new FamilyBirthName(true, personId, displayName, string.Empty);
    }

    /// <summary>
    /// Stable newborn identity generation. IDs are derived from the ordinal-sorted
    /// parent pair and the settlement date anchor, and display names come from original
    /// male/female short-name tables. The same family/date inputs always produce the
    /// same ID and base name; a numeric suffix is added only when that name is already
    /// in use, so IDs and display names never collide with registered people.
    /// </summary>
    public static class FamilyBirthNaming
    {
        private static readonly string[] MaleGivenNames =
        {
            "宗太郎", "新之助", "隼人", "清十郎", "政之", "頼光", "小次郎", "又兵衛",
            "藤吉", "半蔵", "内記", "与四郎"
        };

        private static readonly string[] FemaleGivenNames =
        {
            "千代", "阿初", "佐和", "小春", "千鹤", "美弥", "阿绫", "铃",
            "奈绪", "志乃", "多加", "小夜"
        };

        /// <summary>
        /// Creates one newborn identity from the ordinal-sorted parent pair and a stable
        /// date anchor. Monthly birth planning passes the conception date so the identity
        /// does not depend on a later due-date recalculation; standalone callers may pass
        /// the birth date. Only <see cref="PersonGender.Male"/> and
        /// <see cref="PersonGender.Female"/> are accepted.
        /// </summary>
        public static FamilyBirthName Create(SuccessionState state, PersonId parentA,
            PersonId parentB, GameDate birthDate, PersonGender gender) =>
            Create(state, parentA, parentB, birthDate, gender, null, null);

        internal static FamilyBirthName Create(SuccessionState state, PersonId parentA,
            PersonId parentB, GameDate birthDate, PersonGender gender,
            ICollection<string> reservedIds, ICollection<string> reservedNames)
        {
            if (state == null || state.World == null)
                return FamilyBirthName.Failure("家族状态为空，无法为新生儿命名。");
            if (parentA.IsEmpty || parentB.IsEmpty)
                return FamilyBirthName.Failure("新生儿父母 ID 不能为空。");
            if (parentA == parentB)
                return FamilyBirthName.Failure("新生儿父母不能是同一人物。");
            if (!state.World.TryGetPerson(parentA, out _) ||
                !state.World.TryGetPerson(parentB, out _))
                return FamilyBirthName.Failure("新生儿父母不是已登记人物。");
            if (!birthDate.IsValid)
                return FamilyBirthName.Failure("新生儿出生日期无效。");
            if (gender != PersonGender.Male && gender != PersonGender.Female)
                return FamilyBirthName.Failure("新生儿性别必须是男或女。");

            string first = parentA.Value ?? string.Empty;
            string second = parentB.Value ?? string.Empty;
            if (string.CompareOrdinal(first, second) > 0)
            {
                string swap = first;
                first = second;
                second = swap;
            }

            string baseId = "person.birth." + Sanitize(first) + "." + Sanitize(second) + "." +
                            birthDate.Year.ToString("D4", CultureInfo.InvariantCulture) +
                            birthDate.Month.ToString("D2", CultureInfo.InvariantCulture) +
                            birthDate.Day.ToString("D2", CultureInfo.InvariantCulture);
            string personId = UniqueId(state, baseId, reservedIds);

            string[] table = gender == PersonGender.Male ? MaleGivenNames : FemaleGivenNames;
            int index = (int)(StableHash(personId) % (uint)table.Length);
            string displayName = UniqueName(state, table[index], reservedNames);

            return FamilyBirthName.Success(new PersonId(personId), displayName);
        }

        private static string UniqueId(SuccessionState state, string baseId,
            ICollection<string> reservedIds)
        {
            string candidate = baseId;
            int suffix = 2;
            while (state.World.TryGetPerson(new PersonId(candidate), out _) ||
                   (reservedIds != null && reservedIds.Contains(candidate)))
            {
                candidate = baseId + "." + suffix.ToString(CultureInfo.InvariantCulture);
                suffix++;
            }
            if (reservedIds != null)
                reservedIds.Add(candidate);
            return candidate;
        }

        private static string UniqueName(SuccessionState state, string baseName,
            ICollection<string> reservedNames)
        {
            var used = new HashSet<string>(StringComparer.Ordinal);
            foreach (PersonState person in state.World.Persons)
                used.Add(person.DisplayName ?? string.Empty);
            if (reservedNames != null)
                foreach (string reserved in reservedNames)
                    used.Add(reserved ?? string.Empty);

            string candidate = baseName;
            int suffix = 2;
            while (used.Contains(candidate))
            {
                candidate = baseName + " " +
                            suffix.ToString(CultureInfo.InvariantCulture);
                suffix++;
            }
            if (reservedNames != null)
                reservedNames.Add(candidate);
            return candidate;
        }

        private static string Sanitize(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "unknown";
            var builder = new System.Text.StringBuilder(value.Length);
            foreach (char character in value)
            {
                if (char.IsLetterOrDigit(character) || character == '-' || character == '_')
                    builder.Append(character);
                else
                    builder.Append('-');
            }
            return builder.ToString();
        }

        private static uint StableHash(string value)
        {
            // FNV-1a: intentionally independent of the save seed so a replayed or
            // reloaded settlement names the same newborn identically.
            uint hash = 2166136261u;
            if (value != null)
            {
                foreach (char character in value)
                {
                    hash ^= character;
                    hash *= 16777619u;
                }
            }
            return hash;
        }
    }
}
