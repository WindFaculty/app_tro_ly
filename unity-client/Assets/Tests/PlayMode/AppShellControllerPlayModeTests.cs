using LocalAssistant.App;
using LocalAssistant.Avatar;
using LocalAssistant.Chat;
using LocalAssistant.Core;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace LocalAssistant.Tests.PlayMode
{
    public class AppShellControllerPlayModeTests
    {
        [Test]
        public void RenderHealthAndStageUpdatesShellText()
        {
            var refs = new AppShellRefs
            {
                HealthBanner = new Label(),
                AvatarStateText = new Label(),
                StageStatusText = new Label(),
                RefreshButton = new Button(),
            };
            var controller = new AppShellController(refs);
            var health = new HealthResponse
            {
                status = "partial",
                database = new DatabaseHealth { available = true },
                runtimes = new RuntimeHealthCollection
                {
                    llm = new RuntimeHealth { available = true, provider = "local" },
                    stt = new RuntimeHealth { available = false },
                    tts = new RuntimeHealth { available = true },
                },
            };
            var settingsStore = new SettingsViewModelStore();
            settingsStore.SetSpeakReplies(true);
            var chatStore = new ChatViewModelStore();
            chatStore.SetDiagnostics("planner", "local", 0, false);

            controller.RenderHealth(health);
            controller.RenderStage(AvatarState.Listening, health, AppScreen.Week, "2026-03-26", settingsStore, chatStore);

            StringAssert.Contains("Partial", refs.HealthBanner.text);
            Assert.AreEqual("Listening", refs.AvatarStateText.text);
            StringAssert.Contains("planner", refs.StageStatusText.text);
        }

        [Test]
        public void BindRaisesRefreshRequested()
        {
            var refs = new AppShellRefs
            {
                RefreshButton = new Button(),
            };
            var controller = new AppShellController(refs);
            var observed = false;
            controller.RefreshRequested += () => observed = true;
            controller.RequestRefresh();

            Assert.IsTrue(observed);
        }
    }
}
