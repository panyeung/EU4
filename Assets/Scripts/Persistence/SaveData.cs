using System;
using System.Collections.Generic;
using SengokuChronicle.Domain;

namespace SengokuChronicle.Persistence
{
    /// <summary>
    /// Flat, versioned root of the single Phase 1 save slot.
    /// Contains only plain data and stable IDs; never Unity objects or asset references.
    /// </summary>
    [Serializable]
    public sealed class SaveData
    {
        /// <summary>Single schema-version source; mirrors <see cref="SaveDataValidator.CurrentVersion"/>.</summary>
        public const int CurrentGameSchemaVersion = SaveDataValidator.CurrentVersion;

        /// <summary>Rules contract version shared by capture, validation and migration.</summary>
        public const int CurrentRulesVersion = 1;

        /// <summary>
        /// Fallback event-content version used only when no catalog is loaded. The
        /// active catalog's <c>contentVersion</c> is authoritative for capture/load.
        /// </summary>
        public const int CurrentEventContentVersion = 2;

        /// <summary>Displayed game build version. Older values are not rejected on load.</summary>
        public const string CurrentGameVersion = "0.1.0";

        public int saveFormatVersion;
        public string gameVersion;
        public int rulesVersion;
        public int eventContentVersion;
        public int year;
        public int month;
        public int day;
        public string locationId;
        public int supplyShortageStage;
        public int sealedLetterStage;
        public int marketTallyStage;
        public RelationshipSaveEntry[] relationships;
        public RandomStateSaveData randomState;
        public EventRuntimeSaveData eventRuntime;
        public SuccessionSaveData succession;
        public ChronicleSaveEntry[] chronicle;

        public SaveData()
        {
            saveFormatVersion = SaveDataValidator.CurrentVersion;
            gameVersion = CurrentGameVersion;
            rulesVersion = CurrentRulesVersion;
            eventContentVersion = CurrentEventContentVersion;
            randomState = RandomStateManager.Create(0UL).CaptureState();
            eventRuntime = new EventRuntimeSaveData();
            succession = SuccessionSaveData.CreateDefault();
            chronicle = new ChronicleSaveEntry[0];
        }
    }

    /// <summary>
    /// Presence probe parsed from raw save JSON before <see cref="SaveData"/>.
    /// Unity's <c>JsonUtility.FromJson</c> runs the parameterless constructor and
    /// field initializers, so a v3 file that omits a field would otherwise be
    /// indistinguishable from that field's constructor default. Scalar fields here
    /// stay zero/empty when absent. Nested <c>[Serializable]</c> classes are still
    /// materialized by JsonUtility even when their property is absent, so callers
    /// must pair this probe with a root-property check on the raw JSON.
    /// </summary>
    [Serializable]
    public sealed class SaveDataPresenceProbe
    {
        public int saveFormatVersion;
        public string gameVersion;
        public int rulesVersion;
        public int eventContentVersion;
        public RandomStateSaveData randomState;
        public EventRuntimeSaveData eventRuntime;
        public SuccessionSaveData succession;
        public ChronicleSaveEntry[] chronicle;
    }

    /// <summary>One player-to-NPC relationship value, keyed by stable character ID.</summary>
    [Serializable]
    public sealed class RelationshipSaveEntry
    {
        public string characterId;
        public int value;
    }

    /// <summary>Stable phases in which a save is allowed.</summary>
    public enum StableGamePhase
    {
        Management = 0,
        AwaitingEventChoice = 1
    }

    /// <summary>Version-neutral persistence DTO for deterministic event runtime state.</summary>
    [Serializable]
    public sealed class EventRuntimeSaveData
    {
        public int phase;
        public string[] pendingEventIds;
        public EventCooldownSaveEntry[] cooldowns;
        public string[] onceEventIds;
        public EventHistorySaveEntry[] history;
        public string[] committedResultIds;

        public EventRuntimeSaveData()
        {
            phase = (int)StableGamePhase.Management;
            pendingEventIds = new string[0];
            cooldowns = new EventCooldownSaveEntry[0];
            onceEventIds = new string[0];
            history = new EventHistorySaveEntry[0];
            committedResultIds = new string[0];
        }
    }

