using System;
using UnityEngine;

namespace AvatarSystem.Data
{
    /// <summary>
    /// Maps each viseme type to one or more blendshape targets.
    /// Used by AvatarLipSyncDriver to translate phonemes into facial movement.
    /// </summary>
    [CreateAssetMenu(fileName = "NewLipSyncMap", menuName = "AvatarSystem/LipSync Map")]
    public sealed class LipSyncMapDefinition : ScriptableObject
    {
        public VisemeMapping[] mappings;

        /// <summary>
        /// Finds the mapping for a given viseme, or null if unmapped.
        /// </summary>
        public VisemeMapping? GetMapping(VisemeType viseme)
        {
            if (mappings == null) return null;
            for (int i = 0; i < mappings.Length; i++)
            {
                if (mappings[i].viseme == viseme) return mappings[i];
            }
            return null;
        }
    }

    [Serializable]
    public struct VisemeMapping
    {
        public VisemeType viseme;
        public BlendShapeTarget[] targets;
    }
}
