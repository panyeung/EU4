#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;
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
    /// 全国地图屏幕的 PlayMode 输入回归。与 EditMode 的
    /// <c>JapanMapViewportTests</c>/<c>JapanMapScreenViewportTests</c> 不同，这里
    /// 不手工铺画布、不直接调用 <c>JapanMapViewportInput.OnBeginDrag/OnDrag/OnEndDrag</c>，
    /// 也不走 <c>ExecuteEvents</c>：加载真实 <c>FoundationScene</c>，用场景里的
    /// <see cref="CanvasScaler"/>、<see cref="GraphicRaycaster"/>、
    /// <see cref="EventSystem"/> 与 <see cref="InputSystemUIInputModule"/>，再用
    /// Input System 的虚拟 <see cref="Mouse"/> 产生“移动 → 按下 → 阈值内移动 →
    /// 越过拖动阈值 → 松开”的状态序列，让 UI 模块按帧自己 Process。
    /// <para>
    /// 复审缺口：EditMode 用例直接调用拖动处理函数，不能证明真实 EventSystem 会在
    /// 达到 <c>pixelDragThreshold</c> 后派发 BeginDrag/Drag/EndDrag，也不能证明
    /// 节点 <see cref="Button"/> 不会在拖动后误触。本夹具用真实射线命中枢纽按钮
    /// 作为按下点，断言拖动状态只在阈值之后出现、共享地图内容真的被平移、松开后
    /// 拖动状态被清除，并断言没有误选枢纽、没有旅行预览、位置与日期不变；短按则
    /// 仍走原有的选择/预览路径。
    /// </para>
    /// <para>
    /// 虚拟设备只证明输入状态事件到达真实 UI 模块后的行为，不代表实体鼠标，也不覆盖
    /// OS 到 Unity 的输入采集。射线断言要求 Editor 真正渲染 Game 视图。
    /// </para>
    /// </summary>
    public sealed class JapanGeographicMapPlayModeTests : InputTestFixture
    {
        private const string SceneName = "FoundationScene";
        private const string OkazakiHubId = "mikawa.okazaki";
        private const string MapAreaName = "JapanMapArea";
        private const string MapContentName = "MapContent";
        private const string NodeLayerName = "HubNodes";

        private Canvas _canvas;
        private EventSystem _eventSystem;
        private InputSystemUIInputModule _module;
        private GameSession _session;
        private JapanMapScreen _screen;
        private JapanMapViewportInput _input;
        private RectTransform _mapArea;
        private RectTransform _mapContent;
        private RectTransform _nodeLayer;
        private Mouse _mouse;

        [UnityTest]
        public IEnumerator VirtualMouse_DragFromHubPastThreshold_PansWithoutSelectingOrPreviewing()
        {
            yield return LoadFoundationSceneAndOpenMap();
            yield return AddVirtualMouse();

            string startLocation = _session.CurrentLocation.Value;
            GameDate startDate = _session.WorldClock.CurrentDate;
            Assert.That(_screen.SelectedHubId, Is.EqualTo(startLocation),
                "前置条件：打开地图时选中项是当前所在地。");

            // 先滚轮放大，给平移到余量；滚轮也走真实的 Input System → UI 模块链路。
            Vector2 mapCenter = ScreenPointOf(_mapArea, _mapArea.rect.center);
            for (int attempt = 0; attempt < 6 && _mapContent.localScale.x < 1.5f; attempt++)
                yield return ScrollAt(mapCenter, 120f);
            Assert.That(_mapContent.localScale.x, Is.GreaterThan(1.5f),
                "真实滚轮输入没有放大视口，无法观测拖动位移。");

            RectTransform hub = FindHubNode(OkazakiHubId);
            Vector2 pressPoint = ScreenPointOf(hub, hub.rect.center);
            Graphic top = TopmostRaycast(pressPoint);
            Assert.That(top, Is.Not.Null, "枢纽按钮中心 " + pressPoint + " 没有命中任何 UI。");
            Assert.That(top.transform.IsChildOf(hub), Is.True,
                "按下点最上层是 " + top.name + "，不在枢纽按钮层级下，拖动起点没有落在按钮上。");

            yield return MovePointerTo(pressPoint);
            Press(_mouse.leftButton, queueEventOnly: true);
            yield return WaitFrames(2);
            Assert.That(_input.IsDragging, Is.False,
                "按下未越过拖动阈值时不得进入拖动状态。");

            // 阈值内移动：仍是候选点击，EventSystem 不应派发 BeginDrag。
            Vector2 subThreshold = new Vector2(_eventSystem.pixelDragThreshold * 0.4f, 0f);
            yield return MovePointerTo(pressPoint + subThreshold);
            Assert.That(_input.IsDragging, Is.False,
                "未超过 uGUI 拖动阈值时不得派发 BeginDrag。");

            // 越过阈值：真实模块应派发 BeginDrag + Drag。
            Vector2 thresholdPoint = pressPoint + subThreshold +
                new Vector2(_eventSystem.pixelDragThreshold + 6f, -4f);
            yield return MovePointerTo(thresholdPoint);
            for (int frame = 0; frame < 8 && !_input.IsDragging; frame++)
                yield return null;
            Assert.That(_input.IsDragging, Is.True,
                "超过阈值后 BeginDrag 没有到达地图视口输入组件。");

            Vector2 contentAtThreshold = _mapContent.localPosition;
            Vector2 extra = new Vector2(90f, -50f);
            Vector2 dragEnd = thresholdPoint + extra;
            yield return MovePointerTo(dragEnd);

            Vector2 expected = MapToViewportLocal(dragEnd) - MapToViewportLocal(thresholdPoint);
            Vector2 moved = (Vector2)_mapContent.localPosition - contentAtThreshold;
            for (int frame = 0; frame < 8; frame++)
            {
                if (IsClose(moved, expected, 1.5f))
                    break;
                yield return null;
                moved = (Vector2)_mapContent.localPosition - contentAtThreshold;
            }

            Vector2 margin = ZoomMargin();
            Assert.That(Mathf.Abs(expected.x), Is.LessThan(margin.x - 1f),
                "夹具的横向拖动必须留在夹取余量内，否则会测到边界而不是拖动。");
            Assert.That(Mathf.Abs(expected.y), Is.LessThan(margin.y - 1f),
                "夹具的纵向拖动必须留在夹取余量内，否则会测到边界而不是拖动。");
            Assert.That(expected.magnitude, Is.GreaterThan(10f),
                "夹具的拖动必须产生可观测位移。");
            Assert.That(moved.x, Is.EqualTo(expected.x).Within(1.5f),
                "拖动没有按真实起终点平移共享地图内容（Drag 未到达）。");
            Assert.That(moved.y, Is.EqualTo(expected.y).Within(1.5f),
                "拖动没有按真实起终点平移共享地图内容（Drag 未到达）。");

            // 地图内容跟随指针平移，松手点必须仍在同一个枢纽按钮上；否则
            // “拖动后没有误选” 可能只是因为松手落到了空白处，断言就没有区分力。
            Graphic releaseTop = TopmostRaycast(dragEnd);
            Assert.That(releaseTop, Is.Not.Null,
                "松手点 " + dragEnd + " 没有命中任何 UI。");
            Assert.That(releaseTop.transform.IsChildOf(hub), Is.True,
                "松开时最上层是 " + releaseTop.name + "，不在被按下的枢纽按钮上，" +
                "误选断言不成立。");

            Release(_mouse.leftButton, queueEventOnly: true);
            yield return WaitFrames(2);
            Assert.That(_input.IsDragging, Is.False,
                "松开后 EndDrag 没有清除拖动状态。");

            // 拖动结束后不得误选节点或产生旅行预览，也不得改变旅行事实。
            Assert.That(_screen.SelectedHubId, Is.EqualTo(startLocation),
                "从枢纽按钮开始拖动后，松手误选了节点。");
            Assert.That(ConfirmTravelButton().interactable, Is.False,
                "拖动后产生了旅行预览。");
            Assert.That(_session.CurrentLocation.Value, Is.EqualTo(startLocation),
                "拖动不得改变当前位置。");
            Assert.That(_session.WorldClock.CurrentDate, Is.EqualTo(startDate),
                "拖动不得推进日期。");
        }

        [UnityTest]
        public IEnumerator VirtualMouse_ShortPressOnHub_SelectsAndPreviewsWithoutTraveling()
        {
            yield return LoadFoundationSceneAndOpenMap();
            yield return AddVirtualMouse();

            string startLocation = _session.CurrentLocation.Value;
            GameDate startDate = _session.WorldClock.CurrentDate;
            Assert.That(_screen.SelectedHubId, Is.EqualTo(startLocation));

            RectTransform hub = FindHubNode(OkazakiHubId);
            Vector2 pressPoint = ScreenPointOf(hub, hub.rect.center);
            Assert.That(TopmostRaycast(pressPoint).transform.IsChildOf(hub), Is.True,
                "按下点没有落在枢纽按钮上。");

            yield return MovePointerTo(pressPoint);
            Press(_mouse.leftButton, queueEventOnly: true);
            yield return WaitFrames(2);
            Assert.That(_input.IsDragging, Is.False);

            Release(_mouse.leftButton, queueEventOnly: true);
            yield return WaitFrames(3);

            Assert.That(_screen.SelectedHubId, Is.EqualTo(OkazakiHubId),
                "短按枢纽按钮没有沿用原来的选择路径。");
            Assert.That(ConfirmTravelButton().interactable, Is.True,
                "短按可达枢纽没有产生旅行预览。");
            Assert.That(_session.CurrentLocation.Value, Is.EqualTo(startLocation),
                "预览不得移动当前位置。");
            Assert.That(_session.WorldClock.CurrentDate, Is.EqualTo(startDate),
                "预览不得推进日期。");
        }

        private IEnumerator LoadFoundationSceneAndOpenMap()
        {
            AsyncOperation load = SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
            Assert.That(load, Is.Not.Null, "FoundationScene 必须在 Build Settings 中才能按名加载。");
            while (!load.isDone)
                yield return null;
            yield return null;

            TitleScreenController title =
                UnityEngine.Object.FindAnyObjectByType<TitleScreenController>(
                    FindObjectsInactive.Include);
            Assert.That(title, Is.Not.Null, "FoundationScene 里应有 TitleScreenController。");
            // 走玩家的真实开始路径：新局 1560-04-01 清洲并隐藏标题。
            Assert.That(title.StartNewGame(), Is.True, "新游戏没有启动：" + title.Status);
            yield return null;

            _session = UnityEngine.Object.FindAnyObjectByType<GameSession>(
                FindObjectsInactive.Include);
            Assert.That(_session, Is.Not.Null, "FoundationScene 里应有 GameSession。");

            _screen = UnityEngine.Object.FindAnyObjectByType<JapanMapScreen>(
                FindObjectsInactive.Include);
            Assert.That(_screen, Is.Not.Null,
                "FoundationScene 的日本地图入口应已创建 JapanMapScreen。");
            _screen.Open();
            yield return null;

            _eventSystem = EventSystem.current;
            Assert.That(_eventSystem, Is.Not.Null, "FoundationScene 应自带 EventSystem。");
            _module = _eventSystem.currentInputModule as InputSystemUIInputModule;
            Assert.That(_module, Is.Not.Null,
                "EventSystem 应使用真实 InputSystemUIInputModule，实际为 " +
                (_eventSystem.currentInputModule == null
                    ? "null"
                    : _eventSystem.currentInputModule.GetType().Name));
            Assert.That(_module.enabled, Is.True);

            _mapArea = (RectTransform)_screen.transform.Find(MapAreaName);
            Assert.That(_mapArea, Is.Not.Null, "地图区域必须存在。");
            _canvas = _mapArea.GetComponentInParent<Canvas>().rootCanvas;
            Assert.That(_canvas, Is.Not.Null);
            Assert.That(_canvas.GetComponent<GraphicRaycaster>(), Is.Not.Null,
                "画布应有 GraphicRaycaster。");
            Assert.That(_canvas.GetComponent<CanvasScaler>(), Is.Not.Null,
                "画布应有 CanvasScaler。");

            _mapContent = (RectTransform)_mapArea.Find(MapContentName);
            _nodeLayer = _mapContent != null
                ? (RectTransform)_mapContent.Find(NodeLayerName)
                : null;
            Assert.That(_mapContent, Is.Not.Null, "地图视口必须有直接子级 MapContent。");
            Assert.That(_nodeLayer, Is.Not.Null, "MapContent 下必须有 HubNodes 节点层。");
            Assert.That(_nodeLayer.childCount, Is.GreaterThan(0), "地图节点尚未生成。");

            _input = _mapArea.GetComponent<JapanMapViewportInput>();
            Assert.That(_input, Is.Not.Null, "地图视口必须挂接 JapanMapViewportInput。");

            // 射线命中要求图形真的被渲染过（Graphic.depth 只在渲染后更新）。
            Graphic nodeGraphic = FindHubNode(OkazakiHubId).GetComponent<Graphic>();
            for (int frame = 0; frame < 300 && nodeGraphic.depth < 0; frame++)
                yield return null;
            Assert.That(nodeGraphic.depth, Is.GreaterThanOrEqualTo(0),
                "枢纽节点在 300 帧内没有被渲染（Graphic.depth 仍为 -1）。" +
                "PlayMode 射线断言需要 Editor 真正渲染 Game 视图。");
        }

        private IEnumerator AddVirtualMouse()
        {
            _mouse = InputSystem.AddDevice<Mouse>("JapanMapVirtualMouse");
            _mouse.MakeCurrent();
            Assert.That(Mouse.current, Is.SameAs(_mouse));
            yield return null;
        }

        private IEnumerator MovePointerTo(Vector2 screenPoint)
        {
            Move(_mouse.position, screenPoint, queueEventOnly: true);
            yield return WaitFrames(2);
        }

        private IEnumerator ScrollAt(Vector2 screenPoint, float amount)
        {
            Move(_mouse.position, screenPoint, queueEventOnly: true);
            yield return WaitFrames(2);
            Set(_mouse.scroll, new Vector2(0f, amount), queueEventOnly: true);
            yield return null;
            Set(_mouse.scroll, Vector2.zero, queueEventOnly: true);
            yield return WaitFrames(2);
        }

        private RectTransform FindHubNode(string hubId)
        {
            Transform node = _nodeLayer.Find("Hub_" + hubId);
            Assert.That(node, Is.Not.Null, "缺少枢纽节点 Hub_" + hubId);
            return (RectTransform)node;
        }

        private Button ConfirmTravelButton()
        {
            Transform button = _screen.transform.Find("DetailPanel/ConfirmTravelButton");
            Assert.That(button, Is.Not.Null, "缺少确认出发按钮。");
            return button.GetComponent<Button>();
        }

        private Vector2 ZoomMargin()
        {
            return new Vector2(
                (_mapContent.rect.width * _mapContent.localScale.x - _mapArea.rect.width) * 0.5f,
                (_mapContent.rect.height * _mapContent.localScale.y - _mapArea.rect.height) * 0.5f);
        }

        private Vector2 MapToViewportLocal(Vector2 screen)
        {
            bool mapped = RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _mapArea, screen, null, out Vector2 local);
            Assert.That(mapped, Is.True, "屏幕坐标必须能映射到地图视口。");
            return local;
        }

        private Graphic TopmostRaycast(Vector2 screenPoint)
        {
            var pointer = new PointerEventData(_eventSystem) { position = screenPoint };
            var results = new List<RaycastResult>();
            _eventSystem.RaycastAll(pointer, results);
            return results.Count == 0 ? null : results[0].gameObject.GetComponent<Graphic>();
        }

        private static Vector2 ScreenPointOf(RectTransform rect, Vector2 localPoint)
        {
            return RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(localPoint));
        }

        private static IEnumerator WaitFrames(int frames)
        {
            for (int i = 0; i < frames; i++)
                yield return null;
        }

        private static bool IsClose(Vector2 value, Vector2 expected, float tolerance)
        {
            return Mathf.Abs(value.x - expected.x) <= tolerance &&
                   Mathf.Abs(value.y - expected.y) <= tolerance;
        }
    }
}
#endif