    [Serializable]
    public sealed class EventCooldownSaveEntry
    {
        public string eventId;
        public int availableAtMonth;
    }

    [Serializable]
    public sealed class EventHistorySaveEntry
    {
        public string eventId;
        public string optionId;
        public string resultId;
        public int year;
        public int month;
        public string summary;
    }

    /// <summary>
    /// Version-neutral SC-010 succession snapshot. It stores only stable IDs and
    /// plain data; the live SC-003 types are rebuilt by <see cref="TryRestore"/>.
    /// </summary>
    [Serializable]
    public sealed class SuccessionSaveData
    {
        public int phase;
        public string familyId;
        public string familyName;
        public string headPersonId;
        public string designatedHeirId;
        public string pendingSuccessorId;
        public string pendingRegentId;
        public int adulthoodAge;
        public int successionCount;
        public string completedResultId;
        public bool crisisAdoptionUsed;
        public bool regencyCompleted;
        public SuccessionPersonSaveEntry[] persons;
        public SuccessionMemberSaveEntry[] members;
        public SuccessionKinshipSaveEntry[] kinship;

        /// <summary>Default roster used by a brand-new session and by v1/v2 migration.</summary>
        public static SuccessionSaveData CreateDefault() => Capture(SuccessionSample.CreateStandard());

        public static SuccessionSaveData Capture(SuccessionState state)
        {
            if (state == null)
                return null;

            var persons = new List<SuccessionPersonSaveEntry>();
            foreach (PersonState person in state.World.Persons)
            {
                state.World.TryGetLife(person.Id, out SuccessionPersonLife life);
                persons.Add(new SuccessionPersonSaveEntry
                {
                    personId = person.Id.Value,
                    displayName = person.DisplayName,
                    birthYear = life.BirthYear,
                    alive = life.Alive,
                    retired = life.Retired
                });
            }

            var members = new List<SuccessionMemberSaveEntry>();
            foreach (FamilyMembership membership in state.World.Family.Members)
                members.Add(new SuccessionMemberSaveEntry
                {
                    personId = membership.PersonId.Value,
                    role = (int)membership.Role
                });

            var kinship = new List<SuccessionKinshipSaveEntry>();
            foreach (KinshipLink link in state.World.Kinship.Links)
                kinship.Add(new SuccessionKinshipSaveEntry
                {
                    from = link.From.Value,
                    to = link.To.Value,
                    type = (int)link.Type
                });
            kinship.Sort(CompareKinshipEntries);

            return new SuccessionSaveData
            {
                phase = (int)state.Phase,
                familyId = state.World.Family.Id.Value,
                familyName = state.World.Family.DisplayName,
                headPersonId = state.HeadPersonId.Value,
                designatedHeirId = state.DesignatedHeirId.Value,
                pendingSuccessorId = state.PendingSuccessorId.Value,
                pendingRegentId = state.PendingRegentId.Value,
                adulthoodAge = state.AdulthoodAge,
                successionCount = state.SuccessionCount,
                completedResultId = state.CompletedResultId ?? string.Empty,
                crisisAdoptionUsed = state.CrisisAdoptionUsed,
                regencyCompleted = state.RegencyCompleted,
                persons = persons.ToArray(),
                members = members.ToArray(),
                kinship = kinship.ToArray()
            };
        }

        public SuccessionSaveData Clone()
        {
            var clone = new SuccessionSaveData
            {
                phase = phase,
                familyId = familyId,
                familyName = familyName,
                headPersonId = headPersonId,
                designatedHeirId = designatedHeirId,
                pendingSuccessorId = pendingSuccessorId,
                pendingRegentId = pendingRegentId,
                adulthoodAge = adulthoodAge,
                successionCount = successionCount,
                completedResultId = completedResultId,
                crisisAdoptionUsed = crisisAdoptionUsed,
                regencyCompleted = regencyCompleted
            };
            clone.persons = ClonePersons(persons);
            clone.members = CloneMembers(members);
            clone.kinship = CloneKinship(kinship);
            return clone;
        }

