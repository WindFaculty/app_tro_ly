using System.Collections;
using LocalAssistant.Chat;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace LocalAssistant.Tests.PlayMode
{
    public class SubtitlePresenterPlayModeTests
    {
        [UnityTest]
        public IEnumerator SubtitlePresenterTogglesVisibility()
        {
            var go = new GameObject("SubtitlePresenter");
            var presenter = go.AddComponent<SubtitlePresenter>();
            var text = new Label();
            var card = new VisualElement();
            presenter.Bind(text, card);
            
            presenter.Show("Xin chao");
            yield return null;

            Assert.IsFalse(card.ClassListContains("hidden"));
            presenter.Hide();
            Assert.IsTrue(card.ClassListContains("hidden"));
            Object.Destroy(go);
        }

        [UnityTest]
        public IEnumerator SubtitlePresenterReplacesTextAndClearsOnHide()
        {
            var go = new GameObject("SubtitlePresenter");
            var presenter = go.AddComponent<SubtitlePresenter>();
            var text = new Label();
            var card = new VisualElement();
            presenter.Bind(text, card);
            
            presenter.Show("Ban dau");
            presenter.Show("Da cap nhat");
            yield return null;

            Assert.AreEqual("Da cap nhat", text.text);
            presenter.Hide();
            Assert.AreEqual(string.Empty, text.text);
            Assert.IsTrue(card.ClassListContains("hidden"));
            Object.Destroy(go);
        }
    }
}
