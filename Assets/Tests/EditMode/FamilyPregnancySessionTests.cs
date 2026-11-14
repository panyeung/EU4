using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;
using SengokuChronicle.Events;
using SengokuChronicle.Persistence;
using UnityEngine;

namespace SengokuChronicle.Tests.EditMode
{
    /// <summary>M04B Task 4: transactional monthly conception, birth and rollback.</summary>
    public sealed class FamilyPregnancySessionTests
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
        public void ManualPregnancy_DoesNotBirthBeforeMonthNineAndBirthsExactlyOnce()
        {
            GameDate start = _session.WorldClock.CurrentDate;
            SetPregnancy(_session, M2FixedScenario.ChieId, M2FixedScenario.HarunobuId, start);
            int before = _session.Succession.World.PersonCount;

            for (int month = 1; month <= 8; month++)
            {
                AdvanceAndResolve(_session);
                Assert.That(_session.Succession.World.PersonCount, Is.EqualTo(before),
                    "month " + month);
                Assert.That(_session.FamilyPregnancies, Has.Count.EqualTo(1));
            }

            AdvanceAndResolve(_session);

            // M08A may also add one autonomous spouse at this same annual boundary;
            // the birth itself must add exactly one person on top of that.
            Assert.That(_session.Succession.World.PersonCount,
                Is.GreaterThanOrEqualTo(before + 1));
            Assert.That(_session.FamilyPregnancies, Is.Empty);
            PersonState newborn = _session.Succession.World.Persons
                .First(person => person.Id.Value != null && person.Id.Value.StartsWith(
                    "person.birth.", StringComparison.Ordinal));
            Assert.That(newborn.Gender == PersonGender.Male ||
                        newborn.Gender == PersonGender.Female, Is.True);
            Assert.That(IsChildOf(M2FixedScenario.HarunobuId, newborn.Id), Is.True);
            Assert.That(IsChildOf(M2FixedScenario.ChieId, newborn.Id), Is.True);
            Assert.That(HasSibling(M2FixedScenario.MasatsunaId, newborn.Id), Is.True,
                "an existing child of both parents becomes a full sibling");
            Assert.That(HasSibling(M2FixedScenario.NobumasaId, newborn.Id), Is.False,
                "Nobumasa has only Masatsuna recorded as parent, so he is not a full sibling");
            Assert.That(_session.Development.Contains(newborn.Id), Is.True);
            Assert.That(_session.Development.TryGetValues(newborn.Id,
                out CharacterGrowthValues values), Is.True);
            Assert.That(values, Is.EqualTo(CharacterGrowthValues.CreateClamped(0, 0, 0, 0, 0)));
            Assert.That(_session.DailyActions.Any(person => person.PersonId == newborn.Id),
                Is.True, "newborn receives an idle daily-action row");
            Assert.That(_session.TryGetPersonLocation(newborn.Id, out LocationId newbornLocation),
                Is.True);
            Assert.That(newbornLocation, Is.EqualTo(_session.CurrentLocation),
                "the newborn row uses the post-month confirmed location");
            Assert.That(_session.Succession.World.Family.TryGetRole(newborn.Id,
                out FamilyMemberRole role), Is.True);
            Assert.That(role, Is.EqualTo(FamilyMemberRole.DirectKin));
            Assert.That(_session.ChronicleEntries.Count(entry =>
                entry.Category == ChronicleCategory.FamilyFormation &&
                entry.SourceId == FamilyChronicleEntry.BirthSourceId), Is.EqualTo(1));
            Assert.That(_session.ChronicleEntries.First(entry =>
                entry.Category == ChronicleCategory.FamilyFormation &&
                entry.SourceId == FamilyChronicleEntry.BirthSourceId).Title,
                Is.EqualTo("家族新生"));

            int afterBirth = _session.Succession.World.PersonCount;
            AdvanceAndResolve(_session);
            Assert.That(_session.Succession.World.PersonCount, Is.EqualTo(afterBirth),
                "month ten must not create a second birth from the same pregnancy");
        }