        /// <summary>
        /// Structural and cross-reference validation for untrusted v3 save data.
        /// Returns the first problem; never throws.
        /// </summary>
        public SaveOperationResult ValidateForSave(GameDate date)
        {
            if (!Enum.IsDefined(typeof(SuccessionPhase), phase))
                return Invalid("存档继承阶段无效。");
            if (!FamilyId.TryParse(familyId, out _))
                return Invalid("存档继承家族 ID 无效。");
            if (string.IsNullOrWhiteSpace(familyName))
                return Invalid("存档继承家族名称为空。");
            if (adulthoodAge < 14 || adulthoodAge > 25)
                return Invalid("存档成年年龄阈值无效。");
            if (successionCount < 0)
                return Invalid("存档继承次数无效。");
            if (persons == null || persons.Length == 0)
                return Invalid("存档继承人物列表为空。");

            var personMap = new Dictionary<PersonId, SuccessionPersonSaveEntry>();
            string previousPersonId = null;
            foreach (SuccessionPersonSaveEntry entry in persons)
            {
                if (entry == null)
                    return Invalid("存档继承人物记录为空。");
                if (!PersonId.TryParse(entry.personId, out PersonId personId))
                    return Invalid("存档继承人物 ID 无效。");
                if (personMap.ContainsKey(personId))
                    return Invalid("存档继承人物 ID 重复。");
                if (previousPersonId != null &&
                    string.CompareOrdinal(previousPersonId, entry.personId) > 0)
                    return Invalid("存档继承人物顺序不稳定。");
                previousPersonId = entry.personId;
                if (string.IsNullOrWhiteSpace(entry.displayName))
                    return Invalid("存档继承人物名称为空。");
                if (entry.birthYear < 1400 || entry.birthYear > date.Year)
                    return Invalid("存档继承人物出生年份无效。");
                personMap.Add(personId, entry);
            }

            if (members == null || members.Length == 0)
                return Invalid("存档继承家族成员列表为空。");
            var roleMap = new Dictionary<PersonId, FamilyMemberRole>();
            string previousMemberId = null;
            foreach (SuccessionMemberSaveEntry entry in members)
            {
                if (entry == null)
                    return Invalid("存档继承成员记录为空。");
                if (!PersonId.TryParse(entry.personId, out PersonId personId) ||
                    !personMap.ContainsKey(personId))
                    return Invalid("存档继承成员不是已登记人物。");
                if (!Enum.IsDefined(typeof(FamilyMemberRole), entry.role))
                    return Invalid("存档继承成员角色无效。");
                if (roleMap.ContainsKey(personId))
                    return Invalid("存档继承成员重复。");
                if (previousMemberId != null &&
                    string.CompareOrdinal(previousMemberId, entry.personId) > 0)
                    return Invalid("存档继承成员顺序不稳定。");
                previousMemberId = entry.personId;
                roleMap.Add(personId, (FamilyMemberRole)entry.role);
            }

            int headCount = 0;
            foreach (FamilyMemberRole role in roleMap.Values)
                if (role == FamilyMemberRole.Head)
                    headCount++;
            if (headCount != 1)
                return Invalid("存档继承家族必须恰好有一名 Head。");

            if (!PersonId.TryParse(headPersonId, out PersonId headId) ||
                !roleMap.TryGetValue(headId, out FamilyMemberRole headRole) ||
                headRole != FamilyMemberRole.Head)
                return Invalid("存档继承家主 ID 与 Head 身份不一致。");
            if (!personMap.TryGetValue(headId, out SuccessionPersonSaveEntry headEntry) ||
                !headEntry.alive || headEntry.retired)
                return Invalid("存档继承家主必须是存活且未退休的人物。");

            PersonId designated = default;
            if (!TryParseOptional(designatedHeirId, out designated, personMap))
                return Invalid("存档指定继承人 ID 无效。");
            PersonId pendingSuccessor = default;
            if (!TryParseOptional(pendingSuccessorId, out pendingSuccessor, personMap))
                return Invalid("存档待定继承人 ID 无效。");
            PersonId pendingRegent = default;
            if (!TryParseOptional(pendingRegentId, out pendingRegent, personMap))
                return Invalid("存档待定摄政 ID 无效。");

            SuccessionPhase parsedPhase = (SuccessionPhase)phase;
            if (parsedPhase == SuccessionPhase.AwaitingSuccessionChoice)
            {
                if (!pendingRegent.IsEmpty)
                    return Invalid("存档继承选择阶段不应有摄政。");
                if (!pendingSuccessor.IsEmpty &&
                    !IsValidCandidate(personMap, roleMap, pendingSuccessor, headId, adulthoodAge,
                        date, requireMinor: false))
                    return Invalid("存档待定继承人无效。");
            }
            else if (parsedPhase == SuccessionPhase.AwaitingRegentChoice)
            {
                if (pendingSuccessor.IsEmpty ||
                    !IsValidCandidate(personMap, roleMap, pendingSuccessor, headId, adulthoodAge,
                        date, requireMinor: true))
                    return Invalid("存档待定未成年继承人无效。");
                if (!pendingRegent.IsEmpty &&
                    !IsValidRegent(personMap, roleMap, pendingRegent, headId, pendingSuccessor,
                        adulthoodAge, date))
                    return Invalid("存档待定摄政无效。");
            }
            else if (parsedPhase == SuccessionPhase.Regency)
            {
                if (!pendingSuccessor.IsEmpty)
                    return Invalid("存档摄政阶段不应有待定继承人。");
                if (pendingRegent.IsEmpty ||
                    !IsValidRegent(personMap, roleMap, pendingRegent, headId, default,
                        adulthoodAge, date))
                    return Invalid("存档摄政阶段缺少合资格摄政。");
            }
            else if (parsedPhase == SuccessionPhase.CrisisDecision)
            {
                if (!pendingSuccessor.IsEmpty || !pendingRegent.IsEmpty)
                    return Invalid("存档危机阶段不应有待定人选。");
                if (crisisAdoptionUsed)
                    return Invalid("存档危机阶段与已使用标记冲突。");
            }
            else if (parsedPhase == SuccessionPhase.None || parsedPhase == SuccessionPhase.Extinct)
            {
                if (!pendingSuccessor.IsEmpty || !pendingRegent.IsEmpty)
                    return Invalid("存档稳定阶段不应有待定人选。");
            }

            if (kinship == null)
                return Invalid("存档继承亲属关系列表为空。");
            var seenKinship = new HashSet<string>(StringComparer.Ordinal);
            SuccessionKinshipSaveEntry previousKinship = null;
            foreach (SuccessionKinshipSaveEntry entry in kinship)
            {
                if (entry == null)
                    return Invalid("存档继承亲属记录为空。");
                if (!PersonId.TryParse(entry.from, out PersonId from) ||
                    !PersonId.TryParse(entry.to, out PersonId to) ||
                    from == to)
                    return Invalid("存档继承亲属端点无效。");
                if (!personMap.ContainsKey(from) || !personMap.ContainsKey(to))
                    return Invalid("存档继承亲属端点不是已登记人物。");
                if (!Enum.IsDefined(typeof(KinshipType), entry.type))
                    return Invalid("存档继承亲属类型无效。");
                var link = new KinshipLink(from, to, (KinshipType)entry.type);
                string normalized = NormalizeKinshipKey(link);
                if (!seenKinship.Add(normalized))
                    return Invalid("存档继承亲属关系重复。");
                if (previousKinship != null && CompareKinshipEntries(previousKinship, entry) > 0)
                    return Invalid("存档继承亲属顺序不稳定。");
                previousKinship = entry;
            }

            return SaveOperationResult.Success();
        }

