using System;
using NUnit.Framework;
using SengokuChronicle.Domain;
using SengokuChronicle.Application;

namespace SengokuChronicle.Tests.EditMode
{
    /// <summary>
    /// M1 WorldClock 日期边界测试（EditMode）。
    /// 约定：Try 风格推进——失败时不抛异常、不改状态，返回 false。
    /// </summary>
    public class WorldClockTests
    {
        /// <inheritdoc cref="WorldClock.NewGameDate"/> — 新局起始日。
        private static readonly GameDate Start = new GameDate(1560, 4, 1);

        private static WorldClock NewClock()
        {
            return new WorldClock();
        }

        // 1. 初始日期 1560-04-01，推进一次到 1560-04-02。
        [Test]
        public void NewGame_StartsAt1560April1()
        {
            var clock = NewClock();
            Assert.That(clock.CurrentDate, Is.EqualTo(Start),
                "新局日期必须严格为 1560-04-01");
            Assert.That(WorldClock.NewGameDate, Is.EqualTo(Start));
        }

        [Test]
        public void AdvanceOneDay_FromApril1_GoesToApril2()
        {
            var clock = NewClock();
            Assert.That(clock.AdvanceOneDay(), Is.True, "从 1560-04-01 推进一天必须成功");
            Assert.That(clock.CurrentDate, Is.EqualTo(new GameDate(1560, 4, 2)));
        }

        [Test]
        public void AdvanceOneDay_IsRepeatable()
        {
            var clock = NewClock();
            Assert.That(clock.AdvanceOneDay(), Is.True);
            Assert.That(clock.AdvanceOneDay(), Is.True);
            Assert.That(clock.AdvanceOneDay(), Is.True);
            Assert.That(clock.CurrentDate, Is.EqualTo(new GameDate(1560, 4, 4)));
        }

        // 2. 月末：1560-04-30 -> 1560-05-01。
        [Test]
        public void AdvanceOneDay_April30_RollsToMay1()
        {
            var clock = NewClock();
            Assert.That(clock.AdvanceDays(29), Is.True, "辅助推进 29 天到 4 月 30 日必须成功");
            Assert.That(clock.CurrentDate, Is.EqualTo(new GameDate(1560, 4, 30)));

            Assert.That(clock.AdvanceOneDay(), Is.True, "4 月 30 日推进必须成功");
            Assert.That(clock.CurrentDate, Is.EqualTo(new GameDate(1560, 5, 1)),
                "月末必须进入 5 月 1 日");
        }

        // 3. 年末：1560-12-31 -> 1561-01-01。
        [Test]
        public void AdvanceOneDay_December31_RollsToJanuary1NextYear()
        {
            var clock = new WorldClock(new GameDate(1560, 12, 31));
            Assert.That(clock.AdvanceOneDay(), Is.True, "12 月 31 日推进必须成功");
            Assert.That(clock.CurrentDate, Is.EqualTo(new GameDate(1561, 1, 1)),
                "年末必须进入次年 1 月 1 日");
        }

        // 4a. 闰年：1560-02-28 -> 1560-02-29 -> 1560-03-01。
        [Test]
        public void LeapYear_1560_FebruaryHas29Days()
        {
            var clock = new WorldClock(new GameDate(1560, 2, 28));
            Assert.That(clock.AdvanceOneDay(), Is.True, "1560 是闰年，2 月 28 日推进必须成功");
            Assert.That(clock.CurrentDate, Is.EqualTo(new GameDate(1560, 2, 29)));

            Assert.That(clock.AdvanceOneDay(), Is.True, "2 月 29 日推进必须成功");
            Assert.That(clock.CurrentDate, Is.EqualTo(new GameDate(1560, 3, 1)),
                "闰年 2 月 29 日之后必须是 3 月 1 日");
        }

