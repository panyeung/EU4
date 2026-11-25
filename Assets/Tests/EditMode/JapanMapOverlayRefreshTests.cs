using NUnit.Framework;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;
using SengokuChronicle.Presentation;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SengokuChronicle.Tests.EditMode
{
    /// <summary>
    /// 回归：在全国地图覆盖层确认旅行后，底下的旧版地图页必须立即按
    /// GameSession 的当前位置与日期刷新，不需要切换标签页。
    /// 复现路径：新局清洲 → 打开全国地图 → 选冈崎 → 确认出发 → 关闭覆盖层。
    /// 测试只驱动真实按钮处理函数与状态事件，不手动调用 RefreshAll。
    /// </summary>
    public sealed class JapanMapOverlayRefreshTests
    {
        private const string OkazakiHubId = "mikawa.okazaki";

        private GameObject _canvasObject;
        private GameObject _controllerObject;
        private GameSession _session;
        private WorldMapController _controller;
        private JapanMapScreen _screen;
        private Text _marker;
        private Text _feedback;
        private Text _currentDate;
        private Text _destination;

        [SetUp]
        public void SetUp()
        {
            _canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
            var canvas = _canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            _session = GameSession.NewGame();
            _session.transform.SetParent(_canvasObject.transform, false);

            _controllerObject = new GameObject("M2WorldMapRoot", typeof(RectTransform));
            _controllerObject.transform.SetParent(_canvasObject.transform, false);
            _controllerObject.SetActive(false);
            _controller = _controllerObject.AddComponent<WorldMapController>();
            _marker = CreateText("LocationMarkerText");
            _feedback = CreateText("FeedbackText");
            _currentDate = CreateText("CurrentDateText");
            _destination = CreateText("DestinationNameText");
            _controller.BindReferences(
                _session, null,
                _marker, _feedback, _currentDate,
                _destination, CreateText("TravelTimeText"), CreateText("ArrivalDateText"),
                CreateButton("ConfirmButton"), CreateButton("CancelButton"),
                CreateButton("ReturnToMapButton"), null, null, null);
            _controllerObject.SetActive(true);
            // 真实运行时的 Start 会先画一次初始状态；这里显式补同样的起点。
            _controller.RefreshAll();
            // 玩家从标签栏进入“地图与出行”页时走的就是 ReturnToMap。
            _controller.ReturnToMap();

            _screen = JapanMapSceneBootstrap.EnsureFor(_session, canvas);
            Assert.That(_screen, Is.Not.Null, "全国地图界面必须能创建");
        }

        [TearDown]
        public void TearDown()
        {
            if (_canvasObject != null)
                Object.DestroyImmediate(_canvasObject);
            if (_session != null)
                Object.DestroyImmediate(_session.gameObject);
        }

        [Test]
        public void ConfirmTravelInNationalMap_PublishesOneSessionStateChange()
        {
            int notifications = 0;
            void OnChanged() => notifications++;
            _session.SessionStateChanged += OnChanged;
            try
            {
                Assert.That(TravelToOkazakiThroughTheOverlay(), Is.True,
                    _session.LastTravelBlockReason);
            }
            finally
            {
                _session.SessionStateChanged -= OnChanged;
            }

            Assert.That(notifications, Is.EqualTo(1),
                "一次成功的旅程是原子命令，必须恰好发布一次状态变化，让其它页面刷新");
        }

        [Test]
        public void StandardDemoConfirmTravel_PublishesOneSessionStateChange()
        {
            // SC-010 标准样例没有 SC-013 的 NPC 名册，旅行时不会顺带产生需求成长。
            // 这种会话里位置和日期一样会变，页面刷新仍必须由旅行本身驱动。
            GameSession session = GameSession.NewGame();
            try
            {
                session.InitStandardDemo();
                int notifications = 0;
                void OnChanged() => notifications++;
                session.SessionStateChanged += OnChanged;
                try
                {
                    TravelPreview? preview = session.PreviewTravel(LocationId.Atsuta);
                    Assert.That(preview.HasValue, Is.True, session.LastTravelBlockReason);
                    Assert.That(session.ConfirmTravel(preview.Value), Is.True,
                        session.LastTravelBlockReason);
                }
                finally
                {
                    session.SessionStateChanged -= OnChanged;
                }

                Assert.That(notifications, Is.EqualTo(1),
                    "位置与日期都变了，旅行必须发布一次状态变化，否则页面保持旧地点");
            }
            finally
            {
                Object.DestroyImmediate(session.gameObject);
            }
        }

        [Test]
        public void ConfirmTravelInNationalMap_RefreshesTheMapPageLocationMarker()
        {
            Assert.That(TravelToOkazakiThroughTheOverlay(), Is.True,
                _session.LastTravelBlockReason);

            Assert.That(_marker.text, Is.EqualTo("当前位置：冈崎"),
                "全国地图里确认旅行后，旧地图页的位置标记必须立即更新，不能等切换标签页");
            Assert.That(_currentDate.text, Does.Contain("1560-04-03"),
                "旧地图页的日期必须与 GameSession 一致");
        }

        [Test]
        public void ConfirmTravelInNationalMap_RefreshesTheMapHintFromTheNewLocation()
        {
            Assert.That(_feedback.text, Does.Contain("清洲"),
                "前置条件：进入地图页时的提示行点名旧位置");

            Assert.That(TravelToOkazakiThroughTheOverlay(), Is.True,
                _session.LastTravelBlockReason);

            Assert.That(_feedback.text, Does.Contain("冈崎"),
                "提示行必须按当前位置重新生成");
            Assert.That(_feedback.text, Does.Not.Contain("清洲"),
                "提示行不得保留旧位置的文字");
        }

        [Test]
        public void ClosingTheNationalMap_KeepsTheRefreshedMapPageState()
        {
            Assert.That(TravelToOkazakiThroughTheOverlay(), Is.True,
                _session.LastTravelBlockReason);

            _screen.Close();

            Assert.That(_screen.IsOpen, Is.False);
            Assert.That(_session.CurrentLocation.Value, Is.EqualTo(OkazakiHubId));
            Assert.That(_marker.text, Is.EqualTo("当前位置：冈崎"),
                "关闭覆盖层后，露出的旧地图页必须显示当前地点");
            Assert.That(_feedback.text, Does.Not.Contain("清洲"),
                "关闭覆盖层后，底部提示不得再点名旧地点");
        }

        private bool TravelToOkazakiThroughTheOverlay()
        {
            _screen.Open();
            if (!_screen.SelectHub(OkazakiHubId))
                return false;
            _screen.ConfirmSelectedTravel();
            return _session.CurrentLocation.Value == OkazakiHubId;
        }

        private Text CreateText(string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(_controllerObject.transform, false);
            return go.GetComponent<Text>();
        }

        private Button CreateButton(string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Button));
            go.transform.SetParent(_controllerObject.transform, false);
            return go.GetComponent<Button>();
        }
    }
}
