#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AvatarSystem.Data;
using UnityEditor;
using UnityEngine;

namespace AvatarSystem.Editor
{
    /// <summary>
    /// Creates the prototype facial preset assets used by the production avatar and
    /// syncs those assets onto the base avatar prefab.
    /// </summary>
    public static class BaseAvatarFacialPresetUtility
    {
        public const string BaseAvatarPrefabPath = "Assets/AvatarSystem/AvatarProduction/BaseAvatar/Prefab/CHR_Avatar_Base_v001.prefab";
        public const string ExpressionPresetFolder = "Assets/AvatarSystem/AvatarProduction/Presets/ExpressionPresets";
        public const string LipSyncMapPath = "Assets/AvatarSystem/AvatarProduction/Data/LipSyncMaps/LipSyncMap_Prototype_Default.asset";

        private static readonly EmotionType[] RequiredExpressionEmotions =
        {
            EmotionType.Happy,
            EmotionType.Sad,
            EmotionType.Surprised,
            EmotionType.Focused,
            EmotionType.SoftSmile,
            EmotionType.Curious,
            EmotionType.Apologetic,
        };

        [MenuItem("Tools/AvatarSystem/Generate Prototype Facial Presets")]
        public static void GeneratePrototypeFacialPresets()
        {
            EnsureFoldersExist();

            foreach (var spec in BuildExpressionSpecs())
            {
                UpsertExpressionAsset(spec);
            }

            UpsertLipSyncMapAsset();
            SyncBaseAvatarPrefabReferences();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[BaseAvatarFacialPresetUtility] Generated prototype facial presets and synced the base avatar prefab.");
        }

