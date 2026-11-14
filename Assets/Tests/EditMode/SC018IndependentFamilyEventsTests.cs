using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;
using SengokuChronicle.Events;
using SengokuChronicle.Persistence;
using UnityEngine;

namespace SengokuChronicle.Tests.EditMode
{
    /// <summary>
    /// Contract tests for the seven SC-018 standalone family events and the
    /// ordered (current head, family member) pair model. They lock the 30-event /
    /// 5-chain catalog shape, the person-pair semantics, transactional event effects,
    /// v5 save round-tripping, v4 migration, and chronicle names/values.
    /// </summary>
    public sealed class SC018IndependentFamilyEventsTests
    {
        private static readonly string[] NewEventIds =
        {
            "event.sc018.sibling-duty",
            "event.sc018.child-study",
            "event.sc018.parent-care",
            "event.sc018.heirloom",
            "event.sc018.spouse-visit",
            "event.sc018.adoption-welcome",
            "event.sc018.family-assembly"
        };

        private static readonly string[] GatedEventIds =
        {
            "event.sc018.child-study",
            "event.sc018.heirloom",
            "event.sc018.family-assembly"
        };

        // These four events stay eligible across successions, but the affected
        // member's kinship to the current head changes each generation. Their text
        // must therefore avoid head-relative role words.
        private static readonly string[] AdaptiveNarrativeEventIds =
        {
            "event.sc018.sibling-duty",
            "event.sc018.child-study",
            "event.sc018.parent-care",
            "event.sc018.spouse-visit"
        };

        private static readonly string[] HeadRelativeRoleFragments =
        {
            "手足", "配偶", "归宁", "子女", "孩子", "长子", "次子", "父亲", "母亲",
            "父母", "妻子", "丈夫", "长辈", "女婿", "儿媳", "孙子", "孙女", "侄子",
            "外甥", "叔父", "姑母", "兄长", "弟弟", "妹妹", "姐姐"
        };

        // eventId|chainId|priority|weight|optionCount|conditionCount, captured before SC-018.
        private static readonly string[] BaselineEvents =
        {
            "event.rainy-arrival|chain.rainy-arrival|10|10|2|1",
            "event.rainy-arrival.legacy-honored|chain.rainy-arrival|100|1|3|2",
            "event.rainy-arrival.legacy-deferred|chain.rainy-arrival|100|1|3|2",
            "event.sc013.inheritance-pledge|chain.sc013.main|1000|1|2|1",
            "event.sc013.finale|chain.sc013.main|1000|1|2|1",
            "event.sc013.refugee-arrival|chain.sc013.refugee-legacy|5|1|3|1",
            "event.sc013.refugee-loyalty|chain.sc013.refugee-legacy|50|1|2|2",
            "event.sc013.refugee-aid|chain.sc013.refugee-legacy|500|1|2|2",
            "event.sc013.refugee-rival|chain.sc013.refugee-legacy|500|1|2|2",
            "event.sc013.family-letters.first|chain.sc013.family-letters|6|1|2|1",
            "event.sc013.family-letters.second-open|chain.sc013.family-letters|60|1|2|2",
            "event.sc013.family-letters.second-sealed|chain.sc013.family-letters|60|1|2|2",
            "event.sc013.family-letters.third-full-public|chain.sc013.family-letters|600|1|1|2",
            "event.sc013.family-letters.third-short-history|chain.sc013.family-letters|600|1|1|2",
            "event.sc013.family-letters.third-opened-by-heir|chain.sc013.family-letters|600|1|1|2",
            "event.sc013.family-letters.third-sealed|chain.sc013.family-letters|600|1|1|2",
            "event.sc013.commission-legacy.first|chain.sc013.commission-legacy|8|1|2|1",
            "event.sc013.commission-legacy.second-kept|chain.sc013.commission-legacy|80|1|2|2",
            "event.sc013.commission-legacy.second-broken|chain.sc013.commission-legacy|80|1|2|2",
            "event.sc013.commission-legacy.third-safe|chain.sc013.commission-legacy|800|1|1|2",
            "event.sc013.commission-legacy.third-public|chain.sc013.commission-legacy|800|1|1|2",
            "event.sc013.commission-legacy.third-cleared|chain.sc013.commission-legacy|800|1|1|2",
            "event.sc013.commission-legacy.third-severed|chain.sc013.commission-legacy|800|1|1|2"
        };

        private static readonly string[] MandatoryM2EventIds =
        {
            "event.rainy-arrival",
            "event.rainy-arrival.legacy-honored",
            "event.rainy-arrival.legacy-deferred",
            "event.sc013.inheritance-pledge",
            "event.sc013.finale"
        };

        private static readonly string[] FamilyNames =
        {
            "晴信", "千恵", "正綱", "信正", "時姫"
        };

        private readonly List<GameSession> _sessions = new List<GameSession>();

        [TearDown]
        public void TearDown()
        {
            foreach (GameSession session in _sessions)
                if (session != null)
                    UnityEngine.Object.DestroyImmediate(session.gameObject);
            _sessions.Clear();
        }

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
            EventDefinitionData found = catalog.events.FirstOrDefault(
                definition => string.Equals(definition.eventId, eventId, StringComparison.Ordinal));
            Assert.That(found, Is.Not.Null, "Missing event " + eventId);
            return found;
        }

        private static EventChainData FindChain(EventCatalogData catalog, string chainId)
        {
            EventChainData found = catalog.chains.FirstOrDefault(
                chain => string.Equals(chain.chainId, chainId, StringComparison.Ordinal));
            Assert.That(found, Is.Not.Null, "Missing chain " + chainId);
            return found;
        }

        private static EventTextEntry FindText(EventTextCatalog text, string textId)
        {
            EventTextEntry found = text.entries.FirstOrDefault(
                entry => string.Equals(entry.textId, textId, StringComparison.Ordinal));
            Assert.That(found, Is.Not.Null, "Missing text " + textId);
            return found;
        }

        private static PersonId[] FamilyMemberIds()
        {
            return new[]
            {
                M2FixedScenario.HarunobuId,
                M2FixedScenario.ChieId,
                M2FixedScenario.MasatsunaId,
                M2FixedScenario.NobumasaId,
                M2FixedScenario.TokihimeId,
                M2FixedScenario.SakonId
            };
        }

        private static EventRuleContext ContextFor(PersonId head, int successionCount = 0, int settled = 0)
        {
            return new EventRuleContext(successionCount, settled, EventScenarioIds.Sc013,
                head, FamilyMemberIds());
        }

