using System;
using SengokuChronicle.Domain;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SengokuChronicle.Presentation
{
    /// <summary>
    /// 单张家族名册卡片的指针接收面。它位于卡片的键盘按钮之上，
    /// 因此在卡片上起手的拖拽会滚动图谱，而不是选中人物：只有指针抬起时
    /// 仍停在 <see cref="DragThreshold"/> 范围内才算点击选中，
    /// 拖拽事件则转发给图谱的 <see cref="ScrollRect"/>。选中始终保持只读。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FamilyRosterCardInteraction : MonoBehaviour, IPointerDownHandler,
        IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public const float DragThreshold = 8f;

        private ScrollRect _scroll;
        private PersonId _personId;
        private Action<PersonId> _select;
        private Vector2 _pressPosition;
        private bool _pressed;

        public PersonId PersonId => _personId;

        public void Bind(ScrollRect scroll, PersonId personId, Action<PersonId> select)
        {
            _scroll = scroll;
            _personId = personId;
            _select = select;
            _pressed = false;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _pressPosition = eventData.position;
            _pressed = true;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            bool pressed = _pressed;
            _pressed = false;
            if (!pressed)
                return;
            if ((eventData.position - _pressPosition).sqrMagnitude >
                DragThreshold * DragThreshold)
                return;
            _select?.Invoke(_personId);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _pressed = false;
            if (_scroll != null)
                _scroll.OnBeginDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_scroll != null)
                _scroll.OnDrag(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (_scroll != null)
                _scroll.OnEndDrag(eventData);
        }
    }
}
