using LocalAssistant.Chat;
using NUnit.Framework;

namespace LocalAssistant.Tests.EditMode
{
    public class ChatViewModelStoreTests
    {
        [Test]
        public void BuildTranscriptIncludesDiagnosticsAndDraftAssistantReply()
        {
            var store = new ChatViewModelStore();
            store.AddUser("Lap ke hoach hom nay");
            store.SetDiagnostics("hybrid_plan_then_groq", "groq", 321, true);
            store.AppendAssistantDraft("Ban nen uu tien ");
            store.AppendAssistantDraft("slide truoc.");

            var transcript = store.BuildTranscript();

            StringAssert.Contains("hybrid_plan_then_groq", transcript);
            StringAssert.Contains("groq", transcript);
            StringAssert.Contains("Fallbacks", transcript);
            StringAssert.Contains("slide truoc.", transcript);
        }
    }
}