        /// <summary>
        /// Rebuilds a validated domain snapshot. The caller must not assign the
        /// result until this method succeeds, preserving atomic load semantics.
        /// </summary>
        public static bool TryRestore(SuccessionSaveData data, GameDate date,
            out SuccessionState state, out string error)
        {
            state = null;
            if (data == null)
            {
                error = "存档继承状态为空。";
                return false;
            }

            SaveOperationResult validation = data.ValidateForSave(date);
            if (!validation.Succeeded)
            {
                error = validation.Message;
                return false;
            }

            if (!FamilyId.TryParse(data.familyId, out FamilyId familyId))
            {
                error = "存档继承家族 ID 无效。";
                return false;
            }

            var world = new SuccessionWorld(familyId, data.familyName);
            foreach (SuccessionPersonSaveEntry entry in data.persons)
            {
                PersonId.TryParse(entry.personId, out PersonId personId);
                if (!world.TryAddPerson(new PersonState(personId, entry.displayName),
                        new SuccessionPersonLife(entry.birthYear, entry.alive, entry.retired),
                        out error))
                    return false;
            }
            foreach (SuccessionMemberSaveEntry entry in data.members)
            {
                PersonId.TryParse(entry.personId, out PersonId personId);
                if (!world.TryAddMember(personId, (FamilyMemberRole)entry.role, out error))
                    return false;
            }
            foreach (SuccessionKinshipSaveEntry entry in data.kinship)
            {
                PersonId.TryParse(entry.from, out PersonId from);
                PersonId.TryParse(entry.to, out PersonId to);
                if (!KinshipLink.TryCreate(from, to, (KinshipType)entry.type,
                        out KinshipLink link, out error) ||
                    !world.TryAddKinship(link, out error))
                    return false;
            }

            TryParseOptional(data.headPersonId, out PersonId headId, null);
            TryParseOptional(data.designatedHeirId, out PersonId designated, null);
            TryParseOptional(data.pendingSuccessorId, out PersonId pendingSuccessor, null);
            TryParseOptional(data.pendingRegentId, out PersonId pendingRegent, null);
            state = new SuccessionState(world)
            {
                Phase = (SuccessionPhase)data.phase,
                HeadPersonId = headId,
                DesignatedHeirId = designated,
                PendingSuccessorId = pendingSuccessor,
                PendingRegentId = pendingRegent,
                AdulthoodAge = data.adulthoodAge,
                SuccessionCount = data.successionCount,
                CompletedResultId = data.completedResultId ?? string.Empty,
                CrisisAdoptionUsed = data.crisisAdoptionUsed,
                RegencyCompleted = data.regencyCompleted
            };
            error = string.Empty;
            return true;
        }

