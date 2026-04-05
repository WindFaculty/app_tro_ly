using System;
using UnityEngine;

namespace LocalAssistant.World.Objects
{
    [Serializable]
    public sealed class RoomObjectPlacement
    {
        public string ObjectId = string.Empty;
        public string InstanceName = string.Empty;
        public string AnchorType = string.Empty;
        public Vector3 LocalPosition = Vector3.zero;
        public Vector3 LocalEulerAngles = Vector3.zero;
        public bool UseScaleOverride;
        public Vector3 ScaleOverride = Vector3.one;
    }
}
