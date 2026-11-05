using System;

namespace SengokuChronicle.Domain
{
    /// <summary>
    /// 游戏世界时钟：纯 C# 日期状态与推进规则。
    /// 起始日固定为 1560-04-01，使用简化前推公历（proleptic Gregorian）。
    /// 支持范围：1560-01-01 ~ 2000-12-31（含）。
    /// 推进失败时返回 false，且绝不修改已有状态。
    /// </summary>
    public sealed class WorldClock
    {
        /// <summary>支持的最小日期。</        /// </summary>
        public static readonly GameDate MinDate = new GameDate(1560, 1, 1);

        /// <summary>支持的最大日期。
        /// </summary>
        public static readonly GameDate MaxDate = new GameDate(2000, 12, 31);

        /// <summary>新游戏的起始日期。</summary>
        public static readonly GameDate NewGameDate = new GameDate(1560, 4, 1);

        private GameDate _current;

        public WorldClock()
            : this(NewGameDate)
        {
        }

        public WorldClock(GameDate startDate)
        {
            if (!startDate.IsValid)
                throw new ArgumentOutOfRangeException(nameof(startDate), $"非法起始日期：{startDate}。必须在 1560-01-01 ~ 2000-12-31 范围内。");

            _current = startDate;
        }

        /// <summary>当前游戏日期（只读）。</summary>
        public GameDate CurrentDate => _current;

        /// <summary>尝试创建一个时钟；非法日期返回 null（不抛异常）。
        /// 适用于不需要异常流的构造场景。</summary>
        public static WorldClock TryCreate(GameDate startDate)
        {
            if (!startDate.IsValid)
                return null;
            return new WorldClock(startDate);
        }

        /// <summary>推进一天。成功返回 true；失败返回 false 且状态不变。</summary>
        public bool AdvanceOneDay()
        {
            return AdvanceDays(1);
        }

        /// <summary>推进多天。count 必须为正整数；失败返回 false 且状态不变。</summary>
        public bool AdvanceDays(int count)
        {
            if (count <= 0)
                return false;

            if (!GameDate.TryAddDays(_current, count, out GameDate candidate))
                return false;

            _current = candidate;
            return true;
        }

        /// <summary>
        /// 结束本月：推进到下一个日历月的 1 日（SC-004 月度回合）。
        /// 成功返回 true；下一月超出支持范围（1560-01-01 ~ 2000-12-31）时返回 false 且状态不变。
        /// 不影响既有的按天推进 API。
        /// </summary>
        public bool TryAdvanceToNextMonth()
        {
            int year = _current.Year;
            int month = _current.Month;
            if (month < 12)
            {
                month = month + 1;
            }
            else
            {
                month = 1;
                year = year + 1;
            }

            // 每月 1 日恒存在，TryParse 同时完成 1560~2000 支持范围校验。
            if (!GameDate.TryParse(year, month, 1, out GameDate candidate))
                return false;

            _current = candidate;
            return true;
        }

        /// <summary>
        /// Restores a previously valid date inside the same clock instance. Used only
        /// by the transactional month-settlement rollback; production time entry
        /// points never call it to move forward.
        /// </summary>
        internal bool TryRestoreDate(GameDate date)
        {
            if (!date.IsValid)
                return false;
            _current = date;
            return true;
        }

        public bool EqualsClock(WorldClock other)
        {
            if (other == null)
                return false;
            return _current == other._current;
        }
    }
}