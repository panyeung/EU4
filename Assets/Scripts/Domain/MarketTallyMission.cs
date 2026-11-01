namespace SengokuChronicle.Domain
{
    using System;

    public enum MarketTallyStage
    {
        NotAccepted,
        Accepted,
        Investigated,
        RewardClaimed
    }

    public readonly struct MarketTallyActionResult
    {
        public MarketTallyActionResult(bool succeeded, MarketTallyStage stage, int merit, string feedback)
        {
            Succeeded = succeeded;
            Stage = stage;
            Merit = merit;
            Feedback = feedback ?? string.Empty;
        }

        public bool Succeeded { get; }
        public MarketTallyStage Stage { get; }
        public int Merit { get; }
        public string Feedback { get; }
    }

    public sealed class MarketTallyMission
    {
        public const int RewardMerit = 5;

        public MarketTallyStage Stage { get; private set; }
        public int Merit { get; private set; }

        public MarketTallyMission()
        {
        }

        private MarketTallyMission(MarketTallyStage stage, int merit)
        {
            Stage = stage;
            Merit = merit;
        }

        /// <summary>
        /// Rebuilds a mission from saved stage data. Merit is derived from the stage,
        /// so a completed mission can never restore a different reward amount.
        /// Returns null for an undefined stage.
        /// </summary>
        public static MarketTallyMission TryRestore(MarketTallyStage stage)
        {
            if (!Enum.IsDefined(typeof(MarketTallyStage), stage))
                return null;

            return new MarketTallyMission(
                stage,
                stage == MarketTallyStage.RewardClaimed ? RewardMerit : 0);
        }

        public MarketTallyActionResult TryAccept(LocationId location)
        {
            if (Stage != MarketTallyStage.NotAccepted)
                return Result(false, "已领取过市集账目任务。");
            if (location != LocationId.Nagoya)
                return Result(false, "请在那古野向酒井 次郎兵衛领取任务。");

            Stage = MarketTallyStage.Accepted;
            return Result(true, "市集账目任务已领取，请前往清洲调查。");
        }

        public MarketTallyActionResult TryInvestigate(LocationId location)
        {
            if (Stage != MarketTallyStage.Accepted)
                return Result(false, "请先领取任务，且调查只能完成一次。");
            if (location != LocationId.Kiyosu)
                return Result(false, "请在清洲向森 源左衛門调查市集账目。");

            Stage = MarketTallyStage.Investigated;
            return Result(true, "账目调查完成，请返回那古野报告。");
        }

        public MarketTallyActionResult TryReport(LocationId location)
        {
            if (Stage != MarketTallyStage.Investigated)
                return Result(false, "请先完成调查，且奖励只能领取一次。");
            if (location != LocationId.Nagoya)
                return Result(false, "请在那古野向酒井 次郎兵衛报告。");

            Stage = MarketTallyStage.RewardClaimed;
            Merit = RewardMerit;
            return Result(true, "市集账目报告完成，获得5功绩。");
        }

        private MarketTallyActionResult Result(bool succeeded, string feedback)
        {
            return new MarketTallyActionResult(succeeded, Stage, Merit, feedback);
        }
    }
}
