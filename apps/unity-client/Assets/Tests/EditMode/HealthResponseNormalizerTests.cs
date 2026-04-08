using LocalAssistant.Core;
using NUnit.Framework;

namespace LocalAssistant.Tests.EditMode
{
    public class HealthResponseNormalizerTests
    {
        [Test]
        public void NormalizeCreatesSafeDefaultsForSparsePayload()
        {
            var health = new HealthResponse
            {
                status = "partial",
                database = null,
                runtimes = new RuntimeHealthCollection
                {
                    llm = null,
                    stt = null,
                    tts = null,
                },
                logs = null,
                degraded_features = null,
                recovery_actions = null,
            };

            var normalized = HealthResponseNormalizer.Normalize(health);

            Assert.NotNull(normalized.database);
            Assert.NotNull(normalized.runtimes);
            Assert.NotNull(normalized.runtimes.llm);
            Assert.NotNull(normalized.runtimes.stt);
            Assert.NotNull(normalized.runtimes.tts);
            Assert.NotNull(normalized.logs);
            Assert.NotNull(normalized.degraded_features);
            Assert.NotNull(normalized.recovery_actions);
        }

        [Test]
        public void NormalizeCreatesFallbackPayloadWhenInputIsNull()
        {
            var normalized = HealthResponseNormalizer.Normalize(null);

            Assert.AreEqual("error", normalized.status);
            Assert.NotNull(normalized.database);
            Assert.NotNull(normalized.runtimes);
        }
    }
}