        private static EventEngine CreateEngine(string eventId, EventEffectData[] effects,
            EventConditionData[] conditions = null)
        {
            var definition = new EventDefinitionData
            {
                eventId = eventId,
                scenario = EventScenarioIds.Sc013,
                trigger = "Monthly",
                conditions = conditions ?? new EventConditionData[0],
                weight = 1,
                textId = "text." + eventId,
                options = new[]
                {
                    new EventOptionData
                    {
                        optionId = "option." + eventId,
                        textId = "option-text." + eventId,
                        summaryTextId = "summary." + eventId,
                        requirements = new[] { new EventRequirementData { type = "Always" } },
                        effects = effects
                    }
                },
                cooldownMonths = 0,
                onceScope = "PerGame",
                chainId = string.Empty,
                priority = 1
            };
            var catalog = new EventCatalogData
            {
                contentVersion = 2,
                events = new[] { definition },
                chains = new EventChainData[0]
            };
            var text = new EventTextCatalog
            {
                contentVersion = 2,
                entries = new[]
                {
                    new EventTextEntry { textId = definition.textId, title = eventId, body = "家庭事件。" },
                    new EventTextEntry
                    {
                        textId = definition.options[0].textId, title = "选项", body = "作出选择。"
                    },
                    new EventTextEntry
                    {
                        textId = definition.options[0].summaryTextId, title = "结果", body = "关系改变五点。"
                    }
                }
            };
            return new EventEngine(catalog, text);
        }

        private static EventScheduleResult Schedule(EventEngine engine, EventRuntimeState state,
            EventRuleContext context)
        {
            return engine.ScheduleMonthly(new GameDate(1560, 5, 1), new CharacterRelationships(),
                RandomStateManager.Create(707UL).GetStream(RandomStreamKind.Events), state, context);
        }

        [Test]
        public void CatalogLoadsAndValidatesWithFortyThreeEventsAndFiveChains()
        {
            EventCatalogData catalog = LoadCatalog();
            EventTextCatalog text = LoadText();

            Assert.That(catalog.contentVersion, Is.EqualTo(4));
            Assert.That(text.contentVersion, Is.EqualTo(4));
            EventCatalogValidationResult result = EventCatalogValidator.Validate(catalog, text);
            Assert.That(result.Succeeded, Is.True, result.Message);

            Assert.That(catalog.events, Has.Length.EqualTo(43));
            Assert.That(catalog.chains, Has.Length.EqualTo(5));
            Assert.That(catalog.events.Select(definition => definition.eventId).Distinct().Count(),
                Is.EqualTo(43));
            Assert.That(catalog.events.Count(
                    definition => definition.eventId.StartsWith("event.sc018.", StringComparison.Ordinal)),
                Is.EqualTo(7));
        }

        [Test]
        public void FiveAuthoredChainsAndPriorEventDefinitionsAreUnchanged()
        {
            EventCatalogData catalog = LoadCatalog();

            CollectionAssert.AreEqual(
                new[]
                {
                    "event.rainy-arrival",
                    "event.rainy-arrival.legacy-honored",
                    "event.rainy-arrival.legacy-deferred"
                },
                FindChain(catalog, "chain.rainy-arrival").linkedEventIds);
            CollectionAssert.AreEqual(
                new[] { "event.sc013.inheritance-pledge", "event.sc013.finale" },
                FindChain(catalog, "chain.sc013.main").linkedEventIds);
            CollectionAssert.AreEqual(
                new[]
                {
                    "event.sc013.refugee-arrival",
                    "event.sc013.refugee-loyalty",
                    "event.sc013.refugee-aid",
                    "event.sc013.refugee-rival"
                },
                FindChain(catalog, "chain.sc013.refugee-legacy").linkedEventIds);
            CollectionAssert.AreEqual(
                new[]
                {
                    "event.sc013.family-letters.first",
                    "event.sc013.family-letters.second-open",
                    "event.sc013.family-letters.second-sealed",
                    "event.sc013.family-letters.third-full-public",
                    "event.sc013.family-letters.third-short-history",
                    "event.sc013.family-letters.third-opened-by-heir",
                    "event.sc013.family-letters.third-sealed"
                },
                FindChain(catalog, "chain.sc013.family-letters").linkedEventIds);
            CollectionAssert.AreEqual(
                new[]
                {
                    "event.sc013.commission-legacy.first",
                    "event.sc013.commission-legacy.second-kept",
                    "event.sc013.commission-legacy.second-broken",
                    "event.sc013.commission-legacy.third-safe",
                    "event.sc013.commission-legacy.third-public",
                    "event.sc013.commission-legacy.third-cleared",
                    "event.sc013.commission-legacy.third-severed"
                },
                FindChain(catalog, "chain.sc013.commission-legacy").linkedEventIds);

            Assert.That(catalog.events.Count(definition =>
                    !definition.eventId.StartsWith("event.sc018.", StringComparison.Ordinal) &&
                    !definition.eventId.StartsWith("event.m08c.", StringComparison.Ordinal)),
                Is.EqualTo(26),
                "the prior accepted event baseline must stay unchanged");

            foreach (string row in BaselineEvents)
            {
                string[] parts = row.Split('|');
                EventDefinitionData definition = Find(catalog, parts[0]);
                Assert.That(definition.chainId ?? string.Empty, Is.EqualTo(parts[1]), parts[0]);
                Assert.That(definition.priority, Is.EqualTo(int.Parse(parts[2])), parts[0]);
                Assert.That(definition.weight, Is.EqualTo(int.Parse(parts[3])), parts[0]);
                Assert.That(definition.options, Has.Length.EqualTo(int.Parse(parts[4])), parts[0]);
                Assert.That(definition.conditions ?? new EventConditionData[0],
                    Has.Length.EqualTo(int.Parse(parts[5])), parts[0]);
                Assert.That(definition.textId, Is.EqualTo(definition.eventId), parts[0]);
            }
        }

        [Test]
        public void NewEventsAreChainlessMonthlyPerGameWithExpectedGates()
        {
            EventCatalogData catalog = LoadCatalog();

            foreach (string eventId in NewEventIds)
            {
                EventDefinitionData definition = Find(catalog, eventId);
                Assert.That(definition.trigger, Is.EqualTo("Monthly"), eventId);
                Assert.That(definition.onceScope, Is.EqualTo("PerGame"), eventId);
                Assert.That(definition.chainId, Is.Null.Or.Empty, eventId);
                Assert.That(definition.scenario, Is.EqualTo(EventScenarioIds.Sc013), eventId);
                Assert.That(definition.cooldownMonths, Is.Zero, eventId);
                Assert.That(definition.weight, Is.GreaterThan(0), eventId);
                Assert.That(definition.options, Has.Length.EqualTo(2), eventId);
                Assert.That(definition.conditions ?? new EventConditionData[0], Has.None.Matches<EventConditionData>(
                    condition => condition.type == "EventOptionChosen"), eventId);

                bool gated = GatedEventIds.Contains(eventId);
                if (gated)
                {
                    Assert.That(definition.conditions, Has.Length.EqualTo(1), eventId);
                    Assert.That(definition.conditions[0].type, Is.EqualTo("SuccessionCountEquals"), eventId);
                    Assert.That(definition.conditions[0].threshold, Is.Zero, eventId);
                }
                else
                {
                    Assert.That(definition.conditions ?? new EventConditionData[0], Is.Empty, eventId);
                }

                foreach (EventChainData chain in catalog.chains)
                    Assert.That(chain.linkedEventIds, Has.No.Member(eventId), eventId);
            }
        }

