using LocalAssistant.App;
using LocalAssistant.Core;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace LocalAssistant.Tests.PlayMode
{
    public class ShellModulePlayModeTests
    {
        [Test]
        public void BindKeepsDefaultFourZoneState()
        {
            var shell = CreateRefs();
            var module = new ShellModule(shell);

            module.Bind();

            Assert.IsFalse(module.CurrentState.CalendarExpanded);
            Assert.IsTrue(module.CurrentState.ChatVisible);
            Assert.IsFalse(module.CurrentState.SettingsOpen);
            Assert.AreEqual(ShellRegionFocus.Stage, module.CurrentState.Focus);
            Assert.AreEqual(DisplayStyle.Flex, shell.CalendarSheetHost.style.display.value);
            Assert.AreEqual(DisplayStyle.Flex, shell.ChatPanelHost.style.display.value);
            Assert.AreEqual(DisplayStyle.None, shell.SettingsDrawer.style.display.value);
        }

        [Test]
        public void ToggleCalendarAndSettingsUpdatesRegionState()
        {
            var shell = CreateRefs();
            var module = new ShellModule(shell);

            module.Bind();
            module.ToggleCalendar();
            module.ToggleSettings();

            Assert.IsTrue(module.CurrentState.CalendarExpanded);
            Assert.IsTrue(module.CurrentState.SettingsOpen);
            Assert.AreEqual(ShellRegionFocus.Settings, module.CurrentState.Focus);
            Assert.IsTrue(shell.ToggleCalendarButton.ClassListContains("active"));
            Assert.IsTrue(shell.ToggleSettingsButton.ClassListContains("active"));
            Assert.AreEqual(DisplayStyle.Flex, shell.SettingsDrawer.style.display.value);
            Assert.AreEqual(DisplayStyle.Flex, shell.SettingsScrim.style.display.value);
        }

        [Test]
        public void ToggleChatCanHideAndRestoreChatColumn()
        {
            var shell = CreateRefs();
            var module = new ShellModule(shell);

            module.Bind();
            module.ToggleChat();

            Assert.IsFalse(module.CurrentState.ChatVisible);
            Assert.AreEqual(DisplayStyle.None, shell.ChatPanelHost.style.display.value);

            module.ToggleChat();

            Assert.IsTrue(module.CurrentState.ChatVisible);
            Assert.AreEqual(DisplayStyle.Flex, shell.ChatPanelHost.style.display.value);
        }

        [Test]
        public void BindForwardsRefreshRequested()
        {
            var shell = CreateRefs();
            var controller = new AppShellController(shell);
            var module = new ShellModule(controller);
            var observed = false;
            module.RefreshRequested += () => observed = true;

            module.Bind();
            controller.RequestRefresh();

            Assert.IsTrue(observed);
        }

        private static AppShellRefs CreateRefs()
        {
            return new AppShellRefs
            {
                RefreshButton = new Button(),
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
        }
    }
}