        // 4b. 非闰年：1561-02-28 -> 1561-03-01。
        [Test]
        public void NonLeapYear_1561_FebruaryHas28Days()
        {
            var clock = new WorldClock(new GameDate(1561, 2, 28));
            Assert.That(clock.AdvanceOneDay(), Is.True, "非闰年 2 月 28 日推进必须成功");
            Assert.That(clock.CurrentDate, Is.EqualTo(new GameDate(1561, 3, 1)),
                "非闰年 2 月 28 日之后必须是 3 月 1 日（跳过 2 月 29 日）");
        }

        // 闰世纪年规则（简化前推公历）：2000 是闰年。
        [Test]
        public void CenturyYears_2000IsLeap()
        {
            Assert.That(GameDate.IsLeapYear(1560), Is.True);
            Assert.That(GameDate.IsLeapYear(1561), Is.False);
            Assert.That(GameDate.IsLeapYear(2000), Is.True, "2000 可被 400 整除，应为闰年");
            Assert.That(GameDate.IsLeapYear(1700), Is.False, "1700 被 100 整除但非 400，应为平年");
            Assert.That(GameDate.DaysInMonth(1560, 2), Is.EqualTo(29));
            Assert.That(GameDate.DaysInMonth(1561, 2), Is.EqualTo(28));
            Assert.That(GameDate.DaysInMonth(2000, 2), Is.EqualTo(29));

            var clock = new WorldClock(new GameDate(2000, 2, 28));
            Assert.That(clock.AdvanceOneDay(), Is.True, "2000 是闰年，2 月 28 日推进必须成功");
            Assert.That(clock.CurrentDate, Is.EqualTo(new GameDate(2000, 2, 29)));
        }

        // 5. 零/负天数输入必须被拒绝，且状态不变。
        [Test]
        public void AdvanceDays_Zero_IsRejectedWithoutMutation()
        {
            var clock = NewClock();
            Assert.That(clock.AdvanceDays(0), Is.False, "推进 0 天必须被拒绝");
            Assert.That(clock.CurrentDate, Is.EqualTo(Start), "状态必须保持不变");
        }

        [Test]
        public void AdvanceDays_Negative_IsRejectedWithoutMutation()
        {
            var clock = NewClock();
            Assert.That(clock.AdvanceDays(-1), Is.False, "推进负数天必须被拒绝");
            Assert.That(clock.AdvanceDays(-9999), Is.False, "大负数也必须被拒绝");
            Assert.That(clock.CurrentDate, Is.EqualTo(Start), "状态必须保持不变");
        }

        // 6. 超出支持范围（最大 2000-12-31）的推进必须失败且不改状态。
        [Test]
        public void AdvanceDays_BeyondSupportedMaximum_FailsWithoutMutation()
        {
            var clock = new WorldClock(new GameDate(2000, 12, 31));
            Assert.That(clock.AdvanceOneDay(), Is.False,
                "已到支持的最大日期 2000-12-31，再推进必须失败");
            Assert.That(clock.CurrentDate, Is.EqualTo(new GameDate(2000, 12, 31)), "状态必须保持不变");

            Assert.That(clock.AdvanceDays(365), Is.False, "跨出支持范围的多日推进必须失败");
            Assert.That(clock.CurrentDate, Is.EqualTo(new GameDate(2000, 12, 31)));
        }

        [Test]
        public void AdvanceDays_HugeCount_RejectedWithoutMutation()
        {
            var clock = NewClock();
            Assert.That(clock.AdvanceDays(int.MaxValue), Is.False,
                "超过支持范围的巨大天数必须被拒绝（不应阻塞）");
            Assert.That(clock.CurrentDate, Is.EqualTo(Start), "状态必须保持不变");
        }

        [Test]
        public void AdvanceDays_LargeJumpThatStaysInRange_Succeeds()
        {
            var clock = NewClock();
            // 1560-04-01 + 365 天 = 1561-04-01（1560 为闰年，含 2 月 29）。
            Assert.That(clock.AdvanceDays(365), Is.True, "推进 365 天仍在支持范围内");
            Assert.That(clock.CurrentDate, Is.EqualTo(new GameDate(1561, 4, 1)));
        }

