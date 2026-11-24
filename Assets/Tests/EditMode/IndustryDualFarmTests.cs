using NUnit.Framework;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;
using SengokuChronicle.Persistence;

namespace SengokuChronicle.Tests.EditMode
{
    /// <summary>
    /// M08B option B (approved 2026-11-19): the farm owns two posts, each keeping the
    /// single-farm rules. Two staffed posts harvest 8 food per month, the approved supply
    /// target for 40 living members. The target is supply, never a population cap.
    /// </summary>
    public sealed class IndustryDualFarmTests
    {
        private static readonly GameDate Start = new GameDate(1560, 4, 1);
        private static readonly PersonId Harunobu = M2FixedScenario.HarunobuId;
        private static readonly PersonId Chie = M2FixedScenario.ChieId;

        private static SuccessionState Succession() => M2FixedScenario.CreateSuccession();

        private static IndustryState State(int funds, int supplies)
        {
            IndustryState state = IndustryState.CreateInitial(Start);
            Assert.That(IndustryResources.TryCreate(funds, supplies, 0,
                out IndustryResources resources, out string error), Is.True, error);
            state.TrySetResources(resources, out _);
            return state;
        }

        [Test]
        public void SecondPost_HarvestsItsOwnFoodAndChargesItsOwnUpkeep()
        {
            SuccessionState succession = Succession();
            IndustryState state = State(30, 12);
            Assert.That(IndustryService.TryAssign(state, succession, Start,
                IndustryCatalog.FarmId, Harunobu).Succeeded, Is.True);
            Assert.That(IndustryService.TryAssign(state, succession, Start,
                IndustryCatalog.FarmId, Chie).Succeeded, Is.True);

            Assert.That(state.TryGetAssignee(IndustryCatalog.FarmId, 0, out PersonId first), Is.True);
            Assert.That(state.TryGetAssignee(IndustryCatalog.FarmId, 1, out PersonId second), Is.True);
            Assert.That(first, Is.EqualTo(Harunobu));
            Assert.That(second, Is.EqualTo(Chie));

            IndustrySettlementResult result = IndustrySettlement.Plan(state, succession, Start);

            Assert.That(result.Succeeded, Is.True, result.Message);
            IndustryLineResult farmOne = Line(result.Report, IndustryCatalog.FarmId, 0);
            IndustryLineResult farmTwo = Line(result.Report, IndustryCatalog.FarmId, 1);
            Assert.That(farmOne.Status, Is.EqualTo(IndustryLineStatus.Operated));
            Assert.That(farmTwo.Status, Is.EqualTo(IndustryLineStatus.Operated));
            Assert.That(farmOne.GrossOutputFood, Is.EqualTo(4));
            Assert.That(farmTwo.GrossOutputFood, Is.EqualTo(4));
            Assert.That(farmOne.MaterialsCostFunds, Is.EqualTo(1));
            Assert.That(farmTwo.MaterialsCostFunds, Is.EqualTo(1));
            Assert.That(farmOne.NetOutputFunds, Is.EqualTo(1));
            Assert.That(farmTwo.NetOutputFunds, Is.EqualTo(1));
            Assert.That(result.NextState.Resources.Funds, Is.EqualTo(32),
                "two posts each net one fund after their own material cost");
            Assert.That(result.NextState.Resources.Supplies, Is.EqualTo(18),
                "each post retains one of its four harvested units");
            Assert.That(farmOne.IndustryName, Does.Contain("一号"));
            Assert.That(farmTwo.IndustryName, Does.Contain("二号"));
            Assert.That(result.Report.Summary, Does.Contain("产粮 +8")
                    .Or.Contain("产粮 +4"),
                "each operated post reports its own harvest line");
        }

