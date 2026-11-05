using System;
using NUnit.Framework;
using SengokuChronicle.Domain;

namespace SengokuChronicle.Tests.EditMode
{
    /// <summary>
    /// SC-024 Task 2: deterministic family creed and per-head ambition state, including
    /// the three settlement consumers, rejection paths, idempotence and deep cloning.
    /// </summary>
    public sealed class FamilyDoctrineStateTests
    {
        private static readonly PersonId HeadA = new PersonId("person.head.a");
        private static readonly PersonId HeadB = new PersonId("person.head.b");
        private static readonly PersonId HeirA = new PersonId("person.heir.a");
        private static readonly PersonId HeirB = new PersonId("person.heir.b");

        [Test]
        public void NewState_DefaultsToRighteousnessFirstAndHasNoAmbitions()
        {
            var state = new FamilyDoctrineState();

            Assert.That(state.Creed, Is.EqualTo(FamilyCreedKind.RighteousnessFirst));
            Assert.That(state.AmbitionCount, Is.Zero);
            Assert.That(state.HeadIds, Is.Empty);
            Assert.That(state.TryGetAmbition(HeadA, out FamilyAmbitionRecord record), Is.False);
            Assert.That(record, Is.Null);
        }

        [Test]
        public void TrySetCreed_AcceptsAllThreeAndRejectsUnknownValues()
        {
            var state = new FamilyDoctrineState();

            Assert.That(state.TrySetCreed(FamilyCreedKind.SecrecyFirst, out _), Is.True);
            Assert.That(state.Creed, Is.EqualTo(FamilyCreedKind.SecrecyFirst));
            Assert.That(state.TrySetCreed(FamilyCreedKind.MeritFirst, out _), Is.True);
            Assert.That(state.Creed, Is.EqualTo(FamilyCreedKind.MeritFirst));
            Assert.That(state.TrySetCreed(FamilyCreedKind.RighteousnessFirst, out _), Is.True);
            Assert.That(state.Creed, Is.EqualTo(FamilyCreedKind.RighteousnessFirst));

            Assert.That(state.TrySetCreed((FamilyCreedKind)99, out string error), Is.False);
            Assert.That(error, Does.Contain("未知家训"));
            Assert.That(state.Creed, Is.EqualTo(FamilyCreedKind.RighteousnessFirst));
        }

        [Test]
        public void TryStartAmbition_RejectsEmptyHeadUnknownKindAndDuplicateHead()
        {
            var state = new FamilyDoctrineState();

            Assert.That(state.TryStartAmbition(default, FamilyAmbitionKind.DevelopHousehold,
                Array.Empty<string>(), out string emptyError), Is.False);
            Assert.That(emptyError, Does.Contain("不能为空"));

            Assert.That(state.TryStartAmbition(HeadA, (FamilyAmbitionKind)99,
                Array.Empty<string>(), out string kindError), Is.False);
            Assert.That(kindError, Does.Contain("未知抱负"));

            Assert.That(state.TryStartAmbition(HeadA, FamilyAmbitionKind.DevelopHousehold,
                Array.Empty<string>(), out _), Is.True);
            Assert.That(state.TryStartAmbition(HeadA, FamilyAmbitionKind.EducateHeir,
                Array.Empty<string>(), out string duplicateError), Is.False);
            Assert.That(duplicateError, Does.Contain("已经选定抱负"));
            Assert.That(state.AmbitionCount, Is.EqualTo(1));
            Assert.That(state.TryGetAmbition(HeadA, out FamilyAmbitionRecord record), Is.True);
            Assert.That(record.Kind, Is.EqualTo(FamilyAmbitionKind.DevelopHousehold));
            Assert.That(record.Target, Is.EqualTo(FamilyDoctrineState.DevelopHouseholdTarget));
        }

