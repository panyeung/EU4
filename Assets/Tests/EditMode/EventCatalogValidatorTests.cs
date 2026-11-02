using System.IO;
using NUnit.Framework;
using SengokuChronicle.Events;
using UnityEngine;

namespace SengokuChronicle.Tests.EditMode
{
    public sealed class EventCatalogValidatorTests
    {
        private static EventCatalogData ValidCatalog()
        {
            return new EventCatalogData
            {
                contentVersion = 1,
                chains = new[] { new EventChainData { chainId = "chain.guest", linkedEventIds = new[] { "event.sample.guest" } } },
                events = new[]
                {
                    new EventDefinitionData
                    {
                        eventId = "event.sample.guest",
                        trigger = "Monthly",
                        weight = 3,
                        textId = "text.guest",
                        conditions = new[] { new EventConditionData { type = "Always" } },
                        options = new[]
                        {
                            new EventOptionData
                            {
                                optionId = "option.guest.listen",
                                textId = "text.listen",
                                summaryTextId = "text.listen.summary",
                                requirements = new[] { new EventRequirementData { type = "Always" } },
                                effects = new[]
                                {
                                    new EventEffectData
                                    {
                                        type = "RelationshipDelta",
                                        targetCharacterId = "npc.kuroda.iori",
                                        delta = 5
                                    }
                                }
                            },
                            new EventOptionData
                            {
                                optionId = "option.guest.defer",
                                textId = "text.defer",
                                requirements = new EventRequirementData[0],
                                effects = new EventEffectData[0]
                            }
                        },
                        cooldownMonths = 2,
                        onceScope = "Never",
                        chainId = "chain.guest",
                        priority = 10
                    }
                }
            };
        }

        private static EventTextCatalog ValidText()
        {
            return new EventTextCatalog
            {
                contentVersion = 1,
                entries = new[]
                {
                    new EventTextEntry { textId = "text.guest", title = "夜雨来客", body = "一位疲惫的旅人来到院门前。" },
                    new EventTextEntry { textId = "text.listen", title = "听他说完", body = "给旅人一盏灯和一杯热茶。" },
                    new EventTextEntry { textId = "text.listen.summary", title = "关系改善", body = "黑田伊织对家主的信任有所增加。" },
                    new EventTextEntry { textId = "text.defer", title = "明日再议", body = "让旅人先在门房休息。" }
                }
            };
        }

        [Test]
        public void RejectsDuplicateEventAndOptionIds()
        {
            EventCatalogData catalog = ValidCatalog();
            catalog.events = new[] { catalog.events[0], catalog.events[0] };
            Assert.That(EventCatalogValidator.Validate(catalog, ValidText()).Succeeded, Is.False);

            catalog = ValidCatalog();
            catalog.events[0].options[1].optionId = catalog.events[0].options[0].optionId;
            Assert.That(EventCatalogValidator.Validate(catalog, ValidText()).Succeeded, Is.False);
        }

        [Test]
        public void RejectsMissingTextAndDanglingReferences()
        {
            EventCatalogData catalog = ValidCatalog();
            catalog.events[0].textId = "text.missing";
            Assert.That(EventCatalogValidator.Validate(catalog, ValidText()).Succeeded, Is.False);

            catalog = ValidCatalog();
            catalog.events[0].chainId = "chain.missing";
            Assert.That(EventCatalogValidator.Validate(catalog, ValidText()).Succeeded, Is.False);

            catalog = ValidCatalog();
            catalog.chains[0].linkedEventIds[0] = "event.missing";
            Assert.That(EventCatalogValidator.Validate(catalog, ValidText()).Succeeded, Is.False);
        }

        [Test]
        public void RejectsUnknownConditionOrEffect()
        {
            EventCatalogData catalog = ValidCatalog();
            catalog.events[0].conditions[0].type = "RunArbitraryCode";
            Assert.That(EventCatalogValidator.Validate(catalog, ValidText()).Succeeded, Is.False);

            catalog = ValidCatalog();
            catalog.events[0].options[0].effects[0].type = "ExecuteCSharp";
            Assert.That(EventCatalogValidator.Validate(catalog, ValidText()).Succeeded, Is.False);

            catalog = ValidCatalog();
            catalog.events[0].options[0].requirements[0].type = "RunArbitraryCode";
            Assert.That(EventCatalogValidator.Validate(catalog, ValidText()).Succeeded, Is.False);
        }

