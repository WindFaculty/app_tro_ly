using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;

namespace AvatarSystem.Editor
{
    public static class AnimatorControllerGenerator
    {
        [MenuItem("Tools/AvatarSystem/Generate Base Animator Controller")]
        public static void GenerateController()
        {
            string path = "Assets/AvatarSystem/AvatarProduction/BaseAvatar/Animator/AvatarRootController.controller";
            
            // Ensure directory exists
            string fullPath = Path.Combine(UnityEngine.Application.dataPath, "AvatarSystem/AvatarProduction/BaseAvatar/Animator");
            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
                AssetDatabase.Refresh();
            }

            // Create Animator Controller
            var controller = AnimatorController.CreateAnimatorControllerAtPath(path);

            if (controller == null)
            {
                Debug.LogError($"Failed to create Animator Controller at {path}");
                return;
            }

            // Add Standard Parameters
            controller.AddParameter("IsListening", AnimatorControllerParameterType.Bool);
            controller.AddParameter("IsThinking", AnimatorControllerParameterType.Bool);
            controller.AddParameter("IsSpeaking", AnimatorControllerParameterType.Bool);
            controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
            controller.AddParameter("MoveSpeed", AnimatorControllerParameterType.Float);
            controller.AddParameter("TurnAngle", AnimatorControllerParameterType.Float);
            controller.AddParameter("GestureIndex", AnimatorControllerParameterType.Int);
            controller.AddParameter("EmotionIndex", AnimatorControllerParameterType.Int);
            
            // Create a Base Layer State Machine 
            var rootStateMachine = controller.layers[0].stateMachine;

            // Example simple states, not strictly required but good for prototype
            var idleState = rootStateMachine.AddState("Idle");
            var listenState = rootStateMachine.AddState("Listening");
            var speakState = rootStateMachine.AddState("Speaking");
            
            // Default to Idle state
            rootStateMachine.defaultState = idleState;

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            Debug.Log($"[Success] Animator Controller generated at {path} with standard parameters.");
        }
    }
}
