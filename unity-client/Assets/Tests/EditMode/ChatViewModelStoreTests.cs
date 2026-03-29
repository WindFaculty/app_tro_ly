using LocalAssistant.Chat;
using LocalAssistant.Core;
using NUnit.Framework;

namespace LocalAssistant.Tests.EditMode
{
    public class ChatViewModelStoreTests
    {
        [Test]
        public void BuildTranscriptIncludesDraftAssistantReply()
        {
            var store = new ChatViewModelStore();
            store.AddUser("Lap ke hoach hom nay");
            store.SetDiagnostics("hybrid_plan_then_groq", "groq", 321, true);
            store.AppendAssistantDraft("Ban nen uu tien ");
            store.AppendAssistantDraft("slide truoc.");

            var transcript = store.BuildTranscript();

            StringAssert.Contains("Lap ke hoach hom nay", transcript);
            StringAssert.Contains("slide truoc.", transcript);
        }

        [Test]
        public void BuildPanelSnapshotIncludesDiagnosticsActionConfirmationAndTranscriptPreview()
        {
            var store = new ChatViewModelStore();
            store.SetDiagnostics("hybrid_plan_then_groq", "groq", 321, true);
            store.SetListening(true);
            store.SetTranscriptPreview("nhac toi hop nhom ngay mai");
            store.SetTaskActions(new[]
            {
                new TaskActionReport
                {
                    type = "create_task",
                    title = "hop nhom",
                    detail = "Task created from validated action",
                },
            });

            var snapshot = store.BuildPanelSnapshot(true);

            Assert.AreEqual("LISTENING", snapshot.StatusBadge);
            StringAssert.Contains("hybrid_plan_then_groq", snapshot.StatusDetail);
            StringAssert.Contains("Fallbacks 1", snapshot.StatusDetail);
            Assert.AreEqual("nhac toi hop nhom ngay mai", snapshot.TranscriptPreviewText);
            Assert.AreEqual("Last task action", snapshot.ActionSummaryTitle);
            StringAssert.Contains("Created 'hop nhom'.", snapshot.ActionSummaryText);
        }

        [Test]
        public void BuildPanelSnapshotUsesSystemStatusWhenIdle()
        {
            var store = new ChatViewModelStore();
            store.SetSystemStatus("ERROR", "Backend unavailable", "Only refresh remains available.");

            var snapshot = store.BuildPanelSnapshot(true);

            Assert.AreEqual("ERROR", snapshot.StatusBadge);
            Assert.AreEqual("Backend unavailable", snapshot.StatusTitle);
            StringAssert.Contains("Only refresh remains available.", snapshot.StatusDetail);
        }
    }
}
