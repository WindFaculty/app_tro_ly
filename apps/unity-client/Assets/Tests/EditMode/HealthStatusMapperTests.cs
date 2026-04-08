using LocalAssistant.Core;
using NUnit.Framework;
using UnityEngine;

namespace LocalAssistant.Tests.EditMode
{
    public class HealthStatusMapperTests
    {
        [Test]
        public void ReadyStatusMapsToGreenLabel()
        {
            Assert.AreEqual("Ready", HealthStatusMapper.ToLabel("ready"));
            Assert.AreEqual(new Color(0.21f, 0.65f, 0.38f), HealthStatusMapper.ToColor("ready"));
        }

        [Test]
        public void UnknownStatusFallsBackToError()
        {
            Assert.AreEqual("Error", HealthStatusMapper.ToLabel("unknown"));
        }
    }
}
