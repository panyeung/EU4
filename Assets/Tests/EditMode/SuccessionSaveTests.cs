using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;
using SengokuChronicle.Persistence;
using UnityEngine;

namespace SengokuChronicle.Tests.EditMode
{
    public class SuccessionSaveTests
    {
        private GameSession _session;

        [SetUp]
        public void SetUp()
        {
            _session = GameSession.NewGame();
            _session.InitStandardDemo();
        }

        [TearDown]
        public void TearDown()
        {
            if (_session != null)
                UnityEngine.Object.DestroyImmediate(_session.gameObject);
        }

        [Test]
        public void CaptureSave_UsesCurrentSchemaVersion()
        {
            SaveData data = _session.CaptureSave();

            Assert.That(data.saveFormatVersion, Is.EqualTo(SaveDataValidator.CurrentVersion));
            Assert.That(SaveData.CurrentGameSchemaVersion, Is.EqualTo(SaveDataValidator.CurrentVersion));
            Assert.That(data.succession, Is.Not.Null);
            Assert.That(data.succession.headPersonId,
                Is.EqualTo(SuccessionSample.StandardHeadId.Value));
        }

        [Test]
        public void CaptureSave_AtPendingDecision_RoundTripsPhaseAndCandidates()
        {
            _session.BeginSuccessionRetirement();
            Assert.That(_session.SelectSuccessionSuccessor(
                new PersonId("person.akechi.masatsuna")).Succeeded, Is.True);
            SaveData data = _session.CaptureSave();

            Assert.That(data.succession.phase,
                Is.EqualTo((int)SuccessionPhase.AwaitingSuccessionChoice));
            Assert.That(data.succession.pendingSuccessorId,
                Is.EqualTo("person.akechi.masatsuna"));

            GameSession restored = GameSession.NewGame();
            try
            {
                Assert.That(restored.TryApplySave(data).Succeeded, Is.True);
                Assert.That(restored.Succession.Phase,
                    Is.EqualTo(SuccessionPhase.AwaitingSuccessionChoice));
                Assert.That(restored.Succession.PendingSuccessorId,
                    Is.EqualTo(new PersonId("person.akechi.masatsuna")));
                Assert.That(restored.SuccessionCandidates.Count, Is.EqualTo(3));
                Assert.That(restored.ConfirmSuccession().Succeeded, Is.True);
                Assert.That(restored.SuccessionHeadName, Is.EqualTo("明智 正綱"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(restored.gameObject);
            }
        }

        [Test]
        public void CaptureSave_AfterAdultHandoff_RoundTripsCompletedMarkers()
        {
            _session.BeginSuccessionRetirement();
            _session.SelectSuccessionSuccessor(new PersonId("person.akechi.masatsuna"));
            Assert.That(_session.ConfirmSuccession().Succeeded, Is.True);
            SaveData data = _session.CaptureSave();
            string resultId = data.succession.completedResultId;

            Assert.That(data.succession.phase, Is.EqualTo((int)SuccessionPhase.None));
            Assert.That(data.succession.successionCount, Is.EqualTo(1));
            Assert.That(resultId, Does.StartWith("succession.1."));

            GameSession restored = GameSession.NewGame();
            try
            {
                Assert.That(restored.TryApplySave(data).Succeeded, Is.True);
                Assert.That(restored.SuccessionHeadName, Is.EqualTo("明智 正綱"));
                Assert.That(restored.Succession.SuccessionCount, Is.EqualTo(1));
                Assert.That(restored.Succession.CompletedResultId, Is.EqualTo(resultId));
                Assert.That(restored.ConfirmSuccession().Succeeded, Is.False);
                Assert.That(restored.Succession.World.TryGetLife(
                    SuccessionSample.StandardHeadId, out SuccessionPersonLife oldLife), Is.True);
                Assert.That(oldLife.Retired, Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(restored.gameObject);
            }
        }

        [Test]
        public void CaptureSave_DuringRegency_RoundTripsRegentAndCompletesAfterLoad()
        {
            _session.BeginSuccessionRetirement();
            _session.SelectSuccessionSuccessor(new PersonId("person.akechi.tokihime"));
            _session.SelectSuccessionRegent(new PersonId("person.akechi.chie"));
            Assert.That(_session.ConfirmSuccession().Succeeded, Is.True);
            SaveData data = _session.CaptureSave();

            Assert.That(data.succession.phase, Is.EqualTo((int)SuccessionPhase.Regency));
            Assert.That(data.succession.pendingRegentId, Is.EqualTo("person.akechi.chie"));

            GameSession restored = GameSession.NewGame();
            try
            {
                Assert.That(restored.TryApplySave(data).Succeeded, Is.True);
                Assert.That(restored.Succession.Phase, Is.EqualTo(SuccessionPhase.Regency));
                Assert.That(restored.SuccessionHeadName, Is.EqualTo("明智 時姫"));
                Assert.That(restored.Succession.PendingRegentId,
                    Is.EqualTo(new PersonId("person.akechi.chie")));

                Assert.That(restored.AdvanceDays(800), Is.True);
                Assert.That(restored.Succession.Phase, Is.EqualTo(SuccessionPhase.Regency));
                Assert.That(restored.Succession.IsRegencyHandoffDue(
                    restored.WorldClock.CurrentDate), Is.True);
                Assert.That(restored.AdvanceDays(1), Is.False);

                SuccessionResult handoff = restored.TryCompleteSuccessionRegency();
                Assert.That(handoff.Succeeded, Is.True, handoff.Message);
                Assert.That(restored.Succession.Phase, Is.EqualTo(SuccessionPhase.None));
                Assert.That(restored.Succession.RegencyCompleted, Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(restored.gameObject);
            }
        }

        [Test]
        public void CaptureSave_AtDueRegency_RestoresPauseAndRequiresHandoff()
        {
            _session.BeginSuccessionRetirement();
            _session.SelectSuccessionSuccessor(new PersonId("person.akechi.tokihime"));
            _session.SelectSuccessionRegent(new PersonId("person.akechi.chie"));
            Assert.That(_session.ConfirmSuccession().Succeeded, Is.True);
            Assert.That(_session.AdvanceDays(800), Is.True);
            Assert.That(_session.Succession.IsRegencyHandoffDue(
                _session.WorldClock.CurrentDate), Is.True);
            SaveData data = _session.CaptureSave();

            GameSession restored = GameSession.NewGame();
            try
            {
                Assert.That(restored.TryApplySave(data).Succeeded, Is.True);
                Assert.That(restored.Succession.Phase, Is.EqualTo(SuccessionPhase.Regency));
                Assert.That(restored.Succession.IsRegencyHandoffDue(
                    restored.WorldClock.CurrentDate), Is.True);
                Assert.That(restored.AdvanceDays(1), Is.False);
                Assert.That(restored.LastSuccessionMessage, Does.Contain("完成成年交权"));
                Assert.That(restored.TryCompleteSuccessionRegency().Succeeded, Is.True);
                Assert.That(restored.Succession.Phase, Is.EqualTo(SuccessionPhase.None));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(restored.gameObject);
            }
        }

        [Test]
        public void TryApplySave_MalformedSuccessionData_LeavesLiveSessionUnchanged()
        {
            _session.BeginSuccessionRetirement();
            SaveData invalid = _session.CaptureSave();
            invalid.succession.headPersonId = "person.unknown";
            string head = _session.SuccessionHeadName;
            SuccessionPhase phase = _session.Succession.Phase;
            GameDate date = _session.WorldClock.CurrentDate;

            SaveOperationResult result = _session.TryApplySave(invalid);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Error, Is.EqualTo(SaveErrorCode.InvalidData));
            Assert.That(_session.SuccessionHeadName, Is.EqualTo(head));
            Assert.That(_session.Succession.Phase, Is.EqualTo(phase));
            Assert.That(_session.WorldClock.CurrentDate, Is.EqualTo(date));
        }

        [Test]
        public void CaptureSave_AtCrisisDecision_RoundTripsPendingCrisis()
        {
            Assert.That(_session.StartCrisisDemo(), Is.True);
            _session.BeginSuccessionRetirement();
            Assert.That(_session.Succession.Phase, Is.EqualTo(SuccessionPhase.CrisisDecision));
            SaveData data = _session.CaptureSave();

            GameSession restored = GameSession.NewGame();
            try
            {
                Assert.That(restored.TryApplySave(data).Succeeded, Is.True);
                Assert.That(restored.Succession.Phase, Is.EqualTo(SuccessionPhase.CrisisDecision));
                Assert.That(restored.SuccessionCandidates.Count, Is.Zero);
                Assert.That(SuccessionService.CanOfferCrisis(restored.Succession,
                    restored.WorldClock.CurrentDate), Is.True);
                Assert.That(restored.AcceptCrisisAdoption().Succeeded, Is.True);
                Assert.That(restored.SuccessionHeadName, Is.EqualTo("養子 宗次"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(restored.gameObject);
            }
        }

        [Test]
        public void CaptureSave_AfterCrisisAdoption_RoundTripsAcceptedState()
        {
            Assert.That(_session.StartCrisisDemo(), Is.True);
            _session.BeginSuccessionRetirement();
            Assert.That(_session.AcceptCrisisAdoption().Succeeded, Is.True);
            string resultId = _session.Succession.CompletedResultId;
            SaveData data = _session.CaptureSave();

            GameSession restored = GameSession.NewGame();
            try
            {
                Assert.That(restored.TryApplySave(data).Succeeded, Is.True);
                Assert.That(restored.Succession.Phase, Is.EqualTo(SuccessionPhase.None));
                Assert.That(restored.SuccessionHeadName, Is.EqualTo("養子 宗次"));
                Assert.That(restored.Succession.CrisisAdoptionUsed, Is.True);
                Assert.That(restored.Succession.CompletedResultId, Is.EqualTo(resultId));
                Assert.That(restored.Succession.DesignatedHeirId.IsEmpty, Is.True);
                Assert.That(restored.Succession.World.Kinship.Contains(new KinshipLink(
                    SuccessionSample.StandardHeadId, restored.Succession.HeadPersonId,
                    KinshipType.Adoption)), Is.True);
                SuccessionResult retry = restored.BeginSuccessionRetirement();
                Assert.That(retry.Outcome, Is.EqualTo(SuccessionOutcome.Extinction));
                Assert.That(restored.Succession.Phase, Is.EqualTo(SuccessionPhase.Extinct),
                    "The one-shot crisis must not offer a second adoption.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(restored.gameObject);
            }
        }

        [Test]
        public void CaptureSave_AfterCrisisDecline_RoundTripsExtinctState()
        {
            Assert.That(_session.StartCrisisDemo(), Is.True);
            _session.BeginSuccessionRetirement();
            Assert.That(_session.DeclineCrisis().Succeeded, Is.True);
            string resultId = _session.Succession.CompletedResultId;
            SaveData data = _session.CaptureSave();

            GameSession restored = GameSession.NewGame();
            try
            {
                Assert.That(restored.TryApplySave(data).Succeeded, Is.True);
                Assert.That(restored.Succession.Phase, Is.EqualTo(SuccessionPhase.Extinct));
                Assert.That(restored.Succession.CompletedResultId, Is.EqualTo(resultId));
                Assert.That(restored.BeginSuccessionRetirement().Succeeded, Is.False);
                Assert.That(restored.Succession.Phase, Is.EqualTo(SuccessionPhase.Extinct));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(restored.gameObject);
            }
        }

        [Test]
        public void SaveDataValidator_RejectsRetiredHead()
        {
            SaveData data = _session.CaptureSave();
            SuccessionPersonSaveEntry head = Array.Find(data.succession.persons,
                entry => entry != null && entry.personId == SuccessionSample.StandardHeadId.Value);
            Assert.That(head, Is.Not.Null);
            head.retired = true;

            SaveOperationResult result = SaveDataValidator.Validate(data);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Error, Is.EqualTo(SaveErrorCode.InvalidData));
            Assert.That(result.Message, Does.Contain("退休"));
        }

        [Test]
        public void V2Save_MigratesToCurrentVersionWithPlayableSuccessionRoster()
        {
            SaveData legacy = _session.CaptureSave();
            legacy.saveFormatVersion = 2;
            legacy.succession = null;
            legacy.chronicle = null;

            bool succeeded = SaveDataMigrator.TryMigrate(legacy,
                out SaveData migrated, out SaveOperationResult result);

            Assert.That(succeeded, Is.True, result.Message);
            Assert.That(migrated.saveFormatVersion, Is.EqualTo(SaveDataValidator.CurrentVersion));
            Assert.That(migrated.succession, Is.Not.Null);
            Assert.That(migrated.succession.headPersonId,
                Is.EqualTo(SuccessionSample.StandardHeadId.Value));
            Assert.That(SaveDataValidator.Validate(migrated).Succeeded, Is.True);
            Assert.That(legacy.saveFormatVersion, Is.EqualTo(2));
            Assert.That(legacy.succession, Is.Null);
        }

        [Test]
        public void SaveToAndLoadFrom_PendingSuccession_RoundTripsThroughStore()
        {
            string directory = Path.Combine(Path.GetTempPath(),
                "SengokuChronicleTests", Guid.NewGuid().ToString("N"));
            var store = new SaveSlotStore(directory);
            GameSession restored = null;
            try
            {
                _session.BeginSuccessionRetirement();
                _session.SelectSuccessionSuccessor(new PersonId("person.akechi.masatsuna"));
                Assert.That(_session.SaveTo(store).Succeeded, Is.True);

                restored = GameSession.NewGame();
                Assert.That(restored.LoadFrom(store).Succeeded, Is.True);
                Assert.That(restored.Succession.Phase,
                    Is.EqualTo(SuccessionPhase.AwaitingSuccessionChoice));
                Assert.That(restored.Succession.PendingSuccessorId,
                    Is.EqualTo(new PersonId("person.akechi.masatsuna")));
                Assert.That(restored.ConfirmSuccession().Succeeded, Is.True);
                Assert.That(restored.SuccessionHeadName, Is.EqualTo("明智 正綱"));
            }
            finally
            {
                if (restored != null)
                    UnityEngine.Object.DestroyImmediate(restored.gameObject);
                if (Directory.Exists(directory))
                    Directory.Delete(directory, true);
            }
        }

        [Test]
        public void SaveToAndLoadFrom_CompletedSuccession_RoundTripsThroughStore()
        {
            string directory = Path.Combine(Path.GetTempPath(),
                "SengokuChronicleTests", Guid.NewGuid().ToString("N"));
            var store = new SaveSlotStore(directory);
            GameSession restored = null;
            try
            {
                _session.BeginSuccessionRetirement();
                _session.SelectSuccessionSuccessor(new PersonId("person.akechi.masatsuna"));
                Assert.That(_session.ConfirmSuccession().Succeeded, Is.True);
                string resultId = _session.Succession.CompletedResultId;
                Assert.That(_session.SaveTo(store).Succeeded, Is.True);

                restored = GameSession.NewGame();
                Assert.That(restored.LoadFrom(store).Succeeded, Is.True);
                Assert.That(restored.SuccessionHeadName, Is.EqualTo("明智 正綱"));
                Assert.That(restored.Succession.CompletedResultId, Is.EqualTo(resultId));
                Assert.That(restored.Succession.SuccessionCount, Is.EqualTo(1));
            }
            finally
            {
                if (restored != null)
                    UnityEngine.Object.DestroyImmediate(restored.gameObject);
                if (Directory.Exists(directory))
                    Directory.Delete(directory, true);
            }
        }

        [Test]
        public void V3Presence_MissingSuccession_IsRejected()
        {
            string json = V3JsonWithoutSuccession();
            SaveDataPresenceProbe probe = JsonUtility.FromJson<SaveDataPresenceProbe>(json);

            SaveOperationResult result = SaveDataValidator.ValidatePresence(probe, json);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Error, Is.EqualTo(SaveErrorCode.InvalidData));
            Assert.That(result.Message, Does.Contain("succession"));
        }

        [Test]
        public void V4Presence_CompleteSave_Passes()
        {
            SaveData data = _session.CaptureSave();
            string json = JsonUtility.ToJson(data);
            SaveDataPresenceProbe probe = JsonUtility.FromJson<SaveDataPresenceProbe>(json);

            SaveOperationResult result = SaveDataValidator.ValidatePresence(probe, json);

            Assert.That(result.Succeeded, Is.True, result.Message);
        }

        [Test]
        public void SaveDataValidator_RejectsSuccessionPhaseMismatch()
        {
            SaveData data = _session.CaptureSave();
            data.succession.phase = (int)SuccessionPhase.Regency;
            data.succession.pendingRegentId = string.Empty;

            SaveOperationResult result = SaveDataValidator.Validate(data);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Error, Is.EqualTo(SaveErrorCode.InvalidData));
        }

        private static string V3JsonWithoutSuccession()
        {
            var parts = new List<string>
            {
                "\"saveFormatVersion\":3",
                "\"gameVersion\":\"0.1.0\"",
                "\"rulesVersion\":1",
                "\"eventContentVersion\":1",
                "\"year\":1560",
                "\"month\":4",
                "\"day\":1",
                "\"locationId\":\"owari.kiyosu\"",
                "\"supplyShortageStage\":0",
                "\"sealedLetterStage\":0",
                "\"marketTallyStage\":0",
                "\"relationships\":[]",
                "\"randomState\":" + JsonUtility.ToJson(RandomStateManager.Create(7UL).CaptureState()),
                "\"eventRuntime\":" + JsonUtility.ToJson(new EventRuntimeSaveData())
            };
            return "{" + string.Join(",", parts) + "}";
        }
    }
}
