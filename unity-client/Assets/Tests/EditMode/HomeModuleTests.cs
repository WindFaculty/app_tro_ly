using LocalAssistant.Core;
using LocalAssistant.Features.Home;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace LocalAssistant.Tests.EditMode
{
    public class HomeModuleTests
    {
        [Test]
        public void QuickAddPublishesRawInputInsteadOfBusinessCommandText()
        {
            var refs = CreateRefs();
            var controller = new HomeScreenController(refs);
            var module = new HomeModule(controller);
            module.Bind();
            var requested = string.Empty;
            module.QuickAddRequested += value => requested = value;
            refs.QuickAddInput.value = "Buy fruit";

            controller.RequestQuickAdd();

            Assert.AreEqual("Buy fruit", requested);
        }

        private static HomeScreenRefs CreateRefs()
        {
            return new HomeScreenRefs
            {
                QuickAddInput = new TextField(),
                QuickAddButton = new Button(),
                QuickAddStatusText = new Label(),
                QuickAddHintText = new Label(),
                TaskSummaryText = new Label(),
                TaskContentText = new Label(),
                TaskEmptyStateText = new Label(),
                TodayCountText = new Label(),
                DueSoonCountText = new Label(),
                OverdueCountText = new Label(),
                InboxCountText = new Label(),
                CompletedCountText = new Label(),
                FocusText = new Label(),
                DueSoonText = new Label(),
                OverdueText = new Label(),
                StagePlaceholderText = new Label(),
                HomeChatStatusBadge = new Label(),
                HomeChatStatusTitle = new Label(),
                HomeChatStatusDetail = new Label(),
                HomeAvatarStateBadge = new Label(),
            };
        }
    }
}
