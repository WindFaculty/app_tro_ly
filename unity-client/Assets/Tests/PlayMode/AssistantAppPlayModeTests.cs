using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LocalAssistant.Avatar;
using LocalAssistant.Core;
using LocalAssistant.Network;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

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

            StringAssert.Contains("Partial", FindText("HealthBanner").text);
            StringAssert.Contains("Some local features are degraded", FindText("ChatLogText").text);
            Assert.IsFalse(FindButton("MicButton").interactable);
            Assert.IsTrue(FindInput("ChatInput").interactable);
            Assert.IsTrue(FindButton("SaveButton").interactable);
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

            Assert.AreEqual("Backend unavailable.", FindText("SettingsStatusText").text);
            Assert.IsFalse(FindInput("ChatInput").interactable);
            Assert.IsFalse(FindButton("MicButton").interactable);
            Assert.IsFalse(FindButton("SaveButton").interactable);
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

            Assert.AreEqual("Backend unavailable.", FindText("SettingsStatusText").text);
            StringAssert.Contains("Cannot reach the local backend: backend down", FindText("ChatLogText").text);
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
            CreateApp(api, new FakeEventsClient());

            yield return null;
            yield return null;

            FindInput("ChatInput").text = "Hello";
            FindButton("SendButton").onClick.Invoke();

            yield return null;
            yield return null;

            var subtitle = FindText("SubtitleText");
            Assert.IsTrue(subtitle.gameObject.activeSelf);
            Assert.AreEqual("Fallback text response", subtitle.text);

            yield return new WaitForSeconds(2.3f);

            Assert.IsFalse(subtitle.gameObject.activeSelf);
            Assert.AreEqual(string.Empty, subtitle.text);
            Assert.AreEqual(AvatarState.Idle, FindAvatarStateMachine().CurrentState);
            Assert.AreEqual("Hello", api.LastChatRequest.message);
            Assert.IsTrue(api.LastChatRequest.include_voice);
        }

        private static AssistantApp CreateApp(FakeApiClient api, FakeEventsClient eventsClient)
        {
            var root = new GameObject("AssistantAppTestRoot");
            var app = root.AddComponent<AssistantApp>();
            app.ConfigureClientsForTests(api, eventsClient, new FakeStreamClient());
            return app;
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

        private static Text FindText(string name)
        {
            return FindGameObject(name).GetComponent<Text>();
        }

        private static Button FindButton(string name)
        {
            return FindGameObject(name).GetComponent<Button>();
        }

        private static InputField FindInput(string name)
        {
            return FindGameObject(name).GetComponent<InputField>();
        }

        private static AvatarStateMachine FindAvatarStateMachine()
        {
            return FindGameObject("AssistantRuntime").GetComponent<AvatarStateMachine>();
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

            public Task<ChatResponsePayload> SendChatAsync(ChatRequestPayload payload)
            {
                LastChatRequest = payload;
                return Task.FromResult(Chat);
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
                throw new InvalidOperationException("Audio download should not be used in text-only fallback tests.");
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
            public bool IsConnected { get; private set; }

            public Task ConnectAsync(CancellationToken cancellationToken)
            {
                IsConnected = false;
                return Task.CompletedTask;
            }

            public Task SendAsync(string payload, CancellationToken cancellationToken)
            {
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
    }
}
