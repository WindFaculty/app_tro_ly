using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using AvatarSystem.Data;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AvatarSystem.Validation
{
    [DisallowMultipleComponent]
    public sealed class BaseAvatarFacialRuntimeProbe : MonoBehaviour
    {
        public AvatarRootController avatarRoot;
        public AvatarFacialController facialController;
        public ExpressionDefinition[] expressionDefinitions;
        public LipSyncMapDefinition lipSyncMap;
        public EmotionType[] requiredEmotions = Array.Empty<EmotionType>();
        public string reportPath = string.Empty;
        public float blinkTimeoutSeconds = 2.5f;
        public float expressionObservationSeconds = 5f;
        public float neutralSettleSeconds = 0.2f;
        public float amplitudeValidationSeconds = 1.2f;
        public float lipSyncSettleSeconds = 0.4f;

        private IEnumerator Start()
        {
            var report = new BaseAvatarFacialValidationReport
            {
                validatedAtUtc = DateTime.UtcNow.ToString("O"),
            };

            yield return SafeRunValidation(report);

            report.faceMeshBindingPassed = report.faceMeshResolved && report.facialControllerBoundToRootFaceMesh;
            report.success =
                report.faceMeshBindingPassed &&
                report.blinkTestPassed &&
                report.expressionCoveragePassed &&
                report.amplitudeLipSyncPassed &&
                report.expressionLipSyncLayeringPassed &&
                report.errors.Count == 0;

            WriteReport(report);

#if UNITY_EDITOR
            EditorApplication.ExitPlaymode();
#else
            Application.Quit();
#endif
        }

        private IEnumerator SafeRunValidation(BaseAvatarFacialValidationReport report)
        {
            var routine = RunValidation(report);
            while (true)
            {
                object yieldedValue;

                try
                {
                    if (!routine.MoveNext())
                    {
                        yield break;
                    }

                    yieldedValue = routine.Current;
                }
                catch (Exception exception)
                {
                    report.errors.Add($"Runtime probe exception: {exception.Message}");
                    yield break;
                }

                yield return yieldedValue;
            }
        }

        private IEnumerator RunValidation(BaseAvatarFacialValidationReport report)
        {
            if (avatarRoot == null)
            {
                avatarRoot = FindFirstObjectByType<AvatarRootController>();
            }

            if (facialController == null && avatarRoot != null)
            {
                facialController = avatarRoot.Facial;
            }

            if (facialController == null)
            {
                facialController = FindFirstObjectByType<AvatarFacialController>();
            }

            if (avatarRoot == null)
            {
                report.errors.Add("AvatarRootController was not found in the validation scene.");
                yield break;
            }

            if (facialController == null)
            {
                report.errors.Add("AvatarFacialController was not found in the validation scene.");
                yield break;
            }

            avatarRoot.Initialize();
            yield return null;

            report.rootFaceMeshName = avatarRoot.FaceMesh != null ? avatarRoot.FaceMesh.name : string.Empty;
            report.facialControllerFaceMeshName = facialController.FaceMesh != null ? facialController.FaceMesh.name : string.Empty;
            report.faceMeshResolved = avatarRoot.FaceMesh != null;
            report.facialControllerBoundToRootFaceMesh = avatarRoot.FaceMesh != null && facialController.FaceMesh == avatarRoot.FaceMesh;

            if (!report.faceMeshResolved)
            {
                report.errors.Add("AvatarRootController.FaceMesh is null during runtime validation.");
                yield break;
            }

            if (!report.facialControllerBoundToRootFaceMesh)
            {
                report.errors.Add("AvatarFacialController is not bound to AvatarRootController.FaceMesh at runtime.");
                yield break;
            }

            report.faceBlendShapeCount = avatarRoot.FaceMesh.sharedMesh != null ? avatarRoot.FaceMesh.sharedMesh.blendShapeCount : 0;

            yield return ValidateBlink(report);
            yield return ValidateExpressions(report);
            yield return ValidateAmplitudeLipSync(report);
            yield return ValidateExpressionLipSyncLayering(report);
        }

        private IEnumerator ValidateBlink(BaseAvatarFacialValidationReport report)
        {
            var faceRenderer = avatarRoot.FaceMesh;
            int leftIndex = facialController.GetBlendShapeIndex("Blink_L");
            int rightIndex = facialController.GetBlendShapeIndex("Blink_R");
            if (leftIndex < 0 || rightIndex < 0)
            {
                report.errors.Add("Blink validation could not find Blink_L/Blink_R on the bound face mesh.");
                yield break;
            }

            float elapsed = 0f;
            bool blinkStarted = false;
            bool blinkReachedPeak = false;
            bool blinkSettled = false;
            float blinkStartTime = 0f;

            while (elapsed < blinkTimeoutSeconds)
            {
                float leftWeight = faceRenderer.GetBlendShapeWeight(leftIndex);
                float rightWeight = faceRenderer.GetBlendShapeWeight(rightIndex);

                report.blinkPeakLeft = Mathf.Max(report.blinkPeakLeft, leftWeight);
                report.blinkPeakRight = Mathf.Max(report.blinkPeakRight, rightWeight);

                if (!blinkStarted && (leftWeight > 5f || rightWeight > 5f))
                {
                    blinkStarted = true;
                    blinkStartTime = elapsed;
                }

                if (blinkStarted && (report.blinkPeakLeft >= 95f || report.blinkPeakRight >= 95f))
                {
                    blinkReachedPeak = true;
                }

                if (blinkStarted && blinkReachedPeak && leftWeight <= 1f && rightWeight <= 1f)
                {
                    blinkSettled = true;
                    report.blinkDurationObserved = Mathf.Max(0f, elapsed - blinkStartTime);
                    report.blinkSettledLeft = leftWeight;
                    report.blinkSettledRight = rightWeight;
                    break;
                }

                yield return null;
                elapsed += Time.deltaTime;
            }

            report.blinkTestPassed = blinkStarted && blinkReachedPeak && blinkSettled;
            if (!report.blinkTestPassed)
            {
                report.errors.Add(
                    $"Blink validation failed. started={blinkStarted}, peakL={report.blinkPeakLeft:F1}, peakR={report.blinkPeakRight:F1}, settled={blinkSettled}."
                );
            }
        }

        private IEnumerator ValidateExpressions(BaseAvatarFacialValidationReport report)
        {
            report.expressionTestsRequested = requiredEmotions != null ? requiredEmotions.Length : 0;

            var definitionMap = new Dictionary<EmotionType, ExpressionDefinition>();
            if (expressionDefinitions != null)
            {
                foreach (var definition in expressionDefinitions)
                {
                    if (definition == null || definitionMap.ContainsKey(definition.emotionType))
                    {
                        continue;
                    }

                    definitionMap.Add(definition.emotionType, definition);
                }
            }

            if (requiredEmotions == null || requiredEmotions.Length == 0)
            {
                report.warnings.Add("No required emotions were configured for runtime validation.");
                report.expressionCoveragePassed = false;
                yield break;
            }

            foreach (EmotionType emotion in requiredEmotions)
            {
                var result = new ExpressionValidationResult
                {
                    emotion = emotion.ToString(),
                };
                report.expressionResults.Add(result);

                if (!definitionMap.TryGetValue(emotion, out var definition))
                {
                    result.status = "missing_definition";
                    report.expressionTestsSkipped++;
                    report.warnings.Add($"Missing ExpressionDefinition for emotion '{emotion}'.");
                    continue;
                }

                report.expressionTestsConfigured++;
                result.definitionName = definition.name;

                if (definition.targets == null || definition.targets.Length == 0)
                {
                    result.status = "no_targets";
                    report.errors.Add($"ExpressionDefinition '{definition.name}' has no targets.");
                    continue;
                }

                bool hasMissingBlendShape = false;
                foreach (var target in definition.targets)
                {
                    result.targetBlendShapes.Add(target.blendShapeName);
                    if (facialController.GetBlendShapeIndex(target.blendShapeName) >= 0)
                    {
                        continue;
                    }

                    hasMissingBlendShape = true;
                    result.missingBlendShapes.Add(target.blendShapeName);
                }

                if (hasMissingBlendShape)
                {
                    result.status = "missing_blendshape";
                    report.errors.Add(
                        $"ExpressionDefinition '{definition.name}' references missing blendshapes: {string.Join(", ", result.missingBlendShapes)}."
                    );
                    continue;
                }

                facialController.ClearLipSync();
                facialController.ClearEmotion();
                yield return new WaitForSeconds(neutralSettleSeconds);

                facialController.SetEmotion(emotion, definition);

                float elapsed = 0f;
                while (elapsed < expressionObservationSeconds)
                {
                    foreach (var target in definition.targets)
                    {
                        int index = facialController.GetBlendShapeIndex(target.blendShapeName);
                        float observed = avatarRoot.FaceMesh.GetBlendShapeWeight(index);
                        float passThreshold = target.weight <= 10f ? target.weight * 0.5f : target.weight * 0.65f;

                        var observation = result.FindOrCreateObservation(target.blendShapeName, target.weight, passThreshold);
                        observation.maxObservedWeight = Mathf.Max(observation.maxObservedWeight, observed);
                    }

                    yield return null;
                    elapsed += Time.deltaTime;
                }

                result.passed = true;
                foreach (var observation in result.observations)
                {
                    if (observation.targetWeight <= 0.01f)
                    {
                        continue;
                    }

                    if (observation.maxObservedWeight + 0.01f < observation.passThreshold)
                    {
                        result.passed = false;
                    }
                }

                result.status = result.passed ? "passed" : "target_not_reached";
                if (result.passed)
                {
                    report.expressionTestsPassed++;
                }
                else
                {
                    report.errors.Add(
                        $"Expression '{emotion}' did not reach its configured target weights within {expressionObservationSeconds:F1}s."
                    );
                }
            }

            facialController.ClearEmotion();
            yield return new WaitForSeconds(neutralSettleSeconds);

            report.expressionCoveragePassed =
                report.expressionTestsRequested > 0 &&
                report.expressionTestsConfigured == report.expressionTestsRequested &&
                report.expressionTestsPassed == report.expressionTestsRequested &&
                report.expressionTestsSkipped == 0;

            if (!report.expressionCoveragePassed && report.expressionTestsRequested > 0)
            {
                report.warnings.Add(
                    $"Expression coverage incomplete: requested={report.expressionTestsRequested}, configured={report.expressionTestsConfigured}, passed={report.expressionTestsPassed}, skipped={report.expressionTestsSkipped}."
                );
            }
        }

        private IEnumerator ValidateAmplitudeLipSync(BaseAvatarFacialValidationReport report)
        {
            var lipSyncDriver = avatarRoot.LipSync;
            if (lipSyncDriver == null)
            {
                report.errors.Add("AvatarLipSyncDriver was not found on the base avatar prefab.");
                yield break;
            }

            LipSyncMapDefinition activeMap = lipSyncMap != null ? lipSyncMap : lipSyncDriver.LipSyncMap;
            report.lipSyncMapAssigned = activeMap != null;
            if (activeMap == null)
            {
                report.errors.Add("Lip-sync validation could not find a LipSyncMapDefinition.");
                yield break;
            }

            int mouthOpenIndex = facialController.GetBlendShapeIndex("MouthOpen");
            int visemeAaIndex = facialController.GetBlendShapeIndex("Viseme_AA");
            if (mouthOpenIndex < 0 || visemeAaIndex < 0)
            {
                report.errors.Add("Lip-sync validation could not find MouthOpen or Viseme_AA on the face mesh.");
                yield break;
            }

            var audioSource = EnsureValidationAudioSource();
            audioSource.clip = CreateSpeechLikeClip("AmplitudeLipSyncValidation", amplitudeValidationSeconds);
            lipSyncDriver.BindAudioSource(audioSource);
            lipSyncDriver.SetLipSyncMap(activeMap);
            lipSyncDriver.SetMode(AvatarLipSyncDriver.LipSyncMode.Amplitude);

            facialController.ClearLipSync();
            yield return new WaitForSeconds(neutralSettleSeconds);

            audioSource.Play();
            yield return null;
            while (audioSource.isPlaying)
            {
                report.amplitudePeakMouthOpen = Mathf.Max(report.amplitudePeakMouthOpen, avatarRoot.FaceMesh.GetBlendShapeWeight(mouthOpenIndex));
                report.amplitudePeakVisemeAA = Mathf.Max(report.amplitudePeakVisemeAA, avatarRoot.FaceMesh.GetBlendShapeWeight(visemeAaIndex));
                yield return null;
            }

            float elapsed = 0f;
            while (elapsed < lipSyncSettleSeconds)
            {
                report.amplitudeSettledMouthOpen = avatarRoot.FaceMesh.GetBlendShapeWeight(mouthOpenIndex);
                report.amplitudeSettledVisemeAA = avatarRoot.FaceMesh.GetBlendShapeWeight(visemeAaIndex);
                yield return null;
                elapsed += Time.deltaTime;
            }

            report.lipSyncSettledAfterPlayback =
                report.amplitudeSettledMouthOpen <= 1.5f &&
                report.amplitudeSettledVisemeAA <= 1.5f;

            report.amplitudeLipSyncPassed =
                report.amplitudePeakMouthOpen >= 10f &&
                report.amplitudePeakVisemeAA >= 10f &&
                report.lipSyncSettledAfterPlayback;

            if (!report.amplitudeLipSyncPassed)
            {
                report.errors.Add(
                    $"Amplitude lip-sync validation failed. peakMouthOpen={report.amplitudePeakMouthOpen:F1}, peakVisemeAA={report.amplitudePeakVisemeAA:F1}, settled={report.lipSyncSettledAfterPlayback}."
                );
            }

            facialController.ClearLipSync();
            yield return new WaitForSeconds(neutralSettleSeconds);
        }

        private IEnumerator ValidateExpressionLipSyncLayering(BaseAvatarFacialValidationReport report)
        {
            var lipSyncDriver = avatarRoot.LipSync;
            if (lipSyncDriver == null)
            {
                report.errors.Add("Layering validation could not find AvatarLipSyncDriver.");
                yield break;
            }

            ExpressionDefinition expression = FindLayeringExpression();
            if (expression == null)
            {
                report.errors.Add("Layering validation could not find a suitable expression with both upper-face and lower-face targets.");
                yield break;
            }

            if (!TryFindLayeringTargets(expression, out var upperFaceTarget, out var lowerFaceTarget))
            {
                report.errors.Add($"Expression '{expression.name}' does not contain the required upper-face and lower-face targets for layering validation.");
                yield break;
            }

            report.layeringExpressionName = expression.name;
            report.layeringUpperFaceBlendShape = upperFaceTarget.blendShapeName;
            report.layeringUpperFaceTargetWeight = upperFaceTarget.weight;
            report.layeringSuppressedLowerFaceBlendShape = lowerFaceTarget.blendShapeName;
            report.layeringLowerFaceTargetWeight = lowerFaceTarget.weight;

            var audioSource = EnsureValidationAudioSource();
            audioSource.clip = CreateSpeechLikeClip("LayeringValidation", amplitudeValidationSeconds);

            lipSyncDriver.BindAudioSource(audioSource);
            lipSyncDriver.SetLipSyncMap(lipSyncMap != null ? lipSyncMap : lipSyncDriver.LipSyncMap);
            lipSyncDriver.SetMode(AvatarLipSyncDriver.LipSyncMode.Amplitude);

            facialController.ClearLipSync();
            facialController.ClearEmotion();
            yield return new WaitForSeconds(neutralSettleSeconds);

            facialController.SetEmotion(expression.emotionType, expression);
            yield return new WaitForSeconds(0.35f);

            int upperFaceIndex = facialController.GetBlendShapeIndex(upperFaceTarget.blendShapeName);
            int lowerFaceIndex = facialController.GetBlendShapeIndex(lowerFaceTarget.blendShapeName);
            int mouthOpenIndex = facialController.GetBlendShapeIndex("MouthOpen");
            int visemeAaIndex = facialController.GetBlendShapeIndex("Viseme_AA");
            if (upperFaceIndex < 0 || lowerFaceIndex < 0 || mouthOpenIndex < 0 || visemeAaIndex < 0)
            {
                report.errors.Add("Layering validation could not resolve the required blendshape indices.");
                yield break;
            }

            audioSource.Play();
            yield return null;
            bool lipSyncObserved = false;
            while (audioSource.isPlaying)
            {
                float upperFaceWeight = avatarRoot.FaceMesh.GetBlendShapeWeight(upperFaceIndex);
                float lowerFaceWeight = avatarRoot.FaceMesh.GetBlendShapeWeight(lowerFaceIndex);
                float mouthOpenWeight = avatarRoot.FaceMesh.GetBlendShapeWeight(mouthOpenIndex);
                float visemeAaWeight = avatarRoot.FaceMesh.GetBlendShapeWeight(visemeAaIndex);

                if (mouthOpenWeight > 1f || visemeAaWeight > 1f)
                {
                    lipSyncObserved = true;
                    report.layeringUpperFacePeakObserved = Mathf.Max(report.layeringUpperFacePeakObserved, upperFaceWeight);
                    report.layeringLowerFacePeakObserved = Mathf.Max(report.layeringLowerFacePeakObserved, lowerFaceWeight);

                    if (mouthOpenWeight >= report.layeringPeakMouthOpenDuringSpeech)
                    {
                        report.layeringPeakMouthOpenDuringSpeech = mouthOpenWeight;
                        report.layeringLowerFaceObservedAtLipSyncPeak = lowerFaceWeight;
                    }
                }

                yield return null;
            }

            report.expressionLipSyncLayeringPassed =
                lipSyncObserved &&
                report.layeringUpperFacePeakObserved >= Mathf.Max(6f, upperFaceTarget.weight * 0.5f) &&
                report.layeringLowerFaceObservedAtLipSyncPeak <= Mathf.Max(8f, lowerFaceTarget.weight * 0.35f) &&
                report.layeringPeakMouthOpenDuringSpeech >= 10f;

            if (!report.expressionLipSyncLayeringPassed)
            {
                report.errors.Add(
                    $"Expression/lip-sync layering failed for '{expression.name}'. upperFacePeak={report.layeringUpperFacePeakObserved:F1}, lowerFaceAtLipSyncPeak={report.layeringLowerFaceObservedAtLipSyncPeak:F1}, mouthOpenPeak={report.layeringPeakMouthOpenDuringSpeech:F1}."
                );
            }

            facialController.ClearLipSync();
            facialController.ClearEmotion();
            yield return new WaitForSeconds(neutralSettleSeconds);
        }

        private ExpressionDefinition FindLayeringExpression()
        {
            if (expressionDefinitions == null || expressionDefinitions.Length == 0)
            {
                return null;
            }

            foreach (EmotionType preferredEmotion in new[] { EmotionType.Happy, EmotionType.SoftSmile, EmotionType.Apologetic })
            {
                foreach (var definition in expressionDefinitions)
                {
                    if (definition != null && definition.emotionType == preferredEmotion &&
                        TryFindLayeringTargets(definition, out _, out _))
                    {
                        return definition;
                    }
                }
            }

            foreach (var definition in expressionDefinitions)
            {
                if (definition != null && TryFindLayeringTargets(definition, out _, out _))
                {
                    return definition;
                }
            }

            return null;
        }

        private static bool TryFindLayeringTargets(ExpressionDefinition definition, out BlendShapeTarget upperFaceTarget, out BlendShapeTarget lowerFaceTarget)
        {
            upperFaceTarget = default;
            lowerFaceTarget = default;

            if (definition == null || definition.targets == null)
            {
                return false;
            }

            foreach (var target in definition.targets)
            {
                if (upperFaceTarget.weight <= 0f && IsUpperFaceBlendShape(target.blendShapeName))
                {
                    upperFaceTarget = target;
                }

                if (lowerFaceTarget.weight <= 0f && IsLowerFaceBlendShape(target.blendShapeName))
                {
                    lowerFaceTarget = target;
                }
            }

            return upperFaceTarget.weight > 0f && lowerFaceTarget.weight > 0f;
        }

        private AudioSource EnsureValidationAudioSource()
        {
            if (FindFirstObjectByType<AudioListener>() == null)
            {
                gameObject.AddComponent<AudioListener>();
            }

            var audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.volume = 1f;
            audioSource.spatialBlend = 0f;
            return audioSource;
        }

        private static AudioClip CreateSpeechLikeClip(string clipName, float durationSeconds)
        {
            const int sampleRate = 16000;
            int sampleCount = Mathf.Max(1, Mathf.CeilToInt(durationSeconds * sampleRate));
            var samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)sampleRate;
                float normalized = sampleCount <= 1 ? 0f : i / (float)(sampleCount - 1);
                float syllableEnvelope =
                    Pulse(normalized, 0.10f, 0.12f, 0.45f) +
                    Pulse(normalized, 0.38f, 0.12f, 0.75f) +
                    Pulse(normalized, 0.66f, 0.16f, 0.60f);

                float waveform =
                    Mathf.Sin(2f * Mathf.PI * 190f * t) * 0.65f +
                    Mathf.Sin(2f * Mathf.PI * 320f * t) * 0.35f;

                samples[i] = waveform * syllableEnvelope * 0.35f;
            }

            var clip = AudioClip.Create(clipName, sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static float Pulse(float value, float center, float width, float amplitude)
        {
            float distance = Mathf.Abs(value - center);
            if (distance >= width)
            {
                return 0f;
            }

            float normalized = 1f - (distance / width);
            return amplitude * normalized * normalized;
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

        private static bool IsUpperFaceBlendShape(string blendShapeName)
        {
            switch (blendShapeName)
            {
                case "Blink_L":
                case "Blink_R":
                case "SmileEye_L":
                case "SmileEye_R":
                case "WideEye_L":
                case "WideEye_R":
                case "BrowUp_L":
                case "BrowUp_R":
                case "BrowDown_L":
                case "BrowDown_R":
                case "BrowInnerUp":
                    return true;
            }

            return false;
        }

        private void WriteReport(BaseAvatarFacialValidationReport report)
        {
            if (string.IsNullOrWhiteSpace(reportPath))
            {
                Debug.LogError("[BaseAvatarFacialRuntimeProbe] reportPath is empty.");
                return;
            }

            string directory = Path.GetDirectoryName(reportPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(reportPath, JsonUtility.ToJson(report, true));
        }
    }

    [Serializable]
    public sealed class BaseAvatarFacialValidationReport
    {
        public string validatedAtUtc = string.Empty;
        public bool success;
        public bool faceMeshResolved;
        public bool facialControllerBoundToRootFaceMesh;
        public bool faceMeshBindingPassed;
        public bool blinkTestPassed;
        public bool expressionCoveragePassed;
        public bool lipSyncMapAssigned;
        public bool amplitudeLipSyncPassed;
        public bool lipSyncSettledAfterPlayback;
        public bool expressionLipSyncLayeringPassed;
        public int faceBlendShapeCount;
        public float blinkPeakLeft;
        public float blinkPeakRight;
        public float blinkSettledLeft;
        public float blinkSettledRight;
        public float blinkDurationObserved;
        public int expressionTestsRequested;
        public int expressionTestsConfigured;
        public int expressionTestsPassed;
        public int expressionTestsSkipped;
        public float amplitudePeakMouthOpen;
        public float amplitudePeakVisemeAA;
        public float amplitudeSettledMouthOpen;
        public float amplitudeSettledVisemeAA;
        public string layeringExpressionName = string.Empty;
        public string layeringUpperFaceBlendShape = string.Empty;
        public float layeringUpperFaceTargetWeight;
        public float layeringUpperFacePeakObserved;
        public string layeringSuppressedLowerFaceBlendShape = string.Empty;
        public float layeringLowerFaceTargetWeight;
        public float layeringLowerFacePeakObserved;
        public float layeringLowerFaceObservedAtLipSyncPeak;
        public float layeringPeakMouthOpenDuringSpeech;
        public string rootFaceMeshName = string.Empty;
        public string facialControllerFaceMeshName = string.Empty;
        public List<string> warnings = new();
        public List<string> errors = new();
        public List<ExpressionValidationResult> expressionResults = new();

        public static BaseAvatarFacialValidationReport Failure(string message)
        {
            var report = new BaseAvatarFacialValidationReport
            {
                validatedAtUtc = DateTime.UtcNow.ToString("O"),
                success = false,
            };
            report.errors.Add(message);
            return report;
        }
    }

    [Serializable]
    public sealed class ExpressionValidationResult
    {
        public string emotion = string.Empty;
        public string definitionName = string.Empty;
        public string status = string.Empty;
        public bool passed;
        public List<string> targetBlendShapes = new();
        public List<string> missingBlendShapes = new();
        public List<BlendShapeObservation> observations = new();

        public BlendShapeObservation FindOrCreateObservation(string blendShapeName, float targetWeight, float passThreshold)
        {
            foreach (var observation in observations)
            {
                if (observation.blendShapeName == blendShapeName)
                {
                    return observation;
                }
            }

            var created = new BlendShapeObservation
            {
                blendShapeName = blendShapeName,
                targetWeight = targetWeight,
                passThreshold = passThreshold,
            };
            observations.Add(created);
            return created;
        }
    }

    [Serializable]
    public sealed class BlendShapeObservation
    {
        public string blendShapeName = string.Empty;
        public float targetWeight;
        public float passThreshold;
        public float maxObservedWeight;
    }
}
