using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Galileo.UI
{
    /// <summary>Reports pointer presses and drags on a UI element (the launch-window slider handles).</summary>
    public class DragHandle : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public event Action<PointerEventData> Dragged;
        public event Action Released;

        public void OnPointerDown(PointerEventData e) => Dragged?.Invoke(e);
        public void OnDrag(PointerEventData e) => Dragged?.Invoke(e);
        public void OnPointerUp(PointerEventData e) => Released?.Invoke();
    }
}
