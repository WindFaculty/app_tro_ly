using System;

namespace LocalAssistant.World.Interaction
{
    public sealed class SelectedRoomObjectStore
    {
        private RoomObjectSelectionSnapshot current = RoomObjectSelectionSnapshot.None;

        public RoomObjectSelectionSnapshot Current => current;

        public event Action<RoomObjectSelectionSnapshot> Changed;

        public void Set(RoomObjectSelectionSnapshot snapshot)
        {
            current = snapshot ?? RoomObjectSelectionSnapshot.None;
            Changed?.Invoke(current);
        }
    }
}
