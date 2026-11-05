using NUnit.Framework;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;

namespace SengokuChronicle.Tests.EditMode
{
    /// <summary>SC-019 Task 1: the two approved industries and their exact numbers.</summary>
    public sealed class IndustryCatalogTests
    {
        private static readonly GameDate Start = new GameDate(1560, 4, 1);

        [Test]
        public void Catalog_HasFarmAndTradingHouseWithApprovedNumbers()
        {
            Assert.That(IndustryCatalog.All.Count, Is.EqualTo(2));
            Assert.That(IndustryCatalog.All[0].Id, Is.EqualTo(IndustryCatalog.FarmId));
            Assert.That(IndustryCatalog.All[1].Id, Is.EqualTo(IndustryCatalog.TradingHouseId));

            Assert.That(IndustryCatalog.TryGet(IndustryCatalog.FarmId, out IndustryDefinition farm), Is.True);
            Assert.That(farm.DisplayName, Is.EqualTo("农庄"));
            Assert.That(farm.WorkUnitsRequired, Is.EqualTo(3));
            Assert.That(farm.CapacityPosts, Is.EqualTo(1));
            Assert.That(farm.UpkeepFunds, Is.EqualTo(0));
            Assert.That(farm.UpkeepSupplies, Is.EqualTo(1));
            Assert.That(farm.OutputFunds, Is.EqualTo(2));
            Assert.That(farm.OutputSupplies, Is.EqualTo(4));
            Assert.That(farm.OutputIntelligence, Is.EqualTo(0));
            Assert.That(farm.MaintainsFromOutput, Is.True);

            Assert.That(IndustryCatalog.TryGet(IndustryCatalog.TradingHouseId,
                out IndustryDefinition trading), Is.True);
            Assert.That(trading.DisplayName, Is.EqualTo("商馆"));
            Assert.That(trading.WorkUnitsRequired, Is.EqualTo(3));
            Assert.That(trading.CapacityPosts, Is.EqualTo(1));
            Assert.That(trading.UpkeepFunds, Is.EqualTo(6));
            Assert.That(trading.UpkeepSupplies, Is.EqualTo(2));
            Assert.That(trading.OutputFunds, Is.EqualTo(9));
            Assert.That(trading.OutputSupplies, Is.EqualTo(0));
            Assert.That(trading.OutputIntelligence, Is.EqualTo(1));
            Assert.That(trading.MaintainsFromOutput, Is.False);
        }

        [Test]
        public void Catalog_RejectsUnknownIds()
        {
            Assert.That(IndustryCatalog.TryGet("industry.unknown", out _), Is.False);
            Assert.That(IndustryCatalog.TryGet(null, out _), Is.False);
            Assert.That(IndustryCatalog.TryGet(string.Empty, out _), Is.False);
        }

        [Test]
        public void CreateInitial_UsesApprovedOpeningResourcesAndVacantSlots()
        {
            IndustryState state = IndustryState.CreateInitial(Start);

            Assert.That(state.Resources.Funds, Is.EqualTo(30));
            Assert.That(state.Resources.Supplies, Is.EqualTo(12));
            Assert.That(state.Resources.Intelligence, Is.EqualTo(0));
            Assert.That(state.LastSettledMonthIndex,
                Is.EqualTo(IndustryCalendar.MonthIndex(new GameDate(1560, 3, 1))));
            Assert.That(state.Assignments.Count, Is.EqualTo(2));
            Assert.That(state.Assignments[0].IndustryId, Is.EqualTo(IndustryCatalog.FarmId));
            Assert.That(state.Assignments[0].PersonId.IsEmpty, Is.True);
            Assert.That(state.Assignments[1].IndustryId, Is.EqualTo(IndustryCatalog.TradingHouseId));
            Assert.That(state.Assignments[1].PersonId.IsEmpty, Is.True);
            Assert.That(state.TryGetAssignee(IndustryCatalog.FarmId, out PersonId farmAssignee), Is.True);
            Assert.That(farmAssignee.IsEmpty, Is.True);
        }

        [Test]
        public void MonthIndex_UsesZeroBasedMonthArithmetic()
        {
            Assert.That(IndustryCalendar.MonthIndex(new GameDate(1560, 1, 1)), Is.EqualTo(18720));
            Assert.That(IndustryCalendar.MonthIndex(new GameDate(1560, 3, 1)), Is.EqualTo(18722));
            Assert.That(IndustryCalendar.MonthIndex(new GameDate(1560, 4, 1)), Is.EqualTo(18723));
            Assert.That(IndustryCalendar.MonthIndex(new GameDate(1561, 1, 1)), Is.EqualTo(18732));
        }

        [Test]
        public void Catalog_MatchesTheFixedSc013RosterAdultIds()
        {
            SuccessionState succession = M2FixedScenario.CreateSuccession();
            foreach (FamilyMembership membership in succession.World.Family.Members)
            {
                Assert.That(succession.World.TryGetPerson(membership.PersonId, out _), Is.True);
                Assert.That(IndustryService.CanWork(succession, membership.PersonId, Start, out string reason),
                    Is.True, reason);
            }
        }
    }
}
