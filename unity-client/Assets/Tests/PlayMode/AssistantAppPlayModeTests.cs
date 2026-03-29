using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using LocalAssistant.Audio;
using LocalAssistant.Avatar;
using LocalAssistant.Core;
using LocalAssistant.Features.Chat;
using LocalAssistant.Network;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace LocalAssistant.Tests.PlayMode
{
    public class AssistantAppPlayModeTests
    {
        [TearDown]
        public void TearDown()
        {
            DestroyIfExists("AssistantAppTestRoot");
            DestroyIfExists("AssistantCamera");
            DestroyIfExists("EventSystem");
        }

        [UnityTest]
        public IEnumerator AssistantAppStartupWithPartialHealthDisablesOnlyMicAndShowsRecoveryGuidance()
        {
            var api = new FakeApiClient
            {
                Health = CreateHealth(
                    status: "partial",
                    databaseAvailable: true,
                    sttAvailable: false,
                    ttsAvailable: true,
                    llmAvailable: false,
                    recoveryActions: new[] { "Configure assistant_whisper_command.", "Set assistant_groq_api_key." }),
            };
            var eventsClient = new FakeEventsClient();
            CreateApp(api, eventsClient);

            yield return null;
            yield return null;

            StringAssert.Contains("Partial", FindLabel("HealthBanner").text);
            StringAssert.Contains("Some local features are degraded", FindLabel("ChatLogText").text);
            Assert.IsFalse(FindButton("MicButton").enabledSelf);
            Assert.IsTrue(FindTextField("ChatInput").enabledSelf);
            Assert.IsTrue(FindButton("SaveButton").enabledSelf);
            Assert.IsTrue(eventsClient.Connected);
        }

        [UnityTest]
        public IEnumerator AssistantAppStartupWithErrorHealthShowsUnavailableStateAndSkipsEventConnect()
        {
            var api = new FakeApiClient
            {
                Health = CreateHealth(
                    status: "error",
                    databaseAvailable: false,
                    sttAvailable: false,
                    ttsAvailable: false,
                    llmAvailable: false,
                    recoveryActions: new[] { "Check SQLite path." }),
            };
            var eventsClient = new FakeEventsClient();
            CreateApp(api, eventsClient);

            yield return null;
            yield return null;

            Assert.AreEqual("Backend unavailable.", FindLabel("SettingsStatusText").text);
            Assert.IsFalse(FindTextField("ChatInput").enabledSelf);
            Assert.IsFalse(FindButton("MicButton").enabledSelf);
            Assert.IsFalse(FindButton("SaveButton").enabledSelf);
            Assert.AreEqual(AvatarState.Warning, FindAvatarStateMachine().CurrentState);
            Assert.IsFalse(eventsClient.Connected);
        }

        [UnityTest]
        public IEnumerator AssistantAppStartupExceptionShowsBackendUnavailableMessage()
        {
            var api = new FakeApiClient
            {
                HealthException = new InvalidOperationException("backend down"),
            };
            CreateApp(api, new FakeEventsClient());

            yield return null;
            yield return null;

            Assert.AreEqual("Backend unavailable.", FindLabel("SettingsStatusText").text);
            StringAssert.Contains("Cannot reach the local backend: backend down", FindLabel("ChatLogText").text);
            Assert.AreEqual(AvatarState.Warning, FindAvatarStateMachine().CurrentState);
        }

        [UnityTest]
        public IEnumerator AssistantAppTextOnlyChatFallbackUsesSubtitleAndReturnsToIdle()
        {
            var api = new FakeApiClient
            {
                Health = CreateHealth(
                    status: "ready",
                    databaseAvailable: true,
                    sttAvailable: true,
                    ttsAvailable: true,
                    llmAvailable: true,
                    recoveryActions: Array.Empty<string>()),
                Chat = new ChatResponsePayload
                {
                    conversation_id = "conv-test",
                    reply_text = "Fallback text response",
                    animation_hint = "explain",
                    speak = false,
                    task_actions = new List<TaskActionReport>(),
                    cards = new List<ChatCard>(),
                },
            };
            var app = CreateApp(api, new FakeEventsClient());

            yield return null;
            yield return null;

            FindTextField("ChatInput").value = "Hello";
            SubmitCurrentInput(app);

            yield return null;
            yield return null;

            var subtitle = FindLabel("SubtitleText");
            var subtitleCard = FindElement<VisualElement>("SubtitleCard");
            Assert.IsFalse(subtitleCard.ClassListContains("hidden"));
            Assert.AreEqual("Fallback text response", subtitle.text);

            yield return new WaitForSeconds(2.3f);

            Assert.IsTrue(subtitleCard.ClassListContains("hidden"));
            Assert.AreEqual(string.Empty, subtitle.text);
            Assert.AreEqual(AvatarState.Idle, FindAvatarStateMachine().CurrentState);
            Assert.AreEqual("Hello", api.LastChatRequest.message);
            Assert.IsTrue(api.LastChatRequest.include_voice);
        }

        [UnityTest]
        public IEnumerator AssistantAppCompleteTaskActionCallsApiAndUpdatesActionSummary()
        {
            var api = new FakeApiClient
            {
                Health = CreateHealth(
                    status: "ready",
                    databaseAvailable: true,
                    sttAvailable: true,
                    ttsAvailable: true,
                    llmAvailable: true,
                    recoveryActions: Array.Empty<string>()),
                CompleteResponse = new TaskRecord
                {
                    id = "task-1",
                    title = "Hop team",
                    status = "done",
                },
            };
            var app = CreateApp(api, new FakeEventsClient());

            yield return null;
            yield return null;

            RequestCompleteTask(app, "task-1");
            yield return null;
            yield return null;

            Assert.AreEqual("task-1", api.LastCompletedTaskId);
            StringAssert.Contains("Hop team", FindLabel("ChatActionSummaryText").text);
        }

        [UnityTest]
        public IEnumerator AssistantAppScheduleInboxActionCallsApiAndUsesSelectedDate()
        {
            var selectedDate = DateTime.Now.ToString("yyyy-MM-dd");
            var api = new FakeApiClient
            {
                Health = CreateHealth(
                    status: "ready",
                    databaseAvailable: true,
                    sttAvailable: true,
                    ttsAvailable: true,
                    llmAvailable: true,
                    recoveryActions: Array.Empty<string>()),
                RescheduleResponse = new TaskRecord
                {
                    id = "task-2",
                    title = "Gui mail",
                    status = "planned",
                    scheduled_date = selectedDate,
                },
            };
            var app = CreateApp(api, new FakeEventsClient());

            yield return null;
            yield return null;

            RequestScheduleTask(app, "task-2", selectedDate);
            yield return null;
            yield return null;

            Assert.AreEqual("task-2", api.LastRescheduledTaskId);
            Assert.AreEqual(selectedDate, api.LastReschedulePayload.scheduled_date);
            StringAssert.Contains("Gui mail", FindLabel("ChatActionSummaryText").text);
        }

        [UnityTest]
        public IEnumerator AssistantAppSettingsToggleMarksUnsavedChanges()
        {
            var api = new FakeApiClient
            {
                Health = CreateHealth(
                    status: "ready",
                    databaseAvailable: true,
                    sttAvailable: true,
                    ttsAvailable: true,
                    llmAvailable: true,
                    recoveryActions: Array.Empty<string>()),
            };
            CreateApp(api, new FakeEventsClient());

            yield return null;
            yield return null;

            var toggle = FindElement<Toggle>("SpeakRepliesToggle");
            toggle.value = !toggle.value;
            yield return null;

            Assert.AreEqual("Unsaved changes.", FindLabel("SettingsStatusText").text);
        }

        [UnityTest]
        public IEnumerator AssistantAppStreamVoiceLoopDrivesAvatarStates()
        {
            var api = new FakeApiClient
            {
                Health = CreateHealth(
                    status: "ready",
                    databaseAvailable: true,
                    sttAvailable: true,
                    ttsAvailable: true,
                    llmAvailable: true,
                    recoveryActions: Array.Empty<string>()),
            };
            var streamClient = new FakeStreamClient { ConnectedAfterConnect = true };
            CreateApp(api, new FakeEventsClient(), streamClient);

            yield return null;
            yield return null;

            var avatar = FindAvatarStateMachine();
            var observedStates = new List<AvatarState>();
            avatar.StateChanged += observedStates.Add;

            streamClient.EnqueueMessage(UnityJson.Serialize(new AssistantStateChangedEvent
            {
                type = "assistant_state_changed",
                state = "listening",
                animation_hint = "listen",
            }));
            yield return null;
            Assert.AreEqual(AvatarState.Listening, avatar.CurrentState);

            streamClient.EnqueueMessage(UnityJson.Serialize(new AssistantStateChangedEvent
            {
                type = "assistant_state_changed",
                state = "thinking",
                animation_hint = "think",
            }));
            yield return null;
            Assert.AreEqual(AvatarState.Thinking, avatar.CurrentState);

            streamClient.EnqueueMessage(UnityJson.Serialize(new TtsSentenceReadyEvent
            {
                type = "tts_sentence_ready",
                text = "Voice loop reply",
                audio_url = "/speech/cache/test.wav",
            }));
            yield return WaitForAvatarState(avatar, AvatarState.Talking, 20);
            Assert.AreEqual(AvatarState.Talking, avatar.CurrentState);

            FindAudioPlaybackController().Output.Stop();
            yield return WaitForAvatarState(avatar, AvatarState.Idle, 60);
            Assert.AreEqual(AvatarState.Idle, avatar.CurrentState);

            streamClient.EnqueueMessage(UnityJson.Serialize(new AssistantStateChangedEvent
            {
                type = "assistant_state_changed",
                state = "reacting",
                animation_hint = "react",
            }));
            yield return null;
            Assert.AreEqual(AvatarState.Reacting, avatar.CurrentState);

            streamClient.EnqueueMessage(UnityJson.Serialize(new AssistantStateChangedEvent
            {
                type = "assistant_state_changed",
                state = "idle",
                animation_hint = "idle",
            }));
            yield return null;
            Assert.AreEqual(AvatarState.Idle, avatar.CurrentState);

            CollectionAssert.AreEqual(
                new[]
                {
                    AvatarState.Listening,
                    AvatarState.Thinking,
                    AvatarState.Talking,
                    AvatarState.Idle,
                    AvatarState.Reacting,
                    AvatarState.Idle,
                },
                observedStates);
        }

        private static AssistantApp CreateApp(FakeApiClient api, FakeEventsClient eventsClient, FakeStreamClient streamClient = null)
        {
            var root = new GameObject("AssistantAppTestRoot");
            var app = root.AddComponent<AssistantApp>();
            app.ConfigureClientsForTests(api, eventsClient, streamClient ?? new FakeStreamClient());
            return app;
        }

        private static IEnumerator WaitForAvatarState(AvatarStateMachine avatar, AvatarState expectedState, int maxFrames)
        {
            for (var frame = 0; frame < maxFrames && avatar.CurrentState != expectedState; frame++)
            {
                yield return null;
            }

            Assert.AreEqual(expectedState, avatar.CurrentState);
        }

        private static HealthResponse CreateHealth(
            string status,
            bool databaseAvailable,
            bool sttAvailable,
            bool ttsAvailable,
            bool llmAvailable,
            IReadOnlyList<string> recoveryActions)
        {
            return new HealthResponse
            {
                status = status,
                database = new DatabaseHealth { available = databaseAvailable, path = "D:/tmp/app.db" },
                runtimes = new RuntimeHealthCollection
                {
                    stt = new RuntimeHealth { available = sttAvailable, provider = "whisper.cpp" },
                    tts = new RuntimeHealth { available = ttsAvailable, provider = "piper" },
                    llm = new RuntimeHealth { available = llmAvailable, provider = "groq" },
                },
                degraded_features = new List<string>(),
                logs = new LogInfo { app_log = "D:/logs/assistant.log" },
                recovery_actions = new List<string>(recoveryActions ?? Array.Empty<string>()),
            };
        }

        private static Label FindLabel(string name)
        {
            return FindElement<Label>(name);
        }

        private static Button FindButton(string name)
        {
            return FindElement<Button>(name);
        }

        private static TextField FindTextField(string name)
        {
            return FindElement<TextField>(name);
        }

        private static T FindElement<T>(string name) where T : VisualElement
        {
            var element = FindUiDocument().rootVisualElement.Q<T>(name);
            if (element != null)
            {
                return element;
            }

            Assert.Fail("Could not find UI Toolkit element named " + name);
            return null;
        }

        private static UIDocument FindUiDocument()
        {
            foreach (var candidate in Resources.FindObjectsOfTypeAll<UIDocument>())
            {
                if (candidate != null && candidate.gameObject.scene.IsValid())
                {
                    return candidate;
                }
            }

            Assert.Fail("Could not find runtime UIDocument");
            return null;
        }

        private static void SubmitCurrentInput(AssistantApp app)
        {
            var field = typeof(AssistantApp).GetField("chatPanelController", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field, "AssistantApp chatPanelController field was not found.");

            var controller = field.GetValue(app) as ChatPanelController;
            Assert.NotNull(controller, "AssistantApp chatPanelController was not initialized.");
            controller.SubmitCurrentInput();
        }

        private static void RequestCompleteTask(AssistantApp app, string taskId)
        {
            var field = typeof(AssistantApp).GetField("scheduleScreenController", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field, "AssistantApp scheduleScreenController field was not found.");

            var controller = field.GetValue(app) as LocalAssistant.Features.Schedule.ScheduleScreenController;
            Assert.NotNull(controller, "AssistantApp scheduleScreenController was not initialized.");
            controller.RequestCompleteTask(taskId);
        }

        private static void RequestScheduleTask(AssistantApp app, string taskId, string selectedDate)
        {
            var field = typeof(AssistantApp).GetField("scheduleScreenController", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field, "AssistantApp scheduleScreenController field was not found.");

            var controller = field.GetValue(app) as LocalAssistant.Features.Schedule.ScheduleScreenController;
            Assert.NotNull(controller, "AssistantApp scheduleScreenController was not initialized.");
            controller.RequestScheduleTask(taskId, selectedDate);
        }

        private static AvatarStateMachine FindAvatarStateMachine()
        {
            return FindGameObject("AssistantRuntime").GetComponent<AvatarStateMachine>();
        }

        private static AudioPlaybackController FindAudioPlaybackController()
        {
            return FindGameObject("AssistantRuntime").GetComponent<AudioPlaybackController>();
        }

        private static GameObject FindGameObject(string name)
        {
            foreach (var candidate in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (candidate.name == name && candidate.scene.IsValid())
                {
                    return candidate;
                }
            }

            Assert.Fail("Could not find GameObject named " + name);
            return null;
        }

        private static void DestroyIfExists(string name)
        {
            foreach (var candidate in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (candidate.name == name && candidate.scene.IsValid())
                {
                    UnityEngine.Object.DestroyImmediate(candidate);
                    break;
                }
            }
        }

        private sealed class FakeApiClient : IAssistantApiClient
        {
            public string EventsUrl => "ws://127.0.0.1:8096/v1/events";
            public string AssistantStreamUrl => "ws://127.0.0.1:8096/v1/assistant/stream";
            public HealthResponse Health { get; set; } = CreateHealth("ready", true, true, true, true, Array.Empty<string>());
            public SettingsPayload Settings { get; set; } = new SettingsPayload();
            public TodayTasksResponse Today { get; set; } = new TodayTasksResponse();
            public WeekTasksResponse Week { get; set; } = new WeekTasksResponse();
            public TaskListResponse Inbox { get; set; } = new TaskListResponse();
            public TaskListResponse Completed { get; set; } = new TaskListResponse();
            public ChatResponsePayload Chat { get; set; } = new ChatResponsePayload();
            public Exception HealthException { get; set; }
            public ChatRequestPayload LastChatRequest { get; private set; }

            public Task<HealthResponse> GetHealthAsync()
            {
                if (HealthException != null)
                {
                    throw HealthException;
                }

                return Task.FromResult(Health);
            }

            public Task<TodayTasksResponse> GetTodayAsync(string date = null) => Task.FromResult(Today);
            public Task<WeekTasksResponse> GetWeekAsync(string startDate = null) => Task.FromResult(Week);
            public Task<TaskListResponse> GetInboxAsync() => Task.FromResult(Inbox);
            public Task<TaskListResponse> GetCompletedAsync() => Task.FromResult(Completed);
            public Task<SettingsPayload> GetSettingsAsync() => Task.FromResult(Settings);
            public string LastCompletedTaskId { get; private set; }
            public CompleteTaskRequestPayload LastCompletePayload { get; private set; }
            public string LastRescheduledTaskId { get; private set; }
            public RescheduleTaskRequestPayload LastReschedulePayload { get; private set; }
            public TaskRecord CompleteResponse { get; set; } = new TaskRecord { id = "task-complete", title = "Completed task", status = "done" };
            public TaskRecord RescheduleResponse { get; set; } = new TaskRecord { id = "task-reschedule", title = "Rescheduled task", status = "planned" };

            public Task<ChatResponsePayload> SendChatAsync(ChatRequestPayload payload)
            {
                LastChatRequest = payload;
                return Task.FromResult(Chat);
            }

            public Task<TaskRecord> CompleteTaskAsync(string taskId, CompleteTaskRequestPayload payload = null)
            {
                LastCompletedTaskId = taskId;
                LastCompletePayload = payload ?? new CompleteTaskRequestPayload();
                if (string.IsNullOrWhiteSpace(CompleteResponse.id))
                {
                    CompleteResponse.id = taskId;
                }
                return Task.FromResult(CompleteResponse);
            }

            public Task<TaskRecord> RescheduleTaskAsync(string taskId, RescheduleTaskRequestPayload payload)
            {
                LastRescheduledTaskId = taskId;
                LastReschedulePayload = payload ?? new RescheduleTaskRequestPayload();
                if (string.IsNullOrWhiteSpace(RescheduleResponse.id))
                {
                    RescheduleResponse.id = taskId;
                }
                return Task.FromResult(RescheduleResponse);
            }

            public Task<SpeechSttResponse> SendSpeechToTextAsync(byte[] wavBytes, string language = "vi")
            {
                return Task.FromResult(new SpeechSttResponse { text = "stub transcript", language = language, confidence = 1f });
            }

            public Task<SettingsPayload> UpdateSettingsAsync(SettingsPayload payload)
            {
                Settings = payload;
                return Task.FromResult(Settings);
            }

            public Task<AudioClip> DownloadAudioClipAsync(string url)
            {
                const int sampleRate = 16000;
                var lengthSamples = sampleRate / 10;
                var data = new float[lengthSamples];
                for (var i = 0; i < data.Length; i++)
                {
                    data[i] = Mathf.Sin(2f * Mathf.PI * 440f * i / sampleRate) * 0.1f;
                }

                var clip = AudioClip.Create("TestVoiceLoopClip", data.Length, 1, sampleRate, false);
                clip.SetData(data, 0);
                return Task.FromResult(clip);
            }
        }

        private sealed class FakeEventsClient : IAssistantEventsClient
        {
            public bool Connected { get; private set; }

            public Task ConnectAsync(CancellationToken cancellationToken)
            {
                Connected = true;
                return Task.CompletedTask;
            }

            public bool TryDequeue(out string message)
            {
                message = string.Empty;
                return false;
            }

            public void Dispose()
            {
            }
        }

        private sealed class FakeStreamClient : IAssistantStreamClient
        {
            private readonly Queue<string> messages = new();

            public bool ConnectedAfterConnect { get; set; }
            public bool IsConnected { get; private set; }

            public Task ConnectAsync(CancellationToken cancellationToken)
            {
                IsConnected = ConnectedAfterConnect;
                return Task.CompletedTask;
            }

            public Task SendAsync(string payload, CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }

            public bool TryDequeue(out string message)
            {
                if (messages.Count > 0)
                {
                    message = messages.Dequeue();
                    return true;
                }

                message = string.Empty;
                return false;
            }

            public void EnqueueMessage(string message) => messages.Enqueue(message);

            public void Dispose()
            {
            }
        }
    }
}
