using System;
using SengokuChronicle.Domain;

namespace SengokuChronicle.Application
{
    /// <summary>
    /// M08 campaign review surface. It owns only the campaign origin and the two
    /// one-time review acknowledgements; every displayed fact comes from the existing
    /// succession, industry, research, mission-history, chronicle and standing sources.
    /// Phase 1 demo completion remains the separate <see cref="GameSession.IsDemoComplete"/>
    /// rule and is never used as an ending.
    /// </summary>
    public sealed partial class GameSession
    {
        private CampaignProgress _campaignProgress;

        /// <summary>Persisted campaign origin and acknowledgement state.</summary>
        public CampaignProgress CampaignState => EnsureCampaignProgress();

        public bool CampaignStartKnown => EnsureCampaignProgress().StartKnown;

        /// <summary>Invalid date when the legacy origin is unknown.</summary>
        public GameDate CampaignStartDate => EnsureCampaignProgress().StartDate;

        public string CampaignOriginId => EnsureCampaignProgress().OriginId;

        /// <summary>Completed whole years since the proven campaign start; 0 when unknown.</summary>
        public int CampaignCompletedYears =>
            EnsureCampaignProgress().CompletedYears(CurrentGameDate);

        /// <summary>The earliest due review that has not been acknowledged yet.</summary>
        public CampaignReviewMilestone PendingCampaignReview =>
            EnsureCampaignProgress().PendingReview(CurrentGameDate);

        public bool HasPendingCampaignReview =>
            PendingCampaignReview != CampaignReviewMilestone.None;

        /// <summary>Last acknowledgement or rejection feedback.</summary>
        public string LastCampaignReviewMessage { get; private set; } = string.Empty;

        /// <summary>
        /// Player-facing origin line. A legacy save with an unknown origin says so
        /// explicitly instead of claiming an anniversary.
        /// </summary>
        public string CampaignOriginSummary =>
            CampaignReviewView.DescribeCampaign(EnsureCampaignProgress(), CurrentGameDate);

        /// <summary>Read-only review projection for the current pending milestone.</summary>
        public CampaignReviewView CampaignReview
        {
            get
            {
                CampaignReviewMilestone milestone = PendingCampaignReview;
                if (milestone == CampaignReviewMilestone.None)
                    return CampaignReviewView.Unavailable;
                return CampaignReviewView.Build(milestone, EnsureCampaignProgress(),
                    CurrentGameDate, Succession, ChronicleEntries, MissionHistory,
                    EnsureStanding(), EnsureIndustryState().Resources,
                    EnsureResearch().KnownTechniqueIds, Merit, RankName);
            }
        }

        /// <summary>
        /// Acknowledges the currently pending review exactly once. A successful call
        /// persists the flag through <see cref="SaveData.campaign"/>; rejection leaves the
        /// state unchanged. Both reviews are non-terminal and never gate time advance.
        /// </summary>
        public CampaignReviewResult AcknowledgeCampaignReview()
        {
            CampaignProgress progress = EnsureCampaignProgress();
            CampaignReviewMilestone milestone = progress.PendingReview(CurrentGameDate);
            CampaignReviewResult result = progress.Acknowledge(milestone, CurrentGameDate);
            LastCampaignReviewMessage = result.Message;
            if (result.Succeeded)
            {
                _campaignProgress = result.State;
                SessionStateChanged?.Invoke();
            }
            return result;
        }

        private CampaignProgress EnsureCampaignProgress()
        {
            if (_campaignProgress == null)
                _campaignProgress = CampaignProgress.CreateUnknownOrigin();
            return _campaignProgress;
        }

        /// <summary>
        /// New campaigns record the validated actual start date supplied by the caller.
        /// The catalog proves that date against the authored origin, so the campaign
        /// anniversary dates and the world clock can never disagree. An empty origin is
        /// kept as the explicit unknown legacy origin; any other contradictory pair is a
        /// caller defect and is rejected instead of guessing.
        /// </summary>
        internal static CampaignProgress CreateCampaignProgress(GameDate startDate,
            string originId)
        {
            if (string.IsNullOrEmpty(originId))
                return CampaignProgress.CreateUnknownOrigin();
            if (!CampaignProgress.TryCreate(true, startDate, originId, false, false,
                    out CampaignProgress progress, out string error))
                throw new ArgumentOutOfRangeException(nameof(originId), error);
            return progress;
        }

        /// <summary>Test seam: replaces the campaign state without touching the calendar.</summary>
        internal void SetCampaignProgressForTest(CampaignProgress progress) =>
            _campaignProgress = progress ?? CampaignProgress.CreateUnknownOrigin();
    }
}
