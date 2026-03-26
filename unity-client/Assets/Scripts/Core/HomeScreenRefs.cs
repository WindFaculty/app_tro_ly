using UnityEngine.UIElements;

namespace LocalAssistant.Core
{
    public sealed class HomeScreenRefs
    {
        public Label StagePlaceholderText;
        public Label TaskSummaryText;
        public Label TaskContentText;
        public TextField QuickAddInput;
        public Button QuickAddButton;

        public HomeScreenRefs()
        {
        }

        public HomeScreenRefs(VisualElement root)
        {
            StagePlaceholderText = root.Q<Label>(UiElementNames.Home.StagePlaceholderText);
            TaskSummaryText = root.Q<Label>(UiElementNames.Home.TaskSummaryText);
            TaskContentText = root.Q<Label>(UiElementNames.Home.TaskContentText);
            QuickAddInput = root.Q<TextField>(UiElementNames.Home.QuickAddInput);
            QuickAddButton = root.Q<Button>(UiElementNames.Home.QuickAddButton);
        }
    }
}
