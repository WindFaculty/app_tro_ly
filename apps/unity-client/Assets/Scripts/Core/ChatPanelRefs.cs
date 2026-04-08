using UnityEngine.UIElements;

namespace LocalAssistant.Core
{
    public sealed class ChatPanelRefs
    {
        public VisualElement ChatPanelView;
        public Label ChatStateBadge;
        public Label ChatStateTitle;
        public Label ChatStateDetail;
        public Label ChatRouteBadge;
        public Label ChatTranscriptPreviewTitle;
        public Label ChatTranscriptPreviewText;
        public Label ChatActionSummaryTitle;
        public Label ChatActionSummaryText;
        public Label ChatLogText;
        public TextField ChatInput;
        public Button SendButton;
        public Button MicButton;

        public ChatPanelRefs()
        {
        }

        public ChatPanelRefs(VisualElement root)
        {
            ChatPanelView = root.Q<VisualElement>(UiElementNames.Chat.ChatPanelView);
            ChatStateBadge = root.Q<Label>(UiElementNames.Chat.ChatStateBadge);
            ChatStateTitle = root.Q<Label>(UiElementNames.Chat.ChatStateTitle);
            ChatStateDetail = root.Q<Label>(UiElementNames.Chat.ChatStateDetail);
            ChatRouteBadge = root.Q<Label>(UiElementNames.Chat.ChatRouteBadge);
            ChatTranscriptPreviewTitle = root.Q<Label>(UiElementNames.Chat.ChatTranscriptPreviewTitle);
            ChatTranscriptPreviewText = root.Q<Label>(UiElementNames.Chat.ChatTranscriptPreviewText);
            ChatActionSummaryTitle = root.Q<Label>(UiElementNames.Chat.ChatActionSummaryTitle);
            ChatActionSummaryText = root.Q<Label>(UiElementNames.Chat.ChatActionSummaryText);
            ChatLogText = root.Q<Label>(UiElementNames.Chat.ChatLogText);
            ChatInput = root.Q<TextField>(UiElementNames.Chat.ChatInput);
            SendButton = root.Q<Button>(UiElementNames.Chat.SendButton);
            MicButton = root.Q<Button>(UiElementNames.Chat.MicButton);
        }
    }
}
