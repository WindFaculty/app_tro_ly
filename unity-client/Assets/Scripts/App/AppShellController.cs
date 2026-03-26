using System;
using System.Text;
using LocalAssistant.Avatar;
using LocalAssistant.Chat;
using LocalAssistant.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace LocalAssistant.App
{
    public sealed class AppShellController
    {
        private readonly AppShellRefs shell;

        public AppShellController(AppShellRefs shell)
        {
            this.shell = shell;
        }

        public event Action RefreshRequested;

        public void Bind()
        {
            if (shell.RefreshButton != null)
            {
                shell.RefreshButton.clicked += RequestRefresh;
            }
        }

        public void RenderHealth(HealthResponse health)
        {
            if (shell.HealthBanner == null)
            {
                return;
            }

            shell.HealthBanner.text = $"{HealthStatusMapper.ToLabel(health.status)}\nDB {ToOnOff(health.database.available)} | STT {ToOnOff(health.runtimes.stt.available)}";
            shell.HealthBanner.style.color = new StyleColor(HealthStatusMapper.ToColor(health.status));
        }

        public void RenderStage(AvatarState avatarState, HealthResponse health, AppScreen currentScreen, string selectedDate, SettingsViewModelStore settingsStore, ChatViewModelStore chatStore)
        {
            if (shell.AvatarStateText != null)
            {
                shell.AvatarStateText.text = avatarState.ToString();
            }

            if (shell.StageStatusText != null)
            {
                shell.StageStatusText.text = BuildStageStatusText(health, currentScreen, selectedDate, settingsStore, chatStore);
            }
        }

        public void SetRefreshEnabled(bool isEnabled)
        {
            shell.RefreshButton?.SetEnabled(isEnabled);
        }

        public void RequestRefresh()
        {
            RefreshRequested?.Invoke();
        }

        private static string BuildStageStatusText(HealthResponse health, AppScreen currentScreen, string selectedDate, SettingsViewModelStore settingsStore, ChatViewModelStore chatStore)
        {
            var builder = new StringBuilder();
            builder.AppendLine($"Health: {HealthStatusMapper.ToLabel(health.status)}");
            builder.AppendLine($"Focus: {ToTaskTabName(currentScreen)}  |  Date: {(string.IsNullOrEmpty(selectedDate) ? "Auto" : selectedDate)}");
            builder.AppendLine($"{BuildLlmStatus(health.runtimes.llm)}  |  STT {ToOnOff(health.runtimes.stt.available)}  |  TTS {ToOnOff(health.runtimes.tts.available)}");
            builder.AppendLine($"Voice replies {ToOnOff(settingsStore.Current.voice.speak_replies)}  |  Transcript {ToOnOff(settingsStore.Current.voice.show_transcript_preview)}");
            builder.Append($"Route {chatStore.CurrentRoute}  |  Provider {chatStore.CurrentProvider}  |  Fallbacks {chatStore.FallbackCount}");
            return builder.ToString().Trim();
        }

        private static string BuildLlmStatus(RuntimeHealth runtime)
        {
            var label = "LLM";
            if (runtime != null && !string.IsNullOrWhiteSpace(runtime.provider))
            {
                label += " " + runtime.provider;
            }

            return $"{label} {ToOnOff(runtime != null && runtime.available)}";
        }

        private static string ToOnOff(bool value) => value ? "On" : "Off";

        private static string ToTaskTabName(AppScreen screen) => screen switch
        {
            AppScreen.Today => "Today",
            AppScreen.Week => "Week",
            AppScreen.Inbox => "Inbox",
            AppScreen.Completed => "Completed",
            AppScreen.Settings => "Settings",
            _ => "Week",
        };
    }
}
