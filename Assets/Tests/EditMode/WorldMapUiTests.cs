using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;
using SengokuChronicle.Presentation;
using Object = UnityEngine.Object;

namespace SengokuChronicle.Tests.EditMode
{
    /// <summary>
    /// M2-02 WorldMapController 状态机/EditMode 测试。
    /// 不依赖 Canvas：直接调用公开 API 驱动逻辑，断言状态与 UI 字段。
    /// </summary>
    public class WorldMapUiTests
    {
        private GameObject _go;
        private WorldMapController _controller;
        private GameSession _session;
        private WorldClockPanel _worldClockPanel;

        private Text _marker;
        private Text _feedback;
        private Text _curDate;
        private Text _destName;
        private Text _travel;
        private Text _arrival;
        private Button _confirm;
        private Button _cancel;
        private Button _return;
        private GameObject _townRoot;
        private Text _townTitle;
        private Text[] _facilities;

        [SetUp]
        public void SetUp()
        {
            // GameSession（新局：1560-04-01，位置清洲）
            _session = GameSession.NewGame();

            // WorldMapController + UI 占位对象
            _go = new GameObject("WorldMapController_Test");
            _controller = _go.AddComponent<WorldMapController>();

            // M1 WorldClockPanel 占位（用于验证 Refresh 同步）
            var wcpGo = new GameObject("WorldClockPanel");
            _worldClockPanel = wcpGo.AddComponent<WorldClockPanel>();
            var wcpDateText = CreateText("WCPDateText");
            var wcpAdv = CreateButton("WCPAdvButton");
            _worldClockPanel.SetDependencies(_session, wcpAdv, wcpDateText);

            _marker = CreateText("LocationMarker");
            _feedback = CreateText("Feedback");
            _curDate = CreateText("CurrentDate");
            _destName = CreateText("DestinationName");
            _travel = CreateText("TravelTime");
            _arrival = CreateText("ArrivalDate");
            _confirm = CreateButton("Confirm");
            _cancel = CreateButton("Cancel");
            _return = CreateButton("Return");

            _townRoot = new GameObject("TownMenuRoot", typeof(RectTransform));
            _townRoot.transform.SetParent(null);
            Image townBackground = _townRoot.AddComponent<Image>();
            townBackground.color = new Color(0.1f, 0.1f, 0.1f, 0.25f);
            _townRoot.SetActive(false);

            _townTitle = CreateText("TownTitle");
            _townTitle.transform.SetParent(_townRoot.transform, false);
            _facilities = new Text[] { CreateText("F1"), CreateText("F2"), CreateText("F3") };
            foreach (var f in _facilities)
                f.transform.SetParent(_townRoot.transform, false);

            _controller.BindReferences(
                _session, _worldClockPanel,
                _marker, _feedback, _curDate, _destName, _travel, _arrival,
                _confirm, _cancel, _return,
                _townRoot, _townTitle, _facilities);

            // 手动驱动 Start 逻辑：刷新初始状态。（监听由 Start/OnEnable 在真实运行时绑定；
            // 测试直接调用公开方法，故无需绑定 onClick。）
            _controller.RefreshAll();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
            Object.DestroyImmediate(_townRoot);
            Object.DestroyImmediate(_session.gameObject);
        }

        [Test]
        public void InitialState_ShowsCurrentLocationAndDate()
        {
            Assert.That(_controller.Session.CurrentLocation, Is.EqualTo(LocationId.Kiyosu));
            Assert.That(_marker.text, Does.Contain("清洲"));
            Assert.That(_curDate.text, Does.Contain("1560-04-01"), "M2 current-date display must reflect GameSession");
            Assert.That(_controller.InTownMenu, Is.False);
            Assert.That(_controller.CurrentPreview, Is.Null);
        }

