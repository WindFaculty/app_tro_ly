using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LocalAssistant.App;
using LocalAssistant.Audio;
using LocalAssistant.Avatar;
using LocalAssistant.Chat;
using LocalAssistant.Network;
using LocalAssistant.Notifications;
using LocalAssistant.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using AvatarSystem;

namespace LocalAssistant.Core
{
    // Coordinates runtime startup, service lifetimes, state stores, UI wiring,
    // backend/event integration, voice flow, and avatar/chat/task synchronization.
    public sealed class AssistantApp : MonoBehaviour
    {
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

        private HealthResponse currentHealth = new();
        private string currentTab = "Week";
        private string selectedDate = string.Empty;
        private bool isBindingSettingsUi;
        private bool continuousVoiceEnabled;
        private bool isRecording;
        private bool isPlayingSpeechQueue;
        private bool autoResumeListening;
        private AudioClip recordingClip;

        // UI init
        private void Awake()
        {
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
            WireUi();
            RefreshTaskView();
            RefreshChatLog();
            RefreshStagePanel();
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

        private void WireUi()
        {
            ui.TodayTab.clicked += () => SetTab("Today");
            ui.WeekTab.clicked += () => SetTab("Week");
            ui.InboxTab.clicked += () => SetTab("Inbox");
            ui.CompletedTab.clicked += () => SetTab("Completed");
            ui.SettingsTab.clicked += () => SetTab("Settings");
            ui.RefreshButton.clicked += RefreshWorkspace;
            ui.SendButton.clicked += () => SubmitFromInput(ui.ChatInput);
            ui.QuickAddButton.clicked += SubmitQuickAdd;
            ui.ReloadSettingsButton.clicked += ReloadSettings;
            ui.SaveSettingsButton.clicked += SaveSettings;
            ui.SpeakRepliesToggle.RegisterValueChangedCallback(evt => OnSpeakRepliesChanged(evt.newValue));
            ui.TranscriptPreviewToggle.RegisterValueChangedCallback(evt => OnTranscriptPreviewChanged(evt.newValue));
            ui.MiniAssistantToggle.RegisterValueChangedCallback(evt => OnMiniAssistantChanged(evt.newValue));
            ui.ReminderSpeechToggle.RegisterValueChangedCallback(evt => OnReminderSpeechChanged(evt.newValue));
            ui.MicButton.clicked += ToggleVoiceSession;
        }

        // Chat logic
        private async Task InitializeAsync()
        {
            try
            {
                apiClient ??= new LocalApiClient();
                ApplyHealth(await apiClient.GetHealthAsync());
                AppendRecoveryGuidance(currentHealth);
                await LoadSettingsAsync("Settings loaded from local backend.");
                selectedDate = DateTime.Now.ToString("yyyy-MM-dd");
                await ReloadAllAsync();
                if (currentHealth.status == "error")
                {
                    HandleBackendUnavailableState();
                    return;
                }

                chatStore.AddAssistant("Hybrid assistant is ready. Ask about today, planning, or start voice mode.");
                RefreshChatLog();
                eventsClient ??= new EventsClient(apiClient.EventsUrl);
                await eventsClient.ConnectAsync(cancellationTokenSource.Token);
                await TryConnectAssistantStreamAsync();
            }
            catch (Exception exception)
            {
                ApplyHealth(new HealthResponse { status = "error" });
                SetSettingsStatus("Backend unavailable.", new Color(0.67f, 0.24f, 0.20f, 1f));
                chatStore.AddAssistant("Cannot reach the local backend: " + exception.Message);
                RefreshChatLog();
                avatarStateMachine.SetState(AvatarState.Warning);
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
                RefreshChatLog();
            }
        }

        private async Task SendStreamAsync(AssistantStreamRequestPayload payload)
        {
            if (assistantStreamClient == null || !assistantStreamClient.IsConnected) return;
            await assistantStreamClient.SendAsync(UnityJson.Serialize(payload), cancellationTokenSource.Token);
        }

        private async void RefreshWorkspace() { try { ApplyHealth(await apiClient.GetHealthAsync()); await LoadSettingsAsync("Workspace refreshed."); await ReloadAllAsync(); } catch (Exception exception) { chatStore.AddAssistant("Refresh failed: " + exception.Message); RefreshChatLog(); } }

        private async void SubmitFromInput(TextField input) { var text = input.value; input.value = string.Empty; await SubmitChatAsync(text, false); }

        private async void SubmitQuickAdd()
        {
            if (!string.IsNullOrWhiteSpace(ui.QuickAddInput.value))
            {
                var quickText = "Add task " + ui.QuickAddInput.value.Trim();
                ui.QuickAddInput.value = string.Empty;
                await SubmitChatAsync(quickText, false);
            }
        }

        private async Task SubmitChatAsync(string message, bool fromVoice)
        {
            if (string.IsNullOrWhiteSpace(message)) return;
            chatStore.AddUser(message);
            chatStore.SetThinking(true);
            chatStore.ResetAssistantDraft();
            if (fromVoice && settingsStore.Current.voice.show_transcript_preview) chatStore.SetTranscriptPreview(message);
            avatarStateMachine.SetState(AvatarState.Thinking);
            RefreshChatLog();
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
            ApplyCompatibilityChatResponse(await apiClient.SendChatAsync(ChatViewModelStore.CreateRequest(message, chatStore.ConversationId, selectedDate, settingsStore.Current.voice.speak_replies)));
        }

        private void ApplyCompatibilityChatResponse(ChatResponsePayload response)
        {
            chatStore.ConversationId = response.conversation_id;
            chatStore.AddAssistant(response.reply_text);
            chatStore.SetThinking(false);
            chatStore.SetDiagnostics(response.route, response.provider, response.latency_ms, response.fallback_used);
            avatarStateMachine.ApplyBackendState("talking", response.animation_hint);
            RefreshChatLog();
            if (response.task_actions != null && response.task_actions.Count > 0) _ = ReloadAllAsync();
            if (response.speak && !string.IsNullOrEmpty(response.audio_url)) _ = PlaySentenceAsync(response.audio_url, response.reply_text);
            else { subtitlePresenter.Show(response.reply_text); Invoke(nameof(ClearSubtitleAndIdle), 2.2f); }
        }

        private async void ToggleVoiceSession() { continuousVoiceEnabled = !continuousVoiceEnabled; if (continuousVoiceEnabled) await BeginListeningAsync(); else await FinishVoiceCaptureAsync(true); }

        private async Task BeginListeningAsync()
        {
            if (isRecording || Microphone.devices.Length == 0) { if (Microphone.devices.Length == 0) { chatStore.AddAssistant("Microphone is not available."); RefreshChatLog(); } return; }
            recordingClip = Microphone.Start(null, false, 30, 16000);
            isRecording = true;
            chatStore.SetListening(true);
            chatStore.SetThinking(false);
            avatarStateMachine.SetState(AvatarState.Listening);
            avatarConversationBridge?.OnListeningStart();
            await Task.CompletedTask;
            RefreshChatLog();
        }

        private async Task FinishVoiceCaptureAsync(bool sendStop)
        {
            if (!isRecording) return;
            var position = Microphone.GetPosition(null);
            Microphone.End(null);
            isRecording = false;
            chatStore.SetListening(false);
            chatStore.SetThinking(true);
            avatarStateMachine.SetState(AvatarState.Thinking);
            avatarConversationBridge?.OnListeningEnd();
            RefreshChatLog();
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
                RefreshChatLog();
                await SubmitChatAsync(transcript.text, true);
            }
            if (sendStop) autoResumeListening = false;
        }

