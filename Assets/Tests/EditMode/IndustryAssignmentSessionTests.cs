using NUnit.Framework;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;

namespace SengokuChronicle.Tests.EditMode
{
    /// <summary>SC-019 Task 4: session-level assignment API, budget and safe save.</summary>
    public sealed class IndustryAssignmentSessionTests
    {
        private GameSession _session;

        [SetUp]
        public void SetUp() => _session = GameSession.NewGame();

        [TearDown]
        public void TearDown()
        {
            if (_session != null)
                UnityEngine.Object.DestroyImmediate(_session.gameObject);
        }

        [Test]
        public void Assign_UpdatesViewAndReservesThreeOfFourUnits()
        {
            IndustryAssignmentResult result = _session.TryAssignIndustry(
                IndustryCatalog.FarmId, M2FixedScenario.HarunobuId);

            Assert.That(result.Succeeded, Is.True, result.Message);
            IndustrySlotView farm = _session.Industry.Slot(IndustryCatalog.FarmId);
            Assert.That(farm.AssigneeName, Is.EqualTo("明智 晴信"));
            Assert.That(farm.WorkUnitsRequired, Is.EqualTo(3));
            Assert.That(farm.WorkUnitsRemaining, Is.EqualTo(1));
            Assert.That(farm.StatusReason, Is.EqualTo("运作中"));
            Assert.That(_session.Industry.Resources.Funds, Is.EqualTo(30),
                "assignment must not spend resources");
        }

        [Test]
        public void Assign_SecondPostForSamePersonIsRejectedWithReason()
        {
            _session.TryAssignIndustry(IndustryCatalog.FarmId, M2FixedScenario.HarunobuId);
            IndustryAssignmentResult second = _session.TryAssignIndustry(
                IndustryCatalog.TradingHouseId, M2FixedScenario.HarunobuId);

            Assert.That(second.Succeeded, Is.False);
            Assert.That(second.Outcome, Is.EqualTo(IndustryAssignmentOutcome.NoRemainingUnits));
            Assert.That(second.Message, Does.Contain("3/4"));
            Assert.That(_session.Industry.Slot(IndustryCatalog.TradingHouseId).AssigneeId.IsEmpty, Is.True);
        }

        [Test]
        public void Replace_ReleasesOldReservationImmediately()
        {
            _session.TryAssignIndustry(IndustryCatalog.FarmId, M2FixedScenario.HarunobuId);
            IndustryAssignmentResult replaced = _session.TryAssignIndustry(
                IndustryCatalog.FarmId, M2FixedScenario.ChieId);

            Assert.That(replaced.Succeeded, Is.True, replaced.Message);
            Assert.That(replaced.PreviousAssignee, Is.EqualTo(M2FixedScenario.HarunobuId));
            Assert.That(_session.Industry.Slot(IndustryCatalog.FarmId).AssigneeName,
                Is.EqualTo("明智 千恵"));

            IndustryAssignmentResult harunobuAgain = _session.TryAssignIndustry(
                IndustryCatalog.TradingHouseId, M2FixedScenario.HarunobuId);
            Assert.That(harunobuAgain.Succeeded, Is.True, "the old head got the 4-unit budget back");
        }

        [Test]
        public void Unassign_ClearsSlotAndIsIdempotent()
        {
            _session.TryAssignIndustry(IndustryCatalog.FarmId, M2FixedScenario.HarunobuId);
            Assert.That(_session.TryUnassignIndustry(IndustryCatalog.FarmId).Succeeded, Is.True);
            Assert.That(_session.Industry.Slot(IndustryCatalog.FarmId).AssigneeId.IsEmpty, Is.True);

            IndustryAssignmentResult again = _session.TryUnassignIndustry(IndustryCatalog.FarmId);
            Assert.That(again.Succeeded, Is.True);
            Assert.That(again.Outcome, Is.EqualTo(IndustryAssignmentOutcome.NoChange));
        }

        [Test]
        public void Assignment_BlockedByPendingEventOrSuccession()
        {
            Assert.That(_session.TryAdvanceToNextMonth(), Is.True, _session.LastEventMessage);
            Assert.That(_session.PendingEventId, Is.Not.Empty);

            IndustryAssignmentResult blocked = _session.TryAssignIndustry(
                IndustryCatalog.FarmId, M2FixedScenario.HarunobuId);
            Assert.That(blocked.Succeeded, Is.False);
            Assert.That(blocked.Outcome, Is.EqualTo(IndustryAssignmentOutcome.Blocked));
            Assert.That(blocked.Message, Does.Contain("事件"));
            Assert.That(_session.Industry.BlockReason, Does.Contain("事件"));
        }

        [Test]
        public void StartCrisisDemo_ReconcilesAssignmentsOutsideCrisisRoster()
        {
            _session.InitStandardDemo();
            Assert.That(_session.TryAssignIndustry(IndustryCatalog.FarmId,
                new PersonId("person.akechi.masatsuna")).Succeeded, Is.True);
            Assert.That(_session.TryAssignIndustry(IndustryCatalog.TradingHouseId,
                new PersonId("person.akechi.chie")).Succeeded, Is.True);

            Assert.That(_session.StartCrisisDemo(), Is.True);

            Assert.That(_session.Industry.Slot(IndustryCatalog.FarmId).AssigneeId.IsEmpty, Is.True,
                "a person absent from the crisis roster must not keep a post");
            Assert.That(_session.Industry.Slot(IndustryCatalog.TradingHouseId).AssigneeName,
                Is.EqualTo("明智 千恵"), "a still-valid crisis member keeps the post");
            Assert.That(_session.LastIndustryMessage, Does.Contain("已移除失效产业任命"));
        }

        [Test]
        public void ManualAssignment_DoesNotWriteChronicle()
        {
            int before = _session.ChronicleEntries.Count;
            _session.TryAssignIndustry(IndustryCatalog.FarmId, M2FixedScenario.HarunobuId);
            _session.TryAssignIndustry(IndustryCatalog.FarmId, M2FixedScenario.ChieId);
            _session.TryUnassignIndustry(IndustryCatalog.FarmId);

            Assert.That(_session.ChronicleEntries.Count, Is.EqualTo(before));
        }

        [Test]
        public void CaptureSave_RemovesInvalidAssignmentAndStillValidates()
        {
            _session.TryAssignIndustry(IndustryCatalog.FarmId, M2FixedScenario.ChieId);
            int chronicleBefore = _session.ChronicleEntries.Count;
            _session.Succession.World.TrySetLife(M2FixedScenario.ChieId,
                new SuccessionPersonLife(M2FixedScenario.ChieBirthYear, true, true), out _);

            var save = _session.CaptureSave();

            Assert.That(save.industry.slots[0].assignedPersonId, Is.Empty);
            Assert.That(SengokuChronicle.Persistence.SaveDataValidator.Validate(save).Succeeded, Is.True);
            Assert.That(_session.Industry.Slot(IndustryCatalog.FarmId).AssigneeId.IsEmpty, Is.True);
            Assert.That(_session.LastIndustryMessage, Does.Contain("失效"));
            Assert.That(_session.ChronicleEntries.Count, Is.EqualTo(chronicleBefore),
                "safe-save cleanup must not append history");
        }
    }
}
