using System;
using System.Collections.Generic;
using AvatarSystem.Data;
using UnityEngine;

namespace LocalAssistant.Runtime
{
    public sealed class AvatarItemRegistry : MonoBehaviour
    {
        private static readonly StringComparer LookupComparer = StringComparer.OrdinalIgnoreCase;
        private const string DefaultResourcesPath = "AvatarItems";

        [SerializeField] private string resourcesPath = DefaultResourcesPath;
        [SerializeField] private AvatarItemDefinition[] additionalItems = Array.Empty<AvatarItemDefinition>();

        private readonly Dictionary<string, AvatarItemDefinition> itemsByLookup = new(LookupComparer);
        private bool hasLoaded;

        public int RegisteredItemCount { get; private set; }

        public void ConfigureResourcesPath(string path)
        {
            resourcesPath = path ?? string.Empty;
            hasLoaded = false;
        }

        public void ConfigureAdditionalItems(params AvatarItemDefinition[] items)
        {
            additionalItems = items ?? Array.Empty<AvatarItemDefinition>();
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
            itemsByLookup.Clear();
            RegisteredItemCount = 0;

            var uniqueItems = new HashSet<AvatarItemDefinition>();

            if (!string.IsNullOrWhiteSpace(resourcesPath))
            {
                foreach (var item in Resources.LoadAll<AvatarItemDefinition>(resourcesPath))
                {
                    RegisterItem(item, uniqueItems);
                }
            }

            foreach (var item in additionalItems)
            {
                RegisterItem(item, uniqueItems);
            }

            hasLoaded = true;
        }

        public bool TryGetItem(string lookupKey, out AvatarItemDefinition item)
        {
            Initialize();
            if (!TryNormalizeLookupKey(lookupKey, out var normalizedKey))
            {
                item = null;
                return false;
            }

            return itemsByLookup.TryGetValue(normalizedKey, out item);
        }

        public bool TryResolveFoundation(WardrobeFoundationRegistryEntry foundation, out AvatarItemDefinition item)
        {
            Initialize();

            if (foundation != null)
            {
                foreach (var lookupAlias in foundation.LookupAliases)
                {
                    if (TryGetItem(lookupAlias, out item))
                    {
                        return true;
                    }
                }
            }

            item = null;
            return false;
        }

        private void RegisterItem(AvatarItemDefinition item, ISet<AvatarItemDefinition> uniqueItems)
        {
            if (item == null)
            {
                return;
            }

            if (uniqueItems.Add(item))
            {
                RegisteredItemCount++;
            }

            RegisterAlias(item.itemId, item);
            RegisterAlias(item.displayName, item);
            RegisterAlias(item.name, item);
        }

        private void RegisterAlias(string alias, AvatarItemDefinition item)
        {
            if (item == null || !TryNormalizeLookupKey(alias, out var normalizedKey))
            {
                return;
            }

            if (!itemsByLookup.ContainsKey(normalizedKey))
            {
                itemsByLookup[normalizedKey] = item;
            }
        }

        private static bool TryNormalizeLookupKey(string lookupKey, out string normalizedKey)
        {
            normalizedKey = string.IsNullOrWhiteSpace(lookupKey) ? string.Empty : lookupKey.Trim();
            return normalizedKey.Length > 0;
        }
    }
}