        private void DrainEvents() { if (eventsClient == null) return; while (eventsClient.TryDequeue(out var payload)) { var envelope = UnityJson.Deserialize<EventEnvelope>(payload); if (envelope.type == "reminder_due") { var reminder = UnityJson.Deserialize<ReminderDueEvent>(payload); if (reminder != null) reminderPresenter.Push(reminder); } else if (envelope.type == "task_updated") _ = ReloadAllAsync(); else if (envelope.type == "assistant_state_changed") { var assistantState = UnityJson.Deserialize<AssistantStateChangedEvent>(payload); if (assistantState != null) avatarStateMachine.ApplyBackendState(assistantState.state, assistantState.animation_hint); } } }

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
                        if (partial != null && settingsStore.Current.voice.show_transcript_preview) { chatStore.SetTranscriptPreview(partial.text); RefreshChatLog(); }
                        break;
                    case "transcript_final":
                        var transcript = UnityJson.Deserialize<TranscriptEvent>(payload);
                        if (transcript != null && !string.IsNullOrWhiteSpace(transcript.text))
                        {
                            chatStore.SetTranscriptPreview(settingsStore.Current.voice.show_transcript_preview ? transcript.text : string.Empty);
                            chatStore.AddUser(transcript.text);
                            chatStore.SetThinking(true);
                            chatStore.ResetAssistantDraft();
                            RefreshChatLog();
                        }
                        break;
                    case "route_selected":
                        var route = UnityJson.Deserialize<RouteSelectedEvent>(payload);
                        if (route != null) { chatStore.SetDiagnostics(route.route, route.provider, 0, false); RefreshChatLog(); }
                        break;
                    case "assistant_chunk":
                        var chunk = UnityJson.Deserialize<AssistantChunkEvent>(payload);
                        if (chunk != null) { chatStore.AppendAssistantDraft(chunk.text + " "); RefreshChatLog(); }
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
                            RefreshChatLog();
                            if (finalReply.task_actions != null && finalReply.task_actions.Count > 0) _ = ReloadAllAsync();
                            if (!settingsStore.Current.voice.speak_replies) { subtitlePresenter.Show(finalReply.reply_text); Invoke(nameof(ClearSubtitleAndIdle), 2.2f); }
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
                if (autoResumeListening && continuousVoiceEnabled) await BeginListeningAsync();
            }
        }

        // Routing
        private void SetTab(string tabName) { currentTab = tabName; RefreshTaskView(); RefreshStagePanel(); }

        // Task logic
        private void RefreshTaskView()
        {
            if (!HasLiveUi()) return;
            var isHome = currentTab == "Today";
            var isSchedule = currentTab == "Week" || currentTab == "Inbox" || currentTab == "Completed";
            var isSettings = currentTab == "Settings";
            
            ui.TaskSummaryText.text = taskStore.BuildOverviewText().Replace("  |  ", "\n");
            ui.TaskContentText.text = taskStore.BuildTabText(currentTab);
            
            ui.TaskContentText.style.display = isHome ? DisplayStyle.Flex : DisplayStyle.None;
            ui.QuickAddInput.style.display = isHome ? DisplayStyle.Flex : DisplayStyle.None;
            ui.QuickAddButton.style.display = isHome ? DisplayStyle.Flex : DisplayStyle.None;
            
            if (ui.HomeViewContainer != null) ui.HomeViewContainer.style.display = isHome ? DisplayStyle.Flex : DisplayStyle.None;
            if (ui.ScheduleViewContainer != null) ui.ScheduleViewContainer.style.display = isSchedule ? DisplayStyle.Flex : DisplayStyle.None;
            if (ui.SettingsPanel != null) ui.SettingsPanel.style.display = isSettings ? DisplayStyle.Flex : DisplayStyle.None;
            
            if (ui.ChatPanelView != null) ui.ChatPanelView.style.display = isSchedule ? DisplayStyle.None : DisplayStyle.Flex;
            if (ui.ScheduleSideView != null) ui.ScheduleSideView.style.display = isSchedule ? DisplayStyle.Flex : DisplayStyle.None;

            UpdateTabButtonStyles();
            RefreshStagePanel();
        }

        private void RefreshChatLog() { if (HasLiveUi()) ui.ChatLogText.text = chatStore.BuildTranscript(); }
        
        private void ApplyHealth(HealthResponse health) 
        { 
            if (!HasLiveUi()) return; 
            currentHealth = HealthResponseNormalizer.Normalize(health); 
            ui.HealthBanner.text = $"{HealthStatusMapper.ToLabel(currentHealth.status)}\nDB {BoolLabel(currentHealth.database.available)} | STT {BoolLabel(currentHealth.runtimes.stt.available)}"; 
            ui.HealthBanner.style.color = new StyleColor(HealthStatusMapper.ToColor(currentHealth.status)); 
            ApplyInteractionState(currentHealth); 
            RefreshStagePanel(); 
        }

        private static string BoolLabel(bool value) => value ? "On" : "Off";
        private static string BuildLlmStatus(RuntimeHealth runtime) { var label = "LLM"; if (runtime != null && !string.IsNullOrWhiteSpace(runtime.provider)) label += " " + runtime.provider; return $"{label} {BoolLabel(runtime != null && runtime.available)}"; }
        
        // Settings
        private async void ReloadSettings() { try { await LoadSettingsAsync("Settings reloaded."); } catch (Exception exception) { SetSettingsStatus("Reload failed: " + exception.Message, new Color(0.67f, 0.24f, 0.20f, 1f)); } }
        private async void SaveSettings() { try { settingsStore.Apply(await apiClient.UpdateSettingsAsync(settingsStore.Snapshot())); ApplySettingsToUi(); SetSettingsStatus("Settings saved.", new Color(0.16f, 0.55f, 0.33f, 1f)); } catch (Exception exception) { SetSettingsStatus("Save failed: " + exception.Message, new Color(0.67f, 0.24f, 0.20f, 1f)); } }
        private async Task LoadSettingsAsync(string statusMessage) { settingsStore.Apply(await apiClient.GetSettingsAsync()); ApplySettingsToUi(); SetSettingsStatus(statusMessage, new Color(0.31f, 0.36f, 0.43f, 1f)); }
        
        private void ApplySettingsToUi() 
        { 
            if (!HasLiveUi()) return; 
            isBindingSettingsUi = true; 
            try 
            { 
                ui.SpeakRepliesToggle.SetValueWithoutNotify(settingsStore.Current.voice.speak_replies); 
                ui.TranscriptPreviewToggle.SetValueWithoutNotify(settingsStore.Current.voice.show_transcript_preview); 
                ui.MiniAssistantToggle.SetValueWithoutNotify(settingsStore.Current.window_mode.mini_assistant_enabled); 
                ui.ReminderSpeechToggle.SetValueWithoutNotify(settingsStore.Current.reminder.speech_enabled); 
            } 
            finally 
            { 
                isBindingSettingsUi = false; 
            } 
            RefreshSettingsPanel(); 
            RefreshStagePanel(); 
        }

        private void RefreshSettingsPanel() { if (HasLiveUi()) ui.SettingsSummaryText.text = settingsStore.BuildSummary(); }
        
        private void OnSpeakRepliesChanged(bool value) { if (!isBindingSettingsUi) { settingsStore.SetSpeakReplies(value); OnSettingsChanged(); } }
        private void OnTranscriptPreviewChanged(bool value) { if (!isBindingSettingsUi) { settingsStore.SetTranscriptPreview(value); if (!value) chatStore.SetTranscriptPreview(string.Empty); RefreshChatLog(); OnSettingsChanged(); } }
        private void OnMiniAssistantChanged(bool value) { if (!isBindingSettingsUi) { settingsStore.SetMiniAssistantEnabled(value); OnSettingsChanged(); } }
        private void OnReminderSpeechChanged(bool value) { if (!isBindingSettingsUi) { settingsStore.SetReminderSpeechEnabled(value); OnSettingsChanged(); } }
        private void OnSettingsChanged() { RefreshSettingsPanel(); RefreshStagePanel(); SetSettingsStatus("Unsaved changes.", new Color(0.74f, 0.49f, 0.14f, 1f)); }
        
        private void SetSettingsStatus(string message, Color color) { if (HasLiveUi()) { ui.SettingsStatusText.text = message; ui.SettingsStatusText.style.color = new StyleColor(color); } }
        
        private void ApplyInteractionState(HealthResponse health) 
        { 
            if (!HasLiveUi()) return; 
            var enableTaskActions = HealthRecoveryAdvisor.CanUseTaskActions(health); 
            SetInteractable(ui.ChatInput, enableTaskActions);
            SetInteractable(ui.SendButton, enableTaskActions);
            SetInteractable(ui.QuickAddInput, enableTaskActions);
            SetInteractable(ui.QuickAddButton, enableTaskActions);
            SetInteractable(ui.RefreshButton, true);
            SetInteractable(ui.MicButton, HealthRecoveryAdvisor.CanUseMic(health));
            SetInteractable(ui.SaveSettingsButton, HealthRecoveryAdvisor.CanEditSettings(health));
            SetInteractable(ui.ReloadSettingsButton, HealthRecoveryAdvisor.CanEditSettings(health));
        }

        private void SetInteractable(VisualElement element, bool interactable)
        {
            if (element != null) element.SetEnabled(interactable);
        }

        private void AppendRecoveryGuidance(HealthResponse health) { if (!HasLiveUi()) return; var message = HealthRecoveryAdvisor.BuildMessage(health); if (!string.IsNullOrEmpty(message)) { chatStore.AddAssistant(message); RefreshChatLog(); } }
        private void OnAvatarStateChanged(AvatarState _) => RefreshStagePanel();
        private void OnAudioPlaybackCompleted() => RefreshStagePanel();
        
        private void RefreshStagePanel() { if (!HasLiveUi() || avatarStateMachine == null) return; ui.AvatarStateText.text = avatarStateMachine.CurrentState.ToString(); ui.StageStatusText.text = BuildStageStatusText(); ui.StagePlaceholderText.text = BuildStagePlaceholderText(); }
        
        private string BuildStageStatusText() { var builder = new StringBuilder(); builder.AppendLine($"Health: {HealthStatusMapper.ToLabel(currentHealth.status)}"); builder.AppendLine($"Focus: {currentTab}  |  Date: {(string.IsNullOrEmpty(selectedDate) ? "Auto" : selectedDate)}"); builder.AppendLine($"{BuildLlmStatus(currentHealth.runtimes.llm)}  |  STT {BoolLabel(currentHealth.runtimes.stt.available)}  |  TTS {BoolLabel(currentHealth.runtimes.tts.available)}"); builder.AppendLine($"Voice replies {BoolLabel(settingsStore.Current.voice.speak_replies)}  |  Transcript {BoolLabel(settingsStore.Current.voice.show_transcript_preview)}"); builder.Append($"Route {chatStore.CurrentRoute}  |  Provider {chatStore.CurrentProvider}  |  Fallbacks {chatStore.FallbackCount}"); return builder.ToString().Trim(); }
        
        private string BuildStagePlaceholderText() { var builder = new StringBuilder(); builder.AppendLine("KHUNG AVATAR"); builder.AppendLine(); builder.AppendLine("Assistant dang chay theo kieu hybrid stream."); builder.AppendLine("Chat hien route, provider, latency va transcript."); builder.AppendLine(); builder.Append(taskStore.BuildOverviewText()); return builder.ToString().Trim(); }
        
        private void UpdateTabButtonStyles() 
        { 
            if (!HasLiveUi()) return; 
            SetTabButtonVisual(ui.TodayTab, currentTab == "Today"); 
            SetTabButtonVisual(ui.WeekTab, currentTab == "Week" || currentTab == "Inbox" || currentTab == "Completed"); 
            SetTabButtonVisual(ui.InboxTab, currentTab == "Inbox"); 
            SetTabButtonVisual(ui.CompletedTab, currentTab == "Completed"); 
            SetTabButtonVisual(ui.SettingsTab, currentTab == "Settings"); 
        }

        private static void SetTabButtonVisual(Button button, bool isActive) 
        { 
            if (button == null) return; 
            if (isActive) button.AddToClassList("active");
            else button.RemoveFromClassList("active");
        }

        // Voice
        private void ClearSubtitleAndIdle() { subtitlePresenter.Hide(); avatarStateMachine.SetState(AvatarState.Idle); avatarConversationBridge?.OnIdle(); }
        private void HandleBackendUnavailableState() { SetSettingsStatus("Backend unavailable.", new Color(0.67f, 0.24f, 0.20f, 1f)); avatarStateMachine?.SetState(AvatarState.Warning); }
        private static AudioClip TrimClip(AudioClip source, int samples) { var data = new float[samples * source.channels]; source.GetData(data, 0); var clip = AudioClip.Create("RecordedClip", samples, source.channels, source.frequency, false); clip.SetData(data, 0); return clip; }
        private bool HasLiveUi() => ui != null && ui.HealthBanner != null;
    }
}
