using System;
using GameSystems.Sequencing.Values;
using UnityEngine;

namespace GameSystems.Sequencing
{
    [Serializable]
    public sealed class SetGameObjectVariableAction : GameAction
    {
        [SerializeField] string key;
        [SerializeReference] GameObjectValue value = new TargetGameObjectValue();
        public override string Summary => $"Set {key} = {value?.Summary ?? "None"}";
        public override GameActionRuntime CreateRuntime() => new Runtime();
        sealed class Runtime : InstantActionRuntime
        {
            protected override void Execute()
            {
                SetGameObjectVariableAction data = (SetGameObjectVariableAction)Definition;
                if (!Context.TryGet(out GameActionBlackboard variables))
                { Fail("Missing action blackboard."); return; }
                variables.Set(data.key, data.value?.Get(Context));
            }
        }
    }

    [Serializable]
    public sealed class SetFloatVariableAction : GameAction
    {
        [SerializeField] string key;
        [SerializeReference] FloatValue value = new ConstantFloatValue();
        public override string Summary => $"Set {key} = {value?.Summary ?? "0"}";
        public override GameActionRuntime CreateRuntime() => new Runtime();
        sealed class Runtime : InstantActionRuntime
        {
            protected override void Execute()
            {
                SetFloatVariableAction data = (SetFloatVariableAction)Definition;
                if (!Context.TryGet(out GameActionBlackboard variables))
                { Fail("Missing action blackboard."); return; }
                variables.Set(data.key, data.value?.Get(Context) ?? 0f);
            }
        }
    }
}