        [Test]
        public void TryStartAmbition_DeduplicatesAndOrdinalSortsKnownTechniqueSnapshot()
        {
            var state = new FamilyDoctrineState();

            Assert.That(state.TryStartAmbition(HeadA, FamilyAmbitionKind.PassDownTechnique,
                new[] { "technique.z", string.Empty }, out string emptyError), Is.False);
            Assert.That(emptyError, Does.Contain("不能为空"));
            Assert.That(state.TryGetAmbition(HeadA, out _), Is.False);

            // Duplicate stable IDs are deduplicated, not rejected, and the snapshot is ordinal.
            Assert.That(state.TryStartAmbition(HeadA, FamilyAmbitionKind.PassDownTechnique,
                new[] { "technique.z", "technique.a", "technique.a" }, out _), Is.True);
            Assert.That(state.TryGetAmbition(HeadA, out FamilyAmbitionRecord record), Is.True);
            Assert.That(record.KnownTechniqueIds.Count, Is.EqualTo(2));
            Assert.That(record.KnownTechniqueIds, Is.EqualTo(new[] { "technique.a", "technique.z" }));
            Assert.That(record.Target, Is.EqualTo(FamilyDoctrineState.PassDownTechniqueTarget));
        }

        [Test]
        public void TryGetAmbition_RejectsEmptyAndUnknownHeads()
        {
            var state = new FamilyDoctrineState();
            state.TryStartAmbition(HeadA, FamilyAmbitionKind.DevelopHousehold, Array.Empty<string>(), out _);

            Assert.That(state.TryGetAmbition(default, out FamilyAmbitionRecord emptyRecord), Is.False);
            Assert.That(emptyRecord, Is.Null);
            Assert.That(state.TryGetAmbition(HeadB, out FamilyAmbitionRecord unknownRecord), Is.False);
            Assert.That(unknownRecord, Is.Null);
        }

        [Test]
        public void HouseholdAmbition_AdvancesForQualifyingMonthsAndCompletesExactlyOnce()
        {
            var state = new FamilyDoctrineState();
            state.TryStartAmbition(HeadA, FamilyAmbitionKind.DevelopHousehold, Array.Empty<string>(), out _);

            Assert.That(state.TryAdvanceHouseholdAmbition(HeadA, Month(1560, 1, Line(IndustryLineStatus.Operated, 1, 0, 0)), out _), Is.True);
            Assert.That(ProgressOf(state, HeadA), Is.EqualTo(1));
            Assert.That(state.TryAdvanceHouseholdAmbition(HeadA, Month(1560, 2, Line(IndustryLineStatus.Operated, 0, 1, 0)), out _), Is.True);
            Assert.That(ProgressOf(state, HeadA), Is.EqualTo(2));
            Assert.That(state.TryAdvanceHouseholdAmbition(HeadA, Month(1560, 3, Line(IndustryLineStatus.Operated, 0, 0, 1)), out _), Is.True);
            Assert.That(ProgressOf(state, HeadA), Is.EqualTo(3));
            Assert.That(AmbitionOf(state, HeadA).IsComplete, Is.True);

            Assert.That(state.TryAdvanceHouseholdAmbition(HeadA, Month(1560, 4, Line(IndustryLineStatus.Operated, 5, 5, 5)), out _), Is.True);
            Assert.That(ProgressOf(state, HeadA), Is.EqualTo(3));
        }

