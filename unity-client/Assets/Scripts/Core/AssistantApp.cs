using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LocalAssistant.App;
using LocalAssistant.Audio;
using LocalAssistant.Avatar;
using LocalAssistant.Chat;
using LocalAssistant.Features.Chat;
using LocalAssistant.Features.Home;
using LocalAssistant.Features.Schedule;
using LocalAssistant.Features.Settings;
using LocalAssistant.Network;
using LocalAssistant.Notifications;
using LocalAssistant.Tasks;
using UnityEngine;
using AvatarSystem;

namespace LocalAssistant.Core
{
    // Coordinates runtime startup, service lifetimes, state stores, UI wiring,
    // backend/event integration, voice flow, and avatar/chat/task synchronization.
    public sealed class AssistantApp : MonoBehaviour
    {
        private static readonly Color InfoColor = new(0.31f, 0.36f, 0.43f, 1f);
        private static readonly Color LoadingColor = new(0.24f, 0.78f, 0.91f, 1f);
        private static readonly Color WarningColor = new(0.74f, 0.49f, 0.14f, 1f);
        private static readonly Color SuccessColor = new(0.16f, 0.55f, 0.33f, 1f);
        private static readonly Color ErrorColor = new(0.67f, 0.24f, 0.20f, 1f);

        private AssistantUiRefs ui;
        private IAssistantApiClient apiClient;
        private IAssistantEventsClient eventsClient;
        private IAssistantStreamClient assistantStreamClient;
        private CancellationTokenSource cancellationTokenSource;
        private readonly TaskViewModelStore taskStore = new();
        private readonly ChatViewModelStore chatStore = new();
        private readonly SettingsViewModelStore settingsStore = new();
        private readonly Queue<TtsSentenceReadyEvent> speechQueue = new();

        private AvatarStateMachine avatarStateMachine;
        private AvatarConversationBridge avatarConversationBridge;
        private LipSyncController lipSyncController;
        private AudioPlaybackController audioPlaybackController;
        private SubtitlePresenter subtitlePresenter;
        private ReminderPresenter reminderPresenter;
        private AppRouter appRouter;
        private AppShellController appShellController;
        private HomeScreenController homeScreenController;
        private ScheduleScreenController scheduleScreenController;
        private SettingsScreenController settingsScreenController;
        private ChatPanelController chatPanelController;

        private HealthResponse currentHealth = new();
        private AppScreen currentScreen = AppScreen.Week;
        private string selectedDate = string.Empty;
        private bool isBindingSettingsUi;
        private bool continuousVoiceEnabled;
        private bool isRecording;
        private bool isPlayingSpeechQueue;
        private bool autoResumeListening;
        private bool isInitializing;
        private bool isRefreshingWorkspace;
        private bool isReloadingTaskViews;
        private bool isSubmittingChatTurn;
        private bool isMutatingTask;
        private bool isSettingsRequestInFlight;
        private bool awaitingQuickAddResult;
        private AudioClip recordingClip;

        // UI init
        private void Awake()
        {
            selectedDate = DateTime.Now.ToString("yyyy-MM-dd");
            cancellationTokenSource = new CancellationTokenSource();
            var composition = AppCompositionRoot.Compose(gameObject, transform);
            ui = composition.Ui;
            avatarStateMachine = composition.AvatarStateMachine;
            avatarConversationBridge = composition.AvatarConversationBridge;
            lipSyncController = composition.LipSyncController;
            audioPlaybackController = composition.AudioPlaybackController;
            subtitlePresenter = composition.SubtitlePresenter;
            reminderPresenter = composition.ReminderPresenter;
            avatarStateMachine.StateChanged += OnAvatarStateChanged;
            audioPlaybackController.PlaybackCompleted += OnAudioPlaybackCompleted;
            appShellController = new AppShellController(ui.Shell);
            appRouter = new AppRouter(ui.Shell, ui.Schedule, ui.Settings, ui.Chat, OnScreenChanged);
            homeScreenController = new HomeScreenController(ui.Home);
            scheduleScreenController = new ScheduleScreenController(ui.Schedule);
            settingsScreenController = new SettingsScreenController(ui.Settings);
            chatPanelController = new ChatPanelController(ui.Chat);
            appShellController.Bind();
            appRouter.BindTabs();
            homeScreenController.Bind();
            scheduleScreenController.Bind();
            settingsScreenController.Bind();
            chatPanelController.Bind();
            appShellController.RefreshRequested += RefreshWorkspace;
            homeScreenController.QuickAddRequested += SubmitQuickAddMessage;
            scheduleScreenController.TodayRequested += ResetSelectedDateToToday;
            scheduleScreenController.DateOffsetRequested += ShiftSelectedDate;
            scheduleScreenController.DaySelected += SelectDate;
            scheduleScreenController.InboxRequested += () => appRouter.Navigate(AppScreen.Inbox);
            scheduleScreenController.CompletedRequested += () => appRouter.Navigate(AppScreen.Completed);
            scheduleScreenController.CompleteTaskRequested += CompleteTaskFromSchedule;
            scheduleScreenController.ScheduleTaskRequested += ScheduleTaskFromInbox;
            settingsScreenController.ReloadRequested += ReloadSettings;
            settingsScreenController.SaveRequested += SaveSettings;
            settingsScreenController.SpeakRepliesChanged += OnSpeakRepliesChanged;
            settingsScreenController.TranscriptPreviewChanged += OnTranscriptPreviewChanged;
            settingsScreenController.MiniAssistantChanged += OnMiniAssistantChanged;
            settingsScreenController.ReminderSpeechChanged += OnReminderSpeechChanged;
            chatPanelController.SendRequested += SubmitFromMessage;
            chatPanelController.MicRequested += ToggleVoiceSession;

            chatStore.SetThinking(true);
            chatStore.SetSystemStatus("LOADING", "Loading local workspace", "Connecting to the backend and preparing Home, Schedule, Settings, and Chat.");
            appShellController.RenderBootState("Loading runtime", "Connecting to the local backend and preparing shell screens...", LoadingColor);
            homeScreenController.SetQuickAddStatus("Quick add becomes available after the workspace loads.", InfoColor);
            SetSettingsStatus("Loading settings...", InfoColor);

            appRouter.Navigate(currentScreen);
            RefreshTaskView();
            RefreshChatPanel();
            RefreshStagePanel();
            ApplyInteractionState(currentHealth);
        }

