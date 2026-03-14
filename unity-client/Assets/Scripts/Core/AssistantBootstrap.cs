using System;
using UnityEngine;

namespace LocalAssistant.Core
{
    public static class AssistantBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureAssistantApp()
        {
            if (IsAutomatedTestRun())
            {
                return;
            }

            if (UnityEngine.Object.FindFirstObjectByType<AssistantApp>() != null)
            {
                return;
            }

            var root = new GameObject("AssistantApp");
            UnityEngine.Object.DontDestroyOnLoad(root);
            root.AddComponent<AssistantApp>();
        }

        private static bool IsAutomatedTestRun()
        {
            var args = Environment.GetCommandLineArgs();
            foreach (var argument in args)
            {
                if (string.Equals(argument, "-runTests", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(argument, "-runEditorTests", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(argument, "-codexTestMode", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
