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

        public void RenderBootState(string bannerText, string detailText, Color accentColor)
        {
            SetRuntimeStatus(bannerText, accentColor);
            if (shell.HealthBanner != null)
            {
                shell.HealthBanner.text = bannerText;
                shell.HealthBanner.style.color = new StyleColor(accentColor);
            }

            if (shell.StageStatusText != null)
            {
                shell.StageStatusText.text = detailText;
            }
        }

        public void RenderHealth(HealthResponse health)
        {
            if (shell.HealthBanner == null)
            {
                return;
            }

            shell.HealthBanner.text = $"{HealthStatusMapper.ToLabel(health.status)} runtime";
            shell.HealthBanner.style.color = new StyleColor(HealthStatusMapper.ToColor(health.status));
            SetRuntimeStatus(shell.HealthBanner.text, HealthStatusMapper.ToColor(health.status));
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

            if (shell.ScheduleInsightTitle != null)
            {
                shell.ScheduleInsightTitle.text = BuildScheduleInsightTitle(currentScreen);
            }

            if (shell.ScheduleInsightSummary != null)
            {
                shell.ScheduleInsightSummary.text = BuildScheduleInsightSummary(health, currentScreen);
            }

            if (shell.ScheduleInsightMeta != null)
            {
                shell.ScheduleInsightMeta.text = BuildScheduleInsightMeta(selectedDate, settingsStore, chatStore);
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

        private void SetRuntimeStatus(string text, Color color)
        {
            if (shell.TopStatusChipLabel == null)
            {
                return;
            }

            shell.TopStatusChipLabel.text = text;
            shell.TopStatusChipLabel.style.color = new StyleColor(color);
        }

        private static string BuildStageStatusText(HealthResponse health, AppScreen currentScreen, string selectedDate, SettingsViewModelStore settingsStore, ChatViewModelStore chatStore)
        {
            var builder = new StringBuilder();
            builder.AppendLine($"DB {ToOnOff(health.database.available)} | Focus {ToTaskTabName(currentScreen)} | Date {ToDisplayDate(selectedDate)}");
            builder.AppendLine($"{BuildLlmStatus(health.runtimes.llm)}  |  STT {ToOnOff(health.runtimes.stt.available)}  |  TTS {ToOnOff(health.runtimes.tts.available)}");
            builder.AppendLine($"Voice replies {ToOnOff(settingsStore.Current.voice.speak_replies)}  |  Transcript {ToOnOff(settingsStore.Current.voice.show_transcript_preview)}");
            builder.Append($"Route {ToTextOrUnknown(chatStore.CurrentRoute)}  |  Provider {ToTextOrUnknown(chatStore.CurrentProvider)}  |  Fallbacks {chatStore.FallbackCount}");
            return builder.ToString().Trim();
        }

        private static string BuildScheduleInsightTitle(AppScreen screen) => screen switch
        {
            AppScreen.Inbox => "Inbox triage",
            AppScreen.Completed => "Completed review",
            AppScreen.Settings => "Settings snapshot",
            AppScreen.Today => "Home overview",
            _ => "Week overview",
        };

        private static string BuildScheduleInsightSummary(HealthResponse health, AppScreen currentScreen)
        {
            if (health.status == "error")
            {
                return "Backend is unavailable. Keep this shell in recovery mode until the local services come back.";
            }

            if (health.status == "partial")
            {
                return currentScreen switch
                {
                    AppScreen.Inbox => "Some runtime features are degraded, so this panel stays text-first while you sort new work.",
                    AppScreen.Completed => "Some runtime features are degraded, so use completed review to spot follow-up items manually.",
                    _ => "Some runtime features are degraded, so keep the schedule readable while chat and voice recover.",
                };
            }

            return currentScreen switch
            {
                AppScreen.Inbox => "Sort quick captures and unscheduled work here before they land on the calendar.",
                AppScreen.Completed => "Review what finished recently and watch for items that still need a follow-up.",
                AppScreen.Settings => "Adjust runtime and voice preferences here without losing shell context.",
                AppScreen.Today => "Use the home screen for quick-add and current assistant context.",
                _ => "Keep the week visible while the assistant collects routing and reminder updates in the background.",
            };
        }

        private static string BuildScheduleInsightMeta(string selectedDate, SettingsViewModelStore settingsStore, ChatViewModelStore chatStore)
        {
            var builder = new StringBuilder();
            builder.AppendLine($"Date {ToDisplayDate(selectedDate)}");
            builder.AppendLine($"Route {ToTextOrUnknown(chatStore.CurrentRoute)} | Provider {ToTextOrUnknown(chatStore.CurrentProvider)}");
            builder.Append($"Voice {ToOnOff(settingsStore.Current.voice.speak_replies)} | Transcript {ToOnOff(settingsStore.Current.voice.show_transcript_preview)} | Fallbacks {chatStore.FallbackCount}");
            return builder.ToString();
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
        private static string ToDisplayDate(string value) => string.IsNullOrWhiteSpace(value) ? "auto" : value;
        private static string ToTextOrUnknown(string value) => string.IsNullOrWhiteSpace(value) ? "unknown" : value;

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
