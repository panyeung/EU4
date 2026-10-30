using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SengokuChronicle.Presentation;

namespace SengokuChronicle.Editor
{
    /// <summary>
    /// M2 世界地图 UI 的单一布局事实源（M2-02 布局修复）。
    /// 首次创建（CreateM2WorldMap.BuildRoot）与「更新已有根」（UpdateM2WorldMapLayout）
    /// 共用同一套几何，保证两种路径产出一致。
    ///
    /// 根面板位于 M1 WorldClockPanel 与 AdvanceButton 的实际显示矩形下方，
    /// 尺寸相对于 Canvas 的有效 Rect 计算；空间不足时拒绝布局，而不是覆盖 M1。
    /// </summary>
    public static class WorldMapLayout
    {
        public const string RootName = "M2WorldMapRoot";
        public const string WorldClockPanelName = "WorldClockPanel";
        public const string AdvanceButtonName = "AdvanceButton";

        // ---- 响应式分区约束（Canvas reference units）----
        public const float HorizontalMargin = 32f;
        public const float BottomMargin = 8f;
        public const float M1Clearance = 16f;
        public const float MinimumRootWidth = 1120f;
        public const float MinimumRootHeight = 256f;
        public static readonly Vector2 RootPivot = new Vector2(0.5f, 0.5f);
        public static readonly Vector3 RootLocalScale = Vector3.one;

        // ---- 必需后代名称（更新路径唯一解析；缺失/重复即拒绝）----
        public static readonly string[] RequiredChildNames =
        {
            "TitleText", "LocationMarkerText", "CurrentDateText", "FeedbackText",
            "DestinationNameText", "TravelTimeText", "ArrivalDateText",
            "ConfirmButton", "CancelButton", "ReturnToMapButton",
            "TownMenuRoot", "TownMenuTitleText",
            "Facility1Text", "Facility2Text", "Facility3Text",
            "DestButton_0", "DestButton_1", "DestButton_2",
        };

        /// <summary>子对象布局规格（全部 center-anchor / center-pivot / localScale=1）。</summary>
        public sealed class ChildSpec
        {
            public readonly string Name;
            public readonly Vector2 AnchoredPos;
            public readonly Vector2 SizeDelta;
            public readonly int FontSize;
            public readonly bool StretchFill; // 仅 TownMenuRoot

            public ChildSpec(string name, Vector2 pos, Vector2 size, int fontSize, bool stretchFill = false)
            {
                Name = name;
                AnchoredPos = pos;
                SizeDelta = size;
                FontSize = fontSize;
                StretchFill = stretchFill;
            }
        }

        /// <summary>根内部布局（y 以根中心为 0，向上为正；适配实际 M1 下方分区）。</summary>
        public static readonly ChildSpec[] ChildSpecs =
        {
            // 标题与当前状态
            new ChildSpec("TitleText",            new Vector2(0f, 110f), new Vector2(600f, 36f), 32),
            new ChildSpec("LocationMarkerText",   new Vector2(0f, 76f),  new Vector2(540f, 28f), 24),
            new ChildSpec("CurrentDateText",      new Vector2(0f, 48f),  new Vector2(500f, 24f), 22),
            new ChildSpec("FeedbackText",         new Vector2(0f, 22f),  new Vector2(1120f, 22f), 20),
            // 目的地、行程与到达日期：同一预览行分列展示
            new ChildSpec("DestinationNameText",  new Vector2(-380f, -1f),  new Vector2(340f, 24f), 20),
            new ChildSpec("TravelTimeText",       new Vector2(0f, -1f),     new Vector2(240f, 24f), 20),
            new ChildSpec("ArrivalDateText",      new Vector2(380f, -1f),   new Vector2(340f, 24f), 20),
            // 三个横向地点按钮，保持足够点击尺寸
            new ChildSpec("DestButton_0",         new Vector2(-350f, -44f), new Vector2(220f, 60f), 26),
            new ChildSpec("DestButton_1",         new Vector2(0f, -44f),    new Vector2(220f, 60f), 26),
            new ChildSpec("DestButton_2",         new Vector2(350f, -44f),  new Vector2(220f, 60f), 26),
            // 底部操作：确认 / 取消
            new ChildSpec("ConfirmButton",        new Vector2(-145f, -104f), new Vector2(260f, 48f), 26),
            new ChildSpec("CancelButton",         new Vector2(145f, -104f),  new Vector2(260f, 48f), 26),
            // 城镇菜单（同面板内覆盖层）
            new ChildSpec("TownMenuRoot",         Vector2.zero, Vector2.zero, 0, stretchFill: true),
            new ChildSpec("TownMenuTitleText",    new Vector2(0f, 104f), new Vector2(500f, 32f), 28),
            new ChildSpec("Facility1Text",        new Vector2(0f, 38f),  new Vector2(520f, 30f), 20),
            new ChildSpec("Facility2Text",        new Vector2(0f, 0f),   new Vector2(520f, 30f), 20),
            new ChildSpec("Facility3Text",        new Vector2(0f, -38f), new Vector2(520f, 30f), 20),
            new ChildSpec("ReturnToMapButton",    new Vector2(0f, -102f), new Vector2(260f, 52f), 24),
        };


