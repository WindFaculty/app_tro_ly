using System;
using UnityEngine;

namespace LocalAssistant.Runtime
{
    public sealed class InteractionRuntime : MonoBehaviour
    {
        private Transform fallbackRoot;

        public string LastFocusedObject { get; private set; } = string.Empty;
        public event Action<string> ObjectFocused;

        public void Bind(Transform root)
        {
            fallbackRoot = root;
        }

        public bool FocusObject(string objectName)
        {
            if (string.IsNullOrWhiteSpace(objectName))
            {
                return false;
            }

            var target = GameObject.Find(objectName);
            if (target == null && fallbackRoot != null)
            {
                var fallback = fallbackRoot.Find(objectName);
                if (fallback != null)
                {
                    target = fallback.gameObject;
                }
            }

            if (target == null)
            {
                Debug.LogWarning($"[InteractionRuntime] Could not find target object '{objectName}'.");
                return false;
            }

            LastFocusedObject = target.name;
            ObjectFocused?.Invoke(target.name);
            return true;
        }
    }
}
