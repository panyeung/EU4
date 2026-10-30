using System;
using SengokuChronicle.Domain;

namespace SengokuChronicle.Application
{
    /// <summary>
    /// 旅行预览结果（不可变值对象）。
    /// 仅由 WorldMapRules + GameDate 计算得出，不持有任何可变状态，
    /// 也不修改 GameSession 的日期或位置。可直接丢弃。
    /// </summary>
    public readonly struct TravelPreview
    {
        public LocationId Destination { get; }
        public string DestinationName { get; }
        public int TravelDays { get; }
        public GameDate EstimatedArrivalDate { get; }

        public TravelPreview(LocationId destination, string destinationName, int travelDays, GameDate estimatedArrivalDate)
        {
            Destination = destination;
            DestinationName = destinationName;
            TravelDays = travelDays;
            EstimatedArrivalDate = estimatedArrivalDate;
        }

        public override string ToString()
        {
            return $"TravelPreview(to={Destination}, name={DestinationName}, days={TravelDays}, arrive={EstimatedArrivalDate})";
        }
    }
}