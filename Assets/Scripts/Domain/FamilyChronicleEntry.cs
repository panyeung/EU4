using System;
using System.Globalization;

namespace SengokuChronicle.Domain
{
    /// <summary>Stable categories of one family chronicle record.</summary>
    public enum ChronicleCategory
    {
        /// <summary>A committed player event choice.</summary>
        Event = 0,
        /// <summary>One real player-to-NPC relationship value change.</summary>
        Relationship = 1,
        /// <summary>One successful mission stage change.</summary>
        Mission = 2,
        /// <summary>One successful succession state change.</summary>
        Succession = 3,
        /// <summary>One monthly industry settlement or succession-driven industry personnel change.</summary>
        Industry = 4,
        /// <summary>One settled annual child-education year.</summary>
        Education = 5,
        /// <summary>One completed family research project recorded as family knowledge.</summary>
        Research = 6,
        /// <summary>One committed marriage or ordinary adoption relationship.</summary>
        FamilyFormation = 7,
        /// <summary>One annual natural death of a living family member (M08A).</summary>
        NaturalDeath = 8
    }

    /// <summary>
    /// Immutable SC-012 chronicle record. Domain only: no Unity, uGUI or persistence
    /// references. The date precision is year/month because legacy v3 event history
    /// never stored a day. Category-specific values reuse one pair of fields:
    /// relationship values use <see cref="FromValue"/>/<see cref="ToValue"/>, mission
    /// stages use <see cref="FromStage"/>/<see cref="ToStage"/> and succession phases
    /// use <see cref="FromPhase"/>/<see cref="ToPhase"/> plus the optional
    /// <see cref="FromHeadId"/>/<see cref="ToHeadId"/> handoff pair.
    /// </summary>
    public sealed class FamilyChronicleEntry
    {
        public const string EntryIdPrefix = "chronicle.";
        public const string SupplyShortageMissionId = "mission.supply-shortage";
        public const string SealedLetterMissionId = "mission.sealed-letter";
        public const string MarketTallyMissionId = "mission.market-tally";
        public const string ShadowEscortCommissionId = "mission.shadow-escort";
        /// <summary>The single M06 tactical infiltration operation.</summary>
        public const string TacticalMissionId = "mission.m06.intercepted-dispatch";
        public const string MarriageSourceId = "marriage";
        public const string AdoptionSourceId = "adoption";
        /// <summary>M04B dynamic birth; reuses the family-formation category.</summary>
        public const string BirthSourceId = "birth";
        /// <summary>M08A annual natural death; own append-only category and source.</summary>
        public const string NaturalDeathSourceId = "natural-death";

        private FamilyChronicleEntry(int sequence, string entryId, int year, int month,
            ChronicleCategory category, string subjectId, string sourceId, string title, string summary,
            int fromValue, int toValue, int fromStage, int toStage, int fromPhase, int toPhase,
            string fromHeadId, string toHeadId)
        {
            Sequence = sequence;
            EntryId = entryId;
            Year = year;
            Month = month;
            Category = category;
            SubjectId = subjectId;
            SourceId = sourceId;
            Title = title;
            Summary = summary;
            FromValue = fromValue;
            ToValue = toValue;
            FromStage = fromStage;
            ToStage = toStage;
            FromPhase = fromPhase;
            ToPhase = toPhase;
            FromHeadId = fromHeadId ?? string.Empty;
            ToHeadId = toHeadId ?? string.Empty;
        }

        /// <summary>Stable identifier generated from the sequence; also used for saves.</summary>
        public string EntryId { get; }
        /// <summary>One-based append order; the ledger guarantees contiguous ascending values.</summary>
        public int Sequence { get; }
        public int Year { get; }
        public int Month { get; }
        public ChronicleCategory Category { get; }
        /// <summary>Stable subject: event, NPC, person-pair, mission or person ID.</summary>
        public string SubjectId { get; }
        /// <summary>Stable source: option, change channel, mission action or succession result.</summary>
        public string SourceId { get; }
        public string Title { get; }
        public string Summary { get; }
        public int FromValue { get; }
        public int ToValue { get; }
        public int FromStage { get; }
        public int ToStage { get; }
        public int FromPhase { get; }
        public int ToPhase { get; }
        /// <summary>
        /// Previous/new head IDs for a succession record. Both empty for pre-handoff
        /// records. A head handoff can leave <see cref="FromPhase"/> equal to
        /// <see cref="ToPhase"/> (an adult designated heir takes office while the
        /// family stays in <see cref="SuccessionPhase.None"/>), so this pair is the
        /// proof that the same-phase record describes a real change.
        /// </summary>
        public string FromHeadId { get; }
        public string ToHeadId { get; }

