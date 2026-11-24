using System;
using UnityEngine;

namespace SengokuChronicle.Presentation
{
    /// <summary>
    /// 日本全境地图的视口几何切片。它只操作绑定的 <c>mapContent</c> RectTransform，
    /// 不读取坐标表、底图或旅行状态，也不缩放目录、详情和按钮。
    /// <para>
    /// 坐标约定：内容的锚点、轴心与尺寸由调用方决定，视口只改写内容的
    /// <c>localScale</c> 与 <c>localPosition</c>。全境倍率 1x 表示内容按自身
    /// 宽高比完整装进视口；1x–6x 之外的值被夹回。内容小于视口的轴居中，
    /// 大于视口的轴不得露出视口内部空白。
    /// </para>
    /// <para>
    /// 空间前提：<c>mapContent</c> 必须是 <c>viewport</c> 的直接子级，且
    /// <c>localRotation</c> 为单位旋转（未旋转）。这样视口局部坐标与内容
    /// 局部坐标只差平移与统一缩放，屏幕到内容的换算才成立。非直接子级或
    /// 旋转内容会在 <see cref="Bind"/> 时抛出 <see cref="ArgumentException"/>。
    /// 视口自身可以处在任意层级或带旋转，屏幕坐标由 RectTransformUtility
    /// 先换算到视口局部坐标。
    /// </para>
    /// <para>
    /// 本切片不处理 uGUI 拖动阈值、节点点击或 Input System 事件；屏幕集成
    /// 层负责把指针事件换算成 <see cref="ZoomAt"/> 与 <see cref="PanBy"/> 的参数。
    /// 视口尺寸变化后调用 <see cref="Refresh"/> 即可按当前尺寸重新适配，
    /// 不依赖帧回调，结果只由当前矩形与倍率决定。
    /// </para>
    /// </summary>
    public sealed class JapanMapViewport
    {
        public const float MinimumZoom = 1f;
        public const float MaximumZoom = 6f;

        private const float ZoomStep = 1.2f;
        private const float Epsilon = 0.0001f;
        private const float RotationTolerance = 0.01f;

        private RectTransform _viewport;
        private RectTransform _mapContent;
        private Vector2 _contentSize;
        private Vector2 _contentPosition;
        private float _zoom = MinimumZoom;

        /// <summary>当前倍率，1x 为全境适配，范围 1x–6x。</summary>
        public float Zoom => _zoom;

        public bool IsBound => _viewport != null && _mapContent != null;

        /// <summary>
        /// 绑定遮罩视口与地图内容。内容必须是视口的直接子级，且不得旋转
        /// （localRotation 为单位旋转）；否则抛出 <see cref="ArgumentException"/>。
        /// 内容的当前矩形是固定的地图几何尺寸，之后视口尺寸只决定裁切与适配，
        /// 不再改变该尺寸。绑定会复位到 1x 全境并居中。
        /// </summary>
        public void Bind(RectTransform viewport, RectTransform mapContent)
        {
            if (viewport == null)
                throw new ArgumentNullException(nameof(viewport));
            if (mapContent == null)
                throw new ArgumentNullException(nameof(mapContent));
            if (mapContent.parent != viewport)
                throw new ArgumentException(
                    "地图内容必须是视口的直接子级，否则视口局部坐标与内容局部坐标不一致。",
                    nameof(mapContent));
            if (Quaternion.Angle(mapContent.localRotation, Quaternion.identity) > RotationTolerance)
                throw new ArgumentException(
                    "地图内容不得旋转；本组件只支持与视口轴对齐的内容。",
                    nameof(mapContent));

            Vector2 contentSize = mapContent.rect.size;
            if (contentSize.x <= Epsilon || contentSize.y <= Epsilon)
                throw new ArgumentException("地图内容必须先有正的宽高才能绑定。", nameof(mapContent));

            _viewport = viewport;
            _mapContent = mapContent;
            _contentSize = contentSize;
            _zoom = MinimumZoom;
            _contentPosition = Vector2.zero;
            ResetToFullExtent();
        }

        /// <summary>复位到 1x 全境适配并居中。视口尺寸变化后也用它重建全境视图。</summary>
        public void ResetToFullExtent()
        {
            EnsureBound();
            _zoom = MinimumZoom;
            _contentPosition = CenteredPosition(CurrentScale());
            ApplyTransform();
        }

        /// <summary>
        /// 以屏幕坐标处的指针为锚点缩放。scrollDelta 以滚轮格为单位，只取
        /// [-1, 1] 的幅度，因此一帧内收到 1 或 120 都只走一格。
        /// </summary>
        public void ZoomAt(Vector2 screenPosition, float scrollDelta)
        {
            EnsureBound();

            float oldScale = CurrentScale();
            _contentPosition = ClampToViewport(_contentPosition, oldScale);

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _viewport, screenPosition, ResolveCamera(), out Vector2 pointer))
            {
                ApplyTransform();
                return;
            }

