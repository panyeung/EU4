namespace SengokuChronicle.Domain
{
    using System;

    public enum SealedLetterStage
    {
        NotAccepted,
        Accepted,
        RewardClaimed
    }

    public readonly struct SealedLetterActionResult
    {
        public SealedLetterActionResult(bool succeeded, SealedLetterStage stage, int merit, string feedback)
        {
            Succeeded = succeeded;
            Stage = stage;
            Merit = merit;
            Feedback = feedback ?? string.Empty;
        }

        public bool Succeeded { get; }
        public SealedLetterStage Stage { get; }
        public int Merit { get; }
        public string Feedback { get; }
    }

    public sealed class SealedLetterMission
    {
        public const int RewardMerit = 5;

        public SealedLetterStage Stage { get; private set; }
        public int Merit { get; private set; }

        public SealedLetterMission()
        {
        }

        private SealedLetterMission(SealedLetterStage stage, int merit)
        {
            Stage = stage;
            Merit = merit;
        }

        /// <summary>
        /// Rebuilds a mission from saved stage data. Merit is derived from the stage,
        /// so a completed mission can never restore a different reward amount.
        /// Returns null for an undefined stage.
        /// </summary>
        public static SealedLetterMission TryRestore(SealedLetterStage stage)
        {
            if (!Enum.IsDefined(typeof(SealedLetterStage), stage))
                return null;

            return new SealedLetterMission(
                stage,
                stage == SealedLetterStage.RewardClaimed ? RewardMerit : 0);
        }

        public SealedLetterActionResult TryAccept(LocationId location)
        {
            if (Stage != SealedLetterStage.NotAccepted)
                return Result(false, "已领取过密信任务。");
            if (location != LocationId.Kiyosu)
                return Result(false, "请在清洲向橘 すず领取密信。");

            Stage = SealedLetterStage.Accepted;
            return Result(true, "已领取密信，请送往那古野交给星野 彩。");
        }

        public SealedLetterActionResult TryDeliver(LocationId location)
        {
            if (Stage != SealedLetterStage.Accepted)
                return Result(false, "请先领取密信，且奖励只能领取一次。");
            if (location != LocationId.Nagoya)
                return Result(false, "请在那古野向星野 彩交付密信。");

            Stage = SealedLetterStage.RewardClaimed;
            Merit = RewardMerit;
            return Result(true, "密信已送达，获得5功绩。");
        }

        private SealedLetterActionResult Result(bool succeeded, string feedback)
        {
            return new SealedLetterActionResult(succeeded, Stage, Merit, feedback);
        }
    }
}
