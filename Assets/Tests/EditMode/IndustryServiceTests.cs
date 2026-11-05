using NUnit.Framework;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;

namespace SengokuChronicle.Tests.EditMode
{
    /// <summary>SC-019 Task 1: eligibility, the 4/3 work budget, replacement and idempotence.</summary>
    public sealed class IndustryServiceTests
    {
        private static readonly GameDate Start = new GameDate(1560, 4, 1);
        private static readonly PersonId Harunobu = M2FixedScenario.HarunobuId;
        private static readonly PersonId Chie = M2FixedScenario.ChieId;
        private static readonly PersonId Masatsuna = M2FixedScenario.MasatsunaId;
        private static readonly PersonId Nobumasa = M2FixedScenario.NobumasaId;
        private static readonly PersonId Tokihime = M2FixedScenario.TokihimeId;
        private static readonly PersonId Sakon = M2FixedScenario.SakonId;

        private static SuccessionState Succession() => M2FixedScenario.CreateSuccession();

        [Test]
        public void CanWork_AcceptsEveryLivingAdultMember()
        {
            SuccessionState succession = Succession();
            foreach (PersonId id in new[] { Harunobu, Chie, Masatsuna, Nobumasa, Tokihime, Sakon })
            {
                Assert.That(IndustryService.CanWork(succession, id, Start, out string reason), Is.True, reason);
            }
        }

        [Test]
        public void Assign_RejectsUnknownIndustryAndUnknownPerson()
        {
            SuccessionState succession = Succession();
            IndustryState state = IndustryState.CreateInitial(Start);

            IndustryAssignmentResult unknownIndustry = IndustryService.TryAssign(state, succession, Start,
                "industry.unknown", Harunobu);
            Assert.That(unknownIndustry.Succeeded, Is.False);
            Assert.That(unknownIndustry.Outcome, Is.EqualTo(IndustryAssignmentOutcome.UnknownIndustry));

            IndustryAssignmentResult unknownPerson = IndustryService.TryAssign(state, succession, Start,
                IndustryCatalog.FarmId, new PersonId("person.unknown"));
            Assert.That(unknownPerson.Succeeded, Is.False);
            Assert.That(unknownPerson.Outcome, Is.EqualTo(IndustryAssignmentOutcome.UnknownPerson));
            Assert.That(state.TryGetAssignee(IndustryCatalog.FarmId, out PersonId assignee), Is.True);
            Assert.That(assignee.IsEmpty, Is.True);
        }

        [Test]
        public void Assign_RejectsDeadRetiredMinorAndNonMember()
        {
            SuccessionState succession = Succession();
            IndustryState state = IndustryState.CreateInitial(Start);

            PersonId dead = new PersonId("person.sc013.dead");
            succession.World.TryAddPerson(new PersonState(dead, "亡者"),
                new SuccessionPersonLife(1500, false, false), out _);
            succession.World.TryAddMember(dead, FamilyMemberRole.DirectKin, out _);
            Assert.That(IndustryService.TryAssign(state, succession, Start, IndustryCatalog.FarmId, dead).Outcome,
                Is.EqualTo(IndustryAssignmentOutcome.Dead));

            PersonId retired = new PersonId("person.sc013.retired");
            succession.World.TryAddPerson(new PersonState(retired, "隐居者"),
                new SuccessionPersonLife(1500, true, true), out _);
            succession.World.TryAddMember(retired, FamilyMemberRole.DirectKin, out _);
            Assert.That(IndustryService.TryAssign(state, succession, Start, IndustryCatalog.FarmId, retired).Outcome,
                Is.EqualTo(IndustryAssignmentOutcome.Retired));

            PersonId minor = new PersonId("person.sc013.minor");
            succession.World.TryAddPerson(new PersonState(minor, "孩童"),
                new SuccessionPersonLife(Start.Year - 10, true, false), out _);
            succession.World.TryAddMember(minor, FamilyMemberRole.DirectKin, out _);
            Assert.That(IndustryService.TryAssign(state, succession, Start, IndustryCatalog.FarmId, minor).Outcome,
                Is.EqualTo(IndustryAssignmentOutcome.Minor));

            PersonId outsider = new PersonId("person.sc013.outsider");
            succession.World.TryAddPerson(new PersonState(outsider, "外姓"),
                new SuccessionPersonLife(1500, true, false), out _);
            Assert.That(IndustryService.TryAssign(state, succession, Start, IndustryCatalog.FarmId, outsider).Outcome,
                Is.EqualTo(IndustryAssignmentOutcome.NotMember));

            Assert.That(state.TryGetAssignee(IndustryCatalog.FarmId, out PersonId assignee), Is.True);
            Assert.That(assignee.IsEmpty, Is.True);
        }