        // 7. GameDate 输入校验：非法日期必须安全失败，不抛未处理异常。
        [Test]
        public void GameDate_InvalidDay_IsNotValid()
        {
            Assert.That(GameDate.TryParse(1560, 4, 31), Is.False, "4 月没有 31 日");
            Assert.That(GameDate.TryParse(1561, 2, 29), Is.False, "非闰年没有 2 月 29 日");
            Assert.That(GameDate.TryParse(1560, 13, 1), Is.False, "没有 13 月");
            Assert.That(GameDate.TryParse(1560, 0, 1), Is.False, "没有 0 月");
            Assert.That(GameDate.TryParse(1560, 1, 0), Is.False, "没有 0 日");
            Assert.That(GameDate.TryParse(1560, 1, 40), Is.False, "日期不能超过当月天数");
        }

        [Test]
        public void GameDate_OutOfYearRange_IsNotValid()
        {
            Assert.That(GameDate.TryParse(1559, 12, 31), Is.False, "早于支持范围（1560 起）必须无效");
            Assert.That(GameDate.TryParse(2001, 1, 1), Is.False, "晚于支持范围（至 2000）必须无效");
            Assert.That(GameDate.TryParse(0, 1, 1), Is.False, "年份 0 必须无效");
            Assert.That(GameDate.TryParse(999999, 1, 1), Is.False, "超大年份必须无效");
        }

        [Test]
        public void WorldClock_TryCreate_InvalidDate_ReturnsNullWithoutMutation()
        {
            // 通过原始构造器创建非法日期（不抛），TryCreate 应返回 null。
            var bad = new GameDate(1560, 2, 30);          // 原始构造函数不校验
            Assert.That(bad.IsValid, Is.False, "2 月 30 日原应非法");

            var invalid = WorldClock.TryCreate(bad);
            Assert.That(invalid, Is.Null, "非法日期构造必须返回 null 而不是抛异常");

            // 已有合法实例不受影响。
            var clock = NewClock();
            Assert.That(clock.CurrentDate, Is.EqualTo(Start), "已有实例状态不能被非法构造尝试破坏");
        }

        [Test]
        public void WorldClock_Constructor_InvalidDate_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new WorldClock(new GameDate(1560, 2, 30)));
            Assert.Throws<ArgumentOutOfRangeException>(() => new WorldClock(new GameDate(1559, 4, 1)));
            Assert.Throws<ArgumentOutOfRangeException>(() => new WorldClock(new GameDate(2001, 1, 1)));

            var clock = NewClock();
            Assert.That(clock.CurrentDate, Is.EqualTo(Start), "抛出后已有实例仍需保持不变");
        }

        // GameSession 集成测试（M1 范围：只持有时钟状态）。
        [Test]
        public void GameSession_NewGame_StartsAt1560April1()
        {
            var session = SengokuChronicle.Application.GameSession.NewGame();
            Assert.That(session, Is.Not.Null);
            Assert.That(session.WorldClock.CurrentDate, Is.EqualTo(Start));
        }

        [Test]
        public void GameSession_AdvanceOneDay_UpdatesClock()
        {
            var session = SengokuChronicle.Application.GameSession.NewGame();
            Assert.That(session.AdvanceOneDay(), Is.True);
            Assert.That(session.WorldClock.CurrentDate, Is.EqualTo(new GameDate(1560, 4, 2)));
        }

        [Test]
        public void GameSession_AdvanceDays_ZeroOrNegative_LeavesClockUnchanged()
        {
            var session = SengokuChronicle.Application.GameSession.NewGame();
            Assert.That(session.AdvanceDays(0), Is.False);
            Assert.That(session.AdvanceDays(-3), Is.False);
            Assert.That(session.WorldClock.CurrentDate, Is.EqualTo(Start));
        }
    }
}
