using LocalAssistant.World.Interaction;
using LocalAssistant.World.Room;
using UnityEngine;
using AvatarSystem;

namespace LocalAssistant.Avatar
{
    public sealed class CharacterRoomBridge : MonoBehaviour
    {
        private const string ProxyRootName = "CharacterAvatarProxy";

        [SerializeField] private float proxyTurnSpeed = 5.5f;
        [SerializeField] private float proxyScale = 0.92f;

        private AvatarStateMachine avatarStateMachine;
        private RoomWorldController roomWorldController;
        private RoomInteractionController roomInteractionController;
        private AvatarConversationBridge avatarConversationBridge;
        private AvatarRootController avatarRootController;

        private Transform activeAvatarRoot;
        private Transform proxyRoot;
        private Renderer proxyRenderer;
        private Transform attentionTarget;
        private Vector3 defaultFacingForward = Vector3.forward;
        private bool hasAttentionTarget;

        public Transform ActiveAvatarRoot => activeAvatarRoot;
        public bool UsingProxyAvatar => proxyRoot != null && activeAvatarRoot == proxyRoot;

        private void Awake()
        {
            EnsureAttentionTarget();
        }

        private void OnDestroy()
        {
            if (roomInteractionController != null)
            {
                roomInteractionController.SelectionChanged -= HandleRoomSelectionChanged;
            }
        }

        private void Update()
        {
            if (activeAvatarRoot == null)
            {
                return;
            }

            UpdateAttention();
        }

        public void Bind(
            AvatarStateMachine stateMachine,
            RoomWorldController worldController,
            RoomInteractionController interactionController,
            AvatarConversationBridge conversationBridge,
            AvatarRootController rootController)
        {
            EnsureAttentionTarget();
            avatarStateMachine = stateMachine;
            roomWorldController = worldController;
            avatarConversationBridge = conversationBridge;
            avatarRootController = rootController;

            if (roomInteractionController != null)
            {
                roomInteractionController.SelectionChanged -= HandleRoomSelectionChanged;
            }

            roomInteractionController = interactionController;
            if (roomInteractionController != null)
            {
                roomInteractionController.SelectionChanged += HandleRoomSelectionChanged;
            }

            ResolveAvatarRoot();
            MoveAvatarToSpawn();
            ApplyDefaultAttention();
        }

        private void ResolveAvatarRoot()
        {
            if (avatarRootController != null)
            {
                activeAvatarRoot = avatarRootController.transform;
                return;
            }

            if (avatarConversationBridge != null)
            {
                activeAvatarRoot = avatarConversationBridge.transform.root;
                return;
            }

            EnsureProxyAvatar();
            activeAvatarRoot = proxyRoot;
        }

        private void EnsureProxyAvatar()
        {
            if (proxyRoot != null)
            {
                return;
            }

            proxyRoot = new GameObject(ProxyRootName).transform;
            proxyRoot.SetParent(transform, false);
            proxyRoot.localScale = Vector3.one * proxyScale;

            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "ProxyBody";
            body.transform.SetParent(proxyRoot, false);
            body.transform.localPosition = new Vector3(0f, 0.92f, 0f);
            body.transform.localScale = new Vector3(0.62f, 0.92f, 0.58f);
            DisableCollider(body);

            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "ProxyHead";
            head.transform.SetParent(proxyRoot, false);
            head.transform.localPosition = new Vector3(0f, 1.98f, 0.04f);
            head.transform.localScale = new Vector3(0.52f, 0.52f, 0.52f);
            DisableCollider(head);

            var eyeLeft = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            eyeLeft.name = "EyeLeft";
            eyeLeft.transform.SetParent(head.transform, false);
            eyeLeft.transform.localPosition = new Vector3(-0.12f, 0.05f, 0.24f);
            eyeLeft.transform.localScale = new Vector3(0.11f, 0.11f, 0.11f);
            DisableCollider(eyeLeft);

            var eyeRight = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            eyeRight.name = "EyeRight";
            eyeRight.transform.SetParent(head.transform, false);
            eyeRight.transform.localPosition = new Vector3(0.12f, 0.05f, 0.24f);
            eyeRight.transform.localScale = new Vector3(0.11f, 0.11f, 0.11f);
            DisableCollider(eyeRight);

            proxyRenderer = body.GetComponent<Renderer>();
            ApplyProxyColors(body.GetComponent<Renderer>(), new Color(0.78f, 0.80f, 0.87f, 1f));
            ApplyProxyColors(head.GetComponent<Renderer>(), new Color(0.93f, 0.86f, 0.76f, 1f));
            ApplyProxyColors(eyeLeft.GetComponent<Renderer>(), new Color(0.14f, 0.17f, 0.23f, 1f));
            ApplyProxyColors(eyeRight.GetComponent<Renderer>(), new Color(0.14f, 0.17f, 0.23f, 1f));
            avatarStateMachine?.BindPlaceholder(proxyRenderer);
        }

