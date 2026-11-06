using System;
using System.IO;
using NUnit.Framework;
using SengokuChronicle.Events;
using UnityEngine;

namespace SengokuChronicle.Tests.EditMode
{
    /// <summary>
    /// Contract tests for the SC-011 content files. They assert the accepted rainy-arrival
    /// fields are preserved and that the one follow-up encounter has two exclusive variants.
    /// </summary>
    public sealed class SC011EventContentTests
    {
        private static EventCatalogData LoadCatalog()
        {
            return JsonUtility.FromJson<EventCatalogData>(
                File.ReadAllText("Assets/Resources/Events/event-catalog-v1.json"));
        }

        private static EventTextCatalog LoadText()
        {
            return JsonUtility.FromJson<EventTextCatalog>(
                File.ReadAllText("Assets/Resources/Localization/event-text-zh-v1.json"));
        }

        private static EventDefinitionData Find(EventCatalogData catalog, string eventId)
        {
            foreach (EventDefinitionData definition in catalog.events)
                if (string.Equals(definition.eventId, eventId, StringComparison.Ordinal))
                    return definition;
            Assert.Fail("Missing event " + eventId);
            return null;
        }

        private static EventOptionData FindOption(EventDefinitionData definition, string optionId)
        {
            foreach (EventOptionData option in definition.options)
                if (string.Equals(option.optionId, optionId, StringComparison.Ordinal))
                    return option;
            Assert.Fail("Missing option " + optionId);
            return null;
        }

        private static EventTextEntry FindText(EventTextCatalog text, string textId)
        {
            foreach (EventTextEntry entry in text.entries)
                if (string.Equals(entry.textId, textId, StringComparison.Ordinal))
                    return entry;
            Assert.Fail("Missing text " + textId);
            return null;
        }

        [Test]
        public void CatalogAndTextVersionThreeValidate()
        {
            EventCatalogData catalog = LoadCatalog();
            EventTextCatalog text = LoadText();

            Assert.That(catalog.contentVersion, Is.EqualTo(3));
            Assert.That(text.contentVersion, Is.EqualTo(3));
            EventCatalogValidationResult result = EventCatalogValidator.Validate(catalog, text);
            Assert.That(result.Succeeded, Is.True, result.Message);
        }

        [Test]
        public void AcceptedRainyArrivalFieldsArePreserved()
        {
            EventCatalogData catalog = LoadCatalog();
            EventDefinitionData root = Find(catalog, SampleEventIds.EventId);

            Assert.That(root.trigger, Is.EqualTo("Monthly"));
            Assert.That(root.weight, Is.EqualTo(10));
            Assert.That(root.priority, Is.EqualTo(10));
            Assert.That(root.cooldownMonths, Is.EqualTo(3));
            Assert.That(root.onceScope, Is.EqualTo("PerGame"));
            Assert.That(root.chainId, Is.EqualTo(SampleEventIds.ChainId));
            Assert.That(root.textId, Is.EqualTo(SampleEventIds.EventTextId));
            Assert.That(root.conditions, Has.Length.EqualTo(1));
            Assert.That(root.conditions[0].type, Is.EqualTo("SuccessionCountEquals"));
            Assert.That(root.conditions[0].threshold, Is.Zero);

            Assert.That(root.options, Has.Length.EqualTo(2));
            EventOptionData offerRest = FindOption(root, SampleEventIds.OfferRestOptionId);
            Assert.That(offerRest.requirements, Has.Length.EqualTo(1));
            Assert.That(offerRest.requirements[0].type, Is.EqualTo("Always"));
            Assert.That(offerRest.effects, Has.Length.EqualTo(1));
            Assert.That(offerRest.effects[0].type, Is.EqualTo("RelationshipDelta"));
            Assert.That(offerRest.effects[0].targetCharacterId, Is.EqualTo("npc.kuroda.iori"));
            Assert.That(offerRest.effects[0].delta, Is.EqualTo(5));
            Assert.That(offerRest.summaryTextId, Is.EqualTo(SampleEventIds.OfferRestSummaryTextId));

            EventOptionData askTomorrow = FindOption(root, SampleEventIds.AskTomorrowOptionId);
            Assert.That(askTomorrow.requirements, Has.Length.EqualTo(1));
            Assert.That(askTomorrow.requirements[0].type, Is.EqualTo("RelationshipAtLeast"));
            Assert.That(askTomorrow.requirements[0].targetCharacterId, Is.EqualTo("npc.kuroda.iori"));
            Assert.That(askTomorrow.requirements[0].minimumValue, Is.EqualTo(1));
            Assert.That(askTomorrow.effects, Is.Empty);

            EventTextCatalog text = LoadText();
            Assert.That(FindText(text, SampleEventIds.EventTextId).title, Is.EqualTo("雨夜来客"));
            Assert.That(FindText(text, SampleEventIds.OfferRestTextId).title, Is.EqualTo("请他进屋歇息"));
            Assert.That(FindText(text, SampleEventIds.AskTomorrowTextId).title, Is.EqualTo("请他明晨再来"));
        }

        [Test]
        public void ChainContainsRootAndBothFollowUpVariants()
        {
            EventCatalogData catalog = LoadCatalog();

            EventChainData rainy = FindChain(catalog, SampleEventIds.ChainId);
            CollectionAssert.AreEquivalent(
                new[] { SampleEventIds.EventId, SC011EventIds.LegacyHonoredEventId, SC011EventIds.LegacyDeferredEventId },
                rainy.linkedEventIds);
        }

