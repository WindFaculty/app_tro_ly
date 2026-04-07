#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AvatarSystem.Data;
using AvatarSystem.Validation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AvatarSystem.Editor
{
    /// <summary>
    /// Validates facial mesh binding, blink behavior, and expression coverage on the
    /// production base avatar prefab by running a short play mode probe in a temp scene.
    /// </summary>
    public static class BaseAvatarFacialValidator
    {
        private const string BaseAvatarPrefabPath = "Assets/AvatarSystem/AvatarProduction/BaseAvatar/Prefab/CHR_Avatar_Base_v001.prefab";
        private const string ExpressionPresetFolder = "Assets/AvatarSystem/AvatarProduction/Presets/ExpressionPresets";
        private const string TriggerRelativePath = "Temp/BaseAvatarFacialValidation.trigger";
        private const string ReportRelativePath = "Logs/BaseAvatarFacialValidation.json";
        private const string RunningSessionKey = "AvatarSystem.BaseAvatarFacialValidator.Running";
        private const string StartedAtSessionKey = "AvatarSystem.BaseAvatarFacialValidator.StartedAt";
        private const float ValidationTimeoutSeconds = 60f;

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

        private static double nextTriggerPollTime;

        [InitializeOnLoadMethod]
        private static void RegisterWatchers()
        {
            EditorApplication.update -= PollTriggerFile;
            EditorApplication.update += PollTriggerFile;
            EditorApplication.update -= PollValidationTimeout;
            EditorApplication.update += PollValidationTimeout;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        [MenuItem("Tools/AvatarSystem/Validate Base Avatar Facial %#f")]
        public static void ValidateBaseAvatarFacial()
        {
            if (SessionState.GetBool(RunningSessionKey, false))
            {
                Debug.LogWarning("[BaseAvatarFacialValidator] Validation is already running.");
                return;
            }

            DeleteExistingReport();

            if (!PrepareValidationScene(out string failureMessage))
            {
                CompleteWithReport(BaseAvatarFacialValidationReport.Failure(failureMessage));
                return;
            }

            SessionState.SetBool(RunningSessionKey, true);
            SessionState.SetFloat(StartedAtSessionKey, (float)EditorApplication.timeSinceStartup);
            EditorApplication.isPlaying = true;
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
                Debug.LogWarning($"[BaseAvatarFacialValidator] Could not delete trigger file: {exception.Message}");
            }

            ValidateBaseAvatarFacial();
        }

        private static void PollValidationTimeout()
        {
            if (!SessionState.GetBool(RunningSessionKey, false))
            {
                return;
            }

            float startedAt = SessionState.GetFloat(StartedAtSessionKey, 0f);
            if (EditorApplication.timeSinceStartup - startedAt <= ValidationTimeoutSeconds)
            {
                return;
            }

            CompleteWithReport(
                BaseAvatarFacialValidationReport.Failure(
                    $"Timed out after {ValidationTimeoutSeconds:F0}s while waiting for the facial runtime probe."
                )
            );
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange stateChange)
        {
            if (!SessionState.GetBool(RunningSessionKey, false))
            {
                return;
            }

            if (stateChange != PlayModeStateChange.EnteredEditMode)
            {
                return;
            }

            string reportPath = GetProjectRelativePath(ReportRelativePath);
            if (!File.Exists(reportPath))
            {
                CompleteWithReport(BaseAvatarFacialValidationReport.Failure("Runtime probe finished without writing a report."));
                return;
            }

            try
            {
                string json = File.ReadAllText(reportPath);
                var report = JsonUtility.FromJson<BaseAvatarFacialValidationReport>(json);
                if (report == null)
                {
                    CompleteWithReport(BaseAvatarFacialValidationReport.Failure("Could not deserialize the facial validation report."));
                    return;
                }

                CompleteWithReport(report);
            }
            catch (Exception exception)
            {
                CompleteWithReport(BaseAvatarFacialValidationReport.Failure($"Could not read facial validation report: {exception.Message}"));
            }
        }

        private static bool PrepareValidationScene(out string failureMessage)
        {
            failureMessage = string.Empty;

            AssetDatabase.ImportAsset(BaseAvatarPrefabPath, ImportAssetOptions.ForceUpdate);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BaseAvatarPrefabPath);
            if (prefab == null)
            {
                failureMessage = "Base avatar prefab could not be loaded.";
                return false;
            }

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance == null)
            {
                failureMessage = "Base avatar prefab could not be instantiated for validation.";
                return false;
            }

            instance.name = prefab.name;

            var rootController = instance.GetComponent<AvatarRootController>();
            var facialController = instance.GetComponent<AvatarFacialController>();
            if (rootController == null)
            {
                failureMessage = "AvatarRootController is missing from the base avatar prefab.";
                return false;
            }

            if (facialController == null)
            {
                failureMessage = "AvatarFacialController is missing from the base avatar prefab.";
                return false;
            }

            ConfigureFacialControllerForValidation(facialController);

            var probeObject = new GameObject("BaseAvatarFacialRuntimeProbe");
            var probe = probeObject.AddComponent<BaseAvatarFacialRuntimeProbe>();
            probe.avatarRoot = rootController;
            probe.facialController = facialController;
            probe.expressionDefinitions = LoadExpressionDefinitions();
            probe.lipSyncMap = BaseAvatarFacialPresetUtility.LoadLipSyncMap();
            probe.requiredEmotions = RequiredExpressionEmotions.ToArray();
            probe.reportPath = GetProjectRelativePath(ReportRelativePath);
            probe.blinkTimeoutSeconds = 3f;
            probe.expressionObservationSeconds = 5f;
            probe.neutralSettleSeconds = 0.2f;
            return true;
        }

        private static void ConfigureFacialControllerForValidation(AvatarFacialController facialController)
        {
            var serializedObject = new SerializedObject(facialController);
            serializedObject.FindProperty("blinkInterval").floatValue = 0.05f;
            serializedObject.FindProperty("blinkDuration").floatValue = 0.12f;
            serializedObject.FindProperty("blinkRandomRange").floatValue = 0f;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static ExpressionDefinition[] LoadExpressionDefinitions()
        {
            return BaseAvatarFacialPresetUtility.LoadOrderedExpressionDefinitions();
        }

        private static void CompleteWithReport(BaseAvatarFacialValidationReport report)
        {
            ClearSessionState();
            PersistReport(report);
            EmitLogs(report);

            if (Application.isBatchMode)
            {
                EditorApplication.Exit(report.success ? 0 : 1);
            }
        }

        private static void PersistReport(BaseAvatarFacialValidationReport report)
        {
            string reportPath = GetProjectRelativePath(ReportRelativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(reportPath) ?? string.Empty);
            File.WriteAllText(reportPath, JsonUtility.ToJson(report, true));
        }

        private static void DeleteExistingReport()
        {
            string reportPath = GetProjectRelativePath(ReportRelativePath);
            if (!File.Exists(reportPath))
            {
                return;
            }

            try
            {
                File.Delete(reportPath);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[BaseAvatarFacialValidator] Could not delete old report: {exception.Message}");
            }
        }

        private static void ClearSessionState()
        {
            SessionState.SetBool(RunningSessionKey, false);
            SessionState.SetFloat(StartedAtSessionKey, 0f);
        }

        private static void EmitLogs(BaseAvatarFacialValidationReport report)
        {
            Debug.Log(
                $"[BaseAvatarFacialValidator] binding={report.faceMeshBindingPassed}, blink={report.blinkTestPassed}, " +
                $"expressions={report.expressionCoveragePassed}, amplitudeLipSync={report.amplitudeLipSyncPassed}, " +
                $"layering={report.expressionLipSyncLayeringPassed}. Report: {ReportRelativePath}"
            );

            foreach (var result in report.expressionResults)
            {
                Debug.Log(
                    $"[BaseAvatarFacialValidator] Expression {result.emotion}: status={result.status}, definition={result.definitionName}, passed={result.passed}."
                );
            }

            foreach (string warning in report.warnings)
            {
                Debug.LogWarning($"[BaseAvatarFacialValidator] {warning}");
            }

            foreach (string error in report.errors)
            {
                Debug.LogError($"[BaseAvatarFacialValidator] {error}");
            }
        }

        private static string GetProjectRelativePath(string relativePath)
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
            return Path.Combine(projectRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
        }
    }
}
#endif
