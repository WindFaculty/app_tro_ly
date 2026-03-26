using UnityEngine;
using UnityEngine.UIElements;

namespace LocalAssistant.Core
{
    public static class UiDocumentLoader
    {
        public static AssistantUiRefs Load(Transform parent)
        {
            var refs = new AssistantUiRefs();

            var canvasRoot = new GameObject("AssistantUI_Toolkit", typeof(UIDocument));
            canvasRoot.transform.SetParent(parent, false);
            refs.Document = canvasRoot.GetComponent<UIDocument>();

            var visualTree = Resources.Load<VisualTreeAsset>("UI/MainUI");
            if (visualTree == null)
            {
                Debug.LogError("Failed to load UI/MainUI.uxml from Resources");
                return refs;
            }

            refs.Document.visualTreeAsset = visualTree;

            var panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
            panelSettings.clearColor = false;
            panelSettings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panelSettings.referenceResolution = new Vector2Int(1920, 1080);
            panelSettings.match = 0.5f;
            refs.Document.panelSettings = panelSettings;

            var root = refs.Document.rootVisualElement;
            refs.Shell = new AppShellRefs(root);
            refs.Home = new HomeScreenRefs(root);
            refs.Schedule = new ScheduleScreenRefs(root);
            refs.Settings = new SettingsScreenRefs(root);
            refs.Chat = new ChatPanelRefs(root);
            refs.Subtitle = new SubtitleOverlayRefs(root);
            refs.Reminder = new ReminderOverlayRefs(root);
            return refs;
        }
    }
}