        private async void Start() => await InitializeAsync();

        private void Update()
        {
            DrainEvents();
            DrainAssistantStream();
        }

        private void OnDestroy()
        {
            if (avatarStateMachine != null) avatarStateMachine.StateChanged -= OnAvatarStateChanged;
            if (audioPlaybackController != null) audioPlaybackController.PlaybackCompleted -= OnAudioPlaybackCompleted;
            cancellationTokenSource?.Cancel();
            cancellationTokenSource?.Dispose();
            eventsClient?.Dispose();
            assistantStreamClient?.Dispose();
        }

        public void ConfigureClientsForTests(IAssistantApiClient injectedApiClient, IAssistantEventsClient injectedEventsClient, IAssistantStreamClient injectedStreamClient = null)
        {
            apiClient = injectedApiClient;
            eventsClient = injectedEventsClient;
            assistantStreamClient = injectedStreamClient;
        }

        public async void BeginPushToTalk() => await BeginListeningAsync();
        public async void EndPushToTalk() => await FinishVoiceCaptureAsync(false);

        private async Task InitializeAsync()
        {
            isInitializing = true;
            ApplyInteractionState(currentHealth);
            try
            {
                apiClient ??= new LocalApiClient();
                ApplyHealth(await apiClient.GetHealthAsync());
                AppendRecoveryGuidance(currentHealth);
                if (currentHealth.status == "error")
                {
                    HandleBackendUnavailableState();
                    return;
                }

                await LoadSettingsAsync("Settings loaded from local backend.");
                await ReloadAllAsync();
                chatStore.SetThinking(false);
                SyncHealthStatusToChat(currentHealth);
                chatStore.AddAssistant("Hybrid assistant is ready. Ask about today, planning, or start voice mode.");
                RefreshChatPanel();
                eventsClient ??= new EventsClient(apiClient.EventsUrl);
                await eventsClient.ConnectAsync(cancellationTokenSource.Token);
                await TryConnectAssistantStreamAsync();
            }
            catch (Exception exception)
            {
                ApplyHealth(new HealthResponse { status = "error" });
                chatStore.SetThinking(false);
                chatStore.AddAssistant("Cannot reach the local backend: " + exception.Message);
                HandleBackendUnavailableState();
                RefreshChatPanel();
                avatarStateMachine.SetState(AvatarState.Warning);
            }
            finally
            {
                isInitializing = false;
                ApplyInteractionState(currentHealth);
                RefreshChatPanel();
                RefreshStagePanel();
            }
        }

        private async Task ReloadAllAsync()
        {
            taskStore.ApplyToday(await apiClient.GetTodayAsync(selectedDate));
            taskStore.ApplyWeek(await apiClient.GetWeekAsync(selectedDate));
            taskStore.ApplyInbox(await apiClient.GetInboxAsync());
            taskStore.ApplyCompleted(await apiClient.GetCompletedAsync());
            RefreshTaskView();
        }

