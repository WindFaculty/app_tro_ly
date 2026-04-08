using LocalAssistant.Core;
using NUnit.Framework;

namespace LocalAssistant.Tests.EditMode
{
    public class SettingsViewModelStoreTests
    {
        [Test]
        public void ApplyClonesPayloadAndBuildsReadableSummary()
        {
            var payload = new SettingsPayload();
            payload.voice.speak_replies = false;
            payload.window_mode.mini_assistant_enabled = true;
            payload.model.name = "llama3.2:3b";
            payload.reminder.lead_minutes = 30;

            var store = new SettingsViewModelStore();
            store.Apply(payload);
            payload.voice.speak_replies = true;

            Assert.IsFalse(store.Current.voice.speak_replies);

            var summary = store.BuildSummary();
            StringAssert.Contains("Voice replies: Off", summary);
            StringAssert.Contains("Mini assistant: On", summary);
            StringAssert.Contains("llama3.2:3b", summary);
            StringAssert.Contains("30 minutes", summary);
        }

        [Test]
        public void SnapshotPreservesToggleUpdates()
        {
            var store = new SettingsViewModelStore();

            store.SetSpeakReplies(false);
            store.SetTranscriptPreview(false);
            store.SetMiniAssistantEnabled(true);
            store.SetReminderSpeechEnabled(false);

            var snapshot = store.Snapshot();

            Assert.IsFalse(snapshot.voice.speak_replies);
            Assert.IsFalse(snapshot.voice.show_transcript_preview);
            Assert.IsTrue(snapshot.window_mode.mini_assistant_enabled);
            Assert.IsFalse(snapshot.reminder.speech_enabled);
        }

        [Test]
        public void HasUnsavedChangesTracksBaselineAfterApply()
        {
            var store = new SettingsViewModelStore();
            store.Apply(new SettingsPayload());

            Assert.IsFalse(store.HasUnsavedChanges());

            store.SetSpeakReplies(false);
            Assert.IsTrue(store.HasUnsavedChanges());

            store.MarkSaved();
            Assert.IsFalse(store.HasUnsavedChanges());
        }
    }
}
