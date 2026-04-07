using LocalAssistant.Core;
using LocalAssistant.Notifications;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace LocalAssistant.Tests.PlayMode
{
    public class ReminderOverlayControllerPlayModeTests
    {
        [Test]
        public void ReminderOverlayControllerQueuesAndClearsMessages()
        {
            var controller = new ReminderOverlayController();
            var text = new Label();
            var card = new VisualElement();

            controller.Bind(text, card);
            controller.Push(new ReminderDueEvent { title = "Hop team", minutes_until = 15 });
            controller.Push(new ReminderDueEvent { title = "Gui mail", minutes_until = 5 });

            StringAssert.Contains("Hop team", text.text);
            controller.Dismiss();
            StringAssert.Contains("Gui mail", text.text);

            controller.Clear();
            Assert.AreEqual(string.Empty, text.text);
            Assert.IsTrue(card.ClassListContains("hidden"));
        }
    }
}
