using System;
using System.Collections.Generic;

namespace SengokuChronicle.Presentation
{
    /// <summary>
    /// 从当月实时数据源拼出家宅「本月」正文。结算报告与产业消息在结算后会重复
    /// 同一段文字，页面原先按固定 26 px 行距把这句话画了两遍，两份内容互相压住。
    /// 这里按原有顺序去重，各行拼成一段连续文字，而不是固定行距的表格行。
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

        /// <summary>单段自动换行正文；每个不同来源各自成段。</summary>
        public static string Join(IReadOnlyList<string> lines) =>
            string.Join("\n", Distinct(lines));
    }
}