        private async Task TryConnectAssistantStreamAsync()
        {
            try
            {
                assistantStreamClient ??= new AssistantStreamClient(apiClient.AssistantStreamUrl);
                await assistantStreamClient.ConnectAsync(cancellationTokenSource.Token);
                chatStore.SessionId = string.IsNullOrEmpty(chatStore.SessionId) ? Guid.NewGuid().ToString("N") : chatStore.SessionId;
                await SendStreamAsync(new AssistantStreamRequestPayload { type = "session_start", session_id = chatStore.SessionId, selected_date = selectedDate });
            }
            catch (Exception exception)
            {
                assistantStreamClient?.Dispose();
                assistantStreamClient = null;
                chatStore.AddAssistant("Assistant stream unavailable, using compatibility mode: " + exception.Message);
                RefreshChatPanel();
            }
        }

        private async Task SendStreamAsync(AssistantStreamRequestPayload payload)
        {
            if (assistantStreamClient == null || !assistantStreamClient.IsConnected) return;
            await assistantStreamClient.SendAsync(UnityJson.Serialize(payload), cancellationTokenSource.Token);
        }

        private async void RefreshWorkspace()
        {
            if (isRefreshingWorkspace || isInitializing)
            {
                return;
            }

            isRefreshingWorkspace = true;
            chatStore.SetSystemStatus("LOADING", "Refreshing workspace", "Pulling health, settings, and task data from the local backend.");
            appShellController.RenderBootState("Refreshing workspace", "Synchronizing runtime health, settings, and task views...", LoadingColor);
            SetSettingsStatus("Refreshing settings...", InfoColor);
            ApplyInteractionState(currentHealth);
            RefreshChatPanel();

            try
            {
                ApplyHealth(await apiClient.GetHealthAsync());
                if (currentHealth.status == "error")
                {
                    HandleBackendUnavailableState();
                    return;
                }

                await LoadSettingsAsync("Workspace refreshed.");
                await ReloadAllAsync();
                SyncHealthStatusToChat(currentHealth);
                RefreshChatPanel();
            }
            catch (Exception exception)
            {
                chatStore.AddAssistant("Refresh failed: " + exception.Message);
                chatStore.SetSystemStatus("ERROR", "Workspace refresh failed", "Check the backend process and try refresh again.");
                SetSettingsStatus("Refresh failed.", ErrorColor);
                RefreshChatPanel();
            }
            finally
            {
                isRefreshingWorkspace = false;
                ApplyInteractionState(currentHealth);
                RefreshChatPanel();
            }
        }

        private async void SubmitFromMessage(string message) => await SubmitChatAsync(message, false, false);

        private async void SubmitQuickAddMessage(string message) => await SubmitChatAsync(message, false, true);

        private async Task SubmitChatAsync(string message, bool fromVoice, bool fromQuickAdd)
        {
            if (string.IsNullOrWhiteSpace(message) || isSubmittingChatTurn)
            {
                return;
            }

            isSubmittingChatTurn = true;
            awaitingQuickAddResult = fromQuickAdd;
            if (fromQuickAdd)
            {
                homeScreenController.SetQuickAddStatus("Sending quick add to the assistant...", LoadingColor);
            }

            chatStore.ClearSystemStatus();
            chatStore.AddUser(message);
            chatStore.SetListening(false);
            chatStore.SetThinking(true);
            chatStore.SetTalking(false);
            chatStore.SetTaskActions(Array.Empty<TaskActionReport>());
            chatStore.ResetAssistantDraft();
            if (fromVoice && settingsStore.Current.voice.show_transcript_preview)
            {
                chatStore.SetTranscriptPreview(message);
            }

            avatarStateMachine.SetState(AvatarState.Thinking);
            ApplyInteractionState(currentHealth);
            RefreshChatPanel();
            try
            {
                if (assistantStreamClient != null && assistantStreamClient.IsConnected)
                {
                    await SendStreamAsync(new AssistantStreamRequestPayload
                    {
                        type = "text_turn",
                        session_id = chatStore.SessionId,
                        conversation_id = chatStore.ConversationId,
                        message = message,
                        selected_date = selectedDate,
                        voice_mode = fromVoice,
                    });
                    return;
                }

                await ApplyCompatibilityChatResponseAsync(await apiClient.SendChatAsync(
                    ChatViewModelStore.CreateRequest(message, chatStore.ConversationId, selectedDate, settingsStore.Current.voice.speak_replies)));
                CompleteChatTurn();
            }
            catch (Exception exception)
            {
                chatStore.SetThinking(false);
                chatStore.SetTalking(false);
                chatStore.AddAssistant("Request failed: " + exception.Message);
                chatStore.SetSystemStatus("ERROR", "Chat request failed", "Check the backend and try the request again.");
                ResolvePendingQuickAdd(Array.Empty<TaskActionReport>(), "Quick add failed. Check the backend and try again.", ErrorColor);
                RefreshChatPanel();
                CompleteChatTurn();
            }
        }

