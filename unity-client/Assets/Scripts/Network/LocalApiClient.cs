using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using LocalAssistant.Core;
using UnityEngine;
using UnityEngine.Networking;

namespace LocalAssistant.Network
{
    public sealed class LocalApiClient : IAssistantApiClient
    {
        private readonly string baseUrl;

        public LocalApiClient(string rootUrl = "http://127.0.0.1:8096")
        {
            baseUrl = rootUrl.TrimEnd('/');
        }

        public string EventsUrl => baseUrl.Replace("http://", "ws://").Replace("https://", "wss://") + "/v1/events";
        public string AssistantStreamUrl => baseUrl.Replace("http://", "ws://").Replace("https://", "wss://") + "/v1/assistant/stream";

        public Task<HealthResponse> GetHealthAsync() => GetAsync<HealthResponse>("/v1/health");

        public Task<TodayTasksResponse> GetTodayAsync(string date = null)
        {
            var suffix = string.IsNullOrEmpty(date) ? string.Empty : $"?date={date}";
            return GetAsync<TodayTasksResponse>("/v1/tasks/today" + suffix);
        }

        public Task<WeekTasksResponse> GetWeekAsync(string startDate = null)
        {
            var suffix = string.IsNullOrEmpty(startDate) ? string.Empty : $"?start_date={startDate}";
            return GetAsync<WeekTasksResponse>("/v1/tasks/week" + suffix);
        }

        public Task<TaskListResponse> GetInboxAsync() => GetAsync<TaskListResponse>("/v1/tasks/inbox");
        public Task<TaskListResponse> GetCompletedAsync() => GetAsync<TaskListResponse>("/v1/tasks/completed");
        public Task<SettingsPayload> GetSettingsAsync() => GetAsync<SettingsPayload>("/v1/settings");
        public Task<TaskRecord> CompleteTaskAsync(string taskId, CompleteTaskRequestPayload payload = null) => SendJsonAsync<CompleteTaskRequestPayload, TaskRecord>($"/v1/tasks/{taskId}/complete", "POST", payload ?? new CompleteTaskRequestPayload());
        public Task<TaskRecord> RescheduleTaskAsync(string taskId, RescheduleTaskRequestPayload payload) => SendJsonAsync<RescheduleTaskRequestPayload, TaskRecord>($"/v1/tasks/{taskId}/reschedule", "POST", payload ?? new RescheduleTaskRequestPayload());

        public Task<ChatResponsePayload> SendChatAsync(ChatRequestPayload payload) => SendJsonAsync<ChatRequestPayload, ChatResponsePayload>("/v1/chat", "POST", payload);

        public Task<SpeechSttResponse> SendSpeechToTextAsync(byte[] wavBytes, string language = "vi")
        {
            return SendMultipartAsync<SpeechSttResponse>("/v1/speech/stt", wavBytes, language);
        }

        public Task<SettingsPayload> UpdateSettingsAsync(SettingsPayload payload) => SendJsonAsync<SettingsPayload, SettingsPayload>("/v1/settings", "PUT", payload);

        public async Task<AudioClip> DownloadAudioClipAsync(string url)
        {
            var absolute = url.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? url : baseUrl + url;
            using var request = UnityWebRequestMultimedia.GetAudioClip(absolute, AudioType.WAV);
            await request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success)
            {
                throw new InvalidOperationException(request.error);
            }

            return DownloadHandlerAudioClip.GetContent(request);
        }

        private async Task<TResponse> GetAsync<TResponse>(string path)
            where TResponse : class
        {
            using var request = UnityWebRequest.Get(baseUrl + path);
            request.downloadHandler = new DownloadHandlerBuffer();
            await request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success)
            {
                throw new InvalidOperationException(request.error);
            }

            return Deserialize<TResponse>(request.downloadHandler.text);
        }

        private async Task<TResponse> SendJsonAsync<TRequest, TResponse>(string path, string method, TRequest payload)
            where TRequest : class
            where TResponse : class
        {
            var json = UnityJson.Serialize(payload);
            using var request = new UnityWebRequest(baseUrl + path, method);
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            await request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success)
            {
                throw new InvalidOperationException(request.error + "\n" + request.downloadHandler.text);
            }

            return Deserialize<TResponse>(request.downloadHandler.text);
        }

        private async Task<TResponse> SendMultipartAsync<TResponse>(string path, byte[] wavBytes, string language)
            where TResponse : class
        {
            var sections = new List<IMultipartFormSection>
            {
                new MultipartFormFileSection("audio", wavBytes, "speech.wav", "audio/wav"),
                new MultipartFormDataSection("language", language ?? "vi"),
            };

            using var request = UnityWebRequest.Post(baseUrl + path, sections);
            request.downloadHandler = new DownloadHandlerBuffer();
            await request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success)
            {
                throw new InvalidOperationException(request.error + "\n" + request.downloadHandler.text);
            }

            return Deserialize<TResponse>(request.downloadHandler.text);
        }

        private static TResponse Deserialize<TResponse>(string json)
            where TResponse : class
        {
            return UnityJson.Deserialize<TResponse>(json);
        }
    }

    internal static class UnityAsyncOperationExtensions
    {
        public static TaskAwaiter<bool> GetAwaiter(this AsyncOperation operation)
        {
            var taskSource = new TaskCompletionSource<bool>();
            operation.completed += _ => taskSource.TrySetResult(true);
            return taskSource.Task.GetAwaiter();
        }

        public static TaskAwaiter<bool> GetAwaiter(this UnityWebRequestAsyncOperation operation)
        {
            var taskSource = new TaskCompletionSource<bool>();
            operation.completed += _ => taskSource.TrySetResult(true);
            return taskSource.Task.GetAwaiter();
        }
    }
}
