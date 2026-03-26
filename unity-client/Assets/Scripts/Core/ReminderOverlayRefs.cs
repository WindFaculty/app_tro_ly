using UnityEngine.UIElements;

namespace LocalAssistant.Core
{
    public sealed class ReminderOverlayRefs
    {
        public VisualElement ReminderCard;
        public Label ReminderText;

        public ReminderOverlayRefs()
        {
        }

        public ReminderOverlayRefs(VisualElement root)
        {
            ReminderCard = root.Q<VisualElement>(UiElementNames.Reminder.ReminderCard);
            ReminderText = root.Q<Label>(UiElementNames.Reminder.ReminderText);
        }
    }
}
