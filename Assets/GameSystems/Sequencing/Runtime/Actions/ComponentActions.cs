using GameSystems.Sequencing.Values;
using System.Reflection;
using System;
using UnityEngine;

namespace GameSystems.Sequencing
{
    [Serializable]
        public sealed class CallComponentMethodAction : GameAction
        {
            [SerializeField] Component target;
            [SerializeField] string methodName;
            [SerializeField] bool failOnMissing = true;
            [SerializeField] bool failOnFalseReturn = true;
    
            public override string Summary =>
                $"Call {(target != null ? target.GetType().Name : "missing component")}.{methodName}()";
    
            public override GameActionRuntime CreateRuntime() => new Runtime();
    
            sealed class Runtime : InstantActionRuntime
            {
                protected override void Execute()
                {
                    CallComponentMethodAction data = (CallComponentMethodAction)Definition;
                    if (data.target == null || string.IsNullOrWhiteSpace(data.methodName))
                    {
                        if (data.failOnMissing) Fail("Missing component target or method name.");
                        return;
                    }
                    MethodInfo method = data.target.GetType().GetMethod(data.methodName,
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                        null, Type.EmptyTypes, null);
                    if (method == null)
                    {
                        if (data.failOnMissing) Fail($"Method '{data.methodName}' was not found.");
                        return;
                    }
                    object result = method.Invoke(data.target, null);
                    if (data.failOnFalseReturn && result is bool accepted && !accepted)
                        Fail($"Method '{data.methodName}' returned false.");
                }
            }
        }

    [Serializable]
        public sealed class SetBehaviourEnabledAction : GameAction
        {
            [SerializeField, Tooltip("Behaviour whose enabled state is changed.")] Behaviour target;
            [SerializeField] ComponentTarget<Behaviour> binding =
                new(new SelfGameObjectValue(), ComponentSearchScope.OnObject);
            [SerializeField, Tooltip("Desired enabled state.")] bool enabled = true;
            public override string Summary => $"Set {(target != null ? target.GetType().Name : "missing behaviour")} enabled = {enabled.ToString().ToLowerInvariant()}";
            public override GameActionRuntime CreateRuntime() => new Runtime();
            sealed class Runtime : InstantActionRuntime
            {
                protected override void Execute()
                {
                    SetBehaviourEnabledAction data = (SetBehaviourEnabledAction)Definition;
                    Behaviour target = data.target != null ? data.target : data.binding.Get(Context);
                    if (target == null) { Fail("Missing Behaviour target."); return; }
                    target.enabled = data.enabled;
                }
            }
        }

    [Serializable]
        public sealed class SetColliderEnabledAction : GameAction
        {
            [SerializeField, Tooltip("Collider whose enabled state is changed.")] Collider target;
            [SerializeField] ComponentTarget<Collider> binding =
                new(new SelfGameObjectValue(), ComponentSearchScope.OnObject);
            [SerializeField, Tooltip("Desired enabled state.")] bool enabled = true;
    
            public SetColliderEnabledAction() { }
            public SetColliderEnabledAction(bool value) => enabled = value;
    
            public override string Summary =>
                $"Set {(target != null ? target.GetType().Name : "missing collider")} enabled = {enabled.ToString().ToLowerInvariant()}";
    
            public override GameActionRuntime CreateRuntime() => new Runtime();
    
            sealed class Runtime : InstantActionRuntime
            {
                protected override void Execute()
                {
                    SetColliderEnabledAction data = (SetColliderEnabledAction)Definition;
                    Collider target = data.target != null ? data.target : data.binding.Get(Context);
                    if (target == null) { Fail("Missing Collider target."); return; }
                    target.enabled = data.enabled;
                }
            }
        }

    [Serializable]
        public sealed class SetTrailEmissionAction : GameAction
        {
            [SerializeField, Tooltip("Optional root. Uses the context owner when empty.")]
            GameObject root;
            [SerializeReference] GameObjectValue binding = new SelfGameObjectValue();
            [SerializeField] bool emitting = true;
            [SerializeField] bool includeInactive = true;
    
            public override string Summary =>
                $"Set trails emitting = {emitting.ToString().ToLowerInvariant()}";
    
            public override GameActionRuntime CreateRuntime() => new Runtime();
    
            sealed class Runtime : InstantActionRuntime
            {
                protected override void Execute()
                {
                    SetTrailEmissionAction data = (SetTrailEmissionAction)Definition;
                    GameObject root = data.root != null ? data.root : data.binding?.Get(Context);
                    if (root == null)
                    {
                        Fail("Missing trail root.");
                        return;
                    }
                    TrailRenderer[] trails = root.GetComponentsInChildren<TrailRenderer>(
                        data.includeInactive);
                    for (int i = 0; i < trails.Length; i++)
                        if (trails[i] != null) trails[i].emitting = data.emitting;
                }
            }
        }
}
