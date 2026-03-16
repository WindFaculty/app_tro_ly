#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AvatarSystem.Data;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AvatarSystem.Editor
{
    /// <summary>
    /// Builds the production base avatar prefab from the final FBX import.
    /// Access via menu: Tools > AvatarSystem > Build Base Avatar Prefab
    /// </summary>
    public static class BaseAvatarPrefabBuilder
    {
        private const string BaseAvatarModelPath = "Assets/AvatarSystem/AvatarProduction/BaseAvatar/Model/CHR_Avatar_Base_v001.fbx";
        private const string BaseAvatarPrefabPath = "Assets/AvatarSystem/AvatarProduction/BaseAvatar/Prefab/CHR_Avatar_Base_v001.prefab";
        private const string AnimatorControllerPath = "Assets/AvatarSystem/AvatarProduction/BaseAvatar/Animator/CHR_Avatar_Base_v001_Base.controller";
        private const string IdleClipPath = "Assets/AvatarSystem/AvatarProduction/BaseAvatar/Animations/Idle_Default_Placeholder.anim";
        private const string TriggerRelativePath = "Temp/BaseAvatarPrefabBuild.trigger";
        private const string ReportRelativePath = "Logs/BaseAvatarPrefabBuildReport.json";

        private static readonly (BodyRegion region, string rendererName)[] RegionRendererMap =
        {
            (BodyRegion.Head, "Body_Head"),
            (BodyRegion.TorsoUpper, "Body_TorsoUpper"),
            (BodyRegion.TorsoLower, "Body_TorsoLower"),
            (BodyRegion.ArmUpperL, "Body_ArmUpperL"),
            (BodyRegion.ArmUpperR, "Body_ArmUpperR"),
            (BodyRegion.ForearmL, "Body_ForearmL"),
            (BodyRegion.ForearmR, "Body_ForearmR"),
            (BodyRegion.HandL, "Body_HandL"),
            (BodyRegion.HandR, "Body_HandR"),
            (BodyRegion.ThighL, "Body_ThighL"),
            (BodyRegion.ThighR, "Body_ThighR"),
            (BodyRegion.CalfL, "Body_CalfL"),
            (BodyRegion.CalfR, "Body_CalfR"),
            (BodyRegion.FootL, "Body_FootL"),
            (BodyRegion.FootR, "Body_FootR"),
        };

        private static readonly (string name, AnimatorControllerParameterType type)[] AnimatorParameters =
        {
            ("IsListening", AnimatorControllerParameterType.Bool),
            ("IsThinking", AnimatorControllerParameterType.Bool),
            ("IsSpeaking", AnimatorControllerParameterType.Bool),
            ("IsMoving", AnimatorControllerParameterType.Bool),
            ("MoveSpeed", AnimatorControllerParameterType.Float),
            ("TurnAngle", AnimatorControllerParameterType.Float),
            ("GestureIndex", AnimatorControllerParameterType.Int),
            ("EmotionIndex", AnimatorControllerParameterType.Int),
        };

        private static double nextTriggerPollTime;

        [InitializeOnLoadMethod]
        private static void RegisterTriggerWatcher()
        {
            EditorApplication.update -= PollTriggerFile;
            EditorApplication.update += PollTriggerFile;
        }

        [MenuItem("Tools/AvatarSystem/Build Base Avatar Prefab %#b")]
        public static void BuildBaseAvatarPrefab()
        {
            var report = BuildPrefabInternal();
            PersistReport(report);
            EmitLogs(report);
        }

        private static void PollTriggerFile()
        {
            if (EditorApplication.timeSinceStartup < nextTriggerPollTime)
            {
                return;
            }

            nextTriggerPollTime = EditorApplication.timeSinceStartup + 1d;

            string triggerPath = GetProjectRelativePath(TriggerRelativePath);
            if (!File.Exists(triggerPath))
            {
                return;
            }

            try
            {
                File.Delete(triggerPath);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[BaseAvatarPrefabBuilder] Could not delete trigger file: {exception.Message}");
            }

            BuildBaseAvatarPrefab();
        }

        private static BuildReport BuildPrefabInternal()
        {
            AssetDatabase.ImportAsset(BaseAvatarModelPath, ImportAssetOptions.ForceUpdate);

            var importer = AssetImporter.GetAtPath(BaseAvatarModelPath) as ModelImporter;
            if (importer == null)
            {
                return BuildReport.Failure("ModelImporter not found for final base avatar FBX.");
            }

            bool importerChanged = false;
            if (importer.animationType != ModelImporterAnimationType.Human)
            {
                importer.animationType = ModelImporterAnimationType.Human;
                importerChanged = true;
            }

            if (importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel)
            {
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                importerChanged = true;
            }

            if (!importer.autoGenerateAvatarMappingIfUnspecified)
            {
                importer.autoGenerateAvatarMappingIfUnspecified = true;
                importerChanged = true;
            }

            if (!importer.importBlendShapes)
            {
                importer.importBlendShapes = true;
                importerChanged = true;
            }

            if (!importer.preserveHierarchy)
            {
                importer.preserveHierarchy = true;
                importerChanged = true;
            }

            if (importerChanged)
            {
                importer.SaveAndReimport();
            }

            var modelRoot = AssetDatabase.LoadAssetAtPath<GameObject>(BaseAvatarModelPath);
            if (modelRoot == null)
            {
                return BuildReport.Failure("Could not load final base avatar model prefab.");
            }

            var avatar = LoadAvatarSubAsset(BaseAvatarModelPath);
            if (avatar == null || !avatar.isValid || !avatar.isHuman)
            {
                return BuildReport.Failure("Imported avatar is missing or invalid after reimport.");
            }

            Directory.CreateDirectory(Path.GetDirectoryName(GetProjectRelativePath(BaseAvatarPrefabPath)) ?? string.Empty);
            Directory.CreateDirectory(Path.GetDirectoryName(GetProjectRelativePath(AnimatorControllerPath)) ?? string.Empty);
            Directory.CreateDirectory(Path.GetDirectoryName(GetProjectRelativePath(IdleClipPath)) ?? string.Empty);

            AnimationClip idleClip = EnsureIdleClip();
            AnimatorController controller = EnsureAnimatorController(idleClip);

            Scene previewScene = EditorSceneManager.NewPreviewScene();
            GameObject instance = null;

            try
            {
                instance = PrefabUtility.InstantiatePrefab(modelRoot, previewScene) as GameObject;
                if (instance == null)
                {
                    return BuildReport.Failure("Failed to instantiate final base avatar model in preview scene.");
                }

                instance.name = Path.GetFileNameWithoutExtension(BaseAvatarPrefabPath);

                var renderers = instance.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                    .GroupBy(renderer => renderer.name)
                    .ToDictionary(group => group.Key, group => group.First());

                if (!renderers.TryGetValue("Body_Head", out var faceRenderer))
                {
                    return BuildReport.Failure("Body_Head renderer not found on imported model.");
                }

                if (!renderers.TryGetValue("Body_TorsoUpper", out var bodyRenderer))
                {
                    bodyRenderer = renderers.Values.FirstOrDefault(renderer => renderer.name != "Body_Head");
                }

                if (bodyRenderer == null)
                {
                    return BuildReport.Failure("Could not determine a body mesh renderer for AvatarRootController.");
                }

                var rootController = GetOrAddComponent<AvatarRootController>(instance);
                var equipmentManager = GetOrAddComponent<AvatarEquipmentManager>(instance);
                var bodyVisibilityManager = GetOrAddComponent<AvatarBodyVisibilityManager>(instance);
                var facialController = GetOrAddComponent<AvatarFacialController>(instance);
                var lipSyncDriver = GetOrAddComponent<AvatarLipSyncDriver>(instance);
                var animatorBridge = GetOrAddComponent<AvatarAnimatorBridge>(instance);
                var conversationBridge = GetOrAddComponent<AvatarConversationBridge>(instance);
                var locomotionController = GetOrAddComponent<AvatarLocomotionController>(instance);
                var lookAtController = GetOrAddComponent<AvatarLookAtController>(instance);
                var presetManager = GetOrAddComponent<AvatarPresetManager>(instance);
                var animator = GetOrAddComponent<Animator>(instance);

                animator.avatar = avatar;
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.updateMode = AnimatorUpdateMode.Normal;

                SetRootControllerReferences(
                    rootController,
                    equipmentManager,
                    bodyVisibilityManager,
                    facialController,
                    lipSyncDriver,
                    animatorBridge,
                    conversationBridge,
                    locomotionController,
                    lookAtController,
                    presetManager,
                    animator,
                    bodyRenderer,
                    faceRenderer,
                    instance.transform
                );

                SetPresetManagerReference(presetManager, equipmentManager);
                SetBodyRegionMappings(bodyVisibilityManager, renderers);
                SetLookAtReferences(lookAtController, instance.transform);

                PrefabUtility.SaveAsPrefabAsset(instance, BaseAvatarPrefabPath, out bool prefabSavedSuccessfully);
                if (!prefabSavedSuccessfully)
                {
                    return BuildReport.Failure("PrefabUtility.SaveAsPrefabAsset returned false.");
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.ImportAsset(BaseAvatarPrefabPath, ImportAssetOptions.ForceUpdate);

                return ValidateBuiltPrefab(avatar, controller, idleClip);
            }
            finally
            {
                if (instance != null)
                {
                    UnityEngine.Object.DestroyImmediate(instance);
                }

                EditorSceneManager.ClosePreviewScene(previewScene);
            }
        }

        private static AnimationClip EnsureIdleClip()
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(IdleClipPath);
            if (clip != null)
            {
                return clip;
            }

            clip = new AnimationClip
            {
                name = "Idle_Default_Placeholder"
            };

            AssetDatabase.CreateAsset(clip, IdleClipPath);
            AssetDatabase.ImportAsset(IdleClipPath, ImportAssetOptions.ForceUpdate);
            return AssetDatabase.LoadAssetAtPath<AnimationClip>(IdleClipPath);
        }

        private static AnimatorController EnsureAnimatorController(AnimationClip idleClip)
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(AnimatorControllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(AnimatorControllerPath);
            }

            foreach (var (name, type) in AnimatorParameters)
            {
                if (!controller.parameters.Any(parameter => parameter.name == name))
                {
                    controller.AddParameter(name, type);
                }
            }

            var stateMachine = controller.layers[0].stateMachine;
            var idleState = stateMachine.states
                .Select(state => state.state)
                .FirstOrDefault(state => state != null && state.name == "Idle_Default");

            if (idleState == null)
            {
                idleState = stateMachine.AddState("Idle_Default");
            }

            idleState.motion = idleClip;
            stateMachine.defaultState = idleState;

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static BuildReport ValidateBuiltPrefab(Avatar avatar, AnimatorController controller, AnimationClip idleClip)
        {
            var prefabRoot = PrefabUtility.LoadPrefabContents(BaseAvatarPrefabPath);
            try
            {
                var report = new BuildReport
                {
                    builtAtUtc = DateTime.UtcNow.ToString("O"),
                    modelAssetPath = BaseAvatarModelPath,
                    prefabAssetPath = BaseAvatarPrefabPath,
                    animatorControllerPath = AnimatorControllerPath,
                    idleClipPath = IdleClipPath,
                    avatarName = avatar.name,
                    avatarIsValid = avatar.isValid,
                    avatarIsHuman = avatar.isHuman,
                    animatorControllerName = controller != null ? controller.name : string.Empty,
                    idleClipName = idleClip != null ? idleClip.name : string.Empty,
                };

                var rootController = prefabRoot.GetComponent<AvatarRootController>();
                var bodyVisibilityManager = prefabRoot.GetComponent<AvatarBodyVisibilityManager>();
                var animator = prefabRoot.GetComponent<Animator>();
                var renderers = prefabRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                var faceRenderer = renderers.FirstOrDefault(renderer => renderer.name == "Body_Head");

                report.missingScriptCount = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(prefabRoot);
                report.rendererNames = renderers.Select(renderer => renderer.name).OrderBy(name => name).ToArray();
                report.rendererCount = report.rendererNames.Length;
                report.faceRendererName = faceRenderer != null ? faceRenderer.name : string.Empty;
                report.bodyRegionMappingCount = GetBodyRegionMappingCount(bodyVisibilityManager);
                report.bodyRegionMappingsComplete = HasCompleteBodyRegionMappings(bodyVisibilityManager);
                report.hasAvatarRootController = rootController != null;
                report.hasAnimator = animator != null;
                report.hasValidRuntimeController = animator != null && animator.runtimeAnimatorController == controller;
                report.hasValidFaceRenderer = faceRenderer != null;
                report.hasFaceBlendShapes = faceRenderer != null && faceRenderer.sharedMesh != null && faceRenderer.sharedMesh.blendShapeCount >= 28;
                report.faceBlendShapeNames = faceRenderer != null && faceRenderer.sharedMesh != null
                    ? Enumerable.Range(0, faceRenderer.sharedMesh.blendShapeCount).Select(faceRenderer.sharedMesh.GetBlendShapeName).ToArray()
                    : Array.Empty<string>();
                report.requiredFaceBlendShapesPresent = RequiredFaceBlendshapeNames().All(name => report.faceBlendShapeNames.Contains(name));

                bool controllerHasIdleDefault = controller != null &&
                    controller.layers.Length > 0 &&
                    controller.layers[0].stateMachine.defaultState != null &&
                    controller.layers[0].stateMachine.defaultState.name == "Idle_Default" &&
                    controller.layers[0].stateMachine.defaultState.motion == idleClip;

                report.idleSmokeTestPassed =
                    report.avatarIsValid &&
                    report.avatarIsHuman &&
                    report.missingScriptCount == 0 &&
                    report.hasAvatarRootController &&
                    report.hasAnimator &&
                    report.hasValidRuntimeController &&
                    report.hasValidFaceRenderer &&
                    report.hasFaceBlendShapes &&
                    report.requiredFaceBlendShapesPresent &&
                    report.bodyRegionMappingCount == RegionRendererMap.Length &&
                    report.bodyRegionMappingsComplete &&
                    controllerHasIdleDefault;

                report.success = report.idleSmokeTestPassed;

                if (!report.idleSmokeTestPassed)
                {
                    if (!report.hasAvatarRootController) report.errors.Add("AvatarRootController missing on prefab root.");
                    if (!report.hasAnimator) report.errors.Add("Animator missing on prefab root.");
                    if (!report.hasValidRuntimeController) report.errors.Add("Animator controller missing or not assigned.");
                    if (!report.hasValidFaceRenderer) report.errors.Add("Body_Head face renderer missing.");
                    if (!report.hasFaceBlendShapes) report.errors.Add("Body_Head does not expose imported blendshapes.");
                    if (!report.requiredFaceBlendShapesPresent) report.errors.Add("Required face blendshape names are incomplete.");
                    if (report.bodyRegionMappingCount != RegionRendererMap.Length) report.errors.Add("Body region mapping count is incomplete.");
                    if (!report.bodyRegionMappingsComplete) report.errors.Add("One or more body region mappings are missing renderer references.");
                    if (!controllerHasIdleDefault) report.errors.Add("Idle_Default placeholder state or clip is missing.");
                    if (report.missingScriptCount > 0) report.errors.Add($"Prefab has {report.missingScriptCount} missing scripts.");
                }

                return report;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        private static void SetRootControllerReferences(
            AvatarRootController rootController,
            AvatarEquipmentManager equipmentManager,
            AvatarBodyVisibilityManager bodyVisibilityManager,
            AvatarFacialController facialController,
            AvatarLipSyncDriver lipSyncDriver,
            AvatarAnimatorBridge animatorBridge,
            AvatarConversationBridge conversationBridge,
            AvatarLocomotionController locomotionController,
            AvatarLookAtController lookAtController,
            AvatarPresetManager presetManager,
            Animator animator,
            SkinnedMeshRenderer bodyRenderer,
            SkinnedMeshRenderer faceRenderer,
            Transform avatarRoot)
        {
            var serializedObject = new SerializedObject(rootController);
            serializedObject.FindProperty("equipmentManager").objectReferenceValue = equipmentManager;
            serializedObject.FindProperty("bodyVisibilityManager").objectReferenceValue = bodyVisibilityManager;
            serializedObject.FindProperty("facialController").objectReferenceValue = facialController;
            serializedObject.FindProperty("lipSyncDriver").objectReferenceValue = lipSyncDriver;
            serializedObject.FindProperty("animatorBridge").objectReferenceValue = animatorBridge;
            serializedObject.FindProperty("conversationBridge").objectReferenceValue = conversationBridge;
            serializedObject.FindProperty("locomotionController").objectReferenceValue = locomotionController;
            serializedObject.FindProperty("lookAtController").objectReferenceValue = lookAtController;
            serializedObject.FindProperty("presetManager").objectReferenceValue = presetManager;
            serializedObject.FindProperty("avatarAnimator").objectReferenceValue = animator;
            serializedObject.FindProperty("bodyMesh").objectReferenceValue = bodyRenderer;
            serializedObject.FindProperty("faceMesh").objectReferenceValue = faceRenderer;
            serializedObject.FindProperty("avatarRoot").objectReferenceValue = avatarRoot;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetPresetManagerReference(AvatarPresetManager presetManager, AvatarEquipmentManager equipmentManager)
        {
            var serializedObject = new SerializedObject(presetManager);
            serializedObject.FindProperty("equipmentManager").objectReferenceValue = equipmentManager;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetBodyRegionMappings(
            AvatarBodyVisibilityManager bodyVisibilityManager,
            IReadOnlyDictionary<string, SkinnedMeshRenderer> renderers)
        {
            var serializedObject = new SerializedObject(bodyVisibilityManager);
            SerializedProperty mappingsProperty = serializedObject.FindProperty("regionMappings");
            mappingsProperty.arraySize = RegionRendererMap.Length;

            for (int i = 0; i < RegionRendererMap.Length; i++)
            {
                SerializedProperty element = mappingsProperty.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("region").enumValueIndex = (int)RegionRendererMap[i].region;

                SerializedProperty renderersProperty = element.FindPropertyRelative("renderers");
                renderersProperty.arraySize = 1;

                renderers.TryGetValue(RegionRendererMap[i].rendererName, out var renderer);
                renderersProperty.GetArrayElementAtIndex(0).objectReferenceValue = renderer;
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetLookAtReferences(AvatarLookAtController lookAtController, Transform root)
        {
            var serializedObject = new SerializedObject(lookAtController);
            serializedObject.FindProperty("headBone").objectReferenceValue = FindDescendant(root, "Head");
            serializedObject.FindProperty("neckBone").objectReferenceValue = FindDescendant(root, "Neck");
            serializedObject.FindProperty("eyeLeftBone").objectReferenceValue = null;
            serializedObject.FindProperty("eyeRightBone").objectReferenceValue = null;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static int GetBodyRegionMappingCount(AvatarBodyVisibilityManager bodyVisibilityManager)
        {
            if (bodyVisibilityManager == null)
            {
                return 0;
            }

            var serializedObject = new SerializedObject(bodyVisibilityManager);
            return serializedObject.FindProperty("regionMappings").arraySize;
        }

        private static bool HasCompleteBodyRegionMappings(AvatarBodyVisibilityManager bodyVisibilityManager)
        {
            if (bodyVisibilityManager == null)
            {
                return false;
            }

            var serializedObject = new SerializedObject(bodyVisibilityManager);
            SerializedProperty mappings = serializedObject.FindProperty("regionMappings");
            if (mappings.arraySize != RegionRendererMap.Length)
            {
                return false;
            }

            for (int i = 0; i < mappings.arraySize; i++)
            {
                SerializedProperty renderers = mappings.GetArrayElementAtIndex(i).FindPropertyRelative("renderers");
                if (renderers.arraySize == 0)
                {
                    return false;
                }

                bool hasRenderer = false;
                for (int j = 0; j < renderers.arraySize; j++)
                {
                    if (renderers.GetArrayElementAtIndex(j).objectReferenceValue != null)
                    {
                        hasRenderer = true;
                        break;
                    }
                }

                if (!hasRenderer)
                {
                    return false;
                }
            }

            return true;
        }

        private static IEnumerable<string> RequiredFaceBlendshapeNames()
        {
            yield return "Blink_L";
            yield return "Blink_R";
            yield return "MouthOpen";
            yield return "Viseme_AA";
        }

        private static T GetOrAddComponent<T>(GameObject target) where T : Component
        {
            var component = target.GetComponent<T>();
            return component != null ? component : target.AddComponent<T>();
        }

        private static Transform FindDescendant(Transform root, string name)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == name)
                {
                    return child;
                }
            }

            return null;
        }

        private static Avatar LoadAvatarSubAsset(string assetPath)
        {
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(assetPath))
            {
                if (asset is Avatar avatar)
                {
                    return avatar;
                }
            }

            return null;
        }

        private static void PersistReport(BuildReport report)
        {
            string reportPath = GetProjectRelativePath(ReportRelativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(reportPath) ?? string.Empty);
            File.WriteAllText(reportPath, JsonUtility.ToJson(report, true));
        }

        private static void EmitLogs(BuildReport report)
        {
            if (report.success)
            {
                Debug.Log(
                    $"[BaseAvatarPrefabBuilder] Built prefab '{report.prefabAssetPath}' with {report.rendererCount} renderers, " +
                    $"bodyRegionMappings={report.bodyRegionMappingCount}, idleSmokeTestPassed={report.idleSmokeTestPassed}. " +
                    $"Report: {ReportRelativePath}"
                );
                return;
            }

            foreach (string error in report.errors)
            {
                Debug.LogError($"[BaseAvatarPrefabBuilder] {error}");
            }

            Debug.LogError($"[BaseAvatarPrefabBuilder] Build failed. Report: {ReportRelativePath}");
        }

        private static string GetProjectRelativePath(string relativePath)
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
            return Path.Combine(projectRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
        }

        [Serializable]
        private sealed class BuildReport
        {
            public string builtAtUtc = string.Empty;
            public string modelAssetPath = BaseAvatarModelPath;
            public string prefabAssetPath = BaseAvatarPrefabPath;
            public string animatorControllerPath = AnimatorControllerPath;
            public string idleClipPath = IdleClipPath;
            public string avatarName = string.Empty;
            public bool avatarIsValid;
            public bool avatarIsHuman;
            public string animatorControllerName = string.Empty;
            public string idleClipName = string.Empty;
            public bool hasAvatarRootController;
            public bool hasAnimator;
            public bool hasValidRuntimeController;
            public bool hasValidFaceRenderer;
            public bool hasFaceBlendShapes;
            public bool requiredFaceBlendShapesPresent;
            public bool idleSmokeTestPassed;
            public bool success;
            public int rendererCount;
            public int bodyRegionMappingCount;
            public bool bodyRegionMappingsComplete;
            public int missingScriptCount;
            public string faceRendererName = string.Empty;
            public string[] rendererNames = Array.Empty<string>();
            public string[] faceBlendShapeNames = Array.Empty<string>();
            public List<string> errors = new();

            public static BuildReport Failure(string message)
            {
                return new BuildReport
                {
                    builtAtUtc = DateTime.UtcNow.ToString("O"),
                    success = false,
                    idleSmokeTestPassed = false,
                    errors = new List<string> { message },
                };
            }
        }
    }
}
#endif
