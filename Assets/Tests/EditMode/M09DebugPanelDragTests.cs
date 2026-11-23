using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SengokuChronicle.Application;
using SengokuChronicle.Editor;
using SengokuChronicle.Presentation;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SengokuChronicle.Tests.EditMode
{
    /// <summary>
    /// M09 标题栏拖动的回归测试。拖动不是直接调用 <c>OnBeginDrag</c>/<c>OnDrag</c>，而是走
    /// EventSystem 的派发链路：解析按下与最上层图形 →
    /// <c>ExecuteEvents.GetEventHandler</c> 解析 pointerDrag → BeginDrag/Drag 派发。
    /// 断言的量是屏幕像素：面板在屏幕上必须跟随指针走同样的距离，被抓取的那个标题栏点必须始终
    /// 停在指针下方，并且标题栏在整个绘制顺序里是最上层的指针目标。
    /// <para>
    /// 屏幕按 1920x1080 建模。Canvas 使用 Screen Space - Overlay、CanvasScaler 参考分辨率
    /// 1280x720、match 0.5，因此缩放因子为 <c>sqrt(1.5 * 1.5) = 1.5</c>：画布矩形为 1280x720，
    /// 画布局部缩放为 1.5。这两点正是 Unity 在 Overlay 下把画布铺满屏幕的方式，EditMode 里没有
    /// 真实的 Game 视图来驱动 CanvasScaler，所以测试按同一结果显式铺设画布矩形。
    /// </para>
    /// </summary>
    public sealed class M09DebugPanelDragTests
    {
        private const float ScreenWidth = 1920f;
        private const float ScreenHeight = 1080f;
        private const float ExpectedCanvasScale = 1.5f;

        private Scene _scene;
        private Scene _previousScene;
        private bool _previousDirty;
        private Canvas _canvas;
        private EventSystem _eventSystem;
        private bool _createdEventSystem;
        private DebugModePanel _panel;
        private RectTransform _header;

        [SetUp]
        public void SetUp()
        {
            _previousScene = SceneManager.GetActiveScene();
            _previousDirty = _previousScene.isDirty;
            _scene = EditorSceneManager.NewPreviewScene();

            GameObject canvasObject = Create("Canvas", null);
            _canvas = canvasObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            // 预览场景不跑渲染与 willRenderCanvases，CanvasScaler 不会自己把画布铺好；
            // 下面按它在 1920x1080 下的结果显式铺设，并由测试断言该结果。
            scaler.enabled = false;

            var canvasRect = (RectTransform)canvasObject.transform;
            canvasRect.anchorMin = canvasRect.anchorMax = Vector2.zero;
            canvasRect.pivot = new Vector2(0.5f, 0.5f);
            canvasRect.localScale = new Vector3(ExpectedCanvasScale, ExpectedCanvasScale, 1f);
            canvasRect.sizeDelta = new Vector2(ScreenWidth, ScreenHeight) / ExpectedCanvasScale;
            canvasRect.position = new Vector3(ScreenWidth * 0.5f, ScreenHeight * 0.5f, 0f);

            // M6 标题页更新器要求既有的地图根与城镇根，以及一个 GameSession。
            GameObject map = Create("M2WorldMapRoot", Single("Canvas"), typeof(WorldMapController));
            ((RectTransform)map.transform).sizeDelta = new Vector2(1216f, 268f);
            GameObject town = Create("TownMenuRoot", map.transform);
            ((RectTransform)town.transform).sizeDelta = new Vector2(1216f, 268f);
            town.SetActive(false);
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)Single("Canvas"));

            GameSession session = Create("GameSession", null).AddComponent<GameSession>();
            session.InitNewGame();
            Assert.That(AddM6TitleScreen.Update(_scene),
                Is.EqualTo(AddM6TitleScreen.UpdateResult.Success),
                "fixture must start from a valid M6 title screen.");
            Assert.That(AddM09DebugMode.Update(_scene),
                Is.EqualTo(AddM09DebugMode.UpdateResult.Success));
            Create("EventChoiceRoot", Single("Canvas"));
            Create("M6TacticalMissionOverlay", Single("Canvas"));

            // 编辑器里可能已经有一个事件系统（例如打开的场景自带），复用它即可：
            // 这里只需要一个 PointerEventData 的宿主，派发本身是手工完成的。
            _eventSystem = EventSystem.current;
            if (_eventSystem == null)
            {
                _eventSystem = Create("EventSystem", null).AddComponent<EventSystem>();
                typeof(EventSystem).GetMethod("OnEnable",
                        BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(_eventSystem, null);
                _createdEventSystem = true;
            }

            // 打开窗口走生产入口：标题页控制器启动调试会话，窗口在运行期构建。
            TitleScreenController controller = _scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<TitleScreenController>(true))
                .Single();
            controller.ShowTitle();
            Assert.That(controller.StartDebugSession(), Is.True);

            _panel = Single(DebugModePanel.RootName).GetComponent<DebugModePanel>();
            Assert.That(_panel, Is.Not.Null);
            Assert.That(_panel.IsOpen, Is.True);
            _header = (RectTransform)Single(DebugModePanel.RootName).Find("DebugHeader");
            Assert.That(_header, Is.Not.Null);
            Canvas.ForceUpdateCanvases();

            // 画布映射前提：1280x720 的参考分辨率铺满 1920x1080 后缩放为 1.5，
            // 于是画布 1 个世界单位仍然等于 1 个屏幕像素。
            Assert.That(_canvas.transform.localScale.x, Is.EqualTo(ExpectedCanvasScale).Within(1e-4f));
            Assert.That(((RectTransform)_canvas.transform).rect.width,
                Is.EqualTo(1280f).Within(0.01f));
            Assert.That(((RectTransform)_canvas.transform).rect.height,
                Is.EqualTo(720f).Within(0.01f));
        }

        [TearDown]
        public void TearDown()
        {
            if (_createdEventSystem && EventSystem.current == _eventSystem)
                typeof(EventSystem).GetMethod("OnDisable",
                        BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(_eventSystem, null);
            if (_scene.IsValid())
                EditorSceneManager.ClosePreviewScene(_scene);
            Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(_previousScene));
            Assert.That(_previousScene.isDirty, Is.EqualTo(_previousDirty));
        }

        [Test]
        public void Header_IsTheTopmostPointerTargetAcrossTheTitleBar()
        {
            // 标题栏局部坐标：矩形从左上角展开，-600..600 为横向，-66..0 为纵向。
            // 三个采样点分别是水印正下方、标题栏中部、折叠与关闭按钮左侧的空白区。
            Vector2[] samples = { new Vector2(-300f, -33f), new Vector2(0f, -33f),
                new Vector2(350f, -33f) };
            foreach (Vector2 local in samples)
            {
                Vector2 screen = ScreenPointFor(local);
                Graphic top = TopmostGraphicAt(screen);
                Assert.That(top, Is.Not.Null, "指针位置 " + screen + " 上没有可接收指针的 UI。");
                Assert.That(top.gameObject, Is.SameAs(_header.gameObject),
                    "指针位置 " + local + " 的最上层 UI 是 " + top.name +
                    "，标题栏被其它 UI 盖住了。");
                Assert.That(ExecuteEvents.GetEventHandler<IDragHandler>(top.gameObject),
                    Is.SameAs(_header.gameObject),
                    "命中物体不承担拖动，BeginDrag 会落到别处。");
            }

            // 标题栏的拖动由 EventTrigger 注册；面板自身实现接口是第二道防线，
            // 两者都不能替代这里断言的 pointerDrag 解析结果。
            EventTrigger trigger = _header.GetComponent<EventTrigger>();
            Assert.That(trigger, Is.Not.Null);
            Assert.That(trigger.triggers.Select(entry => entry.eventID),
                Is.EquivalentTo(new[] { EventTriggerType.BeginDrag, EventTriggerType.Drag }));
            Assert.That(new UnityEditor.SerializedObject(_panel)
                    .FindProperty("dragHandle").objectReferenceValue,
                Is.SameAs(_header), "序列化的拖动句柄必须就是标题栏。");
        }

        [Test]
        public void Drag_MovesTheWindowOneScreenPixelPerPointerPixel_AtScaleOnePointFive()
        {
            var panelRect = (RectTransform)_panel.transform;
            Vector2 grabLocal = new Vector2(-300f, -33f);
            Vector2 from = ScreenPointFor(grabLocal);
            Vector2 pointerDelta = new Vector2(150f, -60f);
            Vector2 to = from + pointerDelta;

            Vector2 screenBefore = RectTransformUtility.WorldToScreenPoint(null, panelRect.position);
            Vector2 anchoredBefore = panelRect.anchoredPosition;
            Assert.That(_panel.transform.localScale.x, Is.EqualTo(1f).Within(1e-4f),
                "1200x680 的窗口装得进 1280x720 的画布，不应该被缩放。");

            Assert.That(DispatchDrag(from, to), Is.SameAs(_header.gameObject),
                "标题栏按下后拖动应由标题栏承载。");

            Vector2 screenAfter = RectTransformUtility.WorldToScreenPoint(null, panelRect.position);
            Vector2 moved = screenAfter - screenBefore;
            Assert.That(moved.x, Is.EqualTo(pointerDelta.x).Within(0.5f),
                "面板在屏幕上横向只移动了 " + moved.x + "。");
            Assert.That(moved.y, Is.EqualTo(pointerDelta.y).Within(0.5f),
                "面板在屏幕上纵向只移动了 " + moved.y + "。");
            Vector2 shifted = panelRect.anchoredPosition - anchoredBefore;
            Assert.That(shifted.x, Is.EqualTo(pointerDelta.x / ExpectedCanvasScale).Within(0.5f),
                "画布缩放 1.5 时，150 屏幕像素应等于 100 参考单位。");
            Assert.That(shifted.y, Is.EqualTo(pointerDelta.y / ExpectedCanvasScale).Within(0.5f),
                "画布缩放 1.5 时，-60 屏幕像素应等于 -40 参考单位。");

            // 抓取点必须始终停在指针下方：窗口不能跳，也不能只走一半距离。
            Assert.That(RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _header, to, null, out Vector2 grabbed), Is.True);
            Assert.That(grabbed.x, Is.EqualTo(grabLocal.x).Within(0.5f),
                "抓取点横向跑到了 " + grabbed.x + "。");
            Assert.That(grabbed.y, Is.EqualTo(grabLocal.y).Within(0.5f),
                "抓取点纵向跑到了 " + grabbed.y + "。");
        }

        [Test]
        public void Drag_PressesOnATitleBarControlAndStillMovesTheWindow()
        {
            // 折叠按钮位于标题栏右上角；按下它之后拖动，指针位移必须仍然换算到窗口上，
            // 否则实体鼠标从按钮附近开始拖就会失效。
            Button fold = Single(DebugModePanel.RootName).Find("DebugFold").GetComponent<Button>();
            Assert.That(fold, Is.Not.Null);
            var foldRect = (RectTransform)fold.transform;
            Vector2 from = RectTransformUtility.WorldToScreenPoint(null,
                foldRect.TransformPoint(foldRect.rect.center));
            Vector2 to = from + new Vector2(-90f, 45f);
            var panelRect = (RectTransform)_panel.transform;
            Vector2 screenBefore = RectTransformUtility.WorldToScreenPoint(null, panelRect.position);

            GameObject dragTarget = DispatchDrag(from, to);
            Assert.That(dragTarget, Is.SameAs(_panel.gameObject),
                "折叠按钮不实现拖动接口，事件沿层级冒泡到窗口根，由根上的 OnDrag 承载；" +
                "实际解析到的是 " + dragTarget.name + "。");

            Vector2 moved = RectTransformUtility.WorldToScreenPoint(null, panelRect.position) -
                            screenBefore;
            Assert.That(moved.x, Is.EqualTo(-90f).Within(0.5f));
            Assert.That(moved.y, Is.EqualTo(45f).Within(0.5f));
        }

        /// <summary>
        /// 复刻 EventSystem 的鼠标拖动派发：先拿到按下的物体，用
        /// <see cref="ExecuteEvents.GetEventHandler{T}"/> 解析 pointerDrag，再依次派发
        /// BeginDrag 与 Drag。这四步就是 InputSystemUIInputModule 在 ProcessDrag 里做的事，
        /// 所以断言的是生产输入链路，而不是直接调用回调。
        /// </summary>
        private GameObject DispatchDrag(Vector2 from, Vector2 to)
        {
            Graphic pressed = TopmostGraphicAt(from);
            Assert.That(pressed, Is.Not.Null, "按下点 " + from + " 上没有 UI。");
            GameObject dragTarget = ExecuteEvents.GetEventHandler<IBeginDragHandler>(
                pressed.gameObject);
            Assert.That(dragTarget, Is.Not.Null, "按下点 " + from + " 解析不出拖动对象。");

            var pointer = new PointerEventData(_eventSystem)
            {
                button = PointerEventData.InputButton.Left,
                position = from,
                pointerPress = pressed.gameObject,
                pointerDrag = dragTarget
            };
            ExecuteEvents.Execute(dragTarget, pointer, ExecuteEvents.beginDragHandler);

            pointer.delta = to - from;
            pointer.position = to;
            ExecuteEvents.Execute(dragTarget, pointer, ExecuteEvents.dragHandler);
            return dragTarget;
        }

        /// <summary>
        /// EditMode 的画布从不渲染，<see cref="Graphic.depth"/> 恒为 -1，
        /// <see cref="GraphicRaycaster"/> 因此会跳过所有图形，拿不到任何命中。这里改用画布的
        /// 绘制顺序取最上层：父对象先于子对象、同级按兄弟顺序，与 Canvas 的绘制顺序一致。
        /// 渲染之后的真实射线由 Play Mode 证据覆盖。
        /// </summary>
        private Graphic TopmostGraphicAt(Vector2 screenPoint)
        {
            Graphic top = null;
            foreach (Graphic graphic in DrawOrder(_canvas.transform))
            {
                if (!graphic.raycastTarget || !graphic.isActiveAndEnabled)
                    continue;
                if (!RectTransformUtility.RectangleContainsScreenPoint(
                        graphic.rectTransform, screenPoint, null, graphic.raycastPadding))
                    continue;
                top = graphic;
            }
            return top;
        }

        private static IEnumerable<Graphic> DrawOrder(Transform parent)
        {
            foreach (Graphic graphic in parent.GetComponents<Graphic>())
                yield return graphic;
            for (int i = 0; i < parent.childCount; i++)
            {
                foreach (Graphic graphic in DrawOrder(parent.GetChild(i)))
                    yield return graphic;
            }
        }

        private Vector2 ScreenPointFor(Vector2 headerLocal) =>
            RectTransformUtility.WorldToScreenPoint(null, _header.TransformPoint(headerLocal));

        private Transform Single(string name) => _scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
            .Single(transform => transform.name == name);

        private GameObject Create(string name, Transform parent, params Type[] components)
        {
            var go = new GameObject(name, typeof(RectTransform));
            SceneManager.MoveGameObjectToScene(go, _scene);
            if (parent != null)
                go.transform.SetParent(parent, false);
            foreach (Type component in components)
                go.AddComponent(component);
            return go;
        }
    }
}