        [Test]
        public void HouseholdAmbition_IgnoresNonOperatedZeroAndNegativeReports()
        {
            var state = new FamilyDoctrineState();
            state.TryStartAmbition(HeadA, FamilyAmbitionKind.DevelopHousehold, Array.Empty<string>(), out _);

            Assert.That(state.TryAdvanceHouseholdAmbition(HeadA, Month(1560, 1,
                Line(IndustryLineStatus.Stalled, 5, 5, 5)), out _), Is.True);
            Assert.That(state.TryAdvanceHouseholdAmbition(HeadA, Month(1560, 2,
                Line(IndustryLineStatus.Vacant, 5, 5, 5)), out _), Is.True);
            Assert.That(state.TryAdvanceHouseholdAmbition(HeadA, Month(1560, 3,
                Line(IndustryLineStatus.InvalidAssignment, 5, 5, 5)), out _), Is.True);
            Assert.That(state.TryAdvanceHouseholdAmbition(HeadA, Month(1560, 4,
                Line(IndustryLineStatus.Operated, 2, -1, -1)), out _), Is.True);
            Assert.That(state.TryAdvanceHouseholdAmbition(HeadA, Month(1560, 5,
                Line(IndustryLineStatus.Operated, -2, 1, 1)), out _), Is.True);

            // Month 1 was already evaluated as non-qualifying; replaying it cannot count.
            Assert.That(state.TryAdvanceHouseholdAmbition(HeadA, Month(1560, 1,
                Line(IndustryLineStatus.Operated, 5, 5, 5)), out _), Is.True);
            Assert.That(ProgressOf(state, HeadA), Is.Zero);
            Assert.That(state.TryAdvanceHouseholdAmbition(HeadA, null, out string error), Is.False);
            Assert.That(error, Does.Contain("不能为空"));
        }

        [Test]
        public void HouseholdAmbition_RejectsShortageEvenWhenOperatedNetIsPositive()
        {
            var state = new FamilyDoctrineState();
            state.TryStartAmbition(HeadA, FamilyAmbitionKind.DevelopHousehold, Array.Empty<string>(), out _);

            Assert.That(state.TryAdvanceHouseholdAmbition(HeadA, MonthWithShortage(1560, 1, true,
                Line(IndustryLineStatus.Operated, 5, 5, 5)), out _), Is.True);
            Assert.That(ProgressOf(state, HeadA), Is.Zero);

            // The shortage month was consumed; a later replay cannot become progress.
            Assert.That(state.TryAdvanceHouseholdAmbition(HeadA, Month(1560, 1,
                Line(IndustryLineStatus.Operated, 5, 5, 5)), out _), Is.True);
            Assert.That(ProgressOf(state, HeadA), Is.Zero);
        }

        [Test]
        public void HouseholdAmbition_RequiresPositiveAggregateAcrossOperatedLines()
        {
            var state = new FamilyDoctrineState();
            state.TryStartAmbition(HeadA, FamilyAmbitionKind.DevelopHousehold, Array.Empty<string>(), out _);

            // One positive line must not mask a negative operated line when the total is zero.
            Assert.That(state.TryAdvanceHouseholdAmbition(HeadA, Month(1560, 1,
                Line(IndustryLineStatus.Operated, 5, 0, 0),
                Line(IndustryLineStatus.Operated, -5, 0, 0)), out _), Is.True);
            Assert.That(ProgressOf(state, HeadA), Is.Zero);

            // A combined negative total also fails even though one line is positive.
            Assert.That(state.TryAdvanceHouseholdAmbition(HeadA, Month(1560, 2,
                Line(IndustryLineStatus.Operated, 5, 0, 0),
                Line(IndustryLineStatus.Operated, -6, 0, 0)), out _), Is.True);
            Assert.That(ProgressOf(state, HeadA), Is.Zero);

            // Combined positive across different resource types qualifies.
            Assert.That(state.TryAdvanceHouseholdAmbition(HeadA, Month(1560, 3,
                Line(IndustryLineStatus.Operated, 2, 0, 0),
                Line(IndustryLineStatus.Operated, 0, -1, 0)), out _), Is.True);
            Assert.That(ProgressOf(state, HeadA), Is.EqualTo(1));
        }

        [Test]
        public void HouseholdAmbition_RepeatedMonthDoesNotCountTwice()
        {
            var state = new FamilyDoctrineState();
            state.TryStartAmbition(HeadA, FamilyAmbitionKind.DevelopHousehold, Array.Empty<string>(), out _);
            IndustryMonthReport report = Month(1560, 1, Line(IndustryLineStatus.Operated, 2, 0, 0));

            Assert.That(state.TryAdvanceHouseholdAmbition(HeadA, report, out _), Is.True);
            Assert.That(state.TryAdvanceHouseholdAmbition(HeadA, report, out _), Is.True);
            Assert.That(ProgressOf(state, HeadA), Is.EqualTo(1));
        }

