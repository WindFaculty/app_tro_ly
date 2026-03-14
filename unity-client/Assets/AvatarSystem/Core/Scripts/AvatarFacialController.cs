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
    /// Script-driven blendshapes — does NOT rely on Mecanim for facial control.
    /// </summary>
    public sealed class AvatarFacialController : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private float blinkInterval = 3.5f;
        [SerializeField] private float blinkDuration = 0.12f;
        [SerializeField] private float blinkRandomRange = 2f;
        [SerializeField] private float expressionBlendSpeed = 4f;

        [Header("Blink Blendshapes")]
        [SerializeField] private string blinkLeftName = "Blink_L";
        [SerializeField] private string blinkRightName = "Blink_R";

        private SkinnedMeshRenderer faceMesh;
        private readonly Dictionary<string, int> blendShapeIndexCache = new();

        // Current state
        private EmotionType currentEmotion = EmotionType.Neutral;
        private ExpressionDefinition currentExpressionDef;
        private readonly Dictionary<string, float> currentWeights = new();
        private readonly Dictionary<string, float> targetWeights = new();

        // Lip-sync layer (set externally by AvatarLipSyncDriver)
        private readonly Dictionary<string, float> lipSyncWeights = new();

        private Coroutine blinkCoroutine;

        public EmotionType CurrentEmotion => currentEmotion;

        public void Initialize(SkinnedMeshRenderer mesh)
        {
            faceMesh = mesh;
            CacheBlendShapeIndices();
            StartBlinkLoop();
        }

        // ──────────────────────────────────────────────
        // Emotion API
        // ──────────────────────────────────────────────

        /// <summary>Set the active emotion expression.</summary>
        public void SetEmotion(EmotionType emotion, ExpressionDefinition definition = null)
        {
            currentEmotion = emotion;
            currentExpressionDef = definition;

            // Clear expression targets
            targetWeights.Clear();

            if (definition != null && definition.targets != null)
            {
                foreach (var target in definition.targets)
                {
                    targetWeights[target.blendShapeName] = target.weight;
                }
            }
        }

        /// <summary>Clear expression back to neutral.</summary>
        public void ClearEmotion()
        {
            SetEmotion(EmotionType.Neutral, null);
        }

        // ──────────────────────────────────────────────
        // Lip-sync layer (called by AvatarLipSyncDriver)
        // ──────────────────────────────────────────────

        public void SetLipSyncWeight(string blendShapeName, float weight)
        {
            lipSyncWeights[blendShapeName] = weight;
        }

        public void ClearLipSync()
        {
            lipSyncWeights.Clear();
        }

        // ──────────────────────────────────────────────
        // Direct blendshape access
        // ──────────────────────────────────────────────

        public void SetBlendShapeWeight(string shapeName, float weight)
        {
            if (faceMesh == null) return;
            int idx = GetBlendShapeIndex(shapeName);
            if (idx >= 0)
            {
                faceMesh.SetBlendShapeWeight(idx, weight);
            }
        }

        public int GetBlendShapeIndex(string shapeName)
        {
            if (blendShapeIndexCache.TryGetValue(shapeName, out int idx)) return idx;
            return -1;
        }

        // ──────────────────────────────────────────────
        // Update loop
        // ──────────────────────────────────────────────

        private void Update()
        {
            if (faceMesh == null) return;

            float dt = Time.deltaTime;
            float speed = currentExpressionDef != null ? currentExpressionDef.transitionSpeed : expressionBlendSpeed;

            // Blend expression weights
            foreach (var kvp in targetWeights)
            {
                float current = currentWeights.TryGetValue(kvp.Key, out float c) ? c : 0f;
                float blended = Mathf.Lerp(current, kvp.Value, dt * speed);
                currentWeights[kvp.Key] = blended;
            }

            // Decay weights not in target
            var keysToRemove = new List<string>();
            foreach (var kvp in currentWeights)
            {
                if (!targetWeights.ContainsKey(kvp.Key) && !lipSyncWeights.ContainsKey(kvp.Key))
                {
                    float decayed = Mathf.Lerp(kvp.Value, 0f, dt * speed);
                    if (decayed < 0.01f)
                    {
                        keysToRemove.Add(kvp.Key);
                    }
                    else
                    {
                        currentWeights[kvp.Key] = decayed;
                    }
                }
            }
            foreach (var key in keysToRemove) currentWeights.Remove(key);

            // Apply expression weights
            foreach (var kvp in currentWeights)
            {
                int idx = GetBlendShapeIndex(kvp.Key);
                if (idx >= 0) faceMesh.SetBlendShapeWeight(idx, kvp.Value);
            }

            // Apply lip-sync weights (additive, overrides expression on same shapes)
            foreach (var kvp in lipSyncWeights)
            {
                int idx = GetBlendShapeIndex(kvp.Key);
                if (idx >= 0) faceMesh.SetBlendShapeWeight(idx, kvp.Value);
            }
        }

        // ──────────────────────────────────────────────
        // Blink system
        // ──────────────────────────────────────────────

        private void StartBlinkLoop()
        {
            if (blinkCoroutine != null) StopCoroutine(blinkCoroutine);
            blinkCoroutine = StartCoroutine(BlinkLoop());
        }

        private IEnumerator BlinkLoop()
        {
            while (true)
            {
                float wait = blinkInterval + Random.Range(-blinkRandomRange, blinkRandomRange);
                yield return new WaitForSeconds(Mathf.Max(0.5f, wait));

                // Close eyes
                float t = 0f;
                while (t < blinkDuration * 0.5f)
                {
                    t += Time.deltaTime;
                    float w = Mathf.Lerp(0f, 100f, t / (blinkDuration * 0.5f));
                    SetBlendShapeWeight(blinkLeftName, w);
                    SetBlendShapeWeight(blinkRightName, w);
                    yield return null;
                }

                // Open eyes
                t = 0f;
                while (t < blinkDuration * 0.5f)
                {
                    t += Time.deltaTime;
                    float w = Mathf.Lerp(100f, 0f, t / (blinkDuration * 0.5f));
                    SetBlendShapeWeight(blinkLeftName, w);
                    SetBlendShapeWeight(blinkRightName, w);
                    yield return null;
                }

                SetBlendShapeWeight(blinkLeftName, 0f);
                SetBlendShapeWeight(blinkRightName, 0f);
            }
        }

        // ──────────────────────────────────────────────
        // Helpers
        // ──────────────────────────────────────────────

        private void CacheBlendShapeIndices()
        {
            blendShapeIndexCache.Clear();
            if (faceMesh == null || faceMesh.sharedMesh == null) return;
            var mesh = faceMesh.sharedMesh;
            for (int i = 0; i < mesh.blendShapeCount; i++)
            {
                string name = mesh.GetBlendShapeName(i);
                blendShapeIndexCache[name] = i;
            }
        }
    }
}