        [Test]
        public void DeadGestatingParent_CancelsWithoutBirth()
        {
            SetPregnancy(_session, M2FixedScenario.ChieId, M2FixedScenario.HarunobuId,
                _session.WorldClock.CurrentDate);
            _session.Succession.World.TrySetLife(M2FixedScenario.ChieId,
                new SuccessionPersonLife(M2FixedScenario.ChieBirthYear, false, false), out _);
            int before = _session.Succession.World.PersonCount;

            AdvanceAndResolve(_session);

            Assert.That(_session.FamilyPregnancies, Is.Empty);
            Assert.That(_session.Succession.World.PersonCount, Is.EqualTo(before));
            Assert.That(_session.ChronicleEntries.Count(entry => entry.Title == "家族新生"),
                Is.Zero);
        }

        [Test]
        public void DeadOtherParent_StillPreservesTheConfirmedBirth()
        {
            StartFamilyFormation();
            _session.Succession.World.TryAddKinship(Kinship(SC022FamilyFormationScenario.SpouseId,
                SC022FamilyFormationScenario.CollateralId, KinshipType.Spouse), out string linkError);
            Assert.That(linkError, Is.Empty);
            SetPregnancy(_session, SC022FamilyFormationScenario.SpouseId,
                SC022FamilyFormationScenario.CollateralId, _session.WorldClock.CurrentDate);
            _session.Succession.World.TrySetLife(SC022FamilyFormationScenario.CollateralId,
                new SuccessionPersonLife(SC022FamilyFormationScenario.CollateralBirthYear,
                    false, false), out _);
            int before = _session.Succession.World.PersonCount;

            AdvanceMonthsAndResolve(_session, 9);

            Assert.That(_session.Succession.World.PersonCount, Is.EqualTo(before + 1));
            PersonState newborn = _session.Succession.World.Persons
                .First(person => person.Id.Value.StartsWith("person.birth.",
                    StringComparison.Ordinal));
            Assert.That(IsChildOf(SC022FamilyFormationScenario.SpouseId, newborn.Id), Is.True);
            Assert.That(IsChildOf(SC022FamilyFormationScenario.CollateralId, newborn.Id),
                Is.True, "the deceased other parent keeps the confirmed parentage");
        }

        [Test]
        public void BirthMonth_DoesNotReconceiveTheSamePair()
        {
            StartFamilyFormation();
            AdvanceMonthsAndResolve(_session, 9);
            SetPregnancy(_session, SC022FamilyFormationScenario.SpouseId,
                SC022FamilyFormationScenario.HeadId,
                FamilyPregnancyRecord.AddMonths(_session.WorldClock.CurrentDate, -8));

            AdvanceAndResolve(_session);

            Assert.That(_session.FamilyPregnancies, Is.Empty,
                "the completed pair must not conceive again in the same settlement");
            Assert.That(_session.ChronicleEntries.Count(entry => entry.Title == "家族新生"),
                Is.EqualTo(1));
        }

