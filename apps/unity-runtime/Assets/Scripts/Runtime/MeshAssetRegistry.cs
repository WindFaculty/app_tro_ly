using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace LocalAssistant.Runtime
{
    public sealed class MeshAssetRegistry : MonoBehaviour
    {
        private static readonly string[] DefaultManifestRootSegments = { "..", "..", "ai-dev-system", "asset-pipeline", "manifests" };
        private static readonly StringComparer LookupComparer = StringComparer.OrdinalIgnoreCase;

        [SerializeField] private bool loadOnAwake;
        [SerializeField] private string[] manifestSearchRoots = Array.Empty<string>();
        [SerializeField] private TextAsset[] embeddedHandoffManifests = Array.Empty<TextAsset>();

        private readonly Dictionary<string, WardrobeFoundationRegistryEntry> wardrobeByAssetId = new(LookupComparer);
        private readonly Dictionary<string, WardrobeFoundationRegistryEntry> wardrobeByLookupKey = new(LookupComparer);
        private readonly Dictionary<string, RoomAssetRegistryEntry> roomByAssetId = new(LookupComparer);
        private readonly Dictionary<string, RoomAssetRegistryEntry> roomByLookupKey = new(LookupComparer);
        private readonly List<string> loadedManifestPaths = new();

        private bool hasLoaded;

        public int TotalEntryCount => wardrobeByAssetId.Count + roomByAssetId.Count;
        public int WardrobeFoundationCount => wardrobeByAssetId.Count;
        public int RoomAssetCount => roomByAssetId.Count;
        public int IgnoredManifestCount { get; private set; }
        public int FailedManifestCount { get; private set; }
        public IReadOnlyList<string> LoadedManifestPaths => loadedManifestPaths;

        private void Awake()
        {
            if (loadOnAwake)
            {
                Reload();
            }
        }

        public void ConfigureSearchRoots(params string[] searchRoots)
        {
            manifestSearchRoots = searchRoots ?? Array.Empty<string>();
            hasLoaded = false;
        }

        public void ConfigureEmbeddedHandoffManifests(params TextAsset[] manifests)
        {
            embeddedHandoffManifests = manifests ?? Array.Empty<TextAsset>();
            hasLoaded = false;
        }

        public void Initialize()
        {
            if (!hasLoaded)
            {
                Reload();
            }
        }

        public void Reload()
        {
            ResetState();

            foreach (var manifestPath in ResolveManifestFilePaths())
            {
                try
                {
                    RegisterManifest(File.ReadAllText(manifestPath), manifestPath);
                }
                catch (Exception exception)
                {
                    FailedManifestCount++;
                    Debug.LogWarning($"[MeshAssetRegistry] Failed to load manifest '{manifestPath}': {exception.Message}");
                }
            }

            foreach (var manifestAsset in embeddedHandoffManifests)
            {
                if (manifestAsset == null || string.IsNullOrWhiteSpace(manifestAsset.text))
                {
                    continue;
                }

                try
                {
                    RegisterManifest(manifestAsset.text, $"embedded://{manifestAsset.name}");
                }
                catch (Exception exception)
                {
                    FailedManifestCount++;
                    Debug.LogWarning($"[MeshAssetRegistry] Failed to load embedded manifest '{manifestAsset.name}': {exception.Message}");
                }
            }

            hasLoaded = true;
        }

        public bool TryGetWardrobeFoundation(string lookupKey, out WardrobeFoundationRegistryEntry entry)
        {
            EnsureLoaded();
            if (!TryNormalizeLookupKey(lookupKey, out var normalizedKey))
            {
                entry = null;
                return false;
            }

            return wardrobeByAssetId.TryGetValue(normalizedKey, out entry) ||
                   wardrobeByLookupKey.TryGetValue(normalizedKey, out entry);
        }

        public bool TryGetRoomAsset(string lookupKey, out RoomAssetRegistryEntry entry)
        {
            EnsureLoaded();
            if (!TryNormalizeLookupKey(lookupKey, out var normalizedKey))
            {
                entry = null;
                return false;
            }

            return roomByAssetId.TryGetValue(normalizedKey, out entry) ||
                   roomByLookupKey.TryGetValue(normalizedKey, out entry);
        }

        public bool TryApplyRoomFocusPreset(string lookupKey, RoomRuntime roomRuntime, out RoomAssetRegistryEntry entry)
        {
            if (roomRuntime != null &&
                TryGetRoomAsset(lookupKey, out entry) &&
                !string.IsNullOrWhiteSpace(entry.RoomFocusPreset))
            {
                roomRuntime.SetFocusPreset(entry.RoomFocusPreset);
                return true;
            }

            entry = null;
            return false;
        }

        private void EnsureLoaded()
        {
            if (!hasLoaded)
            {
                Reload();
            }
        }

        private void RegisterManifest(string json, string manifestPath)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                IgnoredManifestCount++;
                return;
            }

            var manifest = JsonUtility.FromJson<MeshExportHandoffManifestRecord>(json);
            if (!IsSupportedManifest(manifest))
            {
                IgnoredManifestCount++;
                return;
            }

            var aliases = BuildLookupAliases(manifest);
            if (IsWardrobeAsset(manifest.asset_type))
            {
                var entry = new WardrobeFoundationRegistryEntry(manifest, manifestPath, aliases);
                if (wardrobeByAssetId.ContainsKey(entry.AssetId))
                {
                    IgnoredManifestCount++;
                    Debug.LogWarning($"[MeshAssetRegistry] Duplicate wardrobe manifest ignored for asset '{entry.AssetId}'.");
                    return;
                }

                wardrobeByAssetId[entry.AssetId] = entry;
                RegisterLookupAliases(entry, wardrobeByLookupKey);
                loadedManifestPaths.Add(manifestPath);
                return;
            }

            if (IsRoomAsset(manifest.asset_type))
            {
                var entry = new RoomAssetRegistryEntry(manifest, manifestPath, aliases);
                if (roomByAssetId.ContainsKey(entry.AssetId))
                {
                    IgnoredManifestCount++;
                    Debug.LogWarning($"[MeshAssetRegistry] Duplicate room manifest ignored for asset '{entry.AssetId}'.");
                    return;
                }

                roomByAssetId[entry.AssetId] = entry;
                RegisterLookupAliases(entry, roomByLookupKey);
                loadedManifestPaths.Add(manifestPath);
                return;
            }

            IgnoredManifestCount++;
        }

        private static bool IsSupportedManifest(MeshExportHandoffManifestRecord manifest)
        {
            return manifest != null &&
                   string.Equals(manifest.target_runtime, "unity", StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(manifest.status, "export-ready", StringComparison.OrdinalIgnoreCase) &&
                   !string.IsNullOrWhiteSpace(manifest.asset_id) &&
                   !string.IsNullOrWhiteSpace(manifest.target_unity_path) &&
                   (IsWardrobeAsset(manifest.asset_type) || IsRoomAsset(manifest.asset_type));
        }

        private static bool IsWardrobeAsset(string assetType)
        {
            return string.Equals(assetType, "avatar_clothing", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(assetType, "avatar_accessory", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsRoomAsset(string assetType)
        {
            return string.Equals(assetType, "room_item", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(assetType, "prop", StringComparison.OrdinalIgnoreCase);
        }

        private static string[] BuildLookupAliases(MeshExportHandoffManifestRecord manifest)
        {
            var aliases = new HashSet<string>(LookupComparer);
            AddLookupAlias(aliases, manifest.asset_id);
            AddLookupAlias(aliases, manifest.asset_name);
            AddLookupAlias(aliases, manifest.target_unity_path);
            AddLookupAlias(aliases, ExtractLeafName(manifest.target_unity_path));
            return new List<string>(aliases).ToArray();
        }

        private static void AddLookupAlias(ISet<string> aliases, string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                aliases.Add(value.Trim());
            }
        }

        private static string ExtractLeafName(string targetUnityPath)
        {
            if (string.IsNullOrWhiteSpace(targetUnityPath))
            {
                return string.Empty;
            }

            var normalizedPath = targetUnityPath.Replace('\\', '/');
            var separatorIndex = normalizedPath.LastIndexOf('/');
            return separatorIndex >= 0 ? normalizedPath.Substring(separatorIndex + 1) : normalizedPath;
        }

        private static void RegisterLookupAliases<TEntry>(TEntry entry, IDictionary<string, TEntry> lookupTable)
            where TEntry : MeshAssetRegistryEntry
        {
            foreach (var alias in entry.LookupAliases)
            {
                if (!lookupTable.ContainsKey(alias))
                {
                    lookupTable[alias] = entry;
                }
            }
        }

        private IEnumerable<string> ResolveManifestFilePaths()
        {
            var searchRoots = ResolveSearchRoots();
            var yieldedPaths = new HashSet<string>(LookupComparer);

            foreach (var searchRoot in searchRoots)
            {
                if (!Directory.Exists(searchRoot))
                {
                    continue;
                }

                var manifestPaths = Directory.GetFiles(searchRoot, "*.json", SearchOption.TopDirectoryOnly);
                Array.Sort(manifestPaths, StringComparer.OrdinalIgnoreCase);

                foreach (var manifestPath in manifestPaths)
                {
                    var fullPath = Path.GetFullPath(manifestPath);
                    if (yieldedPaths.Add(fullPath))
                    {
                        yield return fullPath;
                    }
                }
            }
        }

        private IEnumerable<string> ResolveSearchRoots()
        {
            if (manifestSearchRoots != null && manifestSearchRoots.Length > 0)
            {
                foreach (var configuredRoot in manifestSearchRoots)
                {
                    if (TryResolveSearchRoot(configuredRoot, out var resolvedRoot))
                    {
                        yield return resolvedRoot;
                    }
                }

                yield break;
            }

            var projectRoot = GetProjectRoot();
            yield return Path.GetFullPath(Path.Combine(
                projectRoot,
                DefaultManifestRootSegments[0],
                DefaultManifestRootSegments[1],
                DefaultManifestRootSegments[2],
                DefaultManifestRootSegments[3]));
        }

        private static bool TryResolveSearchRoot(string configuredRoot, out string resolvedRoot)
        {
            if (string.IsNullOrWhiteSpace(configuredRoot))
            {
                resolvedRoot = string.Empty;
                return false;
            }

            resolvedRoot = Path.IsPathRooted(configuredRoot)
                ? Path.GetFullPath(configuredRoot)
                : Path.GetFullPath(Path.Combine(GetProjectRoot(), configuredRoot));
            return true;
        }

        private static bool TryNormalizeLookupKey(string lookupKey, out string normalizedKey)
        {
            normalizedKey = string.IsNullOrWhiteSpace(lookupKey) ? string.Empty : lookupKey.Trim();
            return normalizedKey.Length > 0;
        }

        private static string GetProjectRoot()
        {
            return Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
        }

        private void ResetState()
        {
            wardrobeByAssetId.Clear();
            wardrobeByLookupKey.Clear();
            roomByAssetId.Clear();
            roomByLookupKey.Clear();
            loadedManifestPaths.Clear();
            IgnoredManifestCount = 0;
            FailedManifestCount = 0;
        }
    }

    public abstract class MeshAssetRegistryEntry
    {
        protected MeshAssetRegistryEntry(MeshExportHandoffManifestRecord manifest, string manifestPath, IEnumerable<string> lookupAliases)
        {
            AssetId = manifest.asset_id ?? string.Empty;
            AssetName = manifest.asset_name ?? string.Empty;
            AssetType = manifest.asset_type ?? string.Empty;
            Category = manifest.category ?? string.Empty;
            TargetUnityPath = manifest.target_unity_path ?? string.Empty;
            Status = manifest.status ?? string.Empty;
            ManifestPath = manifestPath ?? string.Empty;
            ValidationReportPath = manifest.validation_report_path ?? string.Empty;
            PreviewRenderPath = manifest.preview_render_path ?? string.Empty;
            HandoffSourcePath = manifest.handoff_source_path ?? string.Empty;
            Notes = manifest.notes ?? Array.Empty<string>();
            ManualGates = manifest.manual_gates ?? Array.Empty<string>();
            LookupAliases = new List<string>(lookupAliases ?? Array.Empty<string>()).ToArray();
        }

        public string AssetId { get; }
        public string AssetName { get; }
        public string AssetType { get; }
        public string Category { get; }
        public string TargetUnityPath { get; }
        public string Status { get; }
        public string ManifestPath { get; }
        public string ValidationReportPath { get; }
        public string PreviewRenderPath { get; }
        public string HandoffSourcePath { get; }
        public string[] Notes { get; }
        public string[] ManualGates { get; }
        public string[] LookupAliases { get; }
    }

    public sealed class WardrobeFoundationRegistryEntry : MeshAssetRegistryEntry
    {
        public WardrobeFoundationRegistryEntry(
            MeshExportHandoffManifestRecord manifest,
            string manifestPath,
            IEnumerable<string> lookupAliases)
            : base(manifest, manifestPath, lookupAliases)
        {
            Slot = manifest.slot ?? string.Empty;
        }

        public string Slot { get; }
    }

    public sealed class RoomAssetRegistryEntry : MeshAssetRegistryEntry
    {
        public RoomAssetRegistryEntry(
            MeshExportHandoffManifestRecord manifest,
            string manifestPath,
            IEnumerable<string> lookupAliases)
            : base(manifest, manifestPath, lookupAliases)
        {
            RoomFocusPreset = manifest.room_focus_preset ?? string.Empty;
        }

        public string RoomFocusPreset { get; }
    }

    [Serializable]
    public sealed class MeshExportHandoffManifestRecord
    {
        public string asset_id = string.Empty;
        public string asset_name = string.Empty;
        public string asset_type = string.Empty;
        public string category = string.Empty;
        public string status = string.Empty;
        public string target_runtime = string.Empty;
        public string target_unity_path = string.Empty;
        public string slot = string.Empty;
        public string room_focus_preset = string.Empty;
        public string validation_report_path = string.Empty;
        public string preview_render_path = string.Empty;
        public string handoff_source_path = string.Empty;
        public string[] notes = Array.Empty<string>();
        public string[] manual_gates = Array.Empty<string>();
    }
}