        [Test]
        public void SecondPost_RejectsTheSamePersonOnBothFarms()
        {
            SuccessionState succession = Succession();
            IndustryState state = IndustryState.CreateInitial(Start);
            Assert.That(IndustryService.TryAssign(state, succession, Start,
                IndustryCatalog.FarmId, Harunobu).Succeeded, Is.True);

            IndustryAssignmentResult explicitSecond = IndustryService.TryAssign(state, succession,
                Start, IndustryCatalog.FarmId, 1, Harunobu);
            Assert.That(explicitSecond.Succeeded, Is.False);
            Assert.That(explicitSecond.Outcome,
                Is.EqualTo(IndustryAssignmentOutcome.NoRemainingUnits));

            IndustryAssignmentResult automatic = IndustryService.TryAssign(state, succession, Start,
                IndustryCatalog.FarmId, Harunobu);
            Assert.That(automatic.Succeeded, Is.True);
            Assert.That(automatic.Outcome, Is.EqualTo(IndustryAssignmentOutcome.NoChange),
                "the same person already holding a farm post is a no-op, not a second post");
            Assert.That(state.TryGetAssignee(IndustryCatalog.FarmId, 1, out PersonId second), Is.True);
            Assert.That(second.IsEmpty, Is.True);
        }

        [Test]
        public void FilledFarm_RefusesASilentEviction()
        {
            SuccessionState succession = Succession();
            IndustryState state = IndustryState.CreateInitial(Start);
            IndustryService.TryAssign(state, succession, Start, IndustryCatalog.FarmId, Harunobu);
            IndustryService.TryAssign(state, succession, Start, IndustryCatalog.FarmId, Chie);

            IndustryAssignmentResult third = IndustryService.TryAssign(state, succession, Start,
                IndustryCatalog.FarmId, M2FixedScenario.MasatsunaId);

            Assert.That(third.Succeeded, Is.False);
            Assert.That(third.Outcome, Is.EqualTo(IndustryAssignmentOutcome.NoVacantPost));
            Assert.That(third.Message, Does.Contain("解除"));
            Assert.That(state.TryGetAssignee(IndustryCatalog.FarmId, 0, out PersonId one), Is.True);
            Assert.That(state.TryGetAssignee(IndustryCatalog.FarmId, 1, out PersonId two), Is.True);
            Assert.That(one, Is.EqualTo(Harunobu));
            Assert.That(two, Is.EqualTo(Chie));

            IndustryAssignmentResult explicitReplace = IndustryService.TryAssign(state, succession,
                Start, IndustryCatalog.FarmId, 1, M2FixedScenario.MasatsunaId);
            Assert.That(explicitReplace.Succeeded, Is.True,
                "an explicit post command still replaces that post's holder");
            Assert.That(explicitReplace.PreviousAssignee, Is.EqualTo(Chie));
            Assert.That(state.TryGetAssignee(IndustryCatalog.FarmId, 1, out PersonId replaced), Is.True);
            Assert.That(replaced, Is.EqualTo(M2FixedScenario.MasatsunaId));
        }

        [Test]
        public void Unassign_ReleasesOneFarmPostPerCommand()
        {
            SuccessionState succession = Succession();
            IndustryState state = IndustryState.CreateInitial(Start);
            IndustryService.TryAssign(state, succession, Start, IndustryCatalog.FarmId, Harunobu);
            IndustryService.TryAssign(state, succession, Start, IndustryCatalog.FarmId, Chie);

            IndustryAssignmentResult first = IndustryService.TryUnassign(state, succession, Start,
                IndustryCatalog.FarmId);
            Assert.That(first.Succeeded, Is.True);
            Assert.That(first.PostIndex, Is.EqualTo(1));
            Assert.That(state.TryGetAssignee(IndustryCatalog.FarmId, 0, out PersonId still), Is.True);
            Assert.That(still, Is.EqualTo(Harunobu));

            IndustryAssignmentResult second = IndustryService.TryUnassign(state, succession, Start,
                IndustryCatalog.FarmId);
            Assert.That(second.PostIndex, Is.Zero);
            Assert.That(state.TryGetAssignee(IndustryCatalog.FarmId, 0, out PersonId empty), Is.True);
            Assert.That(empty.IsEmpty, Is.True);

            Assert.That(IndustryService.TryUnassign(state, succession, Start,
                IndustryCatalog.FarmId).Outcome, Is.EqualTo(IndustryAssignmentOutcome.NoChange));
            Assert.That(IndustryService.TryUnassign(state, succession, Start,
                IndustryCatalog.FarmId, 1).Outcome, Is.EqualTo(IndustryAssignmentOutcome.NoChange));
        }

