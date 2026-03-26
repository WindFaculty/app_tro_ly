using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LocalAssistant.Audio;
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
