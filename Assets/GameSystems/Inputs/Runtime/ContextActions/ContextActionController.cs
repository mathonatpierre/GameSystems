using System.Collections.Generic;
using GameSystems.Characters;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GameSystems.Inputs
{
    [DefaultExecutionOrder(-500)]
    [DisallowMultipleComponent]
    public sealed class ContextActionController : MonoBehaviour, ICharacterInputGate
    {
        [SerializeField, Tooltip("Button used to execute the best available contextual action.")]
        InputActionReference action;

        readonly List<ContextActionTrigger> available = new();
        int consumedFrame = -1;

        public bool BlocksCharacterInput => consumedFrame == Time.frameCount;
        public ContextActionTrigger Current { get; private set; }

        public void Configure(InputActionReference value) => action = value;

        internal void Register(ContextActionTrigger trigger)
        {
            if (trigger != null && !available.Contains(trigger)) available.Add(trigger);
        }

        internal void Unregister(ContextActionTrigger trigger)
        {
            available.Remove(trigger);
            if (Current == trigger) Current = null;
        }

        void OnEnable() => action?.action.Enable();
        void OnDisable()
        {
            action?.action.Disable();
            available.Clear();
            Current = null;
        }

        void Update()
        {
            Current = SelectBest();
            if (Current == null || action?.action.WasPressedThisFrame() != true) return;
            if (!Current.TryExecute(this)) return;
            consumedFrame = Time.frameCount;
            GetComponent<PlayerAbilityInputSource>()?.Consume(action.action);
        }

        ContextActionTrigger SelectBest()
        {
            ContextActionTrigger best = null;
            float bestDistance = float.PositiveInfinity;
            for (int i = available.Count - 1; i >= 0; i--)
            {
                ContextActionTrigger candidate = available[i];
                if (candidate == null)
                {
                    available.RemoveAt(i);
                    continue;
                }
                if (!candidate.CanExecute(this)) continue;
                float distance = (candidate.transform.position - transform.position).sqrMagnitude;
                if (best != null && (candidate.Priority < best.Priority ||
                    candidate.Priority == best.Priority && distance >= bestDistance)) continue;
                best = candidate;
                bestDistance = distance;
            }
            return best;
        }
    }
}
