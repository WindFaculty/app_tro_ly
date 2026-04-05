using System.Collections.Generic;
using LocalAssistant.Core;
using LocalAssistant.Features.Home;
using NUnit.Framework;

namespace LocalAssistant.Tests.EditMode
{
    public class HomeQuickAddApplicationServiceTests
    {
        [Test]
        public void CreateAssistantMessageFormatsQuickAddOutsidePresentationLayer()
        {
            var service = new HomeQuickAddApplicationService();
            Assert.IsTrue(HomeQuickAddRequest.TryCreate("  Buy milk  ", out var request));

            var message = service.CreateAssistantMessage(request);

            Assert.AreEqual("Add task Buy milk", message);
        }

        [Test]
        public void ResolveCompletionBuildsSuccessStatusFromTaskAction()
        {
            var service = new HomeQuickAddApplicationService();

            var status = service.ResolveCompletion(new List<TaskActionReport>
            {
                new() { title = "Buy milk", detail = "Task list refreshed." },
            });

            Assert.AreEqual(QuickAddStatusKind.Success, status.Kind);
            Assert.AreEqual("Added 'Buy milk'. Task list refreshed.", status.Message);
        }
    }
}