        public bool HasHeadChange =>
            !string.IsNullOrWhiteSpace(FromHeadId) &&
            !string.IsNullOrWhiteSpace(ToHeadId) &&
            !string.Equals(FromHeadId, ToHeadId, StringComparison.Ordinal);

        public static string BuildEntryId(int sequence) =>
            EntryIdPrefix + sequence.ToString("D6", CultureInfo.InvariantCulture);

        /// <summary>
        /// Validates one untrusted record. Returns false with a Chinese reason instead
        /// of throwing; callers must not add the record to a ledger on failure.
        /// </summary>
        public static bool TryCreate(int sequence, string entryId, int year, int month,
            ChronicleCategory category, string subjectId, string sourceId, string title, string summary,
            int fromValue, int toValue, int fromStage, int toStage, int fromPhase, int toPhase,
            out FamilyChronicleEntry entry, out string error) =>
            TryCreate(sequence, entryId, year, month, category, subjectId, sourceId, title, summary,
                fromValue, toValue, fromStage, toStage, fromPhase, toPhase, null, null,
                out entry, out error);

        /// <summary>
        /// Validates one untrusted record including the optional succession head pair.
        /// </summary>
        public static bool TryCreate(int sequence, string entryId, int year, int month,
            ChronicleCategory category, string subjectId, string sourceId, string title, string summary,
            int fromValue, int toValue, int fromStage, int toStage, int fromPhase, int toPhase,
            string fromHeadId, string toHeadId,
            out FamilyChronicleEntry entry, out string error)
        {
            entry = null;
            error = string.Empty;

            if (sequence < 1)
            {
                error = "编年史序号必须从 1 开始递增。";
                return false;
            }
            if (string.IsNullOrWhiteSpace(entryId))
            {
                error = "编年史记录 ID 为空。";
                return false;
            }
            if (!GameDate.IsValidDate(year, month, 1))
            {
                error = "编年史记录的年份或月份无效。";
                return false;
            }
            if (!Enum.IsDefined(typeof(ChronicleCategory), category))
            {
                error = "编年史记录类别无效。";
                return false;
            }
            if (string.IsNullOrWhiteSpace(subjectId))
            {
                error = "编年史记录主体 ID 为空。";
                return false;
            }
            if (string.IsNullOrWhiteSpace(sourceId))
            {
                error = "编年史记录来源 ID 为空。";
                return false;
            }
            if (string.IsNullOrWhiteSpace(title))
            {
                error = "编年史记录标题为空。";
                return false;
            }

            switch (category)
            {
                case ChronicleCategory.Relationship:
                    if (fromValue < CharacterRelationships.MinimumValue ||
                        fromValue > CharacterRelationships.MaximumValue ||
                        toValue < CharacterRelationships.MinimumValue ||
                        toValue > CharacterRelationships.MaximumValue)
                    {
                        error = "关系记录数值超出允许范围。";
                        return false;
                    }
                    if (fromValue == toValue)
                    {
                        error = "关系记录前后值相同，没有实际变化。";
                        return false;
                    }
                    break;
                case ChronicleCategory.Mission:
                    if (!TryValidateMissionStages(subjectId, fromStage, toStage, out error))
                        return false;
                    if (fromStage == toStage)
                    {
                        error = "任务记录前后阶段相同，没有实际变化。";
                        return false;
                    }
                    break;
                case ChronicleCategory.Succession:
                    if (!Enum.IsDefined(typeof(SuccessionPhase), fromPhase) ||
                        !Enum.IsDefined(typeof(SuccessionPhase), toPhase))
                    {
                        error = "继承记录阶段无效。";
                        return false;
                    }
                    bool hasFromHead = !string.IsNullOrWhiteSpace(fromHeadId);
                    bool hasToHead = !string.IsNullOrWhiteSpace(toHeadId);
                    if (hasFromHead != hasToHead)
                    {
                        error = "继承记录前后家主 ID 必须成对填写。";
                        return false;
                    }
                    if (hasFromHead &&
                        (!PersonId.TryParse(fromHeadId, out _) || !PersonId.TryParse(toHeadId, out _)))
                    {
                        error = "继承记录家主 ID 无效。";
                        return false;
                    }
                    bool headChanged = hasFromHead &&
                        !string.Equals(fromHeadId, toHeadId, StringComparison.Ordinal);
                    if (fromPhase == toPhase)
                    {
                        // Only an adult designated-heir handoff keeps the phase at None
                        // while changing the head. Requiring that exact real transition
                        // rejects arbitrary same-phase no-op records instead of making
                        // equal phases acceptable in general.
                        if (fromPhase != (int)SuccessionPhase.None || !headChanged)
                        {
                            error = "继承记录阶段相同且没有家督交接，没有实际变化。";
                            return false;
                        }
                    }
                    break;
                case ChronicleCategory.Industry:
                    if (fromValue < IndustryResources.Minimum || fromValue > IndustryResources.Maximum ||
                        toValue < IndustryResources.Minimum || toValue > IndustryResources.Maximum)
                    {
                        error = "产业记录资金值超出允许范围。";
                        return false;
                    }
                    if (string.IsNullOrWhiteSpace(summary))
                    {
                        error = "产业记录摘要不能为空。";
                        return false;
                    }
                    break;
                case ChronicleCategory.Education:
                    if (fromValue < CharacterGrowthValues.Minimum ||
                        fromValue > CharacterGrowthValues.Maximum ||
                        toValue < CharacterGrowthValues.Minimum ||
                        toValue > CharacterGrowthValues.Maximum)
                    {
                        error = "培养记录能力值超出允许范围。";
                        return false;
                    }
                    if (string.IsNullOrWhiteSpace(summary))
                    {
                        error = "培养记录摘要不能为空。";
                        return false;
                    }
                    break;
                case ChronicleCategory.Research:
                    if (fromValue < 0 || toValue <= fromValue ||
                        toValue > FamilyResearchCatalog.MaximumRequiredProgress)
                    {
                        error = "研究记录进度无效，必须是从 0 开始的真实推进。";
                        return false;
                    }
                    if (!FamilyResearchCatalog.TryGet(subjectId, out _) ||
                        !FamilyResearchCatalog.IsKnownTechniqueId(sourceId))
                    {
                        error = "研究记录包含未知项目或技艺 ID。";
                        return false;
                    }
                    if (string.IsNullOrWhiteSpace(summary))
                    {
                        error = "研究记录摘要不能为空。";
                        return false;
                    }
                    break;
                case ChronicleCategory.FamilyFormation:
                    if (sourceId != MarriageSourceId && sourceId != AdoptionSourceId &&
                        sourceId != BirthSourceId)
                    {
                        error = "家族关系记录来源必须是婚姻、收养或新生。";
                        return false;
                    }
                    if (string.IsNullOrWhiteSpace(summary))
                    {
                        error = "家族关系记录摘要不能为空。";
                        return false;
                    }
                    break;
                case ChronicleCategory.NaturalDeath:
                    if (sourceId != NaturalDeathSourceId)
                    {
                        error = "自然死亡记录来源必须是自然死亡。";
                        return false;
                    }
                    if (string.IsNullOrWhiteSpace(summary))
                    {
                        error = "自然死亡记录摘要不能为空。";
                        return false;
                    }
                    break;
            }

            entry = new FamilyChronicleEntry(sequence, entryId, year, month, category, subjectId,
                sourceId, title, summary ?? string.Empty, fromValue, toValue, fromStage, toStage,
                fromPhase, toPhase, fromHeadId, toHeadId);
            return true;
        }

