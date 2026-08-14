using System;
using GameSystems.Hooks;
using GameSystems.Sequencing;
using UnityEngine;

namespace GameSystems.Stats.Conditions
{
    [Serializable]
    public sealed class TriggerContactHookCondition : GameCondition
    {
        [SerializeField] HookId hook;
        public TriggerContactHookCondition() { }
        public TriggerContactHookCondition(HookId value) => hook = value;
        public override string Summary => $"Trigger contact is {(hook != null ? hook.name : "missing hook")}";

        protected override bool OnEvaluate(in GameActionContext context)
        {
            if (hook == null || !context.TryGet(out Collider contact) || contact == null) return false;
            GameObject expected = HookRegistry.Get(hook);
            return expected != null && (contact.gameObject == expected ||
                                        contact.transform.IsChildOf(expected.transform));
        }
    }
}