        [Test]
        public void NewEventsTargetNonHeadFamilyMembersWithVisibleDeltas()
        {
            EventCatalogData catalog = LoadCatalog();
            var familyIds = new HashSet<string>(FamilyMemberIds().Select(id => id.Value));

            foreach (string eventId in NewEventIds)
            {
                EventDefinitionData definition = Find(catalog, eventId);
                bool gated = GatedEventIds.Contains(eventId);
                foreach (EventOptionData option in definition.options)
                {
                    Assert.That(option.effects, Is.Not.Null.And.Not.Empty, option.optionId);
                    foreach (EventEffectData effect in option.effects)
                    {
                        Assert.That(effect.type, Is.EqualTo("FamilyRelationshipDelta"), option.optionId);
                        Assert.That(string.IsNullOrWhiteSpace(effect.targetCharacterId), Is.True,
                            "Family effects must not carry an NPC target.");
                        Assert.That(PersonId.TryParse(effect.targetPersonId, out PersonId target), Is.True,
                            effect.targetPersonId);
                        Assert.That(familyIds.Contains(target.Value), Is.True, effect.targetPersonId);
                        Assert.That(target, Is.Not.EqualTo(M2FixedScenario.HarunobuId),
                            "The generation-one head cannot be a family-effect target.");
                        Assert.That(effect.delta, Is.Not.Zero, option.optionId);
                        Assert.That(effect.delta, Is.InRange(-100, 100), option.optionId);

                        bool becomesHead = target == M2FixedScenario.MasatsunaId ||
                                           target == M2FixedScenario.NobumasaId;
                        Assert.That(!becomesHead || gated, Is.True,
                            "An event targeting a future head must be limited to generation one.");
                    }
                }
            }
        }

        [Test]
        public void NewEventsHaveCompleteFamilyLocalizationWithoutNpcTargets()
        {
            EventCatalogData catalog = LoadCatalog();
            EventTextCatalog text = LoadText();

            Assert.That(text.entries.Select(entry => entry.textId).Distinct().Count(),
                Is.EqualTo(text.entries.Length));
            Assert.That(text.entries.Count(entry => entry.textId.Contains("sc018")), Is.EqualTo(35));

            string[] npcFragments =
            {
                "黒田", "黑田", "伊織", "伊织", "橘", "鈴", "铃", "森", "源左衛門", "源左卫门",
                "星野", "酒井", "次郎兵衛", "次郎兵卫"
            };

            foreach (string eventId in NewEventIds)
            {
                EventDefinitionData definition = Find(catalog, eventId);
                EventTextEntry eventText = FindText(text, definition.textId);
                Assert.That(eventText.title, Is.Not.Null.And.Not.Empty, eventId);
                Assert.That(eventText.body, Is.Not.Null.And.Not.Empty, eventId);
                AssertNoNpcName(eventText.body, npcFragments, eventId);

                foreach (EventOptionData option in definition.options)
                {
                    EventTextEntry optionText = FindText(text, option.textId);
                    Assert.That(optionText.title, Is.Not.Null.And.Not.Empty, option.optionId);
                    Assert.That(optionText.body, Is.Not.Null.And.Not.Empty, option.optionId);
                    AssertNoNpcName(optionText.body, npcFragments, option.optionId);

                    Assert.That(option.summaryTextId, Is.Not.Null.And.Not.Empty, option.optionId);
                    EventTextEntry summary = FindText(text, option.summaryTextId);
                    Assert.That(summary.title, Is.Not.Null.And.Not.Empty, option.optionId);
                    Assert.That(summary.body, Does.Contain("五点"), option.optionId);
                    Assert.That(summary.body.Contains("增加") || summary.body.Contains("下降"), Is.True,
                        "Summary must state a visible relationship change: " + option.optionId);
                    AssertNoNpcName(summary.body, npcFragments, option.optionId);
                    Assert.That(FamilyNames.Any(name => summary.body.Contains(name)), Is.True,
                        "Summary must name the affected family member: " + option.optionId);
                }
            }
        }

        private static void AssertNoNpcName(string value, string[] fragments, string owner)
        {
            foreach (string fragment in fragments)
                Assert.That(value.Contains(fragment), Is.False, owner + " must not mention NPC " + fragment);
        }

        [Test]
        public void OptionalFamilyEventsCannotOutrankMandatoryM2Events()
        {
            EventCatalogData catalog = LoadCatalog();
            EventTextCatalog text = LoadText();

            foreach (string eventId in NewEventIds)
            {
                EventDefinitionData definition = Find(catalog, eventId);
                foreach (string mandatoryId in MandatoryM2EventIds)
                    Assert.That(definition.priority, Is.LessThan(Find(catalog, mandatoryId).priority),
                        eventId + " vs " + mandatoryId);
            }

            var engine = new EventEngine(catalog, text);
            Assert.That(engine.IsReady, Is.True, engine.ValidationMessage);
            var eligible = engine.GetEligibleEventIds(
                new GameDate(1560, 5, 1), new CharacterRelationships(), new EventRuntimeState(),
                ContextFor(M2FixedScenario.HarunobuId));

            Assert.That(eligible, Is.Not.Empty);
            Assert.That(eligible[0], Is.EqualTo(SC013EventIds.PledgeEventId));
            foreach (string eventId in NewEventIds)
                Assert.That(eligible, Has.Member(eventId), eventId);
        }

        [Test]
        public void NewEventsAreGenerationGatedWhereTheirTargetsBecomeHead()
        {
            EventCatalogData catalog = LoadCatalog();
            EventTextCatalog text = LoadText();
            var engine = new EventEngine(catalog, text);
            var relationships = new CharacterRelationships();
            var state = new EventRuntimeState();

            var generationTwo = engine.GetEligibleEventIds(
                new GameDate(1560, 5, 1), relationships, state,
                ContextFor(M2FixedScenario.MasatsunaId, 1, 1));
            foreach (string eventId in GatedEventIds)
                Assert.That(generationTwo, Has.No.Member(eventId), eventId);
            Assert.That(generationTwo, Has.Member("event.sc018.sibling-duty"));
            Assert.That(generationTwo, Has.Member("event.sc018.parent-care"));
            Assert.That(generationTwo, Has.Member("event.sc018.spouse-visit"));
            Assert.That(generationTwo, Has.Member("event.sc018.adoption-welcome"));

            var generationThree = engine.GetEligibleEventIds(
                new GameDate(1560, 5, 1), relationships, state,
                ContextFor(M2FixedScenario.NobumasaId, 2, 2));
            foreach (string eventId in GatedEventIds)
                Assert.That(generationThree, Has.No.Member(eventId), eventId);
            Assert.That(generationThree, Has.Member("event.sc018.sibling-duty"));
            Assert.That(generationThree, Has.Member("event.sc018.parent-care"));
            Assert.That(generationThree, Has.Member("event.sc018.spouse-visit"));
            Assert.That(generationThree, Has.Member("event.sc018.adoption-welcome"));
        }

