using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Galileo.UI
{
    /// <summary>Raises <see cref="Pressed"/> as soon as the pointer goes down (for quick targets).</summary>
    public class PressCatcher : MonoBehaviour, IPointerDownHandler
    {
        public event Action Pressed;
        public void OnPointerDown(PointerEventData eventData) => Pressed?.Invoke();
    }
}
