using LocalAssistant.Core;
using NUnit.Framework;
using UnityEngine;

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
                Assert.NotNull(refs.Shell);
                Assert.NotNull(refs.Home);
                Assert.NotNull(refs.Schedule);
                Assert.NotNull(refs.Settings);
                Assert.NotNull(refs.Chat);
                Assert.NotNull(refs.Subtitle);
                Assert.NotNull(refs.Reminder);
                Assert.NotNull(refs.Shell.HealthBanner);
                Assert.NotNull(refs.Home.StagePlaceholderText);
                Assert.NotNull(refs.Schedule.TaskSheetHeaderTitle);
                Assert.NotNull(refs.Settings.SettingsPanel);
                Assert.NotNull(refs.Chat.ChatInput);
                Assert.NotNull(refs.Subtitle.SubtitleText);
                Assert.NotNull(refs.Reminder.ReminderText);
            }
            finally
            {
                Object.DestroyImmediate(parent);
            }
        }
    }
}
