using System.Collections;
using LocalAssistant.Core;
using LocalAssistant.Notifications;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace LocalAssistant.Tests.PlayMode
{
    public class ReminderPresenterPlayModeTests
    {
        [UnityTest]
        public IEnumerator ReminderPresenterShowsIncomingReminder()
        {
            var go = new GameObject("ReminderPresenter");
            var textGo = new GameObject("ReminderText", typeof(Text));
            textGo.transform.SetParent(go.transform);
            var text = textGo.GetComponent<Text>();
            var presenter = go.AddComponent<ReminderPresenter>();
            presenter.Bind(text);
            presenter.Push(new ReminderDueEvent { title = "Hop team", minutes_until = 15 });

            yield return null;

            StringAssert.Contains("Hop team", text.text);
            Object.Destroy(go);
        }

        [UnityTest]
        public IEnumerator ReminderPresenterDismissesQueuedRemindersAndCanClear()
        {
            var go = new GameObject("ReminderPresenter");
            var textGo = new GameObject("ReminderText", typeof(Text));
            textGo.transform.SetParent(go.transform);
            var text = textGo.GetComponent<Text>();
            var presenter = go.AddComponent<ReminderPresenter>();
            presenter.Bind(text);
            presenter.Push(new ReminderDueEvent { title = "Hop team", minutes_until = 15 });
            presenter.Push(new ReminderDueEvent { title = "Gui mail", minutes_until = 5 });

            yield return null;

            StringAssert.Contains("Hop team", text.text);
            presenter.Dismiss();
            StringAssert.Contains("Gui mail", text.text);

            presenter.Clear();
            Assert.IsFalse(text.gameObject.activeSelf);
            Assert.AreEqual(string.Empty, text.text);
            Object.Destroy(go);
        }
    }
}
