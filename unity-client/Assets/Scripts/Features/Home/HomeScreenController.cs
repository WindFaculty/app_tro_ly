using LocalAssistant.App;
using LocalAssistant.Core;
using LocalAssistant.Tasks;
using System;
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

        public event Action<string> QuickAddRequested;

        public void Bind()
        {
            if (home.QuickAddButton != null)
            {
                home.QuickAddButton.clicked += RequestQuickAdd;
            }
        }

        public void Render(TaskViewModelStore taskStore, AppScreen currentScreen)
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
                home.StagePlaceholderText.text = BuildStagePlaceholderText(taskStore);
            }
        }

        public void SetTaskActionsEnabled(bool isEnabled)
        {
            home.QuickAddInput?.SetEnabled(isEnabled);
            home.QuickAddButton?.SetEnabled(isEnabled);
        }

        public void RequestQuickAdd()
        {
            if (home.QuickAddInput == null)
            {
                return;
            }

            var value = home.QuickAddInput.value?.Trim();
            home.QuickAddInput.value = string.Empty;
            if (!string.IsNullOrWhiteSpace(value))
            {
                QuickAddRequested?.Invoke("Add task " + value);
            }
        }

        private static string BuildStagePlaceholderText(TaskViewModelStore taskStore)
        {
            return $"KHUNG AVATAR\n\nAssistant dang chay theo kieu hybrid stream.\nChat hien route, provider, latency va transcript.\n\n{taskStore.BuildOverviewText()}";
        }
    }
}
