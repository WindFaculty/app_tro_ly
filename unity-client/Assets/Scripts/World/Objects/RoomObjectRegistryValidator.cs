using System;
using System.Collections.Generic;

namespace LocalAssistant.World.Objects
{
    public static class RoomObjectRegistryValidator
    {
        public static RoomObjectRegistryValidationReport Validate(
            RoomObjectRegistry registry,
            RoomObjectValidationMode mode = RoomObjectValidationMode.PlaceholderSafe,
            Func<string, bool> prefabPathExists = null)
        {
            return ValidateDefinitions(registry?.Definitions, mode, prefabPathExists);
        }

        public static RoomObjectRegistryValidationReport ValidateDefinitions(
            IEnumerable<RoomObjectDefinition> definitions,
            RoomObjectValidationMode mode = RoomObjectValidationMode.PlaceholderSafe,
            Func<string, bool> prefabPathExists = null)
        {
            var report = new RoomObjectRegistryValidationReport();
            if (definitions == null)
            {
                report.Errors.Add("Registry has no definitions to validate.");
                return report;
            }

            var ids = new HashSet<string>(StringComparer.Ordinal);
            var prefabKeys = new HashSet<string>(StringComparer.Ordinal);

            foreach (var definition in definitions)
            {
                if (definition == null)
                {
                    report.Errors.Add("Registry contains a null room-object definition.");
                    continue;
                }

                report.DefinitionCount++;
                var label = string.IsNullOrWhiteSpace(definition.Id) ? "<missing-id>" : definition.Id;

                if (string.IsNullOrWhiteSpace(definition.Id))
                {
                    report.Errors.Add("Room object definition is missing id.");
                    continue;
                }

                if (!ids.Add(definition.Id))
                {
                    report.Errors.Add($"{label}: duplicate id.");
                }

                if (string.IsNullOrWhiteSpace(definition.DisplayName))
                {
                    report.Warnings.Add($"{label}: displayName is recommended for Home overlay text.");
                }

                if (string.IsNullOrWhiteSpace(definition.AnchorType))
                {
                    report.Warnings.Add($"{label}: anchorType is empty.");
                }

                if (string.IsNullOrWhiteSpace(definition.PrefabKey))
                {
                    report.Warnings.Add($"{label}: prefabKey is empty.");
                }
                else
                {
                    if (!prefabKeys.Add(definition.PrefabKey))
                    {
                        report.Errors.Add($"{label}: duplicate prefabKey '{definition.PrefabKey}'.");
                    }

                    var expectedPrefix = BuildExpectedPrefabPrefix(definition.Category);
                    if (!definition.PrefabKey.StartsWith(expectedPrefix, StringComparison.Ordinal))
                    {
                        report.Warnings.Add($"{label}: prefabKey '{definition.PrefabKey}' should start with '{expectedPrefix}'.");
                    }
                }

                if (string.IsNullOrWhiteSpace(definition.PrefabPath))
                {
                    report.Warnings.Add($"{label}: prefabPath is empty.");
                }
                else
                {
                    if (!definition.PrefabPath.StartsWith("Assets/World/Prefabs/", StringComparison.Ordinal))
                    {
                        report.Warnings.Add($"{label}: prefabPath '{definition.PrefabPath}' is outside Assets/World/Prefabs/.");
                    }

                    if (prefabPathExists != null && !prefabPathExists(definition.PrefabPath))
                    {
                        if (mode == RoomObjectValidationMode.StrictPrefabIntake)
                        {
                            report.Errors.Add($"{label}: prefabPath '{definition.PrefabPath}' does not resolve to a prefab asset.");
                        }
                        else
                        {
                            report.Infos.Add($"{label}: prefabPath '{definition.PrefabPath}' is not in repo yet, so primitive placeholder fallback remains active.");
                        }
                    }
                }

                if (definition.DefaultScale.x <= 0f || definition.DefaultScale.y <= 0f || definition.DefaultScale.z <= 0f)
                {
                    report.Errors.Add($"{label}: defaultScale must stay positive on all axes.");
                }

                if (definition.InteractionType != RoomInteractionType.None && !definition.Selectable)
                {
                    report.Errors.Add($"{label}: interactionType '{definition.InteractionType}' requires selectable=true.");
                }

                if (definition.Inspectable &&
                    definition.InteractionType != RoomInteractionType.Inspect &&
                    definition.InteractionType != RoomInteractionType.InspectAndFocus)
                {
                    report.Warnings.Add($"{label}: inspectable=true but interactionType is '{definition.InteractionType}'.");
                }

                if (!definition.Selectable && definition.Hoverable)
                {
                    report.Warnings.Add($"{label}: hoverable=true while selectable=false.");
                }

                AddDuplicateWarnings(report.Warnings, label, "tag", definition.Tags);
                AddDuplicateWarnings(report.Warnings, label, "optional state", definition.OptionalStates);
            }

            return report;
        }

        private static void AddDuplicateWarnings(List<string> warnings, string label, string fieldName, string[] values)
        {
            if (values == null || values.Length == 0)
            {
                return;
            }

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var index = 0; index < values.Length; index++)
            {
                var value = values[index];
                if (string.IsNullOrWhiteSpace(value))
                {
                    warnings.Add($"{label}: {fieldName} list contains an empty entry.");
                    continue;
                }

                if (!seen.Add(value))
                {
                    warnings.Add($"{label}: duplicate {fieldName} '{value}'.");
                }
            }
        }

        private static string BuildExpectedPrefabPrefix(RoomObjectCategory category)
        {
            return category switch
            {
                RoomObjectCategory.Structure => "OBJ_Structure_",
                RoomObjectCategory.Furniture => "OBJ_Furniture_",
                RoomObjectCategory.Decor => "OBJ_Decor_",
                RoomObjectCategory.Interactive => "OBJ_Interactive_",
                RoomObjectCategory.Lighting => "OBJ_Lighting_",
                RoomObjectCategory.Utility => "OBJ_Utility_",
                _ => "OBJ_",
            };
        }
    }
}
