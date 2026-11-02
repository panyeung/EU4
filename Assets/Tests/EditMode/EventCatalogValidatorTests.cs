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
    }
}
