using UnityEngine;
using UnityEngine.UI;

namespace LocalAssistant.Chat
{
    public sealed class SubtitlePresenter : MonoBehaviour
    {
        [SerializeField] private Text subtitleText;

        public void Bind(Text value)
        {
            subtitleText = value;
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
            subtitleText.gameObject.SetActive(true);
        }

        public void Hide()
        {
            if (subtitleText == null)
            {
                return;
            }

            subtitleText.text = string.Empty;
            subtitleText.gameObject.SetActive(false);
            SetCardVisibility(false);
        }

        private void SetCardVisibility(bool visible)
        {
            if (subtitleText.transform.parent == null)
            {
                return;
            }

            var card = subtitleText.transform.parent.gameObject;
            if (card.name.EndsWith("Card"))
            {
                card.SetActive(visible);
            }
        }
    }
}