        [Test]
        public void RejectsInvalidWeightCooldownAndBounds()
        {
            EventCatalogData catalog = ValidCatalog();
            catalog.events[0].weight = 0;
            Assert.That(EventCatalogValidator.Validate(catalog, ValidText()).Succeeded, Is.False);

            catalog = ValidCatalog();
            catalog.events[0].cooldownMonths = -1;
            Assert.That(EventCatalogValidator.Validate(catalog, ValidText()).Succeeded, Is.False);

            catalog = ValidCatalog();
            catalog.events[0].cooldownMonths = int.MaxValue;
            Assert.That(EventCatalogValidator.Validate(catalog, ValidText()).Succeeded, Is.False);

            catalog = ValidCatalog();
            catalog.events[0].options[0].effects[0].delta = 101;
            Assert.That(EventCatalogValidator.Validate(catalog, ValidText()).Succeeded, Is.False);
        }

        [Test]
        public void AcceptsSampleCatalog()
        {
            EventCatalogData catalog = JsonUtility.FromJson<EventCatalogData>(
                File.ReadAllText("Assets/Resources/Events/event-catalog-v1.json"));
            EventTextCatalog text = JsonUtility.FromJson<EventTextCatalog>(
                File.ReadAllText("Assets/Resources/Localization/event-text-zh-v1.json"));

            EventCatalogValidationResult result = EventCatalogValidator.Validate(catalog, text);

            Assert.That(result.Succeeded, Is.True, result.Message);
        }

        [Test]
        public void ContentVersionIsExplicit()
        {
            EventCatalogData catalog = ValidCatalog();
            catalog.contentVersion = 0;

            Assert.That(EventCatalogValidator.Validate(catalog, ValidText()).Succeeded, Is.False);
        }

        private static EventCatalogData CrossGenerationCatalog()
        {
            return new EventCatalogData
            {
                contentVersion = 2,
                chains = new[]
                {
                    new EventChainData
                    {
                        chainId = "chain.guest",
                        linkedEventIds = new[] { "event.guest.root", "event.guest.kept", "event.guest.deferred" }
                    }
                },
                events = new[]
                {
                    CrossGenerationEvent("event.guest.root", 10, 10, 3,
                        new[] { new EventConditionData { type = "SuccessionCountEquals", threshold = 0 } },
                        new[] { "option.guest.root.offer", "option.guest.root.defer" }),
                    CrossGenerationEvent("event.guest.kept", 100, 1, 0,
                        new[]
                        {
                            new EventConditionData
                            {
                                type = "EventOptionChosen",
                                sourceEventId = "event.guest.root",
                                sourceOptionId = "option.guest.root.offer"
                            },
                            new EventConditionData { type = "SettledSuccessionCountAtLeast", threshold = 1 }
                        },
                        new[] { "option.guest.kept.renew" }),
                    CrossGenerationEvent("event.guest.deferred", 100, 1, 0,
                        new[]
                        {
                            new EventConditionData
                            {
                                type = "EventOptionChosen",
                                sourceEventId = "event.guest.root",
                                sourceOptionId = "option.guest.root.defer"
                            },
                            new EventConditionData { type = "SettledSuccessionCountAtLeast", threshold = 1 }
                        },
                        new[] { "option.guest.deferred.atone" })
                }
            };
        }

        private static EventDefinitionData CrossGenerationEvent(string eventId, int priority, int weight,
            int cooldownMonths, EventConditionData[] conditions, string[] optionIds)
        {
            var options = new EventOptionData[optionIds.Length];
            for (int i = 0; i < optionIds.Length; i++)
                options[i] = new EventOptionData
                {
                    optionId = optionIds[i],
                    textId = "text." + optionIds[i],
                    requirements = new[] { new EventRequirementData { type = "Always" } },
                    effects = new EventEffectData[0]
                };
            return new EventDefinitionData
            {
                eventId = eventId,
                trigger = "Monthly",
                conditions = conditions,
                weight = weight,
                textId = "text." + eventId,
                options = options,
                cooldownMonths = cooldownMonths,
                onceScope = "PerGame",
                chainId = "chain.guest",
                priority = priority
            };
        }

