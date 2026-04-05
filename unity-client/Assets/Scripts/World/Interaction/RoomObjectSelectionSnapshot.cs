namespace LocalAssistant.World.Interaction
{
    public sealed class RoomObjectSelectionSnapshot
    {
        public static RoomObjectSelectionSnapshot None => new()
        {
            HasSelection = false,
            DisplayName = "No object selected",
            CategoryLabel = "Character Space",
            StateText = "State: stage idle",
            DetailText = "Click a highlighted object in the room to inspect it.",
            SuggestedActionText = "Suggested actions: go to, inspect, use, or return to the avatar.",
            ActionText = "Primary action: select a highlighted room object.",
        };

        public bool HasSelection;
        public string ObjectId = string.Empty;
        public string DisplayName = string.Empty;
        public string CategoryLabel = string.Empty;
        public string StateText = string.Empty;
        public string DetailText = string.Empty;
        public string SuggestedActionText = string.Empty;
        public string ActionText = string.Empty;
        public bool SupportsGoTo;
        public bool SupportsInspect;
        public bool SupportsUse;
    }
}