        private async Task ApplyCompatibilityChatResponseAsync(ChatResponsePayload response)
        {
            chatStore.ConversationId = response.conversation_id;
            chatStore.AddAssistant(response.reply_text);
            chatStore.SetThinking(false);
            chatStore.SetDiagnostics(response.route, response.provider, response.latency_ms, response.fallback_used);
            chatStore.SetTaskActions(response.task_actions);
            avatarStateMachine.ApplyBackendState("talking", response.animation_hint);
            ResolvePendingQuickAdd(response.task_actions, "Quick add reached the assistant, but no task was created. Try a shorter task title.", WarningColor);
            RefreshChatPanel();
            if (response.task_actions != null && response.task_actions.Count > 0)
            {
                await ReloadAllAsync();
            }

            if (response.speak && !string.IsNullOrEmpty(response.audio_url))
            {
                _ = PlaySentenceAsync(response.audio_url, response.reply_text);
            }
            else
            {
                subtitlePresenter.Show(response.reply_text);
                Invoke(nameof(ClearSubtitleAndIdle), 2.2f);
            }
        }

        private async void ToggleVoiceSession()
        {
            if (!HealthRecoveryAdvisor.CanUseMic(currentHealth) || isInitializing || isRefreshingWorkspace || isMutatingTask)
            {
                return;
            }

            continuousVoiceEnabled = !continuousVoiceEnabled;
            if (continuousVoiceEnabled)
            {
                await BeginListeningAsync();
            }
            else
            {
                await FinishVoiceCaptureAsync(true);
            }
        }

        private async Task BeginListeningAsync()
        {
            if (isRecording || isSubmittingChatTurn || isMutatingTask || isInitializing || isRefreshingWorkspace)
            {
                return;
            }

            if (Microphone.devices.Length == 0)
            {
                chatStore.AddAssistant("Microphone is not available.");
                RefreshChatPanel();
                return;
            }

            recordingClip = Microphone.Start(null, false, 30, 16000);
            isRecording = true;
            chatStore.SetListening(true);
            chatStore.SetThinking(false);
            chatStore.SetTalking(false);
            avatarStateMachine.SetState(AvatarState.Listening);
            avatarConversationBridge?.OnListeningStart();
            await Task.CompletedTask;
            RefreshChatPanel();
        }

        private async Task FinishVoiceCaptureAsync(bool sendStop)
        {
            if (!isRecording) return;
            var position = Microphone.GetPosition(null);
            Microphone.End(null);
            isRecording = false;
            chatStore.SetListening(false);
            chatStore.SetThinking(true);
            chatStore.SetTalking(false);
            avatarStateMachine.SetState(AvatarState.Thinking);
            avatarConversationBridge?.OnListeningEnd();
            RefreshChatPanel();
            if (recordingClip == null || position <= 0) return;
            if (assistantStreamClient != null && assistantStreamClient.IsConnected)
            {
                await SendStreamAsync(new AssistantStreamRequestPayload
                {
                    type = "voice_end",
                    session_id = chatStore.SessionId,
                    conversation_id = chatStore.ConversationId,
                    selected_date = selectedDate,
                    voice_mode = true,
                    audio_base64 = Convert.ToBase64String(WavEncoder.Encode(TrimClip(recordingClip, position))),
                });
            }
            else
            {
                var transcript = await apiClient.SendSpeechToTextAsync(WavEncoder.Encode(TrimClip(recordingClip, position)));
                chatStore.SetTranscriptPreview(settingsStore.Current.voice.show_transcript_preview ? transcript.text : string.Empty);
                RefreshChatPanel();
                await SubmitChatAsync(transcript.text, true, false);
            }
            if (sendStop) autoResumeListening = false;
        }

        private void DrainEvents()
        {
            if (eventsClient == null) return;
            while (eventsClient.TryDequeue(out var payload))
            {
                var envelope = UnityJson.Deserialize<EventEnvelope>(payload);
                if (envelope.type == "reminder_due")
                {
                    var reminder = UnityJson.Deserialize<ReminderDueEvent>(payload);
                    if (reminder != null)
                    {
                        reminderPresenter.Push(reminder);
                    }
                }
                else if (envelope.type == "task_updated")
                {
                    _ = SafeReloadTasksAsync();
                }
                else if (envelope.type == "assistant_state_changed")
                {
                    var assistantState = UnityJson.Deserialize<AssistantStateChangedEvent>(payload);
                    if (assistantState != null)
                    {
                        avatarStateMachine.ApplyBackendState(assistantState.state, assistantState.animation_hint);
                    }
                }
            }
        }

