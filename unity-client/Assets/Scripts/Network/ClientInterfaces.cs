using System;
using System.Threading;
using System.Threading.Tasks;
using LocalAssistant.Core;
using UnityEngine;

namespace LocalAssistant.Network
{
    public interface IAssistantApiClient
    {
        string EventsUrl { get; }
        string AssistantStreamUrl { get; }

        Task<HealthResponse> GetHealthAsync();
        Task<TodayTasksResponse> GetTodayAsync(string date = null);
        Task<WeekTasksResponse> GetWeekAsync(string startDate = null);
        Task<TaskListResponse> GetInboxAsync();
        Task<TaskListResponse> GetCompletedAsync();
        Task<SettingsPayload> GetSettingsAsync();
        Task<ChatResponsePayload> SendChatAsync(ChatRequestPayload payload);
        Task<SpeechSttResponse> SendSpeechToTextAsync(byte[] wavBytes, string language = "vi");
        Task<SettingsPayload> UpdateSettingsAsync(SettingsPayload payload);
        Task<AudioClip> DownloadAudioClipAsync(string url);
    }

    public interface IAssistantStreamClient : IDisposable
    {
        Task ConnectAsync(CancellationToken cancellationToken);
        Task SendAsync(string payload, CancellationToken cancellationToken);
        bool TryDequeue(out string message);
        bool IsConnected { get; }
    }

    public interface IAssistantEventsClient : IDisposable
    {
        Task ConnectAsync(CancellationToken cancellationToken);
        bool TryDequeue(out string message);
    }
}
