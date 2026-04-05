using LocalAssistant.World.Objects;
using UnityEngine;

namespace LocalAssistant.World.Interaction
{
    public sealed class InteractableObject : MonoBehaviour
    {
        private static readonly Color HoverTint = new(0.68f, 0.87f, 0.98f, 1f);
        private static readonly Color SelectedTint = new(0.95f, 0.81f, 0.45f, 1f);

        private Renderer[] cachedRenderers = System.Array.Empty<Renderer>();
        private Color[] baseColors = System.Array.Empty<Color>();
        private bool isHovered;
        private bool isSelected;

        public RoomObjectDefinition Definition { get; private set; }
        public RoomObjectPlacement Placement { get; private set; }
        public bool SupportsSelection => Definition != null && Definition.Selectable;
        public bool SupportsHover => Definition != null && Definition.Hoverable;
        public RoomInteractionType InteractionType => Definition?.InteractionType ?? RoomInteractionType.None;

        public Vector3 FocusPoint
        {
            get
            {
                if (cachedRenderers.Length == 0)
                {
                    return transform.position;
                }

                var bounds = cachedRenderers[0].bounds;
                for (var index = 1; index < cachedRenderers.Length; index++)
                {
                    bounds.Encapsulate(cachedRenderers[index].bounds);
                }

                return bounds.center;
            }
        }

        public void Initialize(RoomObjectDefinition definition, RoomObjectPlacement placement)
        {
            Definition = definition;
            Placement = placement;
            cachedRenderers = GetComponentsInChildren<Renderer>(true);
            baseColors = new Color[cachedRenderers.Length];
            for (var index = 0; index < cachedRenderers.Length; index++)
            {
                var material = GetMutableMaterial(cachedRenderers[index]);
                baseColors[index] = material != null ? material.color : Color.white;
            }

            ApplyVisualState();
        }

        public void SetHovered(bool value)
        {
            if (isHovered == value)
            {
                return;
            }

            isHovered = value;
            ApplyVisualState();
        }

        public void SetSelected(bool value)
        {
            if (isSelected == value)
            {
                return;
            }

            isSelected = value;
            ApplyVisualState();
        }

        public RoomObjectSelectionSnapshot CreateSnapshot()
        {
            if (Definition == null)
            {
                return RoomObjectSelectionSnapshot.None;
            }

            return new RoomObjectSelectionSnapshot
            {
                HasSelection = true,
                ObjectId = Definition.Id ?? string.Empty,
                DisplayName = string.IsNullOrWhiteSpace(Definition.DisplayName) ? Definition.Id : Definition.DisplayName,
                CategoryLabel = BuildCategoryLabel(Definition),
                StateText = BuildStateText(Definition),
                DetailText = BuildDetailText(Definition),
                SuggestedActionText = BuildSuggestedActionText(Definition),
                ActionText = BuildActionText(Definition.InteractionType),
                SupportsGoTo = Definition.Selectable,
                SupportsInspect = SupportsInspect(Definition),
                SupportsUse = SupportsUse(Definition),
            };
        }

        private void ApplyVisualState()
        {
            for (var index = 0; index < cachedRenderers.Length; index++)
            {
                var material = GetMutableMaterial(cachedRenderers[index]);
                if (material == null)
                {
                    continue;
                }

                var targetColor = baseColors[index];
                if (isSelected)
                {
                    targetColor = Color.Lerp(targetColor, SelectedTint, 0.48f);
                }
                else if (isHovered)
                {
                    targetColor = Color.Lerp(targetColor, HoverTint, 0.32f);
                }

                material.color = targetColor;
            }
        }

        private static Material GetMutableMaterial(Renderer renderer)
        {
            if (renderer == null)
            {
                return null;
            }

            return Application.isPlaying ? renderer.material : renderer.sharedMaterial;
        }

        private static string BuildCategoryLabel(RoomObjectDefinition definition)
        {
            var interaction = definition.InteractionType switch
            {
                RoomInteractionType.Inspect => "Inspect",
                RoomInteractionType.Focus => "Focus",
                RoomInteractionType.InspectAndFocus => "Inspect + Focus",
                _ => "Ambient",
            };

            return $"{definition.Category} | {interaction}";
        }

        private static string BuildDetailText(RoomObjectDefinition definition)
        {
            if (definition.Tags == null || definition.Tags.Length == 0)
            {
                return "Registered room object ready for Character Space interaction.";
            }

            return $"Tags: {string.Join(", ", definition.Tags)}";
        }

        private static string BuildStateText(RoomObjectDefinition definition)
        {
            var baseState = definition.InteractionType switch
            {
                RoomInteractionType.Inspect => "State: ready to inspect",
                RoomInteractionType.Focus => "State: ready to focus",
                RoomInteractionType.InspectAndFocus => "State: ready to inspect and focus",
                _ => "State: ambient decor",
            };

            if (definition.OptionalStates == null || definition.OptionalStates.Length == 0)
            {
                return baseState;
            }

            return $"{baseState} | Profiles: {string.Join(", ", definition.OptionalStates)}";
        }

        private static string BuildSuggestedActionText(RoomObjectDefinition definition)
        {
            var actions = new System.Collections.Generic.List<string>();
            if (definition.Selectable)
            {
                actions.Add("go to");
            }

            if (SupportsInspect(definition))
            {
                actions.Add("inspect");
            }

            if (SupportsUse(definition))
            {
                actions.Add("use");
            }

            return actions.Count == 0
                ? "Suggested actions: return to avatar focus."
                : $"Suggested actions: {string.Join(", ", actions)}.";
        }

        private static string BuildActionText(RoomInteractionType interactionType)
        {
            return interactionType switch
            {
                RoomInteractionType.Inspect => "Action: Inspect object",
                RoomInteractionType.Focus => "Action: Focus stage camera",
                RoomInteractionType.InspectAndFocus => "Action: Inspect object and focus camera",
                _ => "Action: Ambient decor only",
            };
        }

        private static bool SupportsInspect(RoomObjectDefinition definition)
        {
            return definition != null &&
                (definition.Inspectable
                || definition.InteractionType == RoomInteractionType.Inspect
                || definition.InteractionType == RoomInteractionType.InspectAndFocus);
        }

        private static bool SupportsUse(RoomObjectDefinition definition)
        {
            if (definition == null)
            {
                return false;
            }

            return definition.Category == RoomObjectCategory.Interactive
                || definition.Category == RoomObjectCategory.Lighting
                || HasTag(definition, "usable")
                || HasTag(definition, "switch")
                || HasTag(definition, "seat");
        }

        private static bool HasTag(RoomObjectDefinition definition, string expected)
        {
            if (definition?.Tags == null)
            {
                return false;
            }

            for (var index = 0; index < definition.Tags.Length; index++)
            {
                if (string.Equals(definition.Tags[index], expected, System.StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
