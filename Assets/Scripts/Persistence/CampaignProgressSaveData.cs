using System;
using SengokuChronicle.Domain;

namespace SengokuChronicle.Persistence
{
    /// <summary>
    /// Plain-data v16 campaign origin and review-acknowledgement block. A legacy save
    /// that cannot prove its scenario origin is stored as an explicit unknown origin:
    /// no start date, no origin ID and no claimed review. Migration never replays time
    /// or manufactures a historical event.
    /// </summary>
    [Serializable]
    public sealed class CampaignProgressSaveData
    {
        /// <summary>False for a legacy save whose scenario identity cannot prove a start date.</summary>
        public bool startKnown;
        public int startYear;
        public int startMonth;
        public int startDay;
        /// <summary>Stable authored scenario ID from <see cref="CampaignOriginCatalog"/>.</summary>
        public string originId;
        /// <summary>True after the player acknowledged the 55-year stage review.</summary>
        public bool stageReviewAcknowledged;
        /// <summary>True after the player acknowledged the 100-year 百年名门 review.</summary>
        public bool centuryReviewAcknowledged;

        public static CampaignProgressSaveData CreateUnknownOrigin() =>
            new CampaignProgressSaveData
            {
                startKnown = false,
                startYear = 0,
                startMonth = 0,
                startDay = 0,
                originId = string.Empty,
                stageReviewAcknowledged = false,
                centuryReviewAcknowledged = false
            };

        public static CampaignProgressSaveData Capture(CampaignProgress progress)
        {
            if (progress == null || !progress.StartKnown)
                return CreateUnknownOrigin();
            return new CampaignProgressSaveData
            {
                startKnown = true,
                startYear = progress.StartDate.Year,
                startMonth = progress.StartDate.Month,
                startDay = progress.StartDate.Day,
                originId = progress.OriginId ?? string.Empty,
                stageReviewAcknowledged = progress.StageReviewAcknowledged,
                centuryReviewAcknowledged = progress.CenturyReviewAcknowledged
            };
        }

        /// <summary>
        /// Migration entry point. Origin is inferred only when the legacy save still
        /// carries the authored scenario identity and its date is not earlier than that
        /// scenario's fixed start. A defaulted succession (v1/v2) never proves an origin.
        /// </summary>
        public static CampaignProgressSaveData CreateMigrated(SaveData legacy, bool originProvable)
        {
            if (!originProvable || legacy == null)
                return CreateUnknownOrigin();
            if (!GameDate.TryParse(legacy.year, legacy.month, legacy.day, out GameDate savedDate))
                return CreateUnknownOrigin();

            string familyId = legacy.succession != null ? legacy.succession.familyId : string.Empty;
            if (!CampaignOriginCatalog.TryInferLegacyOrigin(familyId, legacy.activeEventScenarioId,
                    savedDate, out CampaignOrigin origin))
                return CreateUnknownOrigin();

            return Capture(CampaignProgress.CreateNew(origin.StartDate, origin.Id));
        }

        public CampaignProgressSaveData Clone() => new CampaignProgressSaveData
        {
            startKnown = startKnown,
            startYear = startYear,
            startMonth = startMonth,
            startDay = startDay,
            originId = originId,
            stageReviewAcknowledged = stageReviewAcknowledged,
            centuryReviewAcknowledged = centuryReviewAcknowledged
        };

        /// <summary>Structural and date-consistency validation; never throws.</summary>
        public SaveOperationResult ValidateForSave(GameDate date)
        {
            if (!TryRestore(out CampaignProgress progress, out string restoreError))
                return Invalid(restoreError);
            if (!progress.StartKnown)
                return SaveOperationResult.Success();
            if (progress.StartDate > date)
                return Invalid("存档战役开局日期晚于当前存档日期。");
            if (progress.StageReviewAcknowledged &&
                progress.TryGetStageReviewDate(out GameDate stage) && date < stage)
                return Invalid("存档已确认五十五周年阶段评价，但日期尚未到达该评价日。");
            if (progress.CenturyReviewAcknowledged &&
                progress.TryGetCenturyReviewDate(out GameDate century) && date < century)
                return Invalid("存档已确认百年名门评定，但日期尚未到达该评定日。");
            return SaveOperationResult.Success();
        }

        /// <summary>Rebuilds the validated domain snapshot; the caller must not assign it on failure.</summary>
        public bool TryRestore(out CampaignProgress progress, out string error)
        {
            progress = null;
            error = string.Empty;
            if (string.IsNullOrWhiteSpace(originId) && startKnown)
            {
                error = "存档战役开局已知，但缺少剧本来源 ID。";
                return false;
            }

            GameDate startDate = default;
            if (startKnown && !GameDate.TryParse(startYear, startMonth, startDay, out startDate))
            {
                error = "存档战役开局日期无效。";
                return false;
            }
            if (!CampaignProgress.TryCreate(startKnown, startDate, originId,
                    stageReviewAcknowledged, centuryReviewAcknowledged,
                    out CampaignProgress restored, out string createError))
            {
                error = string.IsNullOrEmpty(createError) ? "存档战役纪元状态无效。" : createError;
                return false;
            }

            progress = restored;
            return true;
        }

        private static SaveOperationResult Invalid(string message) =>
            SaveOperationResult.Failure(SaveErrorCode.InvalidData, message);
    }
}