        [Test]
        public void TechniqueAmbition_OnlyNewlyCompletedIdsCountAndCompletesOnce()
        {
            var state = new FamilyDoctrineState();
            state.TryStartAmbition(HeadA, FamilyAmbitionKind.PassDownTechnique,
                new[] { "technique.known" }, out _);

            Assert.That(state.TryAdvanceTechniqueAmbition(HeadA, new[] { "technique.known" }, out _), Is.True);
            Assert.That(ProgressOf(state, HeadA), Is.Zero);

            Assert.That(state.TryAdvanceTechniqueAmbition(HeadA, new[] { "technique.new" }, out _), Is.True);
            Assert.That(ProgressOf(state, HeadA), Is.EqualTo(1));
            Assert.That(AmbitionOf(state, HeadA).IsComplete, Is.True);

            Assert.That(state.TryAdvanceTechniqueAmbition(HeadA, new[] { "technique.other" }, out _), Is.True);
            Assert.That(ProgressOf(state, HeadA), Is.EqualTo(1));
        }

        [Test]
        public void TechniqueAmbition_RejectsNullAndEmptyIdsAndMismatchedKind()
        {
            var state = new FamilyDoctrineState();
            state.TryStartAmbition(HeadA, FamilyAmbitionKind.PassDownTechnique,
                new[] { "technique.known" }, out _);

            Assert.That(state.TryAdvanceTechniqueAmbition(HeadA, null, out string nullError), Is.False);
            Assert.That(nullError, Does.Contain("不能为空"));
            Assert.That(state.TryAdvanceTechniqueAmbition(HeadA, new[] { " " }, out string emptyError), Is.False);
            Assert.That(emptyError, Does.Contain("不能为空"));
            Assert.That(ProgressOf(state, HeadA), Is.Zero);

            var household = new FamilyDoctrineState();
            household.TryStartAmbition(HeadB, FamilyAmbitionKind.DevelopHousehold, Array.Empty<string>(), out _);
            Assert.That(household.TryAdvanceTechniqueAmbition(HeadB, new[] { "technique.new" }, out string mismatchError), Is.False);
            Assert.That(mismatchError, Does.Contain("类型不匹配"));
        }

        [Test]
        public void EducationAmbition_SumsPositiveChangesToTargetAndIsIdempotent()
        {
            var state = new FamilyDoctrineState();
            state.TryStartAmbition(HeadA, FamilyAmbitionKind.EducateHeir, Array.Empty<string>(), out _);
            EducationYearResult first = Education(2, 1, 0, 2, 0);

            Assert.That(state.TryAdvanceHeirEducationAmbition(HeadA, HeirA, 8, first, out _), Is.True);
            Assert.That(ProgressOf(state, HeadA), Is.EqualTo(5));
            Assert.That(AmbitionOf(state, HeadA).IsComplete, Is.True);

            Assert.That(state.TryAdvanceHeirEducationAmbition(HeadA, HeirA, 8, Education(5, 5, 5, 5, 5), out _), Is.True);
            Assert.That(state.TryAdvanceHeirEducationAmbition(HeadA, HeirA, 9, Education(5, 5, 5, 5, 5), out _), Is.True);
            Assert.That(ProgressOf(state, HeadA), Is.EqualTo(5));
        }

        [Test]
        public void EducationAmbition_RepeatedAgeYearCannotCountTwice()
        {
            var state = new FamilyDoctrineState();
            state.TryStartAmbition(HeadA, FamilyAmbitionKind.EducateHeir, Array.Empty<string>(), out _);
            EducationYearResult result = Education(1, 1, 0, 0, 0);

            Assert.That(state.TryAdvanceHeirEducationAmbition(HeadA, HeirA, 7, result, out _), Is.True);
            Assert.That(ProgressOf(state, HeadA), Is.EqualTo(2));
            Assert.That(state.TryAdvanceHeirEducationAmbition(HeadA, HeirA, 7, result, out _), Is.True);
            Assert.That(ProgressOf(state, HeadA), Is.EqualTo(2));

            Assert.That(state.TryAdvanceHeirEducationAmbition(HeadA, HeirA, 8, result, out _), Is.True);
            Assert.That(ProgressOf(state, HeadA), Is.EqualTo(4));
        }

