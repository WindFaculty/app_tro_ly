using UnityEngine;
using UnityEngine.UIElements;

namespace LocalAssistant.Chat
{
    public sealed class SubtitlePresenter : MonoBehaviour
    {
        private Label subtitleText;
        private VisualElement cardElement;

        public void Bind(Label textElement, VisualElement card)
        {
            subtitleText = textElement;
            cardElement = card;
            Hide();
        }

        public void Show(string value)
        {
            if (subtitleText == null)
            {
                return;
            }

            subtitleText.text = value;
            SetCardVisibility(true);
        }

        public void Hide()
        {
            if (subtitleText == null)
            {
                return;
            }

            subtitleText.text = string.Empty;
            SetCardVisibility(false);
        }

        private void SetCardVisibility(bool visible)
        {
            if (cardElement != null)
            {
                if (visible)
                    cardElement.RemoveFromClassList("hidden");
                else
                    cardElement.AddToClassList("hidden");
            }
        }
    }
}