        [Test]
        public void CurrentLocationButton_EntersStartingTownWithoutTravel()
        {
            Button kiyosu = BindTestLocationButtons()[0];
            GameDate startingDate = _session.WorldClock.CurrentDate;
            LocationId startingLocation = _session.CurrentLocation;

            Assert.That(startingLocation, Is.EqualTo(LocationId.Kiyosu));
            Assert.That(_controller.InTownMenu, Is.False);
            Assert.That(kiyosu.interactable, Is.True, "The starting town must be enterable from the map.");
            Assert.That(kiyosu.GetComponentInChildren<Text>().text, Does.Contain("进入"));
            kiyosu.onClick.Invoke();

            Assert.That(_controller.InTownMenu, Is.True);
            Assert.That(_townRoot.activeSelf, Is.True);
            Assert.That(_townTitle.text, Does.Contain("清洲"));
            Assert.That(_session.WorldClock.CurrentDate, Is.EqualTo(startingDate));
            Assert.That(_session.CurrentLocation, Is.EqualTo(startingLocation));
            Assert.That(_controller.CurrentPreview, Is.Null);
            Assert.That(_controller.SelectedDestination, Is.Null);
        }

        [TestCase(1)]
        [TestCase(2)]
        public void NoncurrentLocationButton_PreviewsWithoutEnteringTown(int buttonIndex)
        {
            Button[] buttons = BindTestLocationButtons();
            LocationId destination = buttonIndex == 1 ? LocationId.Atsuta : LocationId.Nagoya;
            GameDate startingDate = _session.WorldClock.CurrentDate;

            Assert.That(buttons[buttonIndex].interactable, Is.True);
            buttons[buttonIndex].onClick.Invoke();

            Assert.That(_controller.InTownMenu, Is.False);
            Assert.That(_townRoot.activeSelf, Is.False);
            Assert.That(_controller.SelectedDestination, Is.EqualTo(destination));
            Assert.That(_controller.CurrentPreview.HasValue, Is.True);
            Assert.That(_controller.CurrentPreview.Value.Destination, Is.EqualTo(destination));
            Assert.That(_confirm.interactable, Is.True);
            Assert.That(_session.WorldClock.CurrentDate, Is.EqualTo(startingDate));
            Assert.That(_session.CurrentLocation, Is.EqualTo(LocationId.Kiyosu));
        }

        [TestCase(1)]
        [TestCase(2)]
        public void CurrentLocationButton_AfterTravel_ClearsPreviewAndEntersTown(int buttonIndex)
        {
            Button[] buttons = BindTestLocationButtons();
            LocationId destination = buttonIndex == 1 ? LocationId.Atsuta : LocationId.Nagoya;
            buttons[buttonIndex].onClick.Invoke();
            _controller.ConfirmTravel();
            _controller.ReturnToMap();
            buttons[0].onClick.Invoke(); // Leave a pending return-travel preview.
            Assert.That(_controller.CurrentPreview.HasValue, Is.True);
            GameDate arrivalDate = _session.WorldClock.CurrentDate;

            Assert.That(buttons[buttonIndex].interactable, Is.True);
            Assert.That(buttons[buttonIndex].GetComponentInChildren<Text>().text, Does.Contain("进入"));
            Assert.That(buttons[0].GetComponentInChildren<Text>().text, Does.Not.Contain("进入"));
            buttons[buttonIndex].onClick.Invoke();

            Assert.That(_controller.InTownMenu, Is.True);
            Assert.That(_townRoot.activeSelf, Is.True);
            Assert.That(_townTitle.text, Does.Contain(LocationId.GetChineseName(destination)));
            Assert.That(_controller.CurrentPreview, Is.Null);
            Assert.That(_controller.SelectedDestination, Is.Null);
            Assert.That(_session.WorldClock.CurrentDate, Is.EqualTo(arrivalDate));
            Assert.That(_session.CurrentLocation, Is.EqualTo(destination));
            Assert.That(_return.interactable, Is.True);
        }