        [Test]
        public void Assign_RejectsSecondPostForSamePerson()
        {
            SuccessionState succession = Succession();
            IndustryState state = IndustryState.CreateInitial(Start);
            Assert.That(IndustryService.TryAssign(state, succession, Start,
                IndustryCatalog.FarmId, Harunobu).Succeeded, Is.True);

            IndustryAssignmentResult second = IndustryService.TryAssign(state, succession, Start,
                IndustryCatalog.TradingHouseId, Harunobu);

            Assert.That(second.Succeeded, Is.False);
            Assert.That(second.Outcome, Is.EqualTo(IndustryAssignmentOutcome.NoRemainingUnits));
            Assert.That(state.TryGetAssignee(IndustryCatalog.TradingHouseId, out PersonId trading), Is.True);
            Assert.That(trading.IsEmpty, Is.True);
            Assert.That(IndustryService.RemainingWorkUnits(succession, state, Harunobu, Start), Is.EqualTo(1));
        }

        [Test]
        public void Assign_ReplacesOccupantAtomicallyAndFreesOldReservation()
        {
            SuccessionState succession = Succession();
            IndustryState state = IndustryState.CreateInitial(Start);
            IndustryService.TryAssign(state, succession, Start, IndustryCatalog.FarmId, Harunobu);

            IndustryAssignmentResult replaced = IndustryService.TryAssign(state, succession, Start,
                IndustryCatalog.FarmId, Chie);

            Assert.That(replaced.Succeeded, Is.True, replaced.Message);
            Assert.That(replaced.PreviousAssignee, Is.EqualTo(Harunobu));
            Assert.That(replaced.CurrentAssignee, Is.EqualTo(Chie));
            Assert.That(state.TryGetAssignee(IndustryCatalog.FarmId, out PersonId current), Is.True);
            Assert.That(current, Is.EqualTo(Chie));
            Assert.That(IndustryService.RemainingWorkUnits(succession, state, Harunobu, Start), Is.EqualTo(4));
            Assert.That(IndustryService.RemainingWorkUnits(succession, state, Chie, Start), Is.EqualTo(1));
        }

        [Test]
        public void Assign_SamePersonToSamePostIsNoChange()
        {
            SuccessionState succession = Succession();
            IndustryState state = IndustryState.CreateInitial(Start);
            IndustryService.TryAssign(state, succession, Start, IndustryCatalog.FarmId, Harunobu);

            IndustryAssignmentResult again = IndustryService.TryAssign(state, succession, Start,
                IndustryCatalog.FarmId, Harunobu);

            Assert.That(again.Succeeded, Is.True);
            Assert.That(again.Outcome, Is.EqualTo(IndustryAssignmentOutcome.NoChange));
            Assert.That(IndustryService.RemainingWorkUnits(succession, state, Harunobu, Start), Is.EqualTo(1));
        }