        private void DrainAssistantStream()
        {
            if (assistantStreamClient == null) return;
            while (assistantStreamClient.TryDequeue(out var payload))
            {
                var envelope = UnityJson.Deserialize<EventEnvelope>(payload);
                switch (envelope.type)
                {
                    case "transcript_partial":
                        var partial = UnityJson.Deserialize<TranscriptEvent>(payload);
                        if (partial != null && settingsStore.Current.voice.show_transcript_preview)
                        {
                            chatStore.SetTranscriptPreview(partial.text);
                            RefreshChatPanel();
                        }
                        break;
                    case "transcript_final":
                        var transcript = UnityJson.Deserialize<TranscriptEvent>(payload);
                        if (transcript != null && !string.IsNullOrWhiteSpace(transcript.text))
                        {
                            chatStore.SetTranscriptPreview(settingsStore.Current.voice.show_transcript_preview ? transcript.text : string.Empty);
                            chatStore.AddUser(transcript.text);
                            chatStore.SetThinking(true);
                            chatStore.ResetAssistantDraft();
                            RefreshChatPanel();
                        }
                        break;
                    case "route_selected":
                        var route = UnityJson.Deserialize<RouteSelectedEvent>(payload);
                        if (route != null)
                        {
                            chatStore.SetDiagnostics(route.route, route.provider, 0, false);
                            RefreshChatPanel();
                        }
                        break;
                    case "assistant_chunk":
                        var chunk = UnityJson.Deserialize<AssistantChunkEvent>(payload);
                        if (chunk != null)
                        {
                            chatStore.AppendAssistantDraft(chunk.text + " ");
                            RefreshChatPanel();
                        }
                        break;
                    case "assistant_final":
                        var finalReply = UnityJson.Deserialize<AssistantFinalEvent>(payload);
                        if (finalReply != null)
                        {
                            chatStore.ConversationId = finalReply.conversation_id;
                            chatStore.SessionId = finalReply.session_id;
                            chatStore.FinalizeAssistantDraft(finalReply.reply_text);
                            chatStore.SetThinking(false);
                            chatStore.SetDiagnostics(finalReply.route, finalReply.provider, finalReply.latency_ms, finalReply.fallback_used);
                            chatStore.SetTaskActions(finalReply.task_actions);
                            ResolvePendingQuickAdd(finalReply.task_actions, "Quick add reached the assistant, but no task was created. Try a shorter task title.", WarningColor);
                            RefreshChatPanel();
                            if (finalReply.task_actions != null && finalReply.task_actions.Count > 0)
                            {
                                _ = SafeReloadTasksAsync();
                            }

                            if (!settingsStore.Current.voice.speak_replies)
                            {
                                subtitlePresenter.Show(finalReply.reply_text);
                                Invoke(nameof(ClearSubtitleAndIdle), 2.2f);
                            }

                            CompleteChatTurn();
                        }
                        break;
                    case "tts_sentence_ready":
                        var sentence = UnityJson.Deserialize<TtsSentenceReadyEvent>(payload);
                        if (sentence != null) _ = PlaySentenceAsync(sentence.audio_url, sentence.text);
                        break;
                    case "speech_started":
                        autoResumeListening = continuousVoiceEnabled;
                        break;
                    case "speech_finished":
                        if (!isPlayingSpeechQueue && autoResumeListening && continuousVoiceEnabled) _ = BeginListeningAsync();
                        break;
                    case "assistant_state_changed":
                        var state = UnityJson.Deserialize<AssistantStateChangedEvent>(payload);
                        if (state != null) avatarStateMachine.ApplyBackendState(state.state, state.animation_hint);
                        break;
                }
            }
        }

        private async Task PlaySentenceAsync(string audioUrl, string subtitle)
        {
            speechQueue.Enqueue(new TtsSentenceReadyEvent { audio_url = audioUrl, text = subtitle });
            if (isPlayingSpeechQueue) return;
            isPlayingSpeechQueue = true;
            try
            {
                chatStore.SetTalking(true);
                chatStore.SetThinking(false);
                RefreshChatPanel();
                avatarConversationBridge?.OnSpeakingStart();
                while (speechQueue.Count > 0)
                {
                    var sentence = speechQueue.Dequeue();
                    var clip = await apiClient.DownloadAudioClipAsync(sentence.audio_url);
                    subtitlePresenter.Show(sentence.text);
                    audioPlaybackController.Play(clip, sentence.text, subtitlePresenter, avatarStateMachine);
                    while (audioPlaybackController.IsPlaying) await Task.Delay(50);
                }
                avatarConversationBridge?.OnSpeakingEnd();
            }
            finally
            {
                isPlayingSpeechQueue = false;
                chatStore.SetTalking(false);
                RefreshChatPanel();
                if (autoResumeListening && continuousVoiceEnabled) await BeginListeningAsync();
            }
        }

        private void OnScreenChanged(AppScreen screen)
        {
            currentScreen = screen;
            RefreshTaskView();
            RefreshStagePanel();
        }

        private void RefreshTaskView()
        {
            if (!HasLiveUi()) return;
            homeScreenController.Render(taskStore, currentScreen);
            scheduleScreenController.Render(taskStore, currentScreen, selectedDate);
            RefreshStagePanel();
        }

