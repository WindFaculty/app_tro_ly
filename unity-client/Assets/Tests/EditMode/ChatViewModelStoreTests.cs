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
            Assert.AreEqual("hybrid_plan_then_groq / groq", snapshot.RouteBadgeText);
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
            Assert.AreEqual("Route pending", snapshot.RouteBadgeText);
            Assert.AreEqual("Backend unavailable", snapshot.StatusTitle);
            StringAssert.Contains("Only refresh remains available.", snapshot.StatusDetail);
        }

        [Test]
        public void CompatibilityAndStreamingRepliesShareDiagnosticsAndActionFormatting()
        {
            var compatibilityStore = new ChatViewModelStore();
            compatibilityStore.BeginTurn("nhac toi hop nhom", false, true);
            compatibilityStore.ApplyCompatibilityResponse(new ChatResponsePayload
            {
                conversation_id = "conv-1",
                reply_text = "Da tao nhac viec.",
                route = "hybrid_plan_then_groq",
                provider = "groq",
                latency_ms = 321,
                fallback_used = true,
                task_actions = new System.Collections.Generic.List<TaskActionReport>
                {
                    new()
                    {
                        type = "create_task",
                        title = "hop nhom",
                        detail = "Task created from compatibility mode",
                    },
                },
            });

            var streamingStore = new ChatViewModelStore();
            streamingStore.BeginTurn("nhac toi hop nhom", false, true);
            streamingStore.ApplyAssistantChunk("Da tao");
            streamingStore.ApplyStreamingFinal(new AssistantFinalEvent
            {
                conversation_id = "conv-1",
                session_id = "session-1",
                reply_text = "Da tao nhac viec.",
                route = "hybrid_plan_then_groq",
                provider = "groq",
                latency_ms = 321,
                fallback_used = true,
                task_actions = new System.Collections.Generic.List<TaskActionReport>
                {
                    new()
                    {
                        type = "create_task",
                        title = "hop nhom",
                        detail = "Task created from compatibility mode",
                    },
                },
            });

            var compatibilitySnapshot = compatibilityStore.BuildPanelSnapshot(true);
            var streamingSnapshot = streamingStore.BuildPanelSnapshot(true);

            Assert.AreEqual(compatibilitySnapshot.RouteBadgeText, streamingSnapshot.RouteBadgeText);
            Assert.AreEqual(compatibilitySnapshot.StatusDetail, streamingSnapshot.StatusDetail);
            Assert.AreEqual(compatibilitySnapshot.ActionSummaryTitle, streamingSnapshot.ActionSummaryTitle);
            Assert.AreEqual(compatibilitySnapshot.ActionSummaryText, streamingSnapshot.ActionSummaryText);
            StringAssert.Contains("Da tao nhac viec.", streamingSnapshot.Transcript);
        }

        [Test]
        public void ApplyPlannerActionResultBuildsChatOwnedActionSummary()
        {
            var store = new ChatViewModelStore();
            store.ApplyPlannerActionResult("reschedule_task", "task-2", "Gui mail", "Scheduled to 2026-04-05 from the Schedule inbox view.");

            var snapshot = store.BuildPanelSnapshot(true);

            Assert.AreEqual("Last task action", snapshot.ActionSummaryTitle);
            StringAssert.Contains("Rescheduled 'Gui mail'.", snapshot.ActionSummaryText);
            StringAssert.Contains("Schedule inbox view", snapshot.ActionSummaryText);
        }
    }
}
