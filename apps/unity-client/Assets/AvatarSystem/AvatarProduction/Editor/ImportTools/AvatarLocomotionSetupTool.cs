using UnityEngine;
using UnityEditor;

namespace AvatarSystem.Editor
{
    public static class AvatarLocomotionSetupTool
    {
        [MenuItem("Tools/AvatarSystem/Connect Anchor Points to Locomotion Controller")]
        public static void ConnectAnchorPoints()
        {
            // 1. Find the Anchor Points Container
            GameObject anchorRoot = GameObject.Find("AvatarAnchorPoints");
            if (anchorRoot == null)
            {
                Debug.LogError("[AvatarSystem] Error: Could not find 'AvatarAnchorPoints' in the scene. Please generate them first.");
                return;
            }

            // Find individual points
            Transform idlePoint = anchorRoot.transform.Find("IdlePoint");
            Transform talkPoint = anchorRoot.transform.Find("TalkPoint");
            Transform listenPoint = anchorRoot.transform.Find("ListenPoint");
            Transform wanderA = anchorRoot.transform.Find("WanderA");
            Transform wanderB = anchorRoot.transform.Find("WanderB");

            if (idlePoint == null || talkPoint == null || listenPoint == null || wanderA == null || wanderB == null)
            {
                Debug.LogError("[AvatarSystem] Error: One or more anchor points are missing under 'AvatarAnchorPoints'.");
                return;
            }

            // 2. Find the Locomotion Controller in the Scene
            // Since we don't know the exact name of the Avatar instance (it could be "CHR_Avatar_Base_v001" or "BaseAvatarAnimationPreview"),
            // we search for the component itself.
            AvatarLocomotionController locomotionController = Object.FindAnyObjectByType<AvatarLocomotionController>();
            
            if (locomotionController == null)
            {
                // If it doesn't exist, try to find an avatar base and add it.
                Animator avatarAnimator = Object.FindAnyObjectByType<Animator>();
                if (avatarAnimator != null && avatarAnimator.isHuman)
                {
                    locomotionController = avatarAnimator.gameObject.AddComponent<AvatarLocomotionController>();
                    Debug.Log($"[AvatarSystem] Added AvatarLocomotionController to {avatarAnimator.gameObject.name}");
                }
                    // Try to instantiate from prefab
                    string prefabPath = "Assets/AvatarSystem/AvatarProduction/BaseAvatar/Prefab/CHR_Avatar_Base_v001.prefab";
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                    if (prefab != null)
                    {
                        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                        avatarAnimator = instance.GetComponent<Animator>();
                        if (avatarAnimator != null && avatarAnimator.isHuman)
                        {
                            locomotionController = avatarAnimator.gameObject.AddComponent<AvatarLocomotionController>();
                            Debug.Log($"[AvatarSystem] Instantiated Avatar Prefab and added AvatarLocomotionController to {avatarAnimator.gameObject.name}");
                        }
                    }
                    else
                    {
                        Debug.LogError($"[AvatarSystem] Error: Could not find AvatarLocomotionController or a valid Humanoid Avatar to attach it to. Also failed to load prefab at: {prefabPath}");
                        return;
                    }
            }

            // 3. Use SerializedObject to set private fields
            SerializedObject serializedObject = new SerializedObject(locomotionController);
            
            serializedObject.FindProperty("idlePoint").objectReferenceValue = idlePoint;
            serializedObject.FindProperty("talkPoint").objectReferenceValue = talkPoint;
            serializedObject.FindProperty("listenPoint").objectReferenceValue = listenPoint;
            serializedObject.FindProperty("wanderPointA").objectReferenceValue = wanderA;
            serializedObject.FindProperty("wanderPointB").objectReferenceValue = wanderB;

            serializedObject.ApplyModifiedProperties();
            
            // Register Undo so user can revert easily
            Undo.RecordObject(locomotionController, "Connect Locomotion Anchor Points");
            Selection.activeGameObject = locomotionController.gameObject;

            Debug.Log($"[Success] Anchor points successfully assigned to {locomotionController.gameObject.name}.");
        }
    }
}
