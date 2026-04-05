using LocalAssistant.Core;
using LocalAssistant.Features.Home;
using LocalAssistant.World.Interaction;
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

        [Test]
        public void SelectedRoomObjectOverlayRendersSnapshotDetails()
        {
            var refs = CreateRefs();
            var controller = new HomeScreenController(refs);

            controller.RenderSelectedRoomObject(new RoomObjectSelectionSnapshot
            {
                HasSelection = true,
                DisplayName = "Work Laptop",
                CategoryLabel = "Interactive | Inspect + Focus",
                StateText = "State: ready to inspect and focus",
                DetailText = "Tags: work, laptop, interactive",
                SuggestedActionText = "Suggested actions: go to, inspect, use.",
                ActionText = "Action: Inspect object and focus camera",
                SupportsGoTo = true,
                SupportsInspect = true,
                SupportsUse = true,
            });

            Assert.AreEqual("Work Laptop", refs.SelectedRoomObjectTitleText.text);
            Assert.AreEqual("Interactive | Inspect + Focus", refs.SelectedRoomObjectMetaText.text);
            StringAssert.Contains("State:", refs.SelectedRoomObjectActionText.text);
            StringAssert.Contains("focus camera", refs.SelectedRoomObjectActionText.text);
        }

        [Test]
        public void RoomActionDockPublishesEnabledCommand()
        {
            var refs = CreateRefs();
            var controller = new HomeScreenController(refs);
            HomeRoomAction? observedAction = null;
            controller.RoomActionRequested += action => observedAction = action;
            controller.RenderRoomOverlayState(new HomeRoomOverlayState
            {
                ActivityTitle = "Focused on Work Laptop",
                ActivityDetail = "Suggested actions: go to, inspect, use.",
                ModeLabel = "OBJECT FOCUS",
                HotspotButtonText = "Hide hotspots",
                GoToEnabled = true,
                InspectEnabled = true,
                UseEnabled = true,
                ReturnEnabled = true,
                ToggleHotspotsEnabled = true,
            });

            controller.RequestRoomAction(HomeRoomAction.Use);

            Assert.AreEqual(HomeRoomAction.Use, observedAction);
            Assert.AreEqual("OBJECT FOCUS", refs.RoomModeText.text);
            Assert.AreEqual("Hide hotspots", refs.RoomHotspotToggleButton.text);
            Assert.IsTrue(refs.RoomUseButton.enabledSelf);
        }

        private static HomeScreenRefs CreateRefs()
        {
            return new HomeScreenRefs
            {
                HomeStageViewport = new VisualElement(),
                RoomActivityTitleText = new Label(),
                RoomActivityDetailText = new Label(),
                RoomModeText = new Label(),
                RoomGoToButton = new Button(),
                RoomInspectButton = new Button(),
                RoomUseButton = new Button(),
                RoomReturnButton = new Button(),
                RoomHotspotToggleButton = new Button(),
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
                SelectedRoomObjectTitleText = new Label(),
                SelectedRoomObjectMetaText = new Label(),
                SelectedRoomObjectActionText = new Label(),
            };
        }
    }
}
