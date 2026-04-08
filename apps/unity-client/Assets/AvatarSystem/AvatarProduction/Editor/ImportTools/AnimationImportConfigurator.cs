using UnityEngine;
using UnityEditor;
using System.IO;

namespace AvatarSystem.Editor
{
    public class AnimationImportConfigurator : AssetPostprocessor
    {
        void OnPreprocessModel()
        {
            if (assetPath.Contains("AvatarProduction/BaseAvatar/Animations") && assetPath.EndsWith(".fbx"))
            {
                ModelImporter importer = (ModelImporter)assetImporter;
                
                // Set animation type to Humanoid
                importer.animationType = ModelImporterAnimationType.Human;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;

                // Optimization
                importer.optimizeGameObjects = true;
                
                // Do not import materials/meshes from mixamo animations
                importer.materialImportMode = ModelImporterMaterialImportMode.None;
            }
        }

        void OnPreprocessAnimation()
        {
            if (assetPath.Contains("AvatarProduction/BaseAvatar/Animations") && assetPath.EndsWith(".fbx"))
            {
                ModelImporter importer = (ModelImporter)assetImporter;
                ModelImporterClipAnimation[] clipAnimations = importer.defaultClipAnimations;

                if (clipAnimations != null && clipAnimations.Length > 0)
                {
                    for (int i = 0; i < clipAnimations.Length; i++)
                    {
                        var clip = clipAnimations[i];
                        
                        // Force name to match file name (remove mixamo.com prefix)
                        string fileName = Path.GetFileNameWithoutExtension(assetPath);
                        clip.name = fileName;

                        // Configuration for Loop and Root Motion
                        // In general, we want to loop idle, walk, run.
                        bool shouldLoop = fileName.Contains("Idle") || fileName.Contains("Walk") || fileName.Contains("Listen");
                        
                        clip.loopTime = shouldLoop;
                        clip.loopPose = shouldLoop;

                        // Root Transform Baking (Keep Character in place or allow it to drive Root Motion)
                        // Most Mixamo animations need to be baked into pose if we don't use Root Motion for movement
                        // For Walk/Turn we might want root motion, but usually we use "In-Place" from Mixamo
                        clip.lockRootHeightY = true;
                        clip.lockRootPositionXZ = true;
                        clip.lockRootRotation = true;
                        
                        clip.keepOriginalPositionY = true;
                        clip.keepOriginalPositionXZ = true;
                        clip.keepOriginalOrientation = true;
                    }

                    importer.clipAnimations = clipAnimations;
                }
            }
        }

        [MenuItem("Tools/AvatarSystem/Process All Animations")]
        public static void ProcessAllAnimations()
        {
            string animFolder = "Assets/AvatarSystem/AvatarProduction/BaseAvatar/Animations";
            string[] guids = AssetDatabase.FindAssets("t:Model", new[] { animFolder });
            
            int count = 0;
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith(".fbx"))
                {
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                    count++;
                }
            }
            
            Debug.Log($"[AvatarSystem] Processed {count} animation files.");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
