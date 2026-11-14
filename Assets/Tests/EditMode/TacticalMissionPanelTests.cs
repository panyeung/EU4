using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;
using SengokuChronicle.Presentation;
using Object = UnityEngine.Object;

namespace SengokuChronicle.Tests.EditMode
{
    /// <summary>
    /// Focused projection tests for the M06 infiltration page. They assert what a player
    /// can see and submit: the grid glyphs, guard sight, the alert line, enabled actions,
    /// terminal results with actual resources, and that refresh/open/close never change
    /// the saved campaign state.
    /// </summary>
    public sealed class TacticalMissionPanelTests
    {
        private GameSession _session;
        private GameObject _host;
        private GameObject _page;
        private GameObject _grid;
        private GameObject _candidateScroll;
        private GameObject _candidateContent;
        private GameObject _candidateTemplate;
        private TacticalMissionPanel _panel;
        private Image[] _cells;
        private Text[] _cellLabels;
        private Button _open;
        private Button _close;
        private Button _start;
        private Button _north;
        private Button _east;
        private Button _south;
        private Button _west;
        private Button _move;
        private Button _sprint;
        private Button _wait;
        private Button _interact;
        private Button _extract;
        private Button _withdraw;
        private Button _switch;
        private Text _title;
        private Text _state;
        private Text _date;
        private Text _resources;
        private Text _objective;
        private Text _team;
        private Text _status;
        private Text _guard;
        private Text _result;
        private Text _event;
        private Text _message;
        private Text _chronicle;
        private Text _selection;

