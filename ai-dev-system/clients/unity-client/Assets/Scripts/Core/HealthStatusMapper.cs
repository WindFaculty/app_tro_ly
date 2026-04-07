using UnityEngine;

namespace LocalAssistant.Core
{
    public static class HealthStatusMapper
    {
        public static string ToLabel(string status)
        {
            return status switch
            {
                "ready" => "Ready",
                "partial" => "Partial",
                _ => "Error",
            };
        }

        public static Color ToColor(string status)
        {
            return status switch
            {
                "ready" => new Color(0.21f, 0.65f, 0.38f),
                "partial" => new Color(0.88f, 0.62f, 0.18f),
                _ => new Color(0.79f, 0.26f, 0.22f),
            };
        }
    }
}
