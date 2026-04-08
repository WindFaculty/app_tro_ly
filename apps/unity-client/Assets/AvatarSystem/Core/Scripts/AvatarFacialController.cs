using System.Collections;
using System.Collections.Generic;
using AvatarSystem.Data;
using UnityEngine;

namespace AvatarSystem
{
    /// <summary>
    /// Controls all facial blendshapes across 4 layers:
    ///   1. Background state (neutral, soft smile, listening, thinking)
    ///   2. Emotion expressions
    ///   3. Micro-movements (blink, eye saccade)
    ///   4. Lip-sync (driven by AvatarLipSyncDriver)
    ///
    /// Script-driven blendshapes. It does not rely on Mecanim for facial control.
    /// </summary>
    public sealed class AvatarFacialController : MonoBehaviour
    {
        private const string DefaultFaceRendererName = "Body_Head";

        [Header("Configuration")]
        [SerializeField] private float blinkInterval = 3.5f;
        [SerializeField] private float blinkDuration = 0.12f;
        [SerializeField] private float blinkRandomRange = 2f;
        [SerializeField] private float expressionBlendSpeed = 4f;
        [SerializeField, Range(0f, 1f)] private float mouthExpressionWeightWhenLipSyncActive = 0f;

        [Header("Blink Blendshapes")]
        [SerializeField] private string blinkLeftName = "Blink_L";
        [SerializeField] private string blinkRightName = "Blink_R";

        [Header("References")]
        [SerializeField] private SkinnedMeshRenderer faceMesh;

        private readonly Dictionary<string, int> blendShapeIndexCache = new();
        private readonly Dictionary<string, float> currentWeights = new();
        private readonly Dictionary<string, float> targetWeights = new();
        private readonly Dictionary<string, float> lipSyncWeights = new();

        private EmotionType currentEmotion = EmotionType.Neutral;
        private ExpressionDefinition currentExpressionDef;
        private Coroutine blinkCoroutine;
        private bool hasLoggedMissingFaceMesh;
        private bool hasLoggedMissingBlinkShapes;

        public EmotionType CurrentEmotion => currentEmotion;
        public SkinnedMeshRenderer FaceMesh => faceMesh;

        private void Awake()
        {
            Initialize(faceMesh);
        }

        private void OnDisable()
        {
            StopBlinkLoop();
        }

        public void Initialize(SkinnedMeshRenderer mesh = null)
        {
            BindFaceMesh(mesh);

            if (faceMesh == null || faceMesh.sharedMesh == null)
            {
                blendShapeIndexCache.Clear();
                StopBlinkLoop();
                LogMissingFaceMesh();
                return;
            }

            hasLoggedMissingFaceMesh = false;
            CacheBlendShapeIndices();
            ValidateBlinkBlendShapes();
            StartBlinkLoop();
        }

        /// <summary>Set the active emotion expression.</summary>
        public void SetEmotion(EmotionType emotion, ExpressionDefinition definition = null)
        {
            currentEmotion = emotion;
            currentExpressionDef = definition;
            targetWeights.Clear();

            if (definition == null || definition.targets == null)
            {
                return;
            }

            foreach (var target in definition.targets)
            {
                targetWeights[target.blendShapeName] = target.weight;
            }
        }

        /// <summary>Clear expression back to neutral.</summary>
        public void ClearEmotion()
        {
            SetEmotion(EmotionType.Neutral, null);
        }

        public void SetLipSyncWeight(string blendShapeName, float weight)
        {
            lipSyncWeights[blendShapeName] = weight;
        }

        public void ClearLipSync()
        {
            lipSyncWeights.Clear();
        }

        public void SetBlendShapeWeight(string shapeName, float weight)
        {
            if (faceMesh == null || faceMesh.sharedMesh == null)
            {
                return;
            }

            int idx = GetBlendShapeIndex(shapeName);
            if (idx >= 0)
            {
                faceMesh.SetBlendShapeWeight(idx, weight);
            }
        }

        public int GetBlendShapeIndex(string shapeName)
        {
            return blendShapeIndexCache.TryGetValue(shapeName, out int idx) ? idx : -1;
        }

        private void Update()
        {
            if (faceMesh == null || faceMesh.sharedMesh == null)
            {
                return;
            }

            float dt = Time.deltaTime;
            float speed = currentExpressionDef != null ? currentExpressionDef.transitionSpeed : expressionBlendSpeed;
            bool lipSyncActive = HasActiveLipSyncWeights();

            foreach (var kvp in targetWeights)
            {
                float current = currentWeights.TryGetValue(kvp.Key, out float cachedWeight) ? cachedWeight : 0f;
                float blended = Mathf.Lerp(current, kvp.Value, dt * speed);
                currentWeights[kvp.Key] = blended;
            }

            var keysToRemove = new List<string>();
            var decayedWeights = new List<KeyValuePair<string, float>>();
            foreach (var kvp in currentWeights)
            {
                if (targetWeights.ContainsKey(kvp.Key) || lipSyncWeights.ContainsKey(kvp.Key))
                {
                    continue;
                }

                float decayed = Mathf.Lerp(kvp.Value, 0f, dt * speed);
                if (decayed < 0.01f)
                {
                    keysToRemove.Add(kvp.Key);
                }
                else
                {
                    decayedWeights.Add(new KeyValuePair<string, float>(kvp.Key, decayed));
                }
            }

            foreach (var entry in decayedWeights)
            {
                currentWeights[entry.Key] = entry.Value;
            }

            foreach (string key in keysToRemove)
            {
                currentWeights.Remove(key);
            }

            foreach (var kvp in currentWeights)
            {
                int idx = GetBlendShapeIndex(kvp.Key);
                if (idx >= 0)
                {
                    float effectiveWeight = kvp.Value;
                    if (lipSyncActive && IsLowerFaceBlendShape(kvp.Key))
                    {
                        effectiveWeight *= mouthExpressionWeightWhenLipSyncActive;
                    }

                    faceMesh.SetBlendShapeWeight(idx, effectiveWeight);
                }
            }

            foreach (var kvp in lipSyncWeights)
            {
                int idx = GetBlendShapeIndex(kvp.Key);
                if (idx >= 0)
                {
                    faceMesh.SetBlendShapeWeight(idx, kvp.Value);
                }
            }
        }

