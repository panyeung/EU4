using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;
using SengokuChronicle.Events;
using SengokuChronicle.Persistence;
using UnityEngine;

namespace SengokuChronicle.Tests.EditMode
{
    /// <summary>
    /// M08A Task 3: annual natural deaths, autonomous marriages and the atomic monthly
    /// transaction. Seeds are found by replaying the persisted WorldSimulation stream,
    /// because every fixture makes the annual roll the stream's first draw.
    /// </summary>
    public sealed class GameSessionFamilyLifeCycleTests
    {
        private GameSession _session;

        [SetUp]
        public void SetUp()
        {
            _session = GameSession.NewGame();
        }

        [TearDown]
        public void TearDown()
        {
            if (_session != null)
                UnityEngine.Object.DestroyImmediate(_session.gameObject);
        }

        [Test]
        public void NonBoundaryMonth_PlansNoDeathOrMarriage()
        {
            _session.InitNewGame(GameSession.DefaultRandomSeed);
            int persons = _session.Succession.World.PersonCount;

            AdvanceAndResolve(_session);

            Assert.That(_session.Succession.World.PersonCount, Is.EqualTo(persons));
            Assert.That(ChronicleCount(ChronicleCategory.NaturalDeath), Is.Zero);
            Assert.That(ChronicleCount(ChronicleCategory.FamilyFormation), Is.Zero);
            foreach (PersonState person in _session.Succession.World.Persons)
                Assert.That(_session.Succession.World.IsLiving(person.Id), Is.True);
        }

        [Test]
        public void AnnualBoundary_NonHeadDeathKeepsFactsAndClearsTheIndustryPost()
        {
            ulong seed = FindSeed(stream =>
                stream.TryNextInt(1000, out int headRoll) && headRoll >= 5 &&
                stream.TryNextInt(1000, out int oldRoll) && oldRoll < 250);
            InitTwoMemberFamily(seed, harunobuBirthYear: 1540, sakonBirthYear: 1470);

            Assert.That(_session.TryAssignIndustry(IndustryCatalog.FarmId,
                M2FixedScenario.SakonId).Succeeded, Is.True, _session.LastIndustryMessage);

            AdvanceMonthsAndResolve(_session, 9);

            Assert.That(_session.Succession.World.IsLiving(M2FixedScenario.SakonId), Is.False);
            Assert.That(_session.Succession.World.IsLiving(M2FixedScenario.HarunobuId), Is.True);
            Assert.That(_session.Succession.World.TryGetPerson(M2FixedScenario.SakonId,
                out PersonState sakon), Is.True, "the dead person keeps every recorded fact");
            Assert.That(sakon.DisplayName, Is.Not.Empty);
            Assert.That(_session.Succession.World.Family.IsMember(M2FixedScenario.SakonId),
                Is.True);
            Assert.That(_session.Industry.Slot(IndustryCatalog.FarmId).AssigneeId,
                Is.Not.EqualTo(M2FixedScenario.SakonId),
                "a dead person can no longer hold an industry post");
            Assert.That(ChronicleCount(ChronicleCategory.NaturalDeath), Is.EqualTo(1));
            FamilyChronicleEntry death = FirstChronicle(ChronicleCategory.NaturalDeath);
            Assert.That(death.SubjectId, Is.EqualTo(M2FixedScenario.SakonId.Value));
            Assert.That(death.Year, Is.EqualTo(1561));
            Assert.That(death.Month, Is.EqualTo(1));
            Assert.That(death.FromValue, Is.EqualTo(91));
            Assert.That(death.Title, Is.EqualTo(GameSession.NaturalDeathChronicleTitle));
        }

        [Test]
        public void AnnualBoundary_DueBirthAppliesBeforeTheNaturalDeath()
        {
            ulong seed = FindSeed(stream =>
                stream.TryNextInt(2, out _) &&
                stream.TryNextInt(1000, out int gestatingRoll) && gestatingRoll < 250 &&
                stream.TryNextInt(1000, out int otherRoll) && otherRoll >= 5);
            InitTwoMemberFamily(seed, harunobuBirthYear: 1540, sakonBirthYear: 1530);
            // Replace the second member with the gestating spouse.
            _session.Succession.World.TrySetLife(M2FixedScenario.SakonId,
                new SuccessionPersonLife(1530, false, false), out _);
            _session.Succession.World.TrySetLife(M2FixedScenario.ChieId,
                new SuccessionPersonLife(1470, true, false), out _);
            GameDate start = _session.WorldClock.CurrentDate;
            InjectPregnancy(M2FixedScenario.ChieId, M2FixedScenario.HarunobuId, start);

            AdvanceMonthsAndResolve(_session, 9);

            Assert.That(_session.Succession.World.IsLiving(M2FixedScenario.ChieId), Is.False,
                "the gestating parent dies in the same settlement");
            PersonState newborn = _session.Succession.World.Persons.FirstOrDefault(person =>
                person.Id.Value != null && person.Id.Value.StartsWith("person.birth.",
                    StringComparison.Ordinal));
            Assert.That(newborn, Is.Not.Null, "the due birth resolves before the death list");
            Assert.That(_session.Succession.World.Kinship.Contains(new KinshipLink(
                M2FixedScenario.ChieId, newborn.Id, KinshipType.ParentChild)), Is.True);
            Assert.That(_session.FamilyPregnancies, Is.Empty);
            Assert.That(ChronicleCount(ChronicleCategory.NaturalDeath), Is.EqualTo(1));
            Assert.That(FirstChronicle(ChronicleCategory.FamilyFormation).SourceId,
                Is.EqualTo(FamilyChronicleEntry.BirthSourceId));
        }

