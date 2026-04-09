using UnityEngine;
using UnityEditor;

namespace AvatarSystem.Editor
{
    public static class AvatarLookAtSetupTool
    {
        [MenuItem("Tools/AvatarSystem/Connect LookAt Controller to Camera")]
        public static void ConnectLookAtController()
        {
            // 1. Find the Main Camera
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogError("[AvatarSystem] Error: Could not find a camera tagged as 'MainCamera' in the scene.");
                return;
            }

            // 2. Find the LookAt Controller in the Scene
            AvatarLookAtController lookAtController = Object.FindAnyObjectByType<AvatarLookAtController>();
            
            if (lookAtController == null)
            {
                Animator avatarAnimator = Object.FindAnyObjectByType<Animator>();
                if (avatarAnimator != null && avatarAnimator.isHuman)
                {
                    lookAtController = avatarAnimator.gameObject.AddComponent<AvatarLookAtController>();
                    Debug.Log($"[AvatarSystem] Added AvatarLookAtController to {avatarAnimator.gameObject.name}");
                }
                else
                {
                    // Try to instantiate from prefab
                    string prefabPath = "Assets/AvatarSystem/AvatarProduction/BaseAvatar/Prefab/CHR_Avatar_Base_v001.prefab";
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                    if (prefab != null)
                    {
                        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                        avatarAnimator = instance.GetComponent<Animator>();
                        if (avatarAnimator != null && avatarAnimator.isHuman)
                        {
                            lookAtController = avatarAnimator.gameObject.AddComponent<AvatarLookAtController>();
                            Debug.Log($"[AvatarSystem] Instantiated Avatar Prefab and added AvatarLookAtController to {avatarAnimator.gameObject.name}");
                        }
                    }
                    else
                    {
                        Debug.LogError($"[AvatarSystem] Error: Could not find AvatarLookAtController or a valid Humanoid Avatar to attach it to. Also failed to load prefab at: {prefabPath}");
                        return;
                    }
                }
            }

            // 3. Find Bone References (Optional but good to auto-assign if missing)
            Animator animator = lookAtController.GetComponent<Animator>();
            Transform head = null, leftEye = null, rightEye = null, neck = null;

            if (animator != null && animator.isHuman)
            {
                head = animator.GetBoneTransform(HumanBodyBones.Head);
                leftEye = animator.GetBoneTransform(HumanBodyBones.LeftEye);
                rightEye = animator.GetBoneTransform(HumanBodyBones.RightEye);
                neck = animator.GetBoneTransform(HumanBodyBones.Neck);
            }

            // 4. Use SerializedObject to set private fields
            SerializedObject serializedObject = new SerializedObject(lookAtController);
            
            serializedObject.FindProperty("lookAtTarget").objectReferenceValue = mainCamera.transform;
            
            // Auto-assign bones if they are null in the inspector but we found them
            if (serializedObject.FindProperty("headBone").objectReferenceValue == null && head != null)
                serializedObject.FindProperty("headBone").objectReferenceValue = head;
                
            if (serializedObject.FindProperty("eyeLeftBone").objectReferenceValue == null && leftEye != null)
                serializedObject.FindProperty("eyeLeftBone").objectReferenceValue = leftEye;
                
            if (serializedObject.FindProperty("eyeRightBone").objectReferenceValue == null && rightEye != null)
                serializedObject.FindProperty("eyeRightBone").objectReferenceValue = rightEye;
                
            if (serializedObject.FindProperty("neckBone").objectReferenceValue == null && neck != null)
                serializedObject.FindProperty("neckBone").objectReferenceValue = neck;


            serializedObject.ApplyModifiedProperties();
            
            Undo.RecordObject(lookAtController, "Connect LookAt Camera Target");
            Selection.activeGameObject = lookAtController.gameObject;

            Debug.Log($"[Success] LookAt Target successfully set to {mainCamera.name}.");
        }
    }
}