        private void StartBlinkLoop()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            StopBlinkLoop();
            blinkCoroutine = StartCoroutine(BlinkLoop());
        }

        private void StopBlinkLoop()
        {
            if (blinkCoroutine == null)
            {
                return;
            }

            StopCoroutine(blinkCoroutine);
            blinkCoroutine = null;
        }

        private IEnumerator BlinkLoop()
        {
            while (true)
            {
                float wait = blinkInterval + Random.Range(-blinkRandomRange, blinkRandomRange);
                yield return new WaitForSeconds(Mathf.Max(0.5f, wait));

                float t = 0f;
                float halfBlinkDuration = Mathf.Max(0.01f, blinkDuration * 0.5f);
                while (t < halfBlinkDuration)
                {
                    t += Time.deltaTime;
                    float w = Mathf.Lerp(0f, 100f, t / halfBlinkDuration);
                    SetBlendShapeWeight(blinkLeftName, w);
                    SetBlendShapeWeight(blinkRightName, w);
                    yield return null;
                }

                t = 0f;
                while (t < halfBlinkDuration)
                {
                    t += Time.deltaTime;
                    float w = Mathf.Lerp(100f, 0f, t / halfBlinkDuration);
                    SetBlendShapeWeight(blinkLeftName, w);
                    SetBlendShapeWeight(blinkRightName, w);
                    yield return null;
                }

                SetBlendShapeWeight(blinkLeftName, 0f);
                SetBlendShapeWeight(blinkRightName, 0f);
            }
        }

        private void BindFaceMesh(SkinnedMeshRenderer mesh)
        {
            if (mesh != null)
            {
                faceMesh = mesh;
                return;
            }

            if (faceMesh != null && faceMesh.sharedMesh != null)
            {
                return;
            }

            faceMesh = ResolveFaceMesh();
        }

        private SkinnedMeshRenderer ResolveFaceMesh()
        {
            var rootController = GetComponentInParent<AvatarRootController>();
            if (rootController != null && rootController.FaceMesh != null)
            {
                return rootController.FaceMesh;
            }

            SkinnedMeshRenderer fallback = null;
            foreach (var renderer in GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (renderer == null || renderer.sharedMesh == null)
                {
                    continue;
                }

                if (renderer.name == DefaultFaceRendererName)
                {
                    return renderer;
                }

                if (fallback == null &&
                    HasBlendShape(renderer.sharedMesh, blinkLeftName) &&
                    HasBlendShape(renderer.sharedMesh, blinkRightName))
                {
                    fallback = renderer;
                }
            }

            return fallback;
        }

        private void CacheBlendShapeIndices()
        {
            blendShapeIndexCache.Clear();

            if (faceMesh == null || faceMesh.sharedMesh == null)
            {
                return;
            }

            var mesh = faceMesh.sharedMesh;
            for (int i = 0; i < mesh.blendShapeCount; i++)
            {
                blendShapeIndexCache[mesh.GetBlendShapeName(i)] = i;
            }
        }

        private void ValidateBlinkBlendShapes()
        {
            bool hasBlinkLeft = GetBlendShapeIndex(blinkLeftName) >= 0;
            bool hasBlinkRight = GetBlendShapeIndex(blinkRightName) >= 0;
            if (hasBlinkLeft && hasBlinkRight)
            {
                hasLoggedMissingBlinkShapes = false;
                return;
            }

            if (hasLoggedMissingBlinkShapes)
            {
                return;
            }

            hasLoggedMissingBlinkShapes = true;
            Debug.LogWarning(
                $"[AvatarFacialController] Face mesh '{faceMesh.name}' is missing blink blendshapes '{blinkLeftName}' or '{blinkRightName}'.",
                this
            );
        }

        private void LogMissingFaceMesh()
        {
            if (hasLoggedMissingFaceMesh)
            {
                return;
            }

            hasLoggedMissingFaceMesh = true;
            Debug.LogWarning(
                "[AvatarFacialController] Could not resolve a facial SkinnedMeshRenderer. Expected the Body_Head renderer.",
                this
            );
        }

        private static bool HasBlendShape(Mesh mesh, string shapeName)
        {
            return mesh != null && !string.IsNullOrEmpty(shapeName) && mesh.GetBlendShapeIndex(shapeName) >= 0;
        }

        private bool HasActiveLipSyncWeights()
        {
            foreach (var weight in lipSyncWeights.Values)
            {
                if (weight > 0.01f)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsLowerFaceBlendShape(string blendShapeName)
        {
            switch (blendShapeName)
            {
                case "Smile":
                case "Sad":
                case "Surprise":
                case "AngryLight":
                case "MouthOpen":
                case "MouthNarrow":
                case "MouthWide":
                case "MouthRound":
                    return true;
            }

            return !string.IsNullOrEmpty(blendShapeName) && blendShapeName.StartsWith("Viseme_");
        }
    }
}