        [Test]
        public void AnnualBoundary_DeadGestatingParentCancelsAnUnfinishedPregnancy()
        {
            ulong seed = FindSeed(stream =>
                stream.TryNextInt(1000, out int gestatingRoll) && gestatingRoll < 250 &&
                stream.TryNextInt(1000, out int otherRoll) && otherRoll >= 5);
            InitTwoMemberFamily(seed, harunobuBirthYear: 1540, sakonBirthYear: 1530);
            _session.Succession.World.TrySetLife(M2FixedScenario.SakonId,
                new SuccessionPersonLife(1530, false, false), out _);
            _session.Succession.World.TrySetLife(M2FixedScenario.ChieId,
                new SuccessionPersonLife(1470, true, false), out _);
            InjectPregnancy(M2FixedScenario.ChieId, M2FixedScenario.HarunobuId,
                new GameDate(1560, 11, 1));
            int before = _session.Succession.World.PersonCount;

            AdvanceMonthsAndResolve(_session, 9);

            Assert.That(_session.Succession.World.IsLiving(M2FixedScenario.ChieId), Is.False);
            Assert.That(_session.FamilyPregnancies, Is.Empty,
                "an unfinished pregnancy is cancelled with its gestating parent");
            Assert.That(_session.Succession.World.PersonCount, Is.EqualTo(before),
                "no birth is created from the cancelled pregnancy");
            Assert.That(ChronicleCount(ChronicleCategory.FamilyFormation), Is.Zero);
            Assert.That(ChronicleCount(ChronicleCategory.NaturalDeath), Is.EqualTo(1));
        }

        [Test]
        public void AnnualBoundary_HeadDeathRunsSuccessionOnceAndSkipsMarriage()
        {
            ulong seed = FindSeed(stream =>
                stream.TryNextInt(1000, out int headRoll) && headRoll < 250 &&
                stream.TryNextInt(1000, out int otherRoll) && otherRoll >= 5);
            _session.InitNewGame(seed);
            KillAllExcept(M2FixedScenario.HarunobuId, M2FixedScenario.MasatsunaId);
            _session.Succession.World.TrySetLife(M2FixedScenario.HarunobuId,
                new SuccessionPersonLife(1470, true, false), out _);
            _session.Succession.World.TrySetLife(M2FixedScenario.MasatsunaId,
                new SuccessionPersonLife(1530, true, false), out _);

            AdvanceMonthsAndResolve(_session, 9);

            Assert.That(_session.Succession.World.IsLiving(M2FixedScenario.HarunobuId),
                Is.False);
            Assert.That(_session.Succession.Phase,
                Is.EqualTo(SuccessionPhase.AwaitingSuccessionChoice),
                "the existing death succession flow runs exactly once");
            Assert.That(_session.Succession.IsPending, Is.True);
            Assert.That(ChronicleCount(ChronicleCategory.FamilyFormation), Is.Zero,
                "a pending succession decision skips autonomous marriage");
            Assert.That(_session.TryAdvanceToNextMonth(), Is.False,
                "the pending succession pauses further time advance");
        }

        [Test]
        public void AnnualBoundary_NoLawfulHeirStillReachesTheExistingDecision()
        {
            ulong seed = FindSeed(stream =>
                stream.TryNextInt(1000, out int headRoll) && headRoll < 250);
            _session.InitNewGame(seed);
            KillAllExcept();
            _session.Succession.World.TrySetLife(M2FixedScenario.HarunobuId,
                new SuccessionPersonLife(1470, true, false), out _);

            AdvanceMonthsAndResolve(_session, 9);

            Assert.That(_session.Succession.World.IsLiving(M2FixedScenario.HarunobuId),
                Is.False);
            Assert.That(_session.Succession.Phase, Is.EqualTo(SuccessionPhase.Extinct),
                "the existing crisis/extinction decision still owns a family with no heir");
            Assert.That(ChronicleCount(ChronicleCategory.NaturalDeath), Is.EqualTo(1));
        }

        [Test]
        public void AnnualBoundary_MarriageHitCreatesAVisibleFormalSpouse()
        {
            ulong seed = FindMarriageSeed(hit: true);
            InitTwoMemberFamily(seed, harunobuBirthYear: 1540, sakonBirthYear: 1530);
            int before = _session.Succession.World.PersonCount;

            AdvanceMonthsAndResolve(_session, 9);

            Assert.That(_session.Succession.World.PersonCount, Is.EqualTo(before + 1),
                "one generated spouse joins the roster");
            PersonId spouseId = SpouseOf(M2FixedScenario.SakonId);
            Assert.That(spouseId.IsEmpty, Is.False, "the marriage created a formal spouse edge");
            Assert.That(_session.Succession.World.TryGetPerson(spouseId,
                out PersonState spouse), Is.True);
            Assert.That(_session.Succession.World.Family.TryGetRole(spouse.Id,
                out FamilyMemberRole role), Is.True);
            Assert.That(role, Is.EqualTo(FamilyMemberRole.Spouse));
            Assert.That(_session.Succession.World.IsLiving(spouse.Id), Is.True);
            Assert.That(_session.Succession.World.Kinship.Contains(new KinshipLink(
                spouse.Id, M2FixedScenario.SakonId, KinshipType.Spouse)), Is.True,
                "the formal spouse edge reads symmetrically");
            Assert.That(_session.Development.Contains(spouse.Id), Is.True);
            Assert.That(_session.DailyActions.Any(row => row.PersonId == spouse.Id), Is.True);
            Assert.That(_session.Succession.World.TryGetLife(spouse.Id,
                out SuccessionPersonLife life), Is.True);
            Assert.That(life.AgeAt(_session.WorldClock.CurrentDate),
                Is.InRange(16, 40), "the generated spouse is an adult near the candidate");

            FamilyChronicleEntry marriage = FirstChronicle(ChronicleCategory.FamilyFormation);
            Assert.That(marriage.SourceId, Is.EqualTo(FamilyChronicleEntry.MarriageSourceId));
            Assert.That(marriage.Title, Is.EqualTo(FamilyMarriageSettlement.ChronicleTitle));

            SaveData save = _session.CaptureSave();
            string spouseIdValue = spouse.Id.Value;
            Assert.That(_session.TryApplySave(save).Succeeded, Is.True,
                _session.LastEventMessage);
            Assert.That(_session.Succession.World.TryGetPerson(new PersonId(spouseIdValue),
                out PersonState reloaded), Is.True, "the spouse keeps the same stable ID");
            Assert.That(reloaded.DisplayName, Is.EqualTo(spouse.DisplayName));
        }

