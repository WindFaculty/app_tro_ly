#if UNITY_EDITOR
using AvatarSystem.Data;
using UnityEditor;
using UnityEngine;

namespace AvatarSystem.Editor
{
    /// <summary>
    /// Editor-time validation tools for avatar items and prefabs.
    /// Access via menu: Tools > AvatarSystem > Validate Items
    /// </summary>
    public static class AvatarValidator
    {
        [MenuItem("Tools/AvatarSystem/Validate All Item Definitions")]
        public static void ValidateAllItems()
        {
            string[] guids = AssetDatabase.FindAssets("t:AvatarItemDefinition");
            int errorCount = 0;
            int warnCount = 0;

            Debug.Log($"[AvatarValidator] Scanning {guids.Length} item definitions...");

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var item = AssetDatabase.LoadAssetAtPath<AvatarItemDefinition>(path);
                if (item == null) continue;

                // Check required fields
                if (string.IsNullOrEmpty(item.itemId))
                {
                    Debug.LogError($"[AvatarValidator] {path}: Missing itemId.", item);
                    errorCount++;
                }

                if (string.IsNullOrEmpty(item.displayName))
                {
                    Debug.LogWarning($"[AvatarValidator] {path}: Missing displayName.", item);
                    warnCount++;
                }

                if (item.prefab == null)
                {
                    Debug.LogWarning($"[AvatarValidator] {path}: No prefab assigned.", item);
                    warnCount++;
                }

                // Validate prefab has SkinnedMeshRenderer (for clothing/hair)
                if (item.prefab != null)
                {
                    var smr = item.prefab.GetComponentInChildren<SkinnedMeshRenderer>();
                    var mr = item.prefab.GetComponentInChildren<MeshRenderer>();
                    if (smr == null && mr == null)
                    {
                        Debug.LogWarning($"[AvatarValidator] {path}: Prefab has no SkinnedMeshRenderer or MeshRenderer.", item);
                        warnCount++;
                    }

                    // Check for Animator (items should NOT have their own animator)
                    var anim = item.prefab.GetComponentInChildren<Animator>();
                    if (anim != null)
                    {
                        Debug.LogWarning($"[AvatarValidator] {path}: Prefab has its own Animator — items should share the base avatar Animator.", item);
                        warnCount++;
                    }
                }

                // Check slot consistency
                if (item.blocksSlots != null)
                {
                    foreach (var blocked in item.blocksSlots)
                    {
                        if (blocked == item.slotType)
                        {
                            Debug.LogWarning($"[AvatarValidator] {path}: Item blocks its own slot type.", item);
                            warnCount++;
                        }
                    }
                }

                // Check that requiresSlots don't include own slot
                if (item.requiresSlots != null)
                {
                    foreach (var req in item.requiresSlots)
                    {
                        if (req == item.slotType)
                        {
                            Debug.LogError($"[AvatarValidator] {path}: Item requires its own slot type — circular dependency.", item);
                            errorCount++;
                        }
                    }
                }

                // Thumbnail recommendation
                if (item.thumbnail == null)
                {
                    Debug.Log($"[AvatarValidator] {path}: No thumbnail assigned (optional but recommended).", item);
                }
            }

            Debug.Log($"[AvatarValidator] Validation complete: {errorCount} errors, {warnCount} warnings out of {guids.Length} items.");
        }

        [MenuItem("Tools/AvatarSystem/Validate Outfit Presets")]
        public static void ValidatePresets()
        {
            string[] guids = AssetDatabase.FindAssets("t:OutfitPresetDefinition");
            int issues = 0;

            Debug.Log($"[AvatarValidator] Scanning {guids.Length} outfit presets...");

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var preset = AssetDatabase.LoadAssetAtPath<OutfitPresetDefinition>(path);
                if (preset == null) continue;

                if (string.IsNullOrEmpty(preset.presetName))
                {
                    Debug.LogWarning($"[AvatarValidator] {path}: Missing preset name.", preset);
                    issues++;
                }

                // Check for Dress + Top/Bottom conflict in same preset
                if (preset.dress != null && (preset.top != null || preset.bottom != null))
                {
                    Debug.LogWarning($"[AvatarValidator] {path}: Preset has Dress AND Top/Bottom — Dress should block Top and Bottom.", preset);
                    issues++;
                }
            }

            Debug.Log($"[AvatarValidator] Preset validation complete: {issues} issues out of {guids.Length} presets.");
        }
    }
}
#endif
