using System.Linq;
using NUnit.Framework;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;
using SengokuChronicle.Persistence;
using UnityEngine;

namespace SengokuChronicle.Tests.EditMode
{
    /// <summary>SC-022 Task 2: readable, append-only family-formation chronicle records.</summary>
    public sealed class FamilyFormationChronicleTests
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
        public void Marriage_AppendsExactlyOneReadableRecord()
        {
            Assert.That(_session.TryRegisterMarriage(SC022FamilyFormationScenario.HeadId,
                SC022FamilyFormationScenario.MarriageCandidateOneId).Succeeded, Is.True);

            FamilyChronicleEntry[] entries = _session.ChronicleEntries
                .Where(entry => entry.Category == ChronicleCategory.FamilyFormation).ToArray();
            Assert.That(entries, Has.Length.EqualTo(1));
            Assert.That(entries[0].SourceId, Is.EqualTo(FamilyChronicleEntry.MarriageSourceId));
            Assert.That(entries[0].SubjectId, Is.EqualTo(SC022FamilyFormationScenario.HeadId.Value));
            Assert.That(entries[0].Title, Is.EqualTo("家族婚姻"));
            Assert.That(entries[0].Summary, Does.Contain(SC022FamilyFormationScenario.HeadName));
            Assert.That(entries[0].Summary,
                Does.Contain(SC022FamilyFormationScenario.MarriageCandidateOneName));
            Assert.That(entries[0].Summary, Does.Not.Contain("person."));
            Assert.That(entries[0].Summary, Does.Not.Contain("family."));
        }

        [Test]
        public void Adoption_AppendsExactlyOneReadableRecord()
        {
            Assert.That(_session.TryAdopt(SC022FamilyFormationScenario.HeadId,
                SC022FamilyFormationScenario.ChildAdoptionCandidateId).Succeeded, Is.True);

            FamilyChronicleEntry[] entries = _session.ChronicleEntries
                .Where(entry => entry.Category == ChronicleCategory.FamilyFormation).ToArray();
            Assert.That(entries, Has.Length.EqualTo(1));
            Assert.That(entries[0].SourceId, Is.EqualTo(FamilyChronicleEntry.AdoptionSourceId));
            Assert.That(entries[0].Title, Is.EqualTo("家族收养"));
            Assert.That(entries[0].Summary, Does.Contain(SC022FamilyFormationScenario.HeadName));
            Assert.That(entries[0].Summary,
                Does.Contain(SC022FamilyFormationScenario.ChildAdoptionCandidateName));
            Assert.That(entries[0].Summary, Does.Not.Contain("person."));
        }

        [Test]
        public void RejectedCommands_AppendNoRecord()
        {
            Assert.That(_session.TryRegisterMarriage(SC022FamilyFormationScenario.HeadId,
                SC022FamilyFormationScenario.HeadId).Succeeded, Is.False);
            Assert.That(_session.TryRegisterMarriage(SC022FamilyFormationScenario.HeadId,
                SC022FamilyFormationScenario.BloodAncestorId).Succeeded, Is.False);
            Assert.That(_session.TryAdopt(SC022FamilyFormationScenario.HeadId,
                SC022FamilyFormationScenario.SpouseId).Succeeded, Is.False);
            Assert.That(_session.TryAdopt(SC022FamilyFormationScenario.HeadId,
                SC022FamilyFormationScenario.BloodAncestorId).Succeeded, Is.False);

            Assert.That(_session.ChronicleEntries, Is.Empty);
        }

        [Test]
        public void Ledger_PrepareDoesNotMutateAndAppendPreparedAddsOne()
        {
            HistoryLedger ledger = HistoryLedger.CreateEmpty();
            Assert.That(ledger.TryPrepareFamilyFormation(1560, 5, "person.a",
                FamilyChronicleEntry.MarriageSourceId, "家族婚姻", "摘要", out FamilyChronicleEntry entry,
                out string error), Is.True, error);
            Assert.That(ledger.Count, Is.Zero, "preparation must not append");
            Assert.That(ledger.TryAppendPrepared(entry, out error), Is.True, error);
            Assert.That(ledger.Count, Is.EqualTo(1));
            Assert.That(ledger.Entries[0].Category, Is.EqualTo(ChronicleCategory.FamilyFormation));
        }

        [Test]
        public void Ledger_RejectsUnknownFormationSource()
        {
            HistoryLedger ledger = HistoryLedger.CreateEmpty();
            Assert.That(ledger.TryAppendFamilyFormation(1560, 5, "person.a", "other",
                "家族关系", "摘要", out _, out string error), Is.False);
            Assert.That(error, Does.Contain("来源"));
            Assert.That(ledger.Count, Is.Zero);
        }

        [Test]
        public void Ledger_RestorePreservesFamilyFormationOrderAndText()
        {
            Assert.That(_session.TryRegisterMarriage(SC022FamilyFormationScenario.HeadId,
                SC022FamilyFormationScenario.MarriageCandidateOneId).Succeeded, Is.True);
            Assert.That(_session.TryAdopt(SC022FamilyFormationScenario.HeadId,
                SC022FamilyFormationScenario.ChildAdoptionCandidateId).Succeeded, Is.True);

            Assert.That(HistoryLedger.TryRestore(_session.ChronicleEntries.ToArray(),
                out HistoryLedger restored, out string error), Is.True, error);
            Assert.That(restored.Count, Is.EqualTo(2));
            Assert.That(restored.Entries[0].SourceId,
                Is.EqualTo(FamilyChronicleEntry.MarriageSourceId));
            Assert.That(restored.Entries[1].SourceId,
                Is.EqualTo(FamilyChronicleEntry.AdoptionSourceId));
            Assert.That(restored.Entries[0].Summary, Does.Contain(
                SC022FamilyFormationScenario.MarriageCandidateOneName));
            Assert.That(restored.Entries[1].Summary, Does.Contain(
                SC022FamilyFormationScenario.ChildAdoptionCandidateName));
        }
    }
}