        [Test]
        public void SelectDestination_Valid_PreviewsWithoutMutatingDateOrLocation()
        {
            _controller.SelectDestination(LocationId.Atsuta);

            Assert.That(_controller.SelectedDestination, Is.EqualTo(LocationId.Atsuta));
            Assert.That(_controller.CurrentPreview.HasValue, Is.True);
            Assert.That(_controller.CurrentPreview.Value.TravelDays, Is.EqualTo(1));
            Assert.That(_controller.CurrentPreview.Value.EstimatedArrivalDate, Is.EqualTo(new GameDate(1560, 4, 2)));
            // 预览不得修改会话状态
            Assert.That(_session.CurrentLocation, Is.EqualTo(LocationId.Kiyosu));
            Assert.That(_session.WorldClock.CurrentDate, Is.EqualTo(new GameDate(1560, 4, 1)));
            // UI 反馈
            Assert.That(_controller.Feedback, Does.Contain("热田"));
            Assert.That(_confirm.interactable, Is.True);
            Assert.That(_destName.text, Is.EqualTo("热田"));
            Assert.That(_travel.text, Is.EqualTo("1 天"));
            Assert.That(_arrival.text, Is.EqualTo("1560-04-02"));
        }

        [Test]
        public void SelectDestination_CurrentLocation_FeedbackAndNoPreview()
        {
            _controller.SelectDestination(LocationId.Kiyosu); // 当前即清洲

            Assert.That(_controller.CurrentPreview, Is.Null);
            Assert.That(_controller.Feedback, Does.Contain("当前位置"));
            Assert.That(_confirm.interactable, Is.False);
        }

        [Test]
        public void SelectDestination_NoDirectRoute_GivesFeedbackAndNoPreview()
        {
            // 先前往热田
            _controller.SelectDestination(LocationId.Atsuta);
            _controller.ConfirmTravel();
            Assert.That(_session.CurrentLocation, Is.EqualTo(LocationId.Atsuta));
            _controller.ReturnToMap(); // 回到地图

            // 热田 -> 那古野 无直达路线
            _controller.SelectDestination(LocationId.Nagoya);

            Assert.That(_controller.CurrentPreview, Is.Null);
            Assert.That(_controller.Feedback, Does.Contain("无直达路线"));
            Assert.That(_confirm.interactable, Is.False);
        }

        [Test]
        public void CancelTravel_DoesNotMutateLocationOrDate()
        {
            _controller.SelectDestination(LocationId.Atsuta);
            _controller.CancelTravel();

            Assert.That(_controller.CurrentPreview, Is.Null);
            Assert.That(_controller.SelectedDestination, Is.Null);
            Assert.That(_session.CurrentLocation, Is.EqualTo(LocationId.Kiyosu));
            Assert.That(_session.WorldClock.CurrentDate, Is.EqualTo(new GameDate(1560, 4, 1)));
            Assert.That(_controller.Feedback, Does.Contain("已取消"));
            Assert.That(_confirm.interactable, Is.False);
        }

        [Test]
        public void ConfirmTravel_Valid_UpdatesLocationAndDateAndOpensTownMenu()
        {
            _controller.SelectDestination(LocationId.Atsuta);
            _controller.ConfirmTravel();

            Assert.That(_session.CurrentLocation, Is.EqualTo(LocationId.Atsuta));
            Assert.That(_session.WorldClock.CurrentDate, Is.EqualTo(new GameDate(1560, 4, 2)));
            Assert.That(_controller.InTownMenu, Is.True);
            Assert.That(_townRoot.activeSelf, Is.True);
            Assert.That(_townRoot.GetComponent<Image>().color.a, Is.EqualTo(1f));
            Assert.That(_marker.gameObject.activeSelf, Is.False);
            Assert.That(_feedback.gameObject.activeSelf, Is.False);
            Assert.That(_destName.gameObject.activeSelf, Is.False);
            Assert.That(_travel.gameObject.activeSelf, Is.False);
            Assert.That(_arrival.gameObject.activeSelf, Is.False);
            Assert.That(_confirm.gameObject.activeSelf, Is.False);
            Assert.That(_cancel.gameObject.activeSelf, Is.False);
            Assert.That(_controller.Feedback, Does.Contain("抵达"));
            Assert.That(_townTitle.text, Does.Contain("热田"));
        }

