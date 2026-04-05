using System.Collections.Generic;

namespace LocalAssistant.World.Objects
{
    public enum RoomObjectValidationMode
    {
        PlaceholderSafe,
        StrictPrefabIntake,
    }

    public sealed class RoomObjectRegistryValidationReport
    {
        public int DefinitionCount { get; internal set; }
        public List<string> Infos { get; } = new();
        public List<string> Warnings { get; } = new();
        public List<string> Errors { get; } = new();

        public bool HasErrors => Errors.Count > 0;
    }
}
