using LocalAssistant.App;
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
                TopStatusChipLabel = new Label(),
                HealthBanner = new Label(),
                AvatarStateText = new Label(),
                StageStatusText = new Label(),
                ScheduleInsightTitle = new Label(),
                ScheduleInsightSummary = new Label(),
                ScheduleInsightMeta = new Label(),
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
            controller.RenderStage(
                ShellStageSnapshotBuilder.Build(
                    AvatarState.Listening,
                    health,
                    AppScreen.Week,
                    "2026-03-26",
                    settingsStore,
                    chatStore));

            StringAssert.Contains("Partial", refs.HealthBanner.text);
            StringAssert.Contains("Partial", refs.TopStatusChipLabel.text);
            Assert.AreEqual("Listening", refs.AvatarStateText.text);
            StringAssert.Contains("planner", refs.StageStatusText.text);
            Assert.AreEqual("Week overview", refs.ScheduleInsightTitle.text);
            StringAssert.Contains("degraded", refs.ScheduleInsightSummary.text);
            StringAssert.Contains("2026-03-26", refs.ScheduleInsightMeta.text);
        }

        [Test]
        public void RenderBootStateUpdatesBannerAndTopChip()
        {
            var refs = new AppShellRefs
            {
                TopStatusChipLabel = new Label(),
                HealthBanner = new Label(),
                StageStatusText = new Label(),
            };
            var controller = new AppShellController(refs);

            controller.RenderBootState("Loading runtime", "Preparing shell", new UnityEngine.Color(0.24f, 0.78f, 0.91f));

            Assert.AreEqual("Loading runtime", refs.TopStatusChipLabel.text);
            Assert.AreEqual("Loading runtime", refs.HealthBanner.text);
            Assert.AreEqual("Preparing shell", refs.StageStatusText.text);
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

        [Test]
        public void ToggleMethodsUpdateShellStateAndVisibility()
        {
            var refs = new AppShellRefs
            {
                FocusStageButton = new Button(),
                ToggleCalendarButton = new Button(),
                ToggleChatButton = new Button(),
                ToggleSettingsButton = new Button(),
                CloseSettingsButton = new Button(),
                CalendarSheetHost = new VisualElement(),
                ChatPanelHost = new VisualElement(),
                SettingsDrawer = new VisualElement(),
                SettingsScrim = new VisualElement(),
            };
            var controller = new AppShellController(refs);

            controller.Bind();
            controller.ToggleCalendar();
            controller.ToggleSettings();

            Assert.IsTrue(controller.CurrentState.CalendarExpanded);
            Assert.IsTrue(controller.CurrentState.SettingsOpen);
            Assert.AreEqual(DisplayStyle.Flex, refs.SettingsDrawer.style.display.value);
            Assert.AreEqual(DisplayStyle.Flex, refs.SettingsScrim.style.display.value);

            controller.CloseSettings();
            controller.ToggleChat();

            Assert.IsFalse(controller.CurrentState.SettingsOpen);
            Assert.IsFalse(controller.CurrentState.ChatVisible);
            Assert.AreEqual(DisplayStyle.None, refs.SettingsDrawer.style.display.value);
            Assert.AreEqual(DisplayStyle.None, refs.ChatPanelHost.style.display.value);
        }
    }
}
