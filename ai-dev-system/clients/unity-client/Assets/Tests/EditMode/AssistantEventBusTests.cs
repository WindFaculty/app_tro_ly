using LocalAssistant.App;
using LocalAssistant.Core;
using NUnit.Framework;

namespace LocalAssistant.Tests.EditMode
{
    public class AssistantEventBusTests
    {
        [Test]
        public void PublishDeliversEventsToTypedSubscribers()
        {
            var bus = new AssistantEventBus();
            AppScreen observedScreen = AppScreen.Week;

            bus.Subscribe<PlannerScreenRequestedEvent>(evt => observedScreen = evt.Screen);

            bus.Publish(new PlannerScreenRequestedEvent(AppScreen.Completed));

            Assert.AreEqual(AppScreen.Completed, observedScreen);
        }

        [Test]
        public void DisposedSubscriptionStopsReceivingEvents()
        {
            var bus = new AssistantEventBus();
            var observedCount = 0;
            var subscription = bus.Subscribe<PlannerDateChangedEvent>(_ => observedCount++);

            subscription.Dispose();
            bus.Publish(new PlannerDateChangedEvent("2026-04-05"));

            Assert.AreEqual(0, observedCount);
        }
    }
}