        [Test]
        public void EducationAmbition_FreeGrowthFallbackNeverAdvancesEvenWithPositiveDeltas()
        {
            var state = new FamilyDoctrineState();
            state.TryStartAmbition(HeadA, FamilyAmbitionKind.EducateHeir, Array.Empty<string>(), out _);
            EducationYearResult fallback = FallbackEducation(3, 2, 1, 1, 1);

            Assert.That(state.TryAdvanceHeirEducationAmbition(HeadA, HeirA, 6, fallback, out _), Is.True);
            Assert.That(ProgressOf(state, HeadA), Is.Zero);

            // The fallback year is consumed deterministically; replay remains a no-op.
            Assert.That(state.TryAdvanceHeirEducationAmbition(HeadA, HeirA, 6, fallback, out _), Is.True);
            Assert.That(ProgressOf(state, HeadA), Is.Zero);

            // A normal directed result on a new age year still counts.
            Assert.That(state.TryAdvanceHeirEducationAmbition(HeadA, HeirA, 7,
                Education(2, 1, 0, 0, 0), out _), Is.True);
            Assert.That(ProgressOf(state, HeadA), Is.EqualTo(3));
        }

        [Test]
        public void EducationAmbition_IgnoresNonPositiveChangesAndDifferentHeirCountsSeparately()
        {
            var state = new FamilyDoctrineState();
            state.TryStartAmbition(HeadA, FamilyAmbitionKind.EducateHeir, Array.Empty<string>(), out _);

            EducationYearResult loss = Education(-2, -1, 0, 0, 0);
            Assert.That(state.TryAdvanceHeirEducationAmbition(HeadA, HeirA, 6, loss, out _), Is.True);
            Assert.That(ProgressOf(state, HeadA), Is.Zero);

            // Age year 6 was already consumed; another result for that year cannot count.
            Assert.That(state.TryAdvanceHeirEducationAmbition(HeadA, HeirA, 6, Education(3, 0, 0, 0, 0), out _), Is.True);
            Assert.That(ProgressOf(state, HeadA), Is.Zero);

            Assert.That(state.TryAdvanceHeirEducationAmbition(HeadA, HeirA, 7, Education(2, 1, 0, 0, 0), out _), Is.True);
            Assert.That(ProgressOf(state, HeadA), Is.EqualTo(3));

            Assert.That(state.TryAdvanceHeirEducationAmbition(default, HeirA, 8, Education(2, 0, 0, 0, 0), out string emptyError), Is.False);
            Assert.That(emptyError, Does.Contain("不能为空"));

            Assert.That(state.TryAdvanceHeirEducationAmbition(HeadA, default, 8, Education(2, 0, 0, 0, 0), out string heirError), Is.False);
            Assert.That(heirError, Does.Contain("继承人"));
            Assert.That(state.TryAdvanceHeirEducationAmbition(HeadA, HeirA, 16, Education(2, 0, 0, 0, 0), out string ageError), Is.False);
            Assert.That(ageError, Does.Contain("0..15"));

            Assert.That(state.TryAdvanceHeirEducationAmbition(HeadA, HeirB, 7, Education(2, 0, 0, 0, 0), out _), Is.True);
            Assert.That(ProgressOf(state, HeadA), Is.EqualTo(5));
        }