        // ---- 应用布局（创建与更新共用）----

        /// <summary>设置根面板为 Canvas/M1 实际几何推导出的安全区域。</summary>
        public static void ApplyRootLayout(RectTransform root, RectTransform canvas, Rect safeArea)
        {
            if (root == null) throw new ArgumentNullException(nameof(root));
            if (canvas == null) throw new ArgumentNullException(nameof(canvas));
            Rect canvasRect = canvas.rect;
            if (canvasRect.width <= 0f || canvasRect.height <= 0f)
                throw new ArgumentException("Canvas Rect must have positive width and height.", nameof(canvas));

            if (safeArea.width <= 0f || safeArea.height <= 0f ||
                safeArea.xMin < canvasRect.xMin || safeArea.xMax > canvasRect.xMax ||
                safeArea.yMin < canvasRect.yMin || safeArea.yMax > canvasRect.yMax)
                throw new ArgumentException("Safe area must be a positive rectangle inside the Canvas-local rect.", nameof(safeArea));

            // RectTransform anchors are normalized against the parent's rect, whose local
            // origin is rect.min (not necessarily Vector2.zero). Anchor the root pivot at
            // the safe-area center and express its size in Canvas-local units. This keeps
            // Canvas-local geometry independent of pivot, non-zero rect origins and scale.
            Vector2 centerAnchor = new Vector2(
                (safeArea.center.x - canvasRect.xMin) / canvasRect.width,
                (safeArea.center.y - canvasRect.yMin) / canvasRect.height);
            root.anchorMin = centerAnchor;
            root.anchorMax = centerAnchor;
            root.pivot = RootPivot;
            root.sizeDelta = safeArea.size;
            root.anchoredPosition = Vector2.zero;
            root.transform.localScale = RootLocalScale;
        }

