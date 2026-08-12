using System;
using GameSystems.Actions;
using UnityEngine;

namespace GameSystems.Abilities.Actions
{
    [Serializable]
    public sealed class ReplaceCharacterAction : GameAction
    {
        [SerializeField, Tooltip("Character prefab instantiated in place of the sequence owner.")]
        GameObject prefab;
        [SerializeField] bool inheritPatrolArea = true;

        public override string Summary => $"Replace character with {(prefab != null ? prefab.name : "missing prefab")}";
        public override GameActionRuntime CreateRuntime() => new Runtime();

        sealed class Runtime : InstantActionRuntime
        {
            protected override void Execute()
            {
                ReplaceCharacterAction data = (ReplaceCharacterAction)Definition;
                GameObject owner = GameActionContextUtility.OwnerGameObject(Context);
                if (owner == null || data.prefab == null)
                {
                    Fail("Replace Character requires an owner and a prefab.");
                    return;
                }

                Transform source = owner.transform;
                GameObject replacement = UnityEngine.Object.Instantiate(data.prefab,
                    source.position, source.rotation, source.parent);
                replacement.name = data.prefab.name;
                if (data.inheritPatrolArea &&
                    owner.GetComponent(typeof(ICharacterPatrolArea)) is ICharacterPatrolArea area &&
                    replacement.GetComponent(typeof(ICharacterPatrolAreaReceiver)) is ICharacterPatrolAreaReceiver receiver)
                    receiver.ConfigurePatrolArea(area.MinimumX, area.MaximumX, area.Direction,
                        area.ReferenceFrame);
                UnityEngine.Object.Destroy(owner);
            }
        }
    }
}
