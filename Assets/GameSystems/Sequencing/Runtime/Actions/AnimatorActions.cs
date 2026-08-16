using System;
using GameSystems.Sequencing.Values;
using UnityEngine;

namespace GameSystems.Sequencing
{
    [Serializable]
    public sealed class SetAnimatorBoolAction : GameAction
    {
        [SerializeField] ComponentTarget<Animator> animator =
            new(new SelfGameObjectValue(), ComponentSearchScope.InChildren);
        [SerializeField] string parameter;
        [SerializeReference] BoolValue value = new ConstantBoolValue();
        public override string Summary => $"Set Animator {parameter} = {value?.Summary}";
        public override GameActionRuntime CreateRuntime() => new Runtime();
        sealed class Runtime : InstantActionRuntime
        {
            protected override void Execute()
            {
                SetAnimatorBoolAction data = (SetAnimatorBoolAction)Definition;
                Animator target = data.animator.Get(Context);
                if (target == null) { Fail("Missing Animator."); return; }
                target.SetBool(data.parameter, data.value?.Get(Context) ?? false);
            }
        }
    }

    [Serializable]
    public sealed class SetAnimatorFloatAction : GameAction
    {
        [SerializeField] ComponentTarget<Animator> animator =
            new(new SelfGameObjectValue(), ComponentSearchScope.InChildren);
        [SerializeField] string parameter;
        [SerializeReference] FloatValue value = new ConstantFloatValue();
        public override string Summary => $"Set Animator {parameter} = {value?.Summary}";
        public override GameActionRuntime CreateRuntime() => new Runtime();
        sealed class Runtime : InstantActionRuntime
        {
            protected override void Execute()
            {
                SetAnimatorFloatAction data = (SetAnimatorFloatAction)Definition;
                Animator target = data.animator.Get(Context);
                if (target == null) { Fail("Missing Animator."); return; }
                target.SetFloat(data.parameter, data.value?.Get(Context) ?? 0f);
            }
        }
    }

    [Serializable]
    public sealed class SetAnimatorTriggerAction : GameAction
    {
        [SerializeField] ComponentTarget<Animator> animator =
            new(new SelfGameObjectValue(), ComponentSearchScope.InChildren);
        [SerializeField] string parameter;
        public override string Summary => $"Set Animator trigger {parameter}";
        public override GameActionRuntime CreateRuntime() => new Runtime();
        sealed class Runtime : InstantActionRuntime
        {
            protected override void Execute()
            {
                SetAnimatorTriggerAction data = (SetAnimatorTriggerAction)Definition;
                Animator target = data.animator.Get(Context);
                if (target == null) { Fail("Missing Animator."); return; }
                target.SetTrigger(data.parameter);
            }
        }
    }
}
