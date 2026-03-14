namespace LocalAssistant.Core
{
    public static class HealthResponseNormalizer
    {
        public static HealthResponse Normalize(HealthResponse health)
        {
            var safeHealth = health ?? new HealthResponse();
            if (safeHealth.database == null)
            {
                safeHealth.database = new DatabaseHealth();
            }

            if (safeHealth.runtimes == null)
            {
                safeHealth.runtimes = new RuntimeHealthCollection();
            }

            if (safeHealth.runtimes.llm == null)
            {
                safeHealth.runtimes.llm = new RuntimeHealth();
            }

            if (safeHealth.runtimes.stt == null)
            {
                safeHealth.runtimes.stt = new RuntimeHealth();
            }

            if (safeHealth.runtimes.tts == null)
            {
                safeHealth.runtimes.tts = new RuntimeHealth();
            }

            if (safeHealth.logs == null)
            {
                safeHealth.logs = new LogInfo();
            }

            if (safeHealth.degraded_features == null)
            {
                safeHealth.degraded_features = new System.Collections.Generic.List<string>();
            }

            if (safeHealth.recovery_actions == null)
            {
                safeHealth.recovery_actions = new System.Collections.Generic.List<string>();
            }

            return safeHealth;
        }
    }
}
