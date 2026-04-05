using UnityEngine.UIElements;

namespace LocalAssistant.Core
{
    public sealed class HomeScreenRefs
    {
        public VisualElement HomeStageViewport;
        public Label HomeAvatarStateBadge;
        public Label StagePlaceholderText;
        public Label SelectedRoomObjectTitleText;
        public Label SelectedRoomObjectMetaText;
        public Label SelectedRoomObjectActionText;
        public Label RoomActivityTitleText;
        public Label RoomActivityDetailText;
        public Label RoomModeText;
        public Button RoomGoToButton;
        public Button RoomInspectButton;
        public Button RoomUseButton;
        public Button RoomReturnButton;
        public Button RoomHotspotToggleButton;
        public Label TaskSummaryText;
        public Label TaskContentText;
        public Label TaskEmptyStateText;
        public Label QuickAddStatusText;
        public TextField QuickAddInput;
        public Button QuickAddButton;
        public Label QuickAddHintText;
        public Label TodayCountText;
        public Label DueSoonCountText;
        public Label OverdueCountText;
        public Label InboxCountText;
        public Label CompletedCountText;
        public Label FocusText;
        public Label DueSoonText;
        public Label OverdueText;
        public Label HomeChatStatusBadge;
        public Label HomeChatStatusTitle;
        public Label HomeChatStatusDetail;

        public HomeScreenRefs()
        {
        }

        public HomeScreenRefs(VisualElement root)
        {
            HomeStageViewport = root.Q<VisualElement>(UiElementNames.Home.HomeStageViewport);
            HomeAvatarStateBadge = root.Q<Label>("HomeAvatarStateBadge");
            StagePlaceholderText = root.Q<Label>(UiElementNames.Home.StagePlaceholderText);
            SelectedRoomObjectTitleText = root.Q<Label>(UiElementNames.Home.SelectedRoomObjectTitleText);
            SelectedRoomObjectMetaText = root.Q<Label>(UiElementNames.Home.SelectedRoomObjectMetaText);
            SelectedRoomObjectActionText = root.Q<Label>(UiElementNames.Home.SelectedRoomObjectActionText);
            RoomActivityTitleText = root.Q<Label>(UiElementNames.Home.RoomActivityTitleText);
            RoomActivityDetailText = root.Q<Label>(UiElementNames.Home.RoomActivityDetailText);
            RoomModeText = root.Q<Label>(UiElementNames.Home.RoomModeText);
            RoomGoToButton = root.Q<Button>(UiElementNames.Home.RoomGoToButton);
            RoomInspectButton = root.Q<Button>(UiElementNames.Home.RoomInspectButton);
            RoomUseButton = root.Q<Button>(UiElementNames.Home.RoomUseButton);
            RoomReturnButton = root.Q<Button>(UiElementNames.Home.RoomReturnButton);
            RoomHotspotToggleButton = root.Q<Button>(UiElementNames.Home.RoomHotspotToggleButton);
            TaskSummaryText = root.Q<Label>(UiElementNames.Home.TaskSummaryText);
            TaskContentText = root.Q<Label>(UiElementNames.Home.TaskContentText);
            TaskEmptyStateText = root.Q<Label>(UiElementNames.Home.TaskEmptyStateText);
            QuickAddStatusText = root.Q<Label>(UiElementNames.Home.QuickAddStatusText);
            QuickAddInput = root.Q<TextField>(UiElementNames.Home.QuickAddInput);
            QuickAddButton = root.Q<Button>(UiElementNames.Home.QuickAddButton);
            QuickAddHintText = root.Q<Label>(UiElementNames.Home.QuickAddHintText);
            TodayCountText = root.Q<Label>(UiElementNames.Home.TodayCountText);
            DueSoonCountText = root.Q<Label>(UiElementNames.Home.DueSoonCountText);
            OverdueCountText = root.Q<Label>(UiElementNames.Home.OverdueCountText);
            InboxCountText = root.Q<Label>(UiElementNames.Home.InboxCountText);
            CompletedCountText = root.Q<Label>(UiElementNames.Home.CompletedCountText);
            FocusText = root.Q<Label>(UiElementNames.Home.FocusText);
            DueSoonText = root.Q<Label>(UiElementNames.Home.DueSoonText);
            OverdueText = root.Q<Label>(UiElementNames.Home.OverdueText);
            HomeChatStatusBadge = root.Q<Label>(UiElementNames.Home.HomeChatStatusBadge);
            HomeChatStatusTitle = root.Q<Label>(UiElementNames.Home.HomeChatStatusTitle);
            HomeChatStatusDetail = root.Q<Label>(UiElementNames.Home.HomeChatStatusDetail);
        }
    }
}
