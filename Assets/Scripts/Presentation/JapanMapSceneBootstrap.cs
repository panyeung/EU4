using System;
using SengokuChronicle.Application;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SengokuChronicle.Presentation
{
    /// <summary>
    /// Adds the national-map entry point to an existing scene at runtime. It never
    /// creates a second map when one already exists. A scene without a Canvas is
    /// left untouched. FoundationScene carries one instance so the entry exists
    /// even when WorldMapController is disabled or replaced.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class JapanMapSceneBootstrap : MonoBehaviour
    {
        public const string LauncherName = "JapanMapLauncherButton";

        /// <summary>入口按钮固定尺寸；右上角右对齐，避免按画布百分比缩放后压住顶栏资源条。</summary>
        public const float LauncherWidth = 150f;
        public const float LauncherHeight = 40f;
        public const float LauncherRightMargin = 16f;
        public const float LauncherTopMargin = 6f;

        /// <summary>入口按钮右侧占用的水平总宽度，顶栏资源条据此让位。</summary>
        public const float LauncherReservedWidth = LauncherWidth + LauncherRightMargin;
        public const string RootName = "JapanMapRoot";
        public const string TitleOverlayName = "M6TitleRoot";
        public const string EventChoiceOverlayName = "EventChoiceRoot";
        public const string TacticalOverlayName = "M6TacticalMissionOverlay";

        private void Start()
        {
            GameSession session = FindFirstObjectByType<GameSession>();
            if (session == null)
                return;

            EnsureForSession(session);
        }

        /// <summary>
        /// 地图入口的统一入口：先按 <see cref="ResolveTargetCanvas"/> 解析目标 Canvas，
        /// 再幂等创建入口与地图根。本组件与 <see cref="WorldMapController"/> 的 Start
        /// 都只调这里，两个启动顺序都会落在同一个 Canvas 上，不会各按自身 Canvas 建一套。
        /// </summary>
        public static JapanMapScreen EnsureForSession(GameSession session)
        {
            return EnsureFor(session, ResolveTargetCanvas(session));
        }

        /// <summary>
        /// 入口要挂到的 Canvas。多 Canvas 场景里场景首个 Canvas 不一定是 HUD，
        /// 因此顺序固定为：GameSession 所在 Canvas，其次 WorldMapController 所在
        /// Canvas，最后才回退场景首个 Canvas。三者都没有时返回 null，入口静默跳过。
        /// </summary>
        public static Canvas ResolveTargetCanvas(GameSession session)
        {
            Canvas canvas = session != null ? session.GetComponentInParent<Canvas>() : null;
            if (canvas != null)
                return canvas;

            WorldMapController controller = FindFirstObjectByType<WorldMapController>();
            if (controller != null)
                canvas = controller.GetComponentInParent<Canvas>();
            if (canvas != null)
                return canvas;

            return FindFirstObjectByType<Canvas>();
        }

        public static JapanMapScreen EnsureFor(GameSession session, Canvas canvas)
        {
            if (canvas == null)
                return null;

            JapanMapScreen existing = canvas.GetComponentInChildren<JapanMapScreen>(true);
            if (existing != null)
            {
                existing.BindSession(session);
                return existing;
            }

            JapanMapScreen screen = null;
            GameObject launcher = null;
            GameObject rootObject = null;
            try
            {
                rootObject = new GameObject(RootName, typeof(RectTransform), typeof(Image));
                var rootRect = (RectTransform)rootObject.transform;
                rootRect.SetParent(canvas.transform, false);
                rootRect.anchorMin = Vector2.zero;
                rootRect.anchorMax = Vector2.one;
                rootRect.offsetMin = Vector2.zero;
                rootRect.offsetMax = Vector2.zero;
                rootObject.layer = canvas.gameObject.layer;
                rootObject.SetActive(false);
                PlaceBelowProtectedOverlays(rootRect, canvas.transform);

                screen = rootObject.AddComponent<JapanMapScreen>();
                screen.Initialize(session, rootRect);

                launcher = new GameObject(LauncherName, typeof(RectTransform), typeof(Image),
                    typeof(Button));
                var launcherRect = (RectTransform)launcher.transform;
                launcherRect.SetParent(canvas.transform, false);
                // 固定尺寸右对齐：按画布百分比摆放时，窄窗口下按钮会横跨顶栏资源条，
                // 盖住物资与情报两个 chip。顶栏布局按同一组常量让出这块区域。
                launcherRect.anchorMin = new Vector2(1f, 1f);
                launcherRect.anchorMax = new Vector2(1f, 1f);
                launcherRect.pivot = new Vector2(1f, 1f);
                launcherRect.sizeDelta = new Vector2(LauncherWidth, LauncherHeight);
                launcherRect.anchoredPosition = new Vector2(-LauncherRightMargin, -LauncherTopMargin);
                launcher.layer = canvas.gameObject.layer;

                var launcherImage = launcher.GetComponent<Image>();
                launcherImage.color = new Color(0.16f, 0.34f, 0.48f, 0.96f);
                var launcherButton = launcher.GetComponent<Button>();
                launcherButton.targetGraphic = launcherImage;
                JapanMapScreen captured = screen;
                launcherButton.onClick.AddListener(captured.Toggle);

                Graphic label = JapanMapFonts.CreateText(launcherRect, "Label", 12,
                    TextAlignmentOptions.Center);
                var labelRect = (RectTransform)label.transform;
                labelRect.offsetMin = new Vector2(3f, 1f);
                labelRect.offsetMax = new Vector2(-3f, -1f);
                JapanMapFonts.SetColor(label, Color.white);
                JapanMapFonts.SetWrap(label, false);
                JapanMapFonts.SetEllipsis(label, true);
                JapanMapFonts.SetText(label, "全国地图");

                PlaceBelowProtectedOverlays(launcherRect, canvas.transform);
                return screen;
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Japan map scene bootstrap failed: " + exception.Message);
                if (launcher != null)
                    UnityEngine.Object.Destroy(launcher);
                if (rootObject != null)
                    UnityEngine.Object.Destroy(rootObject);
                return null;
            }
        }

        /// <summary>
        /// Keeps the map root and its entry button above ordinary UI but below the
        /// full-screen title, event-choice and tactical overlays. Those overlays are
        /// opened by activation alone, so a map object left as the last Canvas child
        /// would otherwise draw on top of them. When no protected overlay exists, the
        /// target stays last.
        /// </summary>
        public static int PlaceBelowProtectedOverlays(RectTransform target, Transform canvas)
        {
            if (target == null || canvas == null || target.parent != canvas)
                return -1;

            int overlayIndex = EarliestProtectedOverlayIndex(canvas);
            if (overlayIndex == int.MaxValue)
            {
                target.SetAsLastSibling();
                return target.GetSiblingIndex();
            }

            // Insert immediately before the earliest overlay. If the target is already
            // earlier, removing it first shifts that overlay down by one, so use
            // overlayIndex - 1; if it is later, the overlay index is unchanged.
            int current = target.GetSiblingIndex();
            int insertIndex = current < overlayIndex ? overlayIndex - 1 : overlayIndex;
            if (insertIndex < 0)
                insertIndex = 0;
            if (insertIndex != current)
                target.SetSiblingIndex(insertIndex);
            return target.GetSiblingIndex();
        }

        private static int EarliestProtectedOverlayIndex(Transform canvas)
        {
            int index = int.MaxValue;
            index = Math.Min(index, SiblingIndex(canvas, TitleOverlayName));
            index = Math.Min(index, SiblingIndex(canvas, EventChoiceOverlayName));
            index = Math.Min(index, SiblingIndex(canvas, TacticalOverlayName));
            return index;
        }

        private static int SiblingIndex(Transform canvas, string childName)
        {
            Transform child = canvas.Find(childName);
            return child != null ? child.GetSiblingIndex() : int.MaxValue;
        }
    }
}