        [Test]
        public void GenerationAdaptiveEventTextAvoidsHeadRelativeRoleClaims()
        {
            EventCatalogData catalog = LoadCatalog();
            EventTextCatalog text = LoadText();

            foreach (string eventId in AdaptiveNarrativeEventIds)
            {
                EventDefinitionData definition = Find(catalog, eventId);
                EventTextEntry eventText = FindText(text, definition.textId);
                var values = new List<string> { eventText.title, eventText.body };
                foreach (EventOptionData option in definition.options)
                {
                    EventTextEntry optionText = FindText(text, option.textId);
                    values.Add(optionText.title);
                    values.Add(optionText.body);
                    EventTextEntry summary = FindText(text, option.summaryTextId);
                    values.Add(summary.title);
                    values.Add(summary.body);
                }

                foreach (string value in values)
                    foreach (string fragment in HeadRelativeRoleFragments)
                        Assert.That(value.Contains(fragment), Is.False,
                            eventId + " must not assert the head-relative role '" + fragment +
                            "' in generation two or three: " + value);
            }

            Assert.That(FindText(text, "event.sc018.sibling-duty").title, Does.Contain("家人"),
                "sibling-duty must use generation-neutral household wording");
            Assert.That(FindText(text, "event.sc018.spouse-visit").title, Does.Contain("探亲"),
                "spouse-visit must use generation-neutral family-visit wording");
            Assert.That(FindText(text, "event.sc018.child-study").body, Does.Not.Contain("孩子"),
                "child-study must not call the generation-one grandson a child");
            Assert.That(FindText(text, "event.sc018.parent-care").body, Does.Not.Contain("长辈"),
                "parent-care must not call the generation-one spouse an elder");
        }

        [Test]
        public void FamilyRelationshipsKeyByHeadAndPreserveOldHeadRecords()
        {
            var relationships = FamilyRelationships.CreateEmpty();
            FamilyRelationshipResult first =
                relationships.TryChange(M2FixedScenario.HarunobuId, M2FixedScenario.TokihimeId, 5);

            Assert.That(first.Succeeded, Is.True, first.Error);
            Assert.That(first.PreviousValue, Is.Zero);
            Assert.That(first.CurrentValue, Is.EqualTo(5));
            Assert.That(relationships.TryGet(M2FixedScenario.HarunobuId, M2FixedScenario.TokihimeId)
                .CurrentValue, Is.EqualTo(5));

            // A new head starts at zero and never inherits the outgoing head's value.
            Assert.That(relationships.TryGet(M2FixedScenario.MasatsunaId, M2FixedScenario.TokihimeId)
                .CurrentValue, Is.Zero);
            FamilyRelationshipResult nextHead =
                relationships.TryChange(M2FixedScenario.MasatsunaId, M2FixedScenario.TokihimeId, -3);
            Assert.That(nextHead.Succeeded, Is.True, nextHead.Error);
            Assert.That(relationships.TryGet(M2FixedScenario.HarunobuId, M2FixedScenario.TokihimeId)
                .CurrentValue, Is.EqualTo(5), "old-head records must remain attached to the old head");
            Assert.That(relationships.TryGet(M2FixedScenario.MasatsunaId, M2FixedScenario.TokihimeId)
                .CurrentValue, Is.EqualTo(-3));

            Assert.That(relationships.TryChange(M2FixedScenario.HarunobuId,
                M2FixedScenario.HarunobuId, 5).Succeeded, Is.False);
            Assert.That(relationships.TryChange(M2FixedScenario.HarunobuId, default, 5).Succeeded, Is.False);

            Assert.That(relationships.TryChange(M2FixedScenario.HarunobuId, M2FixedScenario.ChieId, 200)
                .CurrentValue, Is.EqualTo(FamilyRelationships.MaximumValue));
            Assert.That(relationships.TryChange(M2FixedScenario.HarunobuId, M2FixedScenario.ChieId, -500)
                .CurrentValue, Is.EqualTo(FamilyRelationships.MinimumValue));
        }

        [Test]
        public void SuccessionHandoffKeepsOldHeadPairsAndNewHeadStartsAtZero()
        {
            SuccessionState state = M2FixedScenario.CreateSuccession();
            var relationships = FamilyRelationships.CreateEmpty();
            Assert.That(relationships.TryChange(M2FixedScenario.HarunobuId,
                M2FixedScenario.TokihimeId, 10).Succeeded, Is.True);

            SuccessionResult handoff = SuccessionService.BeginRetirement(
                state, new GameDate(1560, 5, 1));

            Assert.That(handoff.Succeeded, Is.True, handoff.Message);
            Assert.That(state.HeadPersonId, Is.EqualTo(M2FixedScenario.MasatsunaId));
            Assert.That(state.World.Family.IsMember(M2FixedScenario.HarunobuId), Is.True,
                "a retired head remains addressable so old pair records stay valid");
            Assert.That(relationships.TryGet(M2FixedScenario.HarunobuId,
                M2FixedScenario.TokihimeId).CurrentValue, Is.EqualTo(10));
            Assert.That(relationships.TryGet(state.HeadPersonId,
                M2FixedScenario.TokihimeId).CurrentValue, Is.Zero,
                "the incoming head owns a separate zero record");
        }

