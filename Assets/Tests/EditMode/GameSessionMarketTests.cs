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
    /// M08B Task 2: the market settlement is part of the atomic month transaction. These
    /// tests drive the real SC-013 new game so every assertion reads the same production,
    /// household consumption, price update and annual-boundary path a player uses.
    /// </summary>
    public sealed class GameSessionMarketTests
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
        public void NewGame_StartsWithOneMonthOfFoodAndBasePrices()
        {
            MarketState market = _session.Market;

            Assert.That(market.FoodInventory, Is.EqualTo(2),
                "the SC-013 six-person family needs ceil(6/5) = 2 food units");
            Assert.That(market.FoodPriceIndexPermille,
                Is.EqualTo(MarketState.BaselinePriceIndexPermille));
            Assert.That(market.MaterialsPriceIndexPermille,
                Is.EqualTo(MarketState.BaselinePriceIndexPermille));
            Assert.That(market.ConsecutiveFoodShortageMonths, Is.Zero);
            Assert.That(market.LastSettledMonthIndex,
                Is.EqualTo(IndustryCalendar.MonthIndex(_session.WorldClock.CurrentDate) - 1));
            Assert.That(_session.Industry.Resources.Supplies, Is.EqualTo(12));
            Assert.That(_session.Industry.Resources.Funds, Is.EqualTo(30));
            Assert.That(_session.LastMarketReport, Is.Null);
        }

        [Test]
        public void MonthAdvance_ProducesFoodAndPaysTheHouseholdBillInTheSameTransaction()
        {
            Assert.That(_session.TryAssignIndustry(IndustryCatalog.FarmId,
                M2FixedScenario.HarunobuId).Succeeded, Is.True, _session.LastIndustryMessage);

            Assert.That(_session.TryAdvanceToNextMonth(), Is.True, _session.LastEventMessage);

            MarketMonthReport report = _session.LastMarketReport;
            Assert.That(report, Is.Not.Null, "a settled month always publishes its market report");
            Assert.That(report.HouseholdMembers, Is.EqualTo(6),
                "the month-start living-family snapshot is the demand basis");
            Assert.That(report.FoodDemand, Is.EqualTo(2));
            Assert.That(report.FoodProduced, Is.EqualTo(4), "the farm harvests four food units");
            Assert.That(report.FoodPurchased, Is.EqualTo(2));
            Assert.That(report.FoodUnmet, Is.Zero);
            Assert.That(report.FoodBillFunds, Is.EqualTo(2));

            Assert.That(_session.Market.FoodInventory, Is.EqualTo(4),
                "2 opening + 4 harvested - 2 consumed");
            Assert.That(_session.Market.ConsecutiveFoodShortageMonths, Is.Zero);
            Assert.That(_session.Market.FoodPriceIndexPermille, Is.EqualTo(900),
                "a six-unit supply against two units of demand hits the -100 permille monthly cap");
            Assert.That(_session.Industry.Resources.Supplies, Is.EqualTo(15),
                "farm materials: 12 - 1 retained + 4 produced");
            Assert.That(_session.Industry.Resources.Funds, Is.EqualTo(29),
                "30 + 2 gross farm funds - 1 material cost - 2 food bill");
            Assert.That(_session.Market.LastSettledMonthIndex,
                Is.EqualTo(IndustryCalendar.MonthIndex(_session.WorldClock.CurrentDate) - 1));
        }

        [Test]
        public void MonthAdvance_SettlesEveryMonthExactlyOnce()
        {
            AdvanceAndResolve(_session);
            int firstSettled = _session.Market.LastSettledMonthIndex;
            AdvanceAndResolve(_session);

            Assert.That(_session.Market.LastSettledMonthIndex, Is.EqualTo(firstSettled + 1));
            Assert.That(_session.Market.LastSettledMonthIndex,
                Is.EqualTo(IndustryCalendar.MonthIndex(_session.WorldClock.CurrentDate) - 1));
            Assert.That(_session.Market.RecentCompositeIndices, Has.Count.EqualTo(2),
                "the twelve-month basket history gains exactly one settled month per advance");
        }

        [Test]
        public void MonthAdvance_WithNoFunds_BuysNothingAndRecordsShortage()
        {
            SaveData withoutFunds = _session.CaptureSave();
            withoutFunds.industry.funds = 0;
            Assert.That(_session.TryApplySave(withoutFunds).Succeeded, Is.True,
                _session.LastEventMessage);

            Assert.That(_session.TryAdvanceToNextMonth(), Is.True, _session.LastEventMessage);

            MarketMonthReport report = _session.LastMarketReport;
            Assert.That(report.FoodPurchased, Is.Zero, "no funds means no purchase and no debt");
            Assert.That(report.FoodBillFunds, Is.Zero);
            Assert.That(report.FoodUnmet, Is.EqualTo(2));
            Assert.That(_session.Industry.Resources.Funds, Is.Zero, "funds never go negative");
            Assert.That(_session.Market.FoodInventory, Is.EqualTo(2),
                "unaffordable food stays in stock instead of being consumed");
            Assert.That(_session.Market.ConsecutiveFoodShortageMonths, Is.EqualTo(1));
        }

        [Test]
        public void MonthAdvance_AfterShortage_ResetsOnTheFirstFullySatisfiedMonth()
        {
            AdvanceAndResolve(_session); // opening stock covers this month
            AdvanceAndResolve(_session); // empty stock: first shortage
            Assert.That(_session.Market.ConsecutiveFoodShortageMonths, Is.EqualTo(1));

            Assert.That(_session.TryAssignIndustry(IndustryCatalog.FarmId,
                M2FixedScenario.HarunobuId).Succeeded, Is.True, _session.LastIndustryMessage);
            AdvanceAndResolve(_session);

            Assert.That(_session.LastMarketReport.FoodUnmet, Is.Zero);
            Assert.That(_session.Market.ConsecutiveFoodShortageMonths, Is.Zero,
                "one fully satisfied month clears the consecutive-shortage count");
        }

        [Test]
        public void ThreeMonthsOfShortage_RaiseTheAnnualDeathCheckForTheSameSeed()
        {
            // The two-member fixture makes the annual death check the WorldSimulation
            // stream's first draw; the rolls are equal for both sessions below.
            ulong seed = FindAnnualSeedForBonusWindow();
            using (var shortage = new TwoMemberSession(seed))
            using (var fed = new TwoMemberSession(seed))
            {
                Assert.That(fed.Session.TryAssignIndustry(IndustryCatalog.FarmId,
                    M2FixedScenario.HarunobuId).Succeeded, Is.True);

                for (int i = 0; i < 9; i++)
                {
                    AdvanceAndResolve(shortage.Session);
                    AdvanceAndResolve(fed.Session);
                }

                Assert.That(shortage.Session.Market.ConsecutiveFoodShortageMonths,
                    Is.GreaterThanOrEqualTo(3));
                Assert.That(fed.Session.Market.ConsecutiveFoodShortageMonths, Is.Zero);
                Assert.That(FamilyLifeCycleRules.MortalityBonusForFoodShortage(
                    shortage.Session.Market.ConsecutiveFoodShortageMonths),
                    Is.GreaterThanOrEqualTo(50));

                Assert.That(shortage.Session.Succession.World.IsLiving(
                    M2FixedScenario.HarunobuId), Is.True,
                    "the head's roll was outside the base probability in both sessions");
                Assert.That(fed.Session.Succession.World.IsLiving(
                    M2FixedScenario.SakonId), Is.True,
                    "without shortage the age probability alone does not kill this person");
                Assert.That(shortage.Session.Succession.World.IsLiving(
                    M2FixedScenario.SakonId), Is.False,
                    "the shortage bonus turns the same roll into a death");
            }
        }

        [Test]
        public void FailedMonthSettlement_RollsBackMarketResourcesPopulationDateAndRandom()
        {
            Assert.That(_session.TryAssignIndustry(IndustryCatalog.FarmId,
                M2FixedScenario.HarunobuId).Succeeded, Is.True, _session.LastIndustryMessage);
            AdvanceAndResolve(_session);

            GameDate dateBefore = _session.WorldClock.CurrentDate;
            MarketState marketBefore = _session.Market;
            IndustryResources resourcesBefore = _session.Industry.Resources;
            int personsBefore = _session.Succession.World.PersonCount;
            int chronicleBefore = _session.ChronicleEntries.Count;
            string rngBefore = RngSignature(_session);
            MarketMonthReport reportBefore = _session.LastMarketReport;

            _session.MonthSettlementFaultInjection = () =>
                throw new InvalidOperationException("market rollback probe");
            try
            {
                Assert.That(_session.TryAdvanceToNextMonth(), Is.False);
            }
            finally
            {
                _session.MonthSettlementFaultInjection = null;
            }

            Assert.That(_session.LastMonthTurnOutcome,
                Is.EqualTo(MonthTurnOutcome.SettlementFailed));
            Assert.That(_session.WorldClock.CurrentDate, Is.EqualTo(dateBefore));
            Assert.That(_session.Market.FoodInventory, Is.EqualTo(marketBefore.FoodInventory));
            Assert.That(_session.Market.FoodPriceIndexPermille,
                Is.EqualTo(marketBefore.FoodPriceIndexPermille));
            Assert.That(_session.Market.MaterialsPriceIndexPermille,
                Is.EqualTo(marketBefore.MaterialsPriceIndexPermille));
            Assert.That(_session.Market.LastSettledMonthIndex,
                Is.EqualTo(marketBefore.LastSettledMonthIndex));
            Assert.That(_session.Market.ConsecutiveFoodShortageMonths,
                Is.EqualTo(marketBefore.ConsecutiveFoodShortageMonths));
            Assert.That(_session.Industry.Resources, Is.EqualTo(resourcesBefore));
            Assert.That(_session.Succession.World.PersonCount, Is.EqualTo(personsBefore));
            Assert.That(_session.ChronicleEntries.Count, Is.EqualTo(chronicleBefore));
            Assert.That(RngSignature(_session), Is.EqualTo(rngBefore));
            Assert.That(_session.LastMarketReport, Is.SameAs(reportBefore));
        }

        [Test]
        public void SameSeedAndSave_ReplayTheSamePricesConsumptionAndDeaths()
        {
            using (var first = new TwoMemberSession(9173UL))
            using (var replay = new TwoMemberSession(9173UL))
            {
                for (int i = 0; i < 9; i++)
                {
                    AdvanceAndResolve(first.Session);
                    AdvanceAndResolve(replay.Session);
                }

                Assert.That(MarketSignature(replay.Session),
                    Is.EqualTo(MarketSignature(first.Session)));
                Assert.That(RngSignature(replay.Session), Is.EqualTo(RngSignature(first.Session)),
                    "the market never consumes or reorders random draws");
                Assert.That(LivingSignature(replay.Session),
                    Is.EqualTo(LivingSignature(first.Session)));
            }
        }

        [Test]
        public void LoadedInvalidMarket_IsRejectedWithoutOverwritingTheLiveSession()
        {
            AdvanceAndResolve(_session);
            int foodBefore = _session.Market.FoodInventory;
            int foodPriceBefore = _session.Market.FoodPriceIndexPermille;

            SaveData invalidPrice = _session.CaptureSave();
            invalidPrice.market.foodPriceIndexPermille =
                MarketState.MinimumPriceIndexPermille - 1;
            Assert.That(_session.TryApplySave(invalidPrice).Succeeded, Is.False);

            SaveData invalidInventory = _session.CaptureSave();
            invalidInventory.market.foodInventory = -1;
            Assert.That(_session.TryApplySave(invalidInventory).Succeeded, Is.False);

            SaveData missing = _session.CaptureSave();
            missing.market = null;
            SaveOperationResult missingResult = _session.TryApplySave(missing);
            Assert.That(missingResult.Succeeded, Is.False);
            Assert.That(missingResult.Message, Does.Contain("market"));

            Assert.That(_session.Market.FoodInventory, Is.EqualTo(foodBefore));
            Assert.That(_session.Market.FoodPriceIndexPermille, Is.EqualTo(foodPriceBefore),
                "a rejected load never partially applies the market block");
        }

        [Test]
        public void MaterialsPriceFromThePriorMonth_PricesTheFollowingIndustryMonth()
        {
            Assert.That(_session.TryAssignIndustry(IndustryCatalog.TradingHouseId,
                M2FixedScenario.ChieId).Succeeded, Is.True, _session.LastIndustryMessage);
            SaveData stocked = _session.CaptureSave();
            stocked.industry.supplies = 200;
            Assert.That(_session.TryApplySave(stocked).Succeeded, Is.True,
                _session.LastEventMessage);

            // Every month demands two material units against a large surplus, so the
            // settled index falls by the -100‰ cap and the next month's material cost is
            // computed from that settled index instead of the fixed 1000‰ base.
            int[] expectedIndexAfterMonth = { 900, 810, 729, 656, 590, 531, 478, 430 };
            int[] expectedMaterialCost = { 2, 2, 2, 2, 2, 2, 2, 1 };
            for (int month = 0; month < expectedMaterialCost.Length; month++)
            {
                AdvanceAndResolve(_session);

                Assert.That(TradingHouseLine().MaterialsCostFunds,
                    Is.EqualTo(expectedMaterialCost[month]),
                    "month " + (month + 1) + " must be priced from the prior settled index");
                Assert.That(_session.Market.MaterialsPriceIndexPermille,
                    Is.EqualTo(expectedIndexAfterMonth[month]),
                    "month " + (month + 1) + " settled index");
            }
        }

        [Test]
        public void LoadedMarket_RejectsInvalidMonthShortageAndHistory()
        {
            AdvanceAndResolve(_session);
            int foodBefore = _session.Market.FoodInventory;
            int settledBefore = _session.Market.LastSettledMonthIndex;

            SaveData wrongMonth = _session.CaptureSave();
            wrongMonth.market.lastSettledMonthIndex = settledBefore + 1;
            AssertLoadRejected(wrongMonth, "已结算月份");

            SaveData negativeShortage = _session.CaptureSave();
            negativeShortage.market.consecutiveFoodShortageMonths = -1;
            AssertLoadRejected(negativeShortage, "不能为负");

            SaveData unorderedHistory = _session.CaptureSave();
            unorderedHistory.market.compositeHistory = new[]
            {
                new MarketIndexPointSaveEntry { monthIndex = 5, compositeIndexPermille = 1000 },
                new MarketIndexPointSaveEntry { monthIndex = 4, compositeIndexPermille = 1000 }
            };
            AssertLoadRejected(unorderedHistory, "递增");

            SaveData oversizedHistory = _session.CaptureSave();
            var tooLong = new MarketIndexPointSaveEntry[
                MarketState.CompositeHistoryCapacity + 1];
            for (int i = 0; i < tooLong.Length; i++)
                tooLong[i] = new MarketIndexPointSaveEntry
                {
                    monthIndex = i + 1,
                    compositeIndexPermille = 1000
                };
            oversizedHistory.market.compositeHistory = tooLong;
            AssertLoadRejected(oversizedHistory,
                MarketState.CompositeHistoryCapacity.ToString());

            SaveData missingHistory = _session.CaptureSave();
            missingHistory.market.compositeHistory = null;
            AssertLoadRejected(missingHistory, "历史");

            Assert.That(_session.Market.FoodInventory, Is.EqualTo(foodBefore));
            Assert.That(_session.Market.LastSettledMonthIndex, Is.EqualTo(settledBefore),
                "every rejected market load leaves the live session untouched");
        }

        [Test]
        public void MarketSaveData_CloneCopiesCompositeHistoryIndependently()
        {
            AdvanceAndResolve(_session);
            MarketSaveData original = _session.CaptureSave().market;
            Assert.That(original.compositeHistory, Is.Not.Empty);

            MarketSaveData clone = original.Clone();

            Assert.That(clone.compositeHistory, Is.Not.SameAs(original.compositeHistory),
                "the history array must be a deep copy");
            clone.compositeHistory[0].monthIndex = original.compositeHistory[0].monthIndex + 100;
            clone.compositeHistory[0].compositeIndexPermille = 1234;
            clone.foodInventory = original.foodInventory + 1;

            Assert.That(original.compositeHistory[0].monthIndex,
                Is.Not.EqualTo(clone.compositeHistory[0].monthIndex));
            Assert.That(original.compositeHistory[0].compositeIndexPermille, Is.Not.EqualTo(1234));
            Assert.That(original.foodInventory, Is.Not.EqualTo(clone.foodInventory));

            var malformed = new MarketSaveData { compositeHistory = null };
            Assert.That(malformed.Clone().compositeHistory, Is.Null,
                "a malformed block stays malformed so validation rejects it");
        }

        // ---- fixtures and assertions ---------------------------------------

        private IndustryLineResult TradingHouseLine()
        {
            IndustryMonthReport report = _session.LastIndustryReport;
            Assert.That(report, Is.Not.Null);
            foreach (IndustryLineResult line in report.Lines)
                if (line.IndustryId == IndustryCatalog.TradingHouseId)
                    return line;
            Assert.Fail("the trading house line is missing from the industry report");
            return default;
        }

        private void AssertLoadRejected(SaveData data, string expectedFragment)
        {
            SaveOperationResult result = _session.TryApplySave(data);
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Message, Does.Contain(expectedFragment));
        }

        private static ulong FindAnnualSeedForBonusWindow()
        {
            for (ulong seed = 1; seed <= 200000; seed++)
            {
                DeterministicRandomStream stream = RandomStateManager.Create(seed)
                    .GetStream(RandomStreamKind.WorldSimulation);
                if (!stream.TryNextInt(1000, out int harunobuRoll) || harunobuRoll < 5)
                    continue;
                if (!stream.TryNextInt(1000, out int sakonRoll))
                    continue;
                // Age 31 has a 5 permille base probability. A roll of 50 or more needs
                // the shortage bonus to die and stays alive without it.
                if (sakonRoll >= 6 && sakonRoll < 100)
                    return seed;
            }

            Assert.Fail("no deterministic seed matched the annual death rolls");
            return 0;
        }

        private static string MarketSignature(GameSession session)
        {
            MarketState market = session.Market;
            var builder = new StringBuilder();
            builder.Append(market.FoodInventory).Append('|')
                .Append(market.FoodPriceIndexPermille).Append('|')
                .Append(market.MaterialsPriceIndexPermille).Append('|')
                .Append(market.LastSettledMonthIndex).Append('|')
                .Append(market.ConsecutiveFoodShortageMonths);
            foreach (MarketIndexPoint point in market.RecentCompositeIndices)
                builder.Append('|').Append(point.MonthIndex).Append(':')
                    .Append(point.CompositeIndexPermille);
            return builder.ToString();
        }

        private static string LivingSignature(GameSession session)
        {
            var builder = new StringBuilder();
            foreach (PersonState person in session.Succession.World.Persons)
                builder.Append(person.Id.Value).Append(':')
                    .Append(session.Succession.World.IsLiving(person.Id) ? '1' : '0')
                    .Append('|');
            return builder.ToString();
        }

        private static string RngSignature(GameSession session)
        {
            RandomStateSaveData state = session.RandomState.CaptureState();
            var builder = new StringBuilder(state.initialSeedHex);
            foreach (RandomStreamSaveEntry entry in state.streams)
                builder.Append('|').Append(entry.streamId).Append(':')
                    .Append(entry.state0Hex).Append(entry.state1Hex);
            return builder.ToString();
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

        /// <summary>
        /// Keeps Harunobu and Sakon alive, removes every other member from life and makes
        /// the annual death check the WorldSimulation stream's first draw.
        /// </summary>
        private sealed class TwoMemberSession : IDisposable
        {
            public TwoMemberSession(ulong seed)
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