        [Test]
        public void VacantSecondPost_ProducesNothingWhileTheFirstStillHarvests()
        {
            SuccessionState succession = Succession();
            IndustryState state = State(30, 12);
            IndustryService.TryAssign(state, succession, Start, IndustryCatalog.FarmId, Harunobu);

            IndustrySettlementResult result = IndustrySettlement.Plan(state, succession, Start);

            Assert.That(Line(result.Report, IndustryCatalog.FarmId, 0).GrossOutputFood,
                Is.EqualTo(4));
            IndustryLineResult vacant = Line(result.Report, IndustryCatalog.FarmId, 1);
            Assert.That(vacant.Status, Is.EqualTo(IndustryLineStatus.Vacant));
            Assert.That(vacant.GrossOutputFood, Is.Zero);
            Assert.That(vacant.RequiredSupplies, Is.Zero);
            Assert.That(result.NextState.Resources.Funds, Is.EqualTo(31));
        }

        [Test]
        public void TwoFarmsStallTogetherWhenTheMaterialIndexOutrunsTheirHarvest()
        {
            SuccessionState succession = Succession();
            IndustryState state = State(0, 12);
            IndustryService.TryAssign(state, succession, Start, IndustryCatalog.FarmId, Harunobu);
            IndustryService.TryAssign(state, succession, Start, IndustryCatalog.FarmId, Chie);

            IndustrySettlementResult result = IndustrySettlement.Plan(state, succession, Start, 5000);

            Assert.That(Line(result.Report, IndustryCatalog.FarmId, 0).Status,
                Is.EqualTo(IndustryLineStatus.Stalled));
            Assert.That(Line(result.Report, IndustryCatalog.FarmId, 1).Status,
                Is.EqualTo(IndustryLineStatus.Stalled));
            Assert.That(Line(result.Report, IndustryCatalog.FarmId, 0).GrossOutputFood, Is.Zero);
            Assert.That(Line(result.Report, IndustryCatalog.FarmId, 1).GrossOutputFood, Is.Zero);
            Assert.That(result.NextState.Resources.Funds, Is.Zero);
            Assert.That(result.Report.HasShortage, Is.True);
        }

        [Test]
        public void TwoFarmsStallWhenTheirHarvestCannotCoverTheMaterialCost()
        {
            SuccessionState succession = Succession();
            IndustryState state = State(0, 12);
            IndustryService.TryAssign(state, succession, Start, IndustryCatalog.FarmId, Harunobu);
            IndustryService.TryAssign(state, succession, Start, IndustryCatalog.FarmId, Chie);

            // ceil(1 * 3000 / 1000) = 3 funds per post against a 2-fund harvest.
            IndustrySettlementResult result = IndustrySettlement.Plan(state, succession, Start, 3000);

            IndustryLineResult one = Line(result.Report, IndustryCatalog.FarmId, 0);
            IndustryLineResult two = Line(result.Report, IndustryCatalog.FarmId, 1);
            Assert.That(one.Status, Is.EqualTo(IndustryLineStatus.Stalled));
            Assert.That(two.Status, Is.EqualTo(IndustryLineStatus.Stalled));
            Assert.That(one.Reason, Does.Contain("材料成本 3"));
            Assert.That(two.Reason, Does.Contain("材料成本 3"));
            Assert.That(result.NextState.Resources.Supplies, Is.EqualTo(12),
                "a stalled post draws no material and produces no food");
        }

        [Test]
        public void TradingHouseStallsForFundsWhileBothFarmsOperate()
        {
            SuccessionState succession = Succession();
            IndustryState state = State(0, 0);
            IndustryService.TryAssign(state, succession, Start, IndustryCatalog.FarmId, Harunobu);
            IndustryService.TryAssign(state, succession, Start, IndustryCatalog.FarmId, Chie);
            IndustryService.TryAssign(state, succession, Start, IndustryCatalog.TradingHouseId,
                M2FixedScenario.MasatsunaId);

            IndustrySettlementResult result = IndustrySettlement.Plan(state, succession, Start);

            Assert.That(Line(result.Report, IndustryCatalog.FarmId, 0).Status,
                Is.EqualTo(IndustryLineStatus.Operated));
            Assert.That(Line(result.Report, IndustryCatalog.FarmId, 1).Status,
                Is.EqualTo(IndustryLineStatus.Operated));
            IndustryLineResult trading = Line(result.Report, IndustryCatalog.TradingHouseId);
            Assert.That(trading.Status, Is.EqualTo(IndustryLineStatus.Stalled),
                "two harvest credits leave 2 funds, short of the trading house's 8");
            Assert.That(trading.Reason, Does.Contain("资金"));
            Assert.That(result.Report.HasShortage, Is.True);
            Assert.That(Length(result.Report), Is.EqualTo(8),
                "both farms still harvest their full output");
            Assert.That(result.NextState.Resources.Funds, Is.EqualTo(2));
        }

