using System;
using System.Collections.Generic;

namespace LocalAssistant.Core
{
    public sealed class AssistantEventBus : IAssistantEventBus
    {
        private readonly Dictionary<Type, List<Delegate>> handlersByType = new();

        public IDisposable Subscribe<TEvent>(Action<TEvent> handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            var eventType = typeof(TEvent);
            if (!handlersByType.TryGetValue(eventType, out var handlers))
            {
                handlers = new List<Delegate>();
                handlersByType[eventType] = handlers;
            }

            handlers.Add(handler);
            return new Subscription(() => Unsubscribe(eventType, handler));
        }

        public void Publish<TEvent>(TEvent eventPayload)
        {
            if (!handlersByType.TryGetValue(typeof(TEvent), out var handlers) || handlers.Count == 0)
            {
                return;
            }

            var snapshot = handlers.ToArray();
            foreach (var handler in snapshot)
            {
                if (handler is Action<TEvent> typedHandler)
                {
                    typedHandler(eventPayload);
                }
            }
        }

        private void Unsubscribe(Type eventType, Delegate handler)
        {
            if (!handlersByType.TryGetValue(eventType, out var handlers))
            {
                return;
            }

            handlers.Remove(handler);
            if (handlers.Count == 0)
            {
                handlersByType.Remove(eventType);
            }
        }

        private sealed class Subscription : IDisposable
        {
            private Action disposeAction;

            public Subscription(Action disposeAction)
            {
                this.disposeAction = disposeAction;
            }

            public void Dispose()
            {
                disposeAction?.Invoke();
                disposeAction = null;
            }
        }
    }
}
