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
    /// 滚轮同理：EditMode 的 <c>JapanMapScreenViewportTests</c> 直接调用
    /// <c>JapanMapViewportInput.OnScroll</c>，只能证明处理函数本身。本夹具把虚拟
    /// <see cref="Mouse"/> 的滚轮送到场景自带的 <see cref="InputSystemUIInputModule"/>，
    /// 断言格数经 <c>ScrollWheel</c> 动作变成 <see cref="PointerEventData.scrollDelta"/>、
    /// 由真实 EventSystem 路由到地图视口后，缩放按指针锚点变化，且不改变选中项、
    /// 旅行预览、当前位置与日期。每一格滚轮前还按生产公式推导未夹取候选位置，
    /// 断言它与视口两侧夹取边界都留有正余量，排除“锚点断言其实撞到边界”的假阳性。
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

        /// <summary>视口每次滚轮一格的生产倍率步长。</summary>
        private const float ExpectedZoomStep = 1.2f;

        /// <summary>候选锚点与夹取边界之间要求的最小正余量（视口局部单位）。</summary>
        private const float ClampMargin = 1f;

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

        [UnityTest]
        public IEnumerator VirtualMouse_ScrollOverMap_ZoomsAtPointerWithoutChangingTravelFacts()
        {
            yield return LoadFoundationSceneAndOpenMap();
            yield return AddVirtualMouse();

            Assert.That(_canvas.renderMode, Is.EqualTo(RenderMode.ScreenSpaceOverlay),
                "夹具按覆盖画布（相机为 null）换算屏幕点，实际渲染模式为 " +
                _canvas.renderMode + "。");

            string startLocation = _session.CurrentLocation.Value;
            GameDate startDate = _session.WorldClock.CurrentDate;
            string startSelection = _screen.SelectedHubId;
            bool startPreview = ConfirmTravelButton().interactable;
            int startNodeCount = _nodeLayer.childCount;
            RectTransform hubBefore = FindHubNode(OkazakiHubId);

            Assert.That(startSelection, Is.EqualTo(startLocation),
                "前置条件：打开地图时选中项是当前所在地。");
            Assert.That(startPreview, Is.False,
                "前置条件：打开地图时没有待确认行程。");
            Assert.That(_mapContent.localScale.x, Is.EqualTo(1f).Within(0.01f),
                "前置条件：打开地图按 1x 全境适配，缩放锚定才可观测。");

            // 滚轮落点离开视口中心，让锚定断言不会被“中心不动”蒙对。
            Rect areaRect = _mapArea.rect;
            Vector2 offset = new Vector2(areaRect.width * 0.15f, areaRect.height * 0.12f);
            Assert.That(offset.magnitude, Is.GreaterThan(20f),
                "地图视口过小，夹具无法给出偏离中心的滚轮落点。");
            Vector2 pointerScreen = ScreenPointOf(_mapArea, areaRect.center + offset);

            Graphic top = TopmostRaycast(pointerScreen);
            Assert.That(top, Is.Not.Null,
                "滚轮落点 " + pointerScreen + " 没有命中任何 UI。");
            Assert.That(top.transform == _mapArea || top.transform.IsChildOf(_mapArea), Is.True,
                "滚轮落点最上层是 " + top.name + "，不在地图区域层级下，" +
                "滚轮不会沿层级路由到地图视口输入。");

            Vector2 mapPointBefore = ContentPointAt(pointerScreen);

            // 每一格滚轮前先按生产公式推导“还没被夹取”的候选内容位置，并按视口矩形、
            // 内容尺寸与 pivot 断言该候选点到两侧夹取边界都留有正余量。这样指针锚点
            // 断言不可能因为撞上 ClampToViewport 边界而假阳性：候选点不贴边，滚轮后
            // 实际位置就等于候选点，锚定不成立只能是没有按指针缩放。
            float previousScaleFirst = _mapContent.localScale.x;
            float targetScaleFirst = ExpectedNextScale();
            Vector2 firstCandidate =
                AssertUnclampedCandidate(pointerScreen, targetScaleFirst, "第一格");

            yield return ScrollAt(pointerScreen, 120f);
            AssertScrollZoomedOneStep(previousScaleFirst, "第一格");
            Assert.That(_mapContent.localScale.x, Is.EqualTo(targetScaleFirst).Within(0.005f),
                "第一格滚轮后的倍率不是按 1.2x 推导出的目标倍率，滚轮链路异常。");
            Assert.That(_mapContent.localPosition.x, Is.EqualTo(firstCandidate.x).Within(0.5f),
                "第一格滚轮后的内容 X 位置不是推导出的未夹取候选值：缩放没有按指针锚点进行。");
            Assert.That(_mapContent.localPosition.y, Is.EqualTo(firstCandidate.y).Within(0.5f),
                "第一格滚轮后的内容 Y 位置不是推导出的未夹取候选值：缩放没有按指针锚点进行。");

            Vector2 mapPointAfterFirst = ContentPointAt(pointerScreen);
            Assert.That(mapPointAfterFirst.x, Is.EqualTo(mapPointBefore.x).Within(0.5f),
                "真实滚轮放大后，指针下的地图局部 X 点移动了，缩放没有按指针锚点进行。");
            Assert.That(mapPointAfterFirst.y, Is.EqualTo(mapPointBefore.y).Within(0.5f),
                "真实滚轮放大后，指针下的地图局部 Y 点移动了，缩放没有按指针锚点进行。");

            // 第二格继续放大，锚点仍须固定在同一个屏幕点下，且候选点同样不贴边。
            float previousScaleSecond = _mapContent.localScale.x;
            float targetScaleSecond = ExpectedNextScale();
            Vector2 secondCandidate =
                AssertUnclampedCandidate(pointerScreen, targetScaleSecond, "第二格");

            yield return ScrollAt(pointerScreen, 120f);
            AssertScrollZoomedOneStep(previousScaleSecond, "第二格");
            Assert.That(_mapContent.localScale.x, Is.EqualTo(targetScaleSecond).Within(0.005f),
                "第二格滚轮后的倍率不是按 1.2x 推导出的目标倍率，滚轮链路异常。");
            Assert.That(_mapContent.localPosition.x, Is.EqualTo(secondCandidate.x).Within(0.5f),
                "第二格滚轮后的内容 X 位置不是推导出的未夹取候选值：缩放没有按指针锚点进行。");
            Assert.That(_mapContent.localPosition.y, Is.EqualTo(secondCandidate.y).Within(0.5f),
                "第二格滚轮后的内容 Y 位置不是推导出的未夹取候选值：缩放没有按指针锚点进行。");

            Vector2 mapPointAfterSecond = ContentPointAt(pointerScreen);
            Assert.That(mapPointAfterSecond.x, Is.EqualTo(mapPointBefore.x).Within(0.5f),
                "连续滚轮放大时指针下的地图局部 X 点必须始终不动。");
            Assert.That(mapPointAfterSecond.y, Is.EqualTo(mapPointBefore.y).Within(0.5f),
                "连续滚轮放大时指针下的地图局部 Y 点必须始终不动。");

            // 缩放只改 MapContent 变换，不得重建节点或改动旅行事实。
            Assert.That(_nodeLayer.childCount, Is.EqualTo(startNodeCount),
                "滚轮缩放不得重建地图节点。");
            Assert.That(FindHubNode(OkazakiHubId), Is.SameAs(hubBefore),
                "滚轮缩放不得重建枢纽节点对象。");
            Assert.That(_screen.SelectedHubId, Is.EqualTo(startSelection),
                "滚轮缩放不得改变选中项。");
            Assert.That(ConfirmTravelButton().interactable, Is.EqualTo(startPreview),
                "滚轮缩放不得产生旅行预览。");
            Assert.That(_session.CurrentLocation.Value, Is.EqualTo(startLocation),
                "滚轮缩放不得改变当前位置。");
            Assert.That(_session.WorldClock.CurrentDate, Is.EqualTo(startDate),
                "滚轮缩放不得推进日期。");
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

        /// <summary>屏幕点在共享地图内容上的局部点。覆盖画布的相机为 null。</summary>
        private Vector2 ContentPointAt(Vector2 screen)
        {
            bool mapped = RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _mapContent, screen, null, out Vector2 local);
            Assert.That(mapped, Is.True, "屏幕坐标必须能映射到地图内容。");
            return local;
        }

        /// <summary>
        /// 一格滚轮的倍率期望：<see cref="JapanMapViewport"/> 每次取一步 1.2x，
        /// 且把单帧滚轮幅度夹到一格。上界同时证明没有重复投递。
        /// </summary>
        private void AssertScrollZoomedOneStep(float previousScale, string step)
        {
            float scale = _mapContent.localScale.x;
            Assert.That(scale, Is.GreaterThan(previousScale * 1.1f),
                "真实滚轮" + step + "没有放大地图内容。");
            Assert.That(scale, Is.LessThan(previousScale * 1.35f),
                "一帧内只应放大一档（1.2x），" + step + "后的倍率是 " + scale +
                "，疑似滚轮被重复投递。");
        }

        /// <summary>屏幕点在视口矩形上的局部点（覆盖画布，相机为 null）。</summary>
        private Vector2 ViewportPointerLocal(Vector2 screen)
        {
            bool mapped = RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _mapArea, screen, null, out Vector2 local);
            Assert.That(mapped, Is.True, "屏幕坐标必须能映射到地图视口的局部坐标。");
            return local;
        }

        /// <summary>
        /// 全境适配倍率，对应 <see cref="JapanMapViewport"/> 里由视口与内容尺寸
        /// 求出的 fit。夹具中内容尺寸等于视口尺寸，因此 1x 时它是 1。
        /// </summary>
        private float FitScale()
        {
            Vector2 viewportSize = _mapArea.rect.size;
            Vector2 contentSize = _mapContent.rect.size;
            Assert.That(contentSize.x, Is.GreaterThan(0f), "地图内容必须先有正的宽度。");
            Assert.That(contentSize.y, Is.GreaterThan(0f), "地图内容必须先有正的高度。");
            return Mathf.Min(viewportSize.x / contentSize.x, viewportSize.y / contentSize.y);
        }

        /// <summary>按生产公式推算下一格滚轮后的目标倍率（fit × 夹取后的 1.2x）。</summary>
        private float ExpectedNextScale()
        {
            float fit = FitScale();
            float zoom = _mapContent.localScale.x / fit;
            float nextZoom = Mathf.Clamp(zoom * ExpectedZoomStep,
                JapanMapViewport.MinimumZoom, JapanMapViewport.MaximumZoom);
            return fit * nextZoom;
        }

        /// <summary>
        /// 按生产公式推导一格滚轮后的未夹取候选内容位置，并断言它与横向、纵向
        /// 两侧夹取边界都留有正余量。
        /// <para>
        /// 推导：<c>ZoomAt</c> 取指针下的内容局部点 q = (pointer − position) / oldScale，
        /// 缩放后保持 q 在同一屏幕点下，未夹取候选位置即
        /// pointer − q × targetScale。内容位置由上一帧 <c>ApplyTransform</c> 夹过，
        /// 且上一格的候选点已被本方法断言在区间内部，所以用实际位置作为入口位置
        /// 与生产入口夹取等价。
        /// </para>
        /// <para>
        /// 区间按生产 <c>ClampAxis</c> 的贴边分支求：内容在该轴大于视口时，
        /// 允许位置是 [xMax − (1 − pivot) × 尺寸, xMin + pivot × 尺寸]。候选点与两端
        /// 都留正余量，就把“锚点断言其实测到夹取边界”的假阳性明确排除掉。
        /// </para>
        /// </summary>
        private Vector2 AssertUnclampedCandidate(Vector2 screen, float targetScale, string step)
        {
            Vector2 pointer = ViewportPointerLocal(screen);
            float oldScale = _mapContent.localScale.x;
            Vector2 oldPosition = _mapContent.localPosition;
            Vector2 contentPoint = (pointer - oldPosition) / oldScale;
            Vector2 candidate = pointer - contentPoint * targetScale;

            Rect viewportRect = _mapArea.rect;
            Vector2 pivot = _mapContent.pivot;
            Vector2 scaledSize = _mapContent.rect.size * targetScale;
            Assert.That(scaledSize.x, Is.GreaterThan(viewportRect.width),
                step + "的目标倍率下内容宽度必须大于视口，横向才有夹取区间可比较。");
            Assert.That(scaledSize.y, Is.GreaterThan(viewportRect.height),
                step + "的目标倍率下内容高度必须大于视口，纵向才有夹取区间可比较。");

            float minimumX = viewportRect.xMax - (1f - pivot.x) * scaledSize.x;
            float maximumX = viewportRect.xMin + pivot.x * scaledSize.x;
            float minimumY = viewportRect.yMax - (1f - pivot.y) * scaledSize.y;
            float maximumY = viewportRect.yMin + pivot.y * scaledSize.y;

            Assert.That(candidate.x - minimumX, Is.GreaterThan(ClampMargin),
                step + "的未夹取候选 X=" + candidate.x + " 距横向左边界 " + minimumX +
                " 不足 " + ClampMargin + "，锚定断言可能测到边界夹取。");
            Assert.That(maximumX - candidate.x, Is.GreaterThan(ClampMargin),
                step + "的未夹取候选 X=" + candidate.x + " 距横向右边界 " + maximumX +
                " 不足 " + ClampMargin + "，锚定断言可能测到边界夹取。");
            Assert.That(candidate.y - minimumY, Is.GreaterThan(ClampMargin),
                step + "的未夹取候选 Y=" + candidate.y + " 距纵向下边界 " + minimumY +
                " 不足 " + ClampMargin + "，锚定断言可能测到边界夹取。");
            Assert.That(maximumY - candidate.y, Is.GreaterThan(ClampMargin),
                step + "的未夹取候选 Y=" + candidate.y + " 距纵向上边界 " + maximumY +
                " 不足 " + ClampMargin + "，锚定断言可能测到边界夹取。");

            return candidate;
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