        [Test]
        public void Unassign_EmptyIsIdempotentSuccess()
        {
            SuccessionState succession = Succession();
            IndustryState state = IndustryState.CreateInitial(Start);

            IndustryAssignmentResult first = IndustryService.TryUnassign(state, succession, Start,
                IndustryCatalog.FarmId);
            IndustryAssignmentResult second = IndustryService.TryUnassign(state, succession, Start,
                IndustryCatalog.FarmId);

            Assert.That(first.Succeeded, Is.True);
            Assert.That(first.Outcome, Is.EqualTo(IndustryAssignmentOutcome.NoChange));
            Assert.That(second.Succeeded, Is.True);
            Assert.That(second.Outcome, Is.EqualTo(IndustryAssignmentOutcome.NoChange));
            Assert.That(IndustryService.TryUnassign(state, succession, Start, "industry.unknown").Outcome,
                Is.EqualTo(IndustryAssignmentOutcome.UnknownIndustry));
        }

        [Test]
        public void RemainingUnits_AreFourMinusThreeWhenAssigned()
        {
            SuccessionState succession = Succession();
            IndustryState state = IndustryState.CreateInitial(Start);
            Assert.That(IndustryService.RemainingWorkUnits(succession, state, Harunobu, Start), Is.EqualTo(4));

            IndustryService.TryAssign(state, succession, Start, IndustryCatalog.FarmId, Harunobu);
            Assert.That(IndustryService.RemainingWorkUnits(succession, state, Harunobu, Start), Is.EqualTo(1));

            IndustryService.TryUnassign(state, succession, Start, IndustryCatalog.FarmId);
            Assert.That(IndustryService.RemainingWorkUnits(succession, state, Harunobu, Start), Is.EqualTo(4));
        }

        [Test]
        public void Reconcile_VacatesInvalidAssignmentsOnly()
        {
            SuccessionState succession = Succession();
            IndustryState state = IndustryState.CreateInitial(Start);
            IndustryService.TryAssign(state, succession, Start, IndustryCatalog.FarmId, Harunobu);
            IndustryService.TryAssign(state, succession, Start, IndustryCatalog.TradingHouseId, Chie);
            succession.World.TrySetLife(Chie, new SuccessionPersonLife(M2FixedScenario.ChieBirthYear, true, true), out _);

            System.Collections.Generic.IReadOnlyList<IndustryVacancy> vacated =
                IndustryService.ReconcileAssignments(state, succession, Start);

            Assert.That(vacated.Count, Is.EqualTo(1));
            Assert.That(vacated[0].IndustryId, Is.EqualTo(IndustryCatalog.TradingHouseId));
            Assert.That(vacated[0].PreviousAssignee, Is.EqualTo(Chie));
            Assert.That(vacated[0].Reason, Does.Contain("退休"));
            Assert.That(state.TryGetAssignee(IndustryCatalog.TradingHouseId, out PersonId trading), Is.True);
            Assert.That(trading.IsEmpty, Is.True);
            Assert.That(state.TryGetAssignee(IndustryCatalog.FarmId, out PersonId farm), Is.True);
            Assert.That(farm, Is.EqualTo(Harunobu));
        }

        [Test]
        public void Reconcile_ClearsDuplicateAssignmentKeepingFirstByIndustryOrder()
        {
            SuccessionState succession = Succession();
            IndustryState state = IndustryState.CreateInitial(Start);
            // Bypass the service to simulate a corrupt save that puts one person in both posts.
            state.TrySetAssignee(IndustryCatalog.FarmId, Harunobu, out _);
            state.TrySetAssignee(IndustryCatalog.TradingHouseId, Harunobu, out _);

            System.Collections.Generic.IReadOnlyList<IndustryVacancy> vacated =
                IndustryService.ReconcileAssignments(state, succession, Start);

            Assert.That(vacated.Count, Is.EqualTo(1));
            Assert.That(vacated[0].IndustryId, Is.EqualTo(IndustryCatalog.TradingHouseId));
            Assert.That(vacated[0].Reason, Does.Contain("重复"));
            Assert.That(state.TryGetAssignee(IndustryCatalog.FarmId, out PersonId farm), Is.True);
            Assert.That(farm, Is.EqualTo(Harunobu));
            Assert.That(state.TryGetAssignee(IndustryCatalog.TradingHouseId, out PersonId trading), Is.True);
            Assert.That(trading.IsEmpty, Is.True);
        }
    }
}
