using System;
using System.IO;
using NUnit.Framework;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;
using SengokuChronicle.Events;
using SengokuChronicle.Persistence;
using UnityEngine;

namespace SengokuChronicle.Tests.EditMode
{
    /// <summary>
    /// Focused coverage for the isolated Debug Mode session: lifecycle and save isolation,
    /// atomic person/kinship/headship transactions, the complex preset, forward date
    /// injection, resource injection and event injection. Scene and Play Mode checks stay
    /// with the lead.
    /// </summary>
    public class DebugSessionTests
    {
        private GameSession _session;
        private string _directory;
        private SaveSlotStore _store;

        [SetUp]
        public void SetUp()
        {
            _session = GameSession.NewGame();
            _directory = Path.Combine(Path.GetTempPath(), "SengokuDebugTests",
                Guid.NewGuid().ToString("N"));
            _store = new SaveSlotStore(_directory);
        }

        [TearDown]
        public void TearDown()
        {
            if (_session != null)
                UnityEngine.Object.DestroyImmediate(_session.gameObject);
            if (Directory.Exists(_directory))
                Directory.Delete(_directory, true);
        }

        private DebugSession Begin() => _session.BeginDebugSession();

        private DebugPersonDraft Draft(string name, PersonGender gender, int birthYear,
            bool member = true, FamilyMemberRole role = FamilyMemberRole.DirectKin)
        {
            return new DebugPersonDraft
            {
                DisplayName = name,
                Gender = gender,
                BirthYear = birthYear,
                Alive = true,
                JoinFamily = member,
                Role = role
            };
        }

        private PersonId Create(DebugSession debug, string name, PersonGender gender,
            int birthYear, bool member = true,
            FamilyMemberRole role = FamilyMemberRole.DirectKin)
        {
            DebugCommandResult result = debug.CreatePerson(
                Draft(name, gender, birthYear, member, role), out PersonId created);
            Assert.That(result.Succeeded, Is.True, result.Message);
            return created;
        }

        // ---------------------------------------------------------------- lifecycle

        [Test]
        public void BeginDebugSession_UsesTheDemoRosterWithoutReadingTheSlot()
        {
            int captures = 0;
            _session.CaptureSaveObserved = () => captures++;

            DebugSession debug = Begin();

            Assert.That(_session.IsDebugSessionActive, Is.True);
            Assert.That(debug.IsActive, Is.True);
            Assert.That(_session.Succession.World.Family.Id,
                Is.EqualTo(SuccessionSample.StandardFamilyId));
            Assert.That(_session.WorldClock.CurrentDate, Is.EqualTo(WorldClock.NewGameDate));
            Assert.That(captures, Is.EqualTo(0), "entering Debug Mode must not build save data.");
            Assert.That(File.Exists(_store.FilePath), Is.False,
                "entering Debug Mode must not touch the player slot.");
        }

        [Test]
        public void EndedSession_RejectsEveryCommandFromTheStaleHandle()
        {
            DebugSession debug = Begin();
            _session.EndDebugSession();

            Assert.That(debug.IsActive, Is.False);
            Assert.That(_session.IsDebugSessionActive, Is.False);
            DebugCommandResult result = debug.CreatePerson(
                Draft("已失效", PersonGender.Male, 1540), out _);
            Assert.That(result.Outcome, Is.EqualTo(DebugCommandOutcome.NotActive));
            Assert.That(debug.AdvanceOneMonth().Outcome, Is.EqualTo(DebugCommandOutcome.NotActive));
            Assert.That(debug.ApplyComplexFamilyPreset().Outcome,
                Is.EqualTo(DebugCommandOutcome.NotActive));
        }

        [Test]
        public void EndingSession_DiscardsInjectedStateAndRestoresTheNormalStartPath()
        {
            DebugSession debug = Begin();
            Create(debug, "临时人物", PersonGender.Male, 1540);
            Assert.That(debug.Roster.Count, Is.GreaterThan(7));

            _session.EndDebugSession();

            Assert.That(_session.IsDebugSessionActive, Is.False);
            Assert.That(_session.Succession.World.Family.Id,
                Is.EqualTo(new FamilyId("family.akechi.sc013")),
                "ending Debug Mode returns to the ordinary SC-013 start path.");
            Assert.That(_session.Succession.World.PersonCount, Is.EqualTo(6));
            Assert.That(_session.SaveTo(_store).Succeeded, Is.True,
                "a normal session after Debug Mode can save again.");
        }

        [Test]
        public void NormalStartDuringDebugSession_ClearsTheDebugMarker()
        {
            Begin();
            _session.InitNewGame();

            Assert.That(_session.IsDebugSessionActive, Is.False);
            Assert.That(_session.SaveTo(_store).Succeeded, Is.True);
        }


        // ---------------------------------------------------------------- broadcast coherence

        [Test]
        public void DebugEntryAndReset_PublishOnlyActiveStatesAndExitPublishesOnce()
        {
            var observed = new System.Collections.Generic.List<bool>();
            _session.SessionStateChanged += () => observed.Add(_session.IsDebugSessionActive);

            _session.BeginDebugSession();

            Assert.That(observed, Is.Not.Empty);
            Assert.That(observed, Has.All.True,
                "entering Debug Mode must not broadcast a transient inactive state.");

            observed.Clear();
            _session.ResetDebugSession();

            Assert.That(observed.Count, Is.EqualTo(1),
                "a reset publishes exactly one coherent state.");
            Assert.That(observed[0], Is.True,
                "the single reset broadcast reports the fresh debug session as active.");
            Assert.That(_session.IsDebugSessionActive, Is.True);

            observed.Clear();
            _session.EndDebugSession();

            Assert.That(observed.Count, Is.EqualTo(1));
            Assert.That(observed[0], Is.False);
            Assert.That(_session.IsDebugSessionActive, Is.False);
        }

        // ---------------------------------------------------------------- save isolation

