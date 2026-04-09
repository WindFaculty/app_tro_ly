#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AvatarSystem.Editor
{
    /// <summary>
    /// Validates the imported Humanoid avatar for the rig-clean base model.
    /// Access via menu: Tools > AvatarSystem > Validate Base Avatar Humanoid
    /// </summary>
    public static class BaseAvatarHumanoidValidator
    {
        private const string BaseAvatarPath = "Assets/AvatarSystem/AvatarProduction/BaseAvatar/Model/CHR_Avatar_Base_v001_rigclean.fbx";
        private const string TriggerRelativePath = "Temp/BaseAvatarHumanoidValidation.trigger";
        private const string ReportRelativePath = "Logs/BaseAvatarHumanoidValidation.json";
        private static double nextTriggerPollTime;

        private static readonly HumanBodyBones[] RequiredBones =
        {
            HumanBodyBones.Hips,
            HumanBodyBones.Spine,
            HumanBodyBones.Head,
            HumanBodyBones.LeftUpperLeg,
            HumanBodyBones.LeftLowerLeg,
            HumanBodyBones.LeftFoot,
            HumanBodyBones.RightUpperLeg,
            HumanBodyBones.RightLowerLeg,
            HumanBodyBones.RightFoot,
            HumanBodyBones.LeftUpperArm,
            HumanBodyBones.LeftLowerArm,
            HumanBodyBones.LeftHand,
            HumanBodyBones.RightUpperArm,
            HumanBodyBones.RightLowerArm,
            HumanBodyBones.RightHand,
        };

        private static readonly (HumanBodyBones bone, string expectedName)[] ExpectedMappings =
        {
            (HumanBodyBones.Hips, "Hips"),
            (HumanBodyBones.Spine, "Spine"),
            (HumanBodyBones.Chest, "Spine01"),
            (HumanBodyBones.UpperChest, "Spine02"),
            (HumanBodyBones.Neck, "Neck"),
            (HumanBodyBones.Head, "Head"),
            (HumanBodyBones.LeftShoulder, "LeftShoulder"),
            (HumanBodyBones.LeftUpperArm, "LeftArm"),
            (HumanBodyBones.LeftLowerArm, "LeftForeArm"),
            (HumanBodyBones.LeftHand, "LeftHand"),
            (HumanBodyBones.RightShoulder, "RightShoulder"),
            (HumanBodyBones.RightUpperArm, "RightArm"),
            (HumanBodyBones.RightLowerArm, "RightForeArm"),
            (HumanBodyBones.RightHand, "RightHand"),
            (HumanBodyBones.LeftUpperLeg, "LeftUpLeg"),
            (HumanBodyBones.LeftLowerLeg, "LeftLeg"),
            (HumanBodyBones.LeftFoot, "LeftFoot"),
            (HumanBodyBones.LeftToes, "LeftToeBase"),
            (HumanBodyBones.RightUpperLeg, "RightUpLeg"),
            (HumanBodyBones.RightLowerLeg, "RightLeg"),
            (HumanBodyBones.RightFoot, "RightFoot"),
            (HumanBodyBones.RightToes, "RightToeBase"),
        };

        [InitializeOnLoadMethod]
        private static void RegisterTriggerWatcher()
        {
            EditorApplication.update -= PollTriggerFile;
            EditorApplication.update += PollTriggerFile;
        }

        [MenuItem("Tools/AvatarSystem/Validate Base Avatar Humanoid %#h")]
        public static void ValidateBaseAvatarHumanoid()
        {
            var report = BuildReport();
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
                Debug.LogWarning($"[BaseAvatarHumanoidValidator] Could not delete trigger file: {exception.Message}");
            }

            ValidateBaseAvatarHumanoid();
        }

        private static ValidationReport BuildReport()
        {
            AssetDatabase.ImportAsset(BaseAvatarPath, ImportAssetOptions.ForceUpdate);

            var importer = AssetImporter.GetAtPath(BaseAvatarPath) as ModelImporter;
            if (importer == null)
            {
                return ValidationReport.Failure(BaseAvatarPath, "ModelImporter not found.");
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

            if (importerChanged)
            {
                importer.SaveAndReimport();
                importer = AssetImporter.GetAtPath(BaseAvatarPath) as ModelImporter;
                if (importer == null)
                {
                    return ValidationReport.Failure(BaseAvatarPath, "ModelImporter disappeared after SaveAndReimport.");
                }
            }

            var avatar = LoadAvatarSubAsset(BaseAvatarPath);
            if (avatar == null)
            {
                return ValidationReport.Failure(
                    BaseAvatarPath,
                    $"No Avatar sub-asset found. animationType={importer.animationType}, avatarSetup={importer.avatarSetup}"
                );
            }

            if (!avatar.isValid)
            {
                return ValidationReport.Failure(BaseAvatarPath, $"Avatar is invalid: {avatar.name}");
            }

            if (!avatar.isHuman)
            {
                return ValidationReport.Failure(BaseAvatarPath, $"Avatar is valid but not Humanoid: {avatar.name}");
            }

            var root = AssetDatabase.LoadAssetAtPath<GameObject>(BaseAvatarPath);
            if (root == null)
            {
                return ValidationReport.Failure(BaseAvatarPath, "Could not load model GameObject.");
            }

            var instance = PrefabUtility.InstantiatePrefab(root) as GameObject;
            if (instance == null)
            {
                return ValidationReport.Failure(BaseAvatarPath, "Failed to instantiate model prefab.");
            }

            try
            {
                var animator = instance.GetComponent<Animator>();
                if (animator == null)
                {
                    animator = instance.AddComponent<Animator>();
                }

                animator.avatar = avatar;
                animator.Rebind();

                var report = new ValidationReport
                {
                    validatedAtUtc = DateTime.UtcNow.ToString("O"),
                    assetPath = BaseAvatarPath,
                    importerAnimationType = importer.animationType.ToString(),
                    importerAvatarSetup = importer.avatarSetup.ToString(),
                    autoGenerateAvatarMapping = importer.autoGenerateAvatarMappingIfUnspecified,
                    avatarName = avatar.name,
                    avatarIsValid = avatar.isValid,
                    avatarIsHuman = avatar.isHuman,
                };

                var seen = new HashSet<string>();

                foreach (var requiredBone in RequiredBones)
                {
                    var transform = animator.GetBoneTransform(requiredBone);
                    if (transform == null)
                    {
                        report.errors.Add($"Missing required humanoid bone: {requiredBone}");
                    }
                }

                foreach (var (bone, expectedName) in ExpectedMappings)
                {
                    var transform = animator.GetBoneTransform(bone);
                    var actualName = transform != null ? transform.name : "<missing>";
                    report.mappings.Add(new BoneMappingResult
                    {
                        humanBone = bone.ToString(),
                        expectedTransform = expectedName,
                        actualTransform = actualName,
                        isMatch = actualName == expectedName,
                    });

                    if (transform == null)
                    {
                        report.warnings.Add($"Optional/expected bone missing: {bone}");
                        continue;
                    }

                    if (actualName != expectedName)
                    {
                        report.warnings.Add($"Bone {bone} mapped to '{actualName}', expected '{expectedName}'.");
                    }

                    if (!seen.Add(actualName))
                    {
                        report.warnings.Add($"Duplicate transform reused by multiple humanoid slots: {actualName}");
                    }
                }

                report.summary = string.Join(
                    ", ",
                    report.mappings.Select(mapping => $"{mapping.humanBone}={mapping.actualTransform}")
                );
                report.success = report.errors.Count == 0;
                return report;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        private static void PersistReport(ValidationReport report)
        {
            string reportPath = GetProjectRelativePath(ReportRelativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(reportPath));
            File.WriteAllText(reportPath, JsonUtility.ToJson(report, true));
        }

        private static void EmitLogs(ValidationReport report)
        {
            if (!string.IsNullOrEmpty(report.importerAnimationType) || !string.IsNullOrEmpty(report.importerAvatarSetup))
            {
                Debug.Log(
                    $"[BaseAvatarHumanoidValidator] Rig settings: animationType={report.importerAnimationType}, " +
                    $"avatarSetup={report.importerAvatarSetup}, autoMapping={report.autoGenerateAvatarMapping}"
                );
            }

            if (!string.IsNullOrEmpty(report.summary))
            {
                Debug.Log($"[BaseAvatarHumanoidValidator] Humanoid summary: {report.summary}");
            }

            foreach (string warning in report.warnings)
            {
                Debug.LogWarning($"[BaseAvatarHumanoidValidator] {warning}");
            }

            foreach (string error in report.errors)
            {
                Debug.LogError($"[BaseAvatarHumanoidValidator] {error}");
            }

            Debug.Log(
                $"[BaseAvatarHumanoidValidator] Validation complete: {report.errors.Count} errors, {report.warnings.Count} warnings. " +
                $"Report: {ReportRelativePath}"
            );
        }

        private static string GetProjectRelativePath(string relativePath)
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
            return Path.Combine(projectRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
        }

        private static Avatar LoadAvatarSubAsset(string assetPath)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            foreach (var asset in assets)
            {
                if (asset is Avatar avatar)
                {
                    return avatar;
                }
            }

            return null;
        }

        [Serializable]
        private sealed class ValidationReport
        {
            public string validatedAtUtc = string.Empty;
            public string assetPath = string.Empty;
            public string importerAnimationType = string.Empty;
            public string importerAvatarSetup = string.Empty;
            public bool autoGenerateAvatarMapping;
            public string avatarName = string.Empty;
            public bool avatarIsValid;
            public bool avatarIsHuman;
            public bool success;
            public string summary = string.Empty;
            public List<string> warnings = new();
            public List<string> errors = new();
            public List<BoneMappingResult> mappings = new();

            public static ValidationReport Failure(string path, string message)
            {
                return new ValidationReport
                {
                    validatedAtUtc = DateTime.UtcNow.ToString("O"),
                    assetPath = path,
                    success = false,
                    errors = new List<string> { message },
                };
            }
        }

        [Serializable]
        private sealed class BoneMappingResult
        {
            public string humanBone = string.Empty;
            public string expectedTransform = string.Empty;
            public string actualTransform = string.Empty;
            public bool isMatch;
        }
    }
}
#endif
