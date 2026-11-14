using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;
using SengokuChronicle.Events;
using SengokuChronicle.Persistence;
using SengokuChronicle.Presentation;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SengokuChronicle.Tests.EditMode
{
    /// <summary>
    /// M08 campaign review coverage: exact 55/100-year boundaries, one-time
    /// acknowledgement, continuation after the preliminary review, save/load at both
    /// reviews, unknown legacy origin, every GDD 10.3 category, and unchanged Phase 1
    /// demo completion. All dates advance through the internal fixture seam so no Unity
    /// Play Mode is required.
    /// </summary>
    public sealed class CampaignMilestoneTests
    {
        private static readonly GameDate StartDate = GameDate.Parse(1560, 4, 1);
        private static readonly GameDate StageReviewDate = GameDate.Parse(1615, 4, 1);
        private static readonly GameDate CenturyReviewDate = GameDate.Parse(1660, 4, 1);

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
                Object.DestroyImmediate(_session.gameObject);
        }

        private static int DaysBetween(GameDate from, GameDate to)
        {
            int days = 0;
            GameDate cursor = from;
            while (cursor < to && GameDate.TryNext(cursor, out GameDate next))
            {
                cursor = next;
                days++;
            }
            return days;
        }

        private void AdvanceTo(GameDate target)
        {
            int days = DaysBetween(_session.WorldClock.CurrentDate, target);
            Assert.That(days, Is.GreaterThanOrEqualTo(0));
            if (days == 0)
                return;
            Assert.That(_session.AdvanceDaysForLegacyFixtureSetup(days), Is.True);
            Assert.That(_session.WorldClock.CurrentDate, Is.EqualTo(target));
        }

        [Test]
        public void CampaignOriginCatalog_MatchesEveryAuthoredScenario()
        {
            Assert.That(CampaignOriginCatalog.All, Has.Count.EqualTo(7));

            Assert.That(CampaignOriginCatalog.Sc010FamilyId,
                Is.EqualTo(SuccessionSample.StandardFamilyId.Value));
            Assert.That(CampaignOriginCatalog.Sc013FamilyId,
                Is.EqualTo(M2FixedScenario.FamilyIdValue));
            Assert.That(CampaignOriginCatalog.Sc020FamilyId,
                Is.EqualTo(SC020EducationScenario.FamilyIdValue));
            Assert.That(CampaignOriginCatalog.Sc021FamilyId,
                Is.EqualTo(SC021FamilyResearchScenario.FamilyIdValue));
            Assert.That(CampaignOriginCatalog.Sc022FamilyId,
                Is.EqualTo(SC022FamilyFormationScenario.FamilyIdValue));
            Assert.That(CampaignOriginCatalog.Sc023FamilyId,
                Is.EqualTo(SC023SeclusionScenario.FamilyIdValue));

            Assert.That(CampaignOriginCatalog.Sc010StartDate,
                Is.EqualTo(WorldClock.NewGameDate));
            Assert.That(CampaignOriginCatalog.Sc013StartDate,
                Is.EqualTo(WorldClock.NewGameDate));
            Assert.That(CampaignOriginCatalog.Sc020StartDate,
                Is.EqualTo(SC020EducationScenario.StartDate));
            Assert.That(CampaignOriginCatalog.Sc021StartDate,
                Is.EqualTo(SC021FamilyResearchScenario.StartDate));
            Assert.That(CampaignOriginCatalog.Sc022StartDate,
                Is.EqualTo(SC022FamilyFormationScenario.StartDate));
            Assert.That(CampaignOriginCatalog.Sc023StartDate,
                Is.EqualTo(SC023SeclusionScenario.StartDate));
            Assert.That(CampaignOriginCatalog.Sc024EventScenarioId,
                Is.EqualTo(SC024FamilyDoctrineScenario.ScenarioId));
        }

        [Test]
        public void NewCampaign_RecordsAuthoredStartDate()
        {
            Assert.That(_session.CampaignStartKnown, Is.True);
            Assert.That(_session.CampaignStartDate, Is.EqualTo(StartDate));
            Assert.That(_session.CampaignOriginId, Is.EqualTo(CampaignOriginCatalog.Sc013OriginId));
            Assert.That(_session.CampaignCompletedYears, Is.Zero);
            Assert.That(_session.PendingCampaignReview, Is.EqualTo(CampaignReviewMilestone.None));
            Assert.That(_session.CampaignOriginSummary, Does.Contain("SC-013"));
        }

        [Test]
        public void StageReview_IsDueExactlyAtTheFiftyFifthAnniversary()
        {
            AdvanceTo(GameDate.Parse(1615, 3, 31));
            Assert.That(_session.PendingCampaignReview, Is.EqualTo(CampaignReviewMilestone.None));

            AdvanceTo(StageReviewDate);
            Assert.That(_session.CampaignCompletedYears, Is.EqualTo(55));
            Assert.That(_session.PendingCampaignReview, Is.EqualTo(CampaignReviewMilestone.StageReview));

            CampaignReviewView review = _session.CampaignReview;
            Assert.That(review.IsAvailable, Is.True);
            Assert.That(review.Milestone, Is.EqualTo(CampaignReviewMilestone.StageReview));
            Assert.That(review.Title, Does.Contain("阶段评价"));
            Assert.That(review.ContinueLabel, Does.Contain("继续家族"));
        }

        [Test]
        public void StageReview_AcknowledgementIsOneTimeAndPersistsAcrossSaveAndLoad()
        {
            AdvanceTo(StageReviewDate);

            CampaignReviewResult acknowledged = _session.AcknowledgeCampaignReview();
            Assert.That(acknowledged.Succeeded, Is.True, acknowledged.Message);
            Assert.That(_session.CampaignState.StageReviewAcknowledged, Is.True);
            Assert.That(_session.CampaignState.CenturyReviewAcknowledged, Is.False);
            Assert.That(_session.PendingCampaignReview, Is.EqualTo(CampaignReviewMilestone.None));

            CampaignReviewResult repeated = _session.AcknowledgeCampaignReview();
            Assert.That(repeated.Succeeded, Is.False);
            Assert.That(_session.PendingCampaignReview, Is.EqualTo(CampaignReviewMilestone.None));

            SaveData saved = _session.CaptureSave();
            Assert.That(saved.campaign.startKnown, Is.True);
            Assert.That(saved.campaign.stageReviewAcknowledged, Is.True);

            GameSession reloaded = GameSession.NewGame();
            try
            {
                Assert.That(reloaded.TryApplySave(saved).Succeeded, Is.True,
                    reloaded.LastEventMessage);
                Assert.That(reloaded.WorldClock.CurrentDate, Is.EqualTo(StageReviewDate));
                Assert.That(reloaded.CampaignState.StageReviewAcknowledged, Is.True);
                Assert.That(reloaded.PendingCampaignReview,
                    Is.EqualTo(CampaignReviewMilestone.None));
            }
            finally
            {
                Object.DestroyImmediate(reloaded.gameObject);
            }
        }

        [Test]
        public void PendingStageReview_AlsoPersistsBeforeAcknowledgement()
        {
            AdvanceTo(StageReviewDate);
            SaveData saved = _session.CaptureSave();

            GameSession reloaded = GameSession.NewGame();
            try
            {
                Assert.That(reloaded.TryApplySave(saved).Succeeded, Is.True,
                    reloaded.LastEventMessage);
                Assert.That(reloaded.PendingCampaignReview,
                    Is.EqualTo(CampaignReviewMilestone.StageReview));
                Assert.That(reloaded.CampaignState.StageReviewAcknowledged, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(reloaded.gameObject);
            }
        }

        [Test]
        public void StageReview_IsNonTerminalAndPhaseOneCompletionStillNeedsAllThreeMissions()
        {
            AdvanceTo(StageReviewDate);
            Assert.That(_session.IsDemoComplete, Is.False);

            Assert.That(_session.AcknowledgeCampaignReview().Succeeded, Is.True);
            Assert.That(_session.PendingCampaignReview, Is.EqualTo(CampaignReviewMilestone.None));
            Assert.That(_session.IsDemoComplete, Is.False,
                "the stage review must not be confused with Phase 1 completion");

            Assert.That(_session.AdvanceDaysForLegacyFixtureSetup(1), Is.True,
                "the campaign must continue after the preliminary review");
            Assert.That(_session.WorldClock.CurrentDate, Is.EqualTo(GameDate.Parse(1615, 4, 2)));
            Assert.That(_session.IsDemoComplete, Is.False);
        }

        [Test]
        public void CenturyReview_IsDueExactlyAtTheHundredthAnniversary()
        {
            AdvanceTo(StageReviewDate);
            Assert.That(_session.AcknowledgeCampaignReview().Succeeded, Is.True);

            AdvanceTo(GameDate.Parse(1660, 3, 31));
            Assert.That(_session.PendingCampaignReview, Is.EqualTo(CampaignReviewMilestone.None));

            AdvanceTo(CenturyReviewDate);
            Assert.That(_session.CampaignCompletedYears, Is.EqualTo(100));
            Assert.That(_session.PendingCampaignReview,
                Is.EqualTo(CampaignReviewMilestone.CenturyReview));
            CampaignReviewView review = _session.CampaignReview;
            Assert.That(review.Title, Does.Contain("百年名门"));
            Assert.That(review.ContinueLabel, Does.Contain("确认"));

            SaveData pendingSave = _session.CaptureSave();
            GameSession pendingReload = GameSession.NewGame();
            try
            {
                Assert.That(pendingReload.TryApplySave(pendingSave).Succeeded, Is.True,
                    pendingReload.LastEventMessage);
                Assert.That(pendingReload.PendingCampaignReview,
                    Is.EqualTo(CampaignReviewMilestone.CenturyReview));
                Assert.That(pendingReload.CampaignState.StageReviewAcknowledged, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(pendingReload.gameObject);
            }

            CampaignReviewResult acknowledged = _session.AcknowledgeCampaignReview();
            Assert.That(acknowledged.Succeeded, Is.True, acknowledged.Message);
            Assert.That(_session.CampaignState.CenturyReviewAcknowledged, Is.True);
            Assert.That(_session.PendingCampaignReview, Is.EqualTo(CampaignReviewMilestone.None));
            Assert.That(_session.AcknowledgeCampaignReview().Succeeded, Is.False);

            SaveData saved = _session.CaptureSave();
            GameSession reloaded = GameSession.NewGame();
            try
            {
                Assert.That(reloaded.TryApplySave(saved).Succeeded, Is.True,
                    reloaded.LastEventMessage);
                Assert.That(reloaded.CampaignState.CenturyReviewAcknowledged, Is.True);
                Assert.That(reloaded.CampaignState.StageReviewAcknowledged, Is.True);
                Assert.That(reloaded.PendingCampaignReview,
                    Is.EqualTo(CampaignReviewMilestone.None));
            }
            finally
            {
                Object.DestroyImmediate(reloaded.gameObject);
            }
        }

        [Test]
        public void UnknownLegacyOrigin_NeverClaimsEitherMilestone()
        {
            _session.SetCampaignProgressForTest(CampaignProgress.CreateUnknownOrigin());
            Assert.That(_session.CampaignStartKnown, Is.False);
            Assert.That(_session.CampaignStartDate.IsValid, Is.False);
            Assert.That(_session.PendingCampaignReview, Is.EqualTo(CampaignReviewMilestone.None));

            AdvanceTo(GameDate.Parse(1700, 4, 1));
            Assert.That(_session.PendingCampaignReview, Is.EqualTo(CampaignReviewMilestone.None));
            Assert.That(_session.CampaignOriginSummary, Does.Contain("未知"));
            Assert.That(_session.AcknowledgeCampaignReview().Succeeded, Is.False);

            SaveData saved = _session.CaptureSave();
            Assert.That(saved.campaign.startKnown, Is.False);
            Assert.That(saved.campaign.originId, Is.Empty);
            Assert.That(saved.campaign.stageReviewAcknowledged, Is.False);
            Assert.That(saved.campaign.centuryReviewAcknowledged, Is.False);
        }

        [Test]
        public void LegacyOriginInference_RequiresAuthoredScenarioAndConsistentDate()
        {
            Assert.That(CampaignOriginCatalog.TryInferLegacyOrigin(
                CampaignOriginCatalog.Sc013FamilyId, string.Empty,
                GameDate.Parse(1620, 1, 1), out CampaignOrigin sc013), Is.True);
            Assert.That(sc013.Id, Is.EqualTo(CampaignOriginCatalog.Sc013OriginId));
            Assert.That(sc013.StartDate, Is.EqualTo(StartDate));

            Assert.That(CampaignOriginCatalog.TryInferLegacyOrigin(
                CampaignOriginCatalog.Sc013FamilyId, CampaignOriginCatalog.Sc024EventScenarioId,
                GameDate.Parse(1620, 1, 1), out CampaignOrigin sc024), Is.True);
            Assert.That(sc024.Id, Is.EqualTo(CampaignOriginCatalog.Sc024OriginId));

            // A date before the authored start contradicts the identity.
            Assert.That(CampaignOriginCatalog.TryInferLegacyOrigin(
                CampaignOriginCatalog.Sc022FamilyId, string.Empty,
                GameDate.Parse(1560, 4, 1), out _), Is.False);
            // An unknown family never proves an origin.
            Assert.That(CampaignOriginCatalog.TryInferLegacyOrigin(
                "family.unknown.test", string.Empty,
                GameDate.Parse(1620, 1, 1), out _), Is.False);
        }

        [Test]
        public void MigratedV15Save_InfersAuthoredOriginButNeverClaimsAReview()
        {
            SaveData legacy = _session.CaptureSave();
            legacy.saveFormatVersion = 15;
            legacy.rulesVersion = 8;
            legacy.campaign = null;

            Assert.That(SaveDataMigrator.TryMigrate(legacy, null, out SaveData current,
                out SaveOperationResult result), Is.True, result.Message);
            Assert.That(current.saveFormatVersion, Is.EqualTo(SaveDataValidator.CurrentVersion));
            Assert.That(current.campaign.startKnown, Is.True);
            Assert.That(current.campaign.originId, Is.EqualTo(CampaignOriginCatalog.Sc013OriginId));
            Assert.That(current.campaign.startYear, Is.EqualTo(1560));
            Assert.That(current.campaign.startMonth, Is.EqualTo(4));
            Assert.That(current.campaign.startDay, Is.EqualTo(1));
            Assert.That(current.campaign.stageReviewAcknowledged, Is.False);
            Assert.That(current.campaign.centuryReviewAcknowledged, Is.False);
        }

        [Test]
        public void MigratedV15Save_WithUnknownFamilyKeepsExplicitUnknownOrigin()
        {
            SaveData legacy = _session.CaptureSave();
            legacy.saveFormatVersion = 15;
            legacy.rulesVersion = 8;
            legacy.campaign = null;
            legacy.succession.familyId = "family.unknown.test";

            Assert.That(SaveDataMigrator.TryMigrate(legacy, null, out SaveData current,
                out SaveOperationResult result), Is.True, result.Message);
            Assert.That(current.campaign.startKnown, Is.False);
            Assert.That(current.campaign.originId, Is.Empty);
            Assert.That(current.campaign.stageReviewAcknowledged, Is.False);
            Assert.That(current.campaign.centuryReviewAcknowledged, Is.False);
            Assert.That(current.chronicle, Is.Not.Null,
                "migration must not manufacture historical events");
        }

        [Test]
        public void LegacyV1Migration_DoesNotInferOriginFromDefaultedSuccession()
        {
            var legacy = new SaveData
            {
                saveFormatVersion = 1,
                year = 1560,
                month = 4,
                day = 1,
                locationId = LocationId.Kiyosu.Value,
                supplyShortageStage = 0,
                sealedLetterStage = 0,
                marketTallyStage = 0,
                relationships = new RelationshipSaveEntry[0]
            };

            Assert.That(SaveDataMigrator.TryMigrate(legacy, null, out SaveData current,
                out SaveOperationResult result), Is.True, result.Message);
            Assert.That(current.campaign.startKnown, Is.False,
                "a defaulted legacy roster cannot prove the campaign origin");
        }

        [Test]
        public void CampaignProgress_RejectsUnknownOriginWithClaimedState()
        {
            Assert.That(CampaignProgress.TryCreate(false, StartDate, string.Empty, false, false,
                out _, out string dateError), Is.False);
            Assert.That(dateError, Is.Not.Empty);

            Assert.That(CampaignProgress.TryCreate(false, default, string.Empty, true, false,
                out _, out string flagError), Is.False);
            Assert.That(flagError, Is.Not.Empty);

            Assert.That(CampaignProgress.TryCreate(true, StartDate, string.Empty, false, false,
                out _, out string originError), Is.False);
            Assert.That(originError, Is.Not.Empty);
        }

        [Test]
        public void CampaignProgress_RejectsOriginStartDateMismatch()
        {
            GameDate later = GameDate.Parse(1560, 4, 2);
            Assert.That(CampaignProgress.TryCreate(true, later,
                    CampaignOriginCatalog.Sc013OriginId, false, false, out _,
                    out string laterError), Is.False);
            Assert.That(laterError, Does.Contain("不一致"));

            // A different scenario's authored date is equally not this origin's date.
            Assert.That(CampaignProgress.TryCreate(true, GameDate.Parse(1560, 12, 1),
                    CampaignOriginCatalog.Sc013OriginId, false, false, out _,
                    out string otherScenarioError), Is.False);
            Assert.That(otherScenarioError, Does.Contain("不一致"));

            Assert.That(CampaignProgress.TryCreate(true, CampaignOriginCatalog.Sc013StartDate,
                    CampaignOriginCatalog.Sc013OriginId, false, false,
                    out CampaignProgress accepted, out string acceptedError), Is.True,
                acceptedError);
            Assert.That(accepted.StartDate, Is.EqualTo(StartDate));

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                CampaignProgress.CreateNew(later, CampaignOriginCatalog.Sc013OriginId));
        }

        [Test]
        public void CampaignProgress_UsesLeapDaySafeAnniversary()
        {
            // 1615 is not a leap year, so the 55-year anniversary falls back to Feb 28;
            // 1660 is a leap year, so the 100-year anniversary keeps Feb 29.
            GameDate leapStart = new GameDate(1560, 2, 29);
            Assert.That(CampaignProgress.TryGetAnniversary(leapStart, 55, out GameDate stage),
                Is.True);
            Assert.That(stage, Is.EqualTo(new GameDate(1615, 2, 28)));
            Assert.That(CampaignProgress.TryGetAnniversary(leapStart, 100, out GameDate century),
                Is.True);
            Assert.That(century, Is.EqualTo(new GameDate(1660, 2, 29)));

            // No authored scenario starts on Feb 29, so that date cannot claim SC-013.
            Assert.That(CampaignProgress.TryCreate(true, leapStart,
                CampaignOriginCatalog.Sc013OriginId, false, false, out _, out _), Is.False);
        }

        [Test]
        public void CampaignSaveData_RejectsOriginAndStartDateMismatch()
        {
            CampaignProgressSaveData mismatched = CampaignProgressSaveData.Capture(
                CampaignProgress.CreateNew(StartDate, CampaignOriginCatalog.Sc013OriginId));
            mismatched.startMonth = 12;
            mismatched.startDay = 1;

            Assert.That(mismatched.TryRestore(out _, out string restoreError), Is.False);
            Assert.That(restoreError, Does.Contain("不一致"));

            SaveOperationResult save = mismatched.ValidateForSave(GameDate.Parse(1700, 1, 1));
            Assert.That(save.Succeeded, Is.False);
            Assert.That(save.Message, Does.Contain("不一致"));

            CampaignProgressSaveData consistent = CampaignProgressSaveData.Capture(
                CampaignProgress.CreateNew(StartDate, CampaignOriginCatalog.Sc013OriginId));
            Assert.That(consistent.ValidateForSave(StageReviewDate).Succeeded, Is.True);

            SaveData whole = _session.CaptureSave();
            whole.campaign.startMonth = 12;
            whole.campaign.startDay = 1;
            Assert.That(SaveDataValidator.Validate(whole).Succeeded, Is.False,
                "the whole save is rejected when the campaign origin and start date disagree");
        }

        [Test]
        public void GameSession_CreateCampaignProgressUsesSuppliedValidatedStartDate()
        {
            CampaignProgress actual = GameSession.CreateCampaignProgress(
                SC023SeclusionScenario.StartDate, CampaignOriginCatalog.Sc023OriginId);
            Assert.That(actual.StartKnown, Is.True);
            Assert.That(actual.StartDate, Is.EqualTo(SC023SeclusionScenario.StartDate),
                "the supplied actual start date must be recorded, not a catalog lookup");
            Assert.That(actual.OriginId, Is.EqualTo(CampaignOriginCatalog.Sc023OriginId));
            Assert.That(actual.TryGetStageReviewDate(out GameDate stage), Is.True);
            Assert.That(stage, Is.EqualTo(GameDate.Parse(1615, 12, 1)));

            Assert.Throws<ArgumentOutOfRangeException>(() => GameSession.CreateCampaignProgress(
                StartDate, CampaignOriginCatalog.Sc023OriginId));
            Assert.Throws<ArgumentOutOfRangeException>(() => GameSession.CreateCampaignProgress(
                StartDate, "scenario.unknown"));
            Assert.That(GameSession.CreateCampaignProgress(StartDate, string.Empty).StartKnown,
                Is.False);
        }

        [Test]
        public void NonSc013Scenario_KeepsClockOriginAndAnniversariesConsistent()
        {
            _session.InitSeclusionDemoScenario();
            GameDate authoredStart = GameDate.Parse(1560, 12, 1);
            Assert.That(_session.WorldClock.CurrentDate, Is.EqualTo(authoredStart));
            Assert.That(_session.CampaignStartDate, Is.EqualTo(_session.WorldClock.CurrentDate));
            Assert.That(_session.CampaignOriginId,
                Is.EqualTo(CampaignOriginCatalog.Sc023OriginId));

            Assert.That(_session.CampaignState.TryGetStageReviewDate(out GameDate stage), Is.True);
            Assert.That(stage, Is.EqualTo(GameDate.Parse(1615, 12, 1)));
            Assert.That(_session.CampaignState.TryGetCenturyReviewDate(out GameDate century),
                Is.True);
            Assert.That(century, Is.EqualTo(GameDate.Parse(1660, 12, 1)));

            AdvanceTo(GameDate.Parse(1615, 11, 30));
            Assert.That(_session.CampaignCompletedYears, Is.EqualTo(54));
            Assert.That(_session.PendingCampaignReview, Is.EqualTo(CampaignReviewMilestone.None));

            AdvanceTo(GameDate.Parse(1615, 12, 1));
            Assert.That(_session.CampaignCompletedYears, Is.EqualTo(55));
            Assert.That(_session.PendingCampaignReview,
                Is.EqualTo(CampaignReviewMilestone.StageReview));
        }

        [Test]
        public void StageReviewPrecedesCenturyReviewAt1660WhenStillUnacknowledged()
        {
            AdvanceTo(CenturyReviewDate);
            Assert.That(_session.CampaignCompletedYears, Is.EqualTo(100));
            Assert.That(_session.PendingCampaignReview,
                Is.EqualTo(CampaignReviewMilestone.StageReview),
                "the preliminary stage review stays first while unacknowledged");
            Assert.That(_session.CampaignReview.Milestone,
                Is.EqualTo(CampaignReviewMilestone.StageReview));

            CampaignReviewResult acknowledged = _session.AcknowledgeCampaignReview();
            Assert.That(acknowledged.Succeeded, Is.True, acknowledged.Message);
            Assert.That(_session.CampaignState.StageReviewAcknowledged, Is.True);
            Assert.That(_session.PendingCampaignReview,
                Is.EqualTo(CampaignReviewMilestone.CenturyReview));
            Assert.That(_session.CampaignReview.Milestone,
                Is.EqualTo(CampaignReviewMilestone.CenturyReview));

            Assert.That(_session.AcknowledgeCampaignReview().Succeeded, Is.True);
            Assert.That(_session.PendingCampaignReview, Is.EqualTo(CampaignReviewMilestone.None));
        }

        [Test]
        public void ReviewView_RendersEveryGddCategoryFromAuthoritativeSources()
        {
            CampaignReviewView view = BuildRichReview();

            Assert.That(view.IsAvailable, Is.True);
            Assert.That(HasRow(view, CampaignHistoryCategory.Rulers), Is.True);
            Assert.That(HasRow(view, CampaignHistoryCategory.Wealth), Is.True);
            Assert.That(HasRow(view, CampaignHistoryCategory.Ninjutsu), Is.True);
            Assert.That(HasRow(view, CampaignHistoryCategory.People), Is.True);
            Assert.That(HasRow(view, CampaignHistoryCategory.Relations), Is.True);
            Assert.That(HasRow(view, CampaignHistoryCategory.HistoricalEvents), Is.True);
            Assert.That(HasRow(view, CampaignHistoryCategory.FinalStanding), Is.True);
            Assert.That(HasLabel(view, "阵亡"), Is.True);
            Assert.That(HasLabel(view, "负伤"), Is.True);
            Assert.That(HasLabel(view, "人物关系"), Is.True);
            Assert.That(HasLabel(view, "任务结算资产变化"), Is.True,
                "mission resource deltas are the dated wealth source when no industry ran");
            Assert.That(view.MissingSourceNotes, Has.None.Contains("家族财富变化"),
                "a settled mission delta is an authoritative wealth record");

            Assert.That(view.Dimensions, Has.Count.EqualTo(5),
                "the five GDD 10.1 dimensions are shown separately");
            Assert.That(view.ScorePolicyNote, Does.Contain("不生成综合评分"));

            string body = view.FormatBody();
            Assert.That(body, Does.Contain("历代家主"));
            Assert.That(body, Does.Contain("家族财富变化"));
            Assert.That(body, Does.Contain("忍术成就"));
            Assert.That(body, Does.Contain("人物死亡与重要经历"));
            Assert.That(body, Does.Contain("家族盟友和仇敌"));
            Assert.That(body, Does.Contain("重大历史事件"));
            Assert.That(body, Does.Contain("家族最终地位"));
        }

        [Test]
        public void ReviewView_WealthNoteIsDecidedAfterMissionDeltasAreKnown()
        {
            CampaignProgress progress = CampaignProgress.CreateNew(StartDate,
                CampaignOriginCatalog.Sc013OriginId);
            CampaignReviewView view = CampaignReviewView.Build(
                CampaignReviewMilestone.StageReview, progress, StageReviewDate,
                M2FixedScenario.CreateSuccession(), new FamilyChronicleEntry[0],
                new[] { CreateMissionRecord() }, FamilyStandingState.CreateInitial(), default,
                new string[0], 0, string.Empty);

            Assert.That(HasRow(view, CampaignHistoryCategory.Wealth), Is.True,
                "the mission funds/supplies deltas are the dated wealth fact");
            Assert.That(view.MissingSourceNotes, Has.None.Contains("家族财富变化"),
                "the wealth note must wait until every source has contributed");
        }

        [Test]
        public void ReviewView_FormatBodyListsEachCategoryOnceInChronology()
        {
            CampaignReviewView view = BuildRichReview();
            string body = view.FormatBody();

            var order = new[]
            {
                CampaignHistoryCategory.Rulers,
                CampaignHistoryCategory.Wealth,
                CampaignHistoryCategory.Ninjutsu,
                CampaignHistoryCategory.People,
                CampaignHistoryCategory.Relations,
                CampaignHistoryCategory.HistoricalEvents,
                CampaignHistoryCategory.FinalStanding
            };
            string[] lines = body.Split('\n');
            int line = 0;
            int rowLines = 0;
            for (int c = 0; c < order.Length; c++)
            {
                string header = "【" + CampaignReviewView.CategoryName(order[c]) + "】";
                Assert.That(CountOccurrences(body, header), Is.EqualTo(1),
                    header + " must have exactly one section");
                Assert.That(lines[line], Is.EqualTo(header),
                    "category sections must follow the GDD order with no extra section");
                line++;

                List<CampaignHistoryRow> expected = RowsOf(view, order[c]);
                int listed = 0;
                while (line < lines.Length && !lines[line].StartsWith("【", StringComparison.Ordinal))
                {
                    string current = lines[line];
                    line++;
                    if (current.Length == 0)
                        continue;
                    if (current == "（无权威记录）")
                    {
                        Assert.That(expected, Is.Empty, header + " is not empty");
                        continue;
                    }
                    Assert.That(listed, Is.LessThan(expected.Count),
                        header + " lists more rows than the recorded history");
                    CampaignHistoryRow row = expected[listed];
                    Assert.That(current, Does.Contain(row.Text),
                        header + " row " + listed + " must be the next recorded fact");
                    if (row.Date.IsValid)
                        Assert.That(current, Does.StartWith(row.Date.ToString()),
                            header + " rows must stay in date order");
                    listed++;
                    rowLines++;
                }
                Assert.That(listed, Is.EqualTo(expected.Count),
                    header + " must list its complete history exactly once");
            }

            Assert.That(rowLines, Is.EqualTo(view.Timeline.Count),
                "every recorded row is printed exactly once");
            Assert.That(body, Does.Not.Contain("【家族编年时间线】"),
                "the history must not be repeated as a second flat timeline");
        }

        [Test]
        public void ReviewView_ReportsMissingFactsInsteadOfInventingThem()
        {
            SuccessionState succession = M2FixedScenario.CreateSuccession();
            CampaignProgress progress = CampaignProgress.CreateNew(StartDate,
                CampaignOriginCatalog.Sc013OriginId);
            CampaignReviewView view = CampaignReviewView.Build(
                CampaignReviewMilestone.StageReview, progress, StageReviewDate, succession,
                new FamilyChronicleEntry[0], new MissionRecord[0],
                FamilyStandingState.CreateInitial(), default, new string[0], 0, string.Empty);

            Assert.That(view.IsAvailable, Is.True);
            Assert.That(HasRow(view, CampaignHistoryCategory.Rulers), Is.True,
                "the current head is a recorded fact");
            Assert.That(HasLabel(view, "现任家主"), Is.True);
            Assert.That(HasRow(view, CampaignHistoryCategory.Wealth), Is.False);
            Assert.That(HasRow(view, CampaignHistoryCategory.Ninjutsu), Is.False);
            Assert.That(HasRow(view, CampaignHistoryCategory.People), Is.False);
            Assert.That(HasRow(view, CampaignHistoryCategory.HistoricalEvents), Is.False);
            Assert.That(HasRow(view, CampaignHistoryCategory.FinalStanding), Is.True,
                "current standing is always a sourced fact");
            Assert.That(view.Dimensions, Has.Count.EqualTo(5));

            string notes = string.Join("\n", view.MissingSourceNotes);
            Assert.That(notes, Does.Contain("家族财富变化"));
            Assert.That(notes, Does.Contain("人物死亡与重要经历"));
            Assert.That(notes, Does.Contain("忍术成就"));
            Assert.That(notes, Does.Contain("重大历史事件"));
            Assert.That(notes, Does.Contain("家族盟友和仇敌"));
            Assert.That(notes, Does.Contain("不回填"),
                "the history note must say past historical nodes are not backfilled");
            Assert.That(notes, Does.Not.Contain("世界历史事件流尚未实现"),
                "the stale unimplemented-stream claim must be gone");
        }

        /// <summary>
        /// M08C: once a historical timeline row is committed it is one dated event fact in
        /// the review, and the empty-source note stops claiming the stream is missing.
        /// </summary>
        [Test]
        public void ReviewView_ListsCommittedHistoricalNodeAndNeverBackfillsPastOnes()
        {
            var ledger = HistoryLedger.CreateEmpty();
            Assert.That(ledger.TryAppendEvent(1579, 1, SC010EventIds.FirstIgaNodeEventId,
                    SC010EventIds.FirstIgaNodeOptionId, "第一次天正伊贺之乱",
                    "家主将此事写入家史。", out _, out string appendError),
                Is.True, appendError);
            CampaignProgress progress = CampaignProgress.CreateNew(StartDate,
                CampaignOriginCatalog.Sc010OriginId);

            CampaignReviewView view = CampaignReviewView.Build(
                CampaignReviewMilestone.StageReview, progress, StageReviewDate,
                SuccessionSample.CreateStandard(), ledger.Entries,
                new MissionRecord[0], FamilyStandingState.CreateInitial(), default,
                new string[0], 0, string.Empty);

            List<CampaignHistoryRow> rows = RowsOf(view, CampaignHistoryCategory.HistoricalEvents);
            Assert.That(rows, Has.Count.EqualTo(1));
            Assert.That(rows[0].Date, Is.EqualTo(GameDate.Parse(1579, 1, 1)));
            Assert.That(rows[0].Text, Does.Contain("第一次天正伊贺之乱"));
            Assert.That(rows[0].Text, Does.Not.Contain("月"),
                "the row carries the year-level fact, not a claimed historical month");
            string notes = string.Join("\n", view.MissingSourceNotes);
            Assert.That(notes, Does.Not.Contain("重大历史事件"),
                "a committed node is a recorded fact, not a missing source");
            Assert.That(notes, Does.Not.Contain("不回填"));
        }

        [Test]
        public void CampaignReviewPanel_ShowsOnlyWhenPendingAndContinueClosesIt()
        {
            using (PanelRig rig = PanelRig.Create(_session))
            {
                CampaignReviewPanel panel = rig.Panel;
                panel.Refresh();
                Assert.That(panel.IsVisible, Is.False);
                Assert.That(panel.StaticButtonBindingCount, Is.EqualTo(1));

                AdvanceTo(StageReviewDate);
                panel.Refresh();
                Assert.That(panel.IsVisible, Is.True);
                Assert.That(panel.Title, Does.Contain("阶段评价"));
                Assert.That(panel.Body, Does.Contain("五项发展维度"));
                Assert.That(panel.Body, Does.Contain("（无权威记录）"),
                    "empty categories stay explicit instead of disappearing");
                Assert.That(panel.ContinueLabel, Does.Contain("继续家族"));

                panel.Continue();
                Assert.That(panel.IsVisible, Is.False);
                Assert.That(panel.Status, Does.Contain("已确认"));
                Assert.That(_session.PendingCampaignReview,
                    Is.EqualTo(CampaignReviewMilestone.None));
            }
        }

        [Test]
        public void CampaignReviewPanel_AppearsOnSessionStateChangeWithoutManualRefresh()
        {
            using (PanelRig rig = PanelRig.Create(_session))
            {
                AdvanceTo(GameDate.Parse(1615, 3, 31));
                Assert.That(rig.Panel.IsVisible, Is.False,
                    "the fixture date seam publishes no state change");

                Assert.That(_session.TryAdvanceToNextMonth(), Is.True,
                    _session.LastEventMessage);
                Assert.That(_session.WorldClock.CurrentDate, Is.EqualTo(StageReviewDate));
                Assert.That(_session.PendingCampaignReview,
                    Is.EqualTo(CampaignReviewMilestone.StageReview));
                Assert.That(rig.Panel.IsVisible, Is.True,
                    "SessionStateChanged must reveal the pending review without a manual Refresh");
            }
        }

        [Test]
        public void TitleContinueHint_ShowsPersistedOriginAndStartDate()
        {
            string directory = Path.Combine(Path.GetTempPath(),
                "SengokuChronicleCampaignTests", Guid.NewGuid().ToString("N"));
            var controllerObject = new GameObject("TitleScreenController");
            var titleRoot = new GameObject("M6TitleRoot", typeof(RectTransform));
            var newGameObject = new GameObject("NewGameButton", typeof(RectTransform));
            var continueObject = new GameObject("ContinueButton", typeof(RectTransform));
            var statusObject = new GameObject("Status", typeof(RectTransform));
            GameSession session = GameSession.NewGame();
            try
            {
                var store = new SaveSlotStore(directory);
                var controller = controllerObject.AddComponent<TitleScreenController>();
                controller.Bind(session, store, null, titleRoot,
                    newGameObject.AddComponent<Button>(),
                    continueObject.AddComponent<Button>(),
                    statusObject.AddComponent<Text>());

                session.InitSeclusionDemoScenario();
                SaveData save = session.CaptureSave();
                Assert.That(store.Save(save).Succeeded, Is.True);
                controller.RefreshTitleState();
                Assert.That(controller.Status, Does.Contain("SC-023"));
                Assert.That(controller.Status, Does.Contain("1560-12-01"));

                // A legacy save that cannot prove its origin must not imply one.
                SaveData unknown = session.CaptureSave();
                unknown.campaign = CampaignProgressSaveData.CreateUnknownOrigin();
                Assert.That(store.Save(unknown).Succeeded, Is.True);
                controller.RefreshTitleState();
                Assert.That(controller.Status, Does.Contain("开局日期未知"));
                Assert.That(controller.Status, Does.Not.Contain("SC-023"));
            }
            finally
            {
                Object.DestroyImmediate(controllerObject);
                Object.DestroyImmediate(titleRoot);
                Object.DestroyImmediate(newGameObject);
                Object.DestroyImmediate(continueObject);
                Object.DestroyImmediate(statusObject);
                Object.DestroyImmediate(session.gameObject);
                if (Directory.Exists(directory))
                    Directory.Delete(directory, true);
            }
        }

        [Test]
        public void EmptySaveSlot_LeavesTheSessionUntouched()
        {
            string directory = Path.Combine(Path.GetTempPath(),
                "SengokuChronicleCampaignTests", Guid.NewGuid().ToString("N"));
            try
            {
                var store = new SaveSlotStore(directory);
                Assert.That(store.Load().Succeeded, Is.False);
                Assert.That(store.Load().Error, Is.EqualTo(SaveErrorCode.NotFound));

                SaveOperationResult result = _session.LoadFrom(store);
                Assert.That(result.Succeeded, Is.False);
                Assert.That(_session.WorldClock.CurrentDate, Is.EqualTo(StartDate));
            }
            finally
            {
                if (Directory.Exists(directory))
                    Directory.Delete(directory, true);
            }
        }

        [Test]
        public void FailedSave_PreservesTheExistingSlotContents()
        {
            string directory = Path.Combine(Path.GetTempPath(),
                "SengokuChronicleCampaignTests", Guid.NewGuid().ToString("N"));
            try
            {
                var store = new SaveSlotStore(directory);
                Assert.That(store.Save(_session.CaptureSave()).Succeeded, Is.True);
                byte[] before = File.ReadAllBytes(store.FilePath);

                SaveData invalid = _session.CaptureSave();
                invalid.year = 1500;
                SaveOperationResult failed = store.Save(invalid);
                Assert.That(failed.Succeeded, Is.False);
                Assert.That(File.ReadAllBytes(store.FilePath), Is.EqualTo(before),
                    "a failed save must leave the previous slot bytes untouched");
            }
            finally
            {
                if (Directory.Exists(directory))
                    Directory.Delete(directory, true);
            }
        }

        [Test]
        public void PhaseOneCompletion_StillRequiresAllThreeMissions()
        {
            var data = _session.CaptureSave();
            data.year = 1560;
            data.month = 4;
            data.day = 3;
            data.supplyShortageStage = (int)SupplyShortageStage.RewardClaimed;
            data.sealedLetterStage = (int)SealedLetterStage.RewardClaimed;
            data.marketTallyStage = (int)MarketTallyStage.RewardClaimed;
            Assert.That(_session.TryApplySave(data).Succeeded, Is.True, _session.LastEventMessage);
            Assert.That(_session.IsDemoComplete, Is.True);

            // Campaign review acknowledgements are separate from Phase 1 completion.
            AdvanceTo(StageReviewDate);
            Assert.That(_session.IsDemoComplete, Is.True);
            Assert.That(_session.AcknowledgeCampaignReview().Succeeded, Is.True);
            Assert.That(_session.IsDemoComplete, Is.True);
            Assert.That(_session.Merit, Is.EqualTo(20));
        }

        /// <summary>Minimal uGUI wiring for the review panel; disposes every fixture object.</summary>
        private sealed class PanelRig : IDisposable
        {
            private readonly List<GameObject> _objects = new List<GameObject>();

            public CampaignReviewPanel Panel { get; private set; }

            public static PanelRig Create(GameSession session)
            {
                var rig = new PanelRig();
                GameObject host = rig.Track("CampaignReviewPanel");
                GameObject root = rig.Track("Root", typeof(RectTransform));
                Text title = rig.Track("Title", typeof(RectTransform)).AddComponent<Text>();
                Text context = rig.Track("Context", typeof(RectTransform)).AddComponent<Text>();
                Text body = rig.Track("Body", typeof(RectTransform)).AddComponent<Text>();
                Text label = rig.Track("Label", typeof(RectTransform)).AddComponent<Text>();
                Text status = rig.Track("Status", typeof(RectTransform)).AddComponent<Text>();
                Button button = rig.Track("Continue", typeof(RectTransform)).AddComponent<Button>();

                rig.Panel = host.AddComponent<CampaignReviewPanel>();
                rig.Panel.BindReferences(session, root, title, context, body, label, status,
                    button);
                return rig;
            }

            public void Dispose()
            {
                for (int i = 0; i < _objects.Count; i++)
                    if (_objects[i] != null)
                        Object.DestroyImmediate(_objects[i]);
            }

            private GameObject Track(string name, params Type[] components)
            {
                var go = new GameObject(name, components);
                _objects.Add(go);
                return go;
            }
        }

        private static MissionRecord CreateMissionRecord()
        {
            var participants = new[]
            {
                M2FixedScenario.MasatsunaId, M2FixedScenario.NobumasaId
            };
            Assert.That(MissionRecord.TryCreate("run.m06.intercepted-dispatch",
                FamilyChronicleEntry.TacticalMissionId, MissionKind.TacticalInfiltration,
                MissionOperationMode.Manual, GameDate.Parse(1605, 6, 1),
                SharedMissionOutcome.SevereFailure, M2FixedScenario.MasatsunaId, participants,
                participants, MissionPreparation.None, 40, 30, 0, 50, 10, 20, 0, 0, 50, 45,
                new[]
                {
                    new MissionStandingChange(MissionRelationIds.InterceptedDispatchTarget, 0, -5)
                }, 0, -3, 0, new[]
                {
                    new MissionParticipantState(M2FixedScenario.MasatsunaId, true, false,
                        false, false),
                    new MissionParticipantState(M2FixedScenario.NobumasaId, true, false, true,
                        true)
                }, "chronicle.000007", out MissionRecord record, out string recordError), Is.True,
                recordError);
            return record;
        }

        /// <summary>
        /// One review with a sourced fact in every GDD 10.3 category. Only authored
        /// scenario IDs and recorded deltas feed it; nothing is invented for display.
        /// </summary>
        private static CampaignReviewView BuildRichReview()
        {
            SuccessionState succession = M2FixedScenario.CreateSuccession();
            var ledger = HistoryLedger.CreateEmpty();
            Assert.That(ledger.TryAppendSuccession(1600, 1, M2FixedScenario.HarunobuId.Value,
                "result.sc013", (int)SuccessionPhase.None, (int)SuccessionPhase.None,
                "家督交接", "晴信传位正綱", M2FixedScenario.HarunobuId.Value,
                M2FixedScenario.MasatsunaId.Value, out _, out string successionError), Is.True,
                successionError);
            Assert.That(ledger.TryAppendIndustry(1601, 2, "industry.monthly", "settlement",
                30, 40, "产业月结", "资金 30→40，物资 12→13", out _, out string industryError),
                Is.True, industryError);
            Assert.That(ledger.TryAppendResearch(1602, 3,
                FamilyResearchCatalog.SmokeMixingResearchId,
                FamilyResearchCatalog.SmokeMixingTechniqueId, 1, 2, "家族研究",
                "完成烟幕调制术", out _, out string researchError), Is.True, researchError);
            Assert.That(ledger.TryAppendEvent(1603, 4, "event.sc011.rainy-arrival",
                "option.accept", "雨夜来客", "旧客来访，选择接纳。", out _,
                out string eventError), Is.True, eventError);
            Assert.That(ledger.TryAppendRelationship(1603, 5, CharacterId.KurodaIori.Value,
                "event", -3, 5, "黑田伊织", "旧交关系由疑转信。", out _,
                out string relationshipError), Is.True, relationshipError);
            Assert.That(ledger.TryAppendEducation(1604, 5, M2FixedScenario.NobumasaId.Value,
                "education.free-growth", 0, 0, "信正的年度培养", "自由成长。", out _,
                out string educationError), Is.True, educationError);
            Assert.That(ledger.TryAppendFamilyFormation(1604, 6,
                M2FixedScenario.MasatsunaId.Value, FamilyChronicleEntry.MarriageSourceId,
                "婚姻登记", "正綱完成婚姻登记。", out _, out string formationError), Is.True,
                formationError);

            FamilyStandingState standing = FamilyStandingState.CreateInitial();
            Assert.That(standing.TryApplyDeltas(3, 0, new[]
            {
                new MissionRelationDelta(MissionRelationIds.ShadowEscortMerchant, 2)
            }, out _, out string standingError), Is.True, standingError);
            Assert.That(IndustryResources.TryCreate(40, 13, 6, out IndustryResources resources,
                out string resourceError), Is.True, resourceError);

            CampaignProgress progress = CampaignProgress.CreateNew(StartDate,
                CampaignOriginCatalog.Sc013OriginId);
            return CampaignReviewView.Build(
                CampaignReviewMilestone.CenturyReview, progress, CenturyReviewDate, succession,
                ledger.Entries, new[] { CreateMissionRecord() }, standing, resources,
                new[] { FamilyResearchCatalog.SmokeMixingTechniqueId }, 22, "武士头");
        }

        private static List<CampaignHistoryRow> RowsOf(CampaignReviewView view,
            CampaignHistoryCategory category)
        {
            var rows = new List<CampaignHistoryRow>();
            for (int i = 0; i < view.Timeline.Count; i++)
                if (view.Timeline[i].Category == category)
                    rows.Add(view.Timeline[i]);
            return rows;
        }

        private static int CountOccurrences(string text, string value)
        {
            int count = 0;
            int index = 0;
            while ((index = text.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
            {
                count++;
                index += value.Length;
            }
            return count;
        }

        private static bool HasRow(CampaignReviewView view, CampaignHistoryCategory category)
        {
            for (int i = 0; i < view.Timeline.Count; i++)
                if (view.Timeline[i].Category == category)
                    return true;
            return false;
        }

        private static bool HasLabel(CampaignReviewView view, string label)
        {
            for (int i = 0; i < view.Timeline.Count; i++)
                if (string.Equals(view.Timeline[i].Label, label, StringComparison.Ordinal))
                    return true;
            return false;
        }
    }
}
