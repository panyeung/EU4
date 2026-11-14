using System;
using System.Collections.Generic;

namespace SengokuChronicle.Domain
{
    /// <summary>
    /// The two long-term campaign reviews defined by GDD 10.2/10.3 and 19.7. The
    /// historical stage review is preliminary and non-terminal; the century review is
    /// the separate 百年名门 evaluation. Neither replaces the Phase 1 demo completion.
    /// </summary>
    public enum CampaignReviewMilestone
    {
        None = 0,
        /// <summary>Fifth-five-year anniversary: preliminary, non-terminal stage review.</summary>
        StageReview = 1,
        /// <summary>Hundred-year anniversary: separate 百年名门 evaluation.</summary>
        CenturyReview = 2
    }

    /// <summary>Immutable result of one acknowledgement request.</summary>
    public readonly struct CampaignReviewResult
    {
        public CampaignReviewResult(bool succeeded, string message, CampaignProgress state)
        {
            Succeeded = succeeded;
            Message = message ?? string.Empty;
            State = state;
        }

        public bool Succeeded { get; }
        public string Message { get; }
        /// <summary>State after a successful acknowledgement, otherwise the unchanged state.</summary>
        public CampaignProgress State { get; }
    }

    /// <summary>
    /// One authored campaign origin. The start date is fixed by the scenario definition,
    /// so a save that still carries that scenario identity can prove its origin. No
    /// other origin is invented, and a campaign with an unknown origin never claims a
    /// milestone.
    /// </summary>
    public readonly struct CampaignOrigin
    {
        public CampaignOrigin(string id, string displayName, GameDate startDate)
        {
            Id = id ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            StartDate = startDate;
        }

        public string Id { get; }
        public string DisplayName { get; }
        public GameDate StartDate { get; }
    }

    /// <summary>
    /// Stable mapping from an authored scenario identity to its fixed start date. The
    /// family IDs are the persistence-visible identity of each scenario; the event route
    /// distinguishes SC-024 from ordinary SC-013 because both reuse one family roster.
    /// </summary>
    public static class CampaignOriginCatalog
    {
        public const string Sc010OriginId = "sc010";
        public const string Sc013OriginId = "sc013";
        public const string Sc020OriginId = "sc020";
        public const string Sc021OriginId = "sc021";
        public const string Sc022OriginId = "sc022";
        public const string Sc023OriginId = "sc023";
        public const string Sc024OriginId = "sc024";

        public const string Sc010FamilyId = "family.akechi";
        public const string Sc013FamilyId = "family.akechi.sc013";
        public const string Sc020FamilyId = "family.aoba.sc020";
        public const string Sc021FamilyId = "family.kageyama.sc021";
        public const string Sc022FamilyId = "family.asaba.sc022";
        public const string Sc023FamilyId = "family.tachibana.sc023";
        /// <summary>SC-024 reuses the SC-013 roster and adds its own event route.</summary>
        public const string Sc024EventScenarioId = "sc024";

        public static readonly GameDate Sc010StartDate = new GameDate(1560, 4, 1);
        public static readonly GameDate Sc013StartDate = new GameDate(1560, 4, 1);
        public static readonly GameDate Sc020StartDate = new GameDate(1560, 1, 1);
        public static readonly GameDate Sc021StartDate = new GameDate(1560, 1, 1);
        public static readonly GameDate Sc022StartDate = new GameDate(1560, 5, 1);
        public static readonly GameDate Sc023StartDate = new GameDate(1560, 12, 1);

        private static readonly CampaignOrigin[] Origins =
        {
            new CampaignOrigin(Sc010OriginId, "SC-010 标准样例", Sc010StartDate),
            new CampaignOrigin(Sc013OriginId, "SC-013 固定三代剧本", Sc013StartDate),
            new CampaignOrigin(Sc020OriginId, "SC-020 子女培养示范", Sc020StartDate),
            new CampaignOrigin(Sc021OriginId, "SC-021 家族研究示范", Sc021StartDate),
            new CampaignOrigin(Sc022OriginId, "SC-022 婚姻与收养示范", Sc022StartDate),
            new CampaignOrigin(Sc023OriginId, "SC-023 隐居与后见人示范", Sc023StartDate),
            new CampaignOrigin(Sc024OriginId, "SC-024 家训与抱负示范", Sc013StartDate)
        };

        public static IReadOnlyList<CampaignOrigin> All { get; } = Array.AsReadOnly(Origins);

        public static bool TryGet(string originId, out CampaignOrigin origin)
        {
            for (int i = 0; i < Origins.Length; i++)
            {
                if (string.Equals(Origins[i].Id, originId, StringComparison.Ordinal))
                {
                    origin = Origins[i];
                    return true;
                }
            }

            origin = default;
            return false;
        }

        public static bool IsKnown(string originId) => TryGet(originId, out _);

