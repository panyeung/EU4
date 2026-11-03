using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using SengokuChronicle.Domain;
using SengokuChronicle.Application;
using SengokuChronicle.Events;
using SengokuChronicle.Persistence;
using SengokuChronicle.Presentation;
using Object = UnityEngine.Object;

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

        // ===== SC-004 月末推进（结束月份 → 下月 1 日）=====

        private static GameDate MaxDate => new GameDate(2000, 12, 31);

        // 1. 主路径：1560-04-01 结束月份 → 1560-05-01（Play Mode 验收锚点）。
        [Test]
        public void TryAdvanceToNextMonth_FromNewGameDate_GoesToMayFirst()
        {
            var clock = NewClock();
            Assert.That(clock.TryAdvanceToNextMonth(), Is.True, "1560-04-01 结束月份必须成功");
            Assert.That(clock.CurrentDate, Is.EqualTo(new GameDate(1560, 5, 1)),
                "结束月份必须落在下月 1 日");
        }

        // 2. 月中任意日期同样落在下月 1 日。
        [Test]
        public void TryAdvanceToNextMonth_MidMonth_GoesToFirstOfNextMonth()
        {
            var clock = new WorldClock(new GameDate(1560, 4, 17));
            Assert.That(clock.TryAdvanceToNextMonth(), Is.True);
            Assert.That(clock.CurrentDate, Is.EqualTo(new GameDate(1560, 5, 1)));

            clock = new WorldClock(new GameDate(1560, 1, 31));
            Assert.That(clock.TryAdvanceToNextMonth(), Is.True);
            Assert.That(clock.CurrentDate, Is.EqualTo(new GameDate(1560, 2, 1)));
        }

        // 3. 年末滚动：12 月 → 次年 1 月 1 日。
        [Test]
        public void TryAdvanceToNextMonth_December_RollsToJanuaryNextYear()
        {
            var clock = new WorldClock(new GameDate(1560, 12, 1));
            Assert.That(clock.TryAdvanceToNextMonth(), Is.True);
            Assert.That(clock.CurrentDate, Is.EqualTo(new GameDate(1561, 1, 1)), "12 月结束月份必须跨年到 1 月 1 日");

            clock = new WorldClock(new GameDate(1560, 12, 31));
            Assert.That(clock.TryAdvanceToNextMonth(), Is.True);
            Assert.That(clock.CurrentDate, Is.EqualTo(new GameDate(1561, 1, 1)));
        }

        // 4. 闰年 2 月 29 日 / 平年 2 月 28 日 → 3 月 1 日（月末与天数无关）。
        [Test]
        public void TryAdvanceToNextMonth_February_GoesToMarchFirst()
        {
            var leap = new WorldClock(new GameDate(1560, 2, 29));   // 1560 闰年
            Assert.That(leap.TryAdvanceToNextMonth(), Is.True);
            Assert.That(leap.CurrentDate, Is.EqualTo(new GameDate(1560, 3, 1)), "闰年 2 月 29 日结束月份必须是 3 月 1 日");

            var common = new WorldClock(new GameDate(1561, 2, 28)); // 1561 平年
            Assert.That(common.TryAdvanceToNextMonth(), Is.True);
            Assert.That(common.CurrentDate, Is.EqualTo(new GameDate(1561, 3, 1)), "平年 2 月 28 日结束月份必须是 3 月 1 日");
        }

        // 5. 范围拒绝：下一月落在 2001 年及以后必须失败且状态不变。
        [Test]
        public void TryAdvanceToNextMonth_AtDecemberOfMaxYear_FailsWithoutMutation()
        {
            var clock = new WorldClock(new GameDate(2000, 12, 1));
            Assert.That(clock.TryAdvanceToNextMonth(), Is.False, "2000-12 之后是 2001-01，超出支持范围必须失败");
            Assert.That(clock.CurrentDate, Is.EqualTo(new GameDate(2000, 12, 1)), "失败后状态必须保持不变");

            clock = new WorldClock(MaxDate);
            Assert.That(clock.TryAdvanceToNextMonth(), Is.False, "最大日期结束月份必须失败");
            Assert.That(clock.CurrentDate, Is.EqualTo(MaxDate), "失败后状态必须保持不变");
        }

        // 6. 上限边界：2000-11 → 2000-12-01 仍成功（含月末恰好落在上限当月）。
        [Test]
        public void TryAdvanceToNextMonth_NovemberOfMaxYear_SucceedsToDecemberFirst()
        {
            var clock = new WorldClock(new GameDate(2000, 11, 30));
            Assert.That(clock.TryAdvanceToNextMonth(), Is.True);
            Assert.That(clock.CurrentDate, Is.EqualTo(new GameDate(2000, 12, 1)));
        }

        // 7. 全程推进到边界：1560-04-01 起连续 5288 次成功到达 2000-12-01，第 5289 次失败且不动。
        //    跨度 = 2000-1560 = 440 年 = 5280 个月，再加 4 月→12 月的 8 个月。
        [Test]
        public void TryAdvanceToNextMonth_RepeatedToEndOfRange_StopsCleanlyAtBoundary()
        {
            var clock = NewClock();
            int clicks = 0;
            while (clock.TryAdvanceToNextMonth())
            {
                clicks++;
                Assert.That(clicks, Is.LessThanOrEqualTo(5289), "推进次数不应超过理论月份数");
            }

            Assert.That(clicks, Is.EqualTo(5288), "1560-04 → 2000-12 共 5288 个月度回合");
            Assert.That(clock.CurrentDate, Is.EqualTo(new GameDate(2000, 12, 1)), "最后一次成功必须落在 2000-12-01");
            Assert.That(clock.TryAdvanceToNextMonth(), Is.False);
            Assert.That(clock.CurrentDate, Is.EqualTo(new GameDate(2000, 12, 1)));
        }

        // 8. 与按天推进互不干扰：月末后再走一天 = 下月 2 日；天推进后月末仍到下月 1 日。
        [Test]
        public void TryAdvanceToNextMonth_KeepsDayAdvancementSemantics()
        {
            var clock = NewClock();
            Assert.That(clock.TryAdvanceToNextMonth(), Is.True);
            Assert.That(clock.AdvanceOneDay(), Is.True);
            Assert.That(clock.CurrentDate, Is.EqualTo(new GameDate(1560, 5, 2)));

            clock = NewClock();
            Assert.That(clock.AdvanceOneDay(), Is.True);
            Assert.That(clock.TryAdvanceToNextMonth(), Is.True);
            Assert.That(clock.CurrentDate, Is.EqualTo(new GameDate(1560, 5, 1)),
                "月中（4 月 2 日）结束月份同样落在 5 月 1 日");
        }

        // 9. GameSession 包装：暴露同一月末规则。
        [Test]
        public void GameSession_TryAdvanceToNextMonth_UpdatesClock()
        {
            var session = SengokuChronicle.Application.GameSession.NewGame();
            Assert.That(session.TryAdvanceToNextMonth(), Is.True);
            Assert.That(session.WorldClock.CurrentDate, Is.EqualTo(new GameDate(1560, 5, 1)));
        }

        [Test]
        public void GameSession_TryAdvanceToNextMonth_AtMaxDate_FailsWithoutMutation()
        {
            var session = SengokuChronicle.Application.GameSession.NewGame();
            Assert.That(session.TryApplySave(MaxDateSaveData()).Succeeded, Is.True);

            Assert.That(session.TryAdvanceToNextMonth(), Is.False);
            Assert.That(session.WorldClock.CurrentDate, Is.EqualTo(MaxDate), "失败时会话时钟必须保持不变");
        }

        // 10. 旅行语义回归：月末推进不走旅行路径；旅行预览仍按天计算。
        [Test]
        public void GameSession_TryAdvanceToNextMonth_DoesNotTouchTravel()
        {
            var session = SengokuChronicle.Application.GameSession.NewGame();
            LocationId origin = session.CurrentLocation;
            TravelPreview? previewBefore = session.PreviewTravel(LocationId.Atsuta);
            Assert.That(previewBefore.HasValue, Is.True, "清洲 → 热田应有直达路线");

            Assert.That(session.TryAdvanceToNextMonth(), Is.True);
            Assert.That(session.CurrentLocation, Is.EqualTo(origin), "结束月份不得改变位置");

            // The sample monthly event is now pending, so travel preview is refused
            // until it is resolved (intentional SC-005–SC-009 contract).
            Assert.That(session.PendingEventId, Is.Not.Empty);
            Assert.That(session.PreviewTravel(LocationId.Atsuta), Is.Null,
                "待选事件期间必须拒绝旅行预览");

            ResolvePendingEvent(session);

            TravelPreview? previewAfter = session.PreviewTravel(LocationId.Atsuta);
            Assert.That(previewAfter.HasValue, Is.True);
            Assert.That(previewAfter.Value.TravelDays, Is.EqualTo(previewBefore.Value.TravelDays),
                "旅行耗时仍按原规则（天）计算");
            Assert.That(GameDate.TryAddDays(
                session.WorldClock.CurrentDate, previewAfter.Value.TravelDays, out GameDate expected), Is.True);
            Assert.That(previewAfter.Value.EstimatedArrivalDate, Is.EqualTo(expected),
                "预计到达 = 当前日期 + 旅行天数（按天推进语义不变）");
        }

        [Test]
        public void GameSession_PendingEventBlocksTravelAndAdvancementUntilResolved()
        {
            // This is the legacy SC-010 time/event contract; the M2 default starts
            // with the SC-013 pledge and still has the SC-011 rainy-arrival pending
            // after the second month, so it is not the schedule this test asserts.
            var session = SengokuChronicle.Application.GameSession.NewGame();
            session.InitStandardDemo();
            Assert.That(session.TryAdvanceToNextMonth(), Is.True, session.LastEventMessage);
            GameDate pendingDate = session.WorldClock.CurrentDate;
            LocationId pendingLocation = session.CurrentLocation;
            Assert.That(session.PendingEventId, Is.Not.Empty);

            Assert.That(session.AdvanceOneDay(), Is.False, "待选事件期间不得按天推进");
            Assert.That(session.AdvanceDays(3), Is.False, "待选事件期间不得按天推进");
            Assert.That(session.TryAdvanceToNextMonth(), Is.False, "待选事件期间不得再次结束月份");
            Assert.That(session.PreviewTravel(LocationId.Atsuta), Is.Null, "待选事件期间不得预览旅行");
            Assert.That(session.WorldClock.CurrentDate, Is.EqualTo(pendingDate));
            Assert.That(session.CurrentLocation, Is.EqualTo(pendingLocation));

            ResolvePendingEvent(session);

            Assert.That(session.TryAdvanceToNextMonth(), Is.True, "事件结算后必须恢复正常月推进");
            Assert.That(session.WorldClock.CurrentDate, Is.EqualTo(new GameDate(1560, 6, 1)));
            Assert.That(session.PendingEventId, Is.Empty);
        }

        // ===== SC-004 「结束月份」按钮 UI 测试 =====

        [Test]
        public void Panel_EndMonthClick_RefreshesClockAndMapDateLabels()
        {
            GameSession session = SpawnSession();
            WorldClockPanel panel = SpawnPanel(session, out Text panelDate, out _);
            WorldMapController controller = SpawnMapController(session, panel, out Text mapDate);
            Button monthButton = SpawnButton("SC004MonthButton");
            panel.BindMonthTurn(monthButton, controller);

            monthButton.onClick.Invoke();

            Assert.That(session.WorldClock.CurrentDate, Is.EqualTo(new GameDate(1560, 5, 1)));
            Assert.That(panelDate.text, Is.EqualTo("1560-05-01"), "面板日期必须刷新为下月 1 日");
            Assert.That(mapDate.text, Does.Contain("1560-05-01"), "地图日期必须同步刷新");
        }

        [Test]
        public void Panel_EndMonthClick_AtMaxDate_KeepsDateAndLabels()
        {
            GameSession session = SpawnSession();
            Assert.That(session.TryApplySave(MaxDateSaveData()).Succeeded, Is.True);
            WorldClockPanel panel = SpawnPanel(session, out Text panelDate, out _);
            panel.Refresh();
            Assert.That(panelDate.text, Is.EqualTo("2000-12-31"));

            Button monthButton = SpawnButton("SC004MonthButton");
            panel.BindMonthTurn(monthButton, null);
            monthButton.onClick.Invoke();

            Assert.That(session.WorldClock.CurrentDate, Is.EqualTo(MaxDate), "越界点击不得改动日期");
            Assert.That(panelDate.text, Is.EqualTo("2000-12-31"), "失败时标签文本必须保持不变");
            Assert.That(panel.LastMonthTurnMessage, Does.Contain("超出支持范围"),
                "日期越界必须保留独立的范围错误说明");
        }

        [Test]
        public void Panel_EndMonthClick_WhileEventPending_ReportsPendingReason()
        {
            GameSession session = SpawnSession();
            WorldClockPanel panel = SpawnPanel(session, out _, out _);
            Button monthButton = SpawnButton("SC004MonthButton");
            panel.BindMonthTurn(monthButton, null);

            monthButton.onClick.Invoke();
            Assert.That(session.PendingEventId, Is.Not.Empty);
            GameDate pendingDate = session.WorldClock.CurrentDate;

            monthButton.onClick.Invoke();

            Assert.That(session.WorldClock.CurrentDate, Is.EqualTo(pendingDate),
                "待选事件期间结束月份必须被拒绝且日期不变");
            Assert.That(panel.LastMonthTurnMessage, Does.Contain("事件"),
                "必须说明拒绝原因是待处理事件");
            Assert.That(panel.LastMonthTurnMessage, Does.Not.Contain("超出支持范围"),
                "待处理事件不得误报为日期越界");
        }

        [Test]
        public void Panel_RepeatedMonthBinding_DoesNotDoubleSubscribe()
        {
            GameSession session = SpawnSession();
            WorldClockPanel panel = SpawnPanel(session, out _, out _);
            Button monthButton = SpawnButton("SC004MonthButton");
            panel.BindMonthTurn(monthButton, null);
            panel.BindMonthTurn(monthButton, null);

            monthButton.onClick.Invoke();
            Assert.That(session.WorldClock.CurrentDate, Is.EqualTo(new GameDate(1560, 5, 1)),
                "重复绑定后一次点击只能推进一个月份");
        }

        [Test]
        public void Panel_RebindMonthButton_UnsubscribesPreviousButton()
        {
            GameSession session = SpawnSession();
            WorldClockPanel panel = SpawnPanel(session, out _, out _);
            Button first = SpawnButton("SC004FirstMonthButton");
            Button second = SpawnButton("SC004SecondMonthButton");
            panel.BindMonthTurn(first, null);
            panel.BindMonthTurn(second, null);

            first.onClick.Invoke();
            Assert.That(session.WorldClock.CurrentDate, Is.EqualTo(Start), "旧按钮必须不再推进月份");

            second.onClick.Invoke();
            Assert.That(session.WorldClock.CurrentDate, Is.EqualTo(new GameDate(1560, 5, 1)),
                "新按钮一次点击恰好推进一个月份");
        }

        [Test]
        public void Panel_DisableRemovesMonthListener_EnableRestoresItExactlyOnce()
        {
            GameSession session = SpawnSession();
            WorldClockPanel panel = SpawnPanel(session, out _, out _);
            Button monthButton = SpawnButton("SC004MonthButton");
            panel.BindMonthTurn(monthButton, null);
            InvokePanelLifecycle(panel, "OnEnable");

            monthButton.onClick.Invoke();
            Assert.That(session.WorldClock.CurrentDate, Is.EqualTo(new GameDate(1560, 5, 1)));
            ResolvePendingEvent(session);

            InvokePanelLifecycle(panel, "OnDisable");
            monthButton.onClick.Invoke();
            Assert.That(session.WorldClock.CurrentDate, Is.EqualTo(new GameDate(1560, 5, 1)),
                "OnDisable 之后点击不得再推进");

            InvokePanelLifecycle(panel, "OnEnable");
            monthButton.onClick.Invoke();
            Assert.That(session.WorldClock.CurrentDate, Is.EqualTo(new GameDate(1560, 6, 1)),
                "重新启用后监听必须恢复且只推进一个月份");
        }

        [Test]
        public void Panel_DayButtonStillAdvancesExactlyOneDay()
        {
            GameSession session = SpawnSession();
            WorldClockPanel panel = SpawnPanel(session, out Text panelDate, out Button advanceButton);
            InvokePanelLifecycle(panel, "OnEnable");

            advanceButton.onClick.Invoke();

            Assert.That(session.WorldClock.CurrentDate, Is.EqualTo(new GameDate(1560, 4, 2)),
                "「推进一天」语义必须保持不变");
            Assert.That(panelDate.text, Is.EqualTo("1560-04-02"));
        }

        // ---- SC-004 测试夹具 ----

        private readonly List<GameObject> _spawned = new List<GameObject>();

        [TearDown]
        public void CleanUpSpawnedUiObjects()
        {
            foreach (GameObject go in _spawned)
                if (go != null)
                    Object.DestroyImmediate(go);
            _spawned.Clear();
        }

        private static void ResolvePendingEvent(GameSession session)
        {
            string eventId = session.PendingEventId;
            Assert.That(eventId, Is.Not.Empty, "测试夹具预期存在待处理事件");
            EventDefinitionData definition = null;
            foreach (EventDefinitionData candidate in session.EventCatalog.events)
                if (candidate != null && candidate.eventId == eventId)
                {
                    definition = candidate;
                    break;
                }

            Assert.That(definition, Is.Not.Null, "待处理事件必须存在于活动目录：" + eventId);
            foreach (EventOptionData option in definition.options)
            {
                if (option == null ||
                    !session.TryGetEventOptionAvailability(eventId, option.optionId, out bool enabled, out _) ||
                    !enabled)
                    continue;

                EventChoiceResult result = session.TryChooseEventOption(eventId, option.optionId,
                    session.CreateEventResultId(eventId, option.optionId));
                Assert.That(result.Succeeded, Is.True, result.Message);
                return;
            }

            Assert.Fail("待处理事件没有可用选项：" + eventId);
        }

        private static SaveData MaxDateSaveData()
        {
            return new SaveData
            {
                saveFormatVersion = SaveDataValidator.CurrentVersion,
                year = 2000,
                month = 12,
                day = 31,
                locationId = LocationId.Kiyosu.Value,
                supplyShortageStage = (int)SupplyShortageStage.NotAccepted,
                sealedLetterStage = (int)SealedLetterStage.NotAccepted,
                marketTallyStage = (int)MarketTallyStage.NotAccepted,
                relationships = null
            };
        }

        private GameSession SpawnSession()
        {
            GameSession session = SengokuChronicle.Application.GameSession.NewGame();
            _spawned.Add(session.gameObject);
            return session;
        }

        private WorldClockPanel SpawnPanel(GameSession session, out Text dateText, out Button advanceButton)
        {
            var panelGo = new GameObject("SC004ClockPanel", typeof(RectTransform));
            panelGo.transform.SetParent(null, false);
            var rect = panelGo.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(560f, 200f);
            WorldClockPanel panel = panelGo.AddComponent<WorldClockPanel>();
            _spawned.Add(panelGo);

            dateText = CreateLabel("SC004PanelDateText");
            advanceButton = CreateButton("SC004AdvanceButton");
            panel.SetDependencies(session, advanceButton, dateText);
            return panel;
        }

        private WorldMapController SpawnMapController(GameSession session, WorldClockPanel panel, out Text currentDateText)
        {
            var mapGo = new GameObject("SC004WorldMapController", typeof(RectTransform));
            mapGo.transform.SetParent(null, false);
            WorldMapController controller = mapGo.AddComponent<WorldMapController>();
            _spawned.Add(mapGo);

            currentDateText = CreateLabel("SC004MapCurrentDateText");
            controller.BindReferences(
                session, panel,
                CreateLabel("SC004Marker"), CreateLabel("SC004Feedback"), currentDateText,
                CreateLabel("SC004Destination"), CreateLabel("SC004TravelTime"), CreateLabel("SC004ArrivalDate"),
                CreateButton("SC004Confirm"), CreateButton("SC004Cancel"), CreateButton("SC004ReturnToMap"),
                null, null, null);
            return controller;
        }

        private Button SpawnButton(string name)
        {
            Button button = CreateButton(name);
            return button;
        }

        private Text CreateLabel(string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.localScale = Vector3.one;
            Text text = go.AddComponent<Text>();
            _spawned.Add(go);
            return text;
        }

        private Button CreateButton(string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            go.AddComponent<Image>();
            Button button = go.AddComponent<Button>();
            _spawned.Add(go);
            return button;
        }

        private static void InvokePanelLifecycle(WorldClockPanel panel, string methodName)
        {
            MethodInfo method = typeof(WorldClockPanel).GetMethod(
                methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, $"缺少生命周期方法：{methodName}");
            method.Invoke(panel, null);
        }
    }
}
