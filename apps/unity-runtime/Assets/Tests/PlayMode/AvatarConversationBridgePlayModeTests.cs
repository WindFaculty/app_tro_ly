using System.Collections;
using System.Collections.Generic;
using AvatarSystem;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace LocalAssistant.Tests.PlayMode
{
    public class AvatarConversationBridgePlayModeTests
    {
        [UnityTest]
        public IEnumerator AvatarConversationBridgeTransitionsThroughConversationFlow()
        {
            var go = new GameObject("AvatarConversationBridgeTest");
            var bridge = go.AddComponent<AvatarConversationBridge>();
            var observedStates = new List<ConversationState>();
            bridge.StateChanged += observedStates.Add;

            Assert.AreEqual(ConversationState.Idle, bridge.CurrentState);

            bridge.OnListeningStart();
            yield return null;
            Assert.AreEqual(ConversationState.Listening, bridge.CurrentState);

            bridge.OnListeningEnd();
            yield return null;
            Assert.AreEqual(ConversationState.Thinking, bridge.CurrentState);

            bridge.OnSpeakingStart();
            yield return null;
            Assert.AreEqual(ConversationState.Speaking, bridge.CurrentState);

            bridge.OnReacting();
            yield return null;
            Assert.AreEqual(ConversationState.Reacting, bridge.CurrentState);

            bridge.OnIdle();
            yield return null;
            Assert.AreEqual(ConversationState.Idle, bridge.CurrentState);

            CollectionAssert.AreEqual(
                new[]
                {
                    ConversationState.Listening,
                    ConversationState.Thinking,
                    ConversationState.Speaking,
                    ConversationState.Reacting,
                    ConversationState.Idle,
                },
                observedStates);

            Object.Destroy(go);
        }
    }
}