        [Test]
        public void DebugSession_RejectsSaveLoadAndApplyBeforeAnySlotIo()
        {
            _session.InitStandardDemo();
            Assert.That(_store.Save(_session.CaptureSave()).Succeeded, Is.True);
            string before = File.ReadAllText(_store.FilePath);
            string backupBefore = File.Exists(_store.BackupFilePath)
                ? File.ReadAllText(_store.BackupFilePath)
                : string.Empty;
            SaveData snapshot = _session.CaptureSave();
            int day = _session.WorldClock.CurrentDate.Day;

            DebugSession debug = Begin();

            SaveOperationResult save = _session.SaveTo(_store);
            SaveOperationResult load = _session.LoadFrom(_store);
            SaveOperationResult apply = _session.TryApplySave(snapshot);

            Assert.That(save.Succeeded, Is.False);
            Assert.That(save.Error, Is.EqualTo(SaveErrorCode.WriteFailed));
            Assert.That(load.Succeeded, Is.False);
            Assert.That(load.Error, Is.EqualTo(SaveErrorCode.ReadFailed));
            Assert.That(apply.Succeeded, Is.False);
            Assert.That(File.ReadAllText(_store.FilePath), Is.EqualTo(before));
            Assert.That(File.Exists(_store.BackupFilePath)
                ? File.ReadAllText(_store.BackupFilePath)
                : string.Empty, Is.EqualTo(backupBefore));
            Assert.That(_session.WorldClock.CurrentDate.Day, Is.EqualTo(day),
                "a rejected load must not apply save data.");
            Assert.That(debug.IsActive, Is.True);
        }

        // ---------------------------------------------------------------- person commands

        [Test]
        public void CreatePerson_GeneratesSystemIdsAndKeepsThemUntilReset()
        {
            DebugSession debug = Begin();
            PersonId first = Create(debug, "测试甲", PersonGender.Male, 1540);
            PersonId second = Create(debug, "测试乙", PersonGender.Female, 1542,
                role: FamilyMemberRole.CollateralKin);

            Assert.That(first.Value, Does.StartWith("debug.person."));
            Assert.That(second, Is.Not.EqualTo(first));
            Assert.That(_session.Succession.World.TryGetPerson(first, out PersonState person),
                Is.True);
            Assert.That(person.Gender, Is.EqualTo(PersonGender.Male));
            Assert.That(_session.Succession.World.Family.TryGetRole(second,
                out FamilyMemberRole role), Is.True);
            Assert.That(role, Is.EqualTo(FamilyMemberRole.CollateralKin));
        }

        [Test]
        public void CreatePerson_RejectsBlankNameUndefinedGenderAndFutureBirthAtomically()
        {
            DebugSession debug = Begin();
            int persons = _session.Succession.World.PersonCount;
            string before = debug.CaptureFingerprint();

            DebugCommandResult blank = debug.CreatePerson(Draft("   ", PersonGender.Male, 1540),
                out _);
            DebugCommandResult gender = debug.CreatePerson(
                Draft("甲", (PersonGender)99, 1540), out _);
            DebugCommandResult future = debug.CreatePerson(
                Draft("乙", PersonGender.Male, debug.Today.Year + 1), out _);
            DebugCommandResult head = debug.CreatePerson(
                Draft("丙", PersonGender.Male, 1540, true, FamilyMemberRole.Head), out _);

            Assert.That(blank.Succeeded, Is.False);
            Assert.That(blank.Code, Is.EqualTo("person.invalid_name"));
            Assert.That(gender.Succeeded, Is.False);
            Assert.That(future.Succeeded, Is.False);
            Assert.That(future.Code, Is.EqualTo("person.future_birth"));
            Assert.That(head.Succeeded, Is.False);
            Assert.That(_session.Succession.World.PersonCount, Is.EqualTo(persons));
            Assert.That(debug.CaptureFingerprint(), Is.EqualTo(before));
        }

        [Test]
        public void EditPerson_ChangesEveryFactWithoutTouchingThePermanentId()
        {
            DebugSession debug = Begin();
            PersonId id = Create(debug, "旧名", PersonGender.Male, 1540);
            int chronicles = _session.ChronicleEntries.Count;

            DebugCommandResult result = debug.EditPerson(new DebugPersonEdit
            {
                PersonId = id,
                DisplayName = "新名",
                Gender = PersonGender.Female,
                BirthYear = 1538,
                Alive = false,
                IsFamilyMember = true,
                Role = FamilyMemberRole.AdoptedChild
            });

            Assert.That(result.Succeeded, Is.True, result.Message);
            Assert.That(result.IsDebugInjection, Is.True);
            Assert.That(result.Message, Does.Contain("调试注入"));
            Assert.That(_session.Succession.World.TryGetPerson(id, out PersonState person), Is.True);
            Assert.That(person.DisplayName, Is.EqualTo("新名"));
            Assert.That(person.Gender, Is.EqualTo(PersonGender.Female));
            Assert.That(_session.Succession.World.TryGetLife(id,
                out SuccessionPersonLife life), Is.True);
            Assert.That(life.BirthYear, Is.EqualTo(1538));
            Assert.That(life.Alive, Is.False);
            Assert.That(_session.Succession.World.Family.TryGetRole(id,
                out FamilyMemberRole role), Is.True);
            Assert.That(role, Is.EqualTo(FamilyMemberRole.AdoptedChild));
            Assert.That(_session.ChronicleEntries.Count, Is.EqualTo(chronicles),
                "a direct alive-state edit must not fabricate a death record.");
        }

