using LocalAssistant.App;
using LocalAssistant.Core;
using LocalAssistant.Tasks;
using UnityEngine.UIElements;

namespace LocalAssistant.Features.Home
{
    public sealed class HomeScreenController
    {
        private readonly AssistantUiRefs ui;

        public HomeScreenController(AssistantUiRefs ui)
        {
            this.ui = ui;
        }

        public void Render(TaskViewModelStore taskStore, string stagePlaceholderText, AppScreen currentScreen)
        {
            var isHome = currentScreen == AppScreen.Today;
            if (ui.TaskSummaryText != null)
            {
                ui.TaskSummaryText.text = taskStore.BuildOverviewText().Replace("  |  ", "\n");
            }

            if (ui.TaskContentText != null)
            {
                ui.TaskContentText.text = taskStore.BuildTabText("Today");
                ui.TaskContentText.style.display = isHome ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (ui.QuickAddInput != null)
            {
                ui.QuickAddInput.style.display = isHome ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (ui.QuickAddButton != null)
            {
                ui.QuickAddButton.style.display = isHome ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (ui.StagePlaceholderText != null)
            {
                ui.StagePlaceholderText.text = stagePlaceholderText;
            }
        }
    }
}