        [Test]
        public void Sc013EventsAreScenarioTaggedAndLeaveSc011RowsUntouched()
        {
            EventCatalogData catalog = LoadCatalog();
            EventDefinitionData pledge = Find(catalog, SC013EventIds.PledgeEventId);
            EventDefinitionData finale = Find(catalog, SC013EventIds.FinaleEventId);

            Assert.That(pledge.scenario, Is.EqualTo(EventScenarioIds.Sc013));
            Assert.That(finale.scenario, Is.EqualTo(EventScenarioIds.Sc013));
            Assert.That(pledge.chainId, Is.EqualTo(SC013EventIds.ChainId));
            Assert.That(finale.chainId, Is.EqualTo(SC013EventIds.ChainId));
            Assert.That(Find(catalog, SampleEventIds.EventId).scenario, Is.Null.Or.Empty);
            Assert.That(Find(catalog, SC011EventIds.LegacyHonoredEventId).scenario, Is.Null.Or.Empty);
            Assert.That(Find(catalog, SC011EventIds.LegacyDeferredEventId).scenario, Is.Null.Or.Empty);
        }

        private static EventChainData FindChain(EventCatalogData catalog, string chainId)
        {
            foreach (EventChainData chain in catalog.chains)
                if (string.Equals(chain.chainId, chainId, StringComparison.Ordinal))
                    return chain;
            Assert.Fail("Missing chain " + chainId);
            return null;
        }

        [Test]
        public void HonoredVariantKeysOfferRestAndPreservesDesignText()
        {
            EventCatalogData catalog = LoadCatalog();
            EventTextCatalog text = LoadText();
            EventDefinitionData variant = Find(catalog, SC011EventIds.LegacyHonoredEventId);

            AssertVariantShape(variant, SampleEventIds.OfferRestOptionId);

            EventTextEntry eventText = FindText(text, variant.textId);
            Assert.That(eventText.title, Is.EqualTo("旧客的雨夜回访"));
            Assert.That(eventText.body, Does.Contain("当年那盏热茶"));

            Assert.That(variant.options, Has.Length.EqualTo(3));
            Assert.That(FindOption(variant, SC011EventIds.LegacyHonoredRenewOptionId).effects[0].delta, Is.EqualTo(5));
            Assert.That(FindOption(variant, SC011EventIds.LegacyHonoredTermsOptionId).effects, Is.Empty);
            Assert.That(FindOption(variant, SC011EventIds.LegacyHonoredCloseOptionId).effects[0].delta, Is.EqualTo(-5));
            Assert.That(FindText(text, SC011EventIds.LegacyHonoredRenewOptionId).title, Is.EqualTo("重申先代的门诺"));
            Assert.That(FindText(text, SC011EventIds.LegacyHonoredTermsOptionId).title, Is.EqualTo("按当下利害重议"));
            Assert.That(FindText(text, SC011EventIds.LegacyHonoredCloseOptionId).title, Is.EqualTo("止于这一夜"));
            Assert.That(FindText(text, "summary.legacy-honored.renew").body, Does.Contain("五点"));
        }

        [Test]
        public void DeferredVariantKeysAskTomorrowAndPreservesDesignText()
        {
            EventCatalogData catalog = LoadCatalog();
            EventTextCatalog text = LoadText();
            EventDefinitionData variant = Find(catalog, SC011EventIds.LegacyDeferredEventId);

            AssertVariantShape(variant, SampleEventIds.AskTomorrowOptionId);

            EventTextEntry eventText = FindText(text, variant.textId);
            Assert.That(eventText.title, Is.EqualTo("门房里的旧雨"));
            Assert.That(eventText.body, Does.Contain("门房"));

            Assert.That(variant.options, Has.Length.EqualTo(3));
            Assert.That(FindOption(variant, SC011EventIds.LegacyDeferredAtoneOptionId).effects[0].delta, Is.EqualTo(5));
            Assert.That(FindOption(variant, SC011EventIds.LegacyDeferredExplainOptionId).effects, Is.Empty);
            Assert.That(FindOption(variant, SC011EventIds.LegacyDeferredDismissOptionId).effects[0].delta, Is.EqualTo(-5));
            Assert.That(FindText(text, SC011EventIds.LegacyDeferredAtoneOptionId).title, Is.EqualTo("补上先代欠下的礼数"));
            Assert.That(FindText(text, SC011EventIds.LegacyDeferredExplainOptionId).title, Is.EqualTo("说明当日不得已"));
            Assert.That(FindText(text, SC011EventIds.LegacyDeferredDismissOptionId).title, Is.EqualTo("旧事不必再提"));
        }

        private static void AssertVariantShape(EventDefinitionData variant, string sourceOptionId)
        {
            Assert.That(variant.trigger, Is.EqualTo("Monthly"));
            Assert.That(variant.weight, Is.EqualTo(1));
            Assert.That(variant.priority, Is.EqualTo(100));
            Assert.That(variant.cooldownMonths, Is.Zero);
            Assert.That(variant.onceScope, Is.EqualTo("PerGame"));
            Assert.That(variant.chainId, Is.EqualTo(SampleEventIds.ChainId));
            Assert.That(variant.conditions, Has.Length.EqualTo(2));
            Assert.That(variant.conditions[0].type, Is.EqualTo("EventOptionChosen"));
            Assert.That(variant.conditions[0].sourceEventId, Is.EqualTo(SampleEventIds.EventId));
            Assert.That(variant.conditions[0].sourceOptionId, Is.EqualTo(sourceOptionId));
            Assert.That(variant.conditions[1].type, Is.EqualTo("SettledSuccessionCountAtLeast"));
            Assert.That(variant.conditions[1].threshold, Is.EqualTo(1));
            foreach (EventOptionData option in variant.options)
            {
                Assert.That(option.requirements, Has.Length.EqualTo(1));
                Assert.That(option.requirements[0].type, Is.EqualTo("Always"));
            }
        }
    }
}
