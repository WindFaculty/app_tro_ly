using LocalAssistant.App;
using LocalAssistant.Avatar;
using LocalAssistant.Chat;
using LocalAssistant.Core;
using LocalAssistant.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace LocalAssistant.Features.Home
{
    public sealed class HomeScreenController
    {
        private readonly HomeScreenRefs home;
        private bool taskActionsEnabled = true;
        private string quickAddStatusText = string.Empty;
        private Color quickAddStatusColor = new(0.24f, 0.78f, 0.91f, 1f);

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

            if (home.QuickAddInput != null)
            {
                home.QuickAddInput.RegisterCallback<KeyDownEvent>(HandleQuickAddKeyDown);
            }
        }

        public void Render(TaskViewModelStore taskStore, AppScreen currentScreen)
        {
            var isHome = currentScreen == AppScreen.Today;
            var scheduledCount = CountOf(taskStore.Today.items);
            var dueSoonCount = CountOf(taskStore.Today.due_soon);
            var overdueCount = CountOf(taskStore.Today.overdue);
            var inboxCount = CountOf(taskStore.Inbox.items);
            var completedCount = CountOf(taskStore.Completed.items);
            var hasAnyHomeTasks = scheduledCount + dueSoonCount + overdueCount + inboxCount > 0;

            SetLabel(home.TaskSummaryText, BuildHomeSummary(scheduledCount, dueSoonCount, overdueCount, inboxCount, completedCount));
            SetLabel(home.TaskContentText, BuildTodayQueueText(taskStore));
            SetLabel(home.QuickAddHintText, hasAnyHomeTasks
                ? "Capture a short task now and sort it later from Schedule or chat."
                : "Start with one short task. It will land in inbox until you schedule it.");
            SetLabel(home.TodayCountText, scheduledCount.ToString());
            SetLabel(home.DueSoonCountText, dueSoonCount.ToString());
            SetLabel(home.OverdueCountText, overdueCount.ToString());
            SetLabel(home.InboxCountText, inboxCount.ToString());
            SetLabel(home.CompletedCountText, completedCount.ToString());
            SetLabel(home.FocusText, BuildLaneText(taskStore.Today.in_progress, taskStore.Today.items, "No active focus yet."));
            SetLabel(home.DueSoonText, BuildLaneText(taskStore.Today.due_soon, null, "No due-soon tasks."));
            SetLabel(home.OverdueText, BuildLaneText(taskStore.Today.overdue, null, "Nothing overdue."));
            SetLabel(home.StagePlaceholderText, BuildStagePlaceholderText(taskStore));

            if (home.TaskContentText != null)
            {
                home.TaskContentText.style.display = isHome && hasAnyHomeTasks ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (home.TaskEmptyStateText != null)
            {
                home.TaskEmptyStateText.text = "No scheduled work yet. Use quick add or ask chat to capture something.";
                home.TaskEmptyStateText.style.display = hasAnyHomeTasks ? DisplayStyle.None : DisplayStyle.Flex;
            }

            if (home.QuickAddInput != null)
            {
                home.QuickAddInput.style.display = isHome ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (home.QuickAddButton != null)
            {
                home.QuickAddButton.style.display = isHome ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (home.QuickAddStatusText != null)
            {
                home.QuickAddStatusText.text = quickAddStatusText;
                home.QuickAddStatusText.style.color = new StyleColor(quickAddStatusColor);
                home.QuickAddStatusText.style.display = isHome && !string.IsNullOrWhiteSpace(quickAddStatusText)
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
            }
        }

        public void RenderAssistantOrbit(ChatPanelSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return;
            }

            SetLabel(home.HomeChatStatusBadge, snapshot.StatusBadge);
            SetLabel(home.HomeChatStatusTitle, snapshot.StatusTitle);
            SetLabel(home.HomeChatStatusDetail, snapshot.StatusDetail);
        }

        public void SetTaskActionsEnabled(bool isEnabled)
        {
            taskActionsEnabled = isEnabled;
            home.QuickAddInput?.SetEnabled(isEnabled);
            home.QuickAddButton?.SetEnabled(isEnabled);
        }

        public void SetQuickAddStatus(string message, Color color)
        {
            quickAddStatusText = message ?? string.Empty;
            quickAddStatusColor = color;
            if (home.QuickAddStatusText != null)
            {
                home.QuickAddStatusText.text = quickAddStatusText;
                home.QuickAddStatusText.style.color = new StyleColor(quickAddStatusColor);
                home.QuickAddStatusText.style.display = string.IsNullOrWhiteSpace(quickAddStatusText)
                    ? DisplayStyle.None
                    : DisplayStyle.Flex;
            }
        }

        public void HandleQuickAddKeyDown(KeyDownEvent evt)
        {
            if (evt == null)
            {
                return;
            }

            if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
            {
                RequestQuickAdd();
                evt.StopPropagation();
            }
        }

        public void RequestQuickAdd()
        {
            if (home.QuickAddInput == null || !taskActionsEnabled)
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

        public void RenderStage(AvatarState avatarState)
        {
            if (home.HomeAvatarStateBadge != null)
            {
                home.HomeAvatarStateBadge.text = avatarState.ToString().ToUpperInvariant();
            }
        }

        private static string BuildHomeSummary(int scheduledCount, int dueSoonCount, int overdueCount, int inboxCount, int completedCount)
        {
            return
                $"{scheduledCount} scheduled, {dueSoonCount} due soon, and {overdueCount} overdue. " +
                $"Inbox has {inboxCount} items and completed holds {completedCount}.";
        }

        private static string BuildTodayQueueText(TaskViewModelStore taskStore)
        {
            var scheduled = BuildTaskSection("Scheduled", taskStore.Today.items, 3);
            var dueSoon = BuildTaskSection("Due soon", taskStore.Today.due_soon, 2);
            var overdue = BuildTaskSection("Overdue", taskStore.Today.overdue, 1);
            return $"{scheduled}\n\n{dueSoon}\n\n{overdue}".Trim();
        }

        private static string BuildTaskSection(string title, List<TaskRecord> items, int maxItems)
        {
            if (items == null || items.Count == 0)
            {
                return $"{title}\n- None";
            }

            var result = title + "\n";
            var limit = Math.Min(items.Count, maxItems);
            for (var index = 0; index < limit; index++)
            {
                result += $"- {items[index].title}{BuildTimeSuffix(items[index])}\n";
            }

            if (items.Count > limit)
            {
                result += $"- +{items.Count - limit} more\n";
            }

            return result.TrimEnd();
        }

        private static string BuildLaneText(List<TaskRecord> primary, List<TaskRecord> fallback, string emptyText)
        {
            var task = FirstOrDefault(primary) ?? FirstOrDefault(fallback);
            if (task == null)
            {
                return emptyText;
            }

            var additionalCount = Math.Max(CountOf(primary) - 1, 0);
            if (additionalCount == 0 && !ReferenceEquals(primary, fallback))
            {
                additionalCount = Math.Max(CountOf(fallback) - 1, 0);
            }

            var suffix = additionalCount > 0 ? $" +{additionalCount} more" : string.Empty;
            return $"{task.title}{BuildTimeSuffix(task)}{suffix}";
        }

        private static string BuildStagePlaceholderText(TaskViewModelStore taskStore)
        {
            return
                "Avatar stage placeholder with an orbit-style shell.\n" +
                "Hybrid streaming remains active while task, chat, and health signals float around the center stage.\n" +
                $"Today {CountOf(taskStore.Today.items)} | Due soon {CountOf(taskStore.Today.due_soon)} | Overdue {CountOf(taskStore.Today.overdue)}";
        }

        private static string BuildTimeSuffix(TaskRecord task)
        {
            if (task == null)
            {
                return string.Empty;
            }

            if (!string.IsNullOrWhiteSpace(task.start_at))
            {
                return $" @ {task.start_at}";
            }

            if (!string.IsNullOrWhiteSpace(task.due_at))
            {
                return $" due {task.due_at}";
            }

            return string.Empty;
        }

        private static TaskRecord FirstOrDefault(List<TaskRecord> items)
        {
            return items != null && items.Count > 0 ? items[0] : null;
        }

        private static int CountOf<T>(List<T> items)
        {
            return items?.Count ?? 0;
        }

        private static void SetLabel(Label label, string value)
        {
            if (label != null)
            {
                label.text = value;
            }
        }
    }
}