        [Test]
        public void TwoFarmsExactlyCoverTheFortyMemberSupplyTarget()
        {
            GameDate date = Start;
            SuccessionState succession = Succession();
            AddLivingMembers(succession, 40 - MarketSettlement.CountLivingFamilyMembers(succession));
            Assert.That(MarketSettlement.CountLivingFamilyMembers(succession), Is.EqualTo(40));
            Assert.That(MarketSettlement.FoodDemandForHousehold(40), Is.EqualTo(8));

            IndustryState state = State(30, 12);
            Assert.That(IndustryService.TryAssign(state, succession, date,
                IndustryCatalog.FarmId, Harunobu).Succeeded, Is.True);
            Assert.That(IndustryService.TryAssign(state, succession, date,
                IndustryCatalog.FarmId, Chie).Succeeded, Is.True);
            IndustrySettlementResult industry = IndustrySettlement.Plan(state, succession, date);
            Assert.That(industry.Succeeded, Is.True, industry.Message);

            MarketSettlementResult market = MarketSettlement.Plan(
                MarketState.CreateInitial(date, 0), industry.NextState.Resources, succession,
                industry.Report, date);

            Assert.That(market.Succeeded, Is.True, market.Message);
            Assert.That(market.Report.HouseholdMembers, Is.EqualTo(40));
            Assert.That(market.Report.FoodDemand, Is.EqualTo(8));
            Assert.That(market.Report.FoodProduced, Is.EqualTo(8));
            Assert.That(market.Report.FoodPurchased, Is.EqualTo(8));
            Assert.That(market.Report.FoodUnmet, Is.Zero,
                "two staffed farms feed the approved 40-member target when both operate");
            Assert.That(market.NextMarket.ConsecutiveFoodShortageMonths, Is.Zero);
        }

        [Test]
        public void FortyMemberTargetIsSupplyOnlyAndNeverCapsPopulation()
        {
            GameDate date = Start;
            SuccessionState succession = Succession();
            AddLivingMembers(succession, 45 - MarketSettlement.CountLivingFamilyMembers(succession));
            Assert.That(MarketSettlement.CountLivingFamilyMembers(succession), Is.EqualTo(45));
            Assert.That(MarketSettlement.FoodDemandForHousehold(45), Is.EqualTo(9),
                "demand keeps growing with the living roster");

            IndustryState state = State(30, 12);
            IndustryService.TryAssign(state, succession, date, IndustryCatalog.FarmId, Harunobu);
            IndustryService.TryAssign(state, succession, date, IndustryCatalog.FarmId, Chie);
            IndustrySettlementResult industry = IndustrySettlement.Plan(state, succession, date);

            MarketSettlementResult market = MarketSettlement.Plan(
                MarketState.CreateInitial(date, 0), industry.NextState.Resources, succession,
                industry.Report, date);

            Assert.That(market.Succeeded, Is.True, market.Message);
            Assert.That(market.Report.HouseholdMembers, Is.EqualTo(45),
                "45 living members stay in the household; nothing truncates the roster to 40");
            Assert.That(market.Report.FoodUnmet, Is.EqualTo(1),
                "beyond the target the shortage is the ordinary supply risk, not a hard cap");
            Assert.That(market.NextMarket.ConsecutiveFoodShortageMonths, Is.EqualTo(1));
        }

