using GameSystems.Sequencing.Values;
using System;
using UnityEngine;

namespace GameSystems.Sequencing
{
    [Serializable]
        public sealed class DestroyObjectAction : GameAction
        {
            [SerializeField, Tooltip("Explicit object destroyed by this action.")] UnityEngine.Object target;
            [SerializeReference] GameObjectValue binding = new SelfGameObjectValue();
            [SerializeField, Min(0f), Tooltip("Delay passed to Unity Object.Destroy.")] float delay;
            public override string Summary => $"Destroy {(target != null ? target.name : "missing object")} after {delay:0.###}s";
            public override GameActionRuntime CreateRuntime() => new Runtime();
            sealed class Runtime : InstantActionRuntime
            {
                protected override void Execute()
                {
                    DestroyObjectAction data = (DestroyObjectAction)Definition;
                    UnityEngine.Object target = data.target != null ? data.target : data.binding?.Get(Context);
                    if (target == null) { Fail("Missing object to destroy."); return; }
                    UnityEngine.Object.Destroy(target, data.delay);
                }
            }
        }

    [Serializable]
        public sealed class InstantiateGameObjectAction : GameAction
        {
            [SerializeField] GameObject prefab;
            [SerializeReference] Vector3Value position = new TransformPositionValue();
            [SerializeReference] QuaternionValue rotation = new TransformRotationValue();
            [SerializeReference] GameObjectValue parent;
            [SerializeField] string storeResultAs = "Spawned";
            public override string Summary => $"Instantiate {prefab?.name ?? "missing prefab"}";
            public override GameActionRuntime CreateRuntime() => new Runtime();
            sealed class Runtime : InstantActionRuntime
            {
                protected override void Execute()
                {
                    InstantiateGameObjectAction data = (InstantiateGameObjectAction)Definition;
                    if (data.prefab == null) { Fail("Missing prefab."); return; }
                    GameObject instance = UnityEngine.Object.Instantiate(data.prefab,
                        data.position?.Get(Context) ?? Vector3.zero,
                        data.rotation?.Get(Context) ?? Quaternion.identity,
                        data.parent?.Get(Context)?.transform);
                    if (Context.TryGet(out GameActionBlackboard variables))
                        variables.Set(data.storeResultAs, instance);
                }
            }
        }

    [Serializable]
        public sealed class SetGameObjectActiveAction : GameAction
        {
            [SerializeField, Tooltip("Optional explicit target. Uses the context owner when empty.")] GameObject target;
            [SerializeReference] GameObjectValue binding = new SelfGameObjectValue();
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
                    GameObject target = data.target != null ? data.target : data.binding?.Get(Context);
                    if (target == null) { Fail("Missing GameObject target."); return; }
                    target.SetActive(data.active);
                }
            }
        }
}
