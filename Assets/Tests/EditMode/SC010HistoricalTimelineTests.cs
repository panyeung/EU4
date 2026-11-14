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
    /// M08C contract tests. The five year-level historical nodes each own a prior-year
    /// warning and a milestone row; both are acknowledgement-only, once per game, and
    /// SC-010 only. Scheduling dates are game calendar dates, not historical dates.
    /// </summary>
    public sealed class SC010HistoricalTimelineTests
    {
        private static readonly TimelineRow[] Timeline =
        {
            new TimelineRow(SC010EventIds.FirstIgaRumorEventId, SC010EventIds.FirstIgaRumorOptionId,
                SC010EventIds.FirstIgaNodeEventId, SC010EventIds.FirstIgaNodeOptionId, 1578, 1, 1579, 1),
            new TimelineRow(SC010EventIds.SecondIgaRumorEventId, SC010EventIds.SecondIgaRumorOptionId,
                SC010EventIds.SecondIgaNodeEventId, SC010EventIds.SecondIgaNodeOptionId, 1580, 1, 1581, 1),
            new TimelineRow(SC010EventIds.SekigaharaRumorEventId, SC010EventIds.SekigaharaRumorOptionId,
                SC010EventIds.SekigaharaNodeEventId, SC010EventIds.SekigaharaNodeOptionId, 1599, 1, 1600, 1),
            new TimelineRow(SC010EventIds.OsakaWinterRumorEventId, SC010EventIds.OsakaWinterRumorOptionId,
                SC010EventIds.OsakaWinterNodeEventId, SC010EventIds.OsakaWinterNodeOptionId, 1613, 1, 1614, 1),
            new TimelineRow(SC010EventIds.OsakaSummerRumorEventId, SC010EventIds.OsakaSummerRumorOptionId,
                SC010EventIds.OsakaSummerNodeEventId, SC010EventIds.OsakaSummerNodeOptionId, 1614, 2, 1615, 1)
        };

        private readonly struct TimelineRow
        {
            public TimelineRow(string rumorEventId, string rumorOptionId, string nodeEventId,
                string nodeOptionId, int warningYear, int warningMonth, int nodeYear, int nodeMonth)
            {
                RumorEventId = rumorEventId;
                RumorOptionId = rumorOptionId;
                NodeEventId = nodeEventId;
                NodeOptionId = nodeOptionId;
                WarningYear = warningYear;
                WarningMonth = warningMonth;
                NodeYear = nodeYear;
                NodeMonth = nodeMonth;
            }

            public string RumorEventId { get; }
            public string RumorOptionId { get; }
            public string NodeEventId { get; }
            public string NodeOptionId { get; }
            public int WarningYear { get; }
            public int WarningMonth { get; }
            public int NodeYear { get; }
            public int NodeMonth { get; }
        }

        private static EventCatalogData LoadCatalog() =>
            JsonUtility.FromJson<EventCatalogData>(
                File.ReadAllText("Assets/Resources/Events/event-catalog-v1.json"));

        private static EventTextCatalog LoadText() =>
            JsonUtility.FromJson<EventTextCatalog>(
                File.ReadAllText("Assets/Resources/Localization/event-text-zh-v1.json"));

        private static EventEngine LoadEngine()
        {
            EventEngine engine = new EventEngine(LoadCatalog(), LoadText());
            Assert.That(engine.IsReady, Is.True, engine.ValidationMessage);
            return engine;
        }

        private static EventRuleContext Sc010Context => new EventRuleContext(0, 0, SC010EventIds.ScenarioId);

        private static GameDate DateOf(int year, int month) => GameDate.Parse(year, month, 1);

        private static GameDate MonthOffset(GameDate date, int months)
        {
            int index = date.Year * 12 + date.Month - 1 + months;
            return DateOf(index / 12, index % 12 + 1);
        }

        [Test]
        public void Sc010Timeline_ExposesEachWarningAndMilestoneAtItsAuthoredDate()
        {
            EventEngine engine = LoadEngine();
            var relationships = new CharacterRelationships();
            var context = Sc010Context;

            foreach (TimelineRow row in Timeline)
            {
                var warningState = new EventRuntimeState();
                IReadOnlyList<string> warningMonth = engine.GetEligibleEventIds(
                    DateOf(row.WarningYear, row.WarningMonth), relationships, warningState, context);
                Assert.That(warningMonth, Does.Contain(row.RumorEventId),
                    $"{row.RumorEventId} must be offered in its warning month");
                Assert.That(warningMonth[0], Is.EqualTo(row.RumorEventId),
                    "the authored historical row outranks ordinary monthly content");
                Assert.That(warningMonth, Does.Not.Contain(row.NodeEventId),
                    "a node must not be offered in its warning month");
                Assert.That(engine.GetEligibleEventIds(
                        MonthOffset(DateOf(row.WarningYear, row.WarningMonth), -1), relationships,
                        warningState, context),
                    Does.Not.Contain(row.RumorEventId),
                    "the month before the warning must not offer it");
                Assert.That(engine.GetEligibleEventIds(
                        MonthOffset(DateOf(row.WarningYear, row.WarningMonth), 1), relationships,
                        warningState, context),
                    Does.Not.Contain(row.RumorEventId),
                    "the month after the warning must not offer it");

                var nodeState = new EventRuntimeState();
                IReadOnlyList<string> nodeMonth = engine.GetEligibleEventIds(
                    DateOf(row.NodeYear, row.NodeMonth), relationships, nodeState, context);
                Assert.That(nodeMonth, Does.Contain(row.NodeEventId),
                    $"{row.NodeEventId} must be offered in its node year");
                Assert.That(nodeMonth[0], Is.EqualTo(row.NodeEventId),
                    "the authored historical row outranks ordinary monthly content");
                Assert.That(engine.GetEligibleEventIds(
                        MonthOffset(DateOf(row.NodeYear, row.NodeMonth), -1), relationships,
                        nodeState, context),
                    Does.Not.Contain(row.NodeEventId),
                    "the month before the node must not offer it");
                Assert.That(engine.GetEligibleEventIds(
                        MonthOffset(DateOf(row.NodeYear, row.NodeMonth), 1), relationships,
                        nodeState, context),
                    Does.Not.Contain(row.NodeEventId),
                    "the month after the node must not offer it");
            }

            EventDefinitionData node = Find(engine, SC010EventIds.OsakaSummerNodeEventId);
            Assert.That(engine.TryGetText(node.textId, out EventTextEntry nodeText), Is.True);
            Assert.That(nodeText.body, Does.Contain("1615 年"),
                "the node text stays year-level");
            Assert.That(nodeText.body, Does.Not.Contain("月"),
                "the node text must not claim a precise historical month");
            foreach (TimelineRow row in Timeline)
            {
                EventDefinitionData rumor = Find(engine, row.RumorEventId);
                EventDefinitionData milestone = Find(engine, row.NodeEventId);
                Assert.That(rumor.options, Has.Length.EqualTo(1));
                Assert.That(milestone.options, Has.Length.EqualTo(1));
                Assert.That(rumor.options[0].effects, Is.Empty, "a warning has no effect");
                Assert.That(milestone.options[0].effects, Is.Empty, "a node has no effect");
                Assert.That(rumor.scenario, Is.EqualTo(SC010EventIds.ScenarioId));
                Assert.That(milestone.scenario, Is.EqualTo(SC010EventIds.ScenarioId));
                Assert.That(rumor.onceScope, Is.EqualTo(nameof(EventOnceScope.PerGame)));
                Assert.That(milestone.onceScope, Is.EqualTo(nameof(EventOnceScope.PerGame)));
                Assert.That(rumor.priority, Is.EqualTo(2000));
                Assert.That(milestone.priority, Is.EqualTo(2000));
                Assert.That(engine.TryGetEventText(row.RumorEventId, out EventTextEntry rumorText), Is.True);
                Assert.That(engine.TryGetEventText(row.NodeEventId, out EventTextEntry nodeTextEntry), Is.True);
                Assert.That(rumorText.body, Does.Not.Contain("月"),
                    $"{row.RumorEventId} must not claim a precise historical month");
                Assert.That(nodeTextEntry.body, Does.Not.Contain("月"),
                    $"{row.NodeEventId} must not claim a precise historical month");
            }
        }

        [Test]
        public void Sc010Timeline_NoAuthoredMonthContainsTwoRows()
        {
            EventCatalogData catalog = LoadCatalog();
            var months = new HashSet<int>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            int sc010Rows = 0;
            foreach (EventDefinitionData definition in catalog.events)
            {
                if (!string.Equals(definition.scenario, SC010EventIds.ScenarioId, StringComparison.Ordinal))
                    continue;
                sc010Rows++;
                Assert.That(definition.conditions, Has.Length.EqualTo(1),
                    $"{definition.eventId} must carry exactly one authored date");
                EventConditionData condition = definition.conditions[0];
                Assert.That(condition.type, Is.EqualTo(nameof(EventConditionKind.CampaignDateIs)));
                Assert.That(months.Add(condition.year * 12 + condition.month), Is.True,
                    $"{definition.eventId} shares its authored month with another timeline row");
                Assert.That(seen.Add(definition.eventId), Is.True);
            }

            Assert.That(sc010Rows, Is.EqualTo(10), "five warnings plus five milestone rows");
            CollectionAssert.AreEquivalent(
                Timeline.Select(row => row.RumorEventId).Concat(Timeline.Select(row => row.NodeEventId)).ToArray(),
                seen.ToArray());
        }

        [Test]
        public void Sc010Timeline_HistoricalEntryWinsAgainstOrdinaryEventOnSameDate()
        {
            EventDefinitionData ordinary = AlwaysEvent("event.ordinary", 10, 50);
            EventDefinitionData historical = DatedEvent("event.historical", 2000, 1, 1579, 1);
            EventEngine engine = CreateEngine(ordinary, historical);
            var context = new EventRuleContext(0, 0, SC010EventIds.ScenarioId);
            var relationships = new CharacterRelationships();

            for (ulong seed = 1; seed <= 20; seed++)
            {
                var state = new EventRuntimeState();
                EventScheduleResult scheduled = engine.ScheduleMonthly(DateOf(1579, 1), relationships,
                    RandomStateManager.Create(seed).GetStream(RandomStreamKind.Events), state, context);
                Assert.That(scheduled.Succeeded, Is.True, scheduled.Message);
                Assert.That(scheduled.SelectedEventId, Is.EqualTo("event.historical"),
                    "priority 2000 must decide the month before any weight roll");
            }

            var laterState = new EventRuntimeState();
            EventScheduleResult laterMonth = engine.ScheduleMonthly(DateOf(1579, 3), relationships,
                RandomStateManager.Create(7UL).GetStream(RandomStreamKind.Events), laterState, context);
            Assert.That(laterMonth.SelectedEventId, Is.EqualTo("event.ordinary"),
                "an unrelated month keeps its ordinary content");
            Assert.That(laterState.PendingEventId, Is.EqualTo("event.ordinary"));
        }

        [Test]
        public void Sc010Timeline_NeverSchedulesForOtherOrigins()
        {
            EventEngine engine = LoadEngine();
            var relationships = new CharacterRelationships();
            string[] otherScenarios = { string.Empty, EventScenarioIds.Sc013, EventScenarioIds.Sc024 };
            string[] m08cIds = Timeline
                .SelectMany(row => new[] { row.RumorEventId, row.NodeEventId })
                .ToArray();

            foreach (string scenario in otherScenarios)
            {
                var context = new EventRuleContext(0, 0, scenario);
                foreach (TimelineRow row in Timeline)
                {
                    foreach (GameDate date in new[]
                             {
                                 DateOf(row.WarningYear, row.WarningMonth),
                                 DateOf(row.NodeYear, row.NodeMonth)
                             })
                    {
                        IReadOnlyList<string> eligible = engine.GetEligibleEventIds(date,
                            relationships, new EventRuntimeState(), context);
                        foreach (string id in m08cIds)
                            Assert.That(eligible, Does.Not.Contain(id),
                                $"{id} leaked into scenario '{scenario}'");
                    }
                }
            }

            GameSession session = GameSession.NewGame();
            try
            {
                session.InitNewGame(11UL);
                Assert.That(session.CaptureSave().campaign.originId,
                    Is.EqualTo(CampaignOriginCatalog.Sc013OriginId));
                AdvanceTo(session, DateOf(1577, 12));
                Assert.That(session.TryAdvanceToNextMonth(), Is.True, session.LastEventMessage);
                Assert.That(session.PendingEventId, Does.Not.StartWith("event.m08c."));
            }
            finally
            {
                if (session != null)
                    UnityEngine.Object.DestroyImmediate(session.gameObject);
            }
        }

        [Test]
        public void Sc010Timeline_OnceAcknowledgedDoesNotRepeat()
        {
            EventEngine engine = LoadEngine();
            var relationships = new CharacterRelationships();
            var state = new EventRuntimeState();
            var context = Sc010Context;
            GameDate nodeDate = DateOf(1579, 1);

            EventScheduleResult scheduled = engine.ScheduleMonthly(nodeDate, relationships,
                RandomStateManager.Create(29UL).GetStream(RandomStreamKind.Events), state, context);
            Assert.That(scheduled.SelectedEventId, Is.EqualTo(SC010EventIds.FirstIgaNodeEventId));

            EventChoiceResult committed = engine.TryChooseEventOption(scheduled.SelectedEventId,
                SC010EventIds.FirstIgaNodeOptionId, "result.m08c.iga-first.node", nodeDate,
                relationships, state, context);
            Assert.That(committed.Succeeded, Is.True, committed.Message);
            Assert.That(state.Capture().onceEventIds,
                Does.Contain(SC010EventIds.FirstIgaNodeEventId));

            Assert.That(engine.GetEligibleEventIds(nodeDate, relationships, state, context),
                Does.Not.Contain(SC010EventIds.FirstIgaNodeEventId),
                "an acknowledged per-game row must not return on the same date");
            Assert.That(engine.GetEligibleEventIds(DateOf(1581, 1), relationships, state, context),
                Does.Contain(SC010EventIds.SecondIgaNodeEventId),
                "acknowledging one node must not block a later node");
        }

        [Test]
        public void Sc010Timeline_LiveMonthTurnSchedulesTheWarningThroughTheSc010Context()
        {
            GameSession session = GameSession.NewGame();
            try
            {
                session.InitStandardDemo(2027UL);
                Assert.That(session.CaptureSave().campaign.originId,
                    Is.EqualTo(CampaignOriginCatalog.Sc010OriginId));
                AdvanceTo(session, DateOf(1577, 12));

                Assert.That(session.TryAdvanceToNextMonth(), Is.True, session.LastEventMessage);

                Assert.That(session.WorldClock.CurrentDate, Is.EqualTo(DateOf(1578, 1)));
                Assert.That(session.PendingEventId, Is.EqualTo(SC010EventIds.FirstIgaRumorEventId));
                Assert.That(session.EventCatalog.contentVersion, Is.EqualTo(4));
            }
            finally
            {
                if (session != null)
                    UnityEngine.Object.DestroyImmediate(session.gameObject);
            }
        }

        /// <summary>
        /// Live route isolation: each demo origin starts through its real entry point, so
        /// the session's own <c>EventRuleContext</c> decides the route. The turn must reach
        /// 1578-01, otherwise an unscheduled month could hide a leak behind a no-op.
        /// </summary>
        [Test]
        public void Sc010Timeline_LiveOtherDemoOriginsNeverReceiveHistoricalRows()
        {
            AssertLiveOriginIsIsolated(CampaignOriginCatalog.Sc013OriginId,
                session => session.InitNewGame(31UL));
            AssertLiveOriginIsIsolated(CampaignOriginCatalog.Sc020OriginId,
                session => session.InitEducationDemoScenario(32UL));
            AssertLiveOriginIsIsolated(CampaignOriginCatalog.Sc021OriginId,
                session => session.InitResearchDemoScenario(33UL));
            AssertLiveOriginIsIsolated(CampaignOriginCatalog.Sc022OriginId,
                session => session.InitFamilyFormationDemoScenario(34UL));
            AssertLiveOriginIsIsolated(CampaignOriginCatalog.Sc023OriginId,
                session => session.InitSeclusionDemoScenario(35UL));
            AssertLiveOriginIsIsolated(CampaignOriginCatalog.Sc024OriginId,
                session => session.InitFamilyDoctrineDemoScenario(36UL));
        }

        private static void AssertLiveOriginIsIsolated(string expectedOriginId,
            Action<GameSession> startDemo)
        {
            GameSession session = GameSession.NewGame();
            try
            {
                startDemo(session);
                Assert.That(session.CaptureSave().campaign.originId, Is.EqualTo(expectedOriginId));
                AdvanceTo(session, DateOf(1577, 12));

                Assert.That(session.TryAdvanceToNextMonth(), Is.True, session.LastEventMessage);
                Assert.That(session.WorldClock.CurrentDate, Is.EqualTo(DateOf(1578, 1)),
                    "the turn must actually reach the authored warning month");

                foreach (string pendingId in session.CaptureSave().eventRuntime.pendingEventIds)
                    Assert.That(pendingId, Does.Not.StartWith("event.m08c."),
                        $"{pendingId} leaked into origin {expectedOriginId}");
                Assert.That(session.PendingEventId, Does.Not.StartWith("event.m08c."),
                    $"origin {expectedOriginId} must never queue an M08C row");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(session.gameObject);
            }
        }

        [Test]
        public void Sc010Timeline_PendingHistoricalEventSurvivesSaveLoad()
        {
            GameSession original = GameSession.NewGame();
            GameSession reloaded = GameSession.NewGame();
            try
            {
                original.InitStandardDemo(2027UL);
                AdvanceTo(original, DateOf(1577, 12));
                Assert.That(original.TryAdvanceToNextMonth(), Is.True, original.LastEventMessage);
                Assert.That(original.PendingEventId, Is.EqualTo(SC010EventIds.FirstIgaRumorEventId));

                SaveData pending = original.CaptureSave();
                Assert.That(SaveDataValidator.Validate(pending).Succeeded, Is.True);
                Assert.That(pending.eventContentVersion, Is.EqualTo(4));
                Assert.That(pending.eventRuntime.pendingEventIds,
                    Does.Contain(SC010EventIds.FirstIgaRumorEventId));

                reloaded.InitStandardDemo(3UL);
                SaveOperationResult applied = reloaded.TryApplySave(pending);

                Assert.That(applied.Succeeded, Is.True, applied.Message);
                Assert.That(reloaded.WorldClock.CurrentDate, Is.EqualTo(DateOf(1578, 1)));
                Assert.That(reloaded.PendingEventId, Is.EqualTo(SC010EventIds.FirstIgaRumorEventId));
                SaveData restored = reloaded.CaptureSave();
                CollectionAssert.AreEqual(pending.eventRuntime.pendingEventIds,
                    restored.eventRuntime.pendingEventIds);
                Assert.That(restored.eventRuntime.history, Is.Empty);
                Assert.That(restored.eventRuntime.onceEventIds, Is.Empty);

                EventChoiceResult committed = reloaded.TryChooseEventOption(
                    SC010EventIds.FirstIgaRumorEventId, SC010EventIds.FirstIgaRumorOptionId,
                    reloaded.CreateEventResultId(SC010EventIds.FirstIgaRumorEventId,
                        SC010EventIds.FirstIgaRumorOptionId));
                Assert.That(committed.Succeeded, Is.True, committed.Message);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(reloaded.gameObject);
                UnityEngine.Object.DestroyImmediate(original.gameObject);
            }
        }

        [Test]
        public void Sc010Timeline_CommittedHistoricalEventSurvivesSaveLoadWithoutReoffer()
        {
            GameSession original = GameSession.NewGame();
            GameSession reloaded = GameSession.NewGame();
            try
            {
                original.InitStandardDemo(2027UL);
                AdvanceTo(original, DateOf(1577, 12));
                Assert.That(original.TryAdvanceToNextMonth(), Is.True, original.LastEventMessage);
                EventChoiceResult committed = original.TryChooseEventOption(
                    SC010EventIds.FirstIgaRumorEventId, SC010EventIds.FirstIgaRumorOptionId,
                    original.CreateEventResultId(SC010EventIds.FirstIgaRumorEventId,
                        SC010EventIds.FirstIgaRumorOptionId));
                Assert.That(committed.Succeeded, Is.True, committed.Message);

                SaveData committedSave = original.CaptureSave();
                Assert.That(committedSave.eventRuntime.pendingEventIds, Is.Empty);
                Assert.That(committedSave.eventRuntime.onceEventIds,
                    Does.Contain(SC010EventIds.FirstIgaRumorEventId));
                Assert.That(committedSave.eventRuntime.history, Has.Length.EqualTo(1));
                Assert.That(committedSave.eventRuntime.history[0].eventId,
                    Is.EqualTo(SC010EventIds.FirstIgaRumorEventId));
                ChronicleSaveEntry eventRow = committedSave.chronicle.Single(
                    entry => string.Equals(entry.subjectId, SC010EventIds.FirstIgaRumorEventId,
                        StringComparison.Ordinal));
                Assert.That(eventRow.year, Is.EqualTo(1578));
                Assert.That(eventRow.month, Is.EqualTo(1));

                reloaded.InitStandardDemo(3UL);
                SaveOperationResult applied = reloaded.TryApplySave(committedSave);
                Assert.That(applied.Succeeded, Is.True, applied.Message);

                SaveData restored = reloaded.CaptureSave();
                Assert.That(restored.eventRuntime.onceEventIds,
                    Does.Contain(SC010EventIds.FirstIgaRumorEventId));
                Assert.That(restored.eventRuntime.history, Has.Length.EqualTo(1));
                Assert.That(restored.eventRuntime.history[0].eventId,
                    Is.EqualTo(SC010EventIds.FirstIgaRumorEventId));
                Assert.That(restored.chronicle.Count(entry => string.Equals(entry.subjectId,
                        SC010EventIds.FirstIgaRumorEventId, StringComparison.Ordinal)),
                    Is.EqualTo(1));
                Assert.That(reloaded.PendingEventId, Is.Empty);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(reloaded.gameObject);
                UnityEngine.Object.DestroyImmediate(original.gameObject);
            }
        }

        private static EventDefinitionData Find(EventEngine engine, string eventId)
        {
            Assert.That(engine.TryGetEvent(eventId, out EventDefinitionData definition), Is.True,
                "Missing event " + eventId);
            return definition;
        }

        private static EventDefinitionData AlwaysEvent(string eventId, int priority, int weight)
        {
            return new EventDefinitionData
            {
                eventId = eventId,
                trigger = "Monthly",
                conditions = new[] { new EventConditionData { type = nameof(EventConditionKind.Always) } },
                weight = weight,
                textId = "text." + eventId,
                options = new[]
                {
                    new EventOptionData
                    {
                        optionId = "option." + eventId,
                        textId = "option-text." + eventId,
                        requirements = new EventRequirementData[0],
                        effects = new EventEffectData[0]
                    }
                },
                cooldownMonths = 0,
                onceScope = "Never",
                priority = priority
            };
        }

        private static EventDefinitionData DatedEvent(string eventId, int priority, int weight,
            int year, int month)
        {
            EventDefinitionData definition = AlwaysEvent(eventId, priority, weight);
            definition.scenario = SC010EventIds.ScenarioId;
            definition.onceScope = nameof(EventOnceScope.PerGame);
            definition.conditions = new[]
            {
                new EventConditionData
                {
                    type = nameof(EventConditionKind.CampaignDateIs),
                    year = year,
                    month = month
                }
            };
            return definition;
        }

        private static EventEngine CreateEngine(params EventDefinitionData[] definitions)
        {
            var texts = new List<EventTextEntry>();
            foreach (EventDefinitionData definition in definitions)
            {
                texts.Add(new EventTextEntry
                {
                    textId = definition.textId,
                    title = definition.eventId,
                    body = "Year-level history text."
                });
                foreach (EventOptionData option in definition.options)
                    texts.Add(new EventTextEntry
                    {
                        textId = option.textId,
                        title = option.optionId,
                        body = "Acknowledge only."
                    });
            }
            var catalog = new EventCatalogData
            {
                contentVersion = 1,
                events = definitions,
                chains = new EventChainData[0]
            };
            return new EventEngine(catalog,
                new EventTextCatalog { contentVersion = 1, entries = texts.ToArray() });
        }

        private static void AdvanceTo(GameSession session, GameDate target)
        {
            int days = DaysBetween(session.WorldClock.CurrentDate, target);
            Assert.That(days, Is.GreaterThanOrEqualTo(0));
            if (days == 0)
                return;
            Assert.That(session.AdvanceDaysForLegacyFixtureSetup(days), Is.True);
            Assert.That(session.WorldClock.CurrentDate, Is.EqualTo(target));
        }

        private static int DaysBetween(GameDate from, GameDate to)
        {
            int days = 0;
            GameDate cursor = from;
            while (cursor < to && GameDate.TryNext(cursor, out GameDate next))
            {
                cursor = next;
                days++;
            }
            return days;
        }
    }
}