        private async void ResetSelectedDateToToday() => await SetSelectedDateAsync(DateTime.Now.ToString("yyyy-MM-dd"));

        private async void ShiftSelectedDate(int dayOffset)
        {
            var anchor = DateTime.TryParse(selectedDate, out var parsed) ? parsed : DateTime.Now.Date;
            await SetSelectedDateAsync(anchor.AddDays(dayOffset).ToString("yyyy-MM-dd"));
        }

        private async void SelectDate(string dateValue) => await SetSelectedDateAsync(dateValue);

        private async Task SetSelectedDateAsync(string nextDate)
        {
            if (string.IsNullOrWhiteSpace(nextDate) || string.Equals(selectedDate, nextDate, StringComparison.Ordinal) || isReloadingTaskViews)
            {
                return;
            }

            selectedDate = nextDate;
            isReloadingTaskViews = true;
            chatStore.SetSystemStatus("LOADING", "Loading selected day", $"Updating Home and Schedule for {selectedDate}.");
            ApplyInteractionState(currentHealth);
            RefreshChatPanel();
            try
            {
                await ReloadAllAsync();
                SyncHealthStatusToChat(currentHealth);
                RefreshChatPanel();
            }
            catch (Exception exception)
            {
                chatStore.AddAssistant("Could not load the selected day: " + exception.Message);
                chatStore.SetSystemStatus("ERROR", "Selected day failed to load", "Refresh the workspace or pick another date.");
                RefreshChatPanel();
            }
            finally
            {
                isReloadingTaskViews = false;
                ApplyInteractionState(currentHealth);
            }
        }

        private async void CompleteTaskFromSchedule(string taskId)
        {
            if (string.IsNullOrWhiteSpace(taskId) || isMutatingTask)
            {
                return;
            }

            isMutatingTask = true;
            chatStore.SetSystemStatus("UPDATING", "Completing task", "Applying the change and refreshing task lists.");
            ApplyInteractionState(currentHealth);
            RefreshChatPanel();
            try
            {
                var updatedTask = await apiClient.CompleteTaskAsync(taskId, new CompleteTaskRequestPayload());
                chatStore.SetTaskActions(new[]
                {
                    new TaskActionReport
                    {
                        type = "complete_task",
                        status = "applied",
                        task_id = updatedTask.id,
                        title = updatedTask.title,
                        detail = "Updated directly from the Schedule screen.",
                    },
                });
                await ReloadAllAsync();
                SyncHealthStatusToChat(currentHealth);
                RefreshChatPanel();
            }
            catch (Exception exception)
            {
                chatStore.AddAssistant("Could not complete the task: " + exception.Message);
                chatStore.SetSystemStatus("ERROR", "Task update failed", "Check the backend and try again.");
                RefreshChatPanel();
            }
            finally
            {
                isMutatingTask = false;
                ApplyInteractionState(currentHealth);
            }
        }

        private async void ScheduleTaskFromInbox(string taskId, string targetDate)
        {
            if (string.IsNullOrWhiteSpace(taskId) || string.IsNullOrWhiteSpace(targetDate) || isMutatingTask)
            {
                return;
            }

            isMutatingTask = true;
            chatStore.SetSystemStatus("UPDATING", "Scheduling inbox task", $"Moving the item onto {targetDate}.");
            ApplyInteractionState(currentHealth);
            RefreshChatPanel();
            try
            {
                var updatedTask = await apiClient.RescheduleTaskAsync(taskId, new RescheduleTaskRequestPayload
                {
                    scheduled_date = targetDate,
                });
                chatStore.SetTaskActions(new[]
                {
                    new TaskActionReport
                    {
                        type = "reschedule_task",
                        status = "applied",
                        task_id = updatedTask.id,
                        title = updatedTask.title,
                        detail = $"Scheduled to {targetDate} from the Schedule inbox view.",
                    },
                });
                await ReloadAllAsync();
                SyncHealthStatusToChat(currentHealth);
                RefreshChatPanel();
            }
            catch (Exception exception)
            {
                chatStore.AddAssistant("Could not schedule the inbox task: " + exception.Message);
                chatStore.SetSystemStatus("ERROR", "Scheduling failed", "Check the backend and try again.");
                RefreshChatPanel();
            }
            finally
            {
                isMutatingTask = false;
                ApplyInteractionState(currentHealth);
            }
        }

        private async Task SafeReloadTasksAsync()
        {
            if (isReloadingTaskViews || isInitializing || isRefreshingWorkspace || isMutatingTask)
            {
                return;
            }

            isReloadingTaskViews = true;
            ApplyInteractionState(currentHealth);
            try
            {
                await ReloadAllAsync();
            }
            catch (Exception exception)
            {
                chatStore.AddAssistant("Task refresh failed: " + exception.Message);
                RefreshChatPanel();
            }
            finally
            {
                isReloadingTaskViews = false;
                ApplyInteractionState(currentHealth);
            }
        }

