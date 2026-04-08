using UnityEngine.UIElements;

namespace LocalAssistant.Core
{
    public sealed class SubtitleOverlayRefs
    {
        public VisualElement SubtitleCard;
        public Label SubtitleText;

        public SubtitleOverlayRefs()
        {
        }

        public SubtitleOverlayRefs(VisualElement root)
        {
            SubtitleCard = root.Q<VisualElement>(UiElementNames.Subtitle.SubtitleCard);
            SubtitleText = root.Q<Label>(UiElementNames.Subtitle.SubtitleText);
        }
    }
}
