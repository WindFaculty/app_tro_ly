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

        public void Render(IPlannerTaskSnapshotSource taskStore)
        {
            var scheduledCount = CountOf(taskStore.Today.Items);
            var dueSoonCount = CountOf(taskStore.Today.DueSoon);
            var overdueCount = CountOf(taskStore.Today.Overdue);
            var inboxCount = CountOf(taskStore.Inbox.Items);
            var completedCount = CountOf(taskStore.Completed.Items);
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
            SetLabel(home.FocusText, BuildLaneText(taskStore.Today.InProgress, taskStore.Today.Items, "No active focus yet."));
            SetLabel(home.DueSoonText, BuildLaneText(taskStore.Today.DueSoon, null, "No due-soon tasks."));
            SetLabel(home.OverdueText, BuildLaneText(taskStore.Today.Overdue, null, "Nothing overdue."));
            SetLabel(home.StagePlaceholderText, BuildStagePlaceholderText(taskStore));

            if (home.TaskContentText != null)
            {
                home.TaskContentText.style.display = hasAnyHomeTasks ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (home.TaskEmptyStateText != null)
            {
                home.TaskEmptyStateText.text = "No scheduled work yet. Use quick add or ask chat to capture something.";
                home.TaskEmptyStateText.style.display = hasAnyHomeTasks ? DisplayStyle.None : DisplayStyle.Flex;
            }

            if (home.QuickAddInput != null)
            {
                home.QuickAddInput.style.display = DisplayStyle.Flex;
            }

            if (home.QuickAddButton != null)
            {
                home.QuickAddButton.style.display = DisplayStyle.Flex;
            }

            if (home.QuickAddStatusText != null)
            {
                home.QuickAddStatusText.text = quickAddStatusText;
                home.QuickAddStatusText.style.color = new StyleColor(quickAddStatusColor);
                home.QuickAddStatusText.style.display = !string.IsNullOrWhiteSpace(quickAddStatusText)
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

        private static string BuildTodayQueueText(IPlannerTaskSnapshotSource taskStore)
        {
            var scheduled = BuildTaskSection("Scheduled", taskStore.Today.Items, 3);
            var dueSoon = BuildTaskSection("Due soon", taskStore.Today.DueSoon, 2);
            var overdue = BuildTaskSection("Overdue", taskStore.Today.Overdue, 1);
            return $"{scheduled}\n\n{dueSoon}\n\n{overdue}".Trim();
        }

        private static string BuildTaskSection(string title, List<PlannerTaskItem> items, int maxItems)
        {
            if (items == null || items.Count == 0)
            {
                return $"{title}\n- None";
            }

            var result = title + "\n";
            var limit = Math.Min(items.Count, maxItems);
            for (var index = 0; index < limit; index++)
            {
                result += $"- {items[index].Title}{BuildTimeSuffix(items[index])}\n";
            }

            if (items.Count > limit)
            {
                result += $"- +{items.Count - limit} more\n";
            }

            return result.TrimEnd();
        }

        private static string BuildLaneText(List<PlannerTaskItem> primary, List<PlannerTaskItem> fallback, string emptyText)
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
            return $"{task.Title}{BuildTimeSuffix(task)}{suffix}";
        }

        private static string BuildStagePlaceholderText(IPlannerTaskSnapshotSource taskStore)
        {
            return
                "Avatar stage placeholder with an orbit-style shell.\n" +
                "Hybrid streaming remains active while task, chat, and health signals float around the center stage.\n" +
                $"Today {CountOf(taskStore.Today.Items)} | Due soon {CountOf(taskStore.Today.DueSoon)} | Overdue {CountOf(taskStore.Today.Overdue)}";
        }

        private static string BuildTimeSuffix(PlannerTaskItem task)
        {
            if (task == null)
            {
                return string.Empty;
            }

            if (!string.IsNullOrWhiteSpace(task.StartAt))
            {
                return $" @ {task.StartAt}";
            }

            if (!string.IsNullOrWhiteSpace(task.DueAt))
            {
                return $" due {task.DueAt}";
            }

            return string.Empty;
        }

        private static PlannerTaskItem FirstOrDefault(List<PlannerTaskItem> items)
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
