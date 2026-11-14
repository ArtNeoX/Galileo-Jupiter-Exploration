using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Galileo.UI
{
    /// <summary>Raises <see cref="Clicked"/> for any click on this object or a child that doesn't handle it.</summary>
    public class ClickCatcher : MonoBehaviour, IPointerClickHandler
    {
        public event Action Clicked;
        public void OnPointerClick(PointerEventData eventData) => Clicked?.Invoke();
    }
}
