using LocalAssistant.Core;
using LocalAssistant.Features.Settings;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace LocalAssistant.Tests.EditMode
{
    public class SettingsModuleTests
    {
        [Test]
        public void ApplyRendersOwnedSettingsSnapshot()
        {
            var refs = CreateRefs();
            var module = new SettingsModule(new SettingsScreenController(refs), new SettingsViewModelStore());
            var payload = new SettingsPayload();
            payload.voice.speak_replies = true;
            payload.voice.show_transcript_preview = true;
            payload.window_mode.mini_assistant_enabled = true;
            payload.reminder.speech_enabled = false;

            module.Apply(payload);

            Assert.IsTrue(module.Current.voice.speak_replies);
            Assert.IsTrue(refs.SpeakRepliesToggle.value);
            Assert.IsTrue(refs.TranscriptPreviewToggle.value);
            StringAssert.Contains("Voice replies are On", refs.SettingsSummaryText.text);
        }

        [Test]
        public void BindRoutesToggleChangesThroughOwnedStore()
        {
            var refs = CreateRefs();
            var controller = new SettingsScreenController(refs);
            var module = new SettingsModule(controller, new SettingsViewModelStore());
            var payload = new SettingsPayload();
            payload.voice.show_transcript_preview = true;
            module.Apply(payload);
            module.Bind();
            var changedRaised = false;
            module.SettingsChanged += () => changedRaised = true;

            controller.NotifyTranscriptPreviewChanged(false);

            Assert.IsTrue(changedRaised);
            Assert.IsFalse(module.Current.voice.show_transcript_preview);
            Assert.AreEqual("Unsaved changes.", refs.SettingsStatusText.text);
        }

        [Test]
        public void BindForwardsReloadAndSaveRequests()
        {
            var refs = CreateRefs();
            var controller = new SettingsScreenController(refs);
            var module = new SettingsModule(controller, new SettingsViewModelStore());
            module.Bind();
            var reloadRequested = false;
            var saveRequested = false;
            module.ReloadRequested += () => reloadRequested = true;
            module.SaveRequested += () => saveRequested = true;

            controller.RequestReload();
            controller.RequestSave();

            Assert.IsTrue(reloadRequested);
            Assert.IsTrue(saveRequested);
        }

        private static SettingsScreenRefs CreateRefs()
        {
            return new SettingsScreenRefs
            {
                SpeakRepliesToggle = new Toggle(),
                TranscriptPreviewToggle = new Toggle(),
                MiniAssistantToggle = new Toggle(),
                ReminderSpeechToggle = new Toggle(),
                SettingsSummaryText = new Label(),
                SettingsStatusText = new Label(),
                SettingsActionHintText = new Label(),
                SettingsVoiceSummaryText = new Label(),
                SettingsAutomationSummaryText = new Label(),
                SettingsModelSummaryText = new Label(),
                SettingsMemorySummaryText = new Label(),
                SaveSettingsButton = new Button(),
                ReloadSettingsButton = new Button(),
            };
        }
    }
}
