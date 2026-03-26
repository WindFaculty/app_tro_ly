using UnityEngine.UIElements;

namespace LocalAssistant.Core
{
    public sealed class ChatPanelRefs
    {
        public VisualElement ChatPanelView;
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
            ChatLogText = root.Q<Label>(UiElementNames.Chat.ChatLogText);
            ChatInput = root.Q<TextField>(UiElementNames.Chat.ChatInput);
            SendButton = root.Q<Button>(UiElementNames.Chat.SendButton);
            MicButton = root.Q<Button>(UiElementNames.Chat.MicButton);
        }
    }
}
