using System;
using NUnit.Framework;
using SengokuChronicle.Presentation;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SengokuChronicle.Tests.EditMode
{
    /// <summary>
    /// 日本全境地图视口的纯几何回归：绑定、全境复位、指针锚定缩放、
    /// 1x–6x 倍率限制、四边夹取、窄视口轴居中、非 1:1 画布换算与视口尺寸变更。
    /// 同时锁定绑定边界：地图内容必须是视口的直接子级且未旋转。
    /// 夹具只创建矩形与 Canvas，不加载底图、坐标数据、场景或预制体；
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
            _controller.PanBy(new Vector2(120f, -80f));
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

            _controller.PanBy(new Vector2(100000f, 100000f));
            Assert.That(ContentLeft, Is.EqualTo(_viewport.rect.xMin).Within(Tolerance),
                "向右拖到底时内容左边必须贴住视口左边，不能露空白。");
            Assert.That(ContentTop, Is.EqualTo(_viewport.rect.yMax).Within(Tolerance),
                "向上拖到底时内容上边必须贴住视口上边，不能露空白。");
            AssertCoversViewport();

            _controller.PanBy(new Vector2(-200000f, -200000f));
            Assert.That(ContentRight, Is.EqualTo(_viewport.rect.xMax).Within(Tolerance),
                "向左拖到底时内容右边必须贴住视口右边，不能露空白。");
            Assert.That(ContentBottom, Is.EqualTo(_viewport.rect.yMin).Within(Tolerance),
                "向下拖到底时内容下边必须贴住视口下边，不能露空白。");
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

            _controller.PanBy(new Vector2(5000f, 5000f));
            Assert.That(_content.localPosition.x, Is.EqualTo(0f).Within(Tolerance),
                "内容宽度等于视口宽度时，横向不允许拖动。");
            Assert.That(_content.localPosition.y, Is.EqualTo(0f).Within(Tolerance),
                "内容高度小于视口时，纵向必须保持居中。");

            _controller.ZoomAt(ScreenPointFor(Vector2.zero), 1f);
            _controller.PanBy(new Vector2(5000f, 5000f));

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
            _controller.PanBy(new Vector2(30f, -17.5f));
            Vector2 moved = (Vector2)_content.localPosition - positionBefore;
            Assert.That(moved.x, Is.EqualTo(20f).Within(0.05f),
                "画布横向缩放 1.5 时，30 屏幕像素等于 20 个视口单位。");
            Assert.That(moved.y, Is.EqualTo(-10f).Within(0.05f),
                "画布纵向缩放 1.75 时，-17.5 屏幕像素等于 -10 个视口单位。");
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
            return RectTransformUtility.WorldToScreenPoint(null, _viewport.TransformPoint(viewportLocal));
        }

        private Vector2 ContentPointAt(Vector2 screen)
        {
            bool mapped = RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _viewport, screen, null, out Vector2 viewportLocal);
            Assert.That(mapped, Is.True, "屏幕坐标必须能映射到视口局部坐标。");
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
}