        private void RefreshChatPanel()
        {
            if (HasLiveUi())
            {
                var snapshot = chatStore.BuildPanelSnapshot(settingsStore.Current.voice.show_transcript_preview);
                chatPanelController.Render(snapshot);
                homeScreenController?.RenderAssistantOrbit(snapshot);
            }
        }

        private void ApplyHealth(HealthResponse health)
        {
            if (!HasLiveUi()) return;
            currentHealth = HealthResponseNormalizer.Normalize(health);
            appShellController.RenderHealth(currentHealth);
            SyncHealthStatusToChat(currentHealth);
            ApplyInteractionState(currentHealth);
            RefreshStagePanel();
            RefreshChatPanel();
        }

        private async void ReloadSettings()
        {
            if (isSettingsRequestInFlight)
            {
                return;
            }

            try
            {
                isSettingsRequestInFlight = true;
                SetSettingsStatus("Reloading settings...", LoadingColor);
                ApplyInteractionState(currentHealth);
                await LoadSettingsAsync("Settings reloaded.");
            }
            catch (Exception exception)
            {
                SetSettingsStatus("Reload failed: " + exception.Message, ErrorColor);
            }
            finally
            {
                isSettingsRequestInFlight = false;
                ApplyInteractionState(currentHealth);
            }
        }

        private async void SaveSettings()
        {
            if (isSettingsRequestInFlight || !settingsStore.HasUnsavedChanges())
            {
                if (!settingsStore.HasUnsavedChanges())
                {
                    SetSettingsStatus("No changes to save.", InfoColor);
                }
                return;
            }

            try
            {
                isSettingsRequestInFlight = true;
                SetSettingsStatus("Saving changes...", LoadingColor);
                ApplyInteractionState(currentHealth);
                settingsStore.Apply(await apiClient.UpdateSettingsAsync(settingsStore.Snapshot()));
                ApplySettingsToUi();
                SetSettingsStatus("Settings saved.", SuccessColor);
            }
            catch (Exception exception)
            {
                SetSettingsStatus("Save failed: " + exception.Message, ErrorColor);
            }
            finally
            {
                isSettingsRequestInFlight = false;
                ApplyInteractionState(currentHealth);
            }
        }

        private async Task LoadSettingsAsync(string statusMessage)
        {
            settingsStore.Apply(await apiClient.GetSettingsAsync());
            ApplySettingsToUi();
            SetSettingsStatus(statusMessage, InfoColor);
        }

        private void ApplySettingsToUi()
        {
            if (!HasLiveUi()) return;
            isBindingSettingsUi = true;
            try
            {
                settingsScreenController.Render(settingsStore);
            }
            finally
            {
                isBindingSettingsUi = false;
            }

            RefreshStagePanel();
        }

        private void RefreshSettingsPanel()
        {
            if (HasLiveUi())
            {
                settingsScreenController.Render(settingsStore);
            }
        }

        private void OnSpeakRepliesChanged(bool value)
        {
            if (!isBindingSettingsUi)
            {
                settingsStore.SetSpeakReplies(value);
                OnSettingsChanged();
            }
        }

        private void OnTranscriptPreviewChanged(bool value)
        {
            if (!isBindingSettingsUi)
            {
                settingsStore.SetTranscriptPreview(value);
                if (!value)
                {
                    chatStore.SetTranscriptPreview(string.Empty);
                }
                RefreshChatPanel();
                OnSettingsChanged();
            }
        }

        private void OnMiniAssistantChanged(bool value)
        {
            if (!isBindingSettingsUi)
            {
                settingsStore.SetMiniAssistantEnabled(value);
                OnSettingsChanged();
            }
        }

        private void OnReminderSpeechChanged(bool value)
        {
            if (!isBindingSettingsUi)
            {
                settingsStore.SetReminderSpeechEnabled(value);
                OnSettingsChanged();
            }
        }

        private void OnSettingsChanged()
        {
            RefreshSettingsPanel();
            RefreshStagePanel();
            if (settingsStore.HasUnsavedChanges())
            {
                SetSettingsStatus("Unsaved changes.", WarningColor);
            }
            else
            {
                SetSettingsStatus("Settings saved.", SuccessColor);
            }
        }

        private void SetSettingsStatus(string message, Color color)
        {
            if (HasLiveUi())
            {
                settingsScreenController.SetStatus(message, color);
            }
        }