        /// <summary>
        /// Infers a legacy save's origin only when its saved family identity (and, for
        /// SC-024, its event route) proves the authored scenario and the saved date is not
        /// earlier than that scenario's fixed start. Anything else stays unknown.
        /// </summary>
        public static bool TryInferLegacyOrigin(string familyId, string eventScenarioId,
            GameDate savedDate, out CampaignOrigin origin)
        {
            origin = default;
            if (string.IsNullOrWhiteSpace(familyId) || !savedDate.IsValid)
                return false;

            string originId;
            if (string.Equals(familyId, Sc013FamilyId, StringComparison.Ordinal))
            {
                originId = string.Equals(eventScenarioId ?? string.Empty, Sc024EventScenarioId,
                    StringComparison.Ordinal)
                    ? Sc024OriginId
                    : Sc013OriginId;
            }
            else if (string.Equals(familyId, Sc010FamilyId, StringComparison.Ordinal))
                originId = Sc010OriginId;
            else if (string.Equals(familyId, Sc020FamilyId, StringComparison.Ordinal))
                originId = Sc020OriginId;
            else if (string.Equals(familyId, Sc021FamilyId, StringComparison.Ordinal))
                originId = Sc021OriginId;
            else if (string.Equals(familyId, Sc022FamilyId, StringComparison.Ordinal))
                originId = Sc022OriginId;
            else if (string.Equals(familyId, Sc023FamilyId, StringComparison.Ordinal))
                originId = Sc023OriginId;
            else
                return false;

            if (!TryGet(originId, out CampaignOrigin inferred))
                return false;
            // A saved date before the authored start contradicts that identity, so the
            // date is not proven and must not be inferred.
            if (savedDate < inferred.StartDate)
                return false;

            origin = inferred;
            return true;
        }
    }

    /// <summary>
    /// Persisted campaign origin plus one-time review acknowledgements. A campaign whose
    /// origin is unknown (a legacy save that cannot prove it) never claims either review
    /// and has no anniversary dates.
    /// </summary>
    public sealed class CampaignProgress
    {
        public const int StageReviewYears = 55;
        public const int CenturyReviewYears = 100;

        private CampaignProgress(bool startKnown, GameDate startDate, string originId,
            bool stageReviewAcknowledged, bool centuryReviewAcknowledged)
        {
            StartKnown = startKnown;
            StartDate = startDate;
            OriginId = originId ?? string.Empty;
            StageReviewAcknowledged = stageReviewAcknowledged;
            CenturyReviewAcknowledged = centuryReviewAcknowledged;
        }

        public bool StartKnown { get; }
        /// <summary>Invalid (<see cref="GameDate"/>.default) when the origin is unknown.</summary>
        public GameDate StartDate { get; }
        public string OriginId { get; }
        public bool StageReviewAcknowledged { get; }
        public bool CenturyReviewAcknowledged { get; }

        public static CampaignProgress CreateNew(GameDate startDate, string originId)
        {
            if (!startDate.IsValid)
                throw new ArgumentOutOfRangeException(nameof(startDate),
                    "战役开局日期无效：" + startDate + "。");
            if (!CampaignOriginCatalog.TryGet(originId, out CampaignOrigin origin))
                throw new ArgumentOutOfRangeException(nameof(originId),
                    "未知剧本来源：" + (originId ?? "(null)") + "。");
            if (startDate != origin.StartDate)
                throw new ArgumentOutOfRangeException(nameof(startDate),
                    AuthoredStartMismatch(startDate, origin));
            return new CampaignProgress(true, startDate, originId, false, false);
        }

        /// <summary>Explicit unknown origin: no anniversary and no claimed review.</summary>
        public static CampaignProgress CreateUnknownOrigin() =>
            new CampaignProgress(false, default, string.Empty, false, false);

        /// <summary>Validates one untrusted snapshot; never throws.</summary>
        public static bool TryCreate(bool startKnown, GameDate startDate, string originId,
            bool stageReviewAcknowledged, bool centuryReviewAcknowledged,
            out CampaignProgress progress, out string error)
        {
            progress = null;
            error = string.Empty;
            if (!startKnown)
            {
                if (startDate.IsValid || !string.IsNullOrEmpty(originId) ||
                    stageReviewAcknowledged || centuryReviewAcknowledged)
                {
                    error = "旧档开局日期未知，不能同时记录开局日期、剧本来源或评价确认。";
                    return false;
                }
                progress = CreateUnknownOrigin();
                return true;
            }

            if (!startDate.IsValid)
            {
                error = "战役开局日期无效。";
                return false;
            }
            // The catalog fixes each authored scenario's start date, so a campaign that
            // names that origin cannot start on another day.
            if (!CampaignOriginCatalog.TryGet(originId, out CampaignOrigin origin))
            {
                error = "战役剧本来源未知：" + (originId ?? "(null)") + "。";
                return false;
            }
            if (startDate != origin.StartDate)
            {
                error = AuthoredStartMismatch(startDate, origin);
                return false;
            }
            if (stageReviewAcknowledged &&
                !TryGetAnniversary(startDate, StageReviewYears, out _))
            {
                error = "五十五周年评价日期超出支持范围。";
                return false;
            }
            if (centuryReviewAcknowledged &&
                !TryGetAnniversary(startDate, CenturyReviewYears, out _))
            {
                error = "百年评价日期超出支持范围。";
                return false;
            }

            progress = new CampaignProgress(true, startDate, originId,
                stageReviewAcknowledged, centuryReviewAcknowledged);
            return true;
        }