        private static bool TryParseOptional(string value, out PersonId personId,
            Dictionary<PersonId, SuccessionPersonSaveEntry> known)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                personId = default;
                return true;
            }
            if (!PersonId.TryParse(value, out personId))
                return false;
            return known == null || known.ContainsKey(personId);
        }

        private static bool IsValidCandidate(Dictionary<PersonId, SuccessionPersonSaveEntry> persons,
            Dictionary<PersonId, FamilyMemberRole> roles, PersonId personId, PersonId headId,
            int adulthoodAge, GameDate date, bool requireMinor)
        {
            if (personId == headId || !persons.TryGetValue(personId, out SuccessionPersonSaveEntry entry) ||
                !roles.TryGetValue(personId, out FamilyMemberRole role))
                return false;
            if (!entry.alive || entry.retired || !SuccessionService.IsSuccessorRole(role))
                return false;
            int age = date.Year - entry.birthYear;
            return requireMinor ? age < adulthoodAge : age >= adulthoodAge;
        }

        private static bool IsValidRegent(Dictionary<PersonId, SuccessionPersonSaveEntry> persons,
            Dictionary<PersonId, FamilyMemberRole> roles, PersonId personId, PersonId headId,
            PersonId pendingSuccessor, int adulthoodAge, GameDate date)
        {
            if (personId == headId || personId == pendingSuccessor ||
                !persons.TryGetValue(personId, out SuccessionPersonSaveEntry entry) ||
                !roles.TryGetValue(personId, out FamilyMemberRole role))
                return false;
            if (!entry.alive || entry.retired || !SuccessionService.IsRegentRole(role))
                return false;
            return date.Year - entry.birthYear >= adulthoodAge;
        }

        /// <summary>
        /// The single ordinal kinship ordering rule shared by capture and validation.
        /// Tuple order (from, then to, then type) is unambiguous even when one person
        /// ID is a textual prefix of another.
        /// </summary>
        internal static int CompareKinshipKeys(string fromA, string toA, int typeA,
            string fromB, string toB, int typeB)
        {
            int byFrom = string.CompareOrdinal(fromA ?? string.Empty, fromB ?? string.Empty);
            if (byFrom != 0)
                return byFrom;
            int byTo = string.CompareOrdinal(toA ?? string.Empty, toB ?? string.Empty);
            if (byTo != 0)
                return byTo;
            return typeA.CompareTo(typeB);
        }

        internal static int CompareKinshipEntries(SuccessionKinshipSaveEntry a,
            SuccessionKinshipSaveEntry b) =>
            CompareKinshipKeys(a?.from, a?.to, a?.type ?? 0, b?.from, b?.to, b?.type ?? 0);

        private static string NormalizeKinshipKey(KinshipLink link)
        {
            string from = link.From.Value ?? string.Empty;
            string to = link.To.Value ?? string.Empty;
            if (link.IsSymmetric && string.CompareOrdinal(from, to) > 0)
            {
                string swap = from;
                from = to;
                to = swap;
            }

            // Length prefixes keep the duplicate key unambiguous for IDs containing
            // separator-like characters (for example "a|b").
            return from.Length.ToString(System.Globalization.CultureInfo.InvariantCulture) + ":" +
                   from + to.Length.ToString(System.Globalization.CultureInfo.InvariantCulture) + ":" +
                   to + ((int)link.Type).ToString(
                       "D3", System.Globalization.CultureInfo.InvariantCulture);
        }

        private static SuccessionPersonSaveEntry[] ClonePersons(SuccessionPersonSaveEntry[] source)
        {
            if (source == null) return null;
            var clone = new SuccessionPersonSaveEntry[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                SuccessionPersonSaveEntry entry = source[i];
                clone[i] = entry == null ? null : new SuccessionPersonSaveEntry
                {
                    personId = entry.personId,
                    displayName = entry.displayName,
                    birthYear = entry.birthYear,
                    alive = entry.alive,
                    retired = entry.retired
                };
            }
            return clone;
        }

        private static SuccessionMemberSaveEntry[] CloneMembers(SuccessionMemberSaveEntry[] source)
        {
            if (source == null) return null;
            var clone = new SuccessionMemberSaveEntry[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                SuccessionMemberSaveEntry entry = source[i];
                clone[i] = entry == null ? null : new SuccessionMemberSaveEntry
                {
                    personId = entry.personId,
                    role = entry.role
                };
            }
            return clone;
        }

        private static SuccessionKinshipSaveEntry[] CloneKinship(SuccessionKinshipSaveEntry[] source)
        {
            if (source == null) return null;
            var clone = new SuccessionKinshipSaveEntry[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                SuccessionKinshipSaveEntry entry = source[i];
                clone[i] = entry == null ? null : new SuccessionKinshipSaveEntry
                {
                    from = entry.from,
                    to = entry.to,
                    type = entry.type
                };
            }
            return clone;
        }

        private static SaveOperationResult Invalid(string message) =>
            SaveOperationResult.Failure(SaveErrorCode.InvalidData, message);
    }

    [Serializable]
    public sealed class SuccessionPersonSaveEntry
    {
        public string personId;
        public string displayName;
        public int birthYear;
        public bool alive;
        public bool retired;
    }

    [Serializable]
    public sealed class SuccessionMemberSaveEntry
    {
        public string personId;
        public int role;
    }

    [Serializable]
    public sealed class SuccessionKinshipSaveEntry
    {
        public string from;
        public string to;
        public int type;
    }

    /// <summary>
    /// Plain-data v4 chronicle record. Field names and values are the single save
    /// format; the runtime <see cref="HistoryLedger"/> rebuild is owned by
    /// <see cref="ChronicleSaveCodec"/>.
    /// </summary>
    [Serializable]
    public sealed class ChronicleSaveEntry
    {
        public string entryId;
        public int sequence;
        public int year;
        public int month;
        public int category;
        public string subjectId;
        public string sourceId;
        public string title;
        public string summary;
        public int fromValue;
        public int toValue;
        public int fromStage;
        public int toStage;
        public int fromPhase;
        public int toPhase;
        public string fromHeadId;
        public string toHeadId;

        public static ChronicleSaveEntry FromEntry(FamilyChronicleEntry entry)
        {
            if (entry == null)
                return null;

            return new ChronicleSaveEntry
            {
                entryId = entry.EntryId,
                sequence = entry.Sequence,
                year = entry.Year,
                month = entry.Month,
                category = (int)entry.Category,
                subjectId = entry.SubjectId,
                sourceId = entry.SourceId,
                title = entry.Title,
                summary = entry.Summary,
                fromValue = entry.FromValue,
                toValue = entry.ToValue,
                fromStage = entry.FromStage,
                toStage = entry.ToStage,
                fromPhase = entry.FromPhase,
                toPhase = entry.ToPhase,
                fromHeadId = entry.FromHeadId,
                toHeadId = entry.ToHeadId
            };
        }
    }

    /// <summary>
    /// Maps the chronicle persistence DTO to and from the immutable domain ledger.
    /// Restore is atomic: a malformed or duplicate record yields no ledger at all.
    /// </summary>
    public static class ChronicleSaveCodec
    {
        public static ChronicleSaveEntry[] Capture(HistoryLedger ledger)
        {
            if (ledger == null || ledger.Count == 0)
                return new ChronicleSaveEntry[0];

            var captured = new ChronicleSaveEntry[ledger.Count];
            for (int i = 0; i < ledger.Count; i++)
                captured[i] = ChronicleSaveEntry.FromEntry(ledger.Entries[i]);
            return captured;
        }

        public static bool TryRestore(ChronicleSaveEntry[] saved, out HistoryLedger ledger,
            out string error)
        {
            ledger = null;
            error = string.Empty;
            if (saved == null || saved.Length == 0)
            {
                ledger = HistoryLedger.CreateEmpty();
                return true;
            }

            var entries = new FamilyChronicleEntry[saved.Length];
            for (int i = 0; i < saved.Length; i++)
            {
                ChronicleSaveEntry dto = saved[i];
                if (dto == null)
                {
                    error = "存档编年史记录为空。";
                    return false;
                }
                if (!FamilyChronicleEntry.TryCreate(dto.sequence, dto.entryId, dto.year, dto.month,
                        (ChronicleCategory)dto.category, dto.subjectId, dto.sourceId, dto.title,
                        dto.summary, dto.fromValue, dto.toValue, dto.fromStage, dto.toStage,
                        dto.fromPhase, dto.toPhase, dto.fromHeadId, dto.toHeadId,
                        out entries[i], out error))
                    return false;
            }

            return HistoryLedger.TryRestore(entries, out ledger, out error);
        }

        public static ChronicleSaveEntry[] Clone(ChronicleSaveEntry[] source)
        {
            if (source == null) return null;
            var clone = new ChronicleSaveEntry[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                ChronicleSaveEntry entry = source[i];
                clone[i] = entry == null ? null : new ChronicleSaveEntry
                {
                    entryId = entry.entryId,
                    sequence = entry.sequence,
                    year = entry.year,
                    month = entry.month,
                    category = entry.category,
                    subjectId = entry.subjectId,
                    sourceId = entry.sourceId,
                    title = entry.title,
                    summary = entry.summary,
                    fromValue = entry.fromValue,
                    toValue = entry.toValue,
                    fromStage = entry.fromStage,
                    toStage = entry.toStage,
                    fromPhase = entry.fromPhase,
                    toPhase = entry.toPhase,
                    fromHeadId = entry.fromHeadId,
                    toHeadId = entry.toHeadId
                };
            }
            return clone;
        }
    }
}