        private void ApplyInteractionState(HealthResponse health)
        {
            if (!HasLiveUi()) return;
            var enableTaskActions = HealthRecoveryAdvisor.CanUseTaskActions(health)
                && !isInitializing
                && !isRefreshingWorkspace
                && !isReloadingTaskViews
                && !isSubmittingChatTurn
                && !isMutatingTask;
            var enableChatText = HealthRecoveryAdvisor.CanUseTaskActions(health)
                && !isInitializing
                && !isRefreshingWorkspace
                && !isSubmittingChatTurn
                && !isMutatingTask;
            var enableMic = HealthRecoveryAdvisor.CanUseMic(health)
                && !isInitializing
                && !isRefreshingWorkspace
                && !isSubmittingChatTurn
                && !isMutatingTask;
            chatPanelController.SetInteractable(enableChatText, enableMic);
            homeScreenController.SetTaskActionsEnabled(enableTaskActions);
            scheduleScreenController.SetTaskActionsEnabled(enableTaskActions);
            appShellController.SetRefreshEnabled(!isInitializing && !isRefreshingWorkspace && !isSubmittingChatTurn && !isMutatingTask && !isSettingsRequestInFlight);
            settingsScreenController.SetEditable(HealthRecoveryAdvisor.CanEditSettings(health) && !isInitializing && !isRefreshingWorkspace && !isSettingsRequestInFlight);
        }

        private void AppendRecoveryGuidance(HealthResponse health)
        {
            if (!HasLiveUi()) return;
            var message = HealthRecoveryAdvisor.BuildMessage(health);
            if (!string.IsNullOrEmpty(message))
            {
                chatStore.AddAssistant(message);
                RefreshChatPanel();
            }
        }

        private void SyncHealthStatusToChat(HealthResponse health)
        {
            if (health == null)
            {
                return;
            }

            if (isInitializing || isRefreshingWorkspace || isReloadingTaskViews)
            {
                return;
            }

            if (health.status == "error")
            {
                chatStore.SetSystemStatus("ERROR", "Backend unavailable", "Only refresh remains available until local services recover.");
                return;
            }

            if (health.status == "partial")
            {
                chatStore.SetSystemStatus("DEGRADED", "Runtime degraded", "Some voice or model features are unavailable, but task and settings flows can still continue.");
                return;
            }

            if (!isSubmittingChatTurn && !isMutatingTask)
            {
                chatStore.ClearSystemStatus();
            }
        }

        private void ResolvePendingQuickAdd(IReadOnlyList<TaskActionReport> actions, string fallbackMessage, Color fallbackColor)
        {
            if (!awaitingQuickAddResult)
            {
                return;
            }

            awaitingQuickAddResult = false;
            if (actions != null && actions.Count > 0)
            {
                var action = actions[0];
                var title = string.IsNullOrWhiteSpace(action.title) ? "task" : action.title;
                var detail = string.IsNullOrWhiteSpace(action.detail) ? "Task list refreshed." : action.detail.Trim();
                homeScreenController.SetQuickAddStatus($"Added '{title}'. {detail}", SuccessColor);
                return;
            }

            homeScreenController.SetQuickAddStatus(fallbackMessage, fallbackColor);
        }

        private void CompleteChatTurn()
        {
            isSubmittingChatTurn = false;
            chatStore.SetThinking(false);
            SyncHealthStatusToChat(currentHealth);
            ApplyInteractionState(currentHealth);
            RefreshChatPanel();
            RefreshStagePanel();
        }

        private void OnAvatarStateChanged(AvatarState _)
        {
            RefreshStagePanel();
        }

        private void OnAudioPlaybackCompleted()
        {
            RefreshStagePanel();
        }

        private void RefreshStagePanel()
        {
            if (!HasLiveUi() || avatarStateMachine == null) return;
            appShellController.RenderStage(avatarStateMachine.CurrentState, currentHealth, currentScreen, selectedDate, settingsStore, chatStore);
            homeScreenController?.RenderStage(avatarStateMachine.CurrentState);
        }

        private void ClearSubtitleAndIdle()
        {
            subtitlePresenter.Hide();
            avatarStateMachine.SetState(AvatarState.Idle);
            avatarConversationBridge?.OnIdle();
        }

        private void HandleBackendUnavailableState()
        {
            SetSettingsStatus("Backend unavailable.", ErrorColor);
            chatStore.SetThinking(false);
            chatStore.SetSystemStatus("ERROR", "Backend unavailable", "Only refresh remains available until local services recover.");
            avatarStateMachine?.SetState(AvatarState.Warning);
            ApplyInteractionState(currentHealth);
            RefreshChatPanel();
        }

        private static AudioClip TrimClip(AudioClip source, int samples)
        {
            var data = new float[samples * source.channels];
            source.GetData(data, 0);
            var clip = AudioClip.Create("RecordedClip", samples, source.channels, source.frequency, false);
            clip.SetData(data, 0);
            return clip;
        }

        private bool HasLiveUi() => ui?.Shell?.HealthBanner != null;
    }
}
