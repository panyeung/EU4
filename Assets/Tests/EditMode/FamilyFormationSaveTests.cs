using System.Linq;
using NUnit.Framework;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;
using SengokuChronicle.Persistence;
using UnityEngine;

namespace SengokuChronicle.Tests.EditMode
{
    /// <summary>SC-022 Task 5: existing save model round-trips candidates, relationships and chronicle.</summary>
    public sealed class FamilyFormationSaveTests
    {
        private GameSession _session;

        [SetUp]
        public void SetUp()
        {
            _session = GameSession.NewGame();
            _session.InitFamilyFormationDemoScenario();
        }

        [TearDown]
        public void TearDown()
        {
            if (_session != null)
                Object.DestroyImmediate(_session.gameObject);
        }

        [Test]
        public void UnmarriedCandidates_AreSavedWithoutFamilyMembership()
        {
            SaveData data = _session.CaptureSave();

            Assert.That(data.saveFormatVersion, Is.EqualTo(SaveDataValidator.CurrentVersion),
                "SC-022 reuses the existing save schema");
            Assert.That(data.succession.persons.Any(person =>
                person.personId == SC022FamilyFormationScenario.MarriageCandidateOneId.Value),
                Is.True);
            Assert.That(data.succession.members.Any(member =>
                member.personId == SC022FamilyFormationScenario.MarriageCandidateOneId.Value),
                Is.False);
            Assert.That(SaveDataValidator.Validate(data).Succeeded, Is.True);

            GameSession restored = GameSession.NewGame();
            try
            {
                Assert.That(restored.TryApplySave(data).Succeeded, Is.True);
                Assert.That(restored.FamilyFormation.Members, Is.Not.Empty);
                Assert.That(restored.FamilyFormation.MarriageCandidatesFor(
                    SC022FamilyFormationScenario.HeadId).Count, Is.EqualTo(3));
                Assert.That(restored.Succession.World.Family.IsMember(
                    SC022FamilyFormationScenario.MarriageCandidateOneId), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(restored.gameObject);
            }
        }

        [Test]
        public void MarriageAndAdoption_RoundTripThroughJsonWithRolesEdgesAndReadableChronicle()
        {
            Assert.That(_session.TryRegisterMarriage(SC022FamilyFormationScenario.HeadId,
                SC022FamilyFormationScenario.MarriageCandidateOneId).Succeeded, Is.True);
            Assert.That(_session.TryAdopt(SC022FamilyFormationScenario.HeadId,
                SC022FamilyFormationScenario.ChildAdoptionCandidateId).Succeeded, Is.True);
            SaveData data = _session.CaptureSave();
            string json = JsonUtility.ToJson(data, true);
            Assert.That(SaveDataValidator.Validate(data).Succeeded, Is.True);

            SaveData parsed = JsonUtility.FromJson<SaveData>(json);
            GameSession restored = GameSession.NewGame();
            try
            {
                Assert.That(restored.TryApplySave(parsed).Succeeded, Is.True);

                Assert.That(restored.Succession.World.Family.TryGetRole(
                    SC022FamilyFormationScenario.MarriageCandidateOneId,
                    out FamilyMemberRole spouseRole), Is.True);
                Assert.That(spouseRole, Is.EqualTo(FamilyMemberRole.Spouse));
                Assert.That(restored.Succession.World.Family.TryGetRole(
                    SC022FamilyFormationScenario.ChildAdoptionCandidateId,
                    out FamilyMemberRole adoptedRole), Is.True);
                Assert.That(adoptedRole, Is.EqualTo(FamilyMemberRole.AdoptedChild));
                Assert.That(restored.Succession.World.Kinship.Contains(new KinshipLink(
                    SC022FamilyFormationScenario.HeadId,
                    SC022FamilyFormationScenario.MarriageCandidateOneId,
                    KinshipType.Spouse)), Is.True);
                Assert.That(restored.Succession.World.Kinship.Contains(new KinshipLink(
                    SC022FamilyFormationScenario.HeadId,
                    SC022FamilyFormationScenario.ChildAdoptionCandidateId,
                    KinshipType.Adoption)), Is.True);

                Assert.That(restored.ChronicleEntries.Count, Is.EqualTo(2));
                Assert.That(restored.ChronicleEntries[0].Category,
                    Is.EqualTo(ChronicleCategory.FamilyFormation));
                Assert.That(restored.ChronicleEntries[0].SourceId,
                    Is.EqualTo(FamilyChronicleEntry.MarriageSourceId));
                Assert.That(restored.ChronicleEntries[1].SourceId,
                    Is.EqualTo(FamilyChronicleEntry.AdoptionSourceId));
                Assert.That(restored.ChronicleEntries[0].Summary,
                    Does.Contain(SC022FamilyFormationScenario.MarriageCandidateOneName));
                Assert.That(restored.ChronicleEntries[1].Summary,
                    Does.Contain(SC022FamilyFormationScenario.ChildAdoptionCandidateName));
                Assert.That(restored.ChronicleEntries[0].Summary, Does.Not.Contain("person."));
            }
            finally
            {
                Object.DestroyImmediate(restored.gameObject);
            }
        }

        [Test]
        public void OldStandardSave_StillLoadsWithoutFamilyFormationRecords()
        {
            _session = Fresh();
            _session.InitStandardDemo();
            SaveData data = _session.CaptureSave();
            Assert.That(data.succession.familyId,
                Is.EqualTo(SuccessionSample.StandardFamilyId.Value));
            Assert.That(data.chronicle, Is.Empty);

            GameSession restored = GameSession.NewGame();
            try
            {
                Assert.That(restored.TryApplySave(data).Succeeded, Is.True);
                Assert.That(restored.Succession.World.Family.Id,
                    Is.EqualTo(SuccessionSample.StandardFamilyId));
                Assert.That(restored.ChronicleEntries, Is.Empty);
                Assert.That(restored.GetFamilyFormationView().IsScenarioActive, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(restored.gameObject);
            }
        }

        [Test]
        public void AdoptedFamily_TwoHandoffsAcrossJsonLoadKeepKinshipAndPrivateValues()
        {
            PersonId originalHead = SC022FamilyFormationScenario.HeadId;
            PersonId firstHeir = SC022FamilyFormationScenario.AdultAdoptionCandidateId;
            PersonId secondHeir = SC022FamilyFormationScenario.MarriageCandidateOneId;
            PersonId member = SC022FamilyFormationScenario.ChildId;
            Assert.That(_session.Succession.World.Family.IsMember(firstHeir), Is.False);
            Assert.That(_session.Succession.World.Family.IsMember(secondHeir), Is.False);
            Assert.That(_session.TryAdopt(originalHead, firstHeir).Succeeded, Is.True);
            Assert.That(_session.TryAdopt(originalHead, secondHeir).Succeeded, Is.True);
            SaveData fixture = _session.CaptureSave();
            fixture.development.persons.Single(row => row.personId == originalHead.Value).ninjutsu = 31;
            fixture.development.persons.Single(row => row.personId == firstHeir.Value).ninjutsu = 11;
            fixture.development.persons.Single(row => row.personId == secondHeir.Value).ninjutsu = 7;
            Assert.That(_session.TryApplySave(fixture).Succeeded, Is.True, _session.LastEventMessage);
            Assert.That(_session.TryChangeFamilyRelationship(member, 12).Succeeded, Is.True);
            SaveData baseline = _session.CaptureSave();
            string[] kinship = baseline.succession.kinship.Select(edge => JsonUtility.ToJson(edge)).ToArray();
            string development = JsonUtility.ToJson(baseline.development);
            int familySize = _session.Succession.World.Family.MemberCount;
            GameDate date = _session.WorldClock.CurrentDate;

            Assert.That(_session.BeginSuccessionRetirement().Succeeded, Is.True, _session.LastSuccessionMessage);
            Assert.That(_session.SelectSuccessionSuccessor(firstHeir).Succeeded, Is.True, _session.LastSuccessionMessage);
            Assert.That(_session.ConfirmSuccession().Succeeded, Is.True, _session.LastSuccessionMessage);
            Assert.That(_session.Succession.HeadPersonId, Is.EqualTo(firstHeir));
            Assert.That(_session.Succession.SuccessionCount, Is.EqualTo(1));
            Assert.That(_session.TryGetFamilyRelationship(member).CurrentValue, Is.Zero);
            Assert.That(_session.TryGetFamilyRelationship(originalHead, member).CurrentValue, Is.EqualTo(12));
            Assert.That(_session.TryChangeFamilyRelationship(member, 5).Succeeded, Is.True);
            SaveData saved = _session.CaptureSave();
            Assert.That(SaveDataValidator.Validate(saved).Succeeded, Is.True);

            GameSession restored = GameSession.NewGame();
            try
            {
                Assert.That(restored.TryApplySave(JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(saved))).Succeeded,
                    Is.True, restored.LastEventMessage);
                Assert.That(restored.Succession.HeadPersonId, Is.EqualTo(firstHeir));
                Assert.That(restored.Succession.SuccessionCount, Is.EqualTo(1));
                Assert.That(restored.TryGetFamilyRelationship(member).CurrentValue, Is.EqualTo(5));
                Assert.That(restored.BeginSuccessionRetirement().Succeeded, Is.True, restored.LastSuccessionMessage);
                Assert.That(restored.SelectSuccessionSuccessor(secondHeir).Succeeded, Is.True, restored.LastSuccessionMessage);
                Assert.That(restored.ConfirmSuccession().Succeeded, Is.True, restored.LastSuccessionMessage);
                Assert.That(restored.Succession.HeadPersonId, Is.EqualTo(secondHeir));
                Assert.That(restored.Succession.SuccessionCount, Is.EqualTo(2));
                Assert.That(restored.TryGetFamilyRelationship(member).CurrentValue, Is.Zero);
                Assert.That(restored.TryGetFamilyRelationship(originalHead, member).CurrentValue, Is.EqualTo(12));
                Assert.That(restored.TryGetFamilyRelationship(firstHeir, member).CurrentValue, Is.EqualTo(5));
                Assert.That(restored.Succession.World.Family.MemberCount, Is.EqualTo(familySize));
                Assert.That(restored.WorldClock.CurrentDate, Is.EqualTo(date));
                Assert.That(restored.Succession.Phase, Is.EqualTo(SuccessionPhase.None));
                SaveData final = restored.CaptureSave();
                Assert.That(SaveDataValidator.Validate(final).Succeeded, Is.True);
                Assert.That(final.succession.kinship.Select(edge => JsonUtility.ToJson(edge)).ToArray(),
                    Is.EqualTo(kinship));
                Assert.That(JsonUtility.ToJson(final.development), Is.EqualTo(development));
                Assert.That(final.chronicle.Length, Is.GreaterThan(saved.chronicle.Length));
                Assert.That(restored.TryApplySave(JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(final))).Succeeded,
                    Is.True, restored.LastEventMessage);
                Assert.That(restored.Succession.HeadPersonId, Is.EqualTo(secondHeir));
                Assert.That(restored.Succession.SuccessionCount, Is.EqualTo(2));
                Assert.That(restored.ChronicleEntries.Count, Is.EqualTo(final.chronicle.Length));
            }
            finally
            {
                Object.DestroyImmediate(restored.gameObject);
            }
        }

        private GameSession Fresh()
        {
            Object.DestroyImmediate(_session.gameObject);
            _session = GameSession.NewGame();
            return _session;
        }
    }
}
