using System;
using System.IO;
using MCPForUnity.Editor;
using UnityEditor;
using UnityEngine;

namespace TroLy.Editor
{
    [InitializeOnLoad]
    public static class McpCiAutoBoot
    {
        private const string BootEnvVar = "UNITY_MCP_CI_BOOT";
        private const string BootFlagRelativePath = "../ProjectSettings/mcp-ci-autoboot.flag";
        private const string SessionKey = "TroLy.McpCiAutoBoot.Started";

        static McpCiAutoBoot()
        {
            if (!IsEnabled())
            {
                return;
            }

            if (SessionState.GetBool(SessionKey, false))
            {
                return;
            }

            SessionState.SetBool(SessionKey, true);
            EditorApplication.delayCall += StartBridgeOnceEditorIsReady;
        }

        private static bool IsEnabled()
        {
            try
            {
                var flagPath = Path.GetFullPath(Path.Combine(Application.dataPath, BootFlagRelativePath));
                if (File.Exists(flagPath))
                {
                    return true;
                }
            }
            catch
            {
                // Ignore file-system issues and fall back to env var detection.
            }

            var raw = Environment.GetEnvironmentVariable(BootEnvVar);
            return !string.IsNullOrWhiteSpace(raw)
                && (raw == "1"
                    || raw.Equals("true", StringComparison.OrdinalIgnoreCase)
                    || raw.Equals("yes", StringComparison.OrdinalIgnoreCase)
                    || raw.Equals("on", StringComparison.OrdinalIgnoreCase));
        }

        private static void StartBridgeOnceEditorIsReady()
        {
            try
            {
                Debug.Log("[MCP CI] UNITY_MCP_CI_BOOT detected. Starting stdio bridge.");
                McpCiBoot.StartStdioForCi();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MCP CI] Failed to start stdio bridge: {ex}");
            }
        }
    }
}
