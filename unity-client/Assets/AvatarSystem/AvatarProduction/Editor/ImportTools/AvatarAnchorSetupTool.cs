using UnityEngine;
using UnityEditor;

namespace AvatarSystem.Editor
{
    public static class AvatarAnchorSetupTool
    {
        [MenuItem("Tools/AvatarSystem/Setup Scene Anchor Points")]
        public static void SetupAnchorPoints()
        {
            // Create root container
            GameObject root = GameObject.Find("AvatarAnchorPoints");
            if (root == null)
            {
                root = new GameObject("AvatarAnchorPoints");
            }

            // Define points to create
            string[] pointNames = { "IdlePoint", "TalkPoint", "ListenPoint", "WanderA", "WanderB" };
            Vector3[] defaultPositions = {
                Vector3.zero,                   // Idle
                new Vector3(0, 0, 1.5f),        // Talk (closer to user)
                new Vector3(-1f, 0, 1f),        // Listen (slightly off center)
                new Vector3(-2f, 0, 0f),        // Wander A (left side)
                new Vector3(2f, 0, 0f)          // Wander B (right side)
            };

            for (int i = 0; i < pointNames.Length; i++)
            {
                Transform existingPoint = root.transform.Find(pointNames[i]);
                if (existingPoint == null)
                {
                    GameObject point = new GameObject(pointNames[i]);
                    point.transform.SetParent(root.transform);
                    point.transform.position = defaultPositions[i];
                    
                    // Add a gizmo drawer script for easy visualization in Editor
                    var gizmoHelper = point.AddComponent<AnchorGizmoDrawer>();
                    gizmoHelper.gizmoColor = GetColorForPoint(i);
                    gizmoHelper.radius = 0.2f;

                    Debug.Log($"[AvatarSystem] Created missing anchor: {pointNames[i]}");
                }
            }
            
            // Register Undo so user can revert easily
            Undo.RegisterCreatedObjectUndo(root, "Create Avatar Anchor Points");
            Selection.activeGameObject = root;
            
            Debug.Log("[AvatarSystem] Scene Anchor Points setup completed.");
        }

        private static Color GetColorForPoint(int index)
        {
            switch (index)
            {
                case 0: return Color.white;   // Idle
                case 1: return Color.green;   // Talk
                case 2: return Color.blue;    // Listen
                case 3: return Color.yellow;  // Wander A
                case 4: return Color.yellow;  // Wander B
                default: return Color.gray;
            }
        }
    }

    // A simple MonoBehaviour purely to draw spheres in the Scene view
    public class AnchorGizmoDrawer : MonoBehaviour
    {
        public Color gizmoColor = Color.white;
        public float radius = 0.2f;

        private void OnDrawGizmos()
        {
            Gizmos.color = gizmoColor;
            Gizmos.DrawWireSphere(transform.position, radius);
            
            // Draw a tiny line to show "forward" facing direction
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, transform.forward * radius * 1.5f);
        }
    }
}
