using System;

namespace LocalAssistant.Runtime
{
    [Serializable]
    public sealed class UnityBridgeCommandPayload
    {
        public string page = string.Empty;
        public string mood = string.Empty;
        public string backend_state = string.Empty;
        public string animation_hint = string.Empty;
        public string emote = string.Empty;
        public string utterance_id = string.Empty;
        public string reason = string.Empty;
        public string focus = string.Empty;
        public string object_name = string.Empty;
        public string item_id = string.Empty;
        public string slot = string.Empty;
    }

    [Serializable]
    public sealed class UnityBridgeCommandEnvelope
    {
        public int protocol_version = 1;
        public string id = string.Empty;
        public string type = string.Empty;
        public string source = string.Empty;
        public string timestamp = string.Empty;
        public UnityBridgeCommandPayload payload = new UnityBridgeCommandPayload();
    }

    [Serializable]
    public sealed class UnityBridgeEventPayload
    {
        public string transport = string.Empty;
        public string url = string.Empty;
        public string message = string.Empty;
        public string detail = string.Empty;
        public string state = string.Empty;
        public string mood = string.Empty;
        public string animation_hint = string.Empty;
        public string animation = string.Empty;
        public string object_name = string.Empty;
        public string interaction = string.Empty;
    }

    [Serializable]
    public sealed class UnityBridgeEventEnvelope
    {
        public int protocol_version = 1;
        public string id = string.Empty;
        public string type = string.Empty;
        public string source = "unity";
        public string timestamp = string.Empty;
        public UnityBridgeEventPayload payload = new UnityBridgeEventPayload();
    }
}