        [SetUp]
        public void SetUp()
        {
            _session = GameSession.NewGame();
            _host = new GameObject("TacticalPanelHost", typeof(RectTransform));
            _host.GetComponent<RectTransform>().sizeDelta = new Vector2(1216f, 268f);
            _page = Child(_host.transform, "Page", typeof(Image));
            _open = MakeButton(_host.transform, "Open", "潜入密令");
            _close = MakeButton(_page.transform, "Close", "关闭");
            _start = MakeButton(_page.transform, "Start", "开始潜入");
            _north = MakeButton(_page.transform, "North", "北");
            _east = MakeButton(_page.transform, "East", "东");
            _south = MakeButton(_page.transform, "South", "南");
            _west = MakeButton(_page.transform, "West", "西");
            _move = MakeButton(_page.transform, "Move", "移动");
            _sprint = MakeButton(_page.transform, "Sprint", "疾行");
            _wait = MakeButton(_page.transform, "Wait", "等待");
            _interact = MakeButton(_page.transform, "Interact", "夺取");
            _extract = MakeButton(_page.transform, "Extract", "撤离");
            _withdraw = MakeButton(_page.transform, "Withdraw", "撤退");
            _switch = MakeButton(_page.transform, "Switch", "换人");
            _title = MakeText(_page.transform, "Title");
            _state = MakeText(_page.transform, "State");
            _date = MakeText(_page.transform, "Date");
            _resources = MakeText(_page.transform, "Resources");
            _objective = MakeText(_page.transform, "Objective");
            _team = MakeText(_page.transform, "Team");
            _status = MakeText(_page.transform, "Status");
            _guard = MakeText(_page.transform, "Guard");
            _result = MakeText(_page.transform, "Result");
            _event = MakeText(_page.transform, "Event");
            _message = MakeText(_page.transform, "Message");
            _chronicle = MakeText(_page.transform, "Chronicle");
            _selection = MakeText(_page.transform, "Selection");

            _grid = Child(_page.transform, "Grid", typeof(GridLayoutGroup));
            _cells = new Image[TacticalMissionScenario.Width * TacticalMissionScenario.Height];
            _cellLabels = new Text[_cells.Length];
            for (int y = 0; y < TacticalMissionScenario.Height; y++)
            {
                for (int x = 0; x < TacticalMissionScenario.Width; x++)
                {
                    int index = y * TacticalMissionScenario.Width + x;
                    GameObject cell = Child(_grid.transform, "Cell_" + x + "_" + y,
                        typeof(Image));
                    _cells[index] = cell.GetComponent<Image>();
                    _cellLabels[index] = MakeText(cell.transform, "Glyph");
                }
            }

            _candidateScroll = Child(_page.transform, "CandidateScroll",
                typeof(Image), typeof(ScrollRect));
            _candidateContent = Child(_candidateScroll.transform, "CandidateContent",
                typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            _candidateTemplate = Child(_candidateContent.transform, "CandidateTemplate",
                typeof(Image), typeof(Button), typeof(LayoutElement));
            MakeText(_candidateTemplate.transform, "Label").text = "姓名";
            _candidateTemplate.SetActive(false);

            _panel = _host.AddComponent<TacticalMissionPanel>();
            _panel.BindReferences(_session, _page, _open, _close, _start, _north, _east, _south,
                _west, _move, _sprint, _wait, _interact, _extract, _withdraw, _switch, _title,
                _state, _date, _resources, _objective, _team, _status, _guard, _result, _event,
                _message, _chronicle, _selection, _grid, _cells, _cellLabels,
                _candidateContent.transform, _candidateTemplate, _candidateScroll);
        }

        [TearDown]
        public void TearDown()
        {
            if (_host != null) Object.DestroyImmediate(_host);
            if (_session != null) Object.DestroyImmediate(_session.gameObject);
        }

        [Test]
        public void Briefing_RepeatedRefreshKeepsOneRowPerCandidateAndTheSelection()
        {
            _panel.Open();
            int expected = _session.TacticalOperativeOptions.Count;
            Assert.That(_panel.CandidateCount, Is.EqualTo(expected));

            _panel.Refresh();
            _panel.Refresh();
            Assert.That(_panel.CandidateCount, Is.EqualTo(expected),
                "a briefing refresh must rebuild, not append, the candidate rows.");

            int first = -1;
            for (int i = 0; i < _panel.CandidateCount; i++)
            {
                if (!_panel.CandidateInteractable(i))
                    continue;
                first = i;
                break;
            }
            Assert.That(first, Is.GreaterThanOrEqualTo(0));
            _panel.ClickCandidate(first);

            Assert.That(_panel.CandidateCount, Is.EqualTo(expected));
            Assert.That(_panel.SelectedCount, Is.EqualTo(1));
            int selectedRows = 0;
            for (int i = 0; i < _panel.CandidateCount; i++)
                if (_panel.CandidateSelected(i))
                    selectedRows++;
            Assert.That(selectedRows, Is.EqualTo(1),
                "exactly one row carries the selected mark after the rebuild.");

            _panel.Refresh();
            Assert.That(_panel.CandidateCount, Is.EqualTo(expected));
            Assert.That(_panel.SelectedCount, Is.EqualTo(1));
        }

        [Test]
        public void Briefing_GuardLineMatchesTheAuthoredPosts()
        {
            _panel.Open();

            Assert.That(_panel.GuardLine, Does.Contain("(7,1)").And.Contain("朝东"));
            Assert.That(_panel.GuardLine, Does.Contain("(6,3)").And.Contain("朝北"));
        }

        [Test]
        public void ActiveOperation_ShowsInProgressInsteadOfNoneOutcome()
        {
            Assert.That(StartWithTwoFreeMembers().Succeeded, Is.True);
            _panel.Open();
            _panel.ClickNorth();
            _panel.ClickMove();

            Assert.That(_panel.StatusLine, Does.Contain("回合 1"));
            Assert.That(_panel.StatusLine, Does.Contain("进行中"));
            Assert.That(_panel.StatusLine, Does.Not.Contain("未开始"));
            Assert.That(_panel.StatusLine, Does.Contain("北"));
        }

        [Test]
        public void MissingSession_DoesNotThrowAndDisablesEveryControl()
        {
            GameObject orphan = new GameObject("OrphanTacticalPanel", typeof(RectTransform));
            try
            {
                GameObject page = Child(orphan.transform, "OrphanPage", typeof(Image));
                GameObject grid = Child(page.transform, "OrphanGrid", typeof(GridLayoutGroup));
                var cells = new Image[48];
                var labels = new Text[48];
                for (int i = 0; i < 48; i++)
                {
                    GameObject cell = Child(grid.transform, "OrphanCell" + i, typeof(Image));
                    cells[i] = cell.GetComponent<Image>();
                    labels[i] = MakeText(cell.transform, "Glyph");
                }
                GameObject scroll = Child(page.transform, "OrphanScroll",
                    typeof(Image), typeof(ScrollRect));
                GameObject template = Child(scroll.transform, "OrphanTemplate",
                    typeof(Image), typeof(Button));
                Text state = MakeText(page.transform, "OrphanState");
                Text message = MakeText(page.transform, "OrphanMessage");

                TacticalMissionPanel panel =
                    orphan.AddComponent<TacticalMissionPanel>();
                panel.BindReferences(null, page, null, null, null, null, null, null, null,
                    null, null, null, null, null, null, null, null, state, null, null,
                    null, null, null, null, null, null, message, null, null, grid, cells, labels,
                    scroll.transform, template, scroll);

                Assert.That(panel.IsBound, Is.False);
                Assert.DoesNotThrow(() => panel.Refresh());
                Assert.DoesNotThrow(() => panel.Open());
                Assert.DoesNotThrow(() => panel.Close());
                Assert.DoesNotThrow(() => panel.ClickMove());
                Assert.DoesNotThrow(() => panel.ClickWithdraw());
                Assert.That(panel.StateLine, Does.Contain("不可用"));
                Assert.That(panel.MessageLine, Does.Contain("缺少 GameSession"));
                Assert.That(panel.MoveInteractable, Is.False);
                Assert.That(panel.WithdrawInteractable, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(orphan);
            }
        }

        [Test]
        public void Briefing_RendersTheFixedMapAndStartsWithTwoSelectedOperatives()
        {
            _panel.Open();

            Assert.That(_panel.IsGridVisible, Is.True);
            Assert.That(_panel.IsCandidateListVisible, Is.True);
            Assert.That(_panel.CellCount, Is.EqualTo(48));
            Assert.That(_panel.CellLabel(3, 1), Is.EqualTo("壁"));
            Assert.That(_panel.CellLabel(2, 2), Is.EqualTo("掩"));
            Assert.That(_panel.CellLabel(0, 0), Is.EqualTo("撤"));
            Assert.That(_panel.CellLabel(6, 4), Is.EqualTo("报"));
            Assert.That(_panel.ObjectiveLine, Does.Contain("(0,0)").And.Contain("(1,0)"));
            Assert.That(_panel.ObjectiveLine, Does.Contain("(6,4)"));
            Assert.That(_panel.StateLine, Does.Contain("待出发"));
            Assert.That(_panel.SelectionLine, Does.Contain("已选择 0/2"));
            Assert.That(_panel.StartInteractable, Is.False);
            Assert.That(_panel.MoveInteractable, Is.False);

            int selected = 0;
            for (int i = 0; i < _panel.CandidateCount && selected < 2; i++)
            {
                if (!_panel.CandidateInteractable(i))
                    continue;
                _panel.ClickCandidate(i);
                selected++;
            }
            Assert.That(selected, Is.EqualTo(2));
            Assert.That(_panel.SelectedCount, Is.EqualTo(2));
            Assert.That(_panel.SelectionLine, Does.Contain("已选择 2/2"));
            Assert.That(_panel.StartInteractable, Is.True);

            _panel.ClickStart();

            Assert.That(_session.IsTacticalMissionActive, Is.True);
            Assert.That(_panel.IsCandidateListVisible, Is.False);
            Assert.That(_panel.StateLine, Does.Contain("潜入进行中"));
            Assert.That(_panel.TeamLine, Does.Contain("潜入队").And.Contain("@ (0,0)"));
            Assert.That(_panel.StatusLine, Does.Contain("回合 0"));
            Assert.That(_panel.MoveInteractable, Is.True);
            Assert.That(_panel.WaitInteractable, Is.True);
            Assert.That(_panel.MoveInteractable || _panel.SprintInteractable, Is.True);
            Assert.That(_panel.CellLabel(0, 0), Does.Contain("忍"));
        }

        [Test]
        public void ModeAndPreparationChoices_AreReachableAndDelegatedStartSettlesOnce()
        {
            _panel.Open();
            for (int i = 0; i < _panel.CandidateCount && _panel.SelectedCount < 2; i++)
                if (_panel.CandidateInteractable(i))
                    _panel.ClickCandidate(i);
            Assert.That(_panel.SelectedCount, Is.EqualTo(2));

            Transform manual = _candidateContent.transform.Find("TacticalMode_Manual");
            Transform delegated = _candidateContent.transform.Find("TacticalMode_Delegated");
            Transform noPrep = _candidateContent.transform.Find("TacticalPreparation_None");
            Transform scouting = _candidateContent.transform.Find("TacticalPreparation_Scouting");
            Assert.That(manual, Is.Not.Null, "manual mode choice must be reachable");
            Assert.That(delegated, Is.Not.Null, "delegated mode choice must be reachable");
            Assert.That(noPrep, Is.Not.Null, "no-preparation choice must be reachable");
            Assert.That(scouting, Is.Not.Null, "scouting choice must be reachable");

            scouting.GetComponent<Button>().onClick.Invoke();
            Assert.That(_panel.ObjectiveLine, Does.Contain("侦察路线")
                .And.Contain("(7,1) → (7,0)"));
            delegated = _candidateContent.transform.Find("TacticalMode_Delegated");
            Assert.That(delegated, Is.Not.Null);
            delegated.GetComponent<Button>().onClick.Invoke();
            Assert.That(_panel.ObjectiveLine, Does.Contain("自动委派"));

            _panel.ClickStart();

            Assert.That(_session.IsTacticalMissionActive, Is.False,
                "delegated play resolves from the frozen request.");
            Assert.That(_session.MissionHistory.Count, Is.EqualTo(1));
            Assert.That(_session.MissionHistory[0].Mode,
                Is.EqualTo(MissionOperationMode.Delegated));
            Assert.That(_session.MissionHistory[0].Preparation,
                Is.EqualTo(MissionPreparation.Scouting));
            Assert.That(_panel.ResultLine, Does.Contain("共享结果"));
        }

        [Test]
        public void Commands_UpdateTurnEventAndGuardSightLines()
        {
            Assert.That(StartWithTwoFreeMembers().Succeeded, Is.True);
            _panel.Open();
            _panel.ClickNorth();
            _panel.ClickMove();

            Assert.That(_panel.StatusLine, Does.Contain("回合 1").And.Contain("北"));
            Assert.That(_panel.EventLine, Does.Contain("移动到"));
            Assert.That(_panel.GuardLine, Does.Contain("朝").And.Contain("巡逻"));

            Assert.That(_panel.InteractInteractable, Is.False,
                "the operative starts far from the dispatch.");
            Assert.That(_panel.ExtractInteractable, Is.False);

            _panel.SwitchOperative();
            Assert.That(_panel.ActiveSlot, Is.EqualTo(1));

            _panel.ClickWait();
            Assert.That(_panel.StatusLine, Does.Contain("回合 2"));
        }

        [Test]
        public void GuardSight_IsRenderedOnTheGrid()
        {
            Assert.That(StartWithTwoFreeMembers().Succeeded, Is.True);
            _panel.Open();

            Assert.That(_panel.CellInGuardSight(6, 4), Is.True,
                "the stationary guard faces the dispatch cell.");
            Assert.That(_panel.CellInGuardSight(5, 4), Is.False,
                "a cover cell at distance two is concealed.");
            Assert.That(_panel.CellInGuardSight(0, 0), Is.False);
            Assert.That(_panel.GuardLine, Does.Contain("(6,3)").And.Contain("朝北"));
        }

        [Test]
        public void TerminalResult_ShowsExactAppliedResourcesAndDisablesActions()
        {
            Assert.That(StartWithTwoFreeMembers().Succeeded, Is.True);
            _panel.Open();
            _panel.ClickWithdraw();

            Assert.That(_session.IsTacticalMissionActive, Is.False);
            Assert.That(_panel.ResultLine, Does.Contain("撤退"));
            Assert.That(_panel.ResultLine, Does.Contain("资金 +0"));
            Assert.That(_panel.ResultLine, Does.Contain("物资 -" +
                TacticalMissionScenario.SupplyCost));
            Assert.That(_panel.ResultLine, Does.Contain("情报 +0"));
            Assert.That(_panel.ChronicleLine, Does.Contain("编年史：潜入密令"));
            Assert.That(_panel.MoveInteractable, Is.False);
            Assert.That(_panel.WithdrawInteractable, Is.False);
            Assert.That(_panel.StartInteractable, Is.False);
            Assert.That(_panel.IsCandidateListVisible, Is.False);
        }

        [Test]
        public void RefreshOpenAndClose_DoNotMutateTheCampaign()
        {
            Assert.That(StartWithTwoFreeMembers().Succeeded, Is.True);
            _panel.Open();
            _panel.ClickNorth();
            _panel.ClickMove();
            _panel.ClickWait();

            int turn = _session.TacticalMissionStatus.Turn;
            string before = JsonUtility.ToJson(_session.CaptureSave());
            GameDate date = _session.WorldClock.CurrentDate;
            LocationId location = _session.CurrentLocation;
            IndustryResources resources = _session.Industry.Resources;
            int chronicle = _session.ChronicleEntries.Count;

            _panel.Refresh();
            _panel.Close();
            _panel.Open();
            _panel.Refresh();

            Assert.That(_session.TacticalMissionStatus.Turn, Is.EqualTo(turn));
            Assert.That(_session.WorldClock.CurrentDate, Is.EqualTo(date));
            Assert.That(_session.CurrentLocation, Is.EqualTo(location));
            Assert.That(_session.Industry.Resources, Is.EqualTo(resources));
            Assert.That(_session.ChronicleEntries.Count, Is.EqualTo(chronicle));
            Assert.That(JsonUtility.ToJson(_session.CaptureSave()), Is.EqualTo(before));
        }

        [Test]
        public void TownNavigation_OpensAndClosesWithoutChangingState()
        {
            Assert.That(StartWithTwoFreeMembers().Succeeded, Is.True);
            string before = JsonUtility.ToJson(_session.CaptureSave());

            GameObject map = new GameObject("TacticalMap", typeof(RectTransform),
                typeof(WorldMapController));
            GameObject town = new GameObject("TacticalTown", typeof(RectTransform));
            try
            {
                town.transform.SetParent(map.transform, false);
                WorldMapController controller = map.GetComponent<WorldMapController>();
                SetReference(controller, "gameSession", _session);
                SetReference(controller, "townMenuRoot", town);
                controller.BindTacticalMissionPanel(_panel);
                controller.OpenTownMenu();
                controller.ToggleTacticalMissionPanel();
                Assert.That(controller.IsTacticalMissionOpen, Is.True);
                controller.ReturnToMap();
                Assert.That(controller.IsTacticalMissionOpen, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(map);
                Object.DestroyImmediate(town);
            }

            Assert.That(JsonUtility.ToJson(_session.CaptureSave()), Is.EqualTo(before));
        }

        [Test]
        public void FailedStart_ShowsTheReasonAndKeepsTheBriefing()
        {
            _panel.Open();
            // Force the start button path with no selection: the panel must refuse and
            // explain instead of starting an operation with zero operatives.
            _panel.ClickStart();

            Assert.That(_session.IsTacticalMissionActive, Is.False);
            Assert.That(_panel.IsCandidateListVisible, Is.True);
            Assert.That(_panel.StartInteractable, Is.False);
        }

        private TacticalActionResult StartWithTwoFreeMembers()
        {
            var eligible = new List<PersonId>();
            foreach (CommissionAssigneeOption option in _session.TacticalOperativeOptions)
                if (option.Eligible)
                    eligible.Add(option.PersonId);
            Assert.That(eligible.Count, Is.GreaterThanOrEqualTo(2));
            return _session.TryBeginTacticalMission(eligible[0], eligible[1]);
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
