using System;
using SengokuChronicle.Domain;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SengokuChronicle.Presentation
{
    /// <summary>
    /// Pointer surface for one family roster card. It sits above the card's keyboard
    /// button so a drag that starts on a card scrolls the graph instead of selecting a
    /// person: a click only selects when the pointer stayed within
    /// <see cref="DragThreshold"/> of where it went down, and drag events are forwarded
    /// to the graph's <see cref="ScrollRect"/>. Selection stays read-only.
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
