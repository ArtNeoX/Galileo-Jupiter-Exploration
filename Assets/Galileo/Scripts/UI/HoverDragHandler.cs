using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Galileo.UI
{
    /// <summary>Reports hover and drag events of a UI element (the pros/cons cards).</summary>
    public class HoverDragHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public Action Entered, Exited;
        public Action<PointerEventData> DragStarted, Dragged, DragEnded;

        public void OnPointerEnter(PointerEventData e) => Entered?.Invoke();
        public void OnPointerExit(PointerEventData e) => Exited?.Invoke();
        public void OnBeginDrag(PointerEventData e) => DragStarted?.Invoke(e);
        public void OnDrag(PointerEventData e) => Dragged?.Invoke(e);
        public void OnEndDrag(PointerEventData e) => DragEnded?.Invoke(e);
    }
}