        [Test]
        public void AnnualBoundary_AFailedMarriageRollAddsNobody()
        {
            ulong seed = FindMarriageSeed(hit: false);
            InitTwoMemberFamily(seed, harunobuBirthYear: 1540, sakonBirthYear: 1530);
            int before = _session.Succession.World.PersonCount;

            AdvanceMonthsAndResolve(_session, 9);

            Assert.That(_session.Succession.World.PersonCount, Is.EqualTo(before));
            Assert.That(ChronicleCount(ChronicleCategory.FamilyFormation), Is.Zero);
            Assert.That(_session.Succession.World.IsLiving(M2FixedScenario.SakonId), Is.True);
        }

        [Test]
        public void ChronicleFailure_RollsBackEveryMonthlySnapshotAndRetriesIdentically()
        {
            ulong seed = FindMarriageSeed(hit: true);
            InitTwoMemberFamily(seed, harunobuBirthYear: 1540, sakonBirthYear: 1530);

            for (int i = 0; i < 8; i++)
                AdvanceAndResolve(_session);

            GameDate dateBefore = _session.WorldClock.CurrentDate;
            string rngBefore = RngSignature(_session);
            int personsBefore = _session.Succession.World.PersonCount;
            int membersBefore = _session.Succession.World.Family.MemberCount;
            int linksBefore = _session.Succession.World.Kinship.Count;
            int chronicleBefore = _session.ChronicleEntries.Count;
            int pregnanciesBefore = _session.FamilyPregnancies.Count;
            int developmentBefore = _session.Development.PersonIds.Count;
            int dailyRowsBefore = _session.DailyActions.Count;
            int industryBefore = _session.Industry.Slots.Count(slot =>
                !slot.AssigneeId.IsEmpty);

            _session.FamilyLifeCycleChronicleFaultInjection = ledger =>
            {
                Assert.That(ledger.TryAppendNaturalDeath(1561, 1, "person.test.dummy", 1,
                    "测试占位", "测试占位记录。", out _, out _), Is.True);
            };

            Assert.That(_session.TryAdvanceToNextMonth(), Is.False);
            Assert.That(_session.LastEventMessage, Does.Contain("家族人口编年史写入失败"));

            Assert.That(_session.WorldClock.CurrentDate, Is.EqualTo(dateBefore));
            Assert.That(RngSignature(_session), Is.EqualTo(rngBefore),
                "the failed month restores the persisted random stream exactly");
            Assert.That(_session.Succession.World.PersonCount, Is.EqualTo(personsBefore));
            Assert.That(_session.Succession.World.Family.MemberCount, Is.EqualTo(membersBefore));
            Assert.That(_session.Succession.World.Kinship.Count, Is.EqualTo(linksBefore));
            Assert.That(_session.ChronicleEntries.Count, Is.EqualTo(chronicleBefore));
            Assert.That(_session.FamilyPregnancies.Count, Is.EqualTo(pregnanciesBefore));
            Assert.That(_session.Development.PersonIds.Count, Is.EqualTo(developmentBefore));
            Assert.That(_session.DailyActions.Count, Is.EqualTo(dailyRowsBefore));
            Assert.That(_session.Industry.Slots.Count(slot => !slot.AssigneeId.IsEmpty),
                Is.EqualTo(industryBefore));

            _session.FamilyLifeCycleChronicleFaultInjection = null;
            AdvanceAndResolve(_session);

            PersonId retrySpouseId = SpouseOf(M2FixedScenario.SakonId);
            Assert.That(retrySpouseId.IsEmpty, Is.False, "the retry completes the same marriage");

            using (var control = new GameSessionControl(seed))
            {
                control.Session.FamilyLifeCycleChronicleFaultInjection = ledger => { };
                for (int i = 0; i < 9; i++)
                    AdvanceAndResolve(control.Session);
                PersonId controlSpouseId = SpouseOf(control.Session,
                    M2FixedScenario.SakonId);
                Assert.That(controlSpouseId.IsEmpty, Is.False);
                Assert.That(retrySpouseId.Value, Is.EqualTo(controlSpouseId.Value),
                    "the retry consumes the same RNG order as a fault-free month");
            }
        }

        [Test]
        public void SettlementException_AfterCommitRestoresTheWholeMonth()
        {
            ulong seed = FindMarriageSeed(hit: true);
            InitTwoMemberFamily(seed, harunobuBirthYear: 1540, sakonBirthYear: 1530);
            for (int i = 0; i < 8; i++)
                AdvanceAndResolve(_session);

            GameDate dateBefore = _session.WorldClock.CurrentDate;
            string rngBefore = RngSignature(_session);
            int personsBefore = _session.Succession.World.PersonCount;
            int chronicleBefore = _session.ChronicleEntries.Count;
            int linksBefore = _session.Succession.World.Kinship.Count;

            _session.MonthSettlementFaultInjection = () =>
                throw new InvalidOperationException("M08A test fault");
            Assert.That(_session.TryAdvanceToNextMonth(), Is.False);

            Assert.That(_session.WorldClock.CurrentDate, Is.EqualTo(dateBefore));
            Assert.That(RngSignature(_session), Is.EqualTo(rngBefore));
            Assert.That(_session.Succession.World.PersonCount, Is.EqualTo(personsBefore));
            Assert.That(_session.Succession.World.Kinship.Count, Is.EqualTo(linksBefore));
            Assert.That(_session.ChronicleEntries.Count, Is.EqualTo(chronicleBefore));

            _session.MonthSettlementFaultInjection = null;
            AdvanceAndResolve(_session);
            Assert.That(_session.Succession.World.PersonCount, Is.EqualTo(personsBefore + 1));
        }