        [Test]
        public void FamilyRelationshipEffectsApplyAndRejectInvalidTargetsTransactionally()
        {
            var context = ContextFor(M2FixedScenario.HarunobuId);
            var family = FamilyRelationships.CreateEmpty();
            var npc = new CharacterRelationships();
            var state = new EventRuntimeState();
            EventEngine engine = CreateEngine("event.test.family",
                new[]
                {
                    new EventEffectData
                    {
                        type = "FamilyRelationshipDelta",
                        targetPersonId = M2FixedScenario.TokihimeId.Value,
                        delta = 5
                    }
                });

            EventScheduleResult scheduled = Schedule(engine, state, context);
            Assert.That(scheduled.Succeeded, Is.True, scheduled.Message);
            EventChoiceResult chosen = engine.TryChooseEventOption("event.test.family",
                "option.event.test.family", "result.test.family", new GameDate(1560, 5, 1),
                npc, state, context, family);
            Assert.That(chosen.Succeeded, Is.True, chosen.Message);
            Assert.That(chosen.AppliedRelationships, Is.Empty);
            Assert.That(chosen.AppliedFamilyRelationships, Has.Count.EqualTo(1));
            Assert.That(family.TryGet(M2FixedScenario.HarunobuId, M2FixedScenario.TokihimeId)
                .CurrentValue, Is.EqualTo(5));
            Assert.That(npc.TryGet(CharacterId.KurodaIori).CurrentValue, Is.Zero);

            var selfState = new EventRuntimeState();
            EventEngine selfEngine = CreateEngine("event.test.self",
                new[]
                {
                    new EventEffectData
                    {
                        type = "FamilyRelationshipDelta",
                        targetPersonId = M2FixedScenario.HarunobuId.Value,
                        delta = 5
                    }
                });
            // A scene whose only effect addresses the current head is never offered: the
            // option cannot be applied, so advertising it would only promise a rejection.
            EventScheduleResult selfOffer = Schedule(selfEngine, new EventRuntimeState(), context);
            Assert.That(selfOffer.Succeeded, Is.True);
            Assert.That(selfOffer.SelectedEventId, Is.Empty,
                "an option that targets the current head must not be advertised");
            Assert.That(selfState.HasPendingEvents, Is.False);

            // The commit guard still rejects a head target when the head changes after the
            // scene was offered under the previous head.
            EventRuleContext offerContext = ContextFor(M2FixedScenario.MasatsunaId);
            Assert.That(Schedule(selfEngine, selfState, offerContext).Succeeded, Is.True);
            Assert.That(selfState.PendingEventId, Is.EqualTo("event.test.self"));
            EventChoiceResult self = selfEngine.TryChooseEventOption("event.test.self",
                "option.event.test.self", "result.test.self", new GameDate(1560, 5, 1),
                npc, selfState, context, family);
            Assert.That(self.Succeeded, Is.False);
            Assert.That(self.Message, Does.Contain("家主本人"));
            Assert.That(selfState.HasPendingEvents, Is.True);

            var outsiderState = new EventRuntimeState();
            EventEngine outsiderEngine = CreateEngine("event.test.outsider",
                new[]
                {
                    new EventEffectData
                    {
                        type = "FamilyRelationshipDelta",
                        targetPersonId = "person.sc013.outsider",
                        delta = 5
                    }
                });
            // A target that is not a current family member is not advertised either.
            EventScheduleResult outsiderOffer = Schedule(outsiderEngine, new EventRuntimeState(),
                context);
            Assert.That(outsiderOffer.Succeeded, Is.True);
            Assert.That(outsiderOffer.SelectedEventId, Is.Empty,
                "an option that targets a non-member must not be advertised");

            // A target that leaves the roster between offer and commit is still rejected at
            // commit, so the transactional guard stays covered.
            var memberState = new EventRuntimeState();
            EventEngine memberEngine = CreateEngine("event.test.member-gone",
                new[]
                {
                    new EventEffectData
                    {
                        type = "FamilyRelationshipDelta",
                        targetPersonId = M2FixedScenario.TokihimeId.Value,
                        delta = 5
                    }
                });
            Assert.That(Schedule(memberEngine, memberState, context).Succeeded, Is.True);
            Assert.That(memberState.PendingEventId, Is.EqualTo("event.test.member-gone"));
            EventRuleContext rosterWithoutTarget = new EventRuleContext(0, 0,
                EventScenarioIds.Sc013, M2FixedScenario.HarunobuId,
                FamilyMemberIds().Where(id => id != M2FixedScenario.TokihimeId).ToArray());
            EventChoiceResult outsider = memberEngine.TryChooseEventOption("event.test.member-gone",
                "option.event.test.member-gone", "result.test.member-gone", new GameDate(1560, 5, 1),
                npc, memberState, rosterWithoutTarget, family);
            Assert.That(outsider.Succeeded, Is.False);
            Assert.That(outsider.Message, Does.Contain("不是当前家庭成员"));

            var partialState = new EventRuntimeState();
            var partialStore = FamilyRelationships.CreateEmpty();
            EventEngine partialEngine = CreateEngine("event.test.partial",
                new[]
                {
                    new EventEffectData
                    {
                        type = "FamilyRelationshipDelta",
                        targetPersonId = M2FixedScenario.TokihimeId.Value,
                        delta = 5
                    },
                    new EventEffectData
                    {
                        type = "FamilyRelationshipDelta",
                        targetPersonId = M2FixedScenario.HarunobuId.Value,
                        delta = 5
                    }
                });
            Assert.That(Schedule(partialEngine, partialState, offerContext).Succeeded, Is.True);
            Assert.That(partialState.PendingEventId, Is.EqualTo("event.test.partial"));
            EventChoiceResult partial = partialEngine.TryChooseEventOption("event.test.partial",
                "option.event.test.partial", "result.test.partial", new GameDate(1560, 5, 1),
                npc, partialState, context, partialStore);
            Assert.That(partial.Succeeded, Is.False);
            Assert.That(partialStore.TryGet(M2FixedScenario.HarunobuId, M2FixedScenario.TokihimeId)
                .CurrentValue, Is.Zero, "a failed preflight must not leave a partial family delta");
        }

