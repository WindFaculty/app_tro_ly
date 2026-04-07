using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace TroLy.Editor
{
    public static class RuntimeUiSettingsBootstrap
    {
        private const string SettingsFolderPath = "Assets/Resources/UI/Settings";
        private const string ThemeStyleSheetPath = SettingsFolderPath + "/RuntimeTheme.tss";
        private const string PanelSettingsPath = SettingsFolderPath + "/RuntimePanelSettings.asset";
        private const string PanelTextSettingsPath = SettingsFolderPath + "/RuntimePanelTextSettings.asset";
        private const string FontAssetPath = SettingsFolderPath + "/RuntimeDefaultFont.asset";

        [InitializeOnLoadMethod]
        private static void EnsureRuntimeUiSettingsAssets()
        {
            EnsureRuntimeUiSettings(verbose: false);
        }

        [MenuItem("Tools/TroLy/Generate Runtime UI Settings")]
        public static void GenerateRuntimeUiSettings()
        {
            EnsureRuntimeUiSettings(verbose: true);
        }

        private static void EnsureRuntimeUiSettings(bool verbose)
        {
            EnsureFolderExists(SettingsFolderPath);
            AssetDatabase.ImportAsset(ThemeStyleSheetPath, ImportAssetOptions.ForceSynchronousImport);

            var themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(ThemeStyleSheetPath);
            if (themeStyleSheet == null)
            {
                if (verbose)
                {
                    Debug.LogError($"[Runtime UI Settings] Failed to load theme style sheet at '{ThemeStyleSheetPath}'.");
                }
                return;
            }

            var panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
            if (panelSettings == null)
            {
                panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
                AssetDatabase.CreateAsset(panelSettings, PanelSettingsPath);
            }

            var panelTextSettings = AssetDatabase.LoadAssetAtPath<PanelTextSettings>(PanelTextSettingsPath);
            if (panelTextSettings == null)
            {
                panelTextSettings = ScriptableObject.CreateInstance<PanelTextSettings>();
                AssetDatabase.CreateAsset(panelTextSettings, PanelTextSettingsPath);
            }

            var fontAsset = AssetDatabase.LoadAssetAtPath<FontAsset>(FontAssetPath);
            if (!IsFontAssetUsable(fontAsset))
            {
                if (fontAsset != null)
                {
                    AssetDatabase.DeleteAsset(FontAssetPath);
                }

                fontAsset = CreateRuntimeFontAsset();
                if (fontAsset == null)
                {
                    if (verbose)
                    {
                        Debug.LogError("[Runtime UI Settings] Failed to create a runtime default FontAsset.");
                    }
                    return;
                }

                AssetDatabase.CreateAsset(fontAsset, FontAssetPath);
            }

            panelSettings.clearColor = false;
            panelSettings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panelSettings.referenceResolution = new Vector2Int(1920, 1080);
            panelSettings.match = 0.5f;
            panelSettings.themeStyleSheet = themeStyleSheet;
            panelSettings.textSettings = panelTextSettings;
            panelTextSettings.defaultFontAsset = fontAsset;

            EditorUtility.SetDirty(panelSettings);
            EditorUtility.SetDirty(panelTextSettings);
            EditorUtility.SetDirty(fontAsset);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            if (verbose)
            {
                Debug.Log($"[Runtime UI Settings] Generated '{PanelSettingsPath}', '{PanelTextSettingsPath}', and '{FontAssetPath}' using '{ThemeStyleSheetPath}'.");
            }
        }

        [MenuItem("Tools/TroLy/Log Runtime UI Diagnostics")]
        public static void LogRuntimeUiDiagnostics()
        {
            var documents = Resources.FindObjectsOfTypeAll<UIDocument>();
            Debug.Log($"[Runtime UI Diagnostics] Found {documents.Length} UIDocument instance(s).");
            foreach (var document in documents)
            {
                if (document == null)
                {
                    continue;
                }

                var root = document.rootVisualElement;
                var textSettings = document.panelSettings != null ? document.panelSettings.textSettings : null;
                var defaultFont = textSettings != null ? textSettings.defaultFontAsset : null;
                Debug.Log(
                    $"[Runtime UI Diagnostics] Document='{document.name}' GameObject='{document.gameObject.name}' " +
                    $"PanelSettings='{document.panelSettings?.name}' TextSettings='{textSettings?.name}' DefaultFont='{defaultFont?.name}' RootChildren='{root?.childCount}'.");

                if (root == null)
                {
                    continue;
                }

                var labels = root.Query<Label>().ToList();
                Debug.Log($"[Runtime UI Diagnostics] Document '{document.name}' label count: {labels.Count}.");
                for (var index = 0; index < Mathf.Min(labels.Count, 8); index++)
                {
                    var label = labels[index];
                    Debug.Log($"[Runtime UI Diagnostics] Label {index}: name='{label.name}' text='{label.text}' display='{label.resolvedStyle.display}' color='{label.resolvedStyle.color}'.");
                }
            }
        }

        private static FontAsset CreateRuntimeFontAsset()
        {
            Font sourceFont = null;
            try
            {
                sourceFont = Font.CreateDynamicFontFromOSFont(new[] { "Segoe UI", "Arial", "Tahoma" }, 16);
            }
            catch
            {
                sourceFont = null;
            }

            sourceFont ??= Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            sourceFont ??= Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (sourceFont == null)
            {
                return null;
            }

            var asset = FontAsset.CreateFontAsset(
                sourceFont,
                90,
                9,
                GlyphRenderMode.SDFAA,
                1024,
                1024,
                AtlasPopulationMode.Dynamic,
                true);
            asset.name = "RuntimeDefaultFont";
            return asset;
        }

        private static bool IsFontAssetUsable(FontAsset fontAsset)
        {
            return fontAsset != null
                && fontAsset.sourceFontFile != null
                && fontAsset.atlasTextures != null
                && fontAsset.atlasTextures.Length > 0
                && fontAsset.atlasTextures[0] != null;
        }

        private static void EnsureFolderExists(string folderPath)
        {
            var normalized = folderPath.Replace('\\', '/');
            if (AssetDatabase.IsValidFolder(normalized))
            {
                return;
            }

            var absolutePath = Path.Combine(Directory.GetCurrentDirectory(), normalized);
            Directory.CreateDirectory(absolutePath);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }
    }
}