        /// <summary>解析 Canvas 下真实的 M1 日期面板与「推进一天」按钮矩形。</summary>
        public static bool TryFindM1ReservedRects(
            Transform canvas,
            out RectTransform worldClockPanel,
            out RectTransform advanceButton,
            out string error)
        {
            worldClockPanel = null;
            advanceButton = null;
            error = null;
            if (canvas == null || canvas.GetComponent<Canvas>() == null)
            {
                error = "Canvas Transform 或 Canvas 组件缺失。";
                return false;
            }

            List<Transform> panelMatches = FindDirectChildren(canvas, WorldClockPanelName);
            if (panelMatches.Count != 1)
            {
                error = $"Canvas 下的 {WorldClockPanelName} 必须恰好存在一个，实际为 {panelMatches.Count}。";
                return false;
            }

            worldClockPanel = panelMatches[0] as RectTransform;
            if (worldClockPanel == null || worldClockPanel.GetComponent<WorldClockPanel>() == null ||
                !worldClockPanel.gameObject.activeInHierarchy)
            {
                error = "M1 WorldClockPanel 缺少活动的 RectTransform 或 WorldClockPanel 组件。";
                return false;
            }

            List<Transform> buttonMatches = FindDirectChildren(worldClockPanel, AdvanceButtonName);
            if (buttonMatches.Count != 1)
            {
                error = $"WorldClockPanel 下的 {AdvanceButtonName} 必须恰好存在一个，实际为 {buttonMatches.Count}。";
                return false;
            }

            advanceButton = buttonMatches[0] as RectTransform;
            Button button = advanceButton != null ? advanceButton.GetComponent<Button>() : null;
            if (advanceButton == null || button == null || !button.isActiveAndEnabled || !button.interactable)
            {
                error = "M1 AdvanceButton 缺少活动的 RectTransform 或可交互 Button。";
                return false;
            }
            return true;
        }

        /// <summary>根据 Canvas 与 M1 实际矩形计算下方可用的宽屏 M2 区域；不足时失败关闭。</summary>
        public static bool TryCalculateSafeArea(
            RectTransform canvas,
            IReadOnlyList<RectTransform> reservedRects,
            out Rect safeArea,
            out string error)
        {
            safeArea = default;
            error = null;
            if (canvas == null)
            {
                error = "Canvas RectTransform 缺失。";
                return false;
            }

            Rect canvasRect = canvas.rect;
            if (canvasRect.width <= 2f * HorizontalMargin || canvasRect.height <= BottomMargin + MinimumRootHeight)
            {
                error = $"Canvas 几何无效或太小：{canvasRect.width}x{canvasRect.height}。";
                return false;
            }
            if (reservedRects == null || reservedRects.Count == 0)
            {
                error = "未提供 M1 保留区域。";
                return false;
            }

            float m1LowestEdge = float.PositiveInfinity;
            foreach (RectTransform reservedRect in reservedRects)
            {
                if (reservedRect == null || !reservedRect.gameObject.activeInHierarchy)
                {
                    error = "M1 保留区域缺失或未激活。";
                    return false;
                }

                Rect bounds = GetCanvasLocalBounds(canvas, reservedRect);
                if (bounds.width <= 0f || bounds.height <= 0f)
                {
                    error = $"M1 对象 '{reservedRect.name}' bounds 无效（零宽或零高），拒绝使用未初始化矩形计算安全区。";
                    return false;
                }
                if (bounds.xMin < canvasRect.xMin - 0.1f || bounds.xMax > canvasRect.xMax + 0.1f ||
                    bounds.yMin < canvasRect.yMin - 0.1f || bounds.yMax > canvasRect.yMax + 0.1f)
                {
                    error = $"M1 对象 '{reservedRect.name}' 没有完全位于 Canvas 可见区域内。";
                    return false;
                }
                m1LowestEdge = Mathf.Min(m1LowestEdge, bounds.yMin);
            }

            float safeBottom = canvasRect.yMin + BottomMargin;
            float safeTop = m1LowestEdge - M1Clearance;
            float safeWidth = canvasRect.width - 2f * HorizontalMargin;
            float safeHeight = safeTop - safeBottom;
            if (safeWidth < MinimumRootWidth || safeHeight < MinimumRootHeight || safeTop > canvasRect.yMax)
            {
                error = $"M1 保留区下方空间不足：可用 M2 区域 {safeWidth}x{safeHeight}，最小要求 {MinimumRootWidth}x{MinimumRootHeight}。";
                return false;
            }

            safeArea = Rect.MinMaxRect(
                canvasRect.xMin + HorizontalMargin,
                safeBottom,
                canvasRect.xMax - HorizontalMargin,
                safeTop);
            return true;
        }