        [Test]
        public void ProgressMethods_RejectMissingOrMismatchedAmbitions()
        {
            var education = new FamilyDoctrineState();
            Assert.That(education.TryAdvanceHeirEducationAmbition(HeadA, HeirA, 1,
                Education(1, 0, 0, 0, 0), out string noneError), Is.False);
            Assert.That(noneError, Does.Contain("尚未选定抱负"));

            education.TryStartAmbition(HeadA, FamilyAmbitionKind.EducateHeir, Array.Empty<string>(), out _);
            Assert.That(education.TryAdvanceHouseholdAmbition(HeadA, Month(1560, 1,
                Line(IndustryLineStatus.Operated, 1, 1, 1)), out string mismatchError), Is.False);
            Assert.That(mismatchError, Does.Contain("类型不匹配"));
        }

        [Test]
        public void StartingSecondHead_KeepsOldHeadRecordIndependent()
        {
            var state = new FamilyDoctrineState();
            state.TryStartAmbition(HeadA, FamilyAmbitionKind.PassDownTechnique,
                new[] { "technique.known" }, out _);
            state.TryAdvanceTechniqueAmbition(HeadA, new[] { "technique.new" }, out _);

            Assert.That(state.TryStartAmbition(HeadB, FamilyAmbitionKind.DevelopHousehold,
                Array.Empty<string>(), out _), Is.True);
            Assert.That(state.AmbitionCount, Is.EqualTo(2));
            Assert.That(state.HeadIds, Is.EqualTo(new[] { HeadA, HeadB }));
            Assert.That(AmbitionOf(state, HeadA).Kind, Is.EqualTo(FamilyAmbitionKind.PassDownTechnique));
            Assert.That(AmbitionOf(state, HeadA).Progress, Is.EqualTo(1));
            Assert.That(AmbitionOf(state, HeadB).Progress, Is.Zero);

            state.TryAdvanceHouseholdAmbition(HeadB, Month(1560, 1,
                Line(IndustryLineStatus.Operated, 1, 0, 0)), out _);
            Assert.That(AmbitionOf(state, HeadA).Progress, Is.EqualTo(1));
            Assert.That(AmbitionOf(state, HeadB).Progress, Is.EqualTo(1));
        }

        [Test]
        public void Clone_PreservesAppliedIndustryMonthKeyAndStaysIndependent()
        {
            var state = new FamilyDoctrineState();
            state.TryStartAmbition(HeadA, FamilyAmbitionKind.DevelopHousehold, Array.Empty<string>(), out _);
            IndustryMonthReport report = Month(1560, 1, Line(IndustryLineStatus.Operated, 1, 0, 0));
            state.TryAdvanceHouseholdAmbition(HeadA, report, out _);
            Assert.That(ProgressOf(state, HeadA), Is.EqualTo(1));

            FamilyDoctrineState clone = state.Clone();

            // Replaying the consumed month on the clone must not add progress.
            Assert.That(clone.TryAdvanceHouseholdAmbition(HeadA, report, out _), Is.True);
            Assert.That(ProgressOf(clone, HeadA), Is.EqualTo(1));

            // A new month advances only the clone; the source stays independent.
            Assert.That(clone.TryAdvanceHouseholdAmbition(HeadA, Month(1560, 2,
                Line(IndustryLineStatus.Operated, 1, 0, 0)), out _), Is.True);
            Assert.That(ProgressOf(clone, HeadA), Is.EqualTo(2));
            Assert.That(ProgressOf(state, HeadA), Is.EqualTo(1));
        }

