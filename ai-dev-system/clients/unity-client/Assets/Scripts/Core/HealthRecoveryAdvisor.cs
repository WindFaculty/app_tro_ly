using System;
using System.Text;

namespace LocalAssistant.Core
{
    public static class HealthRecoveryAdvisor
    {
        public static bool CanUseTaskActions(HealthResponse health)
        {
            return health != null && health.status != "error";
        }

        public static bool CanUseMic(HealthResponse health)
        {
            return CanUseTaskActions(health) && health.runtimes.stt.available;
        }

        public static bool CanEditSettings(HealthResponse health)
        {
            return CanUseTaskActions(health);
        }

        public static string BuildMessage(HealthResponse health)
        {
            if (health == null || health.status == "ready" || health.recovery_actions == null || health.recovery_actions.Count == 0)
            {
                return string.Empty;
            }

            var builder = new StringBuilder();
            builder.AppendLine(health.status == "error" ? "Backend is unavailable. Try:" : "Some local features are degraded. Try:");
            var count = Math.Min(3, health.recovery_actions.Count);
            for (var index = 0; index < count; index++)
            {
                builder.AppendLine("- " + health.recovery_actions[index]);
            }

            if (!string.IsNullOrEmpty(health.logs.app_log))
            {
                builder.AppendLine("Log: " + health.logs.app_log);
            }

            return builder.ToString().Trim();
        }
    }
}