        /// <summary>校验应用后的 M2 根完整处于 Canvas 内，并与所有 M1 保留矩形保持最小间距。</summary>
        public static bool ValidateRootSeparation(
            RectTransform root,
            RectTransform canvas,
            IReadOnlyList<RectTransform> reservedRects)
        {
            if (root == null || canvas == null || reservedRects == null || reservedRects.Count == 0)
                return false;

            Rect canvasRect = canvas.rect;
            Rect rootBounds = GetCanvasLocalBounds(canvas, root);
            if (rootBounds.xMin < canvasRect.xMin - 0.1f || rootBounds.xMax > canvasRect.xMax + 0.1f ||
                rootBounds.yMin < canvasRect.yMin - 0.1f || rootBounds.yMax > canvasRect.yMax + 0.1f)
                return false;

            foreach (RectTransform reservedRect in reservedRects)
            {
                if (reservedRect == null) return false;
                Rect reservedBounds = GetCanvasLocalBounds(canvas, reservedRect);
                if (rootBounds.Overlaps(reservedBounds) ||
                    reservedBounds.yMin - rootBounds.yMax < M1Clearance - 0.1f)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// 按规格排布根下所有已知子对象；只触碰规格表中的对象，
        /// 规格表之外的未知子对象一律不动。
        /// </summary>
        public static void ApplyChildLayout(Transform root, Action<UnityEngine.Object, string> recordUndo = null, string undoName = null)
        {
            if (root == null) throw new ArgumentNullException(nameof(root));
            foreach (ChildSpec spec in ChildSpecs)
            {
                Transform child = FindChild(root, spec.Name);
                if (child == null)
                    throw new InvalidOperationException($"M2 子对象 '{spec.Name}' 缺失，无法应用布局。");

                RectTransform rt = child as RectTransform;
                if (rt == null)
                    throw new InvalidOperationException($"M2 子对象 '{spec.Name}' 不是 RectTransform。");

                recordUndo?.Invoke(rt, undoName);
                if (spec.StretchFill)
                {
                    rt.anchorMin = Vector2.zero;
                    rt.anchorMax = Vector2.one;
                    rt.offsetMin = new Vector2(12f, 12f);
                    rt.offsetMax = new Vector2(-12f, -12f);
                    rt.sizeDelta = Vector2.zero;
                    rt.anchoredPosition = Vector2.zero;
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.transform.localScale = Vector3.one;
                }
                else
                {
                    rt.anchorMin = new Vector2(0.5f, 0.5f);
                    rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = spec.AnchoredPos;
                    rt.sizeDelta = spec.SizeDelta;
                    rt.transform.localScale = Vector3.one;
                }

                // 文本字号（按钮子 Label 一并处理；仅触碰 M2 对象）
                Text text = rt.GetComponent<Text>();
                if (text != null)
                {
                    recordUndo?.Invoke(text, undoName);
                    text.fontSize = spec.FontSize;
                }
                else
                {
                    Button btn = rt.GetComponent<Button>();
                    if (btn != null)
                    {
                        Text label = btn.GetComponentInChildren<Text>(true);
                        if (label != null)
                        {
                            recordUndo?.Invoke(label, undoName);
                            label.fontSize = spec.FontSize;
                        }
                    }
                }
            }
        }

        /// <summary>校验：所有必需子对象存在且唯一，且均位于根之下。</summary>
        public static bool ResolveRequiredChildren(Transform root, out Dictionary<string, Transform> resolved, out string error)
        {
            resolved = new Dictionary<string, Transform>(StringComparer.Ordinal);
            error = null;
            if (root == null)
            {
                error = "根对象为空。";
                return false;
            }
            foreach (string name in RequiredChildNames)
            {
                List<Transform> matches = FindChildrenRecursive(root, name);
                if (matches.Count == 0)
                {
                    error = $"缺少必需子对象 '{name}'。";
                    return false;
                }
                if (matches.Count > 1)
                {
                    error = $"子对象 '{name}' 在根下出现 {matches.Count} 次，目标不唯一。";
                    return false;
                }
                resolved[name] = matches[0];
            }
            return true;
        }

        /// <summary>结构校验：全部必需子对象的世界矩形必须落在根矩形内（容差 2px）。</summary>
        public static bool ValidateContainment(Transform root)
        {
            return ValidateContainment(root, out _);
        }

        /// <summary>与 ValidateContainment 相同，并返回首个失败对象及其矩形，供生成器诊断。</summary>
        public static bool ValidateContainment(Transform root, out string error)
        {
            error = null;
            if (!ResolveRequiredChildren(root, out Dictionary<string, Transform> _, out string resolveError))
            {
                error = resolveError;
                return false;
            }

            RectTransform rootRt = root as RectTransform;
            if (rootRt == null)
            {
                error = "根对象不是 RectTransform。";
                return false;
            }
            Rect rootRect = WorldRect(rootRt);

            foreach (string name in RequiredChildNames)
            {
                Transform child = FindChild(root, name);
                RectTransform childRectTransform = child as RectTransform;
                if (childRectTransform == null)
                {
                    error = $"必需子对象 '{name}' 缺失或不是 RectTransform。";
                    return false;
                }
                Rect childRect = WorldRect(childRectTransform);
                const float tolerance = 2f;
                if (childRect.xMin < rootRect.xMin - tolerance ||
                    childRect.xMax > rootRect.xMax + tolerance ||
                    childRect.yMin < rootRect.yMin - tolerance ||
                    childRect.yMax > rootRect.yMax + tolerance)
                {
                    error = $"子对象 '{name}' bounds={childRect} 超出 root bounds={rootRect}。";
                    return false;
                }
            }
            return true;
        }

        // ---- 内部工具 ----

        /// <summary>将 RectTransform 的世界四角投影到 Canvas 局部空间，不假设 Canvas pivot/origin。</summary>
        public static Rect GetCanvasLocalBounds(RectTransform canvas, RectTransform target)
        {
            if (canvas == null) throw new ArgumentNullException(nameof(canvas));
            if (target == null) throw new ArgumentNullException(nameof(target));

            var corners = new Vector3[4];
            target.GetWorldCorners(corners);
            float xMin = float.PositiveInfinity;
            float xMax = float.NegativeInfinity;
            float yMin = float.PositiveInfinity;
            float yMax = float.NegativeInfinity;
            foreach (Vector3 worldCorner in corners)
            {
                Vector3 localCorner = canvas.InverseTransformPoint(worldCorner);
                xMin = Mathf.Min(xMin, localCorner.x);
                xMax = Mathf.Max(xMax, localCorner.x);
                yMin = Mathf.Min(yMin, localCorner.y);
                yMax = Mathf.Max(yMax, localCorner.y);
            }
            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }

        private static List<Transform> FindDirectChildren(Transform parent, string name)
        {
            var matches = new List<Transform>();
            foreach (Transform child in parent)
            {
                if (child.name == name)
                    matches.Add(child);
            }
            return matches;
        }

        private static Transform FindChild(Transform parent, string name)
        {
            if (parent == null) return null;
            foreach (Transform child in parent)
            {
                if (child.name == name) return child;
                Transform found = FindChild(child, name);
                if (found != null) return found;
            }
            return null;
        }

        private static List<Transform> FindChildrenRecursive(Transform parent, string name)
        {
            var list = new List<Transform>();
            if (parent == null) return list;
            foreach (Transform child in parent)
            {
                if (child.name == name) list.Add(child);
                list.AddRange(FindChildrenRecursive(child, name));
            }
            return list;
        }

        private static Rect WorldRect(RectTransform rt)
        {
            if (rt == null) return new Rect();
            Vector3[] corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            float xMin = Mathf.Min(corners[0].x, corners[2].x);
            float xMax = Mathf.Max(corners[0].x, corners[2].x);
            float yMin = Mathf.Min(corners[0].y, corners[2].y);
            float yMax = Mathf.Max(corners[0].y, corners[2].y);
            return new Rect(xMin, yMin, xMax - xMin, yMax - yMin);
        }
    }
}
