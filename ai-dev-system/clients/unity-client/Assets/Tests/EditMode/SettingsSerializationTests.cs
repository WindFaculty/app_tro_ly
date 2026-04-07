using LocalAssistant.Core;
using NUnit.Framework;

namespace LocalAssistant.Tests.EditMode
{
    public class SettingsSerializationTests
    {
        [Test]
        public void SettingsPayloadRoundTripsWithUnityJson()
        {
            var payload = new SettingsPayload();
            payload.voice.speak_replies = false;
            payload.window_mode.mini_assistant_enabled = false;

            var json = UnityJson.Serialize(payload);
            var restored = UnityJson.Deserialize<SettingsPayload>(json);

            Assert.NotNull(restored);
            Assert.IsFalse(restored.voice.speak_replies);
        }
    }
}
