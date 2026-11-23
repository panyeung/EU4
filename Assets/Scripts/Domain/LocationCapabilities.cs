using System;

namespace SengokuChronicle.Domain
{
    /// <summary>
    /// What the player may do at a travel hub. Every hub is visitable, but only the
    /// three M02 settlements currently have town services, residents and missions.
    /// Callers must consult this rule at the business entry point, not only in UI.
    /// </summary>
    public static class LocationCapabilities
    {
        public const string NonTownHubMessage = "这里是旅行枢纽，没有城镇活动、居民或委托；请继续旅行。";

        /// <summary>True only for the three legacy settlements that own town content.</summary>
        public static bool IsTown(LocationId location) =>
            location == LocationId.Kiyosu ||
            location == LocationId.Atsuta ||
            location == LocationId.Nagoya;

        public static bool CanUseTownServices(LocationId location) => IsTown(location);
        public static bool CanUseLocalActions(LocationId location) => IsTown(location);
        public static bool CanUseNpc(LocationId location) => IsTown(location);
        public static bool CanAcceptMission(LocationId location) => IsTown(location);

        /// <summary>The tactical mission remains exclusive to Kiyosu, as in M07.</summary>
        public static bool CanBeginTacticalMission(LocationId location) =>
            location == LocationId.Kiyosu;

        /// <summary>True for every place that exists on the national travel graph.</summary>
        public static bool IsTravelHub(LocationId location) =>
            JapanTravelGraph.TryGetHubForLocation(location, out _);

        /// <summary>True for Ezo and Ryukyu, which are not among the sixty-eight provinces.</summary>
        public static bool IsSeparateTravelRegion(LocationId location) =>
            JapanTravelGraph.TryGetHubForLocation(location, out JapanMapHub hub) &&
            (string.Equals(hub.DomainId, "ezo", StringComparison.Ordinal) ||
             string.Equals(hub.DomainId, "ryukyu", StringComparison.Ordinal));
    }
}