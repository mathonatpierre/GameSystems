using System;
using UnityEngine;

namespace GameSystems.Sequencing
{
    [Serializable]
    public sealed class SetGameObjectActiveAction : GameAction
    {
        [SerializeField, Tooltip("Optional explicit target. Uses the context owner when empty.")] GameObject target;
        [SerializeField, Tooltip("Desired active state.")] bool active = true;
        public SetGameObjectActiveAction() { }
        public SetGameObjectActiveAction(bool value) => active = value;
        public override string Summary => $"Set {(target != null ? target.name : "owner")} active = {active.ToString().ToLowerInvariant()}";
        public override GameActionRuntime CreateRuntime() => new Runtime();

        sealed class Runtime : InstantActionRuntime
        {
            protected override void Execute()
            {
                SetGameObjectActiveAction data = (SetGameObjectActiveAction)Definition;
                GameObject target = data.target != null ? data.target : GameActionContextUtility.OwnerGameObject(Context);
                if (target == null) { Fail("Missing GameObject target."); return; }
                target.SetActive(data.active);
            }
        }
    }
}