        [Test]
        public void AnnualBoundary_ASpouseWidowedByThisSettlementWaitsForALaterYear()
        {
            // Chie and Sakon are spouses and Chie dies at the 1560→1561 boundary while
            // Harunobu, the player-controlled head, survives. The stream therefore holds
            // three death rolls, no marriage roll, then the following boundary's two
            // death rolls and Sakon's marriage roll. A working farm keeps the household
            // fed, so this fixture isolates the marriage rule; the dedicated M08B shortage
            // test owns the food-mortality bonus.
            ulong seed = FindSeed(stream =>
                stream.TryNextInt(1000, out int chieRoll) && chieRoll < 250 &&
                stream.TryNextInt(1000, out int headRoll) && headRoll >= 5 &&
                stream.TryNextInt(1000, out int sakonRoll) && sakonRoll >= 5 &&
                stream.TryNextInt(1000, out int nextHeadRoll) && nextHeadRoll >= 5 &&
                stream.TryNextInt(1000, out int nextSakonRoll) && nextSakonRoll >= 5 &&
                stream.TryNextInt(FamilyLifeCycleRules.MarriageRollExclusiveMax,
                    out int marriageRoll) &&
                marriageRoll < FamilyLifeCycleRules.MarriagePercent);
            _session.InitNewGame(seed);
            KillAllExcept(M2FixedScenario.HarunobuId, M2FixedScenario.ChieId,
                M2FixedScenario.SakonId);
            _session.Succession.World.TrySetLife(M2FixedScenario.HarunobuId,
                new SuccessionPersonLife(1540, true, false), out _);
            _session.Succession.World.TrySetLife(M2FixedScenario.ChieId,
                new SuccessionPersonLife(1470, true, false), out _);
            _session.Succession.World.TrySetLife(M2FixedScenario.SakonId,
                new SuccessionPersonLife(1530, true, false), out _);
            Assert.That(_session.Succession.World.TryAddKinship(
                    new KinshipLink(M2FixedScenario.ChieId, M2FixedScenario.SakonId,
                        KinshipType.Spouse), out string linkError), Is.True, linkError);
            Assert.That(_session.TryAssignIndustry(IndustryCatalog.FarmId,
                M2FixedScenario.HarunobuId).Succeeded, Is.True, _session.LastIndustryMessage);
            int personsBefore = _session.Succession.World.PersonCount;

            AdvanceMonthsAndResolve(_session, 9);

            Assert.That(_session.Market.ConsecutiveFoodShortageMonths, Is.Zero,
                "the farm feeds the household, so no shortage bonus changes these rolls");
            Assert.That(_session.Succession.World.IsLiving(M2FixedScenario.ChieId),
                Is.False, "Chie dies in the annual settlement");
            Assert.That(ChronicleCount(ChronicleCategory.NaturalDeath), Is.EqualTo(1));
            Assert.That(ChronicleCount(ChronicleCategory.FamilyFormation), Is.Zero,
                "the settlement that widowed Sakon does not remarry him");
            Assert.That(_session.Succession.World.PersonCount, Is.EqualTo(personsBefore));

            AdvanceMonthsAndResolve(_session, 12);

            Assert.That(_session.Succession.World.PersonCount, Is.EqualTo(personsBefore + 1),
                "the widowed spouse is eligible at the next annual settlement");
            PersonId livingSpouse = LivingSpouseOf(_session, M2FixedScenario.SakonId);
            Assert.That(livingSpouse.IsEmpty, Is.False);
            Assert.That(_session.Succession.World.IsLiving(livingSpouse), Is.True);
            Assert.That(FirstChronicle(ChronicleCategory.FamilyFormation).SourceId,
                Is.EqualTo(FamilyChronicleEntry.MarriageSourceId));
        }

        [Test]
        public void AnnualBoundary_NaturalDeathClosesTheRunningCommissionAndStaysSaveable()
        {
            ulong seed = FindSeed(stream =>
                stream.TryNextInt(1000, out int headRoll) && headRoll >= 5 &&
                stream.TryNextInt(1000, out int assigneeRoll) && assigneeRoll < 250);
            InitTwoMemberFamily(seed, harunobuBirthYear: 1540, sakonBirthYear: 1470);

            AdvanceMonthsAndResolve(_session, 8);
            Assert.That(_session.AdvanceDays(30), Is.True, _session.LastEventMessage);
            Assert.That(_session.WorldClock.CurrentDate,
                Is.EqualTo(new GameDate(1560, 12, 31)));

            Assert.That(_session.TryAcceptCommission().Succeeded, Is.True,
                _session.LastCommissionMessage);
            Assert.That(_session.TryAssignCommission(M2FixedScenario.SakonId).Succeeded,
                Is.True, _session.LastCommissionMessage);
            Assert.That(_session.CommissionStage, Is.EqualTo(ShadowEscortStage.InProgress));
            SaveOperationResult inProgress = SaveDataValidator.Validate(_session.CaptureSave());
            Assert.That(inProgress.Succeeded, Is.True,
                "an in-progress commission is saveable before the death: " + inProgress.Message);

            Assert.That(_session.TryAdvanceToNextMonth(), Is.True, _session.LastEventMessage);

            Assert.That(_session.Succession.World.IsLiving(M2FixedScenario.SakonId), Is.False,
                "the assignee dies in the annual settlement");
            Assert.That(_session.CommissionStage, Is.EqualTo(ShadowEscortStage.Abandoned),
                "the settlement closes a commission whose assignee died");
            Assert.That(_session.CommissionOutcome, Is.EqualTo(CommissionOutcome.None));
            Assert.That(_session.MissionHistory, Is.Empty,
                "abandonment writes no shared-result history row");
            Assert.That(_session.CommissionInvestedFunds, Is.GreaterThan(0));
            Assert.That(_session.CommissionStatus.RewardFunds, Is.Zero);
            Assert.That(_session.CommissionStatus.RewardSupplies, Is.Zero);
            Assert.That(_session.LastCommissionMessage, Does.Contain("离世"));

            SaveData save = _session.CaptureSave();
            Assert.That(save.commission.stage, Is.EqualTo((int)ShadowEscortStage.Abandoned));
            Assert.That(save.commission.runId, Is.Empty);
            SaveOperationResult validation = SaveDataValidator.Validate(save);
            Assert.That(validation.Succeeded, Is.True,
                "the same-date save must validate after the death: " + validation.Message);
            Assert.That(save.chronicle, Has.Some.Matches<ChronicleSaveEntry>(entry =>
                    entry != null &&
                    entry.subjectId == FamilyChronicleEntry.ShadowEscortCommissionId &&
                    entry.sourceId == "abandon"),
                "the settlement leaves one readable abandon chronicle row");

            GameSession loaded = GameSession.NewGame();
            try
            {
                SaveOperationResult applied = loaded.TryApplySave(save);
                Assert.That(applied.Succeeded, Is.True, applied.Message);
                Assert.That(loaded.CommissionStage, Is.EqualTo(ShadowEscortStage.Abandoned));
                Assert.That(loaded.CommissionAssignee,
                    Is.EqualTo(M2FixedScenario.SakonId));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(loaded.gameObject);
            }
        }

