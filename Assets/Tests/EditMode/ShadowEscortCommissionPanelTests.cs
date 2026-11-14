using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;
using SengokuChronicle.Persistence;
using SengokuChronicle.Presentation;
using Object = UnityEngine.Object;

namespace SengokuChronicle.Tests.EditMode
{
    /// <summary>
    /// Focused projection tests for the M05 commission page. They assert what a player
    /// can see and submit, not internal panel state; domain lifecycles and save shapes
    /// are already covered by ShadowEscortCommissionTests and M05CommissionSessionTests.
    /// </summary>
    public sealed class ShadowEscortCommissionPanelTests
    {
        private GameSession _session;
        private GameObject _host;
        private GameObject _page;
        private GameObject _content;
        private GameObject _template;
        private ShadowEscortCommissionPanel _panel;
        private Button _open;
        private Button _close;
        private Button _accept;
        private Text _title;
        private Text _state;
        private Text _date;
        private Text _resources;
        private Text _creed;
        private Text _offer;
        private Text _consequence;
        private Text _assignee;
        private Text _outcome;
        private Text _message;
        private Text _chronicle;

        [SetUp]
        public void SetUp()
        {
            _session = GameSession.NewGame();
            _host = new GameObject("CommissionPanelHost", typeof(RectTransform));
            _host.GetComponent<RectTransform>().sizeDelta = new Vector2(1216f, 268f);
            _page = Child(_host.transform, "Page", typeof(Image));
            _open = MakeButton(_host.transform, "Open", "护送委托");
            _close = MakeButton(_page.transform, "Close", "关闭");
            _accept = MakeButton(_page.transform, "Accept", "接取委托");
            _title = MakeText(_page.transform, "Title");
            _state = MakeText(_page.transform, "State");
            _date = MakeText(_page.transform, "Date");
            _resources = MakeText(_page.transform, "Resources");
            _creed = MakeText(_page.transform, "Creed");
            _offer = MakeText(_page.transform, "Offer");
            _consequence = MakeText(_page.transform, "Consequence");
            _assignee = MakeText(_page.transform, "Assignee");
            _outcome = MakeText(_page.transform, "Outcome");
            _message = MakeText(_page.transform, "Message");
            _chronicle = MakeText(_page.transform, "Chronicle");
            _content = Child(_page.transform, "CandidateContent", typeof(RectTransform),
                typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            _template = Child(_content.transform, "CandidateTemplate", typeof(Image),
                typeof(Button), typeof(LayoutElement));
            Text templateLabel = MakeText(_template.transform, "Label");
            templateLabel.text = "姓名";
            _template.SetActive(false);

            _panel = _host.AddComponent<ShadowEscortCommissionPanel>();
            _panel.BindReferences(_session, _page, _open, _close, _accept, _title, _state, _date,
                _resources, _creed, _offer, _consequence, _assignee, _outcome, _message, _chronicle,
                _content.transform, _template);
        }

        [TearDown]
        public void TearDown()
        {
            if (_host != null) Object.DestroyImmediate(_host);
            if (_session != null) Object.DestroyImmediate(_session.gameObject);
        }

        [Test]
        public void MissingSessionAndReferences_DoNotThrowAndDisableAccept()
        {
            GameObject orphan = new GameObject("OrphanPanel", typeof(RectTransform));
            try
            {
                // The projection writes into whatever references it has, so give it the
                // minimal visible surface a missing session must still fill in.
                GameObject page = Child(orphan.transform, "OrphanPage", typeof(Image));
                Text state = MakeText(page.transform, "State");
                Text message = MakeText(page.transform, "Message");
                Button accept = MakeButton(page.transform, "Accept", "接取委托");
                ShadowEscortCommissionPanel panel = orphan.AddComponent<ShadowEscortCommissionPanel>();
                panel.BindReferences(null, page, null, null, accept, null, state, null, null,
                    null, null, null, null, null, message, null, null, null);

                Assert.That(panel.IsBound, Is.False, "A missing session can never count as bound.");
                Assert.DoesNotThrow(() => panel.Refresh());
                Assert.DoesNotThrow(() => panel.Open());
                Assert.DoesNotThrow(() => panel.Close());
                Assert.DoesNotThrow(() => panel.Accept());
                Assert.That(panel.AcceptInteractable, Is.False);
                Assert.That(panel.StateLine, Does.Contain("不可用"));
                Assert.That(panel.MessageLine, Does.Contain("缺少 GameSession"));
                Assert.That(panel.CandidateCount, Is.Zero);
            }
            finally
            {
                Object.DestroyImmediate(orphan);
            }
        }

        [Test]
        public void Offer_IsAcceptableOnlyAtTheClanSeat()
        {
            _panel.Open();

            Assert.That(_panel.StateLine, Does.Contain("待接取").And.Contain("清洲"));
            Assert.That(_panel.AcceptInteractable, Is.True);
            Assert.That(_panel.CreedLine, Does.Contain("接取投入").And.Contain(
                ShadowEscortCommission.InvestmentFundsFor(_session.CurrentFamilyCreed)
                    .ToString()));
            Assert.That(_panel.OfferLine, Does.Contain("武艺").And.Contain("3 个模拟日"));

            TravelPreview preview = _session.PreviewTravel(LocationId.Nagoya).Value;
            Assert.That(_session.ConfirmTravel(preview), Is.True);
            Assert.That(_panel.AcceptInteractable, Is.False,
                "The accept button must follow the clan-seat location gate.");
            Assert.That(_panel.StateLine, Does.Contain("需返回清洲"));
        }

        [Test]
        public void ModeChoices_AreTwoExplicitRowsAndFreezeAtAssignment()
        {
            _panel.Open();
            Assert.That(_session.CommissionStage, Is.EqualTo(ShadowEscortStage.NotAccepted));
            Assert.That(_panel.CandidateCount, Is.Zero,
                "assignee rows appear only after the offer is accepted.");

            Transform manual = _content.transform.Find("CommissionMode_Manual");
            Transform delegated = _content.transform.Find("CommissionMode_Delegated");
            Assert.That(manual, Is.Not.Null, "manual mode choice must be reachable");
            Assert.That(delegated, Is.Not.Null, "delegated mode choice must be reachable");
            Assert.That(LabelOf(delegated), Does.StartWith("✔"),
                "the default delegated mode is shown as selected");
            Assert.That(_session.CommissionMode, Is.EqualTo(MissionOperationMode.Delegated));

            manual.GetComponent<Button>().onClick.Invoke();
            Transform refreshedManual = _content.transform.Find("CommissionMode_Manual");
            Assert.That(_session.CommissionMode, Is.EqualTo(MissionOperationMode.Manual));
            Assert.That(LabelOf(refreshedManual), Does.StartWith("✔"));
            Assert.That(LabelOf(_content.transform.Find("CommissionMode_Delegated")),
                Does.Not.StartWith("✔"));

            // The accepted offer is the state that builds assignee rows; the mode stays
            // editable until execution actually starts.
            _panel.Accept();
            Assert.That(_session.CommissionStage, Is.EqualTo(ShadowEscortStage.Accepted));
            Assert.That(_panel.CandidateCount, Is.GreaterThan(0),
                "the accepted offer must list its assignees.");
            Assert.That(LabelOf(_content.transform.Find("CommissionMode_Manual")),
                Does.StartWith("✔"), "the chosen mode is still shown before assignment.");

            int index = -1;
            for (int i = 0; i < _panel.CandidateCount; i++)
                if (_panel.CandidateInteractable(i))
                {
                    index = i;
                    break;
                }
            Assert.That(index, Is.GreaterThanOrEqualTo(0), "an eligible assignee must exist");
            _panel.ClickCandidate(index);
            _panel.Refresh();

            Assert.That(_session.CommissionStage, Is.EqualTo(ShadowEscortStage.InProgress));
            Assert.That(_content.transform.Find("CommissionMode_Manual"), Is.Null,
                "the mode is frozen once execution starts.");
            Assert.That(_session.TrySetCommissionMode(MissionOperationMode.Manual, out string error),
                Is.False);
            Assert.That(error, Does.Contain("执行方式"));
        }

        [Test]
        public void ManualDueSettlement_IsReachableThroughTheAcceptButton()
        {
            Assert.That(_session.TryAcceptCommission().Succeeded, Is.True);
            Assert.That(_session.TrySetCommissionMode(MissionOperationMode.Manual, out _), Is.True);
            PersonId assignee = default;
            IReadOnlyList<CommissionAssigneeOption> options = _session.CommissionAssignees;
            for (int i = 0; i < options.Count; i++)
                if (options[i].Eligible)
                {
                    assignee = options[i].PersonId;
                    break;
                }
            Assert.That(assignee.IsEmpty, Is.False);
            Assert.That(_session.TryAssignCommission(assignee).Succeeded, Is.True);
            for (int i = 0; i < ShadowEscortCommission.DurationDays; i++)
                Assert.That(_session.AdvanceOneDay(), Is.True);

            _panel.Open();
            Assert.That(_panel.AcceptInteractable, Is.True,
                "the due manual commission exposes the settle action.");
            Assert.That(LabelOf(_accept.transform), Is.EqualTo("结算委托"));
            Assert.That(_session.MissionHistory, Is.Empty);

            _panel.Accept();
            Assert.That(_session.MissionHistory.Count, Is.EqualTo(1));
            Assert.That(_session.CommissionStage, Is.Not.EqualTo(ShadowEscortStage.InProgress));
        }

        /// <summary>
        /// A delegated due-day settlement that fails leaves the row InProgress with a
        /// completed window. No public M05 play sequence can induce a non-terminal
        /// settlement failure (the unexecutable-assignee case closes as Abandoned, and the
        /// other failures need corrupt state or fault injection), so this test rebuilds
        /// that exact post-failure row from a legal save. The reload date is two days past
        /// the due date, which makes the recorded date source distinguishable: a retry
        /// that used the current day would record a different date than the canonical due
        /// date. Clicking the page must settle once through the existing action.
        /// </summary>
        [Test]
        public void DelegatedDueRowLeftByAFailedSettlement_CanRetryOnceOnTheDueDate()
        {
            PersonId assignee = FindFreeMember();
            Assert.That(assignee.IsEmpty, Is.False);
            Assert.That(_session.TryAcceptCommission().Succeeded, Is.True);
            Assert.That(_session.TrySetCommissionMode(MissionOperationMode.Delegated,
                out string modeError), Is.True, modeError);
            Assert.That(_session.TryAssignCommission(assignee).Succeeded, Is.True,
                _session.LastCommissionMessage);

            GameDate start = _session.WorldClock.CurrentDate;
            Assert.That(GameDate.TryAddDays(start, ShadowEscortCommission.DurationDays,
                out GameDate due), Is.True);
            Assert.That(GameDate.TryAddDays(due, 2, out GameDate reloadDate), Is.True);

            // Rebuild the exact row a failed due-day settlement leaves behind: still
            // executing at the completed window, with the same frozen run ID, start date,
            // assignee and mode, and no result data. The saved day is after the due date.
            SaveData data = _session.CaptureSave();
            data.year = reloadDate.Year;
            data.month = reloadDate.Month;
            data.day = reloadDate.Day;
            data.commission.elapsedDays = data.commission.durationDays;
            data.commission.stage = (int)ShadowEscortStage.InProgress;
            SaveOperationResult applied = _session.TryApplySave(data);
            Assert.That(applied.Succeeded, Is.True, applied.Message);
            Assert.That(_session.CommissionStage, Is.EqualTo(ShadowEscortStage.InProgress));
            Assert.That(_session.IsCommissionDue, Is.True);
            Assert.That(_session.IsCommissionDelegatedSettlementRetryAvailable, Is.True);
            Assert.That(_session.WorldClock.CurrentDate, Is.EqualTo(reloadDate),
                "the reload day is two days after the due date.");
            Assert.That(_session.MissionHistory, Is.Empty,
                "loading the failed row must not settle it implicitly.");

            _panel.Open();
            Assert.That(_panel.AcceptInteractable, Is.True,
                "a failed due-day delegated settlement must stay retryable.");
            Assert.That(LabelOf(_accept.transform), Is.EqualTo("重试结算"));
            Assert.That(_panel.OutcomeLine, Does.Contain("重试结算"));

            _panel.Accept();
            Assert.That(_session.MissionHistory.Count, Is.EqualTo(1));
            Assert.That(_session.CommissionStage, Is.Not.EqualTo(ShadowEscortStage.InProgress));
            MissionRecord record = _session.MissionHistory[0];
            Assert.That(record.Mode, Is.EqualTo(MissionOperationMode.Delegated));
            Assert.That(record.RunId, Is.EqualTo(GameSession.CommissionRunId));
            Assert.That(record.Date, Is.EqualTo(due),
                "the retry records the canonical due date, not the reload date.");
            Assert.That(record.Date, Is.Not.EqualTo(_session.WorldClock.CurrentDate),
                "the clock stays on the reload day, so the two dates are distinguishable.");
            Assert.That(_session.IsCommissionDelegatedSettlementRetryAvailable, Is.False);

            _panel.Refresh();
            Assert.That(_panel.AcceptInteractable, Is.False,
                "a settled retry offers no second settlement.");
            _panel.Accept();
            Assert.That(_session.MissionHistory.Count, Is.EqualTo(1));
        }

        private static string LabelOf(Transform row)
        {
            if (row == null)
                return string.Empty;
            Text label = row.GetComponentInChildren<Text>(true);
            return label != null ? label.text : string.Empty;
        }

        [Test]
        public void Accept_DeductsInvestmentExactlyOnceAndShowsAssignmentChoices()
        {
            int fundsBefore = _session.Industry.Resources.Funds;
            int expectedCost = ShadowEscortCommission.InvestmentFundsFor(_session.CurrentFamilyCreed);
            _panel.Open();

            _panel.Accept();
            Assert.That(_panel.MessageLine, Does.Contain("已接取"),
                "Accepting must show the investment feedback immediately.");
            _panel.Accept();

            Assert.That(_session.CommissionStage, Is.EqualTo(ShadowEscortStage.Accepted));
            Assert.That(_session.Industry.Resources.Funds, Is.EqualTo(fundsBefore - expectedCost),
                "A double click must not pay the investment twice.");
            Assert.That(_panel.AcceptInteractable, Is.False);
            Assert.That(_panel.CandidateCount, Is.GreaterThan(0));
            Assert.That(_panel.EligibleCandidateCount, Is.GreaterThan(0));
            Assert.That(_panel.StateLine, Does.Contain("等待指派"));
            Assert.That(_panel.AssigneeLine, Does.Contain("待指派"));
            Assert.That(_panel.MessageLine, Does.Contain("不能重复接取"),
                "The rejected second click must surface its duplicate-rejection feedback.");
            Assert.That(_panel.MessageLine, Does.Not.Contain("已接取"),
                "Only one acceptance can be reported, so no second paid acceptance appears.");
        }

        [Test]
        public void BusyMember_IsListedWithReasonAndCannotBeAssigned()
        {
            PersonId head = _session.Succession.HeadPersonId;
            Assert.That(_session.TryAcceptCommission().Succeeded, Is.True);
            Assert.That(_session.TryPlanDailyAction(head, DailyActionKind.Rest, 2, default,
                out string reason), Is.True, reason);
            _panel.Open();

            CommissionAssigneeOption headOption = default;
            foreach (CommissionAssigneeOption option in _session.CommissionAssignees)
                if (option.PersonId == head)
                    headOption = option;
            Assert.That(headOption.PersonId, Is.EqualTo(head));
            Assert.That(headOption.Eligible, Is.False);
            Assert.That(headOption.Reason, Is.EqualTo("有进行中的每日行动"));
            Assert.That(headOption.DisplayName, Is.Not.Empty);

            int headIndex = IndexOf(head);
            Assert.That(headIndex, Is.GreaterThanOrEqualTo(0));
            Assert.That(_panel.CandidateInteractable(headIndex), Is.False);
            Assert.That(_panel.CandidateLabel(headIndex),
                Does.Contain(headOption.DisplayName).And.Contain("有进行中的每日行动"));

            int otherIndex = FirstEligibleIndex(head);
            Assert.That(otherIndex, Is.GreaterThanOrEqualTo(0),
                "The fixed roster must keep at least one free member.");
            Assert.That(_panel.CandidateInteractable(otherIndex), Is.True);
        }

        [Test]
        public void Assign_StartsCountdownAndReassignmentIsRejected()
        {
            Assert.That(_session.TryAcceptCommission().Succeeded, Is.True);
            _panel.Open();
            int index = FirstEligibleIndex(default);
            Assert.That(index, Is.GreaterThanOrEqualTo(0));
            PersonId assignee = _panel.CandidatePerson(index);
            int funds = _session.Industry.Resources.Funds;

            _panel.ClickCandidate(index);

            Assert.That(_session.CommissionStage, Is.EqualTo(ShadowEscortStage.InProgress));
            Assert.That(_session.CommissionAssignee, Is.EqualTo(assignee));
            Assert.That(_session.Industry.Resources.Funds, Is.EqualTo(funds),
                "Assignment itself must not move resources.");
            Assert.That(_panel.CandidateCount, Is.Zero,
                "An executing commission must not offer a second assignee.");
            Assert.That(_panel.AssigneeLine, Does.Contain("已过 0/" +
                ShadowEscortCommission.DurationDays + " 天").And.Contain("剩余 " +
                ShadowEscortCommission.DurationDays + " 天"));

            PersonId other = OtherPerson(assignee);
            Assert.That(other.IsEmpty, Is.False,
                "the fixed roster must keep a second member for the reassignment attempt.");
            ShadowEscortActionResult reassign = _session.TryAssignCommission(other);
            Assert.That(reassign.Succeeded, Is.False,
                "a running commission must reject a second assignee.");
            Assert.That(reassign.Feedback,
                Is.EqualTo("该委托已经指定执行人或已经结算。"),
                "the rejection must name the running-assignment rule, not any generic text.");
            Assert.That(_session.CommissionStage,
                Is.EqualTo(ShadowEscortStage.InProgress));
            Assert.That(_session.CommissionAssignee, Is.EqualTo(assignee),
                "Reassignment must not replace a running assignee.");
        }

        [Test]
        public void Countdown_RefreshesAcrossDaysTravelAndMonthSettlement()
        {
            PersonId assignee = FindFreeMember();
            Assert.That(assignee.IsEmpty, Is.False);
            Assert.That(_session.TryAcceptCommission().Succeeded, Is.True);
            Assert.That(_session.TryAssignCommission(assignee).Succeeded, Is.True);
            _panel.Open();
            int refreshes = _panel.RefreshCount;

            Assert.That(_session.AdvanceDays(1), Is.True);
            Assert.That(_panel.RefreshCount, Is.GreaterThan(refreshes));
            Assert.That(_panel.AssigneeLine, Does.Contain("已过 1/3 天").And.Contain("剩余 2 天"));

            TravelPreview preview = _session.PreviewTravel(LocationId.Nagoya).Value;
            Assert.That(_session.ConfirmTravel(preview), Is.True);
            Assert.That(_panel.AssigneeLine, Does.Contain("已过 2/3 天").And.Contain("剩余 1 天"));

            Assert.That(_session.TryAdvanceToNextMonth(), Is.True, _session.LastEventMessage);
            Assert.That(_session.CommissionOutcome, Is.Not.EqualTo(CommissionOutcome.None));
            Assert.That(_panel.OutcomeLine, Does.Contain("结果："));
            Assert.That(_panel.OutcomeLine, Does.Contain("尾张商队信用"),
                "the panel must label the relationship instead of showing its stable ID.");
            Assert.That(_panel.OutcomeLine,
                Does.Not.Contain(MissionRelationIds.ShadowEscortMerchant));
            Assert.That(_panel.CandidateCount, Is.Zero);
            Assert.That(_panel.ChronicleLine, Does.Contain("编年史：护送委托"));
            Assert.That(_panel.ChronicleLine, Does.Contain("1560-04-04"));
        }

        [Test]
        public void SettledFailure_ShowsScoreRollSpentInvestmentAndNoReward()
        {
            PersonId head = _session.Succession.HeadPersonId;
            Assert.That(_session.TryAcceptCommission().Succeeded, Is.True);
            Assert.That(_session.TryAssignCommission(head).Succeeded, Is.True);
            Assert.That(_session.AdvanceDays(ShadowEscortCommission.DurationDays), Is.True);
            SaveData data = _session.CaptureSave();
            data.commission.stage = (int)ShadowEscortStage.Failed;
            data.commission.outcome = (int)CommissionOutcome.Failure;
            data.commission.elapsedDays = data.commission.durationDays;
            data.commission.score = 12;
            data.commission.roll = 7;
            data.commission.rewardFunds = 0;
            data.commission.rewardSupplies = 0;
            data.commission.lastFeedback = "失败的护送。";

            GameSession loaded = GameSession.NewGame();
            try
            {
                SaveOperationResult applied = loaded.TryApplySave(data);
                Assert.That(applied.Succeeded, Is.True, applied.Message);
                _panel.Bind(loaded);
                _panel.Open();

                Assert.That(_panel.OutcomeLine, Does.Contain("失败"));
                Assert.That(_panel.OutcomeLine, Does.Contain("总分 12"));
                Assert.That(_panel.OutcomeLine, Does.Contain("掷值 7"));
                Assert.That(_panel.OutcomeLine, Does.Contain("已消耗"));
                Assert.That(_panel.OutcomeLine, Does.Not.Contain("+"));
                Assert.That(_panel.MessageLine, Is.EqualTo("失败的护送。"));
            }
            finally
            {
                Object.DestroyImmediate(loaded.gameObject);
            }
        }

        [Test]
        public void SaveLoad_RebindsInProgressCountdown()
        {
            PersonId head = _session.Succession.HeadPersonId;
            Assert.That(_session.TryAcceptCommission().Succeeded, Is.True);
            Assert.That(_session.TryAssignCommission(head).Succeeded, Is.True);
            Assert.That(_session.AdvanceDays(1), Is.True);
            SaveData data = _session.CaptureSave();

            GameSession loaded = GameSession.NewGame();
            try
            {
                Assert.That(loaded.TryApplySave(data).Succeeded, Is.True);
                _panel.Bind(loaded);
                _panel.Open();

                Assert.That(_panel.StateLine, Does.Contain("执行中"));
                Assert.That(_panel.AssigneeLine, Does.Contain("已过 1/3 天").And.Contain("剩余 2 天"));
                Assert.That(_panel.CandidateCount, Is.Zero);
            }
            finally
            {
                Object.DestroyImmediate(loaded.gameObject);
            }
        }

        [Test]
        public void CloseAndTownNavigation_DoNotChangeGameplayState()
        {
            PersonId assignee = FindFreeMember();
            Assert.That(assignee.IsEmpty, Is.False);
            Assert.That(_session.TryAcceptCommission().Succeeded, Is.True);
            Assert.That(_session.TryAssignCommission(assignee).Succeeded, Is.True);
            Assert.That(_session.AdvanceDays(1), Is.True);
            string before = JsonUtility.ToJson(_session.CaptureSave());
            GameDate date = _session.WorldClock.CurrentDate;
            LocationId location = _session.CurrentLocation;
            int funds = _session.Industry.Resources.Funds;
            int entries = _session.ChronicleEntries.Count;

            _panel.Open();
            _panel.Close();
            _panel.Open();
            _panel.Close();

            GameObject map = new GameObject("CommissionMap", typeof(RectTransform),
                typeof(WorldMapController));
            GameObject town = new GameObject("CommissionTown", typeof(RectTransform));
            try
            {
                town.transform.SetParent(map.transform, false);
                WorldMapController controller = map.GetComponent<WorldMapController>();
                SetReference(controller, "gameSession", _session);
                SetReference(controller, "townMenuRoot", town);
                controller.BindShadowEscortCommissionPanel(_panel);
                controller.OpenTownMenu();
                controller.ToggleShadowEscortCommissionPanel();
                Assert.That(controller.IsShadowEscortCommissionOpen, Is.True);
                controller.ReturnToMap();
                Assert.That(controller.IsShadowEscortCommissionOpen, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(map);
                Object.DestroyImmediate(town);
            }

            Assert.That(_session.WorldClock.CurrentDate, Is.EqualTo(date));
            Assert.That(_session.CurrentLocation, Is.EqualTo(location));
            Assert.That(_session.Industry.Resources.Funds, Is.EqualTo(funds));
            Assert.That(_session.ChronicleEntries.Count, Is.EqualTo(entries));
            Assert.That(_session.CommissionStage, Is.EqualTo(ShadowEscortStage.InProgress));
            Assert.That(JsonUtility.ToJson(_session.CaptureSave()), Is.EqualTo(before),
                "Open, close and town navigation must not mutate the saved state.");
        }

        [Test]
        public void NoEligibleMembers_ListsEveryReasonAndDisablesAssignment()
        {
            Assert.That(_session.TryAcceptCommission().Succeeded, Is.True);
            // Snapshot the offer before touching the day planner: a member who is already
            // blocked (work duty, research, mentorship, industry, an action NewGame's NPC
            // pass planned) keeps that documented reason, and only members the offer still
            // calls eligible are made busy by this test.
            var baseline = new List<CommissionAssigneeOption>();
            var expectedReasons = new Dictionary<PersonId, string>();
            foreach (CommissionAssigneeOption option in _session.CommissionAssignees)
            {
                baseline.Add(option);
                if (!option.Eligible)
                    expectedReasons[option.PersonId] = option.Reason;
            }
            Assert.That(baseline.Count, Is.GreaterThan(0));
            foreach (CommissionAssigneeOption option in baseline)
            {
                if (!option.Eligible)
                    continue;
                if (_session.TryPlanDailyAction(option.PersonId, DailyActionKind.Rest, 2,
                        default, out string reason))
                {
                    expectedReasons[option.PersonId] = "有进行中的每日行动";
                    continue;
                }
                Assert.That(_session.HasActiveDailyAction(option.PersonId), Is.True,
                    "Person " + option.PersonId.Value + " was offered as free but could not " +
                    "be made busy: " + reason);
                expectedReasons[option.PersonId] = "有进行中的每日行动";
            }

            _panel.Open();

            Assert.That(_panel.CandidateCount, Is.EqualTo(baseline.Count));
            Assert.That(_panel.EligibleCandidateCount, Is.Zero);
            Assert.That(_panel.AssigneeLine, Does.Contain("无符合条件的家人"));
            for (int i = 0; i < _panel.CandidateCount; i++)
            {
                PersonId personId = _panel.CandidatePerson(i);
                Assert.That(expectedReasons.ContainsKey(personId), Is.True,
                    "Candidate " + personId.Value + " had no expected reason recorded.");
                Assert.That(_panel.CandidateInteractable(i), Is.False,
                    "Every candidate must be unassignable while all family members are busy.");
                Assert.That(_panel.CandidateLabel(i), Does.Contain(expectedReasons[personId]),
                    "Candidate " + personId.Value + " must show why it cannot be assigned.");
            }
        }

        /// <summary>
        /// The candidate row carries the same work-duty reason the command returns, so a
        /// player sees why a young family member cannot be sent and the click cannot
        /// silently assign them.
        /// </summary>
        [Test]
        public void MinorAndRetiredMembers_ShowWorkDutyReasonsAndCannotBeAssigned()
        {
            _session.InitStandardDemo();
            var minor = new PersonId("person.akechi.nobumasa");
            PersonId oldHead = _session.Succession.HeadPersonId;
            Assert.That(_session.BeginSuccessionRetirement().Succeeded, Is.True,
                _session.LastSuccessionMessage);
            Assert.That(_session.SelectSuccessionSuccessor(
                new PersonId("person.akechi.masatsuna")).Succeeded, Is.True,
                _session.LastSuccessionMessage);
            Assert.That(_session.ConfirmSuccession().Succeeded, Is.True,
                _session.LastSuccessionMessage);
            Assert.That(_session.TryAcceptCommission().Succeeded, Is.True);
            _panel.Open();

            int minorIndex = IndexOf(minor);
            Assert.That(minorIndex, Is.GreaterThanOrEqualTo(0),
                "a minor living family member is listed with a reason, not hidden.");
            Assert.That(_panel.CandidateInteractable(minorIndex), Is.False);
            Assert.That(_panel.CandidateLabel(minorIndex), Does.Contain("未成年"));
            _panel.ClickCandidate(minorIndex);
            Assert.That(_session.CommissionAssignee.IsEmpty, Is.True);
            Assert.That(_panel.MessageLine, Does.Contain("未成年"));

            int retiredIndex = IndexOf(oldHead);
            Assert.That(retiredIndex, Is.GreaterThanOrEqualTo(0));
            Assert.That(_panel.CandidateInteractable(retiredIndex), Is.False);
            Assert.That(_panel.CandidateLabel(retiredIndex), Does.Contain("已隐居（退休）"));
            _panel.ClickCandidate(retiredIndex);
            Assert.That(_session.CommissionStage, Is.EqualTo(ShadowEscortStage.Accepted));
            Assert.That(_session.CommissionAssignee.IsEmpty, Is.True);
            Assert.That(_panel.MessageLine, Does.Contain("隐居"));
        }

        private int IndexOf(PersonId personId)
        {
            for (int i = 0; i < _panel.CandidateCount; i++)
                if (_panel.CandidatePerson(i) == personId)
                    return i;
            return -1;
        }

        private int FirstEligibleIndex(PersonId excluded)
        {
            for (int i = 0; i < _panel.CandidateCount; i++)
                if (_panel.CandidatePerson(i) != excluded && _panel.CandidateInteractable(i))
                    return i;
            return -1;
        }

        /// <summary>
        /// The crisis demo replaces the whole family snapshot, so an abandoned M05 row can
        /// keep an assignee the new roster no longer contains. The page must show a readable
        /// label for that unavailable family member instead of the raw PersonId.
        /// </summary>
        [Test]
        public void AbandonedAssignee_RemovedByTheCrisisSwap_ShowsAReadableLabel()
        {
            _session.InitStandardDemo();
            var crisisRoster = new HashSet<PersonId>();
            foreach (FamilyMembership membership in
                SuccessionSample.CreateCrisis().World.Family.Members)
                crisisRoster.Add(membership.PersonId);

            PersonId assignee = default;
            foreach (CommissionAssigneeOption option in _session.CommissionAssignees)
                if (option.Eligible && !crisisRoster.Contains(option.PersonId))
                {
                    assignee = option.PersonId;
                    break;
                }
            Assert.That(assignee.IsEmpty, Is.False,
                "the standard roster must offer an assignee the crisis roster drops.");
            Assert.That(_session.TryAcceptCommission().Succeeded, Is.True);
            Assert.That(_session.TryAssignCommission(assignee).Succeeded, Is.True,
                _session.LastCommissionMessage);
            Assert.That(_session.AdvanceDays(1), Is.True);

            // Lose the running assignee so the due date closes the row once as Abandoned.
            Assert.That(_session.Succession.World.TryGetLife(assignee,
                out SuccessionPersonLife life), Is.True);
            Assert.That(_session.Succession.World.TrySetLife(assignee,
                    new SuccessionPersonLife(life.BirthYear, false, life.Retired),
                    out string killError), Is.True, killError);
            Assert.That(_session.AdvanceDays(_session.CommissionRemainingDays), Is.True);
            Assert.That(_session.CommissionStage, Is.EqualTo(ShadowEscortStage.Abandoned),
                _session.LastCommissionMessage);

            Assert.That(_session.StartCrisisDemo(), Is.True, _session.LastSuccessionMessage);
            Assert.That(_session.Succession.World.Family.IsMember(assignee), Is.False,
                "the crisis roster no longer contains the recorded assignee.");

            _panel.Open();
            Assert.That(_panel.AssigneeLine, Does.Contain("执行中止"));
            Assert.That(_panel.AssigneeLine, Does.Contain("已离开或不可用的家人"),
                "an outlived assignee must read as plain player text.");
            Assert.That(_panel.AssigneeLine, Does.Not.Contain(assignee.Value),
                "the stable PersonId must not leak into the player-facing label.");
            Assert.That(_panel.OutcomeLine, Does.Contain("执行中止"));
        }

        private PersonId FindFreeMember()
        {
            PersonId found = default;
            foreach (CommissionAssigneeOption option in _session.CommissionAssignees)
                if (option.Eligible && option.PersonId != _session.Succession.HeadPersonId)
                {
                    found = option.PersonId;
                    break;
                }
            return found;
        }

        private PersonId OtherPerson(PersonId excluded)
        {
            foreach (CommissionAssigneeOption option in _session.CommissionAssignees)
                if (option.PersonId != excluded)
                    return option.PersonId;
            return default;
        }

        private static void SetReference(Object target, string field, Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject Child(Transform parent, string name, params System.Type[] types)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            foreach (System.Type type in types)
            {
                if (go.GetComponent(type) != null)
                    continue;
                go.AddComponent(type);
            }
            return go;
        }

        private static Text MakeText(Transform parent, string name)
        {
            GameObject go = Child(parent, name, typeof(Text));
            Text text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return text;
        }

        private static Button MakeButton(Transform parent, string name, string label)
        {
            GameObject go = Child(parent, name, typeof(Image), typeof(Button));
            Image image = go.GetComponent<Image>();
            Button button = go.GetComponent<Button>();
            button.targetGraphic = image;
            Text text = MakeText(go.transform, "Label");
            text.text = label;
            return button;
        }
    }
}
