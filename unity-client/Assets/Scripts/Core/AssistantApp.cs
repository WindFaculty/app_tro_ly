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
using LocalAssistant.World.Interaction;
using LocalAssistant.World.Room;
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
        private IPlannerBackendIntegration plannerBackend;
        private IAssistantEventsClient eventsClient;
        private IAssistantStreamClient assistantStreamClient;
        private CancellationTokenSource cancellationTokenSource;
        private readonly TaskViewModelStore taskStore = new();
        private readonly IAssistantEventBus sharedEventBus = new AssistantEventBus();
        private readonly List<IDisposable> eventSubscriptions = new();
        private readonly Queue<TtsSentenceReadyEvent> speechQueue = new();

        private AvatarStateMachine avatarStateMachine;
        private AvatarConversationBridge avatarConversationBridge;
        private LipSyncController lipSyncController;
        private AudioPlaybackController audioPlaybackController;
        private SubtitlePresenter subtitlePresenter;
        private ReminderPresenter reminderPresenter;
        private RoomWorldController roomWorldController;
        private RoomInteractionController roomInteractionController;
        private CharacterRoomBridge characterRoomBridge;
        private IShellModule shellModule;
        private IHomeModule homeModule;
        private IPlannerModule plannerModule;
        private ISettingsModule settingsModule;
        private IChatModule chatModule;
        private readonly ChatTurnApplicationService chatTurnService = new();
        private readonly HomeQuickAddApplicationService homeQuickAddService = new();
        private PlannerTaskCommandApplicationService plannerTaskCommands;

        private HealthResponse currentHealth = new();
        private AppScreen currentPlannerScreen = AppScreen.Week;
        private string selectedDate = string.Empty;
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
        private AvatarState conversationVisualState = AvatarState.Idle;
        private RoomObjectSelectionSnapshot currentRoomSelection = RoomObjectSelectionSnapshot.None;
        private string currentRoomActivityTitle = "Avatar hold";
        private string currentRoomActivityDetail = "Select a highlighted room object to reveal contextual room actions.";

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
            roomWorldController = composition.RoomWorldController;
            roomInteractionController = composition.RoomInteractionController;
            characterRoomBridge = composition.CharacterRoomBridge;
            avatarStateMachine.StateChanged += OnAvatarStateChanged;
            audioPlaybackController.PlaybackCompleted += OnAudioPlaybackCompleted;
            if (roomInteractionController != null)
            {
                roomInteractionController.SelectionChanged += HandleRoomSelectionChanged;
            }
            shellModule = new ShellModule(ui.Shell);
            homeModule = new HomeModule(ui.Home);
            plannerModule = new PlannerModule(ui.Schedule);
            settingsModule = new SettingsModule(ui.Settings);
            chatModule = new ChatModule(ui.Chat);
            shellModule.Bind();
            homeModule.Bind();
            plannerModule.Bind();
            settingsModule.Bind();
            chatModule.Bind();
            RegisterSharedEventFlow();
            shellModule.RefreshRequested += RefreshWorkspace;
            homeModule.QuickAddRequested += SubmitQuickAddMessage;
            homeModule.RoomActionRequested += HandleRoomActionRequested;
            plannerModule.TodayRequested += PublishPlannerTodayRequested;
            plannerModule.DateOffsetRequested += PublishPlannerDateOffsetRequested;
            plannerModule.DaySelected += PublishPlannerDaySelected;
            plannerModule.ScreenRequested += PublishPlannerScreenRequested;
            plannerModule.CompleteTaskRequested += PublishPlannerTaskCompletionRequested;
            plannerModule.ScheduleTaskRequested += PublishPlannerTaskSchedulingRequested;
            settingsModule.ReloadRequested += ReloadSettings;
            settingsModule.SaveRequested += SaveSettings;
            settingsModule.SettingsChanged += HandleSettingsChanged;
            chatModule.SendRequested += SubmitFromMessage;
            chatModule.MicRequested += ToggleVoiceSession;

            PublishConversationVisualState(AvatarState.Thinking);
            chatModule.SetSystemStatus("LOADING", "Loading local workspace", "Connecting to the backend and preparing stage, planner, settings, and chat.");
            shellModule.RenderBootState("Loading runtime", "Connecting to the local backend and preparing the four-zone shell...", LoadingColor);
            homeModule.SetQuickAddStatus("Quick add becomes available after the workspace loads.", InfoColor);
            homeModule.RenderSelectedRoomObject(currentRoomSelection);
            homeModule.RenderRoomOverlayState(BuildRoomOverlayState());
            SetSettingsStatus("Loading settings...", InfoColor);

            plannerModule.ShowScreen(currentPlannerScreen);
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
            if (roomInteractionController != null) roomInteractionController.SelectionChanged -= HandleRoomSelectionChanged;
            foreach (var subscription in eventSubscriptions)
            {
                subscription?.Dispose();
            }

            eventSubscriptions.Clear();
            cancellationTokenSource?.Cancel();
            cancellationTokenSource?.Dispose();
            eventsClient?.Dispose();
            assistantStreamClient?.Dispose();
        }

        public void ConfigureClientsForTests(IAssistantApiClient injectedApiClient, IAssistantEventsClient injectedEventsClient, IAssistantStreamClient injectedStreamClient = null)
        {
            apiClient = injectedApiClient;
            plannerBackend = null;
            plannerTaskCommands = null;
            eventsClient = injectedEventsClient;
            assistantStreamClient = injectedStreamClient;
        }

        public async void BeginPushToTalk() => await BeginListeningAsync();
        public async void EndPushToTalk() => await FinishVoiceCaptureAsync(false);

        private void RegisterSharedEventFlow()
        {
            eventSubscriptions.Add(sharedEventBus.Subscribe<PlannerTodayRequestedEvent>(_ => ResetSelectedDateToToday()));
            eventSubscriptions.Add(sharedEventBus.Subscribe<PlannerDateOffsetRequestedEvent>(evt => ShiftSelectedDate(evt.DayOffset)));
            eventSubscriptions.Add(sharedEventBus.Subscribe<PlannerDaySelectedEvent>(evt => SelectDate(evt.SelectedDate)));
            eventSubscriptions.Add(sharedEventBus.Subscribe<PlannerScreenRequestedEvent>(HandlePlannerScreenRequested));
            eventSubscriptions.Add(sharedEventBus.Subscribe<PlannerTaskCompletionRequestedEvent>(evt => CompleteTaskFromSchedule(evt.TaskId)));
            eventSubscriptions.Add(sharedEventBus.Subscribe<PlannerTaskSchedulingRequestedEvent>(evt => ScheduleTaskFromInbox(evt.TaskId, evt.TargetDate)));
            eventSubscriptions.Add(sharedEventBus.Subscribe<SubtitleVisibilityChangedEvent>(HandleSubtitleVisibilityChanged));
            eventSubscriptions.Add(sharedEventBus.Subscribe<ConversationVisualStateChangedEvent>(HandleConversationVisualStateChanged));
            eventSubscriptions.Add(sharedEventBus.Subscribe<BackendAssistantStateChangedEvent>(HandleBackendAssistantStateChanged));
        }

        private void PublishPlannerTodayRequested() => sharedEventBus.Publish(new PlannerTodayRequestedEvent());
        private void PublishPlannerDateOffsetRequested(int dayOffset) => sharedEventBus.Publish(new PlannerDateOffsetRequestedEvent(dayOffset));
        private void PublishPlannerDaySelected(string selectedDate) => sharedEventBus.Publish(new PlannerDaySelectedEvent(selectedDate));
        private void PublishPlannerScreenRequested(AppScreen screen) => sharedEventBus.Publish(new PlannerScreenRequestedEvent(screen));
        private void PublishPlannerTaskCompletionRequested(string taskId) => sharedEventBus.Publish(new PlannerTaskCompletionRequestedEvent(taskId));
        private void PublishPlannerTaskSchedulingRequested(string taskId, string targetDate) => sharedEventBus.Publish(new PlannerTaskSchedulingRequestedEvent(taskId, targetDate));
        private void PublishSubtitleShown(string text) => sharedEventBus.Publish(new SubtitleVisibilityChangedEvent(text, true));
        private void PublishSubtitleHidden() => sharedEventBus.Publish(new SubtitleVisibilityChangedEvent(string.Empty, false));
        private void PublishConversationVisualState(AvatarState state) => sharedEventBus.Publish(new ConversationVisualStateChangedEvent(state));
        private void PublishBackendAssistantState(string state, string animationHint) => sharedEventBus.Publish(new BackendAssistantStateChangedEvent(state, animationHint));

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
                PublishConversationVisualState(AvatarState.Idle);
                SyncHealthStatusToChat(currentHealth);
                chatModule.AddAssistant("Hybrid assistant is ready. Ask about today, planning, or start voice mode.");
                RefreshChatPanel();
                eventsClient ??= new EventsClient(apiClient.EventsUrl);
                await eventsClient.ConnectAsync(cancellationTokenSource.Token);
                await TryConnectAssistantStreamAsync();
            }
            catch (Exception exception)
            {
                ApplyHealth(new HealthResponse { status = "error" });
                chatModule.AddAssistant("Cannot reach the local backend: " + exception.Message);
                HandleBackendUnavailableState();
                RefreshChatPanel();
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
            taskStore.ApplySnapshot(await GetPlannerBackend().LoadSnapshotAsync(selectedDate));
            RefreshTaskView();
        }

        private async Task TryConnectAssistantStreamAsync()
        {
            try
            {
                assistantStreamClient ??= new AssistantStreamClient(apiClient.AssistantStreamUrl);
                await assistantStreamClient.ConnectAsync(cancellationTokenSource.Token);
                chatModule.SessionId = string.IsNullOrEmpty(chatModule.SessionId) ? Guid.NewGuid().ToString("N") : chatModule.SessionId;
                await SendStreamAsync(new AssistantStreamRequestPayload { type = "session_start", session_id = chatModule.SessionId, selected_date = selectedDate });
            }
            catch (Exception exception)
            {
                assistantStreamClient?.Dispose();
                assistantStreamClient = null;
                chatModule.AddAssistant("Assistant stream unavailable, using compatibility mode: " + exception.Message);
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
            chatModule.SetSystemStatus("LOADING", "Refreshing workspace", "Pulling health, settings, and task data from the local backend.");
            shellModule.RenderBootState("Refreshing workspace", "Synchronizing runtime health, settings, and task views...", LoadingColor);
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
                chatModule.AddAssistant("Refresh failed: " + exception.Message);
                chatModule.SetSystemStatus("ERROR", "Workspace refresh failed", "Check the backend process and try refresh again.");
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

        private async void SubmitQuickAddMessage(string message)
        {
            if (!HomeQuickAddRequest.TryCreate(message, out var request))
            {
                return;
            }

            await SubmitChatAsync(homeQuickAddService.CreateAssistantMessage(request), false, true);
        }

        private async Task SubmitChatAsync(string message, bool fromVoice, bool fromQuickAdd)
        {
            if (isSubmittingChatTurn ||
                !ChatTurnRequest.TryCreate(
                    message,
                    chatModule.ConversationId,
                    chatModule.SessionId,
                    selectedDate,
                    fromVoice,
                    settingsModule.Current.voice.speak_replies,
                    out var request))
            {
                return;
            }

            isSubmittingChatTurn = true;
            awaitingQuickAddResult = fromQuickAdd;
            if (fromQuickAdd)
            {
                ApplyQuickAddStatus(homeQuickAddService.BuildPendingStatus());
            }

            chatModule.BeginTurn(request.Message, fromVoice, settingsModule.Current.voice.show_transcript_preview);

            PublishConversationVisualState(AvatarState.Thinking);
            ApplyInteractionState(currentHealth);
            RefreshChatPanel();
            try
            {
                var executionPlan = chatTurnService.BuildPlan(request, assistantStreamClient != null && assistantStreamClient.IsConnected);
                if (executionPlan.Transport == ChatTurnTransport.Streaming)
                {
                    await SendStreamAsync(executionPlan.StreamRequest);
                    return;
                }

                await ApplyCompatibilityChatResponseAsync(await apiClient.SendChatAsync(executionPlan.CompatibilityRequest));
                CompleteChatTurn();
            }
            catch (Exception exception)
            {
                chatModule.ApplyRequestFailure("Request failed: " + exception.Message, "Check the backend and try the request again.");
                ResolvePendingQuickAdd(homeQuickAddService.BuildFailureStatus());
                RefreshChatPanel();
                CompleteChatTurn();
            }
        }

        private async Task ApplyCompatibilityChatResponseAsync(ChatResponsePayload response)
        {
            chatModule.ApplyCompatibilityResponse(response);
            PublishBackendAssistantState("talking", response.animation_hint);
            ResolvePendingQuickAdd(response.task_actions);
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
                PublishSubtitleShown(response.reply_text);
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
                chatModule.AddAssistant("Microphone is not available.");
                RefreshChatPanel();
                return;
            }

            recordingClip = Microphone.Start(null, false, 30, 16000);
            isRecording = true;
            PublishConversationVisualState(AvatarState.Listening);
            await Task.CompletedTask;
        }

        private async Task FinishVoiceCaptureAsync(bool sendStop)
        {
            if (!isRecording) return;
            var position = Microphone.GetPosition(null);
            Microphone.End(null);
            isRecording = false;
            PublishConversationVisualState(AvatarState.Thinking);
            if (recordingClip == null || position <= 0) return;
            if (assistantStreamClient != null && assistantStreamClient.IsConnected)
            {
                await SendStreamAsync(new AssistantStreamRequestPayload
                {
                    type = "voice_end",
                    session_id = chatModule.SessionId,
                    conversation_id = chatModule.ConversationId,
                    selected_date = selectedDate,
                    voice_mode = true,
                    audio_base64 = Convert.ToBase64String(WavEncoder.Encode(TrimClip(recordingClip, position))),
                });
            }
            else
            {
                var transcript = await apiClient.SendSpeechToTextAsync(WavEncoder.Encode(TrimClip(recordingClip, position)));
                chatModule.SetTranscriptPreview(settingsModule.Current.voice.show_transcript_preview ? transcript.text : string.Empty);
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
                        PublishBackendAssistantState(assistantState.state, assistantState.animation_hint);
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
                        if (partial != null)
                        {
                            chatModule.ApplyTranscriptPartial(partial.text, settingsModule.Current.voice.show_transcript_preview);
                            RefreshChatPanel();
                        }
                        break;
                    case "transcript_final":
                        var transcript = UnityJson.Deserialize<TranscriptEvent>(payload);
                        if (transcript != null && !string.IsNullOrWhiteSpace(transcript.text))
                        {
                            chatModule.ApplyTranscriptFinal(transcript.text, settingsModule.Current.voice.show_transcript_preview);
                            RefreshChatPanel();
                        }
                        break;
                    case "route_selected":
                        var route = UnityJson.Deserialize<RouteSelectedEvent>(payload);
                        if (route != null)
                        {
                            chatModule.ApplyRouteSelection(route.route, route.provider);
                            RefreshChatPanel();
                        }
                        break;
                    case "assistant_chunk":
                        var chunk = UnityJson.Deserialize<AssistantChunkEvent>(payload);
                        if (chunk != null)
                        {
                            chatModule.ApplyAssistantChunk(chunk.text);
                            RefreshChatPanel();
                        }
                        break;
                    case "assistant_final":
                        var finalReply = UnityJson.Deserialize<AssistantFinalEvent>(payload);
                        if (finalReply != null)
                        {
                            chatModule.ApplyStreamingFinal(finalReply);
                            ResolvePendingQuickAdd(finalReply.task_actions);
                            RefreshChatPanel();
                            if (finalReply.task_actions != null && finalReply.task_actions.Count > 0)
                            {
                                _ = SafeReloadTasksAsync();
                            }

                            if (!settingsModule.Current.voice.speak_replies)
                            {
                                PublishSubtitleShown(finalReply.reply_text);
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
                        if (state != null) PublishBackendAssistantState(state.state, state.animation_hint);
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
                PublishConversationVisualState(AvatarState.Talking);
                while (speechQueue.Count > 0)
                {
                    var sentence = speechQueue.Dequeue();
                    var clip = await apiClient.DownloadAudioClipAsync(sentence.audio_url);
                    PublishSubtitleShown(sentence.text);
                    audioPlaybackController.Play(clip);
                    while (audioPlaybackController.IsPlaying) await Task.Delay(50);
                    PublishSubtitleHidden();
                }
            }
            finally
            {
                isPlayingSpeechQueue = false;
                PublishSubtitleHidden();
                PublishConversationVisualState(AvatarState.Idle);
                if (autoResumeListening && continuousVoiceEnabled) await BeginListeningAsync();
            }
        }

        private void HandlePlannerScreenRequested(PlannerScreenRequestedEvent evt)
        {
            if (evt == null)
            {
                return;
            }

            SetPlannerScreen(evt.Screen, true);
        }

        private void HandleSubtitleVisibilityChanged(SubtitleVisibilityChangedEvent evt)
        {
            if (evt == null || subtitlePresenter == null)
            {
                return;
            }

            if (evt.Visible)
            {
                subtitlePresenter.Show(evt.Text);
                return;
            }

            subtitlePresenter.Hide();
        }

        private void HandleConversationVisualStateChanged(ConversationVisualStateChangedEvent evt)
        {
            if (evt == null)
            {
                return;
            }

            var nextState = evt.State;

            if (conversationVisualState == AvatarState.Listening && nextState != AvatarState.Listening)
            {
                avatarConversationBridge?.OnListeningEnd();
            }

            if (conversationVisualState == AvatarState.Talking && nextState != AvatarState.Talking)
            {
                avatarConversationBridge?.OnSpeakingEnd();
            }

            if (nextState == AvatarState.Listening && conversationVisualState != AvatarState.Listening)
            {
                avatarConversationBridge?.OnListeningStart();
            }

            if (nextState == AvatarState.Talking && conversationVisualState != AvatarState.Talking)
            {
                avatarConversationBridge?.OnSpeakingStart();
            }

            if (nextState == AvatarState.Idle)
            {
                avatarConversationBridge?.OnIdle();
            }

            conversationVisualState = nextState;
            chatModule.SetListening(nextState == AvatarState.Listening);
            chatModule.SetThinking(nextState == AvatarState.Thinking);
            chatModule.SetTalking(nextState == AvatarState.Talking);
            avatarStateMachine?.SetState(nextState);
            RefreshChatPanel();
        }

        private void HandleBackendAssistantStateChanged(BackendAssistantStateChangedEvent evt)
        {
            if (evt == null)
            {
                return;
            }

            avatarStateMachine?.ApplyBackendState(evt.State, evt.AnimationHint);
        }

        private void RefreshTaskView()
        {
            if (!HasLiveUi()) return;
            homeModule.Render(taskStore);
            plannerModule.Render(taskStore, selectedDate);
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
            sharedEventBus.Publish(new PlannerDateChangedEvent(selectedDate));
            isReloadingTaskViews = true;
            chatModule.SetSystemStatus("LOADING", "Loading selected day", $"Updating Home and Schedule for {selectedDate}.");
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
                chatModule.AddAssistant("Could not load the selected day: " + exception.Message);
                chatModule.SetSystemStatus("ERROR", "Selected day failed to load", "Refresh the workspace or pick another date.");
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
            chatModule.SetSystemStatus("UPDATING", "Completing task", "Applying the change and refreshing task lists.");
            ApplyInteractionState(currentHealth);
            RefreshChatPanel();
            try
            {
                var updatedTask = await GetPlannerTaskCommands().CompleteTaskAsync(taskId);
                chatModule.ApplyPlannerActionResult(updatedTask.ActionType, updatedTask.TaskId, updatedTask.Title, updatedTask.Detail);
                await ReloadAllAsync();
                SyncHealthStatusToChat(currentHealth);
                RefreshChatPanel();
            }
            catch (Exception exception)
            {
                chatModule.AddAssistant("Could not complete the task: " + exception.Message);
                chatModule.SetSystemStatus("ERROR", "Task update failed", "Check the backend and try again.");
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
            chatModule.SetSystemStatus("UPDATING", "Scheduling inbox task", $"Moving the item onto {targetDate}.");
            ApplyInteractionState(currentHealth);
            RefreshChatPanel();
            try
            {
                var updatedTask = await GetPlannerTaskCommands().ScheduleInboxTaskAsync(taskId, targetDate);
                chatModule.ApplyPlannerActionResult(updatedTask.ActionType, updatedTask.TaskId, updatedTask.Title, updatedTask.Detail);
                await ReloadAllAsync();
                SyncHealthStatusToChat(currentHealth);
                RefreshChatPanel();
            }
            catch (Exception exception)
            {
                chatModule.AddAssistant("Could not schedule the inbox task: " + exception.Message);
                chatModule.SetSystemStatus("ERROR", "Scheduling failed", "Check the backend and try again.");
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
                chatModule.AddAssistant("Task refresh failed: " + exception.Message);
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
                var snapshot = chatModule.Render(settingsModule.Current.voice.show_transcript_preview);
                homeModule?.RenderAssistantOrbit(snapshot);
            }
        }

        private void ApplyHealth(HealthResponse health)
        {
            if (!HasLiveUi()) return;
            currentHealth = HealthResponseNormalizer.Normalize(health);
            shellModule.RenderHealth(currentHealth);
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
            if (isSettingsRequestInFlight || !settingsModule.HasUnsavedChanges)
            {
                if (!settingsModule.HasUnsavedChanges)
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
                settingsModule.Apply(await apiClient.UpdateSettingsAsync(settingsModule.Snapshot()));
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
            settingsModule.Apply(await apiClient.GetSettingsAsync());
            SetSettingsStatus(statusMessage, InfoColor);
        }

        private void HandleSettingsChanged()
        {
            if (!settingsModule.Current.voice.show_transcript_preview)
            {
                chatModule.SetTranscriptPreview(string.Empty);
            }

            RefreshChatPanel();
            RefreshStagePanel();
        }

        private void SetSettingsStatus(string message, Color color)
        {
            settingsModule?.SetStatus(message, color);
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
            chatModule.SetInteractable(enableChatText, enableMic);
            homeModule.SetTaskActionsEnabled(enableTaskActions);
            plannerModule.SetTaskActionsEnabled(enableTaskActions);
            shellModule.SetRefreshEnabled(!isInitializing && !isRefreshingWorkspace && !isSubmittingChatTurn && !isMutatingTask && !isSettingsRequestInFlight);
            settingsModule.SetEditable(HealthRecoveryAdvisor.CanEditSettings(health) && !isInitializing && !isRefreshingWorkspace && !isSettingsRequestInFlight);
        }

        private void AppendRecoveryGuidance(HealthResponse health)
        {
            if (!HasLiveUi()) return;
            var message = HealthRecoveryAdvisor.BuildMessage(health);
            if (!string.IsNullOrEmpty(message))
            {
                chatModule.AddAssistant(message);
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
                chatModule.SetSystemStatus("ERROR", "Backend unavailable", "Only refresh remains available until local services recover.");
                return;
            }

            if (health.status == "partial")
            {
                chatModule.SetSystemStatus("DEGRADED", "Runtime degraded", "Some voice or model features are unavailable, but task and settings flows can still continue.");
                return;
            }

            if (!isSubmittingChatTurn && !isMutatingTask)
            {
                chatModule.ClearSystemStatus();
            }
        }

        private void ResolvePendingQuickAdd(IReadOnlyList<TaskActionReport> actions)
        {
            ResolvePendingQuickAdd(homeQuickAddService.ResolveCompletion(actions));
        }

        private void ResolvePendingQuickAdd(HomeQuickAddStatus status)
        {
            if (!awaitingQuickAddResult)
            {
                return;
            }

            awaitingQuickAddResult = false;
            ApplyQuickAddStatus(status);
        }

        private IPlannerBackendIntegration GetPlannerBackend()
        {
            plannerBackend ??= new PlannerBackendIntegration(apiClient);
            return plannerBackend;
        }

        private PlannerTaskCommandApplicationService GetPlannerTaskCommands()
        {
            plannerTaskCommands ??= new PlannerTaskCommandApplicationService(GetPlannerBackend());
            return plannerTaskCommands;
        }

        private void CompleteChatTurn()
        {
            isSubmittingChatTurn = false;
            chatModule.SetThinking(false);
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
            shellModule.RenderStage(
                ShellStageSnapshotBuilder.Build(
                    avatarStateMachine.CurrentState,
                    currentHealth,
                    currentPlannerScreen,
                    selectedDate,
                    settingsModule,
                    chatModule));
            homeModule?.RenderStage(avatarStateMachine.CurrentState);
            homeModule?.RenderSelectedRoomObject(currentRoomSelection);
            homeModule?.RenderRoomOverlayState(BuildRoomOverlayState());
        }

        private void HandleRoomSelectionChanged(RoomObjectSelectionSnapshot snapshot)
        {
            currentRoomSelection = snapshot ?? RoomObjectSelectionSnapshot.None;
            if (currentRoomSelection.HasSelection)
            {
                currentRoomActivityTitle = $"Focused on {currentRoomSelection.DisplayName}";
                currentRoomActivityDetail = currentRoomSelection.SuggestedActionText;
            }
            else
            {
                currentRoomActivityTitle = "Avatar hold";
                currentRoomActivityDetail = "Select a highlighted room object to reveal contextual room actions.";
            }

            if (!HasLiveUi())
            {
                return;
            }

            homeModule?.RenderSelectedRoomObject(currentRoomSelection);
            homeModule?.RenderRoomOverlayState(BuildRoomOverlayState());
        }

        private void HandleRoomActionRequested(HomeRoomAction action)
        {
            switch (action)
            {
                case HomeRoomAction.GoTo:
                    if (!currentRoomSelection.HasSelection)
                    {
                        return;
                    }

                    if (roomInteractionController != null && roomInteractionController.HasFocusTarget)
                    {
                        characterRoomBridge?.SetAttentionTarget(roomInteractionController.CurrentFocusPoint);
                    }

                    SetRoomActivity(
                        $"Route prepared for {currentRoomSelection.DisplayName}",
                        "Movement is still placeholder-safe in this phase, so the avatar keeps position and locks attention on the selected object.");
                    break;

                case HomeRoomAction.Inspect:
                    if (!currentRoomSelection.HasSelection)
                    {
                        return;
                    }

                    SetRoomActivity(
                        $"Inspecting {currentRoomSelection.DisplayName}",
                        currentRoomSelection.StateText);
                    break;

                case HomeRoomAction.Use:
                    if (!currentRoomSelection.HasSelection)
                    {
                        return;
                    }

                    SetRoomActivity(
                        $"Use intent queued for {currentRoomSelection.DisplayName}",
                        "Phase 6 keeps use flows placeholder-safe: the dock now records intent while later slices can add movement, animation, or real object state changes.");
                    break;

                case HomeRoomAction.ReturnToAvatar:
                    roomInteractionController?.ClearSelection();
                    characterRoomBridge?.ClearAttentionTarget();
                    SetRoomActivity(
                        "Avatar hold",
                        "Selection cleared. The stage camera and avatar attention are back on the room's idle presentation.");
                    break;

                case HomeRoomAction.ToggleHotspots:
                    if (roomWorldController == null)
                    {
                        return;
                    }

                    var hotspotsVisible = roomWorldController.ToggleHotspotsVisible();
                    SetRoomActivity(
                        hotspotsVisible ? "Room hotspots visible" : "Room hotspots hidden",
                        hotspotsVisible
                            ? "Anchor markers are visible again to show the room's main interaction clusters."
                            : "Anchor markers are hidden so the room presentation stays cleaner while the action dock remains available.");
                    break;
            }
        }

        private void ClearSubtitleAndIdle()
        {
            PublishSubtitleHidden();
            PublishConversationVisualState(AvatarState.Idle);
        }

        private void HandleBackendUnavailableState()
        {
            SetSettingsStatus("Backend unavailable.", ErrorColor);
            chatModule.SetSystemStatus("ERROR", "Backend unavailable", "Only refresh remains available until local services recover.");
            PublishConversationVisualState(AvatarState.Warning);
            ApplyInteractionState(currentHealth);
            RefreshChatPanel();
        }

        private void SetPlannerScreen(AppScreen screen, bool expandCalendar)
        {
            currentPlannerScreen = screen;
            plannerModule.ShowScreen(screen);
            if (expandCalendar)
            {
                shellModule.SetCalendarExpanded(true);
            }

            RefreshTaskView();
            RefreshStagePanel();
        }

        private void ApplyQuickAddStatus(HomeQuickAddStatus status)
        {
            if (status == null)
            {
                return;
            }

            homeModule.SetQuickAddStatus(status.Message, MapQuickAddColor(status.Kind));
        }

        private void SetRoomActivity(string title, string detail)
        {
            currentRoomActivityTitle = title ?? string.Empty;
            currentRoomActivityDetail = detail ?? string.Empty;
            homeModule?.RenderRoomOverlayState(BuildRoomOverlayState());
        }

        private HomeRoomOverlayState BuildRoomOverlayState()
        {
            var hotspotsVisible = roomWorldController == null || roomWorldController.HotspotsVisible;
            return new HomeRoomOverlayState
            {
                ActivityTitle = string.IsNullOrWhiteSpace(currentRoomActivityTitle) ? "Avatar hold" : currentRoomActivityTitle,
                ActivityDetail = string.IsNullOrWhiteSpace(currentRoomActivityDetail)
                    ? "Select a highlighted room object to reveal contextual room actions."
                    : currentRoomActivityDetail,
                ModeLabel = currentRoomSelection.HasSelection ? "OBJECT FOCUS" : "ROOM READY",
                HotspotButtonText = hotspotsVisible ? "Hide hotspots" : "Show hotspots",
                GoToEnabled = currentRoomSelection.HasSelection && currentRoomSelection.SupportsGoTo,
                InspectEnabled = currentRoomSelection.HasSelection && currentRoomSelection.SupportsInspect,
                UseEnabled = currentRoomSelection.HasSelection && currentRoomSelection.SupportsUse,
                ReturnEnabled = true,
                ToggleHotspotsEnabled = roomWorldController != null,
            };
        }

        private Color MapQuickAddColor(QuickAddStatusKind kind)
        {
            return kind switch
            {
                QuickAddStatusKind.Success => SuccessColor,
                QuickAddStatusKind.Warning => WarningColor,
                QuickAddStatusKind.Error => ErrorColor,
                _ => LoadingColor,
            };
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
