using System;
using System.Collections.Generic;

namespace SengokuChronicle.Presentation
{
    /// <summary>
    /// Builds the household "本月" body from the live month sources. The settlement
    /// report and the industry message repeat the same text after a settlement, so the
    /// page used to draw that sentence twice on a fixed 26 px pitch and the copies
    /// overlapped. Lines are de-duplicated here, in order, and rendered as one flowing
    /// block instead of fixed-pitch rows.
    /// </summary>
    public static class HouseholdMonthSummary
    {
        public static IReadOnlyList<string> Distinct(IReadOnlyList<string> lines)
        {
            var result = new List<string>();
            if (lines == null)
                return result;
            for (int i = 0; i < lines.Count; i++)
            {
                string line = lines[i]?.Trim();
                if (string.IsNullOrEmpty(line))
                    continue;
                bool duplicate = false;
                for (int j = 0; j < result.Count; j++)
                {
                    if (string.Equals(result[j], line, StringComparison.Ordinal))
                    {
                        duplicate = true;
                        break;
                    }
                }
                if (!duplicate)
                    result.Add(line);
            }
            return result;
        }

        /// <summary>Single wrapped body; each distinct source keeps its own paragraph.</summary>
        public static string Join(IReadOnlyList<string> lines) =>
            string.Join("\n", Distinct(lines));
    }
}
