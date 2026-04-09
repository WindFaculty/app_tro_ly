#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AvatarSystem.Editor
{
    /// <summary>
    /// Generates prototype avatar animations directly from the current humanoid rig.
    /// </summary>
    public static class BaseAvatarAnimationLibraryBuilder
    {
        public const string BaseAvatarModelPath = "Assets/AvatarSystem/AvatarProduction/BaseAvatar/Model/CHR_Avatar_Base_v001.fbx";
        public const string AnimatorControllerPath = "Assets/AvatarSystem/AvatarProduction/BaseAvatar/Animator/CHR_Avatar_Base_v001_Base.controller";
        public const string AnimationFolderPath = "Assets/AvatarSystem/AvatarProduction/BaseAvatar/Animations";

        private const string ReportRelativePath = "Logs/BaseAvatarAnimationLibraryReport.json";
        private const string GestureNoneClipName = "Gesture_None";
        private const float DefaultTransitionDuration = 0.08f;
        private const float TurnThreshold = 20f;
        private const float WalkThreshold = 0.8f;

        private static readonly ClipDefinition[] RequiredClips =
        {
            new("Idle_Default", 2f, true, PopulateIdleDefault),
            new("Idle_Breathing", 2.4f, true, PopulateIdleBreathing),
            new("Listen_Idle", 2f, true, PopulateListenIdle),
            new("Talk_Idle", 1.6f, true, PopulateTalkIdle),
            new("Walk_Forward", 1f, true, PopulateWalkForward),
            new("Turn_Left", 0.85f, true, PopulateTurnLeft),
            new("Turn_Right", 0.85f, true, PopulateTurnRight),
            new("Approach_Short", 0.75f, true, PopulateApproachShort),
            new("Wave_Small", 1.4f, true, PopulateWaveSmall),
            new("Nod_Yes", 1f, true, PopulateNodYes),
            new("HandExplain_01", 1.6f, true, PopulateHandExplain01),
        };

        private static readonly HumanBodyBones[] RequiredBones =
        {
            HumanBodyBones.Hips,
            HumanBodyBones.Spine,
            HumanBodyBones.Chest,
            HumanBodyBones.UpperChest,
            HumanBodyBones.Neck,
            HumanBodyBones.Head,
            HumanBodyBones.LeftUpperArm,
            HumanBodyBones.RightUpperArm,
            HumanBodyBones.LeftLowerArm,
            HumanBodyBones.RightLowerArm,
            HumanBodyBones.LeftHand,
            HumanBodyBones.RightHand,
            HumanBodyBones.LeftUpperLeg,
            HumanBodyBones.RightUpperLeg,
            HumanBodyBones.LeftLowerLeg,
            HumanBodyBones.RightLowerLeg,
            HumanBodyBones.LeftFoot,
            HumanBodyBones.RightFoot,
        };

        [MenuItem("Tools/AvatarSystem/Build Prototype Animation Library %#m")]
        public static void BuildPrototypeAnimationLibrary()
        {
            Report report = BuildAnimationLibrary();
            PersistReport(report);
            EmitLogs(report);
        }

        public static void BuildPrototypeAnimationLibraryCli()
        {
            Report report = BuildAnimationLibrary();
            PersistReport(report);
            EmitLogs(report);

            if (!report.success)
            {
                throw new InvalidOperationException("Prototype animation library build failed.");
            }
        }

        public static Report BuildAnimationLibrary()
        {
            var report = new Report
            {
                builtAtUtc = DateTime.UtcNow.ToString("O"),
                modelAssetPath = BaseAvatarModelPath,
                animatorControllerPath = AnimatorControllerPath,
                animationFolderPath = AnimationFolderPath,
            };

            Directory.CreateDirectory(GetProjectRelativePath("Logs"));

            GameObject modelRoot = AssetDatabase.LoadAssetAtPath<GameObject>(BaseAvatarModelPath);
            if (modelRoot == null)
            {
                report.errors.Add($"Could not load model asset at '{BaseAvatarModelPath}'.");
                return report;
            }

            Directory.CreateDirectory(GetProjectRelativePath(AnimationFolderPath));

            Scene previewScene = EditorSceneManager.NewPreviewScene();
            GameObject instance = null;

            try
            {
                instance = PrefabUtility.InstantiatePrefab(modelRoot, previewScene) as GameObject;
                if (instance == null)
                {
                    report.errors.Add("Failed to instantiate the base avatar model in a preview scene.");
                    return report;
                }

                instance.name = "BaseAvatarAnimationPreview";

                Animator animator = instance.GetComponent<Animator>();
                if (animator == null)
                {
                    animator = instance.AddComponent<Animator>();
                }

                if (animator.avatar == null)
                {
                    animator.avatar = LoadAvatarSubAsset(BaseAvatarModelPath);
                }

                if (animator.avatar == null || !animator.avatar.isHuman || !animator.avatar.isValid)
                {
                    report.errors.Add("The imported avatar is missing or not a valid humanoid avatar.");
                    return report;
                }

                var rig = RigContext.Create(instance.transform, animator, report);
                Dictionary<string, AnimationClip> clips = GenerateClips(rig, report);
                if (clips.Count != RequiredClips.Length)
                {
                    report.errors.Add("One or more required clips could not be generated.");
                    return report;
                }

                AnimationClip gestureNone = EnsureGestureNoneClip();
                if (gestureNone == null)
                {
                    report.errors.Add("Could not create the Gesture_None clip required by the controller.");
                    return report;
                }

                AnimatorController controller = ConfigureAnimatorController(clips, gestureNone, report);
                report.controllerStateNames = controller.layers[0].stateMachine.states
                    .Select(child => child.state != null ? child.state.name : string.Empty)
                    .Where(name => !string.IsNullOrEmpty(name))
                    .OrderBy(name => name)
                    .ToArray();
                report.generatedClipNames = clips.Keys.OrderBy(name => name).ToArray();
                report.generatedClipPaths = clips
                    .OrderBy(pair => pair.Key)
                    .Select(pair => AssetDatabase.GetAssetPath(pair.Value))
                    .ToArray();

                ValidateGeneratedLibrary(clips, controller, report);
                report.success = report.errors.Count == 0;
                return report;
            }
            catch (Exception exception)
            {
                report.errors.Add(exception.ToString());
                return report;
            }
            finally
            {
                if (instance != null)
                {
                    UnityEngine.Object.DestroyImmediate(instance);
                }

                EditorSceneManager.ClosePreviewScene(previewScene);
            }
        }

        private static Dictionary<string, AnimationClip> GenerateClips(RigContext rig, Report report)
        {
            var clips = new Dictionary<string, AnimationClip>(StringComparer.Ordinal);

            foreach (ClipDefinition definition in RequiredClips)
            {
                string clipPath = $"{AnimationFolderPath}/{definition.name}.anim";
                AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
                if (clip == null)
                {
                    clip = new AnimationClip { name = definition.name };
                    AssetDatabase.CreateAsset(clip, clipPath);
                }

                clip.ClearCurves();
                clip.frameRate = 60f;
                clip.name = definition.name;

                definition.populate(clip, rig);
                clip.EnsureQuaternionContinuity();
                ApplyClipSettings(clip, definition.durationSeconds, definition.loop);

                EditorUtility.SetDirty(clip);
                AssetDatabase.ImportAsset(clipPath, ImportAssetOptions.ForceUpdate);

                AnimationClip savedClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
                if (savedClip == null)
                {
                    report.errors.Add($"Failed to save generated clip at '{clipPath}'.");
                    continue;
                }

                clips[definition.name] = savedClip;
            }

            AssetDatabase.SaveAssets();
            return clips;
        }

        private static AnimatorController ConfigureAnimatorController(
            IReadOnlyDictionary<string, AnimationClip> clips,
            AnimationClip gestureNoneClip,
            Report report)
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(AnimatorControllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(AnimatorControllerPath);
            }

            EnsureParameters(controller);

            AnimatorControllerLayer layer = controller.layers[0];
            layer.name = "Base Layer";
            AnimatorStateMachine stateMachine = layer.stateMachine;
            ClearStateMachine(stateMachine);

            AnimatorState idleState = CreateState(stateMachine, "Idle_Default", clips["Idle_Default"], new Vector3(240f, 0f, 0f));
            AnimatorState breathingState = CreateState(stateMachine, "Idle_Breathing", clips["Idle_Breathing"], new Vector3(480f, -60f, 0f));
            AnimatorState listenState = CreateState(stateMachine, "Listen_Idle", clips["Listen_Idle"], new Vector3(480f, 40f, 0f));
            AnimatorState talkState = CreateState(stateMachine, "Talk_Idle", clips["Talk_Idle"], new Vector3(480f, 140f, 0f));
            AnimatorState walkState = CreateState(stateMachine, "Walk_Forward", clips["Walk_Forward"], new Vector3(480f, 260f, 0f));
            AnimatorState turnLeftState = CreateState(stateMachine, "Turn_Left", clips["Turn_Left"], new Vector3(740f, 200f, 0f));
            AnimatorState turnRightState = CreateState(stateMachine, "Turn_Right", clips["Turn_Right"], new Vector3(740f, 320f, 0f));
            AnimatorState approachState = CreateState(stateMachine, "Approach_Short", clips["Approach_Short"], new Vector3(740f, 440f, 0f));
            AnimatorState waveState = CreateState(stateMachine, "Wave_Small", clips["Wave_Small"], new Vector3(1020f, 0f, 0f));
            AnimatorState nodState = CreateState(stateMachine, "Nod_Yes", clips["Nod_Yes"], new Vector3(1020f, 120f, 0f));
            AnimatorState explainState = CreateState(stateMachine, "HandExplain_01", clips["HandExplain_01"], new Vector3(1020f, 240f, 0f));
            AnimatorState gestureNoneState = CreateState(stateMachine, GestureNoneClipName, gestureNoneClip, new Vector3(1020f, 360f, 0f));

            stateMachine.defaultState = idleState;

            AddGestureTransition(stateMachine, waveState, (int)GestureType.Wave);
            AddGestureTransition(stateMachine, nodState, (int)GestureType.Nod);
            AddGestureTransition(stateMachine, explainState, (int)GestureType.Explain);
            AddGestureTransition(stateMachine, gestureNoneState, (int)GestureType.None);

            AnimatorStateTransition turnLeftTransition = AddAnyStateTransition(stateMachine, turnLeftState);
            turnLeftTransition.AddCondition(AnimatorConditionMode.If, 0f, "IsMoving");
            turnLeftTransition.AddCondition(AnimatorConditionMode.Less, -TurnThreshold, "TurnAngle");

            AnimatorStateTransition turnRightTransition = AddAnyStateTransition(stateMachine, turnRightState);
            turnRightTransition.AddCondition(AnimatorConditionMode.If, 0f, "IsMoving");
            turnRightTransition.AddCondition(AnimatorConditionMode.Greater, TurnThreshold, "TurnAngle");

            AnimatorStateTransition walkTransition = AddAnyStateTransition(stateMachine, walkState);
            walkTransition.AddCondition(AnimatorConditionMode.If, 0f, "IsMoving");
            walkTransition.AddCondition(AnimatorConditionMode.Greater, WalkThreshold, "MoveSpeed");
            walkTransition.AddCondition(AnimatorConditionMode.Greater, -TurnThreshold, "TurnAngle");
            walkTransition.AddCondition(AnimatorConditionMode.Less, TurnThreshold, "TurnAngle");

            AnimatorStateTransition approachTransition = AddAnyStateTransition(stateMachine, approachState);
            approachTransition.AddCondition(AnimatorConditionMode.If, 0f, "IsMoving");
            approachTransition.AddCondition(AnimatorConditionMode.Less, WalkThreshold + 0.001f, "MoveSpeed");

            AnimatorStateTransition talkTransition = AddAnyStateTransition(stateMachine, talkState);
            talkTransition.AddCondition(AnimatorConditionMode.If, 0f, "IsSpeaking");
            talkTransition.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsMoving");

            AnimatorStateTransition listenTransition = AddAnyStateTransition(stateMachine, listenState);
            listenTransition.AddCondition(AnimatorConditionMode.If, 0f, "IsListening");
            listenTransition.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsSpeaking");
            listenTransition.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsMoving");

            AnimatorStateTransition breathingTransition = AddAnyStateTransition(stateMachine, breathingState);
            breathingTransition.AddCondition(AnimatorConditionMode.If, 0f, "IsThinking");
            breathingTransition.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsSpeaking");
            breathingTransition.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsListening");
            breathingTransition.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsMoving");

            AnimatorStateTransition idleTransition = AddAnyStateTransition(stateMachine, idleState);
            idleTransition.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsThinking");
            idleTransition.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsListening");
            idleTransition.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsSpeaking");
            idleTransition.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsMoving");

            layer.stateMachine = stateMachine;
            layer.defaultWeight = 1f;
            controller.layers[0] = layer;

            EditorUtility.SetDirty(stateMachine);
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            report.controllerGuid = AssetDatabase.AssetPathToGUID(AnimatorControllerPath);
            return controller;
        }

        private static void ValidateGeneratedLibrary(
            IReadOnlyDictionary<string, AnimationClip> clips,
            AnimatorController controller,
            Report report)
        {
            var stateBindings = controller.layers[0].stateMachine.states
                .Where(child => child.state != null)
                .ToDictionary(
                    child => child.state.name,
                    child => child.state.motion as AnimationClip,
                    StringComparer.Ordinal);

            foreach (ClipDefinition definition in RequiredClips)
            {
                if (!clips.TryGetValue(definition.name, out AnimationClip clip) || clip == null)
                {
                    report.errors.Add($"Missing generated clip '{definition.name}'.");
                    continue;
                }

                if (!stateBindings.TryGetValue(definition.name, out AnimationClip boundClip) || boundClip != clip)
                {
                    report.errors.Add($"Animator state '{definition.name}' is missing or not bound to the expected clip.");
                }
            }

            if (controller.layers[0].stateMachine.defaultState == null ||
                controller.layers[0].stateMachine.defaultState.name != "Idle_Default")
            {
                report.errors.Add("Animator default state is not Idle_Default.");
            }
        }

        private static void EnsureParameters(AnimatorController controller)
        {
            EnsureParameter(controller, "IsListening", AnimatorControllerParameterType.Bool);
            EnsureParameter(controller, "IsThinking", AnimatorControllerParameterType.Bool);
            EnsureParameter(controller, "IsSpeaking", AnimatorControllerParameterType.Bool);
            EnsureParameter(controller, "IsMoving", AnimatorControllerParameterType.Bool);
            EnsureParameter(controller, "MoveSpeed", AnimatorControllerParameterType.Float);
            EnsureParameter(controller, "TurnAngle", AnimatorControllerParameterType.Float);
            EnsureParameter(controller, "GestureIndex", AnimatorControllerParameterType.Int);
            EnsureParameter(controller, "EmotionIndex", AnimatorControllerParameterType.Int);
        }

        private static void EnsureParameter(
            AnimatorController controller,
            string name,
            AnimatorControllerParameterType type)
        {
            if (controller.parameters.Any(parameter => parameter.name == name))
            {
                return;
            }

            controller.AddParameter(name, type);
        }

        private static void ClearStateMachine(AnimatorStateMachine stateMachine)
        {
            foreach (ChildAnimatorState child in stateMachine.states.ToArray())
            {
                if (child.state != null)
                {
                    stateMachine.RemoveState(child.state);
                }
            }

            foreach (ChildAnimatorStateMachine child in stateMachine.stateMachines.ToArray())
            {
                if (child.stateMachine != null)
                {
                    stateMachine.RemoveStateMachine(child.stateMachine);
                }
            }

            foreach (AnimatorTransition transition in stateMachine.entryTransitions.ToArray())
            {
                stateMachine.RemoveEntryTransition(transition);
            }

            foreach (AnimatorStateTransition transition in stateMachine.anyStateTransitions.ToArray())
            {
                stateMachine.RemoveAnyStateTransition(transition);
            }
        }

        private static AnimatorState CreateState(
            AnimatorStateMachine stateMachine,
            string stateName,
            Motion motion,
            Vector3 position)
        {
            AnimatorState state = stateMachine.AddState(stateName, position);
            state.motion = motion;
            state.speed = 1f;
            state.writeDefaultValues = true;
            return state;
        }

        private static AnimatorStateTransition AddAnyStateTransition(AnimatorStateMachine stateMachine, AnimatorState destination)
        {
            AnimatorStateTransition transition = stateMachine.AddAnyStateTransition(destination);
            transition.hasExitTime = false;
            transition.hasFixedDuration = true;
            transition.duration = DefaultTransitionDuration;
            transition.canTransitionToSelf = false;
            return transition;
        }

        private static void AddGestureTransition(
            AnimatorStateMachine stateMachine,
            AnimatorState destination,
            int gestureIndex)
        {
            AnimatorStateTransition transition = AddAnyStateTransition(stateMachine, destination);
            transition.AddCondition(AnimatorConditionMode.Equals, gestureIndex, "GestureIndex");
        }

        private static AnimationClip EnsureGestureNoneClip()
        {
            string path = $"{AnimationFolderPath}/{GestureNoneClipName}.anim";
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null)
            {
                clip = new AnimationClip { name = GestureNoneClipName };
                AssetDatabase.CreateAsset(clip, path);
            }

            clip.ClearCurves();
            clip.frameRate = 60f;
            clip.name = GestureNoneClipName;
            ApplyClipSettings(clip, 1f, true);
            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            return AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        }

        private static void ApplyClipSettings(AnimationClip clip, float durationSeconds, bool loopTime)
        {
            var serializedObject = new SerializedObject(clip);
            SerializedProperty settings = serializedObject.FindProperty("m_AnimationClipSettings");
            if (settings != null)
            {
                settings.FindPropertyRelative("m_StartTime").floatValue = 0f;
                settings.FindPropertyRelative("m_StopTime").floatValue = durationSeconds;
                settings.FindPropertyRelative("m_LoopTime").boolValue = loopTime;
                settings.FindPropertyRelative("m_LoopBlend").boolValue = false;
                settings.FindPropertyRelative("m_KeepOriginalPositionY").boolValue = true;
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void PopulateIdleDefault(AnimationClip clip, RigContext rig)
        {
            SetPositionOffsets(clip, rig, HumanBodyBones.Hips,
                new TimedVector(0f, Vector3.zero),
                new TimedVector(1f, new Vector3(0f, 0.010f, 0f)),
                new TimedVector(2f, Vector3.zero));
            SetRotationOffsets(clip, rig, HumanBodyBones.Chest,
                new TimedVector(0f, new Vector3(0.5f, -0.8f, 0.3f)),
                new TimedVector(1f, new Vector3(-0.4f, 0.8f, -0.3f)),
                new TimedVector(2f, new Vector3(0.5f, -0.8f, 0.3f)));
            SetRotationOffsets(clip, rig, HumanBodyBones.Head,
                new TimedVector(0f, new Vector3(0.3f, -1.2f, 0f)),
                new TimedVector(1f, new Vector3(-0.2f, 1.2f, 0f)),
                new TimedVector(2f, new Vector3(0.3f, -1.2f, 0f)));
        }

        private static void PopulateIdleBreathing(AnimationClip clip, RigContext rig)
        {
            SetPositionOffsets(clip, rig, HumanBodyBones.Hips,
                new TimedVector(0f, Vector3.zero),
                new TimedVector(1.2f, new Vector3(0f, 0.014f, 0f)),
                new TimedVector(2.4f, Vector3.zero));
            SetRotationOffsets(clip, rig, HumanBodyBones.Spine,
                new TimedVector(0f, new Vector3(-0.5f, 0f, 0f)),
                new TimedVector(1.2f, new Vector3(1.8f, 0f, 0f)),
                new TimedVector(2.4f, new Vector3(-0.5f, 0f, 0f)));
            SetRotationOffsets(clip, rig, HumanBodyBones.UpperChest,
                new TimedVector(0f, new Vector3(0f, -0.5f, 0f)),
                new TimedVector(1.2f, new Vector3(2.6f, 0.5f, 0f)),
                new TimedVector(2.4f, new Vector3(0f, -0.5f, 0f)));
            SetRotationOffsets(clip, rig, HumanBodyBones.Head,
                new TimedVector(0f, new Vector3(0.6f, 0f, 0f)),
                new TimedVector(1.2f, new Vector3(1.4f, 0f, 0f)),
                new TimedVector(2.4f, new Vector3(0.6f, 0f, 0f)));
        }

        private static void PopulateListenIdle(AnimationClip clip, RigContext rig)
        {
            SetPositionOffsets(clip, rig, HumanBodyBones.Hips,
                new TimedVector(0f, new Vector3(0f, 0f, 0.004f)),
                new TimedVector(1f, new Vector3(0f, 0.008f, 0.010f)),
                new TimedVector(2f, new Vector3(0f, 0f, 0.004f)));
            SetRotationOffsets(clip, rig, HumanBodyBones.Chest,
                new TimedVector(0f, new Vector3(4f, -1.5f, 0f)),
                new TimedVector(1f, new Vector3(5.5f, 1.8f, 0f)),
                new TimedVector(2f, new Vector3(4f, -1.5f, 0f)));
            SetRotationOffsets(clip, rig, HumanBodyBones.Head,
                new TimedVector(0f, new Vector3(-1.5f, -4f, 0f)),
                new TimedVector(1f, new Vector3(0.5f, 4.5f, 0f)),
                new TimedVector(2f, new Vector3(-1.5f, -4f, 0f)));
            SetRotationOffsets(clip, rig, HumanBodyBones.LeftUpperArm,
                new TimedVector(0f, new Vector3(1f, 0f, -4f)),
                new TimedVector(1f, new Vector3(2f, 0f, -6f)),
                new TimedVector(2f, new Vector3(1f, 0f, -4f)));
            SetRotationOffsets(clip, rig, HumanBodyBones.RightUpperArm,
                new TimedVector(0f, new Vector3(1f, 0f, 4f)),
                new TimedVector(1f, new Vector3(2f, 0f, 6f)),
                new TimedVector(2f, new Vector3(1f, 0f, 4f)));
        }

        private static void PopulateTalkIdle(AnimationClip clip, RigContext rig)
        {
            SetPositionOffsets(clip, rig, HumanBodyBones.Hips,
                new TimedVector(0f, Vector3.zero),
                new TimedVector(0.8f, new Vector3(0f, 0.012f, 0f)),
                new TimedVector(1.6f, Vector3.zero));
            SetRotationOffsets(clip, rig, HumanBodyBones.Chest,
                new TimedVector(0f, new Vector3(1f, -2f, 0f)),
                new TimedVector(0.4f, new Vector3(4f, 2f, 0.6f)),
                new TimedVector(0.8f, new Vector3(-1.5f, -1f, -0.4f)),
                new TimedVector(1.2f, new Vector3(3f, 1.5f, 0.4f)),
                new TimedVector(1.6f, new Vector3(1f, -2f, 0f)));
            SetRotationOffsets(clip, rig, HumanBodyBones.Head,
                new TimedVector(0f, new Vector3(0f, -2f, 0f)),
                new TimedVector(0.4f, new Vector3(2f, 2.5f, 0f)),
                new TimedVector(0.8f, new Vector3(-1f, -0.5f, 0f)),
                new TimedVector(1.2f, new Vector3(1.5f, 1.5f, 0f)),
                new TimedVector(1.6f, new Vector3(0f, -2f, 0f)));
            SetRotationOffsets(clip, rig, HumanBodyBones.RightUpperArm,
                new TimedVector(0f, new Vector3(2f, 0f, 8f)),
                new TimedVector(0.8f, new Vector3(6f, 0f, 16f)),
                new TimedVector(1.6f, new Vector3(2f, 0f, 8f)));
        }

        private static void PopulateWalkForward(AnimationClip clip, RigContext rig)
        {
            SetPositionOffsets(clip, rig, HumanBodyBones.Hips,
                new TimedVector(0f, new Vector3(0f, 0.004f, 0f)),
                new TimedVector(0.25f, new Vector3(0f, 0.018f, 0f)),
                new TimedVector(0.5f, new Vector3(0f, 0.004f, 0f)),
                new TimedVector(0.75f, new Vector3(0f, 0.018f, 0f)),
                new TimedVector(1f, new Vector3(0f, 0.004f, 0f)));
            SetRotationOffsets(clip, rig, HumanBodyBones.LeftUpperLeg,
                new TimedVector(0f, new Vector3(16f, 0f, 0f)),
                new TimedVector(0.5f, new Vector3(-18f, 0f, 0f)),
                new TimedVector(1f, new Vector3(16f, 0f, 0f)));
            SetRotationOffsets(clip, rig, HumanBodyBones.RightUpperLeg,
                new TimedVector(0f, new Vector3(-18f, 0f, 0f)),
                new TimedVector(0.5f, new Vector3(16f, 0f, 0f)),
                new TimedVector(1f, new Vector3(-18f, 0f, 0f)));
            SetRotationOffsets(clip, rig, HumanBodyBones.LeftLowerLeg,
                new TimedVector(0f, new Vector3(8f, 0f, 0f)),
                new TimedVector(0.5f, new Vector3(16f, 0f, 0f)),
                new TimedVector(1f, new Vector3(8f, 0f, 0f)));
            SetRotationOffsets(clip, rig, HumanBodyBones.RightLowerLeg,
                new TimedVector(0f, new Vector3(16f, 0f, 0f)),
                new TimedVector(0.5f, new Vector3(8f, 0f, 0f)),
                new TimedVector(1f, new Vector3(16f, 0f, 0f)));
            SetRotationOffsets(clip, rig, HumanBodyBones.LeftUpperArm,
                new TimedVector(0f, new Vector3(-12f, 0f, -6f)),
                new TimedVector(0.5f, new Vector3(14f, 0f, -2f)),
                new TimedVector(1f, new Vector3(-12f, 0f, -6f)));
            SetRotationOffsets(clip, rig, HumanBodyBones.RightUpperArm,
                new TimedVector(0f, new Vector3(14f, 0f, 6f)),
                new TimedVector(0.5f, new Vector3(-12f, 0f, 2f)),
                new TimedVector(1f, new Vector3(14f, 0f, 6f)));
        }

        private static void PopulateTurnLeft(AnimationClip clip, RigContext rig)
        {
            SetPositionOffsets(clip, rig, HumanBodyBones.Hips,
                new TimedVector(0f, Vector3.zero),
                new TimedVector(0.425f, new Vector3(-0.008f, 0.010f, 0f)),
                new TimedVector(0.85f, Vector3.zero));
            SetRotationOffsets(clip, rig, HumanBodyBones.Hips,
                new TimedVector(0f, new Vector3(0f, -8f, 0f)),
                new TimedVector(0.425f, new Vector3(0f, -20f, 0f)),
                new TimedVector(0.85f, new Vector3(0f, -8f, 0f)));
            SetRotationOffsets(clip, rig, HumanBodyBones.Chest,
                new TimedVector(0f, new Vector3(1f, -10f, 0f)),
                new TimedVector(0.425f, new Vector3(3f, -28f, 0f)),
                new TimedVector(0.85f, new Vector3(1f, -10f, 0f)));
            SetRotationOffsets(clip, rig, HumanBodyBones.Head,
                new TimedVector(0f, new Vector3(0f, -8f, 0f)),
                new TimedVector(0.425f, new Vector3(2f, -18f, 0f)),
                new TimedVector(0.85f, new Vector3(0f, -8f, 0f)));
        }

        private static void PopulateTurnRight(AnimationClip clip, RigContext rig) { }
        private static void PopulateApproachShort(AnimationClip clip, RigContext rig) { }
        private static void PopulateWaveSmall(AnimationClip clip, RigContext rig) { }
        private static void PopulateNodYes(AnimationClip clip, RigContext rig) { }
        private static void PopulateHandExplain01(AnimationClip clip, RigContext rig) { }

        private static void SetRotationOffsets(
            AnimationClip clip,
            RigContext rig,
            HumanBodyBones bone,
            params TimedVector[] keyframes)
        {
            throw new NotImplementedException();
        }

        private static void SetPositionOffsets(
            AnimationClip clip,
            RigContext rig,
            HumanBodyBones bone,
            params TimedVector[] keyframes)
        {
            throw new NotImplementedException();
        }

        private static Avatar LoadAvatarSubAsset(string assetPath)
        {
            throw new NotImplementedException();
        }

        private static void PersistReport(Report report)
        {
            throw new NotImplementedException();
        }

        private static void EmitLogs(Report report)
        {
            throw new NotImplementedException();
        }

        private static string GetProjectRelativePath(string relativePath)
        {
            throw new NotImplementedException();
        }

        private readonly struct ClipDefinition
        {
            public ClipDefinition(string name, float durationSeconds, bool loop, Action<AnimationClip, RigContext> populate)
            {
                this.name = name;
                this.durationSeconds = durationSeconds;
                this.loop = loop;
                this.populate = populate;
            }

            public readonly string name;
            public readonly float durationSeconds;
            public readonly bool loop;
            public readonly Action<AnimationClip, RigContext> populate;
        }

        private readonly struct TimedVector
        {
            public TimedVector(float time, Vector3 value)
            {
                this.time = time;
                this.value = value;
            }

            public readonly float time;
            public readonly Vector3 value;
        }

        private readonly struct BoneBinding
        {
            public BoneBinding(string path, Vector3 localPosition, Quaternion localRotation)
            {
                this.path = path;
                this.localPosition = localPosition;
                this.localRotation = localRotation;
            }

            public readonly string path;
            public readonly Vector3 localPosition;
            public readonly Quaternion localRotation;
        }

        private sealed class RigContext
        {
            public static RigContext Create(Transform root, Animator animator, Report report)
            {
                throw new NotImplementedException();
            }

            public bool TryGetBinding(HumanBodyBones bone, out BoneBinding binding)
            {
                throw new NotImplementedException();
            }
        }

        [Serializable]
        public sealed class Report
        {
            public string builtAtUtc = string.Empty;
            public string modelAssetPath = string.Empty;
            public string animatorControllerPath = string.Empty;
            public string animationFolderPath = string.Empty;
            public string controllerGuid = string.Empty;
            public string[] generatedClipNames = Array.Empty<string>();
            public string[] generatedClipPaths = Array.Empty<string>();
            public string[] controllerStateNames = Array.Empty<string>();
            public List<string> boundBonePaths = new();
            public List<string> warnings = new();
            public List<string> errors = new();
            public bool success;
        }
    }
}
#endif
