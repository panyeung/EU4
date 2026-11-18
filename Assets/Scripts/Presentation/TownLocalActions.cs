using System.Collections.Generic;
using System.Text;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;

namespace SengokuChronicle.Presentation
{
    /// <summary>
    /// Read-only summary of the work the current town can start or continue. Every row
    /// restates an existing domain gate at the location the gate already requires, so the
    /// town page never claims an action that the mission board would refuse. The summary
    /// owns wording only: no rule, resource, route or saved field is added here.
    /// </summary>
    public static class TownLocalActions
    {
        public enum Kind
        {
            SupplyShortage,
            SealedLetter,
            MarketTally,
            EscortCommission,
            TacticalMission
        }

        public readonly struct Entry
        {
            public Kind Kind { get; }
            public string Label { get; }

            public Entry(Kind kind, string label)
            {
                Kind = kind;
                Label = label;
            }
        }

        public const string Header = "当地委托";
        public const string Empty = "当前暂无可办理的本地委托";
        public const string Unavailable = "当前地点不可用";

        private static readonly Kind[] AllKinds = (Kind[])System.Enum.GetValues(typeof(Kind));

        /// <summary>
        /// One activity kind per card. The town page sizes its card area from this count, so
        /// adding a kind grows the area instead of clipping the last card.
        /// </summary>
        public static int KindCount => AllKinds.Length;

        public static IReadOnlyList<Entry> GetAvailable(GameSession session)
        {
            if (session == null || session.WorldClock == null)
                return new Entry[0];

            LocationId location = session.CurrentLocation;
            var entries = new List<Entry>(KindCount);
            AddSupplyShortage(session, location, entries);
            AddSealedLetter(session, location, entries);
            AddMarketTally(session, location, entries);
            AddEscortCommission(session, location, entries);
            AddTacticalMission(session, entries);
            return entries.AsReadOnly();
        }

        /// <summary>Multi-line block for the town page's local-action column.</summary>
        public static string Describe(GameSession session)
        {
            if (session == null)
                return $"{Header}\n{Unavailable}";

            IReadOnlyList<Entry> entries = GetAvailable(session);

            var text = new StringBuilder(Header);
            if (entries.Count == 0)
            {
                text.Append('\n').Append(Empty);
            }
            else
            {
                for (int i = 0; i < entries.Count; i++)
                    text.Append('\n').Append(entries[i].Label);
            }
            return text.ToString();
        }

        /// <summary>
        /// Two-line summary for a town page that has no room for activity cards. The legacy
        /// summary label is 80px tall, so it reports how many activities are available
        /// instead of listing them: five labels would overflow the label and be clipped.
        /// </summary>
        public static string DescribeCompact(GameSession session)
        {
            if (session == null)
                return $"{Header}\n{Unavailable}";

            IReadOnlyList<Entry> entries = GetAvailable(session);
            return entries.Count == 0 ? $"{Header}\n{Empty}" : $"{Header}\n{entries.Count} 项可办理";
        }

        private static void AddSupplyShortage(GameSession session, LocationId location, List<Entry> entries)
        {
            switch (session.SupplyShortageStage)
            {
                case SupplyShortageStage.NotAccepted when location == LocationId.Kiyosu:
                    entries.Add(new Entry(Kind.SupplyShortage, "补给短缺：领取"));
                    break;
                case SupplyShortageStage.Accepted when location == LocationId.Atsuta:
                    entries.Add(new Entry(Kind.SupplyShortage, "补给短缺：调查"));
                    break;
                case SupplyShortageStage.Investigated when location == LocationId.Atsuta:
                    entries.Add(new Entry(Kind.SupplyShortage, "补给短缺：交涉"));
                    break;
                case SupplyShortageStage.Negotiated when location == LocationId.Kiyosu:
                    entries.Add(new Entry(Kind.SupplyShortage, "补给短缺：报告"));
                    break;
            }
        }

        private static void AddSealedLetter(GameSession session, LocationId location, List<Entry> entries)
        {
            if (session.SealedLetterStage == SealedLetterStage.NotAccepted && location == LocationId.Kiyosu)
                entries.Add(new Entry(Kind.SealedLetter, "密信：领取"));
            else if (session.SealedLetterStage == SealedLetterStage.Accepted && location == LocationId.Nagoya)
                entries.Add(new Entry(Kind.SealedLetter, "密信：交付"));
        }

        private static void AddMarketTally(GameSession session, LocationId location, List<Entry> entries)
        {
            switch (session.MarketTallyStage)
            {
                case MarketTallyStage.NotAccepted when location == LocationId.Nagoya:
                    entries.Add(new Entry(Kind.MarketTally, "市集账目：领取"));
                    break;
                case MarketTallyStage.Accepted when location == LocationId.Kiyosu:
                    entries.Add(new Entry(Kind.MarketTally, "市集账目：调查"));
                    break;
                case MarketTallyStage.Investigated when location == LocationId.Nagoya:
                    entries.Add(new Entry(Kind.MarketTally, "市集账目：报告"));
                    break;
            }
        }

        private static void AddEscortCommission(GameSession session, LocationId location, List<Entry> entries)
        {
            if (location != LocationId.Kiyosu || session.CommissionStage != ShadowEscortStage.NotAccepted)
                return;
            int investment = ShadowEscortCommission.InvestmentFundsFor(session.CurrentFamilyCreed);
            if (session.Industry.Resources.Funds >= investment)
                entries.Add(new Entry(Kind.EscortCommission, "护送委托：领取"));
        }

        private static void AddTacticalMission(GameSession session, List<Entry> entries)
        {
            // The gate already requires the clan seat, two eligible operatives and supplies.
            if (session.CanBeginTacticalMission)
                entries.Add(new Entry(Kind.TacticalMission, "潜入密令：开始"));
        }
    }
}
