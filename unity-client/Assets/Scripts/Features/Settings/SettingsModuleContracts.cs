using System;
using LocalAssistant.Core;
using UnityEngine;

namespace LocalAssistant.Features.Settings
{
    public interface ISettingsModule : ISettingsStateSource
    {
        event Action ReloadRequested;
        event Action SaveRequested;
        event Action SettingsChanged;

        bool HasUnsavedChanges { get; }

        void Bind();
        SettingsPayload Snapshot();
        void Apply(SettingsPayload payload);
        void SetStatus(string message, Color color);
        void SetEditable(bool isEditable);
    }
}