        [Test]
        public void EditPerson_RejectsInvalidFactsAndBlankNamesWithoutPartialWrite()
        {
            DebugSession debug = Begin();
            PersonId id = Create(debug, "保留名", PersonGender.Male, 1540);
            string before = debug.CaptureFingerprint();

            DebugCommandResult blank = debug.EditPerson(new DebugPersonEdit
            {
                PersonId = id,
                DisplayName = " ",
                Gender = PersonGender.Female,
                BirthYear = 1530,
                Alive = false,
                IsFamilyMember = true,
                Role = FamilyMemberRole.Spouse
            });
            DebugCommandResult future = debug.EditPerson(new DebugPersonEdit
            {
                PersonId = id,
                DisplayName = "未来",
                Gender = PersonGender.Female,
                BirthYear = debug.Today.Year + 5,
                Alive = true,
                IsFamilyMember = true,
                Role = FamilyMemberRole.Spouse
            });

            Assert.That(blank.Succeeded, Is.False);
            Assert.That(future.Succeeded, Is.False);
            Assert.That(debug.CaptureFingerprint(), Is.EqualTo(before));
        }

        [Test]
        public void EditPerson_RejectsGenderChangeForAnActivePregnancy()
        {
            DebugSession debug = Begin();
            PersonId mother = Create(debug, "孕期人物", PersonGender.Female, 1540);
            PersonId father = Create(debug, "配偶人物", PersonGender.Male, 1538,
                role: FamilyMemberRole.Spouse);
            GameDate today = debug.Today;
            Assert.That(FamilyPregnancyRecord.TryCreate(mother, father, today,
                FamilyPregnancyRecord.AddMonths(today, FamilyPregnancyRecord.GestationMonths),
                out FamilyPregnancyRecord record, out string pregnancyError), Is.True,
                pregnancyError);
            Assert.That(_session.Succession.World.Pregnancies.TryAdd(record, out string addError),
                Is.True, addError);
            string before = debug.CaptureFingerprint();

            DebugCommandResult result = debug.EditPerson(new DebugPersonEdit
            {
                PersonId = mother,
                DisplayName = "孕期人物",
                Gender = PersonGender.Male,
                BirthYear = 1540,
                Alive = true,
                IsFamilyMember = true,
                Role = FamilyMemberRole.DirectKin
            });

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Code, Is.EqualTo("person.pregnant_gender"));
            Assert.That(debug.CaptureFingerprint(), Is.EqualTo(before));
        }

        [Test]
        public void TransferHeadship_KeepsExactlyOneHeadAndTheSuccessionHeadIdAligned()
        {
            DebugSession debug = Begin();
            PersonId target = Create(debug, "新家主", PersonGender.Male, 1530,
                role: FamilyMemberRole.CollateralKin);
            PersonId oldHead = _session.Succession.HeadPersonId;
            int successionCount = _session.Succession.SuccessionCount;
            int chronicles = _session.ChronicleEntries.Count;

            DebugCommandResult result = debug.TransferHeadship(target);

            Assert.That(result.Succeeded, Is.True, result.Message);
            Assert.That(result.IsDebugInjection, Is.True);
            Assert.That(_session.Succession.HeadPersonId, Is.EqualTo(target));
            int heads = 0;
            foreach (FamilyMembership membership in _session.Succession.World.Family.Members)
                if (membership.Role == FamilyMemberRole.Head)
                    heads++;
            Assert.That(heads, Is.EqualTo(1));
            Assert.That(_session.Succession.World.Family.TryGetRole(oldHead,
                out FamilyMemberRole oldRole), Is.True);
            Assert.That(oldRole, Is.Not.EqualTo(FamilyMemberRole.Head));
            Assert.That(_session.Succession.SuccessionCount, Is.EqualTo(successionCount));
            Assert.That(_session.ChronicleEntries.Count, Is.EqualTo(chronicles),
                "debug headship must not add a chronicle succession record.");
        }

        [Test]
        public void TransferHeadship_IsRejectedWhileASuccessionDecisionIsPendingAndDeadTargets()
        {
            DebugSession debug = Begin();
            PersonId target = Create(debug, "候选家主", PersonGender.Male, 1530,
                role: FamilyMemberRole.CollateralKin);
            string before = debug.CaptureFingerprint();

            _session.Succession.Phase = SuccessionPhase.AwaitingSuccessionChoice;
            DebugCommandResult pending = debug.TransferHeadship(target);
            _session.Succession.Phase = SuccessionPhase.None;

            DebugCommandResult outsider = debug.TransferHeadship(
                new PersonId("person.unknown.head"));

            Assert.That(pending.Succeeded, Is.False);
            Assert.That(pending.Code, Is.EqualTo("head.pending"));
            Assert.That(outsider.Succeeded, Is.False);
            Assert.That(_session.Succession.HeadPersonId, Is.Not.EqualTo(target));
            _ = before;
        }

        [Test]
        public void RemovePerson_ReportsEveryRemainingReferenceUntilTheyAreDissolved()
        {
            DebugSession debug = Begin();
            PersonId id = Create(debug, "待移除", PersonGender.Male, 1540);
            string before = debug.CaptureFingerprint();

            DebugCommandResult blocked = debug.RemovePerson(id);

            Assert.That(blocked.Succeeded, Is.False);
            Assert.That(blocked.Message, Does.Contain("家族名册"));
            Assert.That(debug.CaptureFingerprint(), Is.EqualTo(before));

            Assert.That(debug.EditPerson(new DebugPersonEdit
            {
                PersonId = id,
                DisplayName = "待移除",
                Gender = PersonGender.Male,
                BirthYear = 1540,
                Alive = true,
                IsFamilyMember = false,
                Role = FamilyMemberRole.DirectKin
            }).Succeeded, Is.True);

            DebugCommandResult removed = debug.RemovePerson(id);

            Assert.That(removed.Succeeded, Is.True, removed.Message);
            Assert.That(_session.Succession.World.TryGetPerson(id, out _), Is.False);
            Assert.That(_session.Succession.World.TryGetLife(id, out _), Is.False);
        }

