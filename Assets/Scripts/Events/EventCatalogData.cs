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
        /// <summary>Optional scenario gate; empty means the event is scenario-neutral.</summary>
        public string scenario;
        /// <summary>
        /// Additional scenarios that may schedule this row. Used sparingly for a row that
        /// belongs to more than one route; it never makes the row scenario-neutral.
        /// </summary>
        public string[] sharedScenarios;
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
        /// <summary>Stable family-creed ID used by <see cref="EventConditionKind.FamilyCreedIs"/>.</summary>
        public string familyCreedId;
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
        /// <summary>NPC target used by <see cref="EventEffectKind.RelationshipDelta"/>.</summary>
        public string targetCharacterId;
        /// <summary>Family member target used by <see cref="EventEffectKind.FamilyRelationshipDelta"/>.</summary>
        public string targetPersonId;
        public int delta;
        /// <summary>Stable family-creed ID used by <see cref="EventEffectKind.SetFamilyCreed"/>.</summary>
        public string familyCreedId;
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
        SettledSuccessionCountAtLeast = 5,
        /// <summary>The active family creed equals <c>familyCreedId</c>; SC-024 only.</summary>
        FamilyCreedIs = 6
    }

    public enum EventRequirementKind
    {
        Always = 1,
        RelationshipAtLeast = 2
    }

    public enum EventEffectKind
    {
        RelationshipDelta = 1,
        /// <summary>Changes one (current head, non-head family member) pair value.</summary>
        FamilyRelationshipDelta = 2,
        /// <summary>Changes the persistent family creed; SC-024 only.</summary>
        SetFamilyCreed = 3
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

    /// <summary>Known scenario identifiers used by the optional event scenario gate.</summary>
    public static class EventScenarioIds
    {
        public const string Sc013 = "sc013";
        public const string Sc024 = "sc024";
    }

    /// <summary>Stable IDs for the SC-024 family-creed council event set.</summary>
    public static class SC024EventIds
    {
        public const string ScenarioId = EventScenarioIds.Sc024;

        public const string SecrecyCouncilEventId = "event.sc024.creed-council.secrecy";
        public const string RighteousnessCouncilEventId = "event.sc024.creed-council.righteousness";
        public const string MeritCouncilEventId = "event.sc024.creed-council.merit";

        public const string SecrecyKeepOptionId = "option.sc024.creed-council.secrecy.keep";
        public const string SecrecyReformRighteousnessOptionId =
            "option.sc024.creed-council.secrecy.reform-righteousness";
        public const string SecrecyReformMeritOptionId =
            "option.sc024.creed-council.secrecy.reform-merit";

        public const string RighteousnessKeepOptionId =
            "option.sc024.creed-council.righteousness.keep";
        public const string RighteousnessReformSecrecyOptionId =
            "option.sc024.creed-council.righteousness.reform-secrecy";
        public const string RighteousnessReformMeritOptionId =
            "option.sc024.creed-council.righteousness.reform-merit";

        public const string MeritKeepOptionId = "option.sc024.creed-council.merit.keep";
        public const string MeritReformSecrecyOptionId =
            "option.sc024.creed-council.merit.reform-secrecy";
        public const string MeritReformRighteousnessOptionId =
            "option.sc024.creed-council.merit.reform-righteousness";

        public const string SecrecyCreedId = nameof(Domain.FamilyCreedKind.SecrecyFirst);
        public const string RighteousnessCreedId = nameof(Domain.FamilyCreedKind.RighteousnessFirst);
        public const string MeritCreedId = nameof(Domain.FamilyCreedKind.MeritFirst);
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

    /// <summary>Stable IDs for the SC-013 fixed three-generation scenario events.</summary>
    public static class SC013EventIds
    {
        public const string ChainId = "chain.sc013.main";

        public const string PledgeEventId = "event.sc013.inheritance-pledge";
        public const string KeepPromiseOptionId = "option.sc013.inheritance-pledge.keep-promise";
        public const string BreakPromiseOptionId = "option.sc013.inheritance-pledge.break-promise";

        public const string FinaleEventId = "event.sc013.finale";
        public const string SeekAidOptionId = "option.sc013.finale.seek-aid";
        public const string StandAloneOptionId = "option.sc013.finale.stand-alone";

        public const string PledgeEventTextId = "event.sc013.inheritance-pledge";
        public const string KeepPromiseTextId = "option.sc013.inheritance-pledge.keep-promise";
        public const string KeepPromiseSummaryTextId = "summary.sc013.inheritance-pledge.keep-promise";
        public const string BreakPromiseTextId = "option.sc013.inheritance-pledge.break-promise";
        public const string BreakPromiseSummaryTextId = "summary.sc013.inheritance-pledge.break-promise";

        public const string FinaleEventTextId = "event.sc013.finale";
        public const string SeekAidTextId = "option.sc013.finale.seek-aid";
        public const string SeekAidSummaryTextId = "summary.sc013.finale.seek-aid";
        public const string StandAloneTextId = "option.sc013.finale.stand-alone";
        public const string StandAloneSummaryTextId = "summary.sc013.finale.stand-alone";
    }
}
