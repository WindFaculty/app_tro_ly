using LocalAssistant.Features.Chat;
using NUnit.Framework;

namespace LocalAssistant.Tests.EditMode
{
    public class ChatTurnApplicationServiceTests
    {
        [Test]
        public void BuildPlanCreatesStreamingPayloadWhenStreamingIsAvailable()
        {
            var service = new ChatTurnApplicationService();
            Assert.IsTrue(ChatTurnRequest.TryCreate("Plan today", "conv-1", "session-1", "2026-04-05", false, true, out var request));

            var plan = service.BuildPlan(request, true);

            Assert.AreEqual(ChatTurnTransport.Streaming, plan.Transport);
            Assert.AreEqual("text_turn", plan.StreamRequest.type);
            Assert.AreEqual("session-1", plan.StreamRequest.session_id);
            Assert.AreEqual("Plan today", plan.StreamRequest.message);
        }

        [Test]
        public void BuildPlanCreatesCompatibilityPayloadWhenStreamingIsUnavailable()
        {
            var service = new ChatTurnApplicationService();
            Assert.IsTrue(ChatTurnRequest.TryCreate("Plan today", "conv-1", "session-1", "2026-04-05", true, false, out var request));

            var plan = service.BuildPlan(request, false);

            Assert.AreEqual(ChatTurnTransport.Compatibility, plan.Transport);
            Assert.AreEqual("Plan today", plan.CompatibilityRequest.message);
            Assert.AreEqual("conv-1", plan.CompatibilityRequest.conversation_id);
            Assert.AreEqual("2026-04-05", plan.CompatibilityRequest.selected_date);
            Assert.IsFalse(plan.CompatibilityRequest.include_voice);
        }
    }
}
