using LocalAssistant.App;
using LocalAssistant.Core;
using LocalAssistant.Tasks;
using UnityEngine.UIElements;

namespace LocalAssistant.Features.Home
{
    public sealed class HomeScreenController
    {
        private readonly HomeScreenRefs home;

        public HomeScreenController(HomeScreenRefs home)
        {
            this.home = home;
        }

        public void Render(TaskViewModelStore taskStore, string stagePlaceholderText, AppScreen currentScreen)
        {
            var isHome = currentScreen == AppScreen.Today;
            if (home.TaskSummaryText != null)
            {
                home.TaskSummaryText.text = taskStore.BuildOverviewText().Replace("  |  ", "\n");
            }

            if (home.TaskContentText != null)
            {
                home.TaskContentText.text = taskStore.BuildTabText("Today");
                home.TaskContentText.style.display = isHome ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (home.QuickAddInput != null)
            {
                home.QuickAddInput.style.display = isHome ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (home.QuickAddButton != null)
            {
                home.QuickAddButton.style.display = isHome ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (home.StagePlaceholderText != null)
            {
                home.StagePlaceholderText.text = stagePlaceholderText;
            }
        }
    }
}
