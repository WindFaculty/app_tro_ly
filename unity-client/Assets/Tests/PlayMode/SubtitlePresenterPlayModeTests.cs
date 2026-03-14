using System.Collections;
using LocalAssistant.Chat;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace LocalAssistant.Tests.PlayMode
{
    public class SubtitlePresenterPlayModeTests
    {
        [UnityTest]
        public IEnumerator SubtitlePresenterTogglesVisibility()
        {
            var go = new GameObject("SubtitlePresenter");
            var textGo = new GameObject("SubtitleText", typeof(Text));
            textGo.transform.SetParent(go.transform);
            var text = textGo.GetComponent<Text>();
            var presenter = go.AddComponent<SubtitlePresenter>();
            presenter.Bind(text);
            presenter.Show("Xin chao");

            yield return null;

            Assert.IsTrue(text.gameObject.activeSelf);
            presenter.Hide();
            Assert.IsFalse(text.gameObject.activeSelf);
            Object.Destroy(go);
        }

        [UnityTest]
        public IEnumerator SubtitlePresenterReplacesTextAndClearsOnHide()
        {
            var go = new GameObject("SubtitlePresenter");
            var textGo = new GameObject("SubtitleText", typeof(Text));
            textGo.transform.SetParent(go.transform);
            var text = textGo.GetComponent<Text>();
            var presenter = go.AddComponent<SubtitlePresenter>();
            presenter.Bind(text);
            presenter.Show("Ban dau");
            presenter.Show("Da cap nhat");

            yield return null;

            Assert.AreEqual("Da cap nhat", text.text);
            presenter.Hide();
            Assert.AreEqual(string.Empty, text.text);
            Assert.IsFalse(text.gameObject.activeSelf);
            Object.Destroy(go);
        }
    }
}