        private static EventTextCatalog CrossGenerationText(EventCatalogData catalog)
        {
            var entries = new System.Collections.Generic.List<EventTextEntry>();
            foreach (EventDefinitionData definition in catalog.events)
            {
                entries.Add(new EventTextEntry
                {
                    textId = definition.textId,
                    title = definition.eventId,
                    body = "Original cross-generation text."
                });
                foreach (EventOptionData option in definition.options)
                    entries.Add(new EventTextEntry
                    {
                        textId = option.textId,
                        title = option.optionId,
                        body = "An original choice body."
                    });
            }
            return new EventTextCatalog { contentVersion = catalog.contentVersion, entries = entries.ToArray() };
        }

        [Test]
        public void AcceptsCrossGenerationChainShape()
        {
            EventCatalogData catalog = CrossGenerationCatalog();

            EventCatalogValidationResult result = EventCatalogValidator.Validate(catalog, CrossGenerationText(catalog));

            Assert.That(result.Succeeded, Is.True, result.Message);
        }

        [Test]
        public void RejectsUnknownSourceEventOrOption()
        {
            EventCatalogData catalog = CrossGenerationCatalog();
            catalog.events[1].conditions[0].sourceEventId = "event.missing";
            Assert.That(EventCatalogValidator.Validate(catalog, CrossGenerationText(catalog)).Message,
                Does.Contain("unknown source event"));

            catalog = CrossGenerationCatalog();
            catalog.events[1].conditions[0].sourceOptionId = "option.missing";
            Assert.That(EventCatalogValidator.Validate(catalog, CrossGenerationText(catalog)).Message,
                Does.Contain("unknown source option"));
        }

        [Test]
        public void RejectsCrossChainAndSelfReference()
        {
            EventCatalogData catalog = CrossGenerationCatalog();
            catalog.events[1].chainId = "chain.other";
            catalog.chains = new[]
            {
                catalog.chains[0],
                new EventChainData
                {
                    chainId = "chain.other",
                    linkedEventIds = new[] { "event.guest.kept" }
                }
            };
            Assert.That(EventCatalogValidator.Validate(catalog, CrossGenerationText(catalog)).Message,
                Does.Contain("share the source event's chain"));

            catalog = CrossGenerationCatalog();
            catalog.events[1].conditions[0].sourceEventId = "event.guest.kept";
            catalog.events[1].conditions[0].sourceOptionId = "option.guest.kept.renew";
            Assert.That(EventCatalogValidator.Validate(catalog, CrossGenerationText(catalog)).Message,
                Does.Contain("cannot reference itself"));
        }

        [Test]
        public void RejectsDuplicateBranchPairAndRepeatedHistoryCondition()
        {
            EventCatalogData catalog = CrossGenerationCatalog();
            catalog.events[2].conditions[0].sourceOptionId = "option.guest.root.offer";
            Assert.That(EventCatalogValidator.Validate(catalog, CrossGenerationText(catalog)).Message,
                Does.Contain("both claim root branch"));

            catalog = CrossGenerationCatalog();
            catalog.events[1].conditions = new[]
            {
                catalog.events[1].conditions[0],
                catalog.events[1].conditions[0]
            };
            Assert.That(EventCatalogValidator.Validate(catalog, CrossGenerationText(catalog)).Message,
                Does.Contain("more than one EventOptionChosen"));
        }

        [Test]
        public void RejectsInvalidSuccessionGateAndNonPerGameFollowUp()
        {
            EventCatalogData catalog = CrossGenerationCatalog();
            catalog.events[1].conditions[1].threshold = 1001;
            Assert.That(EventCatalogValidator.Validate(catalog, CrossGenerationText(catalog)).Message,
                Does.Contain("out-of-range succession threshold"));

            catalog = CrossGenerationCatalog();
            catalog.events[0].trigger = "Daily";
            Assert.That(EventCatalogValidator.Validate(catalog, CrossGenerationText(catalog)).Message,
                Does.Contain("only allowed on Monthly"));

            catalog = CrossGenerationCatalog();
            catalog.events[1].onceScope = "Never";
            Assert.That(EventCatalogValidator.Validate(catalog, CrossGenerationText(catalog)).Message,
                Does.Contain("requires onceScope PerGame"));
        }
    }
}
