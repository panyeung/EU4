using System;

namespace SengokuChronicle.Domain
{
    /// <summary>
    /// 游戏日期值类型：年/月/日，使用简化前推公历（proleptic Gregorian）。
    /// 支持范围：1560-01-01 ~ 2000-12-31（含）。
    /// 不可变结构，不持有 Unity 引用，可独立测试。
    /// </summary>
    public readonly struct GameDate : IEquatable<GameDate>, IComparable<GameDate>
    {
        public const int MinYear = 1560;
        public const int MaxYear = 2000;

        public readonly int Year;
        public readonly int Month;
        public readonly int Day;

        public GameDate(int year, int month, int day)
        {
            Year = year;
            Month = month;
            Day = day;
        }

        /// <summary>是否为合法且在支持范围内的日历日期。</summary>
        public static bool IsValidDate(int year, int month, int day)
        {
            if (year < MinYear || year > MaxYear)
                return false;
            if (month < 1 || month > 12)
                return false;
            if (day < 1 || day > DaysInMonth(year, month))
                return false;
            return true;
        }

        /// <summary>构造一个合法日期；非法输入抛出 ArgumentOutOfRangeException。</summary>
        public static GameDate Parse(int year, int month, int day)
        {
            if (!IsValidDate(year, month, day))
                throw new ArgumentOutOfRangeException($"非法日期：{year:0000}-{month:00}-{day:00}");
            return new GameDate(year, month, day);
        }

        /// <summary>尝试解析；非法返回 false 且 out 为 default。</summary>
        public static bool TryParse(int year, int month, int day, out GameDate result)
        {
            if (IsValidDate(year, month, day))
            {
                result = new GameDate(year, month, day);
                return true;
            }
            result = default;
            return false;
        }

        /// <summary>便捷：仅校验合法性，返回 bool。</summary>
        public static bool TryParse(int year, int month, int day)
        {
            return IsValidDate(year, month, day);
        }

        /// <summary>是否为合法日期（在支持范围内）。</summary>
        public bool IsValid => IsValidDate(Year, Month, Day);

        /// <summary>推进到下一天。若已达 2000-12-31 则返回 false（不动）。</summary>
        public static bool TryNext(GameDate date, out GameDate next)
        {
            next = default;
            if (date.Year == MaxYear && date.Month == 12 && date.Day == 31)
                return false;

            int year = date.Year;
            int month = date.Month;
            int day = date.Day;
            int dim = DaysInMonth(year, month);

            if (day < dim)
            {
                day = day + 1;
            }
            else
            {
                day = 1;
                if (month < 12)
                    month = month + 1;
                else
                {
                    month = 1;
                    year = year + 1;
                }
            }

            next = new GameDate(year, month, day);
            return true;
        }

        /// <summary>推进 N 天（N>0）。超出范围返回 false，out 保持 default。</summary>
        public static bool TryAddDays(GameDate date, int days, out GameDate result)
        {
            result = default;
            if (days <= 0)
                return false;

            long ordinal = ToOrdinalDays(date) + days;
            long maxOrdinal = ToOrdinalDays(new GameDate(MaxYear, 12, 31));
            if (ordinal > maxOrdinal)
                return false;

            FromOrdinalDays(ordinal, out int y, out int m, out int d);
            result = new GameDate(y, m, d);
            return true;
        }

        /// <summary>计算从 0001-01-01 起的天序（简化前推公历，闭表单）。O(years)。</summary>
        private static long ToOrdinalDays(GameDate d)
        {
            long days = 0;
            for (int y = 1; y < d.Year; y++)
                days += IsLeapYear(y) ? 366L : 365L;
            for (int m = 1; m < d.Month; m++)
                days += DaysInMonth(d.Year, m);
            days += d.Day - 1;
            return days;
        }

        /// <summary>从天序反推日期（二分查找年份）。O(log years)。</summary>
        private static void FromOrdinalDays(long ordinal, out int year, out int month, out int day)
        {
            int lo = MinYear, hi = MaxYear, ans = MinYear;
            while (lo <= hi)
            {
                int mid = lo + ((hi - lo) >> 1);
                if (ToOrdinalDays(new GameDate(mid, 1, 1)) <= ordinal)
                { ans = mid; lo = mid + 1; }
                else
                { hi = mid - 1; }
            }
            year = ans;

            long remaining = ordinal - ToOrdinalDays(new GameDate(year, 1, 1));
            month = 1;
            while (remaining >= DaysInMonth(year, month))
            {
                remaining -= DaysInMonth(year, month);
                month++;
            }
            day = (int)remaining + 1;
        }

        /// <summary>某年某月的天数（含闰年判断）。非法月份返回 0。</summary>
        public static int DaysInMonth(int year, int month)
        {
            switch (month)
            {
                case 1: case 3: case 5: case 7: case 8: case 10: case 12:
                    return 31;
                case 4: case 6: case 9: case 11:
                    return 30;
                case 2:
                    return IsLeapYear(year) ? 29 : 28;
                default:
                    return 0;
            }
        }

        /// <summary>简化前推公历闰年规则。</summary>
        public static bool IsLeapYear(int year)
        {
            if (year % 400 == 0)
                return true;
            if (year % 100 == 0)
                return false;
            return year % 4 == 0;
        }

        public bool Equals(GameDate other)
        {
            return Year == other.Year && Month == other.Month && Day == other.Day;
        }

        public override bool Equals(object obj)
        {
            return obj is GameDate gd && Equals(gd);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Year, Month, Day);
        }

        public static bool operator ==(GameDate a, GameDate b) => a.Equals(b);
        public static bool operator !=(GameDate a, GameDate b) => !a.Equals(b);

        public int CompareTo(GameDate other)
        {
            int c = Year.CompareTo(other.Year);
            if (c != 0) return c;
            c = Month.CompareTo(other.Month);
            if (c != 0) return c;
            return Day.CompareTo(other.Day);
        }

        public static bool operator <(GameDate a, GameDate b) => a.CompareTo(b) < 0;
        public static bool operator >(GameDate a, GameDate b) => a.CompareTo(b) > 0;
        public static bool operator <=(GameDate a, GameDate b) => a.CompareTo(b) <= 0;
        public static bool operator >=(GameDate a, GameDate b) => a.CompareTo(b) >= 0;

        public override string ToString()
        {
            return $"{Year:0000}-{Month:00}-{Day:00}";
        }
    }
}
