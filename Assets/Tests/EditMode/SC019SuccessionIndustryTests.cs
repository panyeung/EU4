using NUnit.Framework;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;
using SengokuChronicle.Events;

namespace SengokuChronicle.Tests.EditMode
{
    /// <summary>SC-019 Task 4: succession handoffs release only the old head's post.</summary>
    public sealed class SC019SuccessionIndustryTests
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

        private static void Choose(GameSession session, string eventId, string optionId)
        {
            Assert.That(session.TryChooseEventOption(eventId, optionId,
                session.CreateEventResultId(eventId, optionId)).Succeeded, Is.True,
                session.LastEventMessage);
        }

        private void ReachFirstHandoffGate()
        {
            Assert.That(_session.TryAdvanceToNextMonth(), Is.True, _session.LastEventMessage);
            Choose(_session, M2FixedScenario.PledgeEventId, M2FixedScenario.BreakPromiseOptionId);
            Assert.That(_session.TryAdvanceToNextMonth(), Is.True, _session.LastEventMessage);
            Choose(_session, SampleEventIds.EventId, SampleEventIds.OfferRestOptionId);
        }

        private void ReachSecondHandoffGate()
        {
            Assert.That(_session.TryAdvanceToNextMonth(), Is.True, _session.LastEventMessage);
            Choose(_session, SC011EventIds.LegacyHonoredEventId, SC011EventIds.LegacyHonoredRenewOptionId);
        }

        [Test]
        public void FirstHandoff_VacatesOldHeadPostAndKeepsOtherAssignments()
        {
            _session.TryAssignIndustry(IndustryCatalog.FarmId, M2FixedScenario.HarunobuId);
            _session.TryAssignIndustry(IndustryCatalog.TradingHouseId, M2FixedScenario.ChieId);
            ReachFirstHandoffGate();

            SuccessionResult handoff = _session.BeginSuccessionRetirement();

            Assert.That(handoff.Succeeded, Is.True, handoff.Message);
            Assert.That(_session.SuccessionHeadName, Is.EqualTo("明智 正綱"));
            Assert.That(_session.Industry.Slot(IndustryCatalog.FarmId).AssigneeId.IsEmpty, Is.True,
                "the retired old head's farm post is released");
            Assert.That(_session.Industry.Slot(IndustryCatalog.TradingHouseId).AssigneeName,
                Is.EqualTo("明智 千恵"), "an unrelated lawful post is kept");
        }

        [Test]
        public void Handoff_DoesNotTransferBudgetOrAutoAssign()
        {
            _session.TryAssignIndustry(IndustryCatalog.FarmId, M2FixedScenario.HarunobuId);
            ReachFirstHandoffGate();
            _session.BeginSuccessionRetirement();

            Assert.That(_session.Industry.Slot(IndustryCatalog.FarmId).AssigneeId.IsEmpty, Is.True,
                "the new head must not inherit the old post");
            IndustryAssignmentResult assigned = _session.TryAssignIndustry(
                IndustryCatalog.FarmId, M2FixedScenario.MasatsunaId);
            Assert.That(assigned.Succeeded, Is.True, assigned.Message);
            Assert.That(_session.Industry.Slot(IndustryCatalog.FarmId).WorkUnitsRemaining, Is.EqualTo(1));
            Assert.That(_session.TryAssignIndustry(IndustryCatalog.TradingHouseId,
                M2FixedScenario.MasatsunaId).Succeeded, Is.False,
                "the new head has only his own 4 units, not the retired head's budget");
        }

        [Test]
        public void Handoff_AllowsSameMonthAppointmentUsingNewHeadOwnBudget()
        {
            _session.TryAssignIndustry(IndustryCatalog.FarmId, M2FixedScenario.HarunobuId);
            ReachFirstHandoffGate();
            _session.BeginSuccessionRetirement();

            IndustryAssignmentResult assigned = _session.TryAssignIndustry(
                IndustryCatalog.TradingHouseId, M2FixedScenario.MasatsunaId);

            Assert.That(assigned.Succeeded, Is.True, assigned.Message);
            Assert.That(_session.Industry.Slot(IndustryCatalog.TradingHouseId).AssigneeName,
                Is.EqualTo("明智 正綱"));
        }

        [Test]
        public void SecondHandoff_VacatesOldHeadPostAndNewHeadStartsUnassigned()
        {
            _session.TryAssignIndustry(IndustryCatalog.TradingHouseId, M2FixedScenario.ChieId);
            ReachFirstHandoffGate();
            _session.BeginSuccessionRetirement();
            _session.TryAssignIndustry(IndustryCatalog.FarmId, M2FixedScenario.MasatsunaId);
            ReachSecondHandoffGate();

            SuccessionResult handoff = _session.BeginSuccessionRetirement();

            Assert.That(handoff.Succeeded, Is.True, handoff.Message);
            Assert.That(_session.SuccessionHeadName, Is.EqualTo("明智 信正"));
            Assert.That(_session.Industry.Slot(IndustryCatalog.FarmId).AssigneeId.IsEmpty, Is.True);
            Assert.That(_session.Industry.Slot(IndustryCatalog.TradingHouseId).AssigneeName,
                Is.EqualTo("明智 千恵"));
            Assert.That(_session.TryAssignIndustry(IndustryCatalog.FarmId,
                M2FixedScenario.NobumasaId).Succeeded, Is.True);
        }

        [Test]
        public void Handoff_AppendsPersonnelChronicleOnce()
        {
            _session.TryAssignIndustry(IndustryCatalog.FarmId, M2FixedScenario.HarunobuId);
            _session.TryAssignIndustry(IndustryCatalog.TradingHouseId, M2FixedScenario.ChieId);
            ReachFirstHandoffGate();
            _session.BeginSuccessionRetirement();

            Assert.That(CountPersonnelEntries(), Is.EqualTo(1));
            // Repeating the same total cleanup must not append a second record.
            _session.TryAssignIndustry(IndustryCatalog.FarmId, M2FixedScenario.MasatsunaId);
            Assert.That(CountPersonnelEntries(), Is.EqualTo(1));

            ReachSecondHandoffGate();
            _session.BeginSuccessionRetirement();
            Assert.That(CountPersonnelEntries(), Is.EqualTo(2));
        }

        private int CountPersonnelEntries()
        {
            int count = 0;
            foreach (FamilyChronicleEntry entry in _session.ChronicleEntries)
                if (entry.Category == ChronicleCategory.Industry &&
                    entry.SubjectId == "industry.assignments")
                    count++;
            return count;
        }
    }
}
