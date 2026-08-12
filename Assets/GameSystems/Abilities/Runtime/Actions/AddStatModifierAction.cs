using System;
using GameSystems.Sequencing;
using GameSystems.Stats;
using UnityEngine;

namespace GameSystems.Abilities.Actions
{
    [Serializable]
    public sealed class AddStatModifierAction : GameAction
    {
        [SerializeField, Tooltip("Stat receiving the modifier.")] StatDefinition stat;
        [SerializeField, Tooltip("Constant or percentage modifier mode.")] StatModifierMode mode;
        [SerializeField, Tooltip("Modifier value interpreted according to Mode.")] float value;
        [SerializeField, Min(0f), Tooltip("Duration in seconds. Zero keeps the modifier until explicitly removed.")] float duration;
        [SerializeField, Tooltip("Optional tag used to identify this modifier.")] string tag;
        public override string Summary => $"Add {value:+0.##;-0.##;0} {mode} to {(stat != null ? stat.DisplayName : "missing stat")} for {(duration > 0f ? $"{duration:0.##}s" : "infinite")}";
        public override GameActionRuntime CreateRuntime() => new Runtime();
        sealed class Runtime : InstantActionRuntime
        {
            protected override void Execute()
            {
                AddStatModifierAction data = (AddStatModifierAction)Definition;
                CharacterStats stats = Context.Get<CharacterRuntimeContext>().Resolve<CharacterStats>();
                if (stats?.GetStat(data.stat) == null) { Fail("Stat is unavailable."); return; }
                var modifier = new StatModifier(data, data.mode, data.value, data.tag);
                if (data.duration > 0f) stats.AddModifier(data.stat, modifier, data.duration);
                else stats.AddModifier(data.stat, modifier);
            }
        }
    }
}