        [Test]
        public void AnnualBoundary_CommissionDueOnJan1_IsAbandonedByDeathWithoutAResult()
        {
            // Same fixture as the in-progress death test, but the three-day delegated
            // trip starts on December 29 so its third simulated day would be January 1.
            // The approved M08A meaning is that the annual death takes effect at that
            // boundary, so death wins before the due day can advance or resolve.
            ulong seed = FindSeed(stream =>
                stream.TryNextInt(1000, out int headRoll) && headRoll >= 5 &&
                stream.TryNextInt(1000, out int assigneeRoll) && assigneeRoll < 250);
            InitTwoMemberFamily(seed, harunobuBirthYear: 1540, sakonBirthYear: 1470);

            AdvanceMonthsAndResolve(_session, 8);
            Assert.That(_session.AdvanceDays(28), Is.True, _session.LastEventMessage);
            Assert.That(_session.WorldClock.CurrentDate,
                Is.EqualTo(new GameDate(1560, 12, 29)));

            Assert.That(_session.TryAcceptCommission().Succeeded, Is.True,
                _session.LastCommissionMessage);
            Assert.That(_session.TryAssignCommission(M2FixedScenario.SakonId).Succeeded,
                Is.True, _session.LastCommissionMessage);
            Assert.That(_session.CommissionStage, Is.EqualTo(ShadowEscortStage.InProgress));
            Assert.That(_session.CommissionRemainingDays,
                Is.EqualTo(ShadowEscortCommission.DurationDays));

            Assert.That(_session.AdvanceDays(2), Is.True, _session.LastEventMessage);
            Assert.That(_session.WorldClock.CurrentDate,
                Is.EqualTo(new GameDate(1560, 12, 31)));
            Assert.That(_session.CommissionStage, Is.EqualTo(ShadowEscortStage.InProgress),
                "the delegated trip is due on January 1");
            Assert.That(_session.CommissionRemainingDays, Is.EqualTo(1));

            string missionsBefore = MissionsSignature(_session);
            int fameBefore = _session.FamilyFame;
            int concealmentBefore = _session.Concealment;
            int historyBefore = _session.MissionHistory.Count;

            Assert.That(_session.TryAdvanceToNextMonth(), Is.True, _session.LastEventMessage);

            Assert.That(_session.WorldClock.CurrentDate,
                Is.EqualTo(new GameDate(1561, 1, 1)));
            Assert.That(_session.Succession.World.IsLiving(M2FixedScenario.SakonId),
                Is.False, "the assignee dies at the annual boundary");
            Assert.That(_session.CommissionStage, Is.EqualTo(ShadowEscortStage.Abandoned),
                "death wins over a commission that would become due on January 1");
            Assert.That(_session.CommissionOutcome, Is.EqualTo(CommissionOutcome.None),
                "no mission result is produced for the abandoned boundary commission");
            Assert.That(_session.CommissionStatus.RewardFunds, Is.Zero);
            Assert.That(_session.CommissionStatus.RewardSupplies, Is.Zero);
            Assert.That(_session.MissionHistory.Count, Is.EqualTo(historyBefore),
                "no mission history row is produced for the abandoned commission");
            Assert.That(_session.LastCommissionMessage, Does.Contain("离世"));
            Assert.That(_session.FamilyFame, Is.EqualTo(fameBefore),
                "abandonment grants no standing");
            Assert.That(_session.Concealment, Is.EqualTo(concealmentBefore));
            Assert.That(MissionsSignature(_session), Is.EqualTo(missionsBefore),
                "abandonment rolls no Missions value");

            FamilyChronicleEntry abandon = _session.ChronicleEntries.Last(entry =>
                entry.SubjectId == FamilyChronicleEntry.ShadowEscortCommissionId &&
                entry.SourceId == "abandon");
            Assert.That(abandon.Year, Is.EqualTo(1561));
            Assert.That(abandon.Month, Is.EqualTo(1));
            Assert.That(abandon.Summary, Does.Contain("离世"));
            Assert.That(abandon.Summary, Does.Contain("1561-01-01"),
                "the abandon row is dated to the boundary day it closed on");

            SaveData save = _session.CaptureSave();
            Assert.That(save.commission.stage, Is.EqualTo((int)ShadowEscortStage.Abandoned));
            Assert.That(save.commission.runId, Is.Empty);
            Assert.That(save.commission.outcome, Is.EqualTo((int)CommissionOutcome.None));
            SaveOperationResult validation = SaveDataValidator.Validate(save);
            Assert.That(validation.Succeeded, Is.True,
                "the same-date save must validate after the boundary death: " +
                validation.Message);
            Assert.That(save.chronicle, Has.Some.Matches<ChronicleSaveEntry>(entry =>
                    entry != null &&
                    entry.subjectId == FamilyChronicleEntry.ShadowEscortCommissionId &&
                    entry.sourceId == "abandon" &&
                    entry.summary != null && entry.summary.Contains("离世")),
                "the settlement leaves one readable abandon chronicle row");

            GameSession loaded = GameSession.NewGame();
            try
            {
                SaveOperationResult applied = loaded.TryApplySave(save);
                Assert.That(applied.Succeeded, Is.True, applied.Message);
                Assert.That(loaded.WorldClock.CurrentDate,
                    Is.EqualTo(new GameDate(1561, 1, 1)));
                Assert.That(loaded.CommissionStage, Is.EqualTo(ShadowEscortStage.Abandoned));
                Assert.That(loaded.CommissionAssignee, Is.EqualTo(M2FixedScenario.SakonId));
                Assert.That(loaded.MissionHistory, Is.Empty);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(loaded.gameObject);
            }
        }

