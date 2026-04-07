using LocalAssistant.Chat;
using LocalAssistant.Core;
using LocalAssistant.Features.Chat;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace LocalAssistant.Tests.PlayMode
{
    public class ChatPanelControllerPlayModeTests
    {
        [Test]
        public void SubmitCurrentInputRaisesEventAndClearsInput()
        {
            var refs = CreateRefs();
            var controller = new ChatPanelController(refs);
            string submitted = null;
            controller.SendRequested += value => submitted = value;
            refs.ChatInput.value = "Xin chao";

            controller.SubmitCurrentInput();

            Assert.AreEqual("Xin chao", submitted);
            Assert.AreEqual(string.Empty, refs.ChatInput.value);
        }

        [Test]
        public void RenderAndInteractableUpdateChatControls()
        {
            var refs = CreateRefs();
            var controller = new ChatPanelController(refs);
            var snapshot = new ChatPanelSnapshot
            {
                StatusBadge = "THINKING",
                StatusTitle = "Planning the next response",
                StatusDetail = "Route planner | Provider local | Latency 40 ms",
                RouteBadgeText = "planner / local",
                TranscriptPreviewTitle = "Transcript preview",
                TranscriptPreviewText = "lap ke hoach ngay mai",
                ActionSummaryTitle = "Last task action",
                ActionSummaryText = "Created 'hop nhom'.",
                Transcript = "AI\nDang xu ly",
            };

            controller.Render(snapshot);
            controller.SetInteractable(false, true);

            Assert.AreEqual("THINKING", refs.ChatStateBadge.text);
            Assert.AreEqual("Planning the next response", refs.ChatStateTitle.text);
            Assert.AreEqual("Route planner | Provider local | Latency 40 ms", refs.ChatStateDetail.text);
            Assert.AreEqual("planner / local", refs.ChatRouteBadge.text);
            Assert.AreEqual("lap ke hoach ngay mai", refs.ChatTranscriptPreviewText.text);
            Assert.AreEqual("Created 'hop nhom'.", refs.ChatActionSummaryText.text);
            Assert.AreEqual("AI\nDang xu ly", refs.ChatLogText.text);
            Assert.IsFalse(refs.ChatInput.enabledSelf);
            Assert.IsFalse(refs.SendButton.enabledSelf);
            Assert.IsTrue(refs.MicButton.enabledSelf);
        }

        [Test]
        public void HandleChatInputKeyDownSubmitsUsingEnter()
        {
            var refs = CreateRefs();
            var controller = new ChatPanelController(refs);
            string submitted = null;
            controller.SendRequested += value => submitted = value;
            refs.ChatInput.value = "Xin chao Enter";

            using var evt = KeyDownEvent.GetPooled('\n', KeyCode.Return, EventModifiers.None);
            controller.HandleChatInputKeyDown(evt);

            Assert.AreEqual("Xin chao Enter", submitted);
        }

        private static ChatPanelRefs CreateRefs()
        {
            return new ChatPanelRefs
            {
                ChatStateBadge = new Label(),
                ChatStateTitle = new Label(),
                ChatStateDetail = new Label(),
                ChatRouteBadge = new Label(),
                ChatTranscriptPreviewTitle = new Label(),
                ChatTranscriptPreviewText = new Label(),
                ChatActionSummaryTitle = new Label(),
                ChatActionSummaryText = new Label(),
                ChatLogText = new Label(),
                ChatInput = new TextField(),
                SendButton = new Button(),
                MicButton = new Button(),
            };
        }
    }
}
