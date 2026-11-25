using SengokuChronicle.Domain;

namespace SengokuChronicle.Persistence
{
    /// <summary>
    /// Migration-time compatibility audit for the national map. It runs only after
    /// the normal save validator has accepted the document, so old saves keep their
    /// existing error messages while locations and in-flight travel are checked
    /// against the same graph the UI reads.
    /// </summary>
    public static class JapanMapSaveAudit
    {
        /// <summary>
        /// Validates a document that is already at the current format/rules. Use
        /// the overload below from the migrator so the pre-migration source version
        /// still controls the retired-route compatibility window.
        /// </summary>
        public static SaveOperationResult Validate(SaveData data)
        {
            if (data == null)
                return Invalid("地图兼容审计：存档数据为空。");
            return Validate(data, data.saveFormatVersion, data.rulesVersion);
        }

        /// <summary>
        /// The migrator rewrites saveFormatVersion/rulesVersion before this audit
        /// runs, so callers must pass the *source* versions. Only a genuine
        /// v17/rules 8 save may resume the three retired in-flight routes; an older
        /// source is held to the current graph even though the migrated payload now
        /// looks current.
        /// </summary>
        public static SaveOperationResult Validate(SaveData data, int sourceFormatVersion,
            int sourceRulesVersion)
        {
            if (data == null)
                return Invalid("地图兼容审计：存档数据为空。");

            bool allowRetiredRoutes = sourceFormatVersion == SaveDataValidator.CurrentVersion &&
                                      sourceRulesVersion == SaveData.CurrentRulesVersion;

            if (!LocationId.TryParse(data.locationId, out LocationId current) ||
                !JapanTravelGraph.TryGetHubForLocation(current, out _))
                return Invalid("地图兼容审计：存档当前位置不在全国旅行图中：" +
                               (data.locationId ?? "(null)") + "。");

            if (data.dailyActions == null || data.dailyActions.persons == null)
                return Invalid("地图兼容审计：存档缺少每日行动状态。");

            DailyActionPersonSaveEntry[] persons = data.dailyActions.persons;
            for (int i = 0; i < persons.Length; i++)
            {
                DailyActionPersonSaveEntry entry = persons[i];
                if (entry == null || !entry.hasAction ||
                    (DailyActionKind)entry.actionKind != DailyActionKind.Travel)
                    continue;

                if (!LocationId.TryParse(entry.travelOriginId, out LocationId origin) ||
                    !LocationId.TryParse(entry.travelDestinationId, out LocationId destination) ||
                    !JapanTravelGraph.TryGetHubForLocation(origin, out _) ||
                    !JapanTravelGraph.TryGetHubForLocation(destination, out _))
                    return Invalid("地图兼容审计：途中旅行端点不在全国旅行图中（" +
                                   (entry.travelOriginId ?? "(null)") + " → " +
                                   (entry.travelDestinationId ?? "(null)") + "）。");

                if (!WorldMapRules.TryGetInFlightTravelDays(origin, destination,
                        out int routeDays, allowRetiredRoutes) ||
                    routeDays != entry.actionDurationDays)
                    return Invalid("地图兼容审计：途中旅行与全国地图路线天数不一致（" +
                                   origin.Value + " → " + destination.Value + "，存档 " +
                                   entry.actionDurationDays + " 天）。");
            }

            return SaveOperationResult.Success();
        }

        private static SaveOperationResult Invalid(string message)
        {
            return SaveOperationResult.Failure(SaveErrorCode.InvalidData, message);
        }
    }
}