        [Test]
        public void ConfirmTravel_StalePreview_IsRejectedAtomically()
        {
            _controller.SelectDestination(LocationId.Atsuta);
            var stale = _controller.CurrentPreview;
            Assert.That(stale.HasValue, Is.True);
            // 模拟“过期”：直接推进时钟，使预览到达日期过时
            _session.AdvanceOneDay(); // -> 1560-04-02
            Assert.That(_session.ConfirmTravel(stale.Value), Is.False);
            Assert.That(_session.CurrentLocation, Is.EqualTo(LocationId.Kiyosu));
            Assert.That(_session.WorldClock.CurrentDate, Is.EqualTo(new GameDate(1560, 4, 2)));
        }

        [Test]
        public void ConfirmTravel_WithoutSelection_FeedbackAndNoMutation()
        {
            _controller.ConfirmTravel();

            Assert.That(_controller.Feedback, Does.Contain("请选择"));
            Assert.That(_session.CurrentLocation, Is.EqualTo(LocationId.Kiyosu));
            Assert.That(_session.WorldClock.CurrentDate, Is.EqualTo(new GameDate(1560, 4, 1)));
        }

        [Test]
        public void TownMenu_FacilitiesLabeledNotOpenAndDisabled()
        {
            _controller.SelectDestination(LocationId.Nagoya);
            _controller.ConfirmTravel();

            Assert.That(_controller.InTownMenu, Is.True);
            Assert.That(_controller.GetFacilityTexts().Length, Is.EqualTo(3));
            foreach (Text t in _controller.GetFacilityTexts())
            {
                Assert.That(t.text, Does.Contain("未开放"));
                Assert.That(t.enabled, Is.False, "facility entries must be disabled (not open)");
            }
        }

        [Test]
        public void ReturnToMap_ExitsTownMenuToMapView()
        {
            _controller.SelectDestination(LocationId.Atsuta);
            _controller.ConfirmTravel();
            Assert.That(_controller.InTownMenu, Is.True);

            _controller.ReturnToMap();

            Assert.That(_controller.InTownMenu, Is.False);
            Assert.That(_townRoot.activeSelf, Is.False);
            Assert.That(_marker.gameObject.activeSelf, Is.True);
            Assert.That(_feedback.gameObject.activeSelf, Is.True);
            Assert.That(_destName.gameObject.activeSelf, Is.True);
            Assert.That(_travel.gameObject.activeSelf, Is.True);
            Assert.That(_arrival.gameObject.activeSelf, Is.True);
            Assert.That(_confirm.gameObject.activeSelf, Is.True);
            Assert.That(_cancel.gameObject.activeSelf, Is.True);
            Assert.That(_controller.Feedback, Does.Contain("返回地图"));
        }

        [Test]
        public void ReturnToMapButton_InteractableOnlyInsideTownMenu()
        {
            Assert.That(_return.interactable, Is.False); // 地图视图
            _controller.SelectDestination(LocationId.Atsuta);
            _controller.ConfirmTravel();
            Assert.That(_return.interactable, Is.True); // 城镇视图
        }

        // ---- helpers ----

        private Button[] BindTestLocationButtons()
        {
            var buttons = new[] { CreateButton("Kiyosu"), CreateButton("Atsuta"), CreateButton("Nagoya") };
            var labels = new Text[buttons.Length];
            for (int i = 0; i < buttons.Length; i++)
            {
                buttons[i].transform.SetParent(_go.transform, false);
                labels[i] = CreateText("Label");
                labels[i].transform.SetParent(buttons[i].transform, false);
            }
            _controller.BindLocationButtons(buttons[0], labels[0], buttons[1], labels[1], buttons[2], labels[2]);
            return buttons;
        }

        private static Text CreateText(string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.localScale = Vector3.one;
            return go.AddComponent<Text>();
        }

        private static Button CreateButton(string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            go.AddComponent<Image>();
            return go.AddComponent<Button>();
        }
    }
}
