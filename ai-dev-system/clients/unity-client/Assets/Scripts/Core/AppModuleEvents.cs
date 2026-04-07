using LocalAssistant.App;

namespace LocalAssistant.Core
{
    public sealed class PlannerTodayRequestedEvent
    {
    }

    public sealed class PlannerDateOffsetRequestedEvent
    {
        public PlannerDateOffsetRequestedEvent(int dayOffset)
        {
            DayOffset = dayOffset;
        }

        public int DayOffset { get; }
    }

    public sealed class PlannerDaySelectedEvent
    {
        public PlannerDaySelectedEvent(string selectedDate)
        {
            SelectedDate = selectedDate ?? string.Empty;
        }

        public string SelectedDate { get; }
    }

    public sealed class PlannerScreenRequestedEvent
    {
        public PlannerScreenRequestedEvent(AppScreen screen)
        {
            Screen = screen;
        }

        public AppScreen Screen { get; }
    }

    public sealed class PlannerTaskCompletionRequestedEvent
    {
        public PlannerTaskCompletionRequestedEvent(string taskId)
        {
            TaskId = taskId ?? string.Empty;
        }

        public string TaskId { get; }
    }

    public sealed class PlannerTaskSchedulingRequestedEvent
    {
        public PlannerTaskSchedulingRequestedEvent(string taskId, string targetDate)
        {
            TaskId = taskId ?? string.Empty;
            TargetDate = targetDate ?? string.Empty;
        }

        public string TaskId { get; }
        public string TargetDate { get; }
    }

    public sealed class PlannerDateChangedEvent
    {
        public PlannerDateChangedEvent(string selectedDate)
        {
            SelectedDate = selectedDate ?? string.Empty;
        }

        public string SelectedDate { get; }
    }

    public sealed class SubtitleVisibilityChangedEvent
    {
        public SubtitleVisibilityChangedEvent(string text, bool visible)
        {
            Text = text ?? string.Empty;
            Visible = visible;
        }

        public string Text { get; }
        public bool Visible { get; }
    }

    public sealed class ConversationVisualStateChangedEvent
    {
        public ConversationVisualStateChangedEvent(AvatarState state)
        {
            State = state;
        }

        public AvatarState State { get; }
    }

    public sealed class BackendAssistantStateChangedEvent
    {
        public BackendAssistantStateChangedEvent(string state, string animationHint)
        {
            State = state ?? string.Empty;
            AnimationHint = animationHint ?? string.Empty;
        }

        public string State { get; }
        public string AnimationHint { get; }
    }
}
