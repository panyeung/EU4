using NUnit.Framework;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;

namespace SengokuChronicle.Tests.EditMode
{
    /// <summary>SC-021 Task 1: one active project, progress, completion and clone independence.</summary>
    public sealed class FamilyResearchStateTests
    {
        private static readonly GameDate Start = new GameDate(1560, 1, 1);

        [Test]
        public void EmptyState_HasNoPlanAndNoFamilyKnowledge()
        {
            FamilyResearchState state = FamilyResearchState.CreateInitial(Start);

            Assert.That(state.HasPlan, Is.False);
            Assert.That(state.ResearchId, Is.Empty);
            Assert.That(state.ResearcherId.IsEmpty, Is.True);
            Assert.That(state.Progress, Is.Zero);
            Assert.That(state.KnownTechniqueIds, Is.Empty);
            Assert.That(state.LastSettledMonthIndex,
                Is.EqualTo(IndustryCalendar.MonthIndex(Start) - 1));
        }

        [Test]
        public void Start_RejectsUnknownProjectAndEmptyResearcher()
        {
            FamilyResearchState state = FamilyResearchState.CreateInitial(Start);

            Assert.That(state.TryStartOrReplacePlan("research.unknown",
                SC021FamilyResearchScenario.HeadId, out string unknownError), Is.False);
            Assert.That(unknownError, Does.Contain("未知研究项目"));
            Assert.That(state.TryStartOrReplacePlan(FamilyResearchCatalog.SmokeMixingResearchId,
                default, out string researcherError), Is.False);
            Assert.That(researcherError, Does.Contain("研究者"));
            Assert.That(state.HasPlan, Is.False);
        }

        [Test]
        public void Advance_IsDeterministicAndCompletesExactlyOnce()
        {
            FamilyResearchState state = FamilyResearchState.CreateInitial(Start);
            Assert.That(state.TryStartOrReplacePlan(FamilyResearchCatalog.SmokeMixingResearchId,
                SC021FamilyResearchScenario.HeadId, out _), Is.True);

            Assert.That(state.TryAdvance(1, out _), Is.True);
            Assert.That(state.HasPlan, Is.True);
            Assert.That(state.Progress, Is.EqualTo(1));
            Assert.That(state.KnownTechniqueIds, Is.Empty);

            Assert.That(state.TryAdvance(1, out _), Is.True);
            Assert.That(state.HasPlan, Is.False);
            Assert.That(state.KnownTechniqueIds, Has.Count.EqualTo(1));
            Assert.That(state.KnownTechniqueIds[0],
                Is.EqualTo(FamilyResearchCatalog.SmokeMixingTechniqueId));
            Assert.That(state.TryAdvance(1, out string error), Is.False);
            Assert.That(error, Does.Contain("没有进行中的研究项目"));
        }

        [Test]
        public void ReplacingResearcher_PreservesProgress()
        {
            FamilyResearchState state = FamilyResearchState.CreateInitial(Start);
            Assert.That(state.TryStartOrReplacePlan(FamilyResearchCatalog.SmokeMixingResearchId,
                SC021FamilyResearchScenario.HeadId, out _), Is.True);
            Assert.That(state.TryAdvance(1, out _), Is.True);

            Assert.That(state.TryStartOrReplacePlan(FamilyResearchCatalog.SmokeMixingResearchId,
                SC021FamilyResearchScenario.SpouseId, out _), Is.True);
            Assert.That(state.ResearcherId, Is.EqualTo(SC021FamilyResearchScenario.SpouseId));
            Assert.That(state.Progress, Is.EqualTo(1));
        }

        [Test]
        public void KnownTechnique_CannotStartAnotherPlan()
        {
            FamilyResearchState state = FamilyResearchState.CreateInitial(Start);
            state.TryStartOrReplacePlan(FamilyResearchCatalog.SmokeMixingResearchId,
                SC021FamilyResearchScenario.HeadId, out _);
            state.TryAdvance(2, out _);

            Assert.That(state.TryStartOrReplacePlan(FamilyResearchCatalog.SmokeMixingResearchId,
                SC021FamilyResearchScenario.SpouseId, out string error), Is.False);
            Assert.That(error, Does.Contain("已经记录为家族知识"));
            Assert.That(state.HasPlan, Is.False);
        }

        [Test]
        public void Clone_IsIndependent()
        {
            FamilyResearchState state = FamilyResearchState.CreateInitial(Start);
            state.TryStartOrReplacePlan(FamilyResearchCatalog.SmokeMixingResearchId,
                SC021FamilyResearchScenario.HeadId, out _);
            FamilyResearchState clone = state.Clone();

            Assert.That(clone.TryAdvance(2, out _), Is.True);
            Assert.That(state.Progress, Is.Zero);
            Assert.That(state.KnownTechniqueIds, Is.Empty);
            Assert.That(clone.KnownTechniqueIds, Has.Count.EqualTo(1));
        }

        [Test]
        public void Restore_RejectsBadProgressDuplicatesAndPlanKnowledgeConflict()
        {
            Assert.That(FamilyResearchState.TryRestore(FamilyResearchCatalog.SmokeMixingResearchId,
                SC021FamilyResearchScenario.HeadId.Value, -1, 0, new string[0], out _, out string error),
                Is.False);
            Assert.That(error, Does.Contain("进度"));

            Assert.That(FamilyResearchState.TryRestore(FamilyResearchCatalog.SmokeMixingResearchId,
                SC021FamilyResearchScenario.HeadId.Value, 2, 0, new string[0], out _, out error),
                Is.False);
            Assert.That(error, Does.Contain("0..1"));

            Assert.That(FamilyResearchState.TryRestore(string.Empty, string.Empty, 0, 0,
                new[] { FamilyResearchCatalog.SmokeMixingTechniqueId,
                    FamilyResearchCatalog.SmokeMixingTechniqueId }, out _, out error), Is.False);
            Assert.That(error, Does.Contain("重复"));

            Assert.That(FamilyResearchState.TryRestore(FamilyResearchCatalog.SmokeMixingResearchId,
                SC021FamilyResearchScenario.HeadId.Value, 1, 0,
                new[] { FamilyResearchCatalog.SmokeMixingTechniqueId }, out _, out error), Is.False);
            Assert.That(error, Does.Contain("冲突"));
        }
    }
}
