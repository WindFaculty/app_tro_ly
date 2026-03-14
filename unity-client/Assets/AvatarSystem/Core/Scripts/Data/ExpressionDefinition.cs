using System;
using UnityEngine;

namespace AvatarSystem.Data
{
    /// <summary>
    /// Defines a facial expression as a set of blendshape targets.
    /// </summary>
    [CreateAssetMenu(fileName = "NewExpression", menuName = "AvatarSystem/Expression Definition")]
    public sealed class ExpressionDefinition : ScriptableObject
    {
        public EmotionType emotionType;
        public string displayName;

        [Tooltip("Priority when multiple expressions compete. Higher wins.")]
        public int priority;

        [Tooltip("Seconds to blend toward target weights.")]
        public float transitionSpeed = 0.15f;

        public BlendShapeTarget[] targets;
    }

    /// <summary>
    /// A single blendshape name and its target weight (0–100).
    /// </summary>
    [Serializable]
    public struct BlendShapeTarget
    {
        [Tooltip("Exact blendshape name on the SkinnedMeshRenderer.")]
        public string blendShapeName;

        [Range(0f, 100f)]
        public float weight;
    }
}