        [Test]
        public void RemovePerson_RejectsTheCurrentHead()
        {
            DebugSession debug = Begin();
            DebugCommandResult result = debug.RemovePerson(_session.Succession.HeadPersonId);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Message, Does.Contain("家主"));
        }

        // ---------------------------------------------------------------- kinship commands

        [Test]
        public void KinshipPreview_RejectsSelfDuplicateDirectLineAndCycles()
        {
            DebugSession debug = Begin();
            PersonId parent = Create(debug, "父", PersonGender.Male, 1510);
            PersonId child = Create(debug, "子", PersonGender.Male, 1540);
            PersonId grandChild = Create(debug, "孙", PersonGender.Male, 1555);

            Assert.That(debug.AddKinship(parent, child, KinshipType.ParentChild).Succeeded, Is.True);
            Assert.That(debug.AddKinship(child, grandChild, KinshipType.ParentChild).Succeeded,
                Is.True);

            DebugKinshipPreview self =
                debug.PreviewKinship(parent, parent, KinshipType.Spouse, true);
            DebugKinshipPreview duplicate =
                debug.PreviewKinship(parent, child, KinshipType.ParentChild, true);
            DebugKinshipPreview spouseLine =
                debug.PreviewKinship(parent, child, KinshipType.Spouse, true);
            DebugKinshipPreview cycle =
                debug.PreviewKinship(grandChild, parent, KinshipType.ParentChild, true);
            DebugKinshipPreview siblingLine =
                debug.PreviewKinship(parent, child, KinshipType.Sibling, true);

            Assert.That(self.CanApply, Is.False);
            Assert.That(duplicate.CanApply, Is.False);
            Assert.That(duplicate.Reason, Does.Contain("已经存在"));
            Assert.That(spouseLine.CanApply, Is.False);
            Assert.That(spouseLine.Reason, Does.Contain("直系"));
            Assert.That(cycle.CanApply, Is.False);
            Assert.That(cycle.Reason, Does.Contain("循环"));
            Assert.That(siblingLine.CanApply, Is.False);
            Assert.That(siblingLine.Path, Does.Contain("亲子"));
        }

        [Test]
        public void KinshipCommands_ApplySpouseSiblingAndAdoptionThenRemoveThem()
        {
            DebugSession debug = Begin();
            PersonId left = Create(debug, "甲", PersonGender.Male, 1540);
            PersonId right = Create(debug, "乙", PersonGender.Female, 1542);

            DebugKinshipPreview preview =
                debug.PreviewKinship(left, right, KinshipType.Spouse, true);
            Assert.That(preview.CanApply, Is.True, preview.Reason);
            DebugCommandResult added = debug.AddKinship(left, right, KinshipType.Spouse);
            Assert.That(added.Succeeded, Is.True, added.Message);
            Assert.That(added.IsDebugInjection, Is.True);

            Assert.That(debug.AddKinship(left, right, KinshipType.Sibling).Succeeded, Is.True);
            Assert.That(_session.Succession.World.Kinship.LinksFor(left).Count, Is.EqualTo(2));

            DebugKinshipPreview removal =
                debug.PreviewKinship(left, right, KinshipType.Spouse, false);
            Assert.That(removal.CanApply, Is.True, removal.Reason);

            DebugCommandResult removed = debug.RemoveKinship(left, right, KinshipType.Spouse);
            Assert.That(removed.Succeeded, Is.True, removed.Message);
            Assert.That(_session.Succession.World.Kinship.LinksFor(left).Count, Is.EqualTo(1));
        }

        [Test]
        public void KinshipCommit_FaultInjectionRestoresTheWholeWorld()
        {
            DebugSession debug = Begin();
            PersonId left = Create(debug, "甲", PersonGender.Male, 1540);
            PersonId right = Create(debug, "乙", PersonGender.Female, 1542);
            string before = debug.CaptureFingerprint();
            debug.CommitFaultInjection = () => throw new InvalidOperationException("boom");

            DebugCommandResult result = debug.AddKinship(left, right, KinshipType.Spouse);

            Assert.That(result.Outcome, Is.EqualTo(DebugCommandOutcome.Faulted));
            Assert.That(debug.CaptureFingerprint(), Is.EqualTo(before));
        }

        // ---------------------------------------------------------------- complex preset

        [Test]
        public void ComplexPreset_AddsSixteenPeopleAcrossSixGenerationsWithEveryKinshipKind()
        {
            DebugSession debug = Begin();
            int personsBefore = _session.Succession.World.PersonCount;
            int linksBefore = _session.Succession.World.Kinship.Count;

            DebugCommandResult result = debug.ApplyComplexFamilyPreset();

            Assert.That(result.Succeeded, Is.True, result.Message);
            Assert.That(result.IsDebugInjection, Is.True);
            Assert.That(_session.Succession.World.PersonCount,
                Is.EqualTo(personsBefore + 16));

            var birthYears = new System.Collections.Generic.HashSet<int>();
            int disconnected = 0;
            foreach (PersonState person in _session.Succession.World.Persons)
            {
                if (!person.Id.Value.StartsWith("debug.person.", StringComparison.Ordinal))
                    continue;
                _session.Succession.World.TryGetLife(person.Id, out SuccessionPersonLife life);
                birthYears.Add(life.BirthYear);
                if (_session.Succession.World.Kinship.LinksFor(person.Id).Count == 0)
                    disconnected++;
            }

            Assert.That(birthYears.Count, Is.GreaterThanOrEqualTo(6),
                "the preset must span six generations.");
            Assert.That(disconnected, Is.GreaterThanOrEqualTo(1),
                "the preset must include a disconnected member.");
            Assert.That(_session.Succession.World.Kinship.Count,
                Is.EqualTo(linksBefore + 21));

            bool spouse = false;
            bool parentChild = false;
            bool sibling = false;
            bool adoption = false;
            foreach (KinshipLink link in _session.Succession.World.Kinship.Links)
            {
                spouse |= link.Type == KinshipType.Spouse;
                parentChild |= link.Type == KinshipType.ParentChild;
                sibling |= link.Type == KinshipType.Sibling;
                adoption |= link.Type == KinshipType.Adoption;
            }

            Assert.That(spouse && parentChild && sibling && adoption, Is.True,
                "the preset must include spouses, parent/child, siblings and adoptions.");
            Assert.That(_session.Succession.World.Family.MemberCount,
                Is.GreaterThan(personsBefore));
        }

        [Test]
        public void ComplexPreset_FaultInjectionRollsBackTheWholePreset()
        {
            DebugSession debug = Begin();
            string before = debug.CaptureFingerprint();
            int personsBefore = _session.Succession.World.PersonCount;
            int linksBefore = _session.Succession.World.Kinship.Count;
            int faults = 0;
            debug.CommitFaultInjection = () =>
            {
                faults++;
                if (faults == 2)
                    throw new InvalidOperationException("preset boom");
            };

            DebugCommandResult result = debug.ApplyComplexFamilyPreset();

            Assert.That(result.Outcome, Is.EqualTo(DebugCommandOutcome.Faulted));
            Assert.That(_session.Succession.World.PersonCount, Is.EqualTo(personsBefore));
            Assert.That(_session.Succession.World.Kinship.Count, Is.EqualTo(linksBefore));
            Assert.That(debug.CaptureFingerprint(), Is.EqualTo(before));
        }

        // ---------------------------------------------------------------- date commands

        [Test]
        public void DateJump_RejectsInvalidBackwardAndUnchangedTargets()
        {
            DebugSession debug = Begin();
            GameDate today = debug.Today;

            Assert.That(debug.PreviewDateJump(1559, 12, 31).CanJump, Is.False);
            Assert.That(debug.PreviewDateJump(2001, 1, 1).CanJump, Is.False);
            Assert.That(debug.PreviewDateJump(today.Year - 1, today.Month, today.Day).CanJump,
                Is.False);
            DebugDatePreview unchanged =
                debug.PreviewDateJump(today.Year, today.Month, today.Day);
            Assert.That(unchanged.CanJump, Is.False);
            Assert.That(unchanged.Reason, Does.Contain("倒退"));

            Assert.That(debug.JumpToDate(1559, 12, 31).Succeeded, Is.False);
            Assert.That(debug.Today, Is.EqualTo(today));
        }

        [Test]
        public void DateJump_IsRejectedWhileAnyInFlightProcessExists()
        {
            DebugSession debug = Begin();
            GameDate today = debug.Today;

            _session.Succession.Phase = SuccessionPhase.AwaitingRegentChoice;
            Assert.That(debug.PreviewDateJump(today.Year + 1, today.Month, today.Day).CanJump,
                Is.False);
            _session.Succession.Phase = SuccessionPhase.None;

            _session.Succession.Phase = SuccessionPhase.Regency;
            Assert.That(debug.PreviewDateJump(today.Year + 1, today.Month, today.Day).CanJump,
                Is.False);
            _session.Succession.Phase = SuccessionPhase.None;

            PersonId mother = Create(debug, "孕期人物", PersonGender.Female, 1540);
            PersonId father = Create(debug, "孕期配偶", PersonGender.Male, 1538,
                role: FamilyMemberRole.Spouse);
            Assert.That(FamilyPregnancyRecord.TryCreate(mother, father, today,
                FamilyPregnancyRecord.AddMonths(today, FamilyPregnancyRecord.GestationMonths),
                out FamilyPregnancyRecord record, out _), Is.True);
            Assert.That(_session.Succession.World.Pregnancies.TryAdd(record, out _), Is.True);

            DebugDatePreview pregnant =
                debug.PreviewDateJump(today.Year + 1, today.Month, today.Day);
            Assert.That(pregnant.CanJump, Is.False);
            Assert.That(pregnant.Reason, Does.Contain("孕期"));
            Assert.That(_session.WorldClock.CurrentDate, Is.EqualTo(today));
        }

        [Test]
        public void DateJump_RealignsLedgersSoTheNextNormalMonthSettlesExactlyOnce()
        {
            DebugSession debug = Begin();
            var target = new GameDate(1561, 4, 1);
            int monthIndex = IndustryCalendar.MonthIndex(target) - 1;

            DebugDatePreview preview = debug.PreviewDateJump(target.Year, target.Month, target.Day);
            Assert.That(preview.CanJump, Is.True, preview.Reason);
            Assert.That(preview.SkippedDays, Is.GreaterThan(300));
            Assert.That(preview.Summary, Does.Contain("调试日期注入"));

            DebugCommandResult jump = debug.JumpToDate(target.Year, target.Month, target.Day);

            Assert.That(jump.Succeeded, Is.True, jump.Message);
            Assert.That(jump.IsDebugInjection, Is.True);
            Assert.That(_session.WorldClock.CurrentDate, Is.EqualTo(target));
            Assert.That(_session.DebugIndustryState.LastSettledMonthIndex, Is.EqualTo(monthIndex));
            Assert.That(_session.DebugMarketState.LastSettledMonthIndex, Is.EqualTo(monthIndex));
            Assert.That(_session.ResearchState.LastSettledMonthIndex, Is.EqualTo(monthIndex));
            Assert.That(_session.LastIndustryReport, Is.Null,
                "a skipped month must not leave a stale month report.");

            int settledBefore = _session.DebugIndustryState.LastSettledMonthIndex;
            DebugCommandResult month = debug.AdvanceOneMonth();

            Assert.That(month.Succeeded, Is.True, month.Message);
            Assert.That(month.IsDebugInjection, Is.False,
                "one real month turn must be reported as the normal rule path.");
            Assert.That(_session.DebugIndustryState.LastSettledMonthIndex,
                Is.EqualTo(settledBefore + 1),
                "the first normal turn after a jump settles exactly one month.");
            Assert.That(_session.WorldClock.CurrentDate, Is.EqualTo(new GameDate(1561, 5, 1)));
        }

        [Test]
        public void DateJump_FaultInjectionRestoresTheClockAndEveryLedger()
        {
            DebugSession debug = Begin();
            string before = debug.CaptureFingerprint();
            debug.CommitFaultInjection = () =>
            {
                if (debug.Today != WorldClock.NewGameDate)
                    throw new InvalidOperationException("date boom");
            };

            DebugCommandResult result = debug.JumpToDate(1562, 6, 15);

            Assert.That(result.Outcome, Is.EqualTo(DebugCommandOutcome.Faulted));
            Assert.That(_session.WorldClock.CurrentDate, Is.EqualTo(WorldClock.NewGameDate));
            Assert.That(debug.CaptureFingerprint(), Is.EqualTo(before));
        }

        // ---------------------------------------------------------------- resource commands

        [Test]
        public void Resources_AreValidatedAndAppliedAtomically()
        {
            DebugSession debug = Begin();
            string before = debug.CaptureFingerprint();

            DebugCommandResult invalid = debug.SetResourcesAndMarket(new DebugResourceRequest
            {
                Funds = 999999,
                Supplies = 5,
                Intelligence = 1,
                FoodInventory = 10,
                FoodPriceIndexPermille = 99,
                MaterialsPriceIndexPermille = 1000
            });

            Assert.That(invalid.Succeeded, Is.False);
            Assert.That(invalid.Code, Is.EqualTo("market.invalid"));
            Assert.That(debug.CaptureFingerprint(), Is.EqualTo(before));

            DebugCommandResult applied = debug.SetResourcesAndMarket(new DebugResourceRequest
            {
                Funds = 500,
                Supplies = 60,
                Intelligence = 7,
                FoodInventory = 42,
                FoodPriceIndexPermille = 1200,
                MaterialsPriceIndexPermille = 900
            });

            Assert.That(applied.Succeeded, Is.True, applied.Message);
            Assert.That(applied.Message, Does.Contain("→"));
            Assert.That(_session.DebugIndustryState.Resources.Funds, Is.EqualTo(500));
            Assert.That(_session.DebugIndustryState.Resources.Supplies, Is.EqualTo(60));
            Assert.That(_session.DebugIndustryState.Resources.Intelligence, Is.EqualTo(7));
            Assert.That(_session.DebugMarketState.FoodInventory, Is.EqualTo(42));
            Assert.That(_session.DebugMarketState.FoodPriceIndexPermille, Is.EqualTo(1200));
            Assert.That(_session.DebugMarketState.MaterialsPriceIndexPermille, Is.EqualTo(900));
        }

        [Test]
        public void Resources_FaultInjectionRestoresEveryField()
        {
            DebugSession debug = Begin();
            string before = debug.CaptureFingerprint();
            debug.CommitFaultInjection = () => throw new InvalidOperationException("resource boom");

            DebugCommandResult result = debug.SetResourcesAndMarket(new DebugResourceRequest
            {
                Funds = 100,
                Supplies = 100,
                Intelligence = 100,
                FoodInventory = 100,
                FoodPriceIndexPermille = 1000,
                MaterialsPriceIndexPermille = 1000
            });

            Assert.That(result.Outcome, Is.EqualTo(DebugCommandOutcome.Faulted));
            Assert.That(debug.CaptureFingerprint(), Is.EqualTo(before));
        }

        // ---------------------------------------------------------------- event commands

        [Test]
        public void EventInjection_RejectsUnknownIdsAndASecondPendingEvent()
        {
            DebugSession debug = Begin();
            string before = debug.CaptureFingerprint();

            DebugCommandResult unknown = debug.InjectEvent("event.not-in-catalog");
            Assert.That(unknown.Succeeded, Is.False);
            Assert.That(unknown.Code, Is.EqualTo("event.unknown"));
            Assert.That(debug.CaptureFingerprint(), Is.EqualTo(before));

            DebugCommandResult first = debug.InjectEvent("event.rainy-arrival");
            Assert.That(first.Succeeded, Is.True, first.Message);
            Assert.That(first.Message, Does.Contain("调试注入"));
            Assert.That(_session.PendingEventId, Is.EqualTo("event.rainy-arrival"));

            DebugCommandResult second = debug.InjectEvent("event.sc013.refugee-arrival");
            Assert.That(second.Succeeded, Is.False);
            Assert.That(debug.PendingEventIds.Count, Is.EqualTo(1));
        }

        [Test]
        public void EventInjection_RunsTheRealOptionAndRefusesARepeatAfterCommit()
        {
            DebugSession debug = Begin();
            Assert.That(debug.InjectEvent("event.rainy-arrival").Succeeded, Is.True);

            EventChoiceResult choice = _session.TryChooseEventOption("event.rainy-arrival",
                "option.rainy-arrival.offer-rest", "debug.test.rainy.1");

            Assert.That(choice.Succeeded, Is.True, choice.Message);
            Assert.That(_session.PendingEventId, Is.Empty);
            DebugCommandResult repeat = debug.InjectEvent("event.rainy-arrival");
            Assert.That(repeat.Succeeded, Is.False,
                "a committed event must not be injected again.");
        }

        [Test]
        public void EventCatalogRows_ExposeStableIdsAndTitles()
        {
            DebugSession debug = Begin();
            Assert.That(debug.EventCatalogRows.Count, Is.GreaterThan(0));
            bool found = false;
            for (int i = 0; i < debug.EventCatalogRows.Count; i++)
            {
                DebugEventRow row = debug.EventCatalogRows[i];
                Assert.That(row.EventId, Is.Not.Empty);
                if (row.EventId == "event.rainy-arrival")
                {
                    found = true;
                    Assert.That(row.Title, Is.Not.Empty);
                }
            }
            Assert.That(found, Is.True);
        }

        // ---------------------------------------------------------------- headship integrity

        [Test]
        public void EditPerson_RejectsDemotingOrRemovingTheCurrentHead()
        {
            DebugSession debug = Begin();
            PersonId head = _session.Succession.HeadPersonId;
            string before = debug.CaptureFingerprint();

            DebugCommandResult demote = debug.EditPerson(new DebugPersonEdit
            {
                PersonId = head,
                DisplayName = "家主",
                Gender = PersonGender.Male,
                BirthYear = 1528,
                Alive = true,
                IsFamilyMember = true,
                Role = FamilyMemberRole.DirectKin
            });
            DebugCommandResult unlist = debug.EditPerson(new DebugPersonEdit
            {
                PersonId = head,
                DisplayName = "家主",
                Gender = PersonGender.Male,
                BirthYear = 1528,
                Alive = true,
                IsFamilyMember = false,
                Role = FamilyMemberRole.DirectKin
            });

            Assert.That(demote.Succeeded, Is.False);
            Assert.That(demote.Code, Is.EqualTo("person.head_role"));
            Assert.That(unlist.Succeeded, Is.False);
            Assert.That(_session.Succession.HeadPersonId, Is.EqualTo(head));
            Assert.That(_session.Succession.World.Family.TryGetRole(head,
                out FamilyMemberRole role), Is.True);
            Assert.That(role, Is.EqualTo(FamilyMemberRole.Head));
            Assert.That(debug.CaptureFingerprint(), Is.EqualTo(before));

            // Renaming the sitting head keeps exactly one Head and stays allowed.
            DebugCommandResult rename = debug.EditPerson(new DebugPersonEdit
            {
                PersonId = head,
                DisplayName = "家主改名",
                Gender = PersonGender.Male,
                BirthYear = 1528,
                Alive = true,
                IsFamilyMember = true,
                Role = FamilyMemberRole.Head
            });
            Assert.That(rename.Succeeded, Is.True, rename.Message);
            Assert.That(_session.Succession.HeadPersonId, Is.EqualTo(head));

            // TransferHeadship still works after the refusal.
            PersonId heir = Create(debug, "正式候选", PersonGender.Male, 1530,
                role: FamilyMemberRole.CollateralKin);
            Assert.That(debug.TransferHeadship(heir).Succeeded, Is.True);
            Assert.That(_session.Succession.HeadPersonId, Is.EqualTo(heir));
        }

        // ---------------------------------------------------------------- reversed symmetric facts

        [Test]
        public void KinshipRemove_AcceptsEitherEndpointOrderForSymmetricFacts()
        {
            DebugSession debug = Begin();
            PersonId left = Create(debug, "对称甲", PersonGender.Male, 1540);
            PersonId right = Create(debug, "对称乙", PersonGender.Female, 1542);

            Assert.That(debug.AddKinship(left, right, KinshipType.Spouse).Succeeded, Is.True);
            DebugKinshipPreview reversedSpouse =
                debug.PreviewKinship(right, left, KinshipType.Spouse, false);
            Assert.That(reversedSpouse.CanApply, Is.True, reversedSpouse.Reason);
            Assert.That(reversedSpouse.Reason, Does.Contain("→"));
            Assert.That(debug.RemoveKinship(right, left, KinshipType.Spouse).Succeeded, Is.True);
            Assert.That(_session.Succession.World.Kinship.LinksFor(left).Count, Is.EqualTo(0));

            Assert.That(debug.AddKinship(left, right, KinshipType.Sibling).Succeeded, Is.True);
            Assert.That(debug.PreviewKinship(right, left, KinshipType.Sibling, false).CanApply,
                Is.True);
            Assert.That(debug.RemoveKinship(right, left, KinshipType.Sibling).Succeeded, Is.True);
            Assert.That(_session.Succession.World.Kinship.LinksFor(left).Count, Is.EqualTo(0));
        }

        [Test]
        public void KinshipRemove_KeepsDirectedFactsOrientationSensitive()
        {
            DebugSession debug = Begin();
            PersonId parent = Create(debug, "定向父", PersonGender.Male, 1510);
            PersonId child = Create(debug, "定向子", PersonGender.Male, 1540);
            Assert.That(debug.AddKinship(parent, child, KinshipType.ParentChild).Succeeded,
                Is.True);

            DebugKinshipPreview reversed =
                debug.PreviewKinship(child, parent, KinshipType.ParentChild, false);

            Assert.That(reversed.CanApply, Is.False,
                "a child-to-parent fact is a different, absent fact.");
            Assert.That(debug.RemoveKinship(child, parent, KinshipType.ParentChild).Succeeded,
                Is.False);
            Assert.That(_session.Succession.World.Kinship.LinksFor(parent).Count, Is.EqualTo(1));
        }

        // ---------------------------------------------------------------- fault-injection rollback

        [Test]
        public void CreatePerson_FaultInjectionRestoresTheWholeSnapshot()
        {
            DebugSession debug = Begin();
            string before = debug.CaptureFingerprint();
            int persons = _session.Succession.World.PersonCount;
            debug.CommitFaultInjection = () => throw new InvalidOperationException("create boom");

            DebugCommandResult result = debug.CreatePerson(
                Draft("故障人物", PersonGender.Male, 1540), out PersonId created);

            Assert.That(result.Outcome, Is.EqualTo(DebugCommandOutcome.Faulted));
            Assert.That(created.IsEmpty, Is.True);
            Assert.That(_session.Succession.World.PersonCount, Is.EqualTo(persons));
            Assert.That(debug.CaptureFingerprint(), Is.EqualTo(before));
        }

        [Test]
        public void EditPerson_FaultInjectionAfterTheLifeWriteRestoresEveryFact()
        {
            DebugSession debug = Begin();
            PersonId id = Create(debug, "原名", PersonGender.Male, 1540);
            string before = debug.CaptureFingerprint();
            int faults = 0;
            debug.CommitFaultInjection = () =>
            {
                faults++;
                if (faults == 2)
                    throw new InvalidOperationException("edit boom");
            };

            DebugCommandResult result = debug.EditPerson(new DebugPersonEdit
            {
                PersonId = id,
                DisplayName = "新名",
                Gender = PersonGender.Female,
                BirthYear = 1530,
                Alive = false,
                IsFamilyMember = true,
                Role = FamilyMemberRole.Spouse
            });

            Assert.That(result.Outcome, Is.EqualTo(DebugCommandOutcome.Faulted));
            Assert.That(debug.CaptureFingerprint(), Is.EqualTo(before));
            Assert.That(_session.Succession.World.TryGetPerson(id, out PersonState person), Is.True);
            Assert.That(person.DisplayName, Is.EqualTo("原名"));
            Assert.That(person.Gender, Is.EqualTo(PersonGender.Male));
        }

        [Test]
        public void RemovePerson_FaultInjectionRestoresThePersonAndReferences()
        {
            DebugSession debug = Begin();
            PersonId id = Create(debug, "待移除", PersonGender.Male, 1540, member: false);
            string before = debug.CaptureFingerprint();
            debug.CommitFaultInjection = () => throw new InvalidOperationException("remove boom");

            DebugCommandResult result = debug.RemovePerson(id);

            Assert.That(result.Outcome, Is.EqualTo(DebugCommandOutcome.Faulted));
            Assert.That(_session.Succession.World.TryGetPerson(id, out _), Is.True);
            Assert.That(debug.CaptureFingerprint(), Is.EqualTo(before));
        }

        [Test]
        public void TransferHeadship_FaultInjectionRestoresBothRolesAndTheHeadId()
        {
            DebugSession debug = Begin();
            PersonId target = Create(debug, "故障家主", PersonGender.Male, 1530,
                role: FamilyMemberRole.CollateralKin);
            PersonId headBefore = _session.Succession.HeadPersonId;
            string before = debug.CaptureFingerprint();
            debug.CommitFaultInjection = () => throw new InvalidOperationException("head boom");

            DebugCommandResult result = debug.TransferHeadship(target);

            Assert.That(result.Outcome, Is.EqualTo(DebugCommandOutcome.Faulted));
            Assert.That(_session.Succession.HeadPersonId, Is.EqualTo(headBefore));
            Assert.That(_session.Succession.World.Family.TryGetRole(headBefore,
                out FamilyMemberRole oldRole), Is.True);
            Assert.That(oldRole, Is.EqualTo(FamilyMemberRole.Head));
            Assert.That(_session.Succession.World.Family.TryGetRole(target,
                out FamilyMemberRole targetRole), Is.True);
            Assert.That(targetRole, Is.EqualTo(FamilyMemberRole.CollateralKin));
            Assert.That(debug.CaptureFingerprint(), Is.EqualTo(before));
        }

        // ---------------------------------------------------------------- release boundary

        [Test]
        public void DebugSurface_IsCompiledOnlyInsideTheEditorOrDevelopmentGuard()
        {
            var expectations = new (string Path, string Symbol)[]
            {
                ("Assets/Scripts/Application/DebugSession.cs", "class DebugSession"),
                ("Assets/Scripts/Application/DebugSession.cs", "enum DebugCommandOutcome"),
                ("Assets/Scripts/Application/GameSession.Debug.cs",
                    "partial class GameSession"),
                ("Assets/Scripts/Presentation/DebugModePanel.cs", "class DebugModePanel"),
                ("Assets/Scripts/Application/GameSession.cs", "IsDebugSessionActive"),
                ("Assets/Scripts/Application/GameSession.cs", "CaptureSaveObserved"),
                ("Assets/Scripts/Application/GameSession.cs",
                    "ClearDebugSessionOnSessionStart"),
                ("Assets/Scripts/Presentation/TitleScreenController.cs",
                    "BindDebugModeEntry"),
                ("Assets/Scripts/Presentation/TitleScreenController.cs",
                    "StartDebugSession"),
                ("Assets/Scripts/Presentation/TitleScreenController.cs",
                    "DebugModeEntryBound"),
                ("Assets/Scripts/Events/EventRuntimeState.cs", "CanInjectDebugEvent"),
                ("Assets/Scripts/Events/EventRuntimeState.cs", "TryInjectDebugEvent"),
                ("Assets/Scripts/Domain/SuccessionState.cs", "TryReplacePerson"),
                ("Assets/Scripts/Domain/SuccessionState.cs", "CaptureSnapshot"),
                ("Assets/Scripts/Domain/SuccessionState.cs", "TryRestoreSnapshot"),
                ("Assets/Scripts/Domain/SuccessionState.cs",
                    "class SuccessionWorldSnapshot"),
                ("Assets/Scripts/Domain/FamilyState.cs", "RestoreMembers"),
                ("Assets/Scripts/Domain/KinshipGraph.cs", "RestoreLinks"),
                ("Assets/Scripts/Domain/FamilyPregnancyState.cs", "RestoreRecords")
            };

            foreach ((string path, string symbol) in expectations)
            {
                Assert.That(File.Exists(path), Is.True, path);
                string[] lines = File.ReadAllLines(path);
                bool[] guarded = GuardedLines(lines);
                int line = FindFirst(lines, symbol);
                Assert.That(line, Is.GreaterThanOrEqualTo(0),
                    symbol + " must exist in " + path);
                Assert.That(guarded[line], Is.True,
                    symbol + " must stay inside its Editor/development build guard in " + path);
            }
        }

        private static int FindFirst(string[] lines, string symbol)
        {
            for (int i = 0; i < lines.Length; i++)
                if (lines[i].Contains(symbol, StringComparison.Ordinal))
                    return i;
            return -1;
        }

        private static bool[] GuardedLines(string[] lines)
        {
            var guarded = new bool[lines.Length];
            bool inside = false;
            for (int i = 0; i < lines.Length; i++)
            {
                string trimmed = lines[i].Trim();
                if (!inside && trimmed.StartsWith("#if UNITY_EDITOR || DEVELOPMENT_BUILD",
                        StringComparison.Ordinal))
                {
                    inside = true;
                    guarded[i] = true;
                    continue;
                }
                if (inside && trimmed.StartsWith("#endif", StringComparison.Ordinal))
                {
                    guarded[i] = true;
                    inside = false;
                    continue;
                }
                guarded[i] = inside;
            }
            return guarded;
        }
    }
}
