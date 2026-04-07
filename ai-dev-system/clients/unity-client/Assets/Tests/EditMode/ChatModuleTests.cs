using LocalAssistant.Core;
using LocalAssistant.Features.Chat;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace LocalAssistant.Tests.EditMode
{
    public class ChatModuleTests
    {
        [Test]
        public void RenderUsesOwnedStateAndUpdatesRouteBadge()
        {
            var refs = CreateRefs();
            var module = new ChatModule(refs);
            module.AddAssistant("Xin chao");
            module.SetDiagnostics("hybrid_plan_then_groq", "groq", 112, true);
            module.SetListening(true);

            var snapshot = module.Render(true);

            Assert.AreEqual("LISTENING", snapshot.StatusBadge);
            Assert.AreEqual("hybrid_plan_then_groq / groq", snapshot.RouteBadgeText);
            Assert.AreEqual("hybrid_plan_then_groq / groq", refs.ChatRouteBadge.text);
            StringAssert.Contains("Xin chao", refs.ChatLogText.text);
        }

        private static ChatPanelRefs CreateRefs()
        {
            return new ChatPanelRefs
            {
                ChatRouteBadge = new Label(),
                ChatStateBadge = new Label(),
                ChatStateTitle = new Label(),
                ChatStateDetail = new Label(),
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
