using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;
using SengokuChronicle.Persistence;
using SengokuChronicle.Presentation;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SengokuChronicle.PlayModeTests
{
    /// <summary>
    /// M08 五十五周年阶段回顾的端到端验证。它按生产 UI 的调用链驱动：
    /// 直接触发 FoundationsScene 标题界面「新游戏」与 SC-013 剧本按钮的 onClick 回调，
    /// 之后每个自然月都触发界面「结束本月」的回调，事件与继承都触发面板按钮回调，
    /// 农庄空缺时触发产业面板的候选与确认按钮回调。它不模拟物理鼠标点击，
    /// 也不验证 EventSystem 导航或焦点命中。任何日期、进度、确认标志、
    /// 存档字段都不直接写入，也不调用 Refresh 伪造状态。随机数固定为 SC-013 新游戏
    /// 使用的 <see cref="GameSession.DefaultRandomSeed"/>，并在存档里回读种子做断言。
    ///
    /// 每触发一次「结束本月」都断言 <see cref="GameSession.LastMonthTurnOutcome"/> 为
    /// <see cref="MonthTurnOutcome.Succeeded"/>，且日期恰好前移一个真实日历月；
    /// 两项断言都通过后才把该月计入成功月结数。从 1560-04-01 到 1615-04-01
    /// 必须正好是 660 次成功月结，而不是只靠终点日期反推。
    ///
    /// 到 1615-04-01 时检查回顾页是否由会话状态自动打开、日期与阶段是否正确、
    /// 正文是否真的渲染出文字；随后触发继续按钮回调，随即在隔离槽存档，
    /// 再触发「结束本月」成功推进一个月，最后读档，验证日期回到刚才的存档日、
    /// 已确认的阶段回顾仍为 None 且回顾页不再显示。
    /// </summary>
    public sealed class M08CampaignReviewPlayModeTests
    {
        private const string ScenePath = "Assets/Scenes/FoundationScene.unity";
        private const string LogPrefix = "[M08-55] ";
        private const string ContinueButtonName = "M08CampaignReviewContinueButton";
        private const string EventOptionPrefix = "EventOption_";
        private const string SuccessionCandidatePrefix = "SuccessionCandidate_";
        private const string SuccessionRegentPrefix = "SuccessionRegent_";
        private const string FarmCandidatePrefix = "IndustryCandidate_" + IndustryCatalog.FarmId + "_";
        private const string MapTabButtonName = "ReferenceNavMapButton";
        private const string HouseholdTabButtonName = "ReferenceNavHouseholdButton";
        // 继承 HUD 与决策弹窗随「家族」页显示（见 PageMaskForRoot），家计页只挂产业面板。
        private const string FamilyTabButtonName = "ReferenceNavFamilyButton";
        private const int MonthCap = 700;
        // 1560-04-01 → 1615-04-01 是 55 个真实年，每年 12 个日历月，合计 660 次月结。
        private const int ExpectedStageReviewMonthTurns = 660;

        private static readonly GameDate ExpectedStart = new GameDate(1560, 4, 1);
        private static readonly GameDate StageReviewDate = new GameDate(1615, 4, 1);
        // SC-013 新游戏固定使用 GameSession.DefaultRandomSeed（0x5343414E4B4F3031）。
        // 存档里的 initialSeedHex 是 16 位大写十六进制，这里断言整条路线都跑在这个固定种子上。
        private const string ExpectedSeedHex = "5343414E4B4F3031";

        private GameSession _session;
        private CampaignReviewPanel _panel;
        private TitleScreenController _title;
        private SaveLoadPanel _saveLoad;
        private string _storeDir;
        private int _successfulMonthTurns;
        private int _events;
        private int _successions;
        private int _farmAssignments;

        [UnityTest]
        public IEnumerator StageReview_OpensAtFiftyFiveYears_ConfirmsAndSurvivesSaveLoad()
        {
            yield return LoadFoundationScene();

            _session = FindComponent<GameSession>();
            _panel = FindComponent<CampaignReviewPanel>();
            _title = FindComponent<TitleScreenController>();
            _saveLoad = FindComponent<SaveLoadPanel>();
            Assert.IsNotNull(_session, "FoundationScene 缺少 GameSession。");
            Assert.IsNotNull(_panel, "FoundationScene 缺少 CampaignReviewPanel。");
            Assert.IsNotNull(_title, "FoundationScene 缺少 TitleScreenController。");
            Assert.IsNotNull(_saveLoad, "FoundationScene 缺少 SaveLoadPanel。");
            Assert.IsTrue(_panel.IsBound, "CampaignReviewPanel 未绑定依赖。");
            Assert.IsFalse(_panel.IsVisible, "标题界面阶段不应显示五十五周年回顾。");

            _storeDir = Path.Combine(Path.GetTempPath(),
                "m08-55-review-playmode-" + Guid.NewGuid().ToString("N"));
            var store = new SaveSlotStore(_storeDir);
            _title.SetStore(store);
            _saveLoad.SetStore(store);

            yield return StartSC013NewGame();
            yield return ShowGalleryPage(MapTabButtonName);
            Button endMonthOnMap = FindButton("EndMonthButton");
            Assert.IsNotNull(endMonthOnMap, "地图页没有「结束本月」按钮。");
            Assert.IsTrue(endMonthOnMap.gameObject.activeInHierarchy,
                "切到地图页后「结束本月」仍未激活。");

            Assert.AreEqual(ExpectedStart, _session.WorldClock.CurrentDate,
                "新游戏开局日期不是 1560-04-01。");
            Assert.AreEqual(1560, _session.CampaignStartDate.Year, "战役开局年份未记录。");
            Assert.AreEqual(0, _session.CampaignCompletedYears, "开局时不应已历年份。");
            Assert.AreEqual(CampaignReviewMilestone.None, _session.PendingCampaignReview,
                "开局时不应有阶段回顾待处理。");

            Assert.AreEqual(ExpectedSeedHex,
                GameSession.DefaultRandomSeed.ToString("X16",
                    System.Globalization.CultureInfo.InvariantCulture),
                "SC-013 固定剧本的默认种子已改变，本用例的复现前提不再成立。");
            Debug.Log(LogPrefix + "固定种子=" + ExpectedSeedHex + "（GameSession.DefaultRandomSeed）");

            yield return DriveMonthsToStageReview();

            Assert.AreEqual(StageReviewDate, _session.WorldClock.CurrentDate,
                "正常月结未能停在 1615-04-01。");
            Assert.IsFalse(_session.IsFamilyExtinct, "到达评价日前家族已灭绝。");
            Assert.AreEqual(55, _session.CampaignCompletedYears, "评价日应已历 55 年。");
            Assert.AreEqual(CampaignReviewMilestone.StageReview, _session.PendingCampaignReview,
                "1615-04-01 应有一个待确认的五十五周年阶段回顾。");

            yield return AssertStageReviewPanelVisible();
            yield return AcknowledgeStageReviewThroughUi();
            yield return AssertConfirmedReviewSurvivesSaveMonthTurnAndLoad();

            Debug.Log(LogPrefix + "完成：成功月结=" + _successfulMonthTurns + " 事件=" + _events +
                      " 继承=" + _successions + " 农庄任命=" + _farmAssignments +
                      " 日期=" + _session.WorldClock.CurrentDate +
                      " 待处理回顾=" + _session.PendingCampaignReview);
        }

        [TearDown]
        public void DeleteIsolatedStore()
        {
            if (string.IsNullOrEmpty(_storeDir) || !Directory.Exists(_storeDir))
                return;
            try
            {
                Directory.Delete(_storeDir, true);
            }
            catch (IOException exception)
            {
                Debug.LogWarning(LogPrefix + "临时存档目录清理失败：" + exception.Message);
            }
        }

        private static IEnumerator LoadFoundationScene()
        {
            AsyncOperation load = SceneManager.LoadSceneAsync(ScenePath, LoadSceneMode.Single);
            Assert.IsNotNull(load, "无法加载 " + ScenePath + "，场景可能不在 Build Settings 中。");
            while (!load.isDone)
                yield return null;
            // 让场景里 Start/OnEnable 的绑定与标题界面状态先跑完。
            yield return null;
            yield return null;
        }

        private IEnumerator StartSC013NewGame()
        {
            Assert.IsTrue(_title.IsTitleVisible, "场景加载后标题界面未显示。");
            ClickButton("M6NewGameButton");
            yield return null;
            Assert.IsTrue(_title.IsScenarioSelectionVisible, "新游戏后未进入剧本选择页。");

            ClickButton("SC013FixedScenarioButton");
            yield return null;
            yield return null;
            Assert.IsFalse(_title.IsTitleVisible, "开始 SC-013 后标题界面仍可见。");
        }

        private static IEnumerator ShowGalleryPage(string tabButtonName)
        {
            Button tab = FindButton(tabButtonName);
            if (tab == null || !tab.gameObject.activeInHierarchy)
            {
                Button launcher = FindButton("M02DailyActionOpenButton");
                if (launcher != null && launcher.gameObject.activeInHierarchy)
                {
                    launcher.onClick.Invoke();
                    yield return null;
                }
            }

            tab = FindButton(tabButtonName);
            Assert.IsNotNull(tab, "找不到页面按钮：" + tabButtonName);
            Assert.IsTrue(tab.gameObject.activeInHierarchy, "页面按钮不在活动层级：" + tabButtonName);
            Assert.IsTrue(tab.interactable, "页面按钮不可交互：" + tabButtonName);
            tab.onClick.Invoke();
            yield return null;
        }

        private IEnumerator DriveMonthsToStageReview()
        {
            Assert.AreEqual(ExpectedStageReviewMonthTurns,
                CalendarMonthsBetween(ExpectedStart, StageReviewDate),
                "SC-013 起点 " + ExpectedStart + " 到 " + StageReviewDate +
                " 不是 " + ExpectedStageReviewMonthTurns + " 个日历月，用例前提不成立。");

            while (_session.WorldClock.CurrentDate < StageReviewDate)
            {
                Assert.Less(_successfulMonthTurns, MonthCap,
                    "在 " + MonthCap + " 个月内未到达 " + StageReviewDate + "，停在 " +
                    _session.WorldClock.CurrentDate + "，成功月结=" + _successfulMonthTurns + "。");
                Assert.IsFalse(_session.IsFamilyExtinct,
                    "家族在第 " + _successfulMonthTurns + " 次成功月结（" +
                    _session.WorldClock.CurrentDate + "）灭绝。");
                Assert.IsFalse(_panel.IsVisible,
                    "五十五周年回顾在 " + _session.WorldClock.CurrentDate + " 就提前打开了。");

                yield return ResolvePendingEvents();
                yield return ResolvePendingSuccession();
                yield return MaintainFarmIfVacant();
                yield return TriggerEndMonthAndAssertOneCalendarMonth();

                if (_successfulMonthTurns % 60 == 0)
                    Debug.Log(LogPrefix + "成功月结=" + _successfulMonthTurns +
                              " 日期=" + _session.WorldClock.CurrentDate +
                              " 事件=" + _events + " 继承=" + _successions +
                              " 农庄任命=" + _farmAssignments);
            }

            Assert.AreEqual(ExpectedStageReviewMonthTurns, _successfulMonthTurns,
                "从 " + ExpectedStart + " 到 " + StageReviewDate + " 应正好是 " +
                ExpectedStageReviewMonthTurns + " 次成功月结，实际为 " + _successfulMonthTurns +
                "，当前日期 " + _session.WorldClock.CurrentDate + "。");
        }

        /// <summary>
        /// 触发生产「结束本月」按钮回调，并断言行随后状态：
        /// outcome 为 Succeeded，且日期恰好前移一个真实日历月。
        /// 两项都成立后才把这次月结计入成功月结数；失败信息带上触发前日期、成功计数与 outcome。
        /// </summary>
        private IEnumerator TriggerEndMonthAndAssertOneCalendarMonth(int settleFrames = 1)
        {
            Button endMonth = FindButton("EndMonthButton");
            if (endMonth == null || !endMonth.gameObject.activeInHierarchy)
            {
                yield return ShowGalleryPage(MapTabButtonName);
                endMonth = FindButton("EndMonthButton");
            }
            Assert.IsNotNull(endMonth, "找不到「结束本月」按钮。");
            Assert.IsTrue(endMonth.gameObject.activeInHierarchy,
                _session.WorldClock.CurrentDate + " 的「结束本月」不在活动层级。");
            Assert.IsTrue(endMonth.interactable,
                _session.WorldClock.CurrentDate + " 的「结束本月」不可交互：" +
                _session.LastMonthTurnOutcome);

            GameDate before = _session.WorldClock.CurrentDate;
            GameDate expected = ExpectedNextCalendarMonth(before);
            endMonth.onClick.Invoke();

            MonthTurnOutcome outcome = _session.LastMonthTurnOutcome;
            GameDate after = _session.WorldClock.CurrentDate;
            Assert.AreEqual(MonthTurnOutcome.Succeeded, outcome,
                "「结束本月」未成功推进：触发前=" + before + "｜成功月结=" +
                _successfulMonthTurns + "｜outcome=" + outcome + "｜触发后=" + after +
                "｜期望日期=" + expected);
            Assert.AreEqual(expected, after,
                "「结束本月」没有恰好前进一个真实日历月：触发前=" + before +
                "｜成功月结=" + _successfulMonthTurns + "｜outcome=" + outcome +
                "｜触发后=" + after + "｜期望日期=" + expected);
            _successfulMonthTurns++;

            for (int i = 0; i < settleFrames; i++)
                yield return null;
        }

        /// <summary>同一日号推进一个真实日历月，月内日号超出目标月天数时取目标月末。</summary>
        private static GameDate ExpectedNextCalendarMonth(GameDate date)
        {
            int year = date.Month == 12 ? date.Year + 1 : date.Year;
            int month = date.Month == 12 ? 1 : date.Month + 1;
            int day = Math.Min(date.Day, GameDate.DaysInMonth(year, month));
            return new GameDate(year, month, day);
        }

        /// <summary>两日期间隔的日历月数；仅在同日号时使用，本用例两端都是月初。</summary>
        private static int CalendarMonthsBetween(GameDate from, GameDate to)
        {
            return (to.Year - from.Year) * 12 + (to.Month - from.Month);
        }

        private IEnumerator ResolvePendingEvents()
        {
            for (int guard = 0; guard < 64 && !string.IsNullOrEmpty(_session.PendingEventId); guard++)
            {
                string eventId = _session.PendingEventId;
                var attempted = new HashSet<string>(StringComparer.Ordinal);
                bool chosen = false;
                while (!chosen)
                {
                    Button option = FirstEventOptionButton(attempted);
                    if (option == null)
                        Assert.Fail("事件 " + eventId + " 在界面上没有可用选项：" +
                                    _session.LastEventMessage);
                    string optionId = option.gameObject.name.Substring(EventOptionPrefix.Length);
                    attempted.Add(optionId);
                    option.onClick.Invoke();
                    yield return null;
                    if (_session.PendingEventId != eventId)
                    {
                        chosen = true;
                        _events++;
                    }
                }
            }

            Assert.IsTrue(string.IsNullOrEmpty(_session.PendingEventId),
                "事件队列在 64 次界面选择后仍未排空：" + _session.PendingEventId);
        }

        private IEnumerator ResolvePendingSuccession()
        {
            SuccessionState initial = _session.Succession;
            bool needsUi = initial.IsPending ||
                           initial.IsRegencyHandoffDue(_session.WorldClock.CurrentDate);
            if (!needsUi)
                yield break;
            // 继承面板挂在「家族」页上，由该页负责显示与隐藏。
            yield return ShowGalleryPage(FamilyTabButtonName);

            for (int step = 0; step < 32; step++)
            {
                SuccessionState state = _session.Succession;
                if (!state.IsPending)
                    yield break;

                GameDate date = _session.WorldClock.CurrentDate;
                if (state.Phase == SuccessionPhase.CrisisDecision)
                {
                    ClickButton("SuccessionAcceptCrisisButton");
                    yield return null;
                    continue;
                }

                if (state.Phase == SuccessionPhase.AwaitingRegentChoice)
                {
                    if (state.PendingRegentId.IsEmpty)
                    {
                        if (!TryClickFirst(SuccessionRegentPrefix))
                            LogSuccessionDiagnostics("摄政候选");
                        Assert.IsTrue(HasActiveButton(SuccessionRegentPrefix),
                            "摄政选择阶段没有可用的摄政候选按钮。");
                        yield return null;
                        continue;
                    }

                    ClickButton("SuccessionConfirmButton");
                    yield return null;
                    _successions++;
                    continue;
                }

                if (state.Phase == SuccessionPhase.AwaitingSuccessionChoice)
                {
                    if (state.PendingSuccessorId.IsEmpty)
                    {
                        if (!TryClickFirst(SuccessionCandidatePrefix))
                            LogSuccessionDiagnostics("继承人候选");
                        Assert.IsTrue(HasActiveButton(SuccessionCandidatePrefix),
                            "继承人选择阶段没有可用的候选人按钮。");
                        yield return null;
                        continue;
                    }

                    ClickButton("SuccessionConfirmButton");
                    yield return null;
                    _successions++;
                    continue;
                }

                if (state.IsRegencyHandoffDue(date))
                {
                    ClickButton("SuccessionConfirmButton");
                    yield return null;
                    _successions++;
                    continue;
                }

                yield break;
            }

            Assert.Fail("继承决策在 32 步界面操作后仍未结束：" + _session.Succession.Phase);
        }

        private IEnumerator MaintainFarmIfVacant()
        {
            if (!_session.CanEditIndustryAssignments)
                yield break;
            if (!_session.Industry.Slot(IndustryCatalog.FarmId).AssigneeId.IsEmpty)
                yield break;

            // 产业面板嵌在「家计」页里，由该页显示为活动面板；结束后回到「地图」页月结。
            yield return ShowGalleryPage(HouseholdTabButtonName);
            Assert.IsTrue(_session.CanEditIndustryAssignments,
                "家计页打开后产业面板仍不可编辑任命。");

            var attempted = new HashSet<string>(StringComparer.Ordinal);
            bool assigned = !_session.Industry.Slot(IndustryCatalog.FarmId).AssigneeId.IsEmpty;
            for (int attempt = 0; attempt < 8 && !assigned; attempt++)
            {
                Button candidate = FirstCandidateButton(FarmCandidatePrefix, attempted);
                if (candidate == null)
                    break;
                attempted.Add(candidate.gameObject.name);
                candidate.onClick.Invoke();
                yield return null;

                Button confirm = FindButton("IndustryConfirmButton");
                if (confirm == null || !confirm.gameObject.activeInHierarchy || !confirm.interactable)
                    continue;
                confirm.onClick.Invoke();
                yield return null;
                assigned = !_session.Industry.Slot(IndustryCatalog.FarmId).AssigneeId.IsEmpty;
                if (assigned)
                    _farmAssignments++;
            }

            if (!assigned)
            {
                IndustryPanel industry = FindComponent<IndustryPanel>();
                Debug.Log(LogPrefix + _session.WorldClock.CurrentDate + " 农庄未能任命：" +
                          (industry != null ? industry.MessageLine : "无产业面板"));
            }

            Button close = FindButton("IndustryCloseButton");
            if (close != null && close.gameObject.activeInHierarchy)
            {
                close.onClick.Invoke();
                yield return null;
            }
        }

        private IEnumerator AssertStageReviewPanelVisible()
        {
            Canvas.ForceUpdateCanvases();
            yield return null;
            Canvas.ForceUpdateCanvases();

            Assert.IsTrue(_panel.IsVisible, "1615-04-01 未自动打开五十五周年回顾页。");
            GameObject page = GameObject.Find(CampaignReviewPanel.PageObjectName);
            Assert.IsNotNull(page, "找不到回顾页 " + CampaignReviewPanel.PageObjectName + "。");
            Assert.IsTrue(page.activeInHierarchy, "回顾页在层级中未激活。");
            AssertReviewRendersAboveHud(page);

            Assert.AreEqual(CampaignReviewView.MilestoneTitle(CampaignReviewMilestone.StageReview),
                _panel.Title, "回顾页标题不正确。");
            Assert.AreEqual(CampaignReviewView.MilestoneContinueLabel(CampaignReviewMilestone.StageReview),
                _panel.ContinueLabel, "继续按钮标题不正确。");

            string context = _panel.Context;
            StringAssert.Contains("开局：1560-04-01", context, "回顾上下文缺少开局日期。");
            StringAssert.Contains("已历 55 年", context, "回顾上下文缺少已历 55 年。");
            StringAssert.Contains("评价日：1615-04-01", context, "回顾上下文缺少评价日。");

            string body = _panel.Body;
            Assert.Greater(body.Length, 1000, "回顾正文过短，可能是空页。");
            StringAssert.Contains("【五项发展维度】", body, "回顾正文缺少五项发展维度段。");
            Assert.GreaterOrEqual(_panel.BodyBlockCount, 1, "回顾正文没有渲染区块。");
            Assert.IsTrue(_panel.CardScale > 0f, "回顾卡片缩放无效。");

            Button continueButton = FindButton(ContinueButtonName);
            Assert.IsNotNull(continueButton, "找不到回顾继续按钮。");
            Assert.IsTrue(continueButton.gameObject.activeInHierarchy, "回顾继续按钮未激活。");
            Assert.IsTrue(continueButton.interactable, "回顾继续按钮不可交互。");
            Debug.Log(LogPrefix + "回顾页已打开：标题=" + _panel.Title +
                      "｜上下文=" + _panel.Context +
                      "｜正文长度=" + body.Length +
                      "｜区块=" + _panel.BodyBlockCount +
                      "｜缩放=" + _panel.CardScale.ToString("0.###",
                          System.Globalization.CultureInfo.InvariantCulture) +
                      "｜继续按钮=" + _panel.ContinueLabel);

            var blocks = BodyBlockTexts();
            Assert.AreEqual(_panel.BodyBlockCount, blocks.Count,
                "正文区块数量与面板报告的区块数量不一致。");
            var joined = new System.Text.StringBuilder();
            for (int i = 0; i < blocks.Count; i++)
                joined.Append(blocks[i].text);
            Assert.AreEqual(body, joined.ToString(), "正文区块拼起来与面板正文不一致。");

            int visibleBlocks = 0;
            for (int i = 0; i < blocks.Count; i++)
            {
                int vertices = blocks[i].cachedTextGenerator != null
                    ? blocks[i].cachedTextGenerator.vertexCount
                    : 0;
                if (vertices > 0)
                    visibleBlocks++;
                Debug.Log(LogPrefix + "正文区块 " + i + " 字符=" + blocks[i].text.Length +
                          " 顶点=" + vertices);
            }

            Assert.Greater(visibleBlocks, 0, "回顾正文没有任何区块生成可见文字。");
            Assert.Greater(blocks[0].cachedTextGenerator.vertexCount, 0,
                "回顾正文第一个区块没有生成可见文字。");

            Text hudDate = FindText("DateText");
            Assert.IsNotNull(hudDate, "找不到主界面日期文本 DateText。");
            Assert.AreEqual("1615-04-01", hudDate.text, "主界面日期未显示 1615-04-01。");
        }

        private static void AssertReviewRendersAboveHud(GameObject page)
        {
            Transform reviewRoot = page.transform.parent;
            Assert.IsNotNull(reviewRoot, "回顾页缺少固定根节点。");
            Transform canvas = reviewRoot.parent;
            Assert.IsNotNull(canvas, "回顾根节点不在 Canvas 下。");
            ReferenceGalleryNavigator navigator = FindComponent<ReferenceGalleryNavigator>();
            Assert.IsNotNull(navigator, "FoundationScene 缺少 ReferenceGalleryNavigator。");
            // Canvas 中靠后的兄弟节点渲染在上。M08 回顾页必须盖住 M02 日常行动页，
            // 同时按 CanvasLayerOrder 的共享座次留在标题、事件选择与战术覆盖层之下。
            Assert.Greater(reviewRoot.GetSiblingIndex(), navigator.transform.GetSiblingIndex(),
                "回顾根节点没有排在 M02 日常行动页（" + navigator.name + "）之上。");
            foreach (string overlay in new[]
                     {
                         "M6TitleRoot", "EventChoiceRoot", TacticalMissionPanel.OverlayObjectName
                     })
            {
                Transform sibling = canvas.Find(overlay);
                if (sibling == null)
                    continue;
                Assert.Less(reviewRoot.GetSiblingIndex(), sibling.GetSiblingIndex(),
                    "回顾根节点没有排在 " + overlay + " 之下。");
            }
        }

        private IEnumerator AcknowledgeStageReviewThroughUi()
        {
            ClickButton(ContinueButtonName);
            yield return null;
            yield return null;

            Assert.AreEqual(CampaignReviewMilestone.None, _session.PendingCampaignReview,
                "点击继续后阶段回顾仍未确认。");
            Assert.IsFalse(_panel.IsVisible, "点击继续后回顾页仍然可见。");
            GameObject page = GameObject.Find(CampaignReviewPanel.PageObjectName);
            Assert.IsTrue(page == null || !page.activeInHierarchy,
                "点击继续后回顾页仍留在层级中激活。");
            Debug.Log(LogPrefix + "确认阶段回顾：" + _session.LastCampaignReviewMessage);
        }

        private IEnumerator AssertConfirmedReviewSurvivesSaveMonthTurnAndLoad()
        {
            Assert.IsTrue(_saveLoad.SaveInteractable, "存档按钮不可交互。");
            ClickButton("M5SaveButton");
            yield return null;
            yield return null;
            Assert.IsTrue(_saveLoad.LastOperationSucceeded, "存档失败：" + _saveLoad.Status);
            string slotPath = Path.Combine(_storeDir, SaveSlotStore.FileName);
            Assert.IsTrue(File.Exists(slotPath), "隔离存档槽中没有写出存档文件：" + slotPath);

            SaveData savedSlot = JsonUtility.FromJson<SaveData>(File.ReadAllText(slotPath));
            Assert.IsNotNull(savedSlot, "隔离存档槽的 JSON 无法解析。");
            Assert.IsNotNull(savedSlot.randomState, "存档没有记录随机状态。");
            Assert.AreEqual(ExpectedSeedHex, savedSlot.randomState.initialSeedHex,
                "存档记录的随机种子不是 SC-013 的固定种子。");
            Debug.Log(LogPrefix + "存档种子=" + savedSlot.randomState.initialSeedHex);

            GameDate savedDate = _session.WorldClock.CurrentDate;
            Assert.AreEqual(StageReviewDate, savedDate, "确认阶段回顾后应立即在 1615-04-01 存档。");

            yield return AdvanceOneMonthThroughUi();

            Assert.AreEqual(new GameDate(1615, 5, 1), _session.WorldClock.CurrentDate,
                "存档后触发「结束本月」没有成功推进一个月到 1615-05-01。");
            Text advancedHudDate = FindText("DateText");
            Assert.IsNotNull(advancedHudDate, "找不到主界面日期文本 DateText。");
            Assert.AreEqual("1615-05-01", advancedHudDate.text,
                "存档后月结没有把主界面日期刷新到 1615-05-01。");
            Assert.AreEqual(CampaignReviewMilestone.None, _session.PendingCampaignReview,
                "确认之后的月份里阶段回顾又变成待处理。");
            Assert.IsFalse(_panel.IsVisible, "确认之后回顾页再次出现。");
            Debug.Log(LogPrefix + "存档后月结推进：" + savedDate + " → " +
                      _session.WorldClock.CurrentDate);

            Assert.IsTrue(_saveLoad.LoadInteractable, "读档按钮不可交互。");
            ClickButton("M5LoadButton");
            yield return null;
            yield return null;

            Assert.IsTrue(_saveLoad.LastOperationSucceeded, "读档失败：" + _saveLoad.Status);
            Assert.AreEqual(savedDate, _session.WorldClock.CurrentDate,
                "读档后日期没有恢复到存档时的 1615-04-01。");
            Text loadedHudDate = FindText("DateText");
            Assert.IsNotNull(loadedHudDate, "读档后找不到主界面日期文本 DateText。");
            Assert.AreEqual("1615-04-01", loadedHudDate.text,
                "读档后主界面日期没有恢复到 1615-04-01。");
            Assert.AreEqual(CampaignReviewMilestone.None, _session.PendingCampaignReview,
                "读档后已确认的阶段回顾又变成待处理。");
            Assert.IsFalse(_panel.IsVisible, "读档后回顾页再次弹出。");
            GameObject page = GameObject.Find(CampaignReviewPanel.PageObjectName);
            Assert.IsTrue(page == null || !page.activeInHierarchy,
                "读档后回顾页仍留在层级中激活。");
            Debug.Log(LogPrefix + "隔离存档→月结→读档通过：" + slotPath +
                      "｜读回日期=" + _session.WorldClock.CurrentDate);
        }

        private IEnumerator AdvanceOneMonthThroughUi()
        {
            yield return ResolvePendingEvents();
            yield return ResolvePendingSuccession();
            yield return MaintainFarmIfVacant();
            // 存档后这一步也是真实月结：同样断言 Succeeded 与恰好一个日历月，再计入成功月结数。
            yield return TriggerEndMonthAndAssertOneCalendarMonth(2);
        }

        private Button FirstEventOptionButton(ICollection<string> skip)
        {
            var found = new List<Button>();
            foreach (Button button in UnityEngine.Object.FindObjectsByType<Button>(
                         FindObjectsInactive.Exclude))
            {
                if (button == null || !button.gameObject.activeInHierarchy || !button.interactable)
                    continue;
                string name = button.gameObject.name;
                if (!name.StartsWith(EventOptionPrefix, StringComparison.Ordinal))
                    continue;
                if (skip != null && skip.Contains(name.Substring(EventOptionPrefix.Length)))
                    continue;
                found.Add(button);
            }

            found.Sort((left, right) =>
                left.transform.GetSiblingIndex().CompareTo(right.transform.GetSiblingIndex()));
            return found.Count > 0 ? found[0] : null;
        }

        private void LogSuccessionDiagnostics(string stage)
        {
            SuccessionPanel panel = FindComponent<SuccessionPanel>();
            ReferenceGalleryNavigator navigator = FindComponent<ReferenceGalleryNavigator>();
            Transform successionRoot = panel != null
                ? FindDeepChild(panel.transform.root, "SuccessionRoot")
                : null;
            Debug.Log(LogPrefix + "继承诊断(" + stage + ")：阶段=" + _session.Succession.Phase +
                      " 候选数=" + _session.SuccessionCandidates.Count +
                      " 摄政候选数=" + _session.SuccessionRegentCandidates.Count +
                      " 页面=" + (navigator != null ? navigator.CurrentPage.ToString() : "无导航") +
                      " 继承根激活=" + (successionRoot != null && successionRoot.gameObject.activeSelf) +
                      " 面板可见=" + (panel != null && panel.IsModalVisible) +
                      " 面板候选=" + (panel != null ? panel.VisibleCandidateCount : -1) +
                      " 面板摄政=" + (panel != null ? panel.VisibleRegentCount : -1) +
                      " 候选人按钮=" + CountButtons(SuccessionCandidatePrefix) +
                      " 摄政按钮=" + CountButtons(SuccessionRegentPrefix) +
                      " 确认按钮=" + (HasActiveButton("SuccessionConfirmButton") ? 1 : 0) +
                      " 状态=" + (panel != null ? panel.StatusMessage : string.Empty) +
                      " 弹窗状态=" + (panel != null ? panel.ModalStatusMessage : string.Empty));
        }

        private static Transform FindDeepChild(Transform parent, string name)
        {
            if (parent == null)
                return null;
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child.name == name)
                    return child;
                Transform found = FindDeepChild(child, name);
                if (found != null)
                    return found;
            }
            return null;
        }

        private static int CountButtons(string namePrefix)
        {
            int count = 0;
            foreach (Button button in UnityEngine.Object.FindObjectsByType<Button>(
                         FindObjectsInactive.Exclude))
                if (button != null && button.gameObject.name.StartsWith(namePrefix,
                        StringComparison.Ordinal))
                    count++;
            return count;
        }

        private static bool HasActiveButton(string namePrefix)
        {
            foreach (Button button in UnityEngine.Object.FindObjectsByType<Button>(
                         FindObjectsInactive.Exclude))
                if (button != null && button.gameObject.name.StartsWith(namePrefix,
                        StringComparison.Ordinal) && button.gameObject.activeInHierarchy &&
                    button.interactable)
                    return true;
            return false;
        }

        private bool TryClickFirst(string namePrefix)
        {
            Button button = FirstCandidateButton(namePrefix, null);
            if (button == null)
                return false;
            button.onClick.Invoke();
            return true;
        }

        private static Button FirstCandidateButton(string namePrefix, ICollection<string> skip)
        {
            var found = new List<Button>();
            foreach (Button button in UnityEngine.Object.FindObjectsByType<Button>(
                         FindObjectsInactive.Exclude))
            {
                if (button == null || !button.gameObject.activeInHierarchy || !button.interactable)
                    continue;
                string name = button.gameObject.name;
                if (!name.StartsWith(namePrefix, StringComparison.Ordinal))
                    continue;
                if (skip != null && skip.Contains(name))
                    continue;
                found.Add(button);
            }

            found.Sort((left, right) => string.CompareOrdinal(left.gameObject.name,
                right.gameObject.name));
            return found.Count > 0 ? found[0] : null;
        }

        private List<Text> BodyBlockTexts()
        {
            const string bodyName = "M08CampaignReviewBodyText";
            var blocks = new List<Text>();
            foreach (Text text in UnityEngine.Object.FindObjectsByType<Text>(
                         FindObjectsInactive.Exclude))
            {
                if (text == null || text.gameObject.name != bodyName &&
                    !text.gameObject.name.StartsWith(bodyName + CampaignReviewPanel.BodyBlockNameSuffix,
                        StringComparison.Ordinal))
                    continue;
                blocks.Add(text);
            }

            blocks.Sort((left, right) =>
                left.transform.GetSiblingIndex().CompareTo(right.transform.GetSiblingIndex()));
            return blocks;
        }

        private static void ClickButton(string name)
        {
            Button button = FindButton(name);
            Assert.IsNotNull(button, "找不到按钮：" + name);
            Assert.IsTrue(button.gameObject.activeInHierarchy, "按钮不在活动层级：" + name);
            Assert.IsTrue(button.interactable, "按钮不可交互：" + name);
            button.onClick.Invoke();
        }

        private static Button FindButton(string name)
        {
            GameObject target = GameObject.Find(name);
            return target != null ? target.GetComponent<Button>() : null;
        }

        private static Text FindText(string name)
        {
            GameObject target = GameObject.Find(name);
            return target != null ? target.GetComponent<Text>() : null;
        }

        private static T FindComponent<T>() where T : Component
        {
            return UnityEngine.Object.FindAnyObjectByType<T>(FindObjectsInactive.Include);
        }
    }
}
