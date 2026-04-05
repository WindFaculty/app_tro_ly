using System;
using LocalAssistant.Tasks;

namespace LocalAssistant.Core
{
    // Shared-core boundaries consume read-only module state instead of owning concrete stores.
    // AssistantApp remains the concrete owner until later extraction phases land.
    public interface IPlannerTaskSnapshotSource
    {
        PlannerTodaySnapshot Today { get; }
        PlannerWeekSnapshot Week { get; }
        PlannerTaskListSnapshot Inbox { get; }
        PlannerTaskListSnapshot Completed { get; }
    }

    public interface IChatStatusSource
    {
        string CurrentRoute { get; }
        string CurrentProvider { get; }
        int FallbackCount { get; }
    }

    public interface ISettingsStateSource
    {
        SettingsPayload Current { get; }
    }

    public interface IAssistantEventBus
    {
        IDisposable Subscribe<TEvent>(Action<TEvent> handler);
        void Publish<TEvent>(TEvent eventPayload);
    }
}