        [Test]
        public void FamilyEventDeltaClampsAtBothBoundsAndReportsActualValues()
        {
            var context = ContextFor(M2FixedScenario.HarunobuId);
            var npc = new CharacterRelationships();

            var upperStore = FamilyRelationships.CreateEmpty();
            Assert.That(upperStore.TryChange(M2FixedScenario.HarunobuId, M2FixedScenario.TokihimeId,
                FamilyRelationships.MaximumValue - 2).Succeeded, Is.True);
            var upperState = new EventRuntimeState();
            EventEngine upperEngine = CreateEngine("event.test.clamp-upper", new[]
            {
                new EventEffectData
                {
                    type = "FamilyRelationshipDelta",
                    targetPersonId = M2FixedScenario.TokihimeId.Value,
                    delta = 5
                }
            });
            Assert.That(Schedule(upperEngine, upperState, context).Succeeded, Is.True);
            EventChoiceResult upper = upperEngine.TryChooseEventOption("event.test.clamp-upper",
                "option.event.test.clamp-upper", "result.test.clamp-upper", new GameDate(1560, 5, 1),
                npc, upperState, context, upperStore);
            Assert.That(upper.Succeeded, Is.True, upper.Message);
            Assert.That(upper.AppliedFamilyRelationships, Has.Count.EqualTo(1));
            Assert.That(upper.AppliedFamilyRelationships[0].PreviousValue,
                Is.EqualTo(FamilyRelationships.MaximumValue - 2));
            Assert.That(upper.AppliedFamilyRelationships[0].CurrentValue,
                Is.EqualTo(FamilyRelationships.MaximumValue),
                "the upper boundary must clamp and report the reached value");
            Assert.That(upper.AppliedFamilyRelationships[0].CurrentValue -
                        upper.AppliedFamilyRelationships[0].PreviousValue, Is.EqualTo(2),
                "the applied delta must be the actual clamped change, not the requested +5");
            Assert.That(upperStore.TryGet(M2FixedScenario.HarunobuId, M2FixedScenario.TokihimeId)
                .CurrentValue, Is.EqualTo(FamilyRelationships.MaximumValue));

            var lowerStore = FamilyRelationships.CreateEmpty();
            Assert.That(lowerStore.TryChange(M2FixedScenario.HarunobuId, M2FixedScenario.TokihimeId,
                FamilyRelationships.MinimumValue + 2).Succeeded, Is.True);
            var lowerState = new EventRuntimeState();
            EventEngine lowerEngine = CreateEngine("event.test.clamp-lower", new[]
            {
                new EventEffectData
                {
                    type = "FamilyRelationshipDelta",
                    targetPersonId = M2FixedScenario.TokihimeId.Value,
                    delta = -5
                }
            });
            Assert.That(Schedule(lowerEngine, lowerState, context).Succeeded, Is.True);
            EventChoiceResult lower = lowerEngine.TryChooseEventOption("event.test.clamp-lower",
                "option.event.test.clamp-lower", "result.test.clamp-lower", new GameDate(1560, 5, 1),
                npc, lowerState, context, lowerStore);
            Assert.That(lower.Succeeded, Is.True, lower.Message);
            Assert.That(lower.AppliedFamilyRelationships, Has.Count.EqualTo(1));
            Assert.That(lower.AppliedFamilyRelationships[0].PreviousValue,
                Is.EqualTo(FamilyRelationships.MinimumValue + 2));
            Assert.That(lower.AppliedFamilyRelationships[0].CurrentValue,
                Is.EqualTo(FamilyRelationships.MinimumValue));
            Assert.That(lower.AppliedFamilyRelationships[0].CurrentValue -
                        lower.AppliedFamilyRelationships[0].PreviousValue, Is.EqualTo(-2));
            Assert.That(lowerStore.TryGet(M2FixedScenario.HarunobuId, M2FixedScenario.TokihimeId)
                .CurrentValue, Is.EqualTo(FamilyRelationships.MinimumValue));

            // A pair already at the boundary succeeds without inventing a chronicle delta.
            var fullStore = FamilyRelationships.CreateEmpty();
            Assert.That(fullStore.TryChange(M2FixedScenario.HarunobuId, M2FixedScenario.TokihimeId,
                FamilyRelationships.MaximumValue).Succeeded, Is.True);
            var fullState = new EventRuntimeState();
            EventEngine fullEngine = CreateEngine("event.test.clamp-full", new[]
            {
                new EventEffectData
                {
                    type = "FamilyRelationshipDelta",
                    targetPersonId = M2FixedScenario.TokihimeId.Value,
                    delta = 5
                }
            });
            Assert.That(Schedule(fullEngine, fullState, context).Succeeded, Is.True);
            EventChoiceResult full = fullEngine.TryChooseEventOption("event.test.clamp-full",
                "option.event.test.clamp-full", "result.test.clamp-full", new GameDate(1560, 5, 1),
                npc, fullState, context, fullStore);
            Assert.That(full.Succeeded, Is.True, full.Message);
            Assert.That(full.AppliedFamilyRelationships, Is.Empty,
                "a clamped no-op must not report a fabricated before/after change");

            // Two effects on the same pair clamp once and report the exact net change.
            var repeatStore = FamilyRelationships.CreateEmpty();
            Assert.That(repeatStore.TryChange(M2FixedScenario.HarunobuId, M2FixedScenario.TokihimeId,
                FamilyRelationships.MaximumValue - 4).Succeeded, Is.True);
            var repeatState = new EventRuntimeState();
            EventEngine repeatEngine = CreateEngine("event.test.clamp-repeat", new[]
            {
                new EventEffectData
                {
                    type = "FamilyRelationshipDelta",
                    targetPersonId = M2FixedScenario.TokihimeId.Value,
                    delta = 5
                },
                new EventEffectData
                {
                    type = "FamilyRelationshipDelta",
                    targetPersonId = M2FixedScenario.TokihimeId.Value,
                    delta = 5
                }
            });
            Assert.That(Schedule(repeatEngine, repeatState, context).Succeeded, Is.True);
            EventChoiceResult repeat = repeatEngine.TryChooseEventOption("event.test.clamp-repeat",
                "option.event.test.clamp-repeat", "result.test.clamp-repeat", new GameDate(1560, 5, 1),
                npc, repeatState, context, repeatStore);
            Assert.That(repeat.Succeeded, Is.True, repeat.Message);
            Assert.That(repeat.AppliedFamilyRelationships, Has.Count.EqualTo(1),
                "a second effect that cannot move a clamped pair must not add a second chronicle row");
            Assert.That(repeat.AppliedFamilyRelationships[0].PreviousValue,
                Is.EqualTo(FamilyRelationships.MaximumValue - 4));
            Assert.That(repeat.AppliedFamilyRelationships[0].CurrentValue,
                Is.EqualTo(FamilyRelationships.MaximumValue));
        }

        [Test]
        public void NpcRelationshipEffectBehaviorIsPreserved()
        {
            var context = ContextFor(M2FixedScenario.HarunobuId);
            var family = FamilyRelationships.CreateEmpty();
            var npc = new CharacterRelationships();
            var state = new EventRuntimeState();
            EventEngine engine = CreateEngine("event.test.npc",
                new[]
                {
                    new EventEffectData
                    {
                        type = "RelationshipDelta",
                        targetCharacterId = CharacterId.KurodaIori.Value,
                        delta = 5
                    }
                });

            Assert.That(Schedule(engine, state, context).Succeeded, Is.True);
            EventChoiceResult chosen = engine.TryChooseEventOption("event.test.npc",
                "option.event.test.npc", "result.test.npc", new GameDate(1560, 5, 1),
                npc, state, context, family);
            Assert.That(chosen.Succeeded, Is.True, chosen.Message);
            Assert.That(chosen.AppliedRelationships, Has.Count.EqualTo(1));
            Assert.That(chosen.AppliedFamilyRelationships, Is.Empty);
            Assert.That(npc.TryGet(CharacterId.KurodaIori).CurrentValue, Is.EqualTo(5));
        }

