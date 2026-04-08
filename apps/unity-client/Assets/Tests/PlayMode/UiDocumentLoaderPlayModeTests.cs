using LocalAssistant.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.TextCore.Text;

namespace LocalAssistant.Tests.PlayMode
{
    public class UiDocumentLoaderPlayModeTests
    {
        [Test]
        public void LoadBuildsTypedRefsForShellScreensAndOverlays()
        {
            var parent = new GameObject("UiLoaderTestRoot");
            try
            {
                var refs = UiDocumentLoader.Load(parent.transform);

                Assert.NotNull(refs.Document);
                Assert.NotNull(refs.Document.panelSettings);
                Assert.NotNull(refs.Document.panelSettings.textSettings);
                Assert.That(refs.Document.panelSettings.textSettings.name, Does.Contain("_Runtime"));
                Assert.NotNull(refs.Document.panelSettings.textSettings.defaultFontAsset);
                Assert.That(refs.Document.panelSettings.textSettings.defaultFontAsset, Is.TypeOf<FontAsset>());
                Assert.That(refs.Document.panelSettings.textSettings.defaultFontAsset.name, Does.Contain("_UITK_Runtime"));
                Assert.NotNull(refs.Document.panelSettings.textSettings.defaultFontAsset.atlasTextures);
                Assert.That(refs.Document.panelSettings.textSettings.defaultFontAsset.atlasTextures.Length, Is.GreaterThan(0));
                Assert.NotNull(refs.Document.panelSettings.textSettings.defaultFontAsset.atlasTextures[0]);
                Assert.NotNull(refs.Shell);
                Assert.NotNull(refs.Home);
                Assert.NotNull(refs.Schedule);
                Assert.NotNull(refs.Settings);
                Assert.NotNull(refs.Chat);
                Assert.NotNull(refs.Subtitle);
                Assert.NotNull(refs.Reminder);
                Assert.NotNull(refs.Shell.HealthBanner);
                Assert.NotNull(refs.Shell.FocusStageButton);
                Assert.NotNull(refs.Shell.ToggleCalendarButton);
                Assert.NotNull(refs.Shell.ToggleChatButton);
                Assert.NotNull(refs.Shell.ToggleSettingsButton);
                Assert.NotNull(refs.Shell.CalendarSheetHost);
                Assert.NotNull(refs.Shell.ChatPanelHost);
                Assert.NotNull(refs.Shell.SettingsDrawer);
                Assert.NotNull(refs.Shell.ScheduleInsightTitle);
                Assert.NotNull(refs.Shell.ScheduleInsightSummary);
                Assert.NotNull(refs.Shell.ScheduleInsightMeta);
                Assert.NotNull(refs.Home.StagePlaceholderText);
                Assert.NotNull(refs.Schedule.TaskSheetHeaderTitle);
                Assert.NotNull(refs.Settings.SettingsPanel);
                Assert.NotNull(refs.Chat.ChatInput);
                Assert.NotNull(refs.Chat.ChatStateTitle);
                Assert.NotNull(refs.Chat.ChatTranscriptPreviewText);
                Assert.NotNull(refs.Chat.ChatActionSummaryText);
                Assert.NotNull(refs.Subtitle.SubtitleText);
                Assert.NotNull(refs.Reminder.ReminderText);
                var focusItems = refs.Document.rootVisualElement.Query<VisualElement>(className: "home-focus-item").ToList();
                Assert.AreEqual(3, focusItems.Count);
                Assert.IsTrue(focusItems[2].ClassListContains("home-focus-item-final"));
            }
            finally
            {
                Object.DestroyImmediate(parent);
            }
        }
    }
}
