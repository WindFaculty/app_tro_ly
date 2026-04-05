using System.Collections.Generic;
using LocalAssistant.Core;

namespace LocalAssistant.Features.Home
{
    public enum QuickAddStatusKind
    {
        Info,
        Success,
        Warning,
        Error,
    }

    public sealed class HomeQuickAddRequest
    {
        private HomeQuickAddRequest(string text)
        {
            Text = text;
        }

        public string Text { get; }

        public static bool TryCreate(string value, out HomeQuickAddRequest request)
        {
            var trimmed = value?.Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                request = null;
                return false;
            }

            request = new HomeQuickAddRequest(trimmed);
            return true;
        }
    }

    public sealed class HomeQuickAddStatus
    {
        public HomeQuickAddStatus(string message, QuickAddStatusKind kind)
        {
            Message = message ?? string.Empty;
            Kind = kind;
        }

        public string Message { get; }
        public QuickAddStatusKind Kind { get; }
    }

    public sealed class HomeQuickAddApplicationService
    {
        public string CreateAssistantMessage(HomeQuickAddRequest request)
        {
            return request == null ? string.Empty : "Add task " + request.Text;
        }

        public HomeQuickAddStatus BuildPendingStatus()
        {
            return new HomeQuickAddStatus("Sending quick add to the assistant...", QuickAddStatusKind.Info);
        }

        public HomeQuickAddStatus BuildFailureStatus()
        {
            return new HomeQuickAddStatus("Quick add failed. Check the backend and try again.", QuickAddStatusKind.Error);
        }

        public HomeQuickAddStatus BuildNoTaskCreatedStatus()
        {
            return new HomeQuickAddStatus("Quick add reached the assistant, but no task was created. Try a shorter task title.", QuickAddStatusKind.Warning);
        }

        public HomeQuickAddStatus ResolveCompletion(IReadOnlyList<TaskActionReport> actions)
        {
            if (actions != null && actions.Count > 0)
            {
                var action = actions[0];
                var title = string.IsNullOrWhiteSpace(action.title) ? "task" : action.title;
                var detail = string.IsNullOrWhiteSpace(action.detail) ? "Task list refreshed." : action.detail.Trim();
                return new HomeQuickAddStatus($"Added '{title}'. {detail}", QuickAddStatusKind.Success);
            }

            return BuildNoTaskCreatedStatus();
        }
    }
}
