using System.Collections;
using LocalAssistant.Avatar;
using LocalAssistant.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace LocalAssistant.Tests.PlayMode
{
    public class AvatarStateMachinePlayModeTests
    {
        [UnityTest]
        public IEnumerator AvatarStateChangesWhenSet()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var stateMachine = go.AddComponent<AvatarStateMachine>();
            stateMachine.BindPlaceholder(go.GetComponent<Renderer>());
            stateMachine.SetState(AvatarState.Listening);

            yield return null;

            Assert.AreEqual(AvatarState.Listening, stateMachine.CurrentState);
            Object.Destroy(go);
        }
    }
}