        [Test]
        public void AnnualBoundary_UnrelatedDeathLeavesTheDueJan1CommissionSettlingNormally()
        {
            // Same December 29 -> January 1 delegated trip as the abandonment regression,
            // but the annual death list names Chie, not the assignee. The boundary close
            // therefore has nothing to abandon and the trip must still take its normal
            // January 1 settlement, with its own result and history row.
            ulong seed = FindSeed(stream =>
                stream.TryNextInt(1000, out int chieRoll) && chieRoll < 250 &&
                stream.TryNextInt(1000, out int headRoll) && headRoll >= 5 &&
                stream.TryNextInt(1000, out int assigneeRoll) && assigneeRoll >= 5 &&
                stream.TryNextInt(FamilyLifeCycleRules.MarriageRollExclusiveMax,
                    out int marriageRoll) &&
                marriageRoll >= FamilyLifeCycleRules.MarriagePercent);
            _session.InitNewGame(seed);
            KillAllExcept(M2FixedScenario.HarunobuId, M2FixedScenario.ChieId,
                M2FixedScenario.SakonId);
            _session.Succession.World.TrySetLife(M2FixedScenario.HarunobuId,
                new SuccessionPersonLife(1540, true, false), out _);
            _session.Succession.World.TrySetLife(M2FixedScenario.ChieId,
                new SuccessionPersonLife(1470, true, false), out _);
            _session.Succession.World.TrySetLife(M2FixedScenario.SakonId,
                new SuccessionPersonLife(1530, true, false), out _);

            AdvanceMonthsAndResolve(_session, 8);
            Assert.That(_session.AdvanceDays(28), Is.True, _session.LastEventMessage);
            Assert.That(_session.WorldClock.CurrentDate,
                Is.EqualTo(new GameDate(1560, 12, 29)));

            Assert.That(_session.TryAcceptCommission().Succeeded, Is.True,
                _session.LastCommissionMessage);
            Assert.That(_session.TryAssignCommission(M2FixedScenario.SakonId).Succeeded,
                Is.True, _session.LastCommissionMessage);
            Assert.That(_session.AdvanceDays(2), Is.True, _session.LastEventMessage);
            Assert.That(_session.WorldClock.CurrentDate,
                Is.EqualTo(new GameDate(1560, 12, 31)));
            Assert.That(_session.CommissionStage, Is.EqualTo(ShadowEscortStage.InProgress),
                "the delegated trip is due on January 1");
            Assert.That(_session.CommissionRemainingDays, Is.EqualTo(1));

            string missionsBefore = MissionsSignature(_session);
            int historyBefore = _session.MissionHistory.Count;
            int abandonRowsBefore = AbandonChronicleCount();

            Assert.That(_session.TryAdvanceToNextMonth(), Is.True, _session.LastEventMessage);

            Assert.That(_session.WorldClock.CurrentDate,
                Is.EqualTo(new GameDate(1561, 1, 1)));
            Assert.That(_session.Succession.World.IsLiving(M2FixedScenario.ChieId), Is.False,
                "the unrelated member dies at the annual boundary");
            Assert.That(_session.Succession.World.IsLiving(M2FixedScenario.SakonId), Is.True,
                "the commission assignee survives");
            Assert.That(_session.CommissionStage,
                Is.EqualTo(ShadowEscortStage.Succeeded)
                    .Or.EqualTo(ShadowEscortStage.PartialSuccess)
                    .Or.EqualTo(ShadowEscortStage.Failed),
                "the unrelated death does not abandon the commission due on January 1");
            Assert.That(_session.CommissionOutcome, Is.Not.EqualTo(CommissionOutcome.None),
                "the normal settlement produces a mission result");
            Assert.That(_session.MissionHistory.Count, Is.EqualTo(historyBefore + 1),
                "the normal settlement writes its terminal history row");
            MissionRecord settled =
                _session.MissionHistory[_session.MissionHistory.Count - 1];
            Assert.That(settled.CaptainId, Is.EqualTo(M2FixedScenario.SakonId));
            Assert.That(settled.Date, Is.EqualTo(new GameDate(1561, 1, 1)));
            Assert.That(settled.Mode, Is.EqualTo(MissionOperationMode.Delegated));
            Assert.That(AbandonChronicleCount(), Is.EqualTo(abandonRowsBefore),
                "no abandon row is written when the assignee survives");
            Assert.That(_session.ChronicleEntries.Any(entry =>
                    entry.SubjectId == FamilyChronicleEntry.ShadowEscortCommissionId &&
                    entry.SourceId == "settle"), Is.True,
                "the settlement records its own terminal action chronicle row");
            Assert.That(MissionsSignature(_session), Is.Not.EqualTo(missionsBefore),
                "the normal settlement still consumes its Missions roll");
            SaveOperationResult validation = SaveDataValidator.Validate(_session.CaptureSave());
            Assert.That(validation.Succeeded, Is.True,
                "the same-date save validates after the unrelated death: " + validation.Message);
        }

