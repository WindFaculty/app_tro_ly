#if UNITY_EDITOR
using LocalAssistant.World.Objects;
using LocalAssistant.World.Room;
using UnityEditor;
using UnityEngine;

namespace LocalAssistant.Editor
{
    public static class RoomObjectValidatorMenu
    {
        [MenuItem("Tools/TroLy/Validate Room Object Registry")]
        public static void ValidateRoomObjectRegistry()
        {
            LogReport(
                "placeholder-safe registry",
                RoomObjectRegistryValidator.Validate(
                    RoomObjectRegistry.CreateFoundationMvp(RoomLayoutDefinition.CreateDefault()),
                    RoomObjectValidationMode.PlaceholderSafe,
                    path => AssetDatabase.LoadAssetAtPath<GameObject>(path) != null));
        }

        [MenuItem("Tools/TroLy/Validate Room Object Prefab Intake")]
        public static void ValidateStrictPrefabIntake()
        {
            LogReport(
                "strict prefab intake",
                RoomObjectRegistryValidator.Validate(
                    RoomObjectRegistry.CreateFoundationMvp(RoomLayoutDefinition.CreateDefault()),
                    RoomObjectValidationMode.StrictPrefabIntake,
                    path => AssetDatabase.LoadAssetAtPath<GameObject>(path) != null));
        }

        private static void LogReport(string label, RoomObjectRegistryValidationReport report)
        {
            report ??= new RoomObjectRegistryValidationReport();
            foreach (var info in report.Infos)
            {
                Debug.Log($"[RoomObjectValidator] {info}");
            }

            foreach (var warning in report.Warnings)
            {
                Debug.LogWarning($"[RoomObjectValidator] {warning}");
            }

            foreach (var error in report.Errors)
            {
                Debug.LogError($"[RoomObjectValidator] {error}");
            }

            Debug.Log(
                $"[RoomObjectValidator] {label} validation complete: " +
                $"{report.Errors.Count} errors, {report.Warnings.Count} warnings, {report.Infos.Count} info entries " +
                $"across {report.DefinitionCount} definitions.");
        }
    }
}
#endif
