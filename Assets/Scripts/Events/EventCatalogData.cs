using System;

namespace SengokuChronicle.Events
{
    [Serializable]
    public sealed class EventCatalogData
    {
        public int contentVersion;
        public EventDefinitionData[] events;
        public EventChainData[] chains;
    }

    [Serializable]
    public sealed class EventDefinitionData
    {
        public string eventId;
        public string trigger;
        public EventConditionData[] conditions;
        public int weight;
        public string textId;
        public EventOptionData[] options;
        public int cooldownMonths;
        public string onceScope;
        public string chainId;
        public int priority;
    }

    [Serializable]
    public sealed class EventConditionData
    {
        public string type;
        public string targetCharacterId;
        public int threshold;
        public string sourceEventId;
        public string sourceOptionId;
    }

    [Serializable]
    public sealed class EventOptionData
    {
        public string optionId;
        public string textId;
        public string summaryTextId;
        public EventRequirementData[] requirements;
        public EventEffectData[] effects;
    }

    [Serializable]
    public sealed class EventRequirementData
    {
        public string type;
        public string targetCharacterId;
        public int minimumValue;
        public string reason;
    }

    [Serializable]
    public sealed class EventEffectData
    {
        public string type;
        public string targetCharacterId;
        public int delta;
    }

    [Serializable]
    public sealed class EventChainData
    {
        public string chainId;
        public string[] linkedEventIds;
    }

    [Serializable]
    public sealed class EventTextCatalog
    {
        public int contentVersion;
        public EventTextEntry[] entries;
    }

    [Serializable]
    public sealed class EventTextEntry
    {
        public string textId;
        public string title;
        public string body;
    }

    public enum EventTriggerKind
    {
        Monthly = 1
    }

    public enum EventConditionKind
    {
        Always = 1,
        RelationshipAtLeast = 2,
        /// <summary>The last committed history entry for <c>sourceEventId</c> used <c>sourceOptionId</c>.</summary>
        EventOptionChosen = 3,
        /// <summary>The raw succession count equals <c>threshold</c>; offer-time only.</summary>
        SuccessionCountEquals = 4,
        /// <summary>The settled succession count is at least <c>threshold</c>; offer-time only.</summary>
        SettledSuccessionCountAtLeast = 5
    }

    public enum EventRequirementKind
    {
        Always = 1,
        RelationshipAtLeast = 2
    }

    public enum EventEffectKind
    {
        RelationshipDelta = 1
    }

    public enum EventOnceScope
    {
        Never = 0,
        PerGame = 1
    }

    /// <summary>Stable identifiers used by the fixed, data-only sample event.</summary>
    public static class SampleEventIds
    {
        public const string EventId = "event.rainy-arrival";
        public const string ChainId = "chain.rainy-arrival";
        public const string OfferRestOptionId = "option.rainy-arrival.offer-rest";
        public const string AskTomorrowOptionId = "option.rainy-arrival.ask-tomorrow";
        public const string EventTextId = "event.rainy-arrival";
        public const string OfferRestTextId = "option.rainy-arrival.offer-rest";
        public const string AskTomorrowTextId = "option.rainy-arrival.ask-tomorrow";
        public const string OfferRestSummaryTextId = "summary.rainy-arrival.offer-rest";
    }

    /// <summary>Stable IDs for the SC-011 cross-generation follow-up encounter variants.</summary>
    public static class SC011EventIds
    {
        public const string LegacyHonoredEventId = "event.rainy-arrival.legacy-honored";
        public const string LegacyDeferredEventId = "event.rainy-arrival.legacy-deferred";

        public const string LegacyHonoredRenewOptionId = "option.legacy-honored.renew";
        public const string LegacyHonoredTermsOptionId = "option.legacy-honored.terms";
        public const string LegacyHonoredCloseOptionId = "option.legacy-honored.close";

        public const string LegacyDeferredAtoneOptionId = "option.legacy-deferred.atone";
        public const string LegacyDeferredExplainOptionId = "option.legacy-deferred.explain";
        public const string LegacyDeferredDismissOptionId = "option.legacy-deferred.dismiss";
    }
}