        [Test]
        public void AnnualBoundary_CommissionFinishedByDec31_RemainsTerminalThroughTheDeath()
        {
            ulong seed = FindSeed(stream =>
                stream.TryNextInt(1000, out int headRoll) && headRoll >= 5 &&
                stream.TryNextInt(1000, out int assigneeRoll) && assigneeRoll < 250);
            InitTwoMemberFamily(seed, harunobuBirthYear: 1540, sakonBirthYear: 1470);

            AdvanceMonthsAndResolve(_session, 8);
            Assert.That(_session.WorldClock.CurrentDate,
                Is.EqualTo(new GameDate(1560, 12, 1)));

            Assert.That(_session.TryAcceptCommission().Succeeded, Is.True,
                _session.LastCommissionMessage);
            Assert.That(_session.TryAssignCommission(M2FixedScenario.SakonId).Succeeded,
                Is.True, _session.LastCommissionMessage);

            Assert.That(_session.AdvanceDays(30), Is.True, _session.LastEventMessage);
            Assert.That(_session.WorldClock.CurrentDate,
                Is.EqualTo(new GameDate(1560, 12, 31)));
            ShadowEscortStage settledStage = _session.CommissionStage;
            Assert.That(settledStage, Is.Not.EqualTo(ShadowEscortStage.InProgress),
                "the delegated trip must finish before the annual boundary");
            Assert.That(settledStage, Is.Not.EqualTo(ShadowEscortStage.Abandoned));
            CommissionOutcome settledOutcome = _session.CommissionOutcome;
            int settledRewardFunds = _session.CommissionStatus.RewardFunds;
            int historyBefore = _session.MissionHistory.Count;
            int abandonRowsBefore = AbandonChronicleCount();

            Assert.That(_session.TryAdvanceToNextMonth(), Is.True, _session.LastEventMessage);

            Assert.That(_session.Succession.World.IsLiving(M2FixedScenario.SakonId),
                Is.False, "the assignee still dies at the annual boundary");
            Assert.That(_session.CommissionStage, Is.EqualTo(settledStage),
                "a commission already terminal on December 31 is not changed by the death");
            Assert.That(_session.CommissionOutcome, Is.EqualTo(settledOutcome));
            Assert.That(_session.CommissionStatus.RewardFunds, Is.EqualTo(settledRewardFunds));
            Assert.That(_session.MissionHistory.Count, Is.EqualTo(historyBefore));
            Assert.That(AbandonChronicleCount(), Is.EqualTo(abandonRowsBefore),
                "no abandon row is written for an already terminal commission");
        }

        [TestCase(0, 0)]
        [TestCase(2, 0)]
        [TestCase(3, 50)]
        [TestCase(5, 50)]
        [TestCase(6, 100)]
        [TestCase(9, 150)]
        [TestCase(12, 200)]
        [TestCase(15, 250)]
        [TestCase(40, 250)]
        public void FoodShortageMortalityBonus_MapsEachThreeMonthBandToFiftyPermille(
            int consecutiveMonths, int expectedBonus)
        {
            Assert.That(FamilyLifeCycleRules.MortalityBonusForFoodShortage(consecutiveMonths),
                Is.EqualTo(expectedBonus));
        }

        [Test]
        public void PlanNaturalDeaths_AddsTheBonusWithoutChangingTheRollOrder()
        {
            ulong seed = FindSeed(stream =>
                stream.TryNextInt(1000, out int first) && first >= 6 && first < 200 &&
                stream.TryNextInt(1000, out int second) && second >= 6 && second < 200);
            _session.InitNewGame(seed);
            KillAllExcept(M2FixedScenario.HarunobuId, M2FixedScenario.SakonId);
            _session.Succession.World.TrySetLife(M2FixedScenario.HarunobuId,
                new SuccessionPersonLife(1540, true, false), out _);
            _session.Succession.World.TrySetLife(M2FixedScenario.SakonId,
                new SuccessionPersonLife(1530, true, false), out _);
            IReadOnlyList<PersonId> candidates =
                FamilyLifeCycleRules.CaptureDeathCandidateIds(_session.Succession);
            GameDate december = GameDate.Parse(1560, 12, 1);
            GameDate january = GameDate.Parse(1561, 1, 1);

            DeterministicRandomStream baselineStream = RandomStateManager.Create(seed)
                .GetStream(RandomStreamKind.WorldSimulation);
            DeterministicRandomStream bonusStream = RandomStateManager.Create(seed)
                .GetStream(RandomStreamKind.WorldSimulation);
            IReadOnlyList<PersonId> baseline = FamilyLifeCycleRules.PlanNaturalDeaths(
                candidates, _session.Succession, december, january, baselineStream);
            IReadOnlyList<PersonId> boosted = FamilyLifeCycleRules.PlanNaturalDeaths(
                candidates, _session.Succession, december, january, bonusStream, 200);

            Assert.That(baseline, Is.Empty,
                "the 5 permille age probability alone keeps both rolls alive");
            Assert.That(boosted, Has.Count.EqualTo(2),
                "the +200 permille shortage bonus kills both of the same rolls");
            for (int i = 0; i < 6; i++)
            {
                Assert.That(baselineStream.TryNextInt(1000, out int withoutBonus), Is.True);
                Assert.That(bonusStream.TryNextInt(1000, out int withBonus), Is.True);
                Assert.That(withBonus, Is.EqualTo(withoutBonus),
                    "the bonus adds no draw and keeps one draw per candidate in ID order");
            }
        }

        // ---- fixtures and assertions ---------------------------------------

        /// <summary>
        /// Keeps Harunobu and Sakon alive with the supplied birth years and removes every
        /// other family member from life. With no fertile spouse pair the annual death
        /// check is the WorldSimulation stream's first draw in each fixture.
        /// </summary>
        private void InitTwoMemberFamily(ulong seed, int harunobuBirthYear,
            int sakonBirthYear)
        {
            _session.InitNewGame(seed);
            KillAllExcept(M2FixedScenario.HarunobuId, M2FixedScenario.SakonId);
            _session.Succession.World.TrySetLife(M2FixedScenario.HarunobuId,
                new SuccessionPersonLife(harunobuBirthYear, true, false), out _);
            _session.Succession.World.TrySetLife(M2FixedScenario.SakonId,
                new SuccessionPersonLife(sakonBirthYear, true, false), out _);
        }