        /// <summary>
        /// Mission stages are only meaningful for a known mission, so an unknown
        /// mission ID and a stage outside that mission's own enum are both rejected.
        /// The former union range 0..4 wrongly accepted impossible stages for the
        /// shorter sealed-letter and market-tally missions.
        /// </summary>
        private static bool TryValidateMissionStages(string missionId, int fromStage, int toStage,
            out string error)
        {
            error = string.Empty;
            switch (missionId)
            {
                case SupplyShortageMissionId:
                    return TryValidateStageRange(typeof(SupplyShortageStage), fromStage, toStage,
                        out error);
                case SealedLetterMissionId:
                    return TryValidateStageRange(typeof(SealedLetterStage), fromStage, toStage,
                        out error);
                case MarketTallyMissionId:
                    return TryValidateStageRange(typeof(MarketTallyStage), fromStage, toStage,
                        out error);
                case ShadowEscortCommissionId:
                    return TryValidateStageRange(typeof(ShadowEscortStage), fromStage, toStage,
                        out error);
                case TacticalMissionId:
                    return TryValidateStageRange(typeof(TacticalMissionStage), fromStage, toStage,
                        out error);
                default:
                    error = "任务记录包含未知任务 ID：" + (missionId ?? string.Empty) + "。";
                    return false;
            }
        }

        private static bool TryValidateStageRange(Type stageType, int fromStage, int toStage,
            out string error)
        {
            error = string.Empty;
            if (!Enum.IsDefined(stageType, fromStage) || !Enum.IsDefined(stageType, toStage))
            {
                error = "任务记录阶段超出该任务允许范围。";
                return false;
            }

            return true;
        }
    }
}