        [Test]
        public void SaveRoundTrip_PreservesBothFarmPostsAndRejectsDuplicatePeople()
        {
            GameSession session = GameSession.NewGame();
            try
            {
                Assert.That(session.TryAssignIndustry(IndustryCatalog.FarmId, 0,
                    Harunobu).Succeeded, Is.True);
                Assert.That(session.TryAssignIndustry(IndustryCatalog.FarmId, 1,
                    Chie).Succeeded, Is.True);

                SaveData data = session.CaptureSave();
                Assert.That(data.industry.slots.Length, Is.EqualTo(3));
                Assert.That(SaveDataValidator.Validate(data).Succeeded, Is.True);

                Assert.That(IndustryStateSaveData.TryRestore(data.industry, data.succession,
                    new GameDate(data.year, data.month, data.day), out IndustryState restored,
                    out string error), Is.True, error);
                Assert.That(restored.TryGetAssignee(IndustryCatalog.FarmId, 0, out PersonId one), Is.True);
                Assert.That(restored.TryGetAssignee(IndustryCatalog.FarmId, 1, out PersonId two), Is.True);
                Assert.That(one, Is.EqualTo(Harunobu));
                Assert.That(two, Is.EqualTo(Chie));

                SaveData duplicate = CloneSave(data);
                duplicate.industry.slots[1].assignedPersonId = Harunobu.Value;
                SaveOperationResult duplicateResult =
                    duplicate.industry.ValidateForSave(new GameDate(duplicate.year,
                        duplicate.month, duplicate.day), duplicate.succession);
                Assert.That(duplicateResult.Succeeded, Is.False);
                Assert.That(duplicateResult.Message, Does.Contain("同一人物"));

                SaveData missingPost = CloneSave(data);
                missingPost.industry.slots = new[]
                {
                    missingPost.industry.slots[0],
                    missingPost.industry.slots[2]
                };
                Assert.That(missingPost.industry.ValidateForSave(new GameDate(missingPost.year,
                    missingPost.month, missingPost.day), missingPost.succession).Succeeded, Is.False,
                    "a save that drops one farm post cannot be loaded");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(session.gameObject);
            }
        }

        [Test]
        public void GameSession_UnassignReleasesTheSecondFarmPostFirst()
        {
            GameSession session = GameSession.NewGame();
            try
            {
                Assert.That(session.TryAssignIndustry(IndustryCatalog.FarmId, 0,
                    Harunobu).Succeeded, Is.True);
                Assert.That(session.TryAssignIndustry(IndustryCatalog.FarmId, 1,
                    Chie).Succeeded, Is.True);

                IndustryAssignmentResult released = session.TryUnassignIndustry(IndustryCatalog.FarmId);

                Assert.That(released.Succeeded, Is.True, released.Message);
                Assert.That(released.PostIndex, Is.EqualTo(1));
                Assert.That(session.Industry.Slot(IndustryCatalog.FarmId, 0).AssigneeId,
                    Is.EqualTo(Harunobu));
                Assert.That(session.Industry.Slot(IndustryCatalog.FarmId, 1).AssigneeId.IsEmpty,
                    Is.True);
                Assert.That(released.Message, Does.Contain("二号"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(session.gameObject);
            }
        }

        private static void AddLivingMembers(SuccessionState succession, int count)
        {
            for (int i = 0; i < count; i++)
            {
                var id = new PersonId("person.test.target." + i.ToString("000"));
                Assert.That(succession.World.TryAddPerson(
                    new PersonState(id, "试验家人" + i, PersonGender.Male),
                    new SuccessionPersonLife(1530, true, false), out string personError),
                    Is.True, personError);
                Assert.That(succession.World.TryAddMember(id, FamilyMemberRole.CollateralKin,
                    out string memberError), Is.True, memberError);
            }
        }

        private static SaveData CloneSave(SaveData source) =>
            UnityEngine.JsonUtility.FromJson<SaveData>(UnityEngine.JsonUtility.ToJson(source));

        private static IndustryLineResult Line(IndustryMonthReport report, string industryId,
            int postIndex = 0)
        {
            foreach (IndustryLineResult line in report.Lines)
                if (line.PostIndex == postIndex &&
                    string.Equals(line.IndustryId, industryId, System.StringComparison.Ordinal))
                    return line;
            Assert.Fail("report has no line " + industryId + "#" + postIndex);
            return default;
        }

        private static int Length(IndustryMonthReport report)
        {
            int total = 0;
            foreach (IndustryLineResult line in report.Lines)
                total += line.GrossOutputFood;
            return total;
        }
    }
}