        private void KillAllExcept(params PersonId[] keep)
        {
            foreach (PersonState person in _session.Succession.World.Persons)
            {
                if (keep.Contains(person.Id))
                    continue;
                _session.Succession.World.TryGetLife(person.Id, out SuccessionPersonLife life);
                _session.Succession.World.TrySetLife(person.Id,
                    new SuccessionPersonLife(life.BirthYear, false, life.Retired), out _);
            }
        }

        private void InjectPregnancy(PersonId mother, PersonId father, GameDate conception)
        {
            Assert.That(FamilyPregnancyRecord.TryCreate(mother, father, conception,
                FamilyPregnancyRecord.AddMonths(conception,
                    FamilyPregnancyRecord.GestationMonths),
                out FamilyPregnancyRecord record, out string error), Is.True, error);
            Assert.That(_session.Succession.World.Pregnancies.TryAdd(record, out error),
                Is.True, error);
        }

        private int ChronicleCount(ChronicleCategory category) =>
            _session.ChronicleEntries.Count(entry => entry.Category == category);

        private PersonId SpouseOf(PersonId candidateId) => SpouseOf(_session, candidateId);

        private static PersonId SpouseOf(GameSession session, PersonId candidateId)
        {
            foreach (KinshipLink link in session.Succession.World.Kinship.Links)
            {
                if (link.Type != KinshipType.Spouse)
                    continue;
                if (link.From == candidateId)
                    return link.To;
                if (link.To == candidateId)
                    return link.From;
            }
            return default;
        }

        /// <summary>The living endpoint of the candidate's formal spouse fact, if any.</summary>
        private static PersonId LivingSpouseOf(GameSession session, PersonId candidateId)
        {
            foreach (KinshipLink link in session.Succession.World.Kinship.Links)
            {
                if (link.Type != KinshipType.Spouse)
                    continue;
                PersonId other;
                if (link.From == candidateId)
                    other = link.To;
                else if (link.To == candidateId)
                    other = link.From;
                else
                    continue;
                if (session.Succession.World.IsLiving(other))
                    return other;
            }
            return default;
        }

        private FamilyChronicleEntry FirstChronicle(ChronicleCategory category) =>
            _session.ChronicleEntries.First(entry => entry.Category == category);

        private static string RngSignature(GameSession session)
        {
            RandomStateSaveData state = session.RandomState.CaptureState();
            var builder = new StringBuilder(state.initialSeedHex);
            foreach (RandomStreamSaveEntry entry in state.streams)
                builder.Append('|').Append(entry.streamId).Append(':')
                    .Append(entry.state0Hex).Append(entry.state1Hex);
            return builder.ToString();
        }

        /// <summary>Two-word signature of the persisted Missions stream only.</summary>
        private static string MissionsSignature(GameSession session)
        {
            RandomStateSaveData state = session.RandomState.CaptureState();
            string missionsId = RandomStateManager.StableId(RandomStreamKind.Missions);
            foreach (RandomStreamSaveEntry entry in state.streams)
                if (entry != null && entry.streamId == missionsId)
                    return entry.state0Hex + ":" + entry.state1Hex;
            return string.Empty;
        }

        private int AbandonChronicleCount() =>
            _session.ChronicleEntries.Count(entry =>
                entry.SubjectId == FamilyChronicleEntry.ShadowEscortCommissionId &&
                entry.SourceId == "abandon");

        private static ulong FindMarriageSeed(bool hit) =>
            FindSeed(stream =>
                stream.TryNextInt(1000, out int headRoll) && headRoll >= 5 &&
                stream.TryNextInt(1000, out int otherRoll) && otherRoll >= 5 &&
                stream.TryNextInt(100, out int marriageRoll) &&
                (hit ? marriageRoll < 15 : marriageRoll >= 15));

        private static ulong FindSeed(Func<DeterministicRandomStream, bool> predicate)
        {
            for (ulong seed = 1; seed <= 200000; seed++)
            {
                DeterministicRandomStream stream = RandomStateManager.Create(seed)
                    .GetStream(RandomStreamKind.WorldSimulation);
                if (predicate(stream))
                    return seed;
            }
            Assert.Fail("no deterministic seed matched the expected WorldSimulation rolls");
            return 0;
        }

        private static void AdvanceMonthsAndResolve(GameSession session, int months)
        {
            for (int i = 0; i < months; i++)
            {
                if (!session.TryAdvanceToNextMonth())
                    Assert.Fail(session.LastEventMessage);
                ResolvePending(session);
            }
        }

        private static void AdvanceAndResolve(GameSession session)
        {
            Assert.That(session.TryAdvanceToNextMonth(), Is.True, session.LastEventMessage);
            ResolvePending(session);
        }

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

        private sealed class GameSessionControl : IDisposable
        {
            public GameSessionControl(ulong seed)
            {
                Session = GameSession.NewGame();
                Session.InitNewGame(seed);
                foreach (PersonState person in Session.Succession.World.Persons)
                {
                    if (person.Id == M2FixedScenario.HarunobuId ||
                        person.Id == M2FixedScenario.SakonId)
                        continue;
                    Session.Succession.World.TryGetLife(person.Id,
                        out SuccessionPersonLife life);
                    Session.Succession.World.TrySetLife(person.Id,
                        new SuccessionPersonLife(life.BirthYear, false, life.Retired), out _);
                }
                Session.Succession.World.TrySetLife(M2FixedScenario.HarunobuId,
                    new SuccessionPersonLife(1540, true, false), out _);
                Session.Succession.World.TrySetLife(M2FixedScenario.SakonId,
                    new SuccessionPersonLife(1530, true, false), out _);
            }

            public GameSession Session { get; }

            public void Dispose()
            {
                if (Session != null)
                    UnityEngine.Object.DestroyImmediate(Session.gameObject);
            }
        }
    }
}