        [Test]
        public void V4PresenceRequiresChronicleAndCurrentVersionRequiresFamilyRelationships()
        {
            var probe = new SaveDataPresenceProbe
            {
                saveFormatVersion = 4,
                gameVersion = "0.1.0",
                rulesVersion = 1,
                eventContentVersion = 2,
                randomState = new RandomStateSaveData(),
                eventRuntime = new EventRuntimeSaveData(),
                succession = SuccessionSaveData.CreateDefault(),
                chronicle = new ChronicleSaveEntry[0]
            };
            const string withoutChronicle =
                "{\"saveFormatVersion\":4,\"gameVersion\":\"0.1.0\",\"rulesVersion\":1," +
                "\"eventContentVersion\":2,\"randomState\":{},\"eventRuntime\":{}," +
                "\"succession\":{}}";
            SaveOperationResult missing =
                SaveDataValidator.ValidatePresence(probe, withoutChronicle);
            Assert.That(missing.Succeeded, Is.False);
            Assert.That(missing.Message, Does.Contain("chronicle"));

            const string withChronicle =
                "{\"saveFormatVersion\":4,\"gameVersion\":\"0.1.0\",\"rulesVersion\":1," +
                "\"eventContentVersion\":2,\"randomState\":{},\"eventRuntime\":{}," +
                "\"succession\":{},\"chronicle\":[]}";
            SaveOperationResult accepted =
                SaveDataValidator.ValidatePresence(probe, withChronicle);
            Assert.That(accepted.Succeeded, Is.True, accepted.Message);

            var currentProbe = new SaveDataPresenceProbe
            {
                saveFormatVersion = SaveDataValidator.CurrentVersion,
                gameVersion = "0.1.0",
                rulesVersion = 1,
                eventContentVersion = 2,
                randomState = new RandomStateSaveData(),
                eventRuntime = new EventRuntimeSaveData(),
                succession = SuccessionSaveData.CreateDefault(),
                chronicle = new ChronicleSaveEntry[0]
            };
            const string currentWithoutFamily =
                "{\"saveFormatVersion\":5,\"gameVersion\":\"0.1.0\",\"rulesVersion\":1," +
                "\"eventContentVersion\":2,\"randomState\":{},\"eventRuntime\":{}," +
                "\"succession\":{},\"chronicle\":[]}";
            SaveOperationResult missingFamily =
                SaveDataValidator.ValidatePresence(currentProbe, currentWithoutFamily);
            Assert.That(missingFamily.Succeeded, Is.False);
            Assert.That(missingFamily.Message, Does.Contain("familyRelationships"));
        }

        [Test]
        public void FamilyRelationshipSaveRoundTripsAndV4MigrationAddsNoValues()
        {
            var session = GameSession.NewGame();
            _sessions.Add(session);
            Assert.That(session.TryChangeFamilyRelationship(M2FixedScenario.TokihimeId, 5).Succeeded,
                Is.True);

            SaveData data = session.CaptureSave();
            Assert.That(data.saveFormatVersion, Is.EqualTo(SaveDataValidator.CurrentVersion));
            Assert.That(data.familyRelationships, Has.Length.EqualTo(1));
            Assert.That(data.familyRelationships[0].headPersonId,
                Is.EqualTo(M2FixedScenario.HarunobuId.Value));
            Assert.That(data.familyRelationships[0].memberPersonId,
                Is.EqualTo(M2FixedScenario.TokihimeId.Value));
            Assert.That(data.familyRelationships[0].value, Is.EqualTo(5));
            Assert.That(SaveDataValidator.Validate(data).Succeeded, Is.True);

            var loaded = GameSession.NewGame();
            _sessions.Add(loaded);
            Assert.That(loaded.TryApplySave(data).Succeeded, Is.True);
            Assert.That(loaded.TryGetFamilyRelationship(M2FixedScenario.HarunobuId,
                M2FixedScenario.TokihimeId).CurrentValue, Is.EqualTo(5));
            Assert.That(loaded.TryGetFamilyRelationship(M2FixedScenario.MasatsunaId,
                M2FixedScenario.TokihimeId).CurrentValue, Is.Zero,
                "load must preserve pair keys and not copy an old head's value to the new head");

            SaveData legacy = session.CaptureSave();
            legacy.saveFormatVersion = 4;
            legacy.familyRelationships = new FamilyRelationshipSaveEntry[0];
            Assert.That(SaveDataMigrator.TryMigrate(legacy, out SaveData migrated,
                out SaveOperationResult result), Is.True, result.Message);
            Assert.That(migrated.saveFormatVersion, Is.EqualTo(SaveDataValidator.CurrentVersion));
            Assert.That(migrated.familyRelationships, Is.Empty,
                "v4 migration must not infer family relationship values");

            SaveData v3 = session.CaptureSave();
            v3.saveFormatVersion = 3;
            v3.chronicle = null;
            v3.familyRelationships = null;
            Assert.That(SaveDataMigrator.TryMigrate(v3, out SaveData v3Migrated,
                out SaveOperationResult v3Result), Is.True, v3Result.Message);
            Assert.That(v3Migrated.familyRelationships, Is.Empty,
                "v3 migration must keep the documented empty family-relationship set");

            SaveData invalid = session.CaptureSave();
            invalid.familyRelationships[0].memberPersonId = invalid.familyRelationships[0].headPersonId;
            Assert.That(SaveDataValidator.Validate(invalid).Succeeded, Is.False);

            SaveData unknownMember = session.CaptureSave();
            unknownMember.familyRelationships[0].memberPersonId = "person.sc013.outsider";
            Assert.That(SaveDataValidator.Validate(unknownMember).Succeeded, Is.False,
                "a save pair must reference an active family member");

            SaveData duplicatePair = session.CaptureSave();
            FamilyRelationshipSaveEntry firstPair = duplicatePair.familyRelationships[0];
            duplicatePair.familyRelationships = new[]
            {
                firstPair,
                new FamilyRelationshipSaveEntry
                {
                    headPersonId = firstPair.headPersonId,
                    memberPersonId = firstPair.memberPersonId,
                    value = 1
                }
            };
            Assert.That(SaveDataValidator.Validate(duplicatePair).Succeeded, Is.False,
                "duplicate person pairs must be rejected");

            SaveData outOfRange = session.CaptureSave();
            outOfRange.familyRelationships[0].value = FamilyRelationships.MaximumValue + 1;
            Assert.That(SaveDataValidator.Validate(outOfRange).Succeeded, Is.False,
                "family relationship values must stay in range");

            SaveData v4WithValues = session.CaptureSave();
            v4WithValues.saveFormatVersion = 4;
            Assert.That(SaveDataMigrator.TryMigrate(v4WithValues, out _,
                out SaveOperationResult v4Result), Is.False);
            Assert.That(v4Result.Message, Does.Contain("familyRelationships"));
        }