            float step = Mathf.Clamp(scrollDelta, -1f, 1f);
            float targetZoom = Mathf.Clamp(
                _zoom * Mathf.Pow(ZoomStep, step), MinimumZoom, MaximumZoom);

            if (Mathf.Abs(targetZoom - _zoom) <= 1e-6f)
            {
                ApplyTransform();
                return;
            }

            // 指针下的内容局部点：p = position + q * scale，缩放后保持 q 落在同一指针下。
            Vector2 anchor = (pointer - _contentPosition) / oldScale;
            _zoom = targetZoom;
            _contentPosition = pointer - anchor * CurrentScale();
            ApplyTransform();
        }

        /// <summary>
        /// 按屏幕像素增量平移地图内容。非 1:1 画布缩放会先换算成视口局部单位，
        /// 然后按边界夹取。
        /// </summary>
        public void PanBy(Vector2 screenDelta)
        {
            EnsureBound();

            float scale = CurrentScale();
            _contentPosition = ClampToViewport(_contentPosition, scale);
            _contentPosition += ScreenDeltaToLocal(screenDelta);
            ApplyTransform();
        }

        /// <summary>
        /// 按当前视口矩形重新计算适配与边界。视口尺寸变化后由调用方显式调用；
        /// 重复调用不会改变倍率，结果确定。
        /// </summary>
        public void Refresh()
        {
            if (!IsBound)
                return;
            ApplyTransform();
        }

        private float CurrentScale()
        {
            Vector2 viewportSize = _viewport.rect.size;
            float fit = Mathf.Min(
                viewportSize.x / _contentSize.x,
                viewportSize.y / _contentSize.y);
            return Mathf.Max(fit, Epsilon) * _zoom;
        }

        private void ApplyTransform()
        {
            float scale = CurrentScale();
            _contentPosition = ClampToViewport(_contentPosition, scale);

            Vector3 contentScale = _mapContent.localScale;
            _mapContent.localScale = new Vector3(scale, scale, contentScale.z);

            Vector3 contentPosition = _mapContent.localPosition;
            _mapContent.localPosition = new Vector3(
                _contentPosition.x, _contentPosition.y, contentPosition.z);
        }

        private Vector2 CenteredPosition(float scale)
        {
            Vector2 viewportCenter = _viewport.rect.center;
            Vector2 scaledSize = _contentSize * scale;
            Vector2 pivot = _mapContent.pivot;
            return new Vector2(
                viewportCenter.x + (pivot.x - 0.5f) * scaledSize.x,
                viewportCenter.y + (pivot.y - 0.5f) * scaledSize.y);
        }

        private Vector2 ClampToViewport(Vector2 position, float scale)
        {
            Rect viewportRect = _viewport.rect;
            Vector2 scaledSize = _contentSize * scale;
            Vector2 pivot = _mapContent.pivot;
            return new Vector2(
                ClampAxis(position.x, viewportRect.xMin, viewportRect.xMax, scaledSize.x, pivot.x),
                ClampAxis(position.y, viewportRect.yMin, viewportRect.yMax, scaledSize.y, pivot.y));
        }

        private static float ClampAxis(
            float position, float viewportMin, float viewportMax, float contentSize, float pivot)
        {
            float viewportSize = viewportMax - viewportMin;
            float viewportCenter = (viewportMin + viewportMax) * 0.5f;
            if (contentSize <= viewportSize)
                return viewportCenter + (pivot - 0.5f) * contentSize;

            float minimum = viewportMax - (1f - pivot) * contentSize;
            float maximum = viewportMin + pivot * contentSize;
            return Mathf.Clamp(position, minimum, maximum);
        }

        private Vector2 ScreenDeltaToLocal(Vector2 screenDelta)
        {
            Camera camera = ResolveCamera();
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _viewport, Vector2.zero, camera, out Vector2 from) &&
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _viewport, screenDelta, camera, out Vector2 to))
            {
                return to - from;
            }

            return Vector2.zero;
        }

        private Camera ResolveCamera()
        {
            Canvas canvas = _viewport.GetComponentInParent<Canvas>();
            if (canvas == null)
                return null;
            Canvas root = canvas.rootCanvas;
            return root.renderMode == RenderMode.ScreenSpaceOverlay ? null : root.worldCamera;
        }

        private void EnsureBound()
        {
            if (!IsBound)
                throw new InvalidOperationException("JapanMapViewport 必须先 Bind 视口与地图内容。");
        }
    }
}
