using LocalAssistant.Core;
using LocalAssistant.Features.Chat;
using NUnit.Framework;
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

            controller.Render("Transcript text");
            controller.SetInteractable(false, true);

            Assert.AreEqual("Transcript text", refs.ChatLogText.text);
            Assert.IsFalse(refs.ChatInput.enabledSelf);
            Assert.IsFalse(refs.SendButton.enabledSelf);
            Assert.IsTrue(refs.MicButton.enabledSelf);
        }

        private static AssistantUiRefs CreateRefs()
        {
            return new AssistantUiRefs
            {
                ChatLogText = new Label(),
                ChatInput = new TextField(),
                SendButton = new Button(),
                MicButton = new Button(),
            };
        }
    }
}
