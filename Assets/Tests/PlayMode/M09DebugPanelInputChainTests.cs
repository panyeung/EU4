#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using SengokuChronicle.Presentation;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SengokuChronicle.Tests.PlayMode
{
    /// <summary>
    /// M09 标题栏拖动的 PlayMode 回归测试。与 EditMode 的
    /// <c>M09DebugPanelDragTests</c> 不同，这里不手工铺画布、不手工派发回调：
    /// 加载真实的 <c>FoundationScene</c>，用场景里的 <see cref="CanvasScaler"/>、
    /// <see cref="GraphicRaycaster"/>、<see cref="EventSystem"/> 与
    /// <see cref="InputSystemUIInputModule"/>，再用 Input System 的虚拟 <see cref="Mouse"/>
    /// 设备产生“移动 → 按下 → 越过拖动阈值 → 移动 → 松开”的状态序列，让模块按帧自己
    /// Process。测试里没有任何 <c>ExecuteEvents</c> 调用，也没有直接调用
    /// <c>DebugModePanel.OnBeginDrag</c>/<c>OnDrag</c>。
    /// <para>
    /// 虚拟设备只证明“Input System 状态事件到达真实 UI 模块时面板如何运动”。它不代表实体
    /// 鼠标，也不覆盖 OS 到 Unity 的输入采集，因此不能据此推断实体鼠标是否失效。
    /// 依赖 <c>TitleScreenController.StartDebugSession</c>（仅在编辑器/开发构建编译），
    /// 所以整段代码包在 <c>UNITY_EDITOR</c> 内。
    /// </para>
    /// </summary>
    public sealed class M09DebugPanelInputChainTests : InputTestFixture
    {
        private const string SceneName = "FoundationScene";
        private const string HeaderName = "DebugHeader";
        private const string FoldButtonName = "DebugFold";

        /// <summary>标题栏内、水印之外的抓取点（标题栏局部坐标）。</summary>
        private static readonly Vector2 HeaderGrabLocal = new Vector2(-300f, -33f);

        private Canvas _canvas;
        private CanvasScaler _scaler;
        private GraphicRaycaster _raycaster;
        private EventSystem _eventSystem;
        private InputSystemUIInputModule _module;
        private DebugModePanel _panel;
        private RectTransform _panelRect;
        private RectTransform _header;
        private Mouse _mouse;

        [UnityTest]
        public IEnumerator SceneWiring_RealScalerRaycasterAndInputModule_HeaderIsTopmostAtGrabPoint()
        {
            yield return LoadFoundationSceneAndOpenPanel();

            // 真实场景组件：不是 EditMode 夹具里手工铺的画布。
            Assert.That(_scaler.enabled, Is.True, "场景 CanvasScaler 必须启用。");
            Assert.That(_scaler.uiScaleMode, Is.EqualTo(CanvasScaler.ScaleMode.ScaleWithScreenSize));
            Assert.That(_scaler.referenceResolution, Is.EqualTo(new Vector2(1280f, 720f)));
            Assert.That(_raycaster.enabled, Is.True, "场景 GraphicRaycaster 必须启用。");
            Assert.That(_module, Is.Not.Null,
                "EventSystem.currentInputModule 必须是真实 InputSystemUIInputModule。");
            Assert.That(_module.enabled, Is.True);
            Assert.That(_canvas.scaleFactor, Is.GreaterThan(0f));

            Vector2 grab = ScreenPointOf(_header, HeaderGrabLocal);
            Graphic top = TopmostRaycast(grab);
            Assert.That(top, Is.Not.Null, "抓取点 " + grab + " 上没有任何 UI 命中。");
            Assert.That(top.gameObject, Is.SameAs(_header.gameObject),
                "抓取点最上层是 " + top.name + "，标题栏被遮住；" +
                "画布缩放为 " + _canvas.scaleFactor + "，命中图形深度为 " + top.depth + "。");
        }

        [UnityTest]
        public IEnumerator VirtualMouse_DragOnHeader_MovesWindowByScreenPixels()
        {
            yield return LoadFoundationSceneAndOpenPanel();
            yield return AddVirtualMouse();

            Vector2 grab = ScreenPointOf(_header, HeaderGrabLocal);
            Vector2 delta = new Vector2(150f, -60f);

            yield return MovePointerTo(grab);
            Press(_mouse.leftButton, queueEventOnly: true);
            yield return WaitFrames(2);

            // 越过 EventSystem 的拖动阈值后才开始拖动；阈值内的位移由 uGUI 的 BeginDrag
            // 吸收，因此基线取在阈值之后。
            yield return MovePointerTo(grab + ThresholdStep());
            Vector2 before = PanelScreenPosition();
            Vector2 anchoredBefore = _panelRect.anchoredPosition;

            Vector2 target = grab + ThresholdStep() + delta;
            yield return MovePointerTo(target);
            Vector2 moved = PanelScreenPosition() - before;
            for (int frame = 0; frame < 8 && !IsClose(moved, delta, 1f); frame++)
            {
                yield return null;
                moved = PanelScreenPosition() - before;
            }
            Vector2 anchoredMoved = _panelRect.anchoredPosition - anchoredBefore;
            bool stillOpen = _panel.IsOpen;

            Release(_mouse.leftButton, queueEventOnly: true);
            yield return WaitFrames(2);

            Assert.That(moved.x, Is.EqualTo(delta.x).Within(1f),
                "指针横向移动 " + delta.x + " 屏幕像素，窗口只移动了 " + moved.x + "。");
            Assert.That(moved.y, Is.EqualTo(delta.y).Within(1f),
                "指针纵向移动 " + delta.y + " 屏幕像素，窗口只移动了 " + moved.y + "。");
            // 画布 1 个世界单位等于 1 个屏幕像素，参考单位位移应等于屏幕位移除以缩放。
            Assert.That(anchoredMoved.x, Is.EqualTo(delta.x / _canvas.scaleFactor).Within(1f),
                "画布缩放 " + _canvas.scaleFactor + " 时，屏幕位移应换算成参考单位。");
            Assert.That(anchoredMoved.y, Is.EqualTo(delta.y / _canvas.scaleFactor).Within(1f));
            Assert.That(stillOpen, Is.True, "拖动标题栏后窗口应保持打开。");
            Assert.That(_panel.IsOpen, Is.True, "松开指针后窗口应保持打开。");
        }

        [UnityTest]
        public IEnumerator VirtualMouse_DragStartingOnFoldButton_MovesWindowWithoutClickingIt()
        {
            yield return LoadFoundationSceneAndOpenPanel();
            yield return AddVirtualMouse();

            Button fold = _panel.transform.Find(FoldButtonName).GetComponent<Button>();
            Assert.That(fold, Is.Not.Null);
            var foldRect = (RectTransform)fold.transform;
            Vector2 press = ScreenPointOf(foldRect, foldRect.rect.center);

            Graphic top = TopmostRaycast(press);
            Assert.That(top, Is.Not.Null);
            // 按钮自身是 Image，文字子节点盖在上面；两者都在按钮层级下，点击会冒泡到 Button。
            Assert.That(top.transform.IsChildOf(fold.transform), Is.True,
                "折叠按钮位置的最上层 UI 是 " + top.name + "，不在按钮层级下，按下点没有落在按钮上。");
            Assert.That(_panel.IsFolded, Is.False);

            Vector2 delta = new Vector2(90f, -45f);
            yield return MovePointerTo(press);
            Press(_mouse.leftButton, queueEventOnly: true);
            yield return WaitFrames(2);

            yield return MovePointerTo(press + ThresholdStep());
            Vector2 before = PanelScreenPosition();

            yield return MovePointerTo(press + ThresholdStep() + delta);
            Vector2 moved = PanelScreenPosition() - before;
            for (int frame = 0; frame < 8 && !IsClose(moved, delta, 1f); frame++)
            {
                yield return null;
                moved = PanelScreenPosition() - before;
            }

            Release(_mouse.leftButton, queueEventOnly: true);
            yield return WaitFrames(2);

            Assert.That(moved.x, Is.EqualTo(delta.x).Within(1f),
                "从折叠按钮按下后横向拖动 " + delta.x + " 像素，窗口只移动了 " + moved.x + "。");
            Assert.That(moved.y, Is.EqualTo(delta.y).Within(1f),
                "从折叠按钮按下后纵向拖动 " + delta.y + " 像素，窗口只移动了 " + moved.y + "。");
            // 越过拖动阈值后松手不应再触发按钮点击，窗口也不能被折叠。
            Assert.That(_panel.IsFolded, Is.False,
                "拖动结束后折叠按钮仍然收到点击，底层控件被误触。");
            Assert.That(_panel.IsOpen, Is.True);
        }

        private IEnumerator LoadFoundationSceneAndOpenPanel()
        {
            AsyncOperation load = SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
            Assert.That(load, Is.Not.Null, "FoundationScene 必须在 Build Settings 中才能按名加载。");
            while (!load.isDone)
                yield return null;
            yield return null;

            TitleScreenController controller =
                UnityEngine.Object.FindAnyObjectByType<TitleScreenController>(
                    FindObjectsInactive.Include);
            Assert.That(controller, Is.Not.Null, "FoundationScene 里应有 TitleScreenController。");
            controller.ShowTitle();
            Assert.That(controller.StartDebugSession(), Is.True,
                "标题页应能打开 Debug 会话：" + controller.Status);
            yield return null;

            _panel = UnityEngine.Object.FindAnyObjectByType<DebugModePanel>(
                FindObjectsInactive.Include);
            Assert.That(_panel, Is.Not.Null);
            Assert.That(_panel.IsOpen, Is.True, "Debug 窗口应已打开。");
            _panelRect = (RectTransform)_panel.transform;
            _header = (RectTransform)_panel.transform.Find(HeaderName);
            Assert.That(_header, Is.Not.Null, "Debug 窗口应有标题栏 " + HeaderName + "。");

            _eventSystem = EventSystem.current;
            Assert.That(_eventSystem, Is.Not.Null, "FoundationScene 应自带 EventSystem。");
            _module = _eventSystem.currentInputModule as InputSystemUIInputModule;
            Assert.That(_module, Is.Not.Null,
                "FoundationScene 的 EventSystem 应使用 InputSystemUIInputModule，实际为 " +
                (_eventSystem.currentInputModule == null
                    ? "null"
                    : _eventSystem.currentInputModule.GetType().Name));

            _canvas = _panel.GetComponentInParent<Canvas>().rootCanvas;
            Assert.That(_canvas, Is.Not.Null);
            _scaler = _canvas.GetComponent<CanvasScaler>();
            Assert.That(_scaler, Is.Not.Null, "画布应有 CanvasScaler。");
            _raycaster = _canvas.GetComponent<GraphicRaycaster>();
            Assert.That(_raycaster, Is.Not.Null, "画布应有 GraphicRaycaster。");
            Assert.That(_panel.transform.localScale.x, Is.EqualTo(1f).Within(1e-4f),
                "1200x680 的窗口装得进 1280x720 的参考分辨率，不应被缩放。");

            // GraphicRaycaster 跳过 depth == -1 的图形，而 depth 只在实际渲染后才有值。
            Graphic headerGraphic = _header.GetComponent<Graphic>();
            for (int frame = 0; frame < 120 && headerGraphic.depth < 0; frame++)
                yield return null;
            Assert.That(headerGraphic.depth, Is.GreaterThanOrEqualTo(0),
                "标题栏在 120 帧内没有被渲染（Graphic.depth 仍为 -1）。PlayMode 射线断言需要 " +
                "Editor 真正渲染 Game 视图。");
        }

        private IEnumerator AddVirtualMouse()
        {
            _mouse = InputSystem.AddDevice<Mouse>("M09VirtualMouse");
            _mouse.MakeCurrent();
            Assert.That(Mouse.current, Is.SameAs(_mouse));
            yield return null;
        }

        private IEnumerator MovePointerTo(Vector2 screenPoint)
        {
            Move(_mouse.position, screenPoint, queueEventOnly: true);
            yield return WaitFrames(2);
        }

        private Vector2 ThresholdStep()
        {
            float step = _eventSystem.pixelDragThreshold + 4f;
            return new Vector2(step, -step);
        }

        private Vector2 PanelScreenPosition() =>
            RectTransformUtility.WorldToScreenPoint(null, _panelRect.position);

        private static Vector2 ScreenPointOf(RectTransform rect, Vector2 localPoint) =>
            RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(localPoint));

        private Graphic TopmostRaycast(Vector2 screenPoint)
        {
            var pointer = new PointerEventData(_eventSystem) { position = screenPoint };
            var results = new List<RaycastResult>();
            _eventSystem.RaycastAll(pointer, results);
            if (results.Count == 0)
                return null;
            return results[0].gameObject.GetComponent<Graphic>();
        }

        private static IEnumerator WaitFrames(int frames)
        {
            for (int i = 0; i < frames; i++)
                yield return null;
        }

        private static bool IsClose(Vector2 value, Vector2 expected, float tolerance) =>
            Mathf.Abs(value.x - expected.x) <= tolerance &&
            Mathf.Abs(value.y - expected.y) <= tolerance;
    }
}
#endif