        [Test]
        public void Clone_DeepCopiesCreedRecordsSnapshotsAndAppliedKeys()
        {
            var state = new FamilyDoctrineState();
            state.TrySetCreed(FamilyCreedKind.SecrecyFirst, out _);
            state.TryStartAmbition(HeadA, FamilyAmbitionKind.PassDownTechnique,
                new[] { "technique.z", "technique.a" }, out _);
            state.TryStartAmbition(HeadB, FamilyAmbitionKind.DevelopHousehold, Array.Empty<string>(), out _);
            state.TryAdvanceTechniqueAmbition(HeadA, new[] { "technique.a" }, out _);

            FamilyDoctrineState clone = state.Clone();

            Assert.That(clone.Creed, Is.EqualTo(FamilyCreedKind.SecrecyFirst));
            Assert.That(clone.AmbitionCount, Is.EqualTo(2));
            Assert.That(clone.TryGetAmbition(HeadA, out FamilyAmbitionRecord clonedA), Is.True);
            Assert.That(state.TryGetAmbition(HeadA, out FamilyAmbitionRecord originalA), Is.True);
            Assert.That(ReferenceEquals(originalA, clonedA), Is.False);
            Assert.That(ReferenceEquals(originalA.KnownTechniqueIds, clonedA.KnownTechniqueIds), Is.False);
            Assert.That(clonedA.KnownTechniqueIds, Is.EqualTo(new[] { "technique.a", "technique.z" }));

            Assert.That(clone.TrySetCreed(FamilyCreedKind.MeritFirst, out _), Is.True);
            Assert.That(clone.TryStartAmbition(new PersonId("person.head.c"),
                FamilyAmbitionKind.EducateHeir, Array.Empty<string>(), out _), Is.True);
            clone.TryAdvanceHouseholdAmbition(HeadB, Month(1560, 1,
                Line(IndustryLineStatus.Operated, 1, 0, 0)), out _);

            Assert.That(state.Creed, Is.EqualTo(FamilyCreedKind.SecrecyFirst));
            Assert.That(state.AmbitionCount, Is.EqualTo(2));
            Assert.That(AmbitionOf(state, HeadB).Progress, Is.Zero);
            Assert.That(AmbitionOf(clone, HeadB).Progress, Is.EqualTo(1));
        }

        private static FamilyAmbitionRecord AmbitionOf(FamilyDoctrineState state, PersonId headId)
        {
            Assert.That(state.TryGetAmbition(headId, out FamilyAmbitionRecord record), Is.True);
            return record;
        }

        private static int ProgressOf(FamilyDoctrineState state, PersonId headId) =>
            AmbitionOf(state, headId).Progress;

        private static IndustryMonthReport Month(int year, int month, params IndustryLineResult[] lines) =>
            MonthWithShortage(year, month, false, lines);

        private static IndustryMonthReport MonthWithShortage(int year, int month, bool hasShortage,
            params IndustryLineResult[] lines)
        {
            IndustryResources.TryCreate(10, 10, 10, out IndustryResources resources, out _);
            return new IndustryMonthReport(year, month, resources, resources, lines,
                Array.Empty<IndustryVacancy>(), hasShortage, true, false, "test");
        }

        private static IndustryLineResult Line(IndustryLineStatus status, int netFunds,
            int netSupplies, int netIntelligence) =>
            new IndustryLineResult("industry.test", "Test Industry", new PersonId("person.worker"),
                "Worker", status, string.Empty, 0, 0, 0, 0, 0,
                netFunds, netSupplies, netIntelligence,
                netFunds, netSupplies, netIntelligence, false);

        private static EducationYearResult Education(int ninjutsu, int martialArts, int scholarship,
            int management, int social)
        {
            CharacterGrowthValues.TryCreate(10, 10, 10, 10, 10,
                out CharacterGrowthValues before, out _);
            CharacterGrowthValues after = CharacterGrowthValues.CreateClamped(
                before.Ninjutsu + ninjutsu, before.MartialArts + martialArts,
                before.Scholarship + scholarship, before.Management + management,
                before.Social + social);
            return new EducationYearResult(EducationDirection.Ninjutsu,
                new PersonId("person.mentor"), before, after, "test", false);
        }

        private static EducationYearResult FallbackEducation(int ninjutsu, int martialArts,
            int scholarship, int management, int social)
        {
            CharacterGrowthValues.TryCreate(10, 10, 10, 10, 10,
                out CharacterGrowthValues before, out _);
            CharacterGrowthValues after = CharacterGrowthValues.CreateClamped(
                before.Ninjutsu + ninjutsu, before.MartialArts + martialArts,
                before.Scholarship + scholarship, before.Management + management,
                before.Social + social);
            return new EducationYearResult(EducationDirection.FreeGrowth, default,
                before, after, "fallback test", true);
        }
    }
}
