using System;
using UnityEngine;

namespace LocalAssistant.Core
{
    public static class UnityJson
    {
        public static string Serialize<T>(T payload)
        {
            if (payload == null)
            {
                throw new ArgumentNullException(nameof(payload));
            }

            return JsonUtility.ToJson(payload);
        }

        public static T Deserialize<T>(string json) where T : class
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new InvalidOperationException("JSON payload is empty.");
            }

            var result = JsonUtility.FromJson<T>(json);
            if (result == null)
            {
                throw new InvalidOperationException("JSON payload deserialized to null.");
            }

            return result;
        }
    }
}