        [Test]
        public void InjectedSettlementFailure_RestoresEveryFamilyFactAndRetryMatchesControl()
        {
            StartFamilyFormation();
            AdvanceMonthsAndResolve(_session, 9);
            SetPregnancy(_session, SC022FamilyFormationScenario.SpouseId,
                SC022FamilyFormationScenario.HeadId,
                FamilyPregnancyRecord.AddMonths(_session.WorldClock.CurrentDate, -8));
            GameSession control = GameSession.NewGame();
            try
            {
                control.InitFamilyFormationDemoScenario(GameSession.DefaultRandomSeed);
                AdvanceMonthsAndResolve(control, 9);
                SetPregnancy(control, SC022FamilyFormationScenario.SpouseId,
                    SC022FamilyFormationScenario.HeadId,
                    FamilyPregnancyRecord.AddMonths(control.WorldClock.CurrentDate, -8));
                AdvanceAndResolve(control);

                GameDate date = _session.WorldClock.CurrentDate;
                int persons = _session.Succession.World.PersonCount;
                int chronicles = _session.ChronicleEntries.Count;
                int developmentRows = _session.Development.PersonCount;
                int dailyRows = _session.DailyActions.Count;
                string rng = JsonUtility.ToJson(_session.RandomState.CaptureState());
                FamilyPregnancyRecord pregnancy = _session.FamilyPregnancies[0];

                _session.MonthSettlementFaultInjection = () =>
                    throw new InvalidOperationException("injected birth rollback");

                Assert.That(_session.TryAdvanceToNextMonth(), Is.False);

                Assert.That(_session.WorldClock.CurrentDate, Is.EqualTo(date));
                Assert.That(_session.Succession.World.PersonCount, Is.EqualTo(persons));
                Assert.That(_session.ChronicleEntries.Count, Is.EqualTo(chronicles));
                Assert.That(_session.Development.PersonCount, Is.EqualTo(developmentRows));
                Assert.That(_session.DailyActions.Count, Is.EqualTo(dailyRows));
                Assert.That(JsonUtility.ToJson(_session.RandomState.CaptureState()),
                    Is.EqualTo(rng));
                Assert.That(_session.FamilyPregnancies, Has.Count.EqualTo(1));
                Assert.That(_session.FamilyPregnancies[0], Is.EqualTo(pregnancy));
                Assert.That(_session.Succession.World.Persons.Any(person =>
                    person.Id.Value.StartsWith("person.birth.", StringComparison.Ordinal)),
                    Is.False);

                _session.MonthSettlementFaultInjection = null;
                AdvanceAndResolve(_session);

                SaveData retried = _session.CaptureSave();
                SaveData expected = control.CaptureSave();
                Assert.That(_session.WorldClock.CurrentDate,
                    Is.EqualTo(control.WorldClock.CurrentDate));
                Assert.That(JsonUtility.ToJson(retried.succession),
                    Is.EqualTo(JsonUtility.ToJson(expected.succession)));
                Assert.That(JsonUtility.ToJson(retried.development),
                    Is.EqualTo(JsonUtility.ToJson(expected.development)));
                Assert.That(JsonUtility.ToJson(retried.dailyActions),
                    Is.EqualTo(JsonUtility.ToJson(expected.dailyActions)));
                Assert.That(JsonUtility.ToJson(retried.chronicle),
                    Is.EqualTo(JsonUtility.ToJson(expected.chronicle)));
                Assert.That(JsonUtility.ToJson(retried.randomState),
                    Is.EqualTo(JsonUtility.ToJson(expected.randomState)));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(control.gameObject);
            }
        }

        private void StartFamilyFormation()
        {
            UnityEngine.Object.DestroyImmediate(_session.gameObject);
            _session = GameSession.NewGame();
            _session.InitFamilyFormationDemoScenario(GameSession.DefaultRandomSeed);
        }

        private static void SetPregnancy(GameSession session, PersonId mother, PersonId father,
            GameDate conception)
        {
            Assert.That(FamilyPregnancyRecord.TryCreate(mother, father, conception,
                FamilyPregnancyRecord.AddMonths(conception,
                    FamilyPregnancyRecord.GestationMonths),
                out FamilyPregnancyRecord record, out string error), Is.True, error);
            Assert.That(session.Succession.World.Pregnancies.TryAdd(record, out error),
                Is.True, error);
        }

        private static void AdvanceMonthsAndResolve(GameSession session, int months)
        {
            for (int i = 0; i < months; i++)
                AdvanceAndResolve(session);
        }

        private static void AdvanceAndResolve(GameSession session)
        {
            Assert.That(session.TryAdvanceToNextMonth(), Is.True, session.LastEventMessage);
            ResolvePending(session);
        }

        private bool IsChildOf(PersonId parentId, PersonId childId)
        {
            return _session.Succession.World.Kinship.Contains(
                new KinshipLink(parentId, childId, KinshipType.ParentChild));
        }

        private bool HasSibling(PersonId first, PersonId second)
        {
            return _session.Succession.World.Kinship.Contains(
                       new KinshipLink(first, second, KinshipType.Sibling)) ||
                   _session.Succession.World.Kinship.Contains(
                       new KinshipLink(second, first, KinshipType.Sibling));
        }

        private static KinshipLink Kinship(PersonId from, PersonId to, KinshipType type)
        {
            Assert.That(KinshipLink.TryCreate(from, to, type, out KinshipLink link,
                out string error), Is.True, error);
            return link;
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
    }
}
