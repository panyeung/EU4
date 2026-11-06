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

        private GameSession Fresh()
        {
            Object.DestroyImmediate(_session.gameObject);
            _session = GameSession.NewGame();
            return _session;
        }
    }
}
