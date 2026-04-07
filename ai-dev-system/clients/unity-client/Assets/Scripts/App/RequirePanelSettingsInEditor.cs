using UnityEngine;
using UnityEngine.UIElements;

namespace LocalAssistant.App
{
    [ExecuteAlways]
    [RequireComponent(typeof(UIDocument))]
    public class RequirePanelSettingsInEditor : MonoBehaviour
    {
        private const string PanelSettingsResourcePath = "UI/Settings/RuntimePanelSettings";

        private void OnEnable()
        {
            if (Application.isPlaying) return;

            var document = GetComponent<UIDocument>();
            if (document != null && document.panelSettings == null)
            {
                var panelSettings = Resources.Load<PanelSettings>(PanelSettingsResourcePath);
                if (panelSettings != null)
                {
                    document.panelSettings = panelSettings;
                    Debug.Log($"[RequirePanelSettingsInEditor] Auto-assigned PanelSettings to {gameObject.name} for UI previewing.");
                }
            }
        }
    }
}
