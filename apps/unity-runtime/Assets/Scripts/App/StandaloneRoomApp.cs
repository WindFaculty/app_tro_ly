using LocalAssistant.Runtime;
using UnityEngine;

namespace LocalAssistant.App
{
    public sealed class StandaloneRoomApp : MonoBehaviour
    {
        public StandaloneRoomComposition Composition { get; private set; }

        private void Awake()
        {
            Composition ??= StandaloneRoomCompositionRoot.Compose(gameObject, transform);
        }
    }
}
