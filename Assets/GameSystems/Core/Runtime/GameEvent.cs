using System;
using UnityEngine;

namespace GameSystems.Core
{
    [CreateAssetMenu(menuName = "Game Systems/Core/Game Event", fileName = "EVENT_")]
    public sealed class GameEvent : ScriptableObject
    {
        public event Action<UnityEngine.Object> Raised;

        public void Raise(UnityEngine.Object sender = null) => Raised?.Invoke(sender);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetAll()
        {
            GameEvent[] events = Resources.FindObjectsOfTypeAll<GameEvent>();
            for (int i = 0; i < events.Length; i++) events[i].Raised = null;
        }
    }
}
