using System;
using NUnit.Framework;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;
using SengokuChronicle.Presentation;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SengokuChronicle.Tests.EditMode
{
    /// <summary>
    /// 日本全境地图视口的纯几何回归：绑定、全境复位、指针锚定缩放、
    /// 1x–6x 倍率限制、四边夹取、窄视口轴居中、非 1:1 画布换算、视口尺寸变更，
    /// 以及透视相机倾斜视口下按真实拖动起终点换算平移。
    /// 同时锁定绑定边界：地图内容必须是视口的直接子级且未旋转。
    /// 夹具只创建矩形、Canvas 与相机，不加载底图、坐标数据、场景或预制体；
    /// 不覆盖 uGUI 按下拖动阈值与节点点击防误选，那些留给屏幕集成测试。
    /// </summary>
    public sealed class JapanMapViewportTests
    {
        private const float ScreenWidth = 1920f;
        private const float ScreenHeight = 1080f;
        private const float Tolerance = 0.01f;

        private Scene _scene;
        private Scene _previousScene;
        private bool _previousDirty;
        private Canvas _canvas;
        private Camera _canvasCamera;
        private RectTransform _viewport;
        private RectTransform _content;
        private JapanMapViewport _controller;

        [SetUp]
        public void SetUp()
        {
            _previousScene = SceneManager.GetActiveScene();
            _previousDirty = _previousScene.isDirty;
            _scene = EditorSceneManager.NewPreviewScene();

            GameObject canvasObject = Create("Canvas", null);
            _canvas = canvasObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var canvasRect = (RectTransform)canvasObject.transform;
            canvasRect.pivot = new Vector2(0.5f, 0.5f);
            canvasRect.sizeDelta = new Vector2(1280f, 720f);
            canvasRect.localScale = Vector3.one;
            canvasRect.position = new Vector3(ScreenWidth * 0.5f, ScreenHeight * 0.5f, 0f);
        }

        [TearDown]
        public void TearDown()
        {
            if (_scene.IsValid())
                EditorSceneManager.ClosePreviewScene(_scene);
            Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(_previousScene));
            Assert.That(_previousScene.isDirty, Is.EqualTo(_previousDirty),
                "视口测试不得弄脏用户当前场景。");
        }

        [Test]
        public void BindFitsFullExtentAndCentersTheShorterAxis()
        {
            Build(new Vector2(800f, 600f), new Vector2(1600f, 800f));

            // 800x600 视口里的 2:1 内容：宽度贴合，上下留白由居中吸收。
            Assert.That(ContentWidth, Is.EqualTo(800f).Within(Tolerance));
            Assert.That(ContentHeight, Is.EqualTo(400f).Within(Tolerance));
            Assert.That(_content.localPosition.x, Is.EqualTo(0f).Within(Tolerance));
            Assert.That(_content.localPosition.y, Is.EqualTo(0f).Within(Tolerance));
            Assert.That(_controller.Zoom, Is.EqualTo(1f).Within(1e-5f));
        }

        [Test]
        public void BindRejectsMapContentThatIsNotADirectChild()
        {
            _viewport = CreateRect("Viewport", _canvas.transform, new Vector2(800f, 600f));
            RectTransform holder = CreateRect("MapContentHolder", _viewport, new Vector2(800f, 600f));
            _content = CreateRect("MapContent", holder, new Vector2(1600f, 1200f));
            var controller = new JapanMapViewport();

            ArgumentException error = Assert.Throws<ArgumentException>(
                () => controller.Bind(_viewport, _content));

            Assert.That(error.ParamName, Is.EqualTo("mapContent"));
            Assert.That(controller.IsBound, Is.False,
                "校验失败时不能留下半绑定状态。");
        }

        [Test]
        public void BindRejectsRotatedMapContent()
        {
            _viewport = CreateRect("Viewport", _canvas.transform, new Vector2(800f, 600f));
            _content = CreateRect("MapContent", _viewport, new Vector2(1600f, 1200f));
            _content.localRotation = Quaternion.Euler(0f, 0f, 90f);
            var controller = new JapanMapViewport();

            ArgumentException error = Assert.Throws<ArgumentException>(
                () => controller.Bind(_viewport, _content));

            Assert.That(error.ParamName, Is.EqualTo("mapContent"));
            Assert.That(controller.IsBound, Is.False,
                "旋转内容会让视口局部坐标与内容局部坐标不一致，必须拒绝绑定。");
        }

        [Test]
        public void ResetToFullExtentRestoresTheBindTransform()
        {
            Build(new Vector2(800f, 600f), new Vector2(1600f, 1200f));
            Vector2 initialPosition = _content.localPosition;
            float initialScale = _content.localScale.x;

            _controller.ZoomAt(ScreenPointFor(new Vector2(100f, 100f)), 1f);
            PanByScreenDelta(new Vector2(120f, -80f));
            Assert.That(_content.localScale.x, Is.GreaterThan(initialScale),
                "缩放后倍率应大于全境适配。");

            _controller.ResetToFullExtent();

            Assert.That(_controller.Zoom, Is.EqualTo(1f).Within(1e-5f));
            Assert.That(_content.localScale.x, Is.EqualTo(initialScale).Within(Tolerance));
            Assert.That(_content.localPosition.x, Is.EqualTo(initialPosition.x).Within(Tolerance));
            Assert.That(_content.localPosition.y, Is.EqualTo(initialPosition.y).Within(Tolerance));
            Assert.That(ContentWidth, Is.EqualTo(800f).Within(Tolerance));
            Assert.That(ContentHeight, Is.EqualTo(600f).Within(Tolerance));
        }

        [Test]
        public void ZoomAtPointerKeepsMapPoint()
        {
            Build(new Vector2(800f, 600f), new Vector2(1600f, 1200f));
            Vector2 screen = ScreenPointFor(new Vector2(100f, 100f));
            Vector2 before = ContentPointAt(screen);

            _controller.ZoomAt(screen, 1f);
            Vector2 afterFirstZoom = ContentPointAt(screen);

            Assert.That(_content.localScale.x, Is.GreaterThan(0.5f),
                "滚轮向上应放大内容。");
            Assert.That(afterFirstZoom.x, Is.EqualTo(before.x).Within(0.05f),
                "未触及边界时，指针下地图点的内容局部 X 不应移动。");
            Assert.That(afterFirstZoom.y, Is.EqualTo(before.y).Within(0.05f),
                "未触及边界时，指针下地图点的内容局部 Y 不应移动。");

            _controller.ZoomAt(screen, 1f);
            Vector2 afterSecondZoom = ContentPointAt(screen);
            Assert.That(afterSecondZoom.x, Is.EqualTo(before.x).Within(0.05f));
            Assert.That(afterSecondZoom.y, Is.EqualTo(before.y).Within(0.05f));
        }

        [Test]
        public void ZoomStopsAtOneAndSix()
        {
            Build(new Vector2(800f, 600f), new Vector2(1600f, 1200f));
            Vector2 center = ScreenPointFor(Vector2.zero);

            for (int i = 0; i < 20; i++)
                _controller.ZoomAt(center, 1f);

            Assert.That(_controller.Zoom, Is.EqualTo(6f).Within(1e-4f),
                "放大倍率上限必须是 6x。");
            Assert.That(ContentWidth, Is.EqualTo(4800f).Within(Tolerance));

            for (int i = 0; i < 40; i++)
                _controller.ZoomAt(center, -1f);

            Assert.That(_controller.Zoom, Is.EqualTo(1f).Within(1e-5f),
                "缩小倍率下限必须是 1x。");
            Assert.That(ContentWidth, Is.EqualTo(800f).Within(Tolerance));
            Assert.That(ContentHeight, Is.EqualTo(600f).Within(Tolerance));
            Assert.That(_content.localPosition.x, Is.EqualTo(0f).Within(Tolerance));
            Assert.That(_content.localPosition.y, Is.EqualTo(0f).Within(Tolerance));
        }

        [Test]
        public void PanAndZoomStayWithinBounds()
        {
            Build(new Vector2(800f, 600f), new Vector2(1600f, 1200f));
            Vector2 center = ScreenPointFor(Vector2.zero);

            _controller.ZoomAt(center, 1f);
            Assert.That(ContentWidth, Is.GreaterThan(800f));
            Assert.That(ContentHeight, Is.GreaterThan(600f));

            PanByScreenDelta(new Vector2(100000f, 100000f));
            Assert.That(ContentLeft, Is.EqualTo(_viewport.rect.xMin).Within(Tolerance),
                "向右拖到底时内容左边必须贴住视口左边，不能露空白。");
            Assert.That(ContentBottom, Is.EqualTo(_viewport.rect.yMin).Within(Tolerance),
                "向上拖到底时内容下边必须贴住视口下边，不能露空白。");
            AssertCoversViewport();

            PanByScreenDelta(new Vector2(-200000f, -200000f));
            Assert.That(ContentRight, Is.EqualTo(_viewport.rect.xMax).Within(Tolerance),
                "向左拖到底时内容右边必须贴住视口右边，不能露空白。");
            Assert.That(ContentTop, Is.EqualTo(_viewport.rect.yMax).Within(Tolerance),
                "向下拖到底时内容上边必须贴住视口上边，不能露空白。");
            AssertCoversViewport();

            // 在靠近角落的指针继续放大，锚定后的位置仍要被夹回边界内。
            _controller.ZoomAt(ScreenPointFor(new Vector2(-350f, -250f)), 1f);
            AssertCoversViewport();

            _controller.ResetToFullExtent();
            Assert.That(ContentWidth, Is.EqualTo(800f).Within(Tolerance));
            Assert.That(ContentHeight, Is.EqualTo(600f).Within(Tolerance));
        }

        [Test]
        public void NarrowViewportCentersTheAxisThatCannotFill()
        {
            Build(new Vector2(360f, 640f), new Vector2(1600f, 1200f));

            // 4:3 内容按宽度适配：360x270，纵向居中。
            Assert.That(ContentWidth, Is.EqualTo(360f).Within(Tolerance));
            Assert.That(ContentHeight, Is.EqualTo(270f).Within(Tolerance));
            Assert.That(_content.localPosition.y, Is.EqualTo(0f).Within(Tolerance));

            PanByScreenDelta(new Vector2(5000f, 5000f));
            Assert.That(_content.localPosition.x, Is.EqualTo(0f).Within(Tolerance),
                "内容宽度等于视口宽度时，横向不允许拖动。");
            Assert.That(_content.localPosition.y, Is.EqualTo(0f).Within(Tolerance),
                "内容高度小于视口时，纵向必须保持居中。");

            _controller.ZoomAt(ScreenPointFor(Vector2.zero), 1f);
            PanByScreenDelta(new Vector2(5000f, 5000f));

            // 放大后宽度大于视口、高度仍小于视口：横向夹到边缘，纵向继续居中。
            Assert.That(ContentWidth, Is.GreaterThan(360f));
            Assert.That(ContentHeight, Is.LessThan(640f));
            Assert.That(ContentLeft, Is.EqualTo(_viewport.rect.xMin).Within(Tolerance));
            Assert.That(_content.localPosition.y, Is.EqualTo(0f).Within(Tolerance));
        }

        [Test]
        public void NonUniformCanvasScaleMapsPointerAndPan()
        {
            _canvas.transform.localScale = new Vector3(1.5f, 1.75f, 1f);
            Build(new Vector2(800f, 600f), new Vector2(1600f, 1200f));

            Vector2 screen = ScreenPointFor(new Vector2(-120f, 90f));
            Vector2 before = ContentPointAt(screen);
            _controller.ZoomAt(screen, 1f);
            Vector2 after = ContentPointAt(screen);
            Assert.That(after.x, Is.EqualTo(before.x).Within(0.05f),
                "非 1:1 画布缩放时指针锚定仍应成立。");
            Assert.That(after.y, Is.EqualTo(before.y).Within(0.05f));

            Vector2 positionBefore = _content.localPosition;
            Vector2 dragStart = ScreenPointFor(new Vector2(-120f, 90f));
            _controller.PanBy(dragStart, dragStart + new Vector2(30f, -17.5f));
            Vector2 moved = (Vector2)_content.localPosition - positionBefore;
            Assert.That(moved.x, Is.EqualTo(20f).Within(0.05f),
                "画布横向缩放 1.5 时，30 屏幕像素等于 20 个视口单位。");
            Assert.That(moved.y, Is.EqualTo(-10f).Within(0.05f),
                "画布纵向缩放 1.75 时，-17.5 屏幕像素等于 -10 个视口单位。");
        }

        [Test]
        public void TiltedPerspectiveViewportMapsPanFromRealDragPoints()
        {
            SwitchToPerspectiveCanvas();
            Build(new Vector2(800f, 600f), new Vector2(1600f, 1200f));
            _viewport.localRotation = Quaternion.Euler(30f, 0f, 0f);
            _controller.Refresh();

            Vector2 center = ScreenPointFor(Vector2.zero);
            for (int i = 0; i < 10; i++)
                _controller.ZoomAt(center, 1f);
            Assert.That(ContentWidth, Is.GreaterThan(2000f),
                "必须放大到留出足够的夹取余量，才能观测未被夹取的拖位移。");

            Vector2 dragStart = new Vector2(-250f, 180f);
            Vector2 start = ScreenPointFor(dragStart);
            Vector2 end = ScreenPointFor(dragStart + new Vector2(120f, -90f));

            Vector2 positionBefore = _content.localPosition;
            _controller.PanBy(start, end);
            Vector2 moved = (Vector2)_content.localPosition - positionBefore;

            Vector2 expected = MapToViewportLocal(end) - MapToViewportLocal(start);
            Assert.That(expected.magnitude, Is.GreaterThan(1f),
                "回归夹具的拖动必须产生可观测位移。");
            Assert.That(expected.magnitude, Is.LessThan(400f),
                "回归夹具的拖动量必须留在夹取余量内，否则断言会测到边界而不是换算。");
            Assert.That(moved.x, Is.EqualTo(expected.x).Within(0.05f),
                "平移量必须等于起终点各自投影到视口平面后的差值。");
            Assert.That(moved.y, Is.EqualTo(expected.y).Within(0.05f),
                "平移量必须等于起终点各自投影到视口平面后的差值。");

            // 旧契约把屏幕原点当作拖动起点：同一段屏幕增量会算出不同的局部量。
            Vector2 fromScreenOrigin = MapToViewportLocal(end - start) - MapToViewportLocal(Vector2.zero);
            Assert.That((fromScreenOrigin - expected).magnitude,
                Is.GreaterThan(expected.magnitude * 0.1f),
                "透视倾斜视口下按屏幕原点推算的增量与真实起点不同，回归必须能分辨两者。");
        }

        [Test]
        public void ViewportResizeRecomputesFitDeterministically()
        {
            Build(new Vector2(800f, 600f), new Vector2(1600f, 1200f));
            Vector2 center = ScreenPointFor(Vector2.zero);
            for (int i = 0; i < 4; i++)
                _controller.ZoomAt(center, 1f);
            float zoom = _controller.Zoom;
            Assert.That(zoom, Is.GreaterThan(1f));

            _viewport.sizeDelta = new Vector2(400f, 600f);
            _controller.Refresh();

            float resizedFit = Mathf.Min(400f / 1600f, 600f / 1200f);
            Assert.That(_controller.Zoom, Is.EqualTo(zoom).Within(1e-5f),
                "视口尺寸变化不应擅自重置倍率。");
            Assert.That(_content.localScale.x, Is.EqualTo(resizedFit * zoom).Within(0.0005f));
            AssertCoversViewport();

            Vector2 positionAfterFirstRefresh = _content.localPosition;
            _controller.Refresh();
            Assert.That(_content.localPosition.x, Is.EqualTo(positionAfterFirstRefresh.x).Within(1e-5f));
            Assert.That(_content.localPosition.y, Is.EqualTo(positionAfterFirstRefresh.y).Within(1e-5f));

            _controller.ResetToFullExtent();
            Assert.That(_content.localScale.x, Is.EqualTo(resizedFit).Within(0.0005f));
            Assert.That(ContentWidth, Is.EqualTo(400f).Within(Tolerance));
            Assert.That(ContentHeight, Is.EqualTo(300f).Within(Tolerance));
            Assert.That(_content.localPosition.y, Is.EqualTo(0f).Within(Tolerance),
                "400x600 视口里的 400x300 内容必须纵向居中。");

            _viewport.sizeDelta = new Vector2(800f, 300f);
            _controller.Refresh();
            Assert.That(_content.localScale.x, Is.EqualTo(0.25f).Within(0.0005f));
            Assert.That(ContentWidth, Is.EqualTo(400f).Within(Tolerance));
            Assert.That(ContentHeight, Is.EqualTo(300f).Within(Tolerance));
            Assert.That(_content.localPosition.x, Is.EqualTo(0f).Within(Tolerance),
                "800x300 视口里的 400x300 内容必须横向居中。");
            Assert.That(_content.localPosition.y, Is.EqualTo(0f).Within(Tolerance));
        }

        private JapanMapViewport Build(Vector2 viewportSize, Vector2 contentSize)
        {
            _viewport = CreateRect("Viewport", _canvas.transform, viewportSize);
            _content = CreateRect("MapContent", _viewport, contentSize);

            _controller = new JapanMapViewport();
            _controller.Bind(_viewport, _content);
            return _controller;
        }

        private RectTransform CreateRect(string name, Transform parent, Vector2 size)
        {
            var rect = (RectTransform)Create(name, parent).transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = Vector2.zero;
            rect.localPosition = Vector3.zero;
            return rect;
        }

        private Vector2 ScreenPointFor(Vector2 viewportLocal)
        {
            return RectTransformUtility.WorldToScreenPoint(
                _canvasCamera, _viewport.TransformPoint(viewportLocal));
        }

        private Vector2 MapToViewportLocal(Vector2 screen)
        {
            bool mapped = RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _viewport, screen, _canvasCamera, out Vector2 viewportLocal);
            Assert.That(mapped, Is.True, "屏幕坐标必须能映射到视口局部坐标。");
            return viewportLocal;
        }

        private void PanByScreenDelta(Vector2 screenDelta)
        {
            Vector2 start = ScreenPointFor(Vector2.zero);
            _controller.PanBy(start, start + screenDelta);
        }

        private void SwitchToPerspectiveCanvas()
        {
            var cameraObject = Create("CanvasCamera", null);
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = false;
            camera.fieldOfView = 60f;
            camera.nearClipPlane = 0.5f;
            camera.farClipPlane = 5000f;
            cameraObject.transform.position = Vector3.zero;
            cameraObject.transform.rotation = Quaternion.identity;

            // 让画布平面距相机一个屏幕高对应的距离，视口局部单位与屏幕像素同量级。
            float planeDistance = ScreenHeight * 0.5f /
                Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad);

            _canvas.renderMode = RenderMode.ScreenSpaceCamera;
            _canvas.worldCamera = camera;
            _canvas.planeDistance = planeDistance;

            var canvasRect = (RectTransform)_canvas.transform;
            canvasRect.localScale = Vector3.one;
            canvasRect.rotation = Quaternion.identity;
            canvasRect.position = new Vector3(0f, 0f, planeDistance);

            _canvasCamera = camera;
        }

        private Vector2 ContentPointAt(Vector2 screen)
        {
            Vector2 viewportLocal = MapToViewportLocal(screen);
            Vector3 contentLocal = _content.InverseTransformPoint(_viewport.TransformPoint(viewportLocal));
            return new Vector2(contentLocal.x, contentLocal.y);
        }

        private float ContentWidth => _content.rect.width * _content.localScale.x;
        private float ContentHeight => _content.rect.height * _content.localScale.y;

        private float ContentLeft =>
            _content.localPosition.x - _content.pivot.x * ContentWidth;

        private float ContentRight =>
            _content.localPosition.x + (1f - _content.pivot.x) * ContentWidth;

        private float ContentBottom =>
            _content.localPosition.y - _content.pivot.y * ContentHeight;

        private float ContentTop =>
            _content.localPosition.y + (1f - _content.pivot.y) * ContentHeight;

        private void AssertCoversViewport()
        {
            Assert.That(ContentLeft, Is.LessThanOrEqualTo(_viewport.rect.xMin + Tolerance),
                "内容左边不得进入视口内部。");
            Assert.That(ContentRight, Is.GreaterThanOrEqualTo(_viewport.rect.xMax - Tolerance),
                "内容右边不得进入视口内部。");
            Assert.That(ContentBottom, Is.LessThanOrEqualTo(_viewport.rect.yMin + Tolerance),
                "内容下边不得进入视口内部。");
            Assert.That(ContentTop, Is.GreaterThanOrEqualTo(_viewport.rect.yMax - Tolerance),
                "内容上边不得进入视口内部。");
        }

        private GameObject Create(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            SceneManager.MoveGameObjectToScene(go, _scene);
            if (parent != null)
                go.transform.SetParent(parent, false);
            return go;
        }
    }

    /// <summary>
    /// t17: 全国地图屏幕的运行时结构与输入接线回归。
    /// 地图区域是唯一裁切视口，直接子级 mapContent 承载路线层与节点层，
    /// 两层共享同一变换；首次打开与尺寸变化按 1x 适配全境；滚轮按指针锚点
    /// 缩放到 6x；拖动走 PanBy(起终点)；短按节点沿用原选择/预览，超过 uGUI
    /// 拖动阈值后的点击不得误选节点或弹出行程；目录、筛选、详情与按钮不缩放。
    /// 夹具用真实 JapanMapScreen 构建运行时 UI，直接驱动指针处理函数，
    /// 不经过 EventSystem 的鼠标模拟。
    /// </summary>
    public sealed class JapanMapScreenViewportTests
    {
        private const string OkazakiHubId = "mikawa.okazaki";
        private const string KiyosuHubId = "owari.kiyosu";

        private GameObject _canvasObject;
        private GameObject _rootObject;
        private GameSession _session;
        private JapanMapScreen _screen;
        private RectTransform _rootRect;
        private RectTransform _mapArea;
        private RectTransform _mapContent;
        private RectTransform _lineLayer;
        private RectTransform _nodeLayer;
        private JapanMapViewportInput _input;

        [SetUp]
        public void SetUp()
        {
            _canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
            var canvas = _canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var canvasRect = (RectTransform)_canvasObject.transform;
            canvasRect.sizeDelta = new Vector2(1280f, 720f);
            canvasRect.position = new Vector3(960f, 540f, 0f);

            _session = GameSession.NewGame();
            _session.transform.SetParent(_canvasObject.transform, false);

            _rootObject = new GameObject("JapanMapRoot", typeof(RectTransform), typeof(Image));
            _rootRect = (RectTransform)_rootObject.transform;
            _rootRect.SetParent(_canvasObject.transform, false);
            _rootRect.anchorMin = Vector2.zero;
            _rootRect.anchorMax = Vector2.one;
            _rootRect.offsetMin = Vector2.zero;
            _rootRect.offsetMax = Vector2.zero;

            _screen = _rootObject.AddComponent<JapanMapScreen>();
            _screen.Initialize(_session, _rootRect);
            _screen.Open();
        }

        [TearDown]
        public void TearDown()
        {
            if (_canvasObject != null)
                UnityEngine.Object.DestroyImmediate(_canvasObject);
            if (_session != null)
                UnityEngine.Object.DestroyImmediate(_session.gameObject);
        }

        [Test]
        public void MapAreaClipsOneContentSharedByRoutesAndNodes()
        {
            RequireViewportStructure();

            Assert.That(_nodeLayer.childCount, Is.EqualTo(JapanTravelGraph.Hubs.Count));
            Assert.That(_lineLayer.childCount, Is.EqualTo(JapanTravelGraph.Routes.Count));
            Assert.That(_lineLayer.GetSiblingIndex(), Is.LessThan(_nodeLayer.GetSiblingIndex()),
                "路线必须垫在节点下面，节点点击优先");

            string[] outside =
            {
                "CatalogPanel", "DetailPanel", "Header", "AllRoutesButton",
                "LandRoutesButton", "SeaRoutesButton", "CurrentButton", "CloseButton",
            };
            foreach (string name in outside)
            {
                var rect = (RectTransform)_rootRect.Find(name);
                Assert.That(rect, Is.Not.Null, "缺少界面区域 " + name);
                Assert.That(rect.IsChildOf(_mapContent), Is.False,
                    name + " 不得放进地图缩放内容里");
            }
        }

        [Test]
        public void FirstOpenFitsTheWholeMapAtOneX()
        {
            RequireViewportStructure();

            Assert.That(_mapContent.rect.width, Is.EqualTo(_mapArea.rect.width).Within(0.5f));
            Assert.That(_mapContent.rect.height, Is.EqualTo(_mapArea.rect.height).Within(0.5f));
            Assert.That(_mapContent.localScale.x, Is.EqualTo(1f).Within(1e-4f),
                "首次打开必须按 1x 全境适配");
            Assert.That(_mapContent.localPosition.x, Is.EqualTo(0f).Within(0.01f));
            Assert.That(_mapContent.localPosition.y, Is.EqualTo(0f).Within(0.01f));
            AssertEveryNodeInsideTheMapArea();
        }

        [Test]
        public void ScrollZoomsAtThePointerAndStopsAtSixAndOne()
        {
            RequireViewportStructure();
            var catalog = (RectTransform)_rootRect.Find("CatalogPanel");
            var detail = (RectTransform)_rootRect.Find("DetailPanel");
            Vector3 catalogScale = catalog.localScale;
            Vector3 catalogPosition = catalog.localPosition;
            Vector3 detailPosition = detail.localPosition;

            Vector2 screen = ScreenPointForViewportLocal(new Vector2(80f, -40f));
            Vector2 mapPointBefore = ContentPointAt(screen);

            _input.OnScroll(ScrollEvent(screen, 1f));

            Assert.That(_mapContent.localScale.x, Is.GreaterThan(1f),
                "滚轮向上必须放大地图");
            Vector2 mapPointAfter = ContentPointAt(screen);
            Assert.That(mapPointAfter.x, Is.EqualTo(mapPointBefore.x).Within(0.5f),
                "放大时指针下的地图点不得移动");
            Assert.That(mapPointAfter.y, Is.EqualTo(mapPointBefore.y).Within(0.5f),
                "放大时指针下的地图点不得移动");

            Assert.That(catalog.localScale, Is.EqualTo(catalogScale),
                "目录不得随地图缩放");
            Assert.That(catalog.localPosition, Is.EqualTo(catalogPosition),
                "目录不得随地图平移或缩放");
            Assert.That(detail.localPosition, Is.EqualTo(detailPosition),
                "详情不得随地图平移或缩放");

            Vector2 center = ScreenPointForViewportLocal(Vector2.zero);
            for (int i = 0; i < 20; i++)
                _input.OnScroll(ScrollEvent(center, 1f));
            Assert.That(_mapContent.rect.width * _mapContent.localScale.x,
                Is.EqualTo(_mapArea.rect.width * 6f).Within(0.5f),
                "放大上限必须是 6x");

            for (int i = 0; i < 40; i++)
                _input.OnScroll(ScrollEvent(center, -1f));
            Assert.That(_mapContent.localScale.x, Is.EqualTo(1f).Within(1e-4f),
                "缩小下限必须回到 1x 全境");
            Assert.That(_mapContent.localPosition.x, Is.EqualTo(0f).Within(0.01f));
            Assert.That(_mapContent.localPosition.y, Is.EqualTo(0f).Within(0.01f));
        }

        [Test]
        public void DragPansTheSharedContentWithoutZoomingTheLayers()
        {
            RequireViewportStructure();
            Vector2 center = ScreenPointForViewportLocal(Vector2.zero);
            for (int i = 0; i < 3; i++)
                _input.OnScroll(ScrollEvent(center, 1f));
            float zoomScale = _mapContent.localScale.x;
            Assert.That(zoomScale, Is.GreaterThan(1.5f),
                "拖动回归必须先放大，留出可观测的平移余量");

            Vector2 start = ScreenPointForViewportLocal(new Vector2(-60f, -30f));
            Vector2 end = ScreenPointForViewportLocal(new Vector2(60f, 30f));
            Vector2 expected = MapToViewportLocal(end) - MapToViewportLocal(start);
            Assert.That(expected.magnitude, Is.GreaterThan(10f),
                "夹具的拖动必须产生可观测位移");
            float marginX = (_mapContent.rect.width * zoomScale - _mapArea.rect.width) * 0.5f;
            float marginY = (_mapContent.rect.height * zoomScale - _mapArea.rect.height) * 0.5f;
            Assert.That(Mathf.Abs(expected.x), Is.LessThan(marginX - 1f),
                "夹具的横向拖动量必须留在夹取余量内，否则断言会测到边界");
            Assert.That(Mathf.Abs(expected.y), Is.LessThan(marginY - 1f),
                "夹具的纵向拖动量必须留在夹取余量内，否则断言会测到边界");

            Vector2 positionBefore = _mapContent.localPosition;
            Vector3 lineScale = _lineLayer.localScale;
            Vector3 nodeScale = _nodeLayer.localScale;
            Vector2 linePosition = _lineLayer.localPosition;
            Vector2 nodePosition = _nodeLayer.localPosition;

            SimulateDrag(start, end);
            Assert.That(_input.IsDragging, Is.True,
                "超过阈值的拖动必须进入拖动状态");

            Vector2 moved = (Vector2)_mapContent.localPosition - positionBefore;
            Assert.That(moved.x, Is.EqualTo(expected.x).Within(0.5f),
                "拖动必须按起终点各自投影到视口平面后的差值平移");
            Assert.That(moved.y, Is.EqualTo(expected.y).Within(0.5f),
                "拖动必须按起终点各自投影到视口平面后的差值平移");
            Assert.That(_mapContent.localScale.x, Is.EqualTo(zoomScale).Within(1e-4f),
                "平移不得改变倍率");
            Assert.That(_lineLayer.localScale, Is.EqualTo(lineScale),
                "路线层不得单独缩放");
            Assert.That(_nodeLayer.localScale, Is.EqualTo(nodeScale),
                "节点层不得单独缩放");
            Assert.That((Vector2)_lineLayer.localPosition, Is.EqualTo(linePosition),
                "路线层只跟随 mapContent，不应被单独平移");
            Assert.That((Vector2)_nodeLayer.localPosition, Is.EqualTo(nodePosition),
                "节点层只跟随 mapContent，不应被单独平移");

            _input.OnEndDrag(EventAt(end));
            Assert.That(_input.IsDragging, Is.False, "松手后拖动状态必须清除");
        }

        [Test]
        public void DragBeyondThresholdSuppressesNodeSelectionAndPreview()
        {
            RequireViewportStructure();
            Assert.That(ConfirmTravelButton().interactable, Is.False,
                "前置条件：打开地图时没有待确认行程");

            // 短按（未超过拖动阈值）继续走原来的选择与旅行预览路径。
            FindHubButton(OkazakiHubId).onClick.Invoke();
            Assert.That(_screen.SelectedHubId, Is.EqualTo(OkazakiHubId),
                "短按节点必须沿用原来的选择路径");
            Assert.That(ConfirmTravelButton().interactable, Is.True,
                "短按可达枢纽必须产生旅行预览");

            // 回到当前所在地，清掉预览，作为拖动误选的基线。
            FindHubButton(KiyosuHubId).onClick.Invoke();
            Assert.That(_screen.SelectedHubId, Is.EqualTo(KiyosuHubId));
            Assert.That(ConfirmTravelButton().interactable, Is.False);

            RectTransform okazakiNode = (RectTransform)_nodeLayer.Find("Hub_" + OkazakiHubId);
            Assert.That(okazakiNode, Is.Not.Null);
            Vector2 start = NodeCenterScreen(okazakiNode);
            Vector2 end = start + new Vector2(120f, -70f);
            SimulateDrag(start, end);
            Assert.That(_input.IsDragging, Is.True);

            // uGUI 在超过阈值后不会再给节点发 click；这里直接调用 onClick，
            // 证明屏幕自己的防线也不会误选或弹出行程预览。
            FindHubButton(OkazakiHubId).onClick.Invoke();
            Assert.That(_screen.SelectedHubId, Is.EqualTo(KiyosuHubId),
                "拖动后的点击不得误选节点");
            Assert.That(ConfirmTravelButton().interactable, Is.False,
                "拖动不得触发旅行预览");
            Assert.That(_session.CurrentLocation.Value, Is.EqualTo(KiyosuHubId),
                "拖动不得改变当前位置");

            _input.OnEndDrag(EventAt(end));
            FindHubButton(OkazakiHubId).onClick.Invoke();
            Assert.That(_screen.SelectedHubId, Is.EqualTo(OkazakiHubId),
                "拖动结束后新的短按必须恢复选择");
        }

        [Test]
        public void ViewportResizeRefitsTheWholeMapAtOneX()
        {
            RequireViewportStructure();
            Vector2 center = ScreenPointForViewportLocal(Vector2.zero);
            for (int i = 0; i < 4; i++)
                _input.OnScroll(ScrollEvent(center, 1f));
            Assert.That(_mapContent.localScale.x, Is.GreaterThan(1.5f));
            Vector2 sizeBefore = _mapArea.rect.size;

            _rootRect.offsetMax = new Vector2(-320f, -180f);
            Canvas.ForceUpdateCanvases();
            _screen.RefreshMapViewport();

            Vector2 sizeAfter = _mapArea.rect.size;
            Assert.That(sizeAfter.x, Is.LessThan(sizeBefore.x - 10f),
                "夹具必须真的缩小地图区域");
            Assert.That(_mapContent.rect.width, Is.EqualTo(sizeAfter.x).Within(0.5f));
            Assert.That(_mapContent.rect.height, Is.EqualTo(sizeAfter.y).Within(0.5f));
            Assert.That(_mapContent.localScale.x, Is.EqualTo(1f).Within(1e-4f),
                "尺寸变化后必须重新适配全境");
            Assert.That(_mapContent.localPosition.x, Is.EqualTo(0f).Within(0.01f));
            Assert.That(_mapContent.localPosition.y, Is.EqualTo(0f).Within(0.01f));
            AssertEveryNodeInsideTheMapArea();
        }

        [Test]
        public void SelectingAHubKeepsTheCurrentZoomAndPan()
        {
            RequireViewportStructure();
            Vector2 center = ScreenPointForViewportLocal(Vector2.zero);
            for (int i = 0; i < 3; i++)
                _input.OnScroll(ScrollEvent(center, 1f));
            Vector2 start = ScreenPointForViewportLocal(new Vector2(-40f, -20f));
            Vector2 end = ScreenPointForViewportLocal(new Vector2(0f, 0f));
            SimulateDrag(start, end);
            _input.OnEndDrag(EventAt(end));

            float scale = _mapContent.localScale.x;
            Vector2 position = _mapContent.localPosition;

            FindHubButton(OkazakiHubId).onClick.Invoke();

            Assert.That(_mapContent.localScale.x, Is.EqualTo(scale).Within(1e-4f),
                "选择枢纽重建节点时不得把地图重置回 1x");
            Assert.That((Vector2)_mapContent.localPosition, Is.EqualTo(position),
                "选择枢纽不得重置已拖动的平移");
            Assert.That(_nodeLayer.childCount, Is.EqualTo(JapanTravelGraph.Hubs.Count));
        }

        private void RequireViewportStructure()
        {
            _mapArea = (RectTransform)_rootRect.Find("JapanMapArea");
            Assert.That(_mapArea, Is.Not.Null, "地图区域必须存在");
            Assert.That(_mapArea.GetComponent<RectMask2D>(), Is.Not.Null,
                "地图区域必须是裁切视口，不能让缩放后的内容露出边界");

            _mapContent = (RectTransform)_mapArea.Find("MapContent");
            Assert.That(_mapContent, Is.Not.Null, "地图区域必须有直接子级 MapContent");
            Assert.That(_mapContent.parent, Is.SameAs(_mapArea),
                "MapContent 必须是裁切视口的直接子级");

            _lineLayer = (RectTransform)_mapContent.Find("RouteLines");
            _nodeLayer = (RectTransform)_mapContent.Find("HubNodes");
            Assert.That(_lineLayer, Is.Not.Null, "路线层必须存在");
            Assert.That(_nodeLayer, Is.Not.Null, "节点层必须存在");
            Assert.That(_lineLayer.parent, Is.SameAs(_mapContent),
                "路线层必须和节点层共享同一个 mapContent 变换");
            Assert.That(_nodeLayer.parent, Is.SameAs(_mapContent),
                "节点层必须和路线层共享同一个 mapContent 变换");

            _input = _mapArea.GetComponent<JapanMapViewportInput>();
            Assert.That(_input, Is.Not.Null, "地图区域必须接收缩放和平移输入");
        }

        private Vector2 ScreenPointForViewportLocal(Vector2 viewportLocal)
        {
            return RectTransformUtility.WorldToScreenPoint(
                null, _mapArea.TransformPoint(viewportLocal));
        }

        private Vector2 MapToViewportLocal(Vector2 screen)
        {
            bool mapped = RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _mapArea, screen, null, out Vector2 viewportLocal);
            Assert.That(mapped, Is.True, "屏幕坐标必须能映射到地图视口");
            return viewportLocal;
        }

        private Vector2 ContentPointAt(Vector2 screen)
        {
            Vector2 viewportLocal = MapToViewportLocal(screen);
            Vector3 contentLocal = _mapContent.InverseTransformPoint(
                _mapArea.TransformPoint(viewportLocal));
            return new Vector2(contentLocal.x, contentLocal.y);
        }

        private static PointerEventData EventAt(Vector2 position)
        {
            return new PointerEventData(EventSystem.current) { position = position };
        }

        private static PointerEventData ScrollEvent(Vector2 position, float scroll)
        {
            return new PointerEventData(EventSystem.current)
            {
                position = position,
                scrollDelta = new Vector2(0f, scroll),
            };
        }

        private void SimulateDrag(Vector2 start, Vector2 end)
        {
            _input.OnBeginDrag(EventAt(start));
            _input.OnDrag(EventAt(end));
        }

        private Button FindHubButton(string hubId)
        {
            Transform node = _nodeLayer.Find("Hub_" + hubId);
            Assert.That(node, Is.Not.Null, "缺少节点 Hub_" + hubId);
            return node.GetComponent<Button>();
        }

        private Button ConfirmTravelButton()
        {
            Transform button = _rootRect.Find("DetailPanel/ConfirmTravelButton");
            Assert.That(button, Is.Not.Null, "缺少确认出发按钮");
            return button.GetComponent<Button>();
        }

        private static Vector2 NodeCenterScreen(Transform node)
        {
            var rect = (RectTransform)node;
            return RectTransformUtility.WorldToScreenPoint(
                null, rect.TransformPoint(rect.rect.center));
        }

        private void AssertEveryNodeInsideTheMapArea()
        {
            Assert.That(_nodeLayer.childCount, Is.EqualTo(JapanTravelGraph.Hubs.Count),
                "全境适配时所有枢纽节点都必须建出来");
            Rect area = _mapArea.rect;
            foreach (Transform child in _nodeLayer)
            {
                var node = (RectTransform)child;
                Vector3 local = _mapArea.InverseTransformPoint(
                    node.TransformPoint(node.rect.center));
                Assert.That(local.x, Is.InRange(area.xMin - 0.5f, area.xMax + 0.5f),
                    child.name + " 必须落在全境视口内");
                Assert.That(local.y, Is.InRange(area.yMin - 0.5f, area.yMax + 0.5f),
                    child.name + " 必须落在全境视口内");
            }
        }
    }
}
