using LocalAssistant.App;
using LocalAssistant.Chat;
using LocalAssistant.Core;
using NUnit.Framework;

namespace LocalAssistant.Tests.EditMode
{
    public class ShellStageSnapshotBuilderTests
    {
        [Test]
        public void BuildIncludesSelectedDateRouteAndVoiceFlags()
        {
            var settings = new SettingsViewModelStore();
            settings.Apply(new SettingsPayload
            {
                voice = new VoiceSettings
                {
                    speak_replies = false,
                    show_transcript_preview = true,
                },
            });

            var chat = new ChatViewModelStore();
            chat.SetDiagnostics("hybrid_plan_then_groq", "groq", 210, true);

            var snapshot = ShellStageSnapshotBuilder.Build(
                AvatarState.Talking,
                new HealthResponse
                {
                    status = "ready",
                    database = new DatabaseHealth { available = true },
                    runtimes = new RuntimeHealthCollection
                    {
                        llm = new RuntimeHealth { available = true, provider = "groq" },
                        stt = new RuntimeHealth { available = false },
                        tts = new RuntimeHealth { available = true },
                    },
                },
                AppScreen.Inbox,
                "2026-04-04",
                settings,
                chat);

            Assert.AreEqual("Talking", snapshot.AvatarStateText);
            Assert.AreEqual("Inbox triage", snapshot.InsightTitle);
            StringAssert.Contains("Date 2026-04-04", snapshot.InsightMeta);
            StringAssert.Contains("Route hybrid_plan_then_groq", snapshot.InsightMeta);
            StringAssert.Contains("Voice Off", snapshot.InsightMeta);
            StringAssert.Contains("DB On | Focus Inbox | Date 2026-04-04", snapshot.StageStatusText);
        }

        [Test]
        public void BuildUsesRecoveryCopyWhenHealthIsPartial()
        {
            var snapshot = ShellStageSnapshotBuilder.Build(
                AvatarState.Warning,
                new HealthResponse { status = "partial" },
                AppScreen.Completed,
                string.Empty,
                new SettingsViewModelStore(),
                new ChatViewModelStore());

            Assert.AreEqual("Completed review", snapshot.InsightTitle);
            StringAssert.Contains("use completed review", snapshot.InsightSummary);
            StringAssert.Contains("Date auto", snapshot.InsightMeta);
        }
    }
}
