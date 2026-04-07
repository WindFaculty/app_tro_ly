using System.Collections.Generic;
using LocalAssistant.Core;
using NUnit.Framework;

namespace LocalAssistant.Tests.EditMode
{
    public class HealthRecoveryAdvisorTests
    {
        [Test]
        public void ReadyHealthKeepsControlsEnabledAndShowsNoRecoveryMessage()
        {
            var health = new HealthResponse
            {
                status = "ready",
                runtimes = new RuntimeHealthCollection
                {
                    stt = new RuntimeHealth { available = true },
                    tts = new RuntimeHealth { available = true },
                    llm = new RuntimeHealth { available = true, provider = "groq" },
                },
            };

            Assert.AreEqual(string.Empty, HealthRecoveryAdvisor.BuildMessage(health));
            Assert.IsTrue(HealthRecoveryAdvisor.CanUseTaskActions(health));
            Assert.IsTrue(HealthRecoveryAdvisor.CanUseMic(health));
            Assert.IsTrue(HealthRecoveryAdvisor.CanEditSettings(health));
        }

        [Test]
        public void PartialHealthBuildsRecoveryMessageAndDisablesMicWhenSttIsOff()
        {
            var health = new HealthResponse
            {
                status = "partial",
                recovery_actions = new List<string>
                {
                    "Set assistant_groq_api_key.",
                    "Configure assistant_whisper_command.",
                },
                logs = new LogInfo { app_log = "D:/logs/assistant.log" },
                runtimes = new RuntimeHealthCollection
                {
                    stt = new RuntimeHealth { available = false },
                    tts = new RuntimeHealth { available = true },
                    llm = new RuntimeHealth { available = false, provider = "groq" },
                },
            };

            var message = HealthRecoveryAdvisor.BuildMessage(health);

            StringAssert.Contains("Some local features are degraded", message);
            StringAssert.Contains("Configure assistant_whisper_command.", message);
            StringAssert.Contains("assistant.log", message);
            Assert.IsFalse(HealthRecoveryAdvisor.CanUseMic(health));
            Assert.IsTrue(HealthRecoveryAdvisor.CanUseTaskActions(health));
            Assert.IsTrue(HealthRecoveryAdvisor.CanEditSettings(health));
        }

        [Test]
        public void ErrorHealthDisablesTaskActions()
        {
            var health = new HealthResponse { status = "error" };

            Assert.IsFalse(HealthRecoveryAdvisor.CanUseTaskActions(health));
            Assert.IsFalse(HealthRecoveryAdvisor.CanUseMic(health));
            Assert.IsFalse(HealthRecoveryAdvisor.CanEditSettings(health));
        }
    }
}