        [Test]
        public void GameSessionRejectsSelfAndNonMemberFamilyTargets()
        {
            var session = GameSession.NewGame();
            _sessions.Add(session);
            int chronicleCount = session.ChronicleEntries.Count;

            FamilyRelationshipResult self =
                session.TryChangeFamilyRelationship(M2FixedScenario.HarunobuId, 5);
            Assert.That(self.Succeeded, Is.False);
            Assert.That(self.Error, Does.Contain("家主本人"));

            FamilyRelationshipResult outsider =
                session.TryChangeFamilyRelationship(new PersonId("person.sc013.outsider"), 5);
            Assert.That(outsider.Succeeded, Is.False);
            Assert.That(outsider.Error, Does.Contain("不是当前家庭成员"));

            FamilyRelationshipResult historicalHead = session.TryChangeFamilyRelationship(
                M2FixedScenario.MasatsunaId, M2FixedScenario.TokihimeId, 5);
            Assert.That(historicalHead.Succeeded, Is.False);
            Assert.That(historicalHead.Error, Does.Contain("当前家主"));

            Assert.That(session.ChronicleEntries.Count, Is.EqualTo(chronicleCount),
                "rejected family changes must not create chronicle records");
        }

        [Test]
        public void FamilyRelationshipSlotRoundTripPreservesPairAndChronicle()
        {
            string directory = Path.Combine(Path.GetTempPath(), "SengokuChronicleTests",
                Guid.NewGuid().ToString("N"));
            try
            {
                var store = new SaveSlotStore(directory);
                var session = GameSession.NewGame();
                _sessions.Add(session);
                Assert.That(session.TryChangeFamilyRelationship(M2FixedScenario.TokihimeId, 5).Succeeded,
                    Is.True);
                SaveOperationResult saved = session.SaveTo(store);
                Assert.That(saved.Succeeded, Is.True, saved.Message);

                var loaded = GameSession.NewGame();
                _sessions.Add(loaded);
                SaveOperationResult restored = loaded.LoadFrom(store);
                Assert.That(restored.Succeeded, Is.True, restored.Message);
                Assert.That(loaded.TryGetFamilyRelationship(M2FixedScenario.HarunobuId,
                    M2FixedScenario.TokihimeId).CurrentValue, Is.EqualTo(5));

                FamilyChronicleEntry entry = loaded.ChronicleEntries.Last();
                Assert.That(entry.Category, Is.EqualTo(ChronicleCategory.Relationship));
                Assert.That(entry.Title, Does.Contain("明智 晴信"));
                Assert.That(entry.Title, Does.Contain("明智 時姫"));
                Assert.That(entry.Summary, Does.Contain("0 → 5"));
            }
            finally
            {
                if (Directory.Exists(directory))
                    Directory.Delete(directory, true);
            }
        }

        [Test]
        public void FamilyChronicleRecordsBothNamesAndBeforeAfterValues()
        {
            var session = GameSession.NewGame();
            _sessions.Add(session);

            Assert.That(session.TryChangeFamilyRelationship(M2FixedScenario.TokihimeId, 5).Succeeded,
                Is.True);
            FamilyChronicleEntry first = session.ChronicleEntries.Last();
            Assert.That(first.Category, Is.EqualTo(ChronicleCategory.Relationship));
            Assert.That(first.Title, Does.Contain("明智 晴信"));
            Assert.That(first.Title, Does.Contain("明智 時姫"));
            Assert.That(first.Title, Does.Not.Contain("person.sc013."));
            Assert.That(first.Summary, Does.Contain("0 → 5"));
            Assert.That(first.FromValue, Is.Zero);
            Assert.That(first.ToValue, Is.EqualTo(5));

            Assert.That(session.TryChangeFamilyRelationship(M2FixedScenario.TokihimeId, 5).Succeeded,
                Is.True);
            FamilyChronicleEntry second = session.ChronicleEntries.Last();
            Assert.That(second.Summary, Does.Contain("5 → 10"));
            Assert.That(second.Title, Does.Contain("明智 晴信"));
            Assert.That(second.Title, Does.Contain("明智 時姫"));
        }

        [Test]
        public void FamilyChronicleRecordsActualClampedBoundaryValues()
        {
            var session = GameSession.NewGame();
            _sessions.Add(session);

            Assert.That(session.TryChangeFamilyRelationship(M2FixedScenario.TokihimeId,
                FamilyRelationships.MaximumValue - 2).Succeeded, Is.True);
            Assert.That(session.TryChangeFamilyRelationship(M2FixedScenario.TokihimeId, 5).Succeeded,
                Is.True);

            FamilyChronicleEntry boundary = session.ChronicleEntries.Last();
            Assert.That(boundary.Summary, Does.Contain("98 → 100"),
                "the chronicle must record the actual clamped before/after values");
            Assert.That(boundary.Summary, Does.Not.Contain("103"),
                "the chronicle must not record the requested overflow delta");
            Assert.That(boundary.FromValue, Is.EqualTo(FamilyRelationships.MaximumValue - 2));
            Assert.That(boundary.ToValue, Is.EqualTo(FamilyRelationships.MaximumValue));

            int count = session.ChronicleEntries.Count;
            Assert.That(session.TryChangeFamilyRelationship(M2FixedScenario.TokihimeId, 5).Succeeded,
                Is.True);
            Assert.That(session.ChronicleEntries.Count, Is.EqualTo(count),
                "a clamped no-op must not append a fake chronicle row");
        }

        [Test]
        public void FamilyEventPathChroniclesActualClampedValues()
        {
            const string eventId = "event.sc018.sibling-duty";
            const string optionId = "option.sc018.sibling-duty.let-rest";

            var session = GameSession.NewGame();
            _sessions.Add(session);
            SaveData data = session.CaptureSave();
            data.eventRuntime.phase = (int)StableGamePhase.AwaitingEventChoice;
            data.eventRuntime.pendingEventIds = new[] { eventId };
            Assert.That(session.TryApplySave(data).Succeeded, Is.True);
            Assert.That(session.PendingEventId, Is.EqualTo(eventId));
            Assert.That(session.TryChangeFamilyRelationship(M2FixedScenario.TokihimeId,
                FamilyRelationships.MaximumValue - 2).Succeeded, Is.True);

            EventChoiceResult result = session.TryChooseEventOption(eventId, optionId,
                session.CreateEventResultId(eventId, optionId));
            Assert.That(result.Succeeded, Is.True, result.Message);
            Assert.That(result.AppliedFamilyRelationships, Has.Count.EqualTo(1));
            Assert.That(result.AppliedFamilyRelationships[0].PreviousValue,
                Is.EqualTo(FamilyRelationships.MaximumValue - 2));
            Assert.That(result.AppliedFamilyRelationships[0].CurrentValue,
                Is.EqualTo(FamilyRelationships.MaximumValue));

            FamilyChronicleEntry entry = session.ChronicleEntries.Last();
            Assert.That(entry.Category, Is.EqualTo(ChronicleCategory.Relationship));
            Assert.That(entry.Summary, Does.Contain("98 → 100"),
                "the event path must chronicle the actual clamped before/after values");
            Assert.That(entry.Summary, Does.Not.Contain("103"));
        }
    }
}
