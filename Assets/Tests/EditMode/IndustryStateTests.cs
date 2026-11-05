using NUnit.Framework;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;

namespace SengokuChronicle.Tests.EditMode
{
    /// <summary>SC-019 Task 1: resource bounds and slot mutation invariants.</summary>
    public sealed class IndustryStateTests
    {
        private static readonly GameDate Start = new GameDate(1560, 4, 1);
        private static readonly PersonId Harunobu = M2FixedScenario.HarunobuId;

        [Test]
        public void Resources_RejectNegativeAndOverCap()
        {
            Assert.That(IndustryResources.TryCreate(-1, 0, 0, out _, out _), Is.False);
            Assert.That(IndustryResources.TryCreate(0, -1, 0, out _, out _), Is.False);
            Assert.That(IndustryResources.TryCreate(0, 0, -1, out _, out _), Is.False);
            Assert.That(IndustryResources.TryCreate(1000000, 0, 0, out _, out _), Is.False);
            Assert.That(IndustryResources.TryCreate(0, 1000000, 0, out _, out _), Is.False);
            Assert.That(IndustryResources.TryCreate(0, 0, 1000000, out _, out _), Is.False);
            Assert.That(IndustryResources.TryCreate(999999, 999999, 999999,
                out IndustryResources capped, out string error), Is.True, error);
            Assert.That(capped.Funds, Is.EqualTo(999999));
        }

        [Test]
        public void TrySetAssignee_RejectsUnknownIndustryAndKeepsSlots()
        {
            IndustryState state = IndustryState.CreateInitial(Start);
            Assert.That(state.TrySetAssignee("industry.unknown", Harunobu, out string error), Is.False);
            Assert.That(error, Is.Not.Empty);
            Assert.That(state.TryGetAssignee(IndustryCatalog.FarmId, out PersonId assignee), Is.True);
            Assert.That(assignee.IsEmpty, Is.True);
        }

        [Test]
        public void TrySetAssignee_AndClear_AreReversible()
        {
            IndustryState state = IndustryState.CreateInitial(Start);
            Assert.That(state.TrySetAssignee(IndustryCatalog.FarmId, Harunobu, out string error), Is.True, error);
            Assert.That(state.TryGetAssignee(IndustryCatalog.FarmId, out PersonId assignee), Is.True);
            Assert.That(assignee, Is.EqualTo(Harunobu));

            Assert.That(state.TryClearAssignee(IndustryCatalog.FarmId, out error), Is.True, error);
            Assert.That(state.TryGetAssignee(IndustryCatalog.FarmId, out assignee), Is.True);
            Assert.That(assignee.IsEmpty, Is.True);
        }

        [Test]
        public void Clone_IsIndependentAndPreservesEveryField()
        {
            IndustryState state = IndustryState.CreateInitial(Start);
            state.TrySetAssignee(IndustryCatalog.TradingHouseId, Harunobu, out _);
            IndustryState clone = state.Clone();
            clone.TryClearAssignee(IndustryCatalog.TradingHouseId, out _);

            Assert.That(clone.LastSettledMonthIndex, Is.EqualTo(state.LastSettledMonthIndex));
            Assert.That(clone.Resources, Is.EqualTo(state.Resources));
            Assert.That(state.TryGetAssignee(IndustryCatalog.TradingHouseId, out PersonId original), Is.True);
            Assert.That(original, Is.EqualTo(Harunobu));
            Assert.That(clone.TryGetAssignee(IndustryCatalog.TradingHouseId, out PersonId copied), Is.True);
            Assert.That(copied.IsEmpty, Is.True);
        }

        [Test]
        public void TrySetResourcesAndLastSettled_RejectInvalidValues()
        {
            IndustryState state = IndustryState.CreateInitial(Start);
            Assert.That(state.TrySetLastSettledMonthIndex(-1, out _), Is.False);
            Assert.That(state.TrySetResources(default, out string error), Is.True, error);
            Assert.That(state.Resources.Funds, Is.Zero);
            Assert.That(state.Resources.Supplies, Is.Zero);
        }
    }
}