        private void MoveAvatarToSpawn()
        {
            if (activeAvatarRoot == null || roomWorldController?.AvatarSpawnPoint == null)
            {
                return;
            }

            var spawn = roomWorldController.AvatarSpawnPoint;
            activeAvatarRoot.position = spawn.position;
            activeAvatarRoot.rotation = spawn.rotation;
            defaultFacingForward = spawn.forward.sqrMagnitude > 0.001f ? spawn.forward : Vector3.forward;
        }

        private void ApplyDefaultAttention()
        {
            ClearAttentionTarget();
        }

        public void SetAttentionTarget(Vector3 worldPosition)
        {
            EnsureAttentionTarget();
            attentionTarget.position = worldPosition;
            hasAttentionTarget = true;
            avatarRootController?.LookAt?.SetLookAtTarget(attentionTarget);
            UpdateAttention();
        }

        public void ClearAttentionTarget()
        {
            hasAttentionTarget = false;
            if (avatarRootController?.LookAt != null)
            {
                avatarRootController.LookAt.ClearLookAt();
            }

            UpdateAttention();
        }

        private void HandleRoomSelectionChanged(RoomObjectSelectionSnapshot snapshot)
        {
            if (roomInteractionController == null || !roomInteractionController.HasFocusTarget)
            {
                ClearAttentionTarget();
                return;
            }

            SetAttentionTarget(roomInteractionController.CurrentFocusPoint);
        }

        private void UpdateAttention()
        {
            var targetDirection = hasAttentionTarget
                ? attentionTarget.position - activeAvatarRoot.position
                : defaultFacingForward;
            targetDirection.y = 0f;
            if (targetDirection.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            var desiredRotation = Quaternion.LookRotation(targetDirection.normalized, Vector3.up);
            var lerpFactor = Application.isPlaying ? Time.deltaTime * proxyTurnSpeed : 1f;
            activeAvatarRoot.rotation = Quaternion.Slerp(
                activeAvatarRoot.rotation,
                desiredRotation,
                lerpFactor);
        }

        private static void ApplyProxyColors(Renderer renderer, Color color)
        {
            if (renderer == null)
            {
                return;
            }

            var shader = Shader.Find("Universal Render Pipeline/Lit")
                ?? Shader.Find("Standard")
                ?? Shader.Find("Sprites/Default");
            if (shader == null)
            {
                return;
            }

            renderer.sharedMaterial = new Material(shader)
            {
                color = color,
            };
        }

        private static void DisableCollider(GameObject target)
        {
            if (target != null && target.TryGetComponent<Collider>(out var collider))
            {
                collider.enabled = false;
            }
        }

        private void EnsureAttentionTarget()
        {
            if (attentionTarget != null)
            {
                return;
            }

            attentionTarget = new GameObject("CharacterAttentionTarget").transform;
            attentionTarget.SetParent(transform, false);
        }
    }
}