        /// <summary>Same-month/day anniversary; a Feb 29 start falls back to Feb 28.</summary>
        public static bool TryGetAnniversary(GameDate start, int years, out GameDate date)
        {
            date = default;
            if (!start.IsValid || years <= 0)
                return false;
            int year = start.Year + years;
            if (year > GameDate.MaxYear)
                return false;
            int day = start.Day;
            if (!GameDate.IsValidDate(year, start.Month, day))
            {
                if (start.Month == 2 && start.Day == 29)
                    day = 28;
                else
                    return false;
            }
            date = new GameDate(year, start.Month, day);
            return true;
        }

        public bool TryGetStageReviewDate(out GameDate date)
        {
            if (StartKnown && TryGetAnniversary(StartDate, StageReviewYears, out date))
                return true;
            date = default;
            return false;
        }

        public bool TryGetCenturyReviewDate(out GameDate date)
        {
            if (StartKnown && TryGetAnniversary(StartDate, CenturyReviewYears, out date))
                return true;
            date = default;
            return false;
        }

        /// <summary>
        /// The earliest unacknowledged review that is already due. The historical stage
        /// review is presented before the century review when both are due. Unknown origin
        /// returns <see cref="CampaignReviewMilestone.None"/> forever.
        /// </summary>
        public CampaignReviewMilestone PendingReview(GameDate today)
        {
            if (!StartKnown || !today.IsValid)
                return CampaignReviewMilestone.None;
            if (!StageReviewAcknowledged &&
                TryGetStageReviewDate(out GameDate stage) && today >= stage)
                return CampaignReviewMilestone.StageReview;
            if (!CenturyReviewAcknowledged &&
                TryGetCenturyReviewDate(out GameDate century) && today >= century)
                return CampaignReviewMilestone.CenturyReview;
            return CampaignReviewMilestone.None;
        }

        public bool IsReviewDue(CampaignReviewMilestone milestone, GameDate today)
        {
            if (!StartKnown || !today.IsValid)
                return false;
            switch (milestone)
            {
                case CampaignReviewMilestone.StageReview:
                    return TryGetStageReviewDate(out GameDate stage) && today >= stage;
                case CampaignReviewMilestone.CenturyReview:
                    return TryGetCenturyReviewDate(out GameDate century) && today >= century;
                default:
                    return false;
            }
        }

        /// <summary>Completed whole campaign years at the supplied date; 0 when unknown.</summary>
        public int CompletedYears(GameDate today)
        {
            if (!StartKnown || !today.IsValid || today < StartDate)
                return 0;
            int years = today.Year - StartDate.Year;
            if (years <= 0)
                return 0;
            if (!TryGetAnniversary(StartDate, years, out GameDate anniversary) || today < anniversary)
                years--;
            return years < 0 ? 0 : years;
        }

        /// <summary>
        /// Acknowledges exactly one due review. Repeating the same acknowledgement, or
        /// acknowledging before the anniversary, is rejected and returns this same state.
        /// </summary>
        public CampaignReviewResult Acknowledge(CampaignReviewMilestone milestone, GameDate today)
        {
            if (milestone == CampaignReviewMilestone.None)
                return Failure("当前没有待确认的家族评价。");
            if (!StartKnown)
                return Failure("旧档开局日期未知：不判定时代评价或百年名门。");
            if (milestone == CampaignReviewMilestone.StageReview && StageReviewAcknowledged)
                return Failure("五十五周年阶段评价已经确认过。");
            if (milestone == CampaignReviewMilestone.CenturyReview && CenturyReviewAcknowledged)
                return Failure("百年名门评定已经确认过。");
            if (!IsReviewDue(milestone, today))
                return Failure(milestone == CampaignReviewMilestone.StageReview
                    ? "五十五周年阶段评价尚未到达。"
                    : "百年名门评定尚未到达。");

            var next = new CampaignProgress(true, StartDate, OriginId,
                StageReviewAcknowledged || milestone == CampaignReviewMilestone.StageReview,
                CenturyReviewAcknowledged || milestone == CampaignReviewMilestone.CenturyReview);
            return new CampaignReviewResult(true,
                milestone == CampaignReviewMilestone.StageReview
                    ? "已确认五十五周年阶段评价：家族继续经营。"
                    : "已确认百年名门评定。",
                next);
        }

        /// <summary>
        /// A campaign that names an authored origin must start on that scenario's fixed
        /// date. The catalog is the authoritative link between the two, so an unrelated
        /// date would make the origin and the anniversary schedule disagree.
        /// </summary>
        private static string AuthoredStartMismatch(GameDate startDate, CampaignOrigin origin) =>
            "战役开局日期与剧本固定开局日期不一致：" + startDate + " ≠ " + origin.StartDate +
            "（" + origin.Id + "）。";

        private CampaignReviewResult Failure(string message) =>
            new CampaignReviewResult(false, message, this);
    }
}