        public static ExpressionDefinition[] LoadOrderedExpressionDefinitions()
        {
            var allDefinitions = AssetDatabase.FindAssets("t:ExpressionDefinition", new[] { ExpressionPresetFolder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(path => AssetDatabase.LoadAssetAtPath<ExpressionDefinition>(path))
                .Where(definition => definition != null)
                .GroupBy(definition => definition.emotionType)
                .ToDictionary(group => group.Key, group => group.First());

            var ordered = new List<ExpressionDefinition>();
            foreach (EmotionType emotion in RequiredExpressionEmotions)
            {
                if (allDefinitions.TryGetValue(emotion, out var definition))
                {
                    ordered.Add(definition);
                }
            }

            return ordered.ToArray();
        }

        public static LipSyncMapDefinition LoadLipSyncMap()
        {
            var direct = AssetDatabase.LoadAssetAtPath<LipSyncMapDefinition>(LipSyncMapPath);
            if (direct != null)
            {
                return direct;
            }

            string guid = AssetDatabase.FindAssets("t:LipSyncMapDefinition", new[] { Path.GetDirectoryName(LipSyncMapPath)?.Replace('\\', '/') ?? string.Empty })
                .FirstOrDefault();

            return string.IsNullOrEmpty(guid)
                ? null
                : AssetDatabase.LoadAssetAtPath<LipSyncMapDefinition>(AssetDatabase.GUIDToAssetPath(guid));
        }

        public static void ApplyExistingAssetReferences(AvatarConversationBridge conversationBridge, AvatarLipSyncDriver lipSyncDriver)
        {
            if (conversationBridge != null)
            {
                var serializedConversationBridge = new SerializedObject(conversationBridge);
                var expressionLibraryProperty = serializedConversationBridge.FindProperty("expressionLibrary");
                var definitions = LoadOrderedExpressionDefinitions();
                expressionLibraryProperty.arraySize = definitions.Length;
                for (int i = 0; i < definitions.Length; i++)
                {
                    expressionLibraryProperty.GetArrayElementAtIndex(i).objectReferenceValue = definitions[i];
                }

                serializedConversationBridge.ApplyModifiedPropertiesWithoutUndo();
            }

            if (lipSyncDriver != null)
            {
                var serializedLipSyncDriver = new SerializedObject(lipSyncDriver);
                serializedLipSyncDriver.FindProperty("lipSyncMap").objectReferenceValue = LoadLipSyncMap();
                serializedLipSyncDriver.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        public static void SyncBaseAvatarPrefabReferences()
        {
            var prefabRoot = PrefabUtility.LoadPrefabContents(BaseAvatarPrefabPath);
            try
            {
                ApplyExistingAssetReferences(
                    prefabRoot.GetComponent<AvatarConversationBridge>(),
                    prefabRoot.GetComponent<AvatarLipSyncDriver>()
                );

                PrefabUtility.SaveAsPrefabAsset(prefabRoot, BaseAvatarPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        private static void EnsureFoldersExist()
        {
            Directory.CreateDirectory(GetProjectRelativePath(ExpressionPresetFolder));
            string lipSyncFolder = Path.GetDirectoryName(LipSyncMapPath)?.Replace('\\', '/') ?? string.Empty;
            if (!string.IsNullOrEmpty(lipSyncFolder))
            {
                Directory.CreateDirectory(GetProjectRelativePath(lipSyncFolder));
            }
        }

        private static void UpsertExpressionAsset(ExpressionSpec spec)
        {
            string assetPath = $"{ExpressionPresetFolder}/{spec.fileName}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<ExpressionDefinition>(assetPath);
            bool created = false;
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<ExpressionDefinition>();
                AssetDatabase.CreateAsset(asset, assetPath);
                created = true;
            }

            asset.emotionType = spec.emotionType;
            asset.displayName = spec.displayName;
            asset.priority = spec.priority;
            asset.transitionSpeed = spec.transitionSpeed;
            asset.targets = spec.targets;

            EditorUtility.SetDirty(asset);
            if (created)
            {
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            }
        }

        private static void UpsertLipSyncMapAsset()
        {
            var asset = AssetDatabase.LoadAssetAtPath<LipSyncMapDefinition>(LipSyncMapPath);
            bool created = false;
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<LipSyncMapDefinition>();
                AssetDatabase.CreateAsset(asset, LipSyncMapPath);
                created = true;
            }

            asset.mappings = new[]
            {
                CreateVisemeMapping(VisemeType.Rest, Target("Viseme_Rest", 100f)),
                CreateVisemeMapping(VisemeType.AA, Target("Viseme_AA", 100f), Target("MouthOpen", 60f)),
                CreateVisemeMapping(VisemeType.E, Target("Viseme_E", 100f)),
                CreateVisemeMapping(VisemeType.I, Target("Viseme_I", 100f)),
                CreateVisemeMapping(VisemeType.O, Target("Viseme_O", 100f)),
                CreateVisemeMapping(VisemeType.U, Target("Viseme_U", 100f)),
                CreateVisemeMapping(VisemeType.FV, Target("Viseme_FV", 100f)),
                CreateVisemeMapping(VisemeType.L, Target("Viseme_L", 100f)),
                CreateVisemeMapping(VisemeType.MBP, Target("Viseme_MBP", 100f)),
            };

            EditorUtility.SetDirty(asset);
            if (created)
            {
                AssetDatabase.ImportAsset(LipSyncMapPath, ImportAssetOptions.ForceUpdate);
            }
        }

        private static ExpressionSpec[] BuildExpressionSpecs()
        {
            return new[]
            {
                new ExpressionSpec(
                    "EXP_Happy_Prototype",
                    EmotionType.Happy,
                    "Happy (Prototype)",
                    10,
                    8f,
                    Target("Smile", 78f),
                    Target("SmileEye_L", 32f),
                    Target("SmileEye_R", 32f),
                    Target("MouthWide", 18f)
                ),
                new ExpressionSpec(
                    "EXP_Sad_Prototype",
                    EmotionType.Sad,
                    "Sad (Prototype)",
                    10,
                    7f,
                    Target("Sad", 70f),
                    Target("BrowInnerUp", 40f),
                    Target("BrowDown_L", 18f),
                    Target("BrowDown_R", 18f)
                ),
                new ExpressionSpec(
                    "EXP_Surprised_Prototype",
                    EmotionType.Surprised,
                    "Surprised (Prototype)",
                    10,
                    8f,
                    Target("Surprise", 78f),
                    Target("WideEye_L", 55f),
                    Target("WideEye_R", 55f),
                    Target("BrowUp_L", 42f),
                    Target("BrowUp_R", 42f),
                    Target("BrowInnerUp", 25f)
                ),
                new ExpressionSpec(
                    "EXP_Focused_Prototype",
                    EmotionType.Focused,
                    "Focused (Prototype)",
                    10,
                    7f,
                    Target("BrowDown_L", 34f),
                    Target("BrowDown_R", 34f),
                    Target("MouthNarrow", 12f)
                ),
                new ExpressionSpec(
                    "EXP_SoftSmile_Prototype",
                    EmotionType.SoftSmile,
                    "Soft Smile (Prototype)",
                    10,
                    8f,
                    Target("Smile", 38f),
                    Target("SmileEye_L", 18f),
                    Target("SmileEye_R", 18f)
                ),
                new ExpressionSpec(
                    "EXP_Curious_Prototype",
                    EmotionType.Curious,
                    "Curious (Prototype)",
                    10,
                    7f,
                    Target("BrowUp_L", 22f),
                    Target("BrowUp_R", 22f),
                    Target("BrowInnerUp", 20f),
                    Target("MouthRound", 10f)
                ),
                new ExpressionSpec(
                    "EXP_Apologetic_Prototype",
                    EmotionType.Apologetic,
                    "Apologetic (Prototype)",
                    10,
                    7f,
                    Target("Sad", 34f),
                    Target("BrowInnerUp", 34f),
                    Target("BrowDown_L", 10f),
                    Target("BrowDown_R", 10f),
                    Target("MouthNarrow", 14f)
                ),
            };
        }

        private static VisemeMapping CreateVisemeMapping(VisemeType viseme, params BlendShapeTarget[] targets)
        {
            return new VisemeMapping
            {
                viseme = viseme,
                targets = targets,
            };
        }

        private static BlendShapeTarget Target(string blendShapeName, float weight)
        {
            return new BlendShapeTarget
            {
                blendShapeName = blendShapeName,
                weight = weight,
            };
        }

        private static string GetProjectRelativePath(string relativePath)
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
            return Path.Combine(projectRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
        }

        private readonly struct ExpressionSpec
        {
            public ExpressionSpec(
                string fileName,
                EmotionType emotionType,
                string displayName,
                int priority,
                float transitionSpeed,
                params BlendShapeTarget[] targets)
            {
                this.fileName = fileName;
                this.emotionType = emotionType;
                this.displayName = displayName;
                this.priority = priority;
                this.transitionSpeed = transitionSpeed;
                this.targets = targets ?? Array.Empty<BlendShapeTarget>();
            }

            public readonly string fileName;
            public readonly EmotionType emotionType;
            public readonly string displayName;
            public readonly int priority;
            public readonly float transitionSpeed;
            public readonly BlendShapeTarget[] targets;
        }
    }
}
#endif
