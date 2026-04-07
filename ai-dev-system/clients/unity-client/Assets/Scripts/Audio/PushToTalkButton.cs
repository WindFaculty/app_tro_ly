using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace LocalAssistant.Audio
{
    public sealed class PushToTalkButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public event Action Pressed;
        public event Action Released;

        public void OnPointerDown(PointerEventData eventData)
        {
            Pressed?.Invoke();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            Released?.Invoke();
        }
    }
}
