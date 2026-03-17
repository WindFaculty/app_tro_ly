using UnityEngine;

namespace AvatarSystem
{
    /// <summary>
    /// Root controller for the avatar prefab. Holds references to all
    /// sub-controllers and coordinates initialization order.
    /// Attach this to the top-level GameObject of the avatar prefab.
    /// </summary>
    public sealed class AvatarRootController : MonoBehaviour
    {
        private const string DefaultFaceRendererName = "Body_Head";
        private const string DefaultBodyRendererName = "Body_TorsoUpper";

        [Header("Sub-Controllers (auto-discovered if left empty)")]
        [SerializeField] private AvatarEquipmentManager equipmentManager;
        [SerializeField] private AvatarBodyVisibilityManager bodyVisibilityManager;
        [SerializeField] private AvatarFacialController facialController;
        [SerializeField] private AvatarLipSyncDriver lipSyncDriver;
        [SerializeField] private AvatarAnimatorBridge animatorBridge;
        [SerializeField] private AvatarConversationBridge conversationBridge;
        [SerializeField] private AvatarLocomotionController locomotionController;
        [SerializeField] private AvatarLookAtController lookAtController;
        [SerializeField] private AvatarPresetManager presetManager;

        [Header("References")]
        [SerializeField] private Animator avatarAnimator;
        [SerializeField] private SkinnedMeshRenderer bodyMesh;
        [SerializeField] private SkinnedMeshRenderer faceMesh;
        [SerializeField] private Transform avatarRoot;

        private bool isInitialized;

        public AvatarEquipmentManager Equipment => equipmentManager;
        public AvatarBodyVisibilityManager BodyVisibility => bodyVisibilityManager;
        public AvatarFacialController Facial => facialController;
        public AvatarLipSyncDriver LipSync => lipSyncDriver;
        public AvatarAnimatorBridge AnimatorBridge => animatorBridge;
        public AvatarConversationBridge ConversationBridge => conversationBridge;
        public AvatarLocomotionController Locomotion => locomotionController;
        public AvatarLookAtController LookAt => lookAtController;
        public AvatarPresetManager Presets => presetManager;
        public Animator AvatarAnimator => avatarAnimator;
        public SkinnedMeshRenderer BodyMesh => bodyMesh;
        public SkinnedMeshRenderer FaceMesh => faceMesh;

        private void Awake()
        {
            Initialize();
        }

        public void Initialize()
        {
            if (isInitialized)
            {
                return;
            }

            isInitialized = true;

            if (avatarRoot == null)
            {
                avatarRoot = transform;
            }

            AutoDiscoverControllers();
            InitializeSubSystems();
        }

        private void AutoDiscoverControllers()
        {
            if (equipmentManager == null) equipmentManager = GetComponentInChildren<AvatarEquipmentManager>();
            if (bodyVisibilityManager == null) bodyVisibilityManager = GetComponentInChildren<AvatarBodyVisibilityManager>();
            if (facialController == null) facialController = GetComponentInChildren<AvatarFacialController>();
            if (lipSyncDriver == null) lipSyncDriver = GetComponentInChildren<AvatarLipSyncDriver>();
            if (animatorBridge == null) animatorBridge = GetComponentInChildren<AvatarAnimatorBridge>();
            if (conversationBridge == null) conversationBridge = GetComponentInChildren<AvatarConversationBridge>();
            if (locomotionController == null) locomotionController = GetComponentInChildren<AvatarLocomotionController>();
            if (lookAtController == null) lookAtController = GetComponentInChildren<AvatarLookAtController>();
            if (presetManager == null) presetManager = GetComponentInChildren<AvatarPresetManager>();
            if (avatarAnimator == null) avatarAnimator = GetComponentInChildren<Animator>();
            if (faceMesh == null) faceMesh = FindFaceMeshRenderer();
            if (bodyMesh == null) bodyMesh = FindBodyMeshRenderer(faceMesh);
        }

        private void InitializeSubSystems()
        {
            if (equipmentManager != null)
            {
                equipmentManager.Initialize(this);
            }

            if (bodyVisibilityManager != null)
            {
                bodyVisibilityManager.Initialize(this);
            }

            if (facialController != null)
            {
                facialController.Initialize(faceMesh);
            }

            if (lipSyncDriver != null)
            {
                lipSyncDriver.Initialize(facialController);
            }

            if (animatorBridge != null)
            {
                animatorBridge.Initialize(avatarAnimator);
            }

            if (conversationBridge != null)
            {
                conversationBridge.Initialize(animatorBridge, facialController, lipSyncDriver);
            }

            if (locomotionController != null)
            {
                locomotionController.Initialize(avatarRoot, animatorBridge);
            }
        }

        private SkinnedMeshRenderer FindFaceMeshRenderer()
        {
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
                    renderer.sharedMesh.GetBlendShapeIndex("Blink_L") >= 0 &&
                    renderer.sharedMesh.GetBlendShapeIndex("Blink_R") >= 0)
                {
                    fallback = renderer;
                }
            }

            return fallback;
        }

        private SkinnedMeshRenderer FindBodyMeshRenderer(SkinnedMeshRenderer resolvedFaceMesh)
        {
            foreach (var renderer in GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (renderer != null && renderer.name == DefaultBodyRendererName)
                {
                    return renderer;
                }
            }

            foreach (var renderer in GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (renderer != null && renderer != resolvedFaceMesh)
                {
                    return renderer;
                }
            }

            return null;
        }
    }
}
