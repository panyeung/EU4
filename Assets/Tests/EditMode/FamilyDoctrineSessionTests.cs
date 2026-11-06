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
    /// SC-024 Task 3: explicit scenario isolation, the three creed council events and
    /// their atomic creed/relationship/chronicle transaction, plus ambition progress in
    /// the existing monthly settlement and across a head succession.
    /// </summary>
    public sealed class FamilyDoctrineSessionTests
    {
        private readonly List<GameSession> _sessions = new List<GameSession>();

        [TearDown]
        public void TearDown()
        {
            foreach (GameSession session in _sessions)
                if (session != null)
                    UnityEngine.Object.DestroyImmediate(session.gameObject);
            _sessions.Clear();
        }

        private GameSession NewSession()
        {
            GameSession session = GameSession.NewGame();
            _sessions.Add(session);
            return session;
        }

        private static EventCatalogData LoadCatalog() =>
            JsonUtility.FromJson<EventCatalogData>(
                File.ReadAllText("Assets/Resources/Events/event-catalog-v1.json"));

        private static EventTextCatalog LoadText() =>
            JsonUtility.FromJson<EventTextCatalog>(
                File.ReadAllText("Assets/Resources/Localization/event-text-zh-v1.json"));

        private static PersonId[] FamilyMemberIds() => new[]
        {
            M2FixedScenario.HarunobuId, M2FixedScenario.ChieId,
            M2FixedScenario.MasatsunaId, M2FixedScenario.NobumasaId,
            M2FixedScenario.TokihimeId, M2FixedScenario.SakonId
        };

        private static EventRuleContext ContextFor(string scenarioId) =>
            new EventRuleContext(0, 0, scenarioId, M2FixedScenario.HarunobuId, FamilyMemberIds());

        private static string[] Sc024Eligible(EventEngine engine, string scenarioId,
            FamilyCreedKind creed) =>
            engine.GetEligibleEventIds(new GameDate(1560, 5, 1), new CharacterRelationships(),
                    new EventRuntimeState(), ContextFor(scenarioId), creed)
                .Where(id => id.StartsWith("event.sc024.", StringComparison.Ordinal))
                .ToArray();

        private static void ResolvePending(GameSession session)
        {
            if (string.IsNullOrEmpty(session.PendingEventId))
                return;
            string pending = session.PendingEventId;
            EventDefinitionData definition = session.EventCatalog.events
                .FirstOrDefault(item => item.eventId == pending);
            Assert.That(definition, Is.Not.Null);
            foreach (EventOptionData option in definition.options)
            {
                if (!session.TryGetEventOptionAvailability(pending, option.optionId,
                        out bool enabled, out _) || !enabled)
                    continue;
                Assert.That(session.TryChooseEventOption(pending, option.optionId,
                    session.CreateEventResultId(pending, option.optionId)).Succeeded, Is.True);
                return;
            }
            Assert.Fail("no enabled option for " + pending);
        }

        private static string Choose(GameSession session, string optionId)
        {
            string pending = session.PendingEventId;
            Assert.That(pending, Is.Not.Empty);
            EventChoiceResult result = session.TryChooseEventOption(pending, optionId,
                session.CreateEventResultId(pending, optionId));
            Assert.That(result.Succeeded, Is.True, result.Message);
            return pending;
        }

        [Test]
        public void Scenario_ReusesM2RosterWithIndependentSc024RouteAndDefaults()
        {
            SuccessionState state = SC024FamilyDoctrineScenario.CreateSuccession();
            Assert.That(state.World.Family.Id, Is.EqualTo(M2FixedScenario.FamilyId));
            Assert.That(state.HeadPersonId, Is.EqualTo(M2FixedScenario.HarunobuId));
            Assert.That(state.DesignatedHeirId, Is.EqualTo(M2FixedScenario.MasatsunaId));
            Assert.That(state.World.Family.Members.Count, Is.EqualTo(6));
            Assert.That(SC024FamilyDoctrineScenario.ScenarioId, Is.EqualTo("sc024"));
            Assert.That(SC024FamilyDoctrineScenario.StartDate,
                Is.EqualTo(SengokuChronicle.Domain.WorldClock.NewGameDate));

            GameSession session = NewSession();
            session.InitFamilyDoctrineDemoScenario();

            Assert.That(session.CurrentFamilyCreed, Is.EqualTo(FamilyCreedKind.RighteousnessFirst));
            Assert.That(session.Succession.HeadPersonId, Is.EqualTo(M2FixedScenario.HarunobuId));
            Assert.That(session.CurrentHeadAmbition, Is.Not.Null);
            Assert.That(session.CurrentHeadAmbition.Kind, Is.EqualTo(FamilyAmbitionKind.DevelopHousehold));
            Assert.That(session.CurrentHeadAmbition.Progress, Is.Zero);
            Assert.That(session.CurrentHeadAmbition.Target,
                Is.EqualTo(FamilyDoctrineState.DevelopHouseholdTarget));
        }

        [Test]
        public void CouncilEvents_AreMutuallyExclusiveByCreedAndScenario()
        {
            EventCatalogData catalog = LoadCatalog();
            EventTextCatalog text = LoadText();
            var engine = new EventEngine(catalog, text);
            Assert.That(engine.IsReady, Is.True, engine.ValidationMessage);

            CollectionAssert.AreEquivalent(
                new[] { SC024EventIds.SecrecyCouncilEventId },
                Sc024Eligible(engine, EventScenarioIds.Sc024, FamilyCreedKind.SecrecyFirst));
            CollectionAssert.AreEquivalent(
                new[] { SC024EventIds.RighteousnessCouncilEventId },
                Sc024Eligible(engine, EventScenarioIds.Sc024, FamilyCreedKind.RighteousnessFirst));
            CollectionAssert.AreEquivalent(
                new[] { SC024EventIds.MeritCouncilEventId },
                Sc024Eligible(engine, EventScenarioIds.Sc024, FamilyCreedKind.MeritFirst));

            Assert.That(Sc024Eligible(engine, EventScenarioIds.Sc013,
                FamilyCreedKind.RighteousnessFirst), Is.Empty);

            foreach (string eventId in new[]
                     {
                         SC024EventIds.SecrecyCouncilEventId,
                         SC024EventIds.RighteousnessCouncilEventId,
                         SC024EventIds.MeritCouncilEventId
                     })
            {
                EventDefinitionData definition = catalog.events.Single(item => item.eventId == eventId);
                Assert.That(definition.scenario, Is.EqualTo(EventScenarioIds.Sc024));
                Assert.That(definition.onceScope, Is.EqualTo("PerGame"));
                Assert.That(definition.conditions, Has.Length.EqualTo(1));
                Assert.That(definition.conditions[0].type, Is.EqualTo("FamilyCreedIs"));
                Assert.That(definition.options.SelectMany(option => option.effects)
                    .Count(effect => effect.type == "SetFamilyCreed"), Is.EqualTo(2));
            }
        }

        [Test]
        public void CatalogValidation_RejectsUnknownCreedIdsInConditionsAndEffects()
        {
            EventCatalogData conditionCatalog = LoadCatalog();
            EventTextCatalog text = LoadText();
            EventDefinitionData eventRow = conditionCatalog.events.Single(
                item => item.eventId == SC024EventIds.RighteousnessCouncilEventId);
            eventRow.conditions[0].familyCreedId = "NotACreed";
            EventCatalogValidationResult conditionResult =
                EventCatalogValidator.Validate(conditionCatalog, text);
            Assert.That(conditionResult.Succeeded, Is.False);
            Assert.That(conditionResult.Message, Does.Contain("invalid family creed ID"));

            EventCatalogData effectCatalog = LoadCatalog();
            EventOptionData reformOption = effectCatalog.events
                .Single(item => item.eventId == SC024EventIds.RighteousnessCouncilEventId)
                .options.Single(option =>
                    option.optionId == SC024EventIds.RighteousnessReformMeritOptionId);
            reformOption.effects.Single(effect => effect.type == "SetFamilyCreed")
                .familyCreedId = "NotACreed";
            EventCatalogValidationResult effectResult =
                EventCatalogValidator.Validate(effectCatalog, text);
            Assert.That(effectResult.Succeeded, Is.False);
            Assert.That(effectResult.Message, Does.Contain("invalid family creed ID"));
        }

        [Test]
        public void ExplicitSc024RouteSchedulesCouncilButNormalSc013StaysUnchanged()
        {
            GameSession sc024 = NewSession();
            sc024.InitFamilyDoctrineDemoScenario();
            Assert.That(sc024.TryAdvanceToNextMonth(), Is.True, sc024.LastEventMessage);
            Assert.That(sc024.PendingEventId, Is.EqualTo(SC024EventIds.RighteousnessCouncilEventId));

            GameSession normal = NewSession();
            normal.InitNewGame();
            Assert.That(normal.TryAdvanceToNextMonth(), Is.True, normal.LastEventMessage);
            Assert.That(normal.PendingEventId, Is.EqualTo(SC013EventIds.PledgeEventId));
            Assert.That(normal.PendingEventId, Does.Not.StartWith("event.sc024."));
        }

        [Test]
        public void ExistingSaveLoad_RestoresDoctrineAndSc024Route()
        {
            GameSession sc024 = NewSession();
            sc024.InitFamilyDoctrineDemoScenario();
            SaveData existing = sc024.CaptureSave();
            Assert.That(existing.activeEventScenarioId, Is.EqualTo("sc024"));

            GameSession loaded = NewSession();
            loaded.InitNewGame();
            Assert.That(loaded.TryApplySave(existing).Succeeded, Is.True);

            Assert.That(loaded.CurrentFamilyCreed, Is.EqualTo(FamilyCreedKind.RighteousnessFirst));
            Assert.That(loaded.CurrentHeadAmbition, Is.Not.Null);
            Assert.That(loaded.CaptureSave().activeEventScenarioId, Is.EqualTo("sc024"));
            Assert.That(loaded.TryAdvanceToNextMonth(), Is.True, loaded.LastEventMessage);
            Assert.That(loaded.PendingEventId, Is.EqualTo(SC024EventIds.RighteousnessCouncilEventId));
        }

        [Test]
        public void MatchingKeepChoice_AppliesBothRelationshipsAndChronicle()
        {
            GameSession session = NewSession();
            session.InitFamilyDoctrineDemoScenario();
            Assert.That(session.TryAdvanceToNextMonth(), Is.True, session.LastEventMessage);
            int before = session.ChronicleEntries.Count;

            string eventId = Choose(session, SC024EventIds.RighteousnessKeepOptionId);

            Assert.That(eventId, Is.EqualTo(SC024EventIds.RighteousnessCouncilEventId));
            Assert.That(session.CurrentFamilyCreed, Is.EqualTo(FamilyCreedKind.RighteousnessFirst));
            Assert.That(session.TryGetFamilyRelationship(M2FixedScenario.MasatsunaId).CurrentValue,
                Is.EqualTo(5));
            Assert.That(session.TryGetFamilyRelationship(M2FixedScenario.NobumasaId).CurrentValue,
                Is.EqualTo(5));
            Assert.That(session.ChronicleEntries.Count, Is.EqualTo(before + 3));
            Assert.That(session.PendingEventId, Is.Empty);

            var added = session.ChronicleEntries.Skip(before).ToArray();
            Assert.That(added[0].Category, Is.EqualTo(ChronicleCategory.Event));
            Assert.That(added[0].SubjectId, Is.EqualTo(eventId));
            Assert.That(added[0].SourceId, Is.EqualTo(SC024EventIds.RighteousnessKeepOptionId));
            Assert.That(added.Count(entry => entry.Category == ChronicleCategory.Relationship),
                Is.EqualTo(2));
        }

        [Test]
        public void ReformChoice_SetsCreedAndThenOnlyTheNewCreedEventIsEligible()
        {
            GameSession session = NewSession();
            session.InitFamilyDoctrineDemoScenario();
            Assert.That(session.TryAdvanceToNextMonth(), Is.True, session.LastEventMessage);
            int before = session.ChronicleEntries.Count;

            Choose(session, SC024EventIds.RighteousnessReformMeritOptionId);

            Assert.That(session.CurrentFamilyCreed, Is.EqualTo(FamilyCreedKind.MeritFirst));
            Assert.That(session.TryGetFamilyRelationship(M2FixedScenario.MasatsunaId).CurrentValue,
                Is.EqualTo(-5));
            Assert.That(session.TryGetFamilyRelationship(M2FixedScenario.NobumasaId).CurrentValue,
                Is.EqualTo(-5));
            Assert.That(session.ChronicleEntries.Count, Is.EqualTo(before + 3));

            Assert.That(session.TryAdvanceToNextMonth(), Is.True, session.LastEventMessage);
            Assert.That(session.PendingEventId, Is.EqualTo(SC024EventIds.MeritCouncilEventId));

            EventChoiceResult oldEvent = session.TryChooseEventOption(
                SC024EventIds.RighteousnessCouncilEventId,
                SC024EventIds.RighteousnessKeepOptionId,
                session.CreateEventResultId(SC024EventIds.RighteousnessCouncilEventId,
                    SC024EventIds.RighteousnessKeepOptionId));
            Assert.That(oldEvent.Succeeded, Is.False);

            Choose(session, SC024EventIds.MeritKeepOptionId);
            Assert.That(session.CurrentFamilyCreed, Is.EqualTo(FamilyCreedKind.MeritFirst));
            Assert.That(session.TryGetFamilyRelationship(M2FixedScenario.MasatsunaId).CurrentValue,
                Is.EqualTo(-10));
            Assert.That(session.TryGetFamilyRelationship(M2FixedScenario.NobumasaId).CurrentValue,
                Is.Zero);
        }

        [Test]
        public void ChronicleAppendFailure_RollsBackCreedRelationshipsQueueAndLedger()
        {
            GameSession session = NewSession();
            session.InitFamilyDoctrineDemoScenario();
            Assert.That(session.TryAdvanceToNextMonth(), Is.True, session.LastEventMessage);

            string pending = session.PendingEventId;
            int chronicleBefore = session.ChronicleEntries.Count;
            string runtimeBefore = JsonUtility.ToJson(session.EventRuntimeStateForSave);
            GameDate date = session.WorldClock.CurrentDate;

            // Prepare succeeds, then this seam advances the ledger exactly like the
            // family-formation fault injection, so the prepared append genuinely fails.
            session.Sc024ChronicleAppendFaultInjection = ledger =>
                Assert.That(ledger.TryAppendEvent(date.Year, date.Month,
                    "event.sc024.injected", "option.sc024.injected", "注入记录",
                    "测试注入记录", out _, out _), Is.True);

            EventChoiceResult failed = session.TryChooseEventOption(
                pending, SC024EventIds.RighteousnessReformSecrecyOptionId,
                session.CreateEventResultId(pending, SC024EventIds.RighteousnessReformSecrecyOptionId));

            Assert.That(failed.Succeeded, Is.False);
            Assert.That(failed.Message, Does.Contain("回滚"));
            Assert.That(failed.Message, Does.Contain("序号无效"),
                "the prepared append, not a short-circuit, must fail after the ledger advanced");
            Assert.That(session.CurrentFamilyCreed, Is.EqualTo(FamilyCreedKind.RighteousnessFirst));
            Assert.That(session.TryGetFamilyRelationship(M2FixedScenario.MasatsunaId).CurrentValue,
                Is.Zero);
            Assert.That(session.TryGetFamilyRelationship(M2FixedScenario.NobumasaId).CurrentValue,
                Is.Zero);
            Assert.That(session.ChronicleEntries.Count, Is.EqualTo(chronicleBefore));
            Assert.That(session.PendingEventId, Is.EqualTo(pending));
            Assert.That(JsonUtility.ToJson(session.EventRuntimeStateForSave),
                Is.EqualTo(runtimeBefore));
            Assert.That(session.ChronicleEntries.Any(entry =>
                    entry.SubjectId == "event.sc024.injected"), Is.False);

            session.Sc024ChronicleAppendFaultInjection = null;
            Choose(session, SC024EventIds.RighteousnessReformSecrecyOptionId);
            Assert.That(session.CurrentFamilyCreed, Is.EqualTo(FamilyCreedKind.SecrecyFirst));
            Assert.That(session.ChronicleEntries.Count, Is.EqualTo(chronicleBefore + 3));
        }

        [Test]
        public void HouseholdAmbition_AdvancesOnQualifyingMonthSettlementAndCompletesOnce()
        {
            GameSession session = NewSession();
            session.InitFamilyDoctrineDemoScenario();
            Assert.That(session.TryAssignIndustry(IndustryCatalog.FarmId,
                M2FixedScenario.HarunobuId).Succeeded, Is.True, session.LastIndustryMessage);

            for (int month = 1; month <= FamilyDoctrineState.DevelopHouseholdTarget; month++)
            {
                Assert.That(session.TryAdvanceToNextMonth(), Is.True, session.LastEventMessage);
                ResolvePending(session);
                Assert.That(session.CurrentHeadAmbition.Progress, Is.EqualTo(month));
            }
            Assert.That(session.CurrentHeadAmbition.IsComplete, Is.True);

            FamilyChronicleEntry[] completions = session.ChronicleEntries
                .Where(entry => entry.Category == ChronicleCategory.Event &&
                                entry.SourceId == "ambition.develop-household")
                .ToArray();
            Assert.That(completions, Has.Length.EqualTo(1),
                "one completion must append exactly one chronicle result");
            Assert.That(completions[0].SubjectId, Is.EqualTo(M2FixedScenario.HarunobuId.Value));
            Assert.That(completions[0].Title, Does.Contain("明智 晴信"));
            Assert.That(completions[0].Summary, Does.Contain("发展家业"));
            Assert.That(completions[0].Summary, Does.Contain("3/3"));
            Assert.That(completions[0].Summary, Does.Contain("未获得额外资源或能力"));

            Assert.That(session.TryAdvanceToNextMonth(), Is.True, session.LastEventMessage);
            ResolvePending(session);
            Assert.That(session.CurrentHeadAmbition.Progress,
                Is.EqualTo(FamilyDoctrineState.DevelopHouseholdTarget));
            Assert.That(session.ChronicleEntries.Count(entry =>
                    entry.Category == ChronicleCategory.Event &&
                    entry.SourceId == "ambition.develop-household"), Is.EqualTo(1),
                "a later settled month must not append a duplicate completion");
        }

        [Test]
        public void TechniqueAmbition_AdvancesOnlyWhenResearchCompletesOnTheMonthlySettlement()
        {
            // SC-024's default ambition is 发展家业, so this source is exercised on the
            // dedicated research scenario, which still uses the same session doctrine.
            GameSession session = NewSession();
            session.InitResearchDemoScenario();
            Assert.That(session.TryStartCurrentHeadAmbition(FamilyAmbitionKind.PassDownTechnique),
                Is.True, session.LastFamilyDoctrineMessage);
            Assert.That(session.TrySetResearchPlan(FamilyResearchCatalog.SmokeMixingResearchId,
                SC021FamilyResearchScenario.HeadId).Succeeded, Is.True,
                session.LastResearchMessage);

            Assert.That(session.TryAdvanceToNextMonth(), Is.True, session.LastEventMessage);
            ResolvePending(session);
            Assert.That(session.ResearchState.Progress, Is.EqualTo(1));
            Assert.That(session.CurrentHeadAmbition.Progress, Is.Zero,
                "an in-progress research month is not a completed technique");

            Assert.That(session.TryAdvanceToNextMonth(), Is.True, session.LastEventMessage);
            ResolvePending(session);
            Assert.That(session.ResearchState.HasPlan, Is.False);
            Assert.That(session.CurrentHeadAmbition.Progress, Is.EqualTo(1));
            Assert.That(session.CurrentHeadAmbition.IsComplete, Is.True);

            Assert.That(session.TryAdvanceToNextMonth(), Is.True, session.LastEventMessage);
            ResolvePending(session);
            Assert.That(session.CurrentHeadAmbition.Progress, Is.EqualTo(1),
                "a later month without a newly completed technique must not advance again");
        }

        [Test]
        public void EducationAmbition_AdvancesOnlyFromTheDesignatedHeirYearResult()
        {
            GameSession designated = NewSession();
            designated.InitSeclusionDemoScenario();
            designated.Succession.DesignatedHeirId = SC023SeclusionScenario.ChildId;
            Assert.That(designated.TryStartCurrentHeadAmbition(FamilyAmbitionKind.EducateHeir),
                Is.True, designated.LastFamilyDoctrineMessage);
            Assert.That(designated.TrySetEducationPlan(SC023SeclusionScenario.ChildId,
                EducationDirection.Scholarship, SC023SeclusionScenario.ElderId).Succeeded,
                Is.True, designated.LastEducationMessage);
            Assert.That(designated.TryAdvanceToNextMonth(), Is.True, designated.LastEventMessage);

            Assert.That(designated.Development.TryGetResult(SC023SeclusionScenario.ChildId, 10,
                out EducationYearResult result), Is.True);
            int expected = Math.Min(FamilyDoctrineState.EducateHeirTarget,
                PositiveGrowthSum(result));
            Assert.That(expected, Is.GreaterThan(0));
            Assert.That(designated.CurrentHeadAmbition.Progress, Is.EqualTo(expected));

            GameSession nonDesignated = NewSession();
            nonDesignated.InitSeclusionDemoScenario();
            nonDesignated.Succession.DesignatedHeirId = SC023SeclusionScenario.HeirId;
            Assert.That(nonDesignated.TryStartCurrentHeadAmbition(FamilyAmbitionKind.EducateHeir),
                Is.True, nonDesignated.LastFamilyDoctrineMessage);
            Assert.That(nonDesignated.TrySetEducationPlan(SC023SeclusionScenario.ChildId,
                EducationDirection.Scholarship, SC023SeclusionScenario.ElderId).Succeeded,
                Is.True, nonDesignated.LastEducationMessage);
            Assert.That(nonDesignated.TryAdvanceToNextMonth(), Is.True,
                nonDesignated.LastEventMessage);
            Assert.That(nonDesignated.CurrentHeadAmbition.Progress, Is.Zero,
                "a non-designated family member's education result must not advance the ambition");
        }

        [Test]
        public void Succession_PreservesCreedRetainsOldRecordAndNewHeadStartsClean()
        {
            GameSession session = NewSession();
            session.InitFamilyDoctrineDemoScenario();
            Assert.That(session.TryAdvanceToNextMonth(), Is.True, session.LastEventMessage);
            Choose(session, SC024EventIds.RighteousnessReformMeritOptionId);
            Assert.That(session.CurrentFamilyCreed, Is.EqualTo(FamilyCreedKind.MeritFirst));

            SuccessionResult handoff = SuccessionService.BeginRetirement(session.Succession,
                session.WorldClock.CurrentDate);
            Assert.That(handoff.Succeeded, Is.True, handoff.Message);
            Assert.That(session.Succession.HeadPersonId, Is.EqualTo(M2FixedScenario.MasatsunaId));

            Assert.That(session.CurrentFamilyCreed, Is.EqualTo(FamilyCreedKind.MeritFirst));
            Assert.That(session.CurrentHeadAmbition, Is.Null);
            Assert.That(session.TryGetFamilyAmbition(M2FixedScenario.HarunobuId,
                out FamilyAmbitionRecord oldRecord), Is.True);
            Assert.That(oldRecord.Kind, Is.EqualTo(FamilyAmbitionKind.DevelopHousehold));

            Assert.That(session.TryStartCurrentHeadAmbition(FamilyAmbitionKind.EducateHeir),
                Is.True, session.LastFamilyDoctrineMessage);
            Assert.That(session.CurrentHeadAmbition.Kind,
                Is.EqualTo(FamilyAmbitionKind.EducateHeir));
            Assert.That(session.TryGetFamilyAmbition(M2FixedScenario.HarunobuId,
                out FamilyAmbitionRecord retained), Is.True);
            Assert.That(retained.Kind, Is.EqualTo(FamilyAmbitionKind.DevelopHousehold),
                "the old head's ambition must never transfer to the new head");
        }

        [Test]
        public void PostSuccessionSettlement_LeavesOldRecordAndAdvancesOnlyNewHeadAfterSelection()
        {
            GameSession session = NewSession();
            session.InitFamilyDoctrineDemoScenario();
            Assert.That(session.TryAssignIndustry(IndustryCatalog.FarmId,
                M2FixedScenario.HarunobuId).Succeeded, Is.True, session.LastIndustryMessage);
            Assert.That(session.TryAdvanceToNextMonth(), Is.True, session.LastEventMessage);
            ResolvePending(session);
            Assert.That(session.CurrentHeadAmbition.Progress, Is.EqualTo(1));

            SuccessionResult handoff = SuccessionService.BeginRetirement(session.Succession,
                session.WorldClock.CurrentDate);
            Assert.That(handoff.Succeeded, Is.True, handoff.Message);
            Assert.That(session.Succession.HeadPersonId, Is.EqualTo(M2FixedScenario.MasatsunaId));
            Assert.That(session.CurrentHeadAmbition, Is.Null);
            Assert.That(session.TryGetFamilyAmbition(M2FixedScenario.HarunobuId,
                out FamilyAmbitionRecord oldRecord), Is.True);
            Assert.That(oldRecord.Progress, Is.EqualTo(1));

            // The new head settles a month without choosing an ambition: neither the
            // old record nor an implicit new record may advance.
            Assert.That(session.TryAdvanceToNextMonth(), Is.True, session.LastEventMessage);
            ResolvePending(session);
            Assert.That(session.CurrentHeadAmbition, Is.Null);
            Assert.That(session.TryGetFamilyAmbition(M2FixedScenario.MasatsunaId, out _), Is.False);
            Assert.That(session.TryGetFamilyAmbition(M2FixedScenario.HarunobuId,
                out FamilyAmbitionRecord unchangedOld), Is.True);
            Assert.That(unchangedOld.Progress, Is.EqualTo(1));

            Assert.That(session.TryStartCurrentHeadAmbition(FamilyAmbitionKind.DevelopHousehold),
                Is.True, session.LastFamilyDoctrineMessage);
            Assert.That(session.TryAssignIndustry(IndustryCatalog.FarmId,
                M2FixedScenario.MasatsunaId).Succeeded, Is.True, session.LastIndustryMessage);
            Assert.That(session.TryAdvanceToNextMonth(), Is.True, session.LastEventMessage);
            ResolvePending(session);

            Assert.That(session.CurrentHeadAmbition.Progress, Is.EqualTo(1));
            Assert.That(session.TryGetFamilyAmbition(M2FixedScenario.HarunobuId,
                out FamilyAmbitionRecord retainedOld), Is.True);
            Assert.That(retainedOld.Progress, Is.EqualTo(1),
                "the old head's record must stay unchanged after the new head settles");
        }

        [Test]
        public void DuplicateSettlementInputs_DoNotAdvanceAmbitionsAgain()
        {
            var household = new FamilyDoctrineState();
            Assert.That(household.TryStartAmbition(M2FixedScenario.HarunobuId,
                FamilyAmbitionKind.DevelopHousehold, Array.Empty<string>(), out _), Is.True);
            IndustryMonthReport report = Month(1560, 1, Line(IndustryLineStatus.Operated, 1, 0, 0));
            Assert.That(household.TryAdvanceHouseholdAmbition(M2FixedScenario.HarunobuId,
                report, out _), Is.True);
            Assert.That(household.TryAdvanceHouseholdAmbition(M2FixedScenario.HarunobuId,
                report, out _), Is.True);
            Assert.That(household.TryGetAmbition(M2FixedScenario.HarunobuId,
                out FamilyAmbitionRecord householdRecord), Is.True);
            Assert.That(householdRecord.Progress, Is.EqualTo(1));

            var technique = new FamilyDoctrineState();
            Assert.That(technique.TryStartAmbition(M2FixedScenario.MasatsunaId,
                FamilyAmbitionKind.PassDownTechnique, Array.Empty<string>(), out _), Is.True);
            Assert.That(technique.TryAdvanceTechniqueAmbition(M2FixedScenario.MasatsunaId,
                new[] { FamilyResearchCatalog.SmokeMixingTechniqueId }, out _), Is.True);
            Assert.That(technique.TryAdvanceTechniqueAmbition(M2FixedScenario.MasatsunaId,
                new[] { FamilyResearchCatalog.SmokeMixingTechniqueId }, out _), Is.True);
            Assert.That(technique.TryGetAmbition(M2FixedScenario.MasatsunaId,
                out FamilyAmbitionRecord techniqueRecord), Is.True);
            Assert.That(techniqueRecord.Progress, Is.EqualTo(1));
        }

        private static int PositiveGrowthSum(EducationYearResult result)
        {
            int sum = 0;
            sum += Math.Max(0, result.After.Ninjutsu - result.Before.Ninjutsu);
            sum += Math.Max(0, result.After.MartialArts - result.Before.MartialArts);
            sum += Math.Max(0, result.After.Scholarship - result.Before.Scholarship);
            sum += Math.Max(0, result.After.Management - result.Before.Management);
            sum += Math.Max(0, result.After.Social - result.Before.Social);
            return sum;
        }

        private static IndustryMonthReport Month(int year, int month, params IndustryLineResult[] lines)
        {
            IndustryResources.TryCreate(10, 10, 10, out IndustryResources resources, out _);
            return new IndustryMonthReport(year, month, resources, resources, lines,
                Array.Empty<IndustryVacancy>(), false, true, false, "test");
        }

        private static IndustryLineResult Line(IndustryLineStatus status, int netFunds,
            int netSupplies, int netIntelligence) =>
            new IndustryLineResult("industry.test", "Test Industry", new PersonId("person.worker"),
                "Worker", status, string.Empty, 0, 0, 0, 0, 0,
                netFunds, netSupplies, netIntelligence,
                netFunds, netSupplies, netIntelligence, false);
    }
}
