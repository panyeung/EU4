using System;

namespace SengokuChronicle.Domain
{
    /// <summary>Zero-based month arithmetic shared by settlement, saves and validation.</summary>
    public static class IndustryCalendar
    {
        public static int MonthIndex(GameDate date) =>
            (int)((long)date.Year * 12L + (date.Month - 1));
    }
}
