namespace SengokuChronicle.Domain
{
    public enum SupplyShortageStage
    {
        NotAccepted,
        Accepted,
        Investigated,
        Negotiated,
        RewardClaimed
    }

    public readonly struct SupplyShortageActionResult
    {
        public SupplyShortageActionResult(bool succeeded, SupplyShortageStage stage, int merit, string feedback)
        {
            Succeeded = succeeded;
            Stage = stage;
            Merit = merit;
            Feedback = feedback ?? string.Empty;
        }

        public bool Succeeded { get; }
        public SupplyShortageStage Stage { get; }
        public int Merit { get; }
        public string Feedback { get; }
    }

    /// <summary>One supply shortage mission; actions never advance time or move the player.</summary>
    public sealed class SupplyShortageMission
    {
        public SupplyShortageStage Stage { get; private set; }
        public int Merit { get; private set; }

        public SupplyShortageActionResult TryAccept(LocationId location)
        {
            if (Stage != SupplyShortageStage.NotAccepted)
                return Result(false, "已领取过补给短缺任务。");
            if (location != LocationId.Kiyosu)
                return Result(false, "请在清洲领取任务。");

            Stage = SupplyShortageStage.Accepted;
            return Result(true, "已领取补给短缺任务，请前往热田调查。");
        }

        public SupplyShortageActionResult TryInvestigate(LocationId location)
        {
            if (Stage != SupplyShortageStage.Accepted)
                return Result(false, "请先领取任务，且调查只能完成一次。");
            if (location != LocationId.Atsuta)
                return Result(false, "请在热田调查补给短缺。");

            Stage = SupplyShortageStage.Investigated;
            return Result(true, "调查完成，请与黑田伊织交涉补给。");
        }

        public SupplyShortageActionResult TryNegotiate(LocationId location, CharacterId merchantId)
        {
            if (Stage != SupplyShortageStage.Investigated)
                return Result(false, "请先完成调查，且交涉只能完成一次。");
            if (location != LocationId.Atsuta || merchantId != CharacterId.KurodaIori)
                return Result(false, "请在热田与黑田伊织交涉。");

            Stage = SupplyShortageStage.Negotiated;
            return Result(true, "交涉完成，请返回清洲报告。");
        }

        public SupplyShortageActionResult TryReport(LocationId location)
        {
            if (Stage != SupplyShortageStage.Negotiated)
                return Result(false, "请先完成交涉，且奖励只能领取一次。");
            if (location != LocationId.Kiyosu)
                return Result(false, "请返回清洲报告。");

            Stage = SupplyShortageStage.RewardClaimed;
            Merit += 10;
            return Result(true, "报告完成，获得10功绩。");
        }

        private SupplyShortageActionResult Result(bool succeeded, string feedback)
        {
            return new SupplyShortageActionResult(succeeded, Stage, Merit, feedback);
        }
    }
}
