using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.TextCore.Text;
using LocalAssistant.Features.Schedule;

namespace LocalAssistant.Core
{
    public static class UiDocumentLoader
    {
        private const string PanelSettingsResourcePath = "UI/Settings/RuntimePanelSettings";
        private const string ThemeStyleSheetResourcePath = "UI/Settings/RuntimeTheme";
        private static readonly string[] RuntimeFontNames = { "Segoe UI", "Arial", "Tahoma" };
        private static FontAsset runtimeFontAsset;
        private static Font runtimeUnityFont;

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
            refs.Document.panelSettings = LoadPanelSettings();

            var root = refs.Document.rootVisualElement;
            ApplyRuntimeFont(root);
            refs.Shell = new AppShellRefs(root);
            refs.Home = new HomeScreenRefs(root);
            refs.Schedule = new ScheduleScreenRefs(root);
            refs.Settings = new SettingsScreenRefs(root);
            refs.Chat = new ChatPanelRefs(root);
            refs.Subtitle = new SubtitleOverlayRefs(root);
            refs.Reminder = new ReminderOverlayRefs(root);
            return refs;
        }

        private static PanelSettings LoadPanelSettings()
        {
            var panelSettings = Resources.Load<PanelSettings>(PanelSettingsResourcePath);
            if (panelSettings != null)
            {
                var runtimePanelSettings = Object.Instantiate(panelSettings);
                runtimePanelSettings.name = panelSettings.name + "_Runtime";
                if (panelSettings.textSettings != null)
                {
                    runtimePanelSettings.textSettings = CloneTextSettings(panelSettings.textSettings);
                }

                EnsureTextSettings(runtimePanelSettings);
                return runtimePanelSettings;
            }

            Debug.LogWarning(
                $"Runtime PanelSettings asset '{PanelSettingsResourcePath}' could not be loaded. Falling back to a runtime-created PanelSettings instance."
            );

            var fallback = ScriptableObject.CreateInstance<PanelSettings>();
            fallback.clearColor = false;
            fallback.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            fallback.referenceResolution = new Vector2Int(1920, 1080);
            fallback.match = 0.5f;

            var fallbackTheme = Resources.Load<ThemeStyleSheet>(ThemeStyleSheetResourcePath);
            if (fallbackTheme != null)
            {
                fallback.themeStyleSheet = fallbackTheme;
            }

            EnsureTextSettings(fallback);
            return fallback;
        }

        private static void EnsureTextSettings(PanelSettings panelSettings)
        {
            if (panelSettings == null)
            {
                return;
            }

            if (panelSettings.textSettings != null)
            {
                ValidateTextSettings(panelSettings.textSettings, panelSettings.name);
                EnsureDefaultFontAsset(panelSettings.textSettings);
                return;
            }

            var runtimeTextSettings = CloneTextSettings(null);
            panelSettings.textSettings = runtimeTextSettings;
            EnsureDefaultFontAsset(runtimeTextSettings);

            Debug.LogWarning(
                $"Runtime PanelSettings '{panelSettings.name}' was missing PanelTextSettings. A runtime fallback instance was created so labels can render."
            );
        }

        private static void EnsureDefaultFontAsset(PanelTextSettings textSettings)
        {
            if (textSettings == null)
            {
                return;
            }

            if (!IsFontAssetUsable(runtimeFontAsset))
            {
                runtimeFontAsset = CreateRuntimeFontAsset();
            }
            if (runtimeFontAsset == null)
            {
                Debug.LogError("UI Toolkit runtime text fallback could not create a default FontAsset. Labels may not render.");
                return;
            }

            if (textSettings.defaultFontAsset == runtimeFontAsset)
            {
                return;
            }

            textSettings.defaultFontAsset = runtimeFontAsset;
        }

        private static void ValidateTextSettings(PanelTextSettings textSettings, string ownerName)
        {
            if (textSettings == null)
            {
                return;
            }

            if (IsFontAssetUsable(textSettings.defaultFontAsset))
            {
                return;
            }

            var fontName = textSettings.defaultFontAsset != null ? textSettings.defaultFontAsset.name : "(null)";
            Debug.LogWarning(
                $"UI Toolkit text settings '{textSettings.name}' on '{ownerName}' referenced an invalid default FontAsset '{fontName}'. " +
                "A runtime fallback font will be regenerated."
            );
        }

        private static PanelTextSettings CloneTextSettings(PanelTextSettings source)
        {
            var runtimeTextSettings = source != null
                ? Object.Instantiate(source)
                : ScriptableObject.CreateInstance<PanelTextSettings>();
            runtimeTextSettings.hideFlags = HideFlags.DontSave;
            runtimeTextSettings.name = source != null ? source.name + "_Runtime" : "RuntimePanelTextSettings";
            return runtimeTextSettings;
        }

        private static void ApplyRuntimeFont(VisualElement root)
        {
            if (root == null)
            {
                return;
            }

            if (!IsFontAssetUsable(runtimeFontAsset))
            {
                runtimeFontAsset = CreateRuntimeFontAsset();
            }

            runtimeUnityFont ??= runtimeFontAsset != null && runtimeFontAsset.sourceFontFile != null
                ? runtimeFontAsset.sourceFontFile
                : LoadRuntimeUnityFont();
            if (runtimeUnityFont == null)
            {
                Debug.LogWarning("UI Toolkit runtime fallback could not load a Unity Font for the root visual tree.");
                return;
            }

            root.style.unityFontDefinition = FontDefinition.FromFont(runtimeUnityFont);
        }

        private static Font LoadRuntimeUnityFont()
        {
            try
            {
                var osFont = Font.CreateDynamicFontFromOSFont(RuntimeFontNames, 16);
                if (osFont != null)
                {
                    return osFont;
                }
            }
            catch
            {
                // Ignore and fall back to built-in runtime fonts.
            }

            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        private static FontAsset CreateRuntimeFontAsset()
        {
            Font sourceFont = null;
            try
            {
                sourceFont = Font.CreateDynamicFontFromOSFont(RuntimeFontNames, 16);
            }
            catch
            {
                sourceFont = null;
            }

            sourceFont ??= Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (sourceFont == null)
            {
                return null;
            }

            var fontAsset = FontAsset.CreateFontAsset(
                sourceFont,
                90,
                9,
                GlyphRenderMode.SDFAA,
                1024,
                1024,
                AtlasPopulationMode.Dynamic,
                true);
            fontAsset.hideFlags = HideFlags.DontSave;
            fontAsset.name = sourceFont.name + "_UITK_Runtime";
            if (!IsFontAssetUsable(fontAsset))
            {
                Debug.LogWarning($"Generated runtime FontAsset '{fontAsset.name}' is missing atlas textures. Labels may require fallback Unity fonts.");
            }
            return fontAsset;
        }

        private static bool IsFontAssetUsable(FontAsset fontAsset)
        {
            return fontAsset != null
                && fontAsset.sourceFontFile != null
                && fontAsset.atlasTextures != null
                && fontAsset.atlasTextures.Length > 0
                && fontAsset.atlasTextures[0] != null;
        }
    }
}
