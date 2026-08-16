using GameSystems.Sequencing.Values;
using GameSystems.Sequencing;
using GameSystems.Stats;
using System;
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

    [Serializable]
        public sealed class ModifyAttributeAction : GameAction
        {
            [SerializeField] ComponentTarget<CharacterStats> target =
                new(new SelfGameObjectValue(), ComponentSearchScope.InParents);
            [SerializeField, Tooltip("Attribute changed on the character stats component.")] AttributeDefinition attribute;
            [SerializeField, Tooltip("Signed amount added to the current value.")] float delta = 1f;
            [SerializeReference] FloatValue deltaValue;
            public ModifyAttributeAction() { }
            public ModifyAttributeAction(AttributeDefinition attribute, float delta,
                ComponentTarget<CharacterStats> target = null)
            { this.attribute = attribute; this.delta = delta; if (target != null) this.target = target; }
            public override string Summary => $"Modify {(attribute != null ? attribute.DisplayName : "missing attribute")} by {deltaValue?.Summary ?? delta.ToString("+0.##;-0.##;0")}";
            public override GameActionRuntime CreateRuntime() => new Runtime();
            sealed class Runtime : InstantActionRuntime
            {
                protected override void Execute()
                {
                    ModifyAttributeAction data = (ModifyAttributeAction)Definition;
                    CharacterStats stats = data.target.Get(Context);
                    if (stats == null && Context.TryGet(out CharacterRuntimeContext character))
                        stats = character.Resolve<CharacterStats>();
                    if (stats == null || !stats.Change(data.attribute,
                            data.deltaValue?.Get(Context) ?? data.delta))
                        Fail("Attribute is unavailable.");
                }
            }
        }

    [Serializable]
        public sealed class RestoreAttributeAction : GameAction
        {
            [SerializeField, Tooltip("Attribute restored to its current calculated maximum.")] AttributeDefinition attribute;
            public override string Summary => $"Restore {(attribute != null ? attribute.DisplayName : "missing attribute")} to maximum";
            public override GameActionRuntime CreateRuntime() => new Runtime();
            sealed class Runtime : InstantActionRuntime
            {
                protected override void Execute()
                {
                    RestoreAttributeAction data = (RestoreAttributeAction)Definition;
                    CharacterStats stats = Context.Get<CharacterRuntimeContext>().Resolve<CharacterStats>();
                    RuntimeAttribute runtime = stats?.GetAttribute(data.attribute);
                    if (runtime == null || !stats.Set(data.attribute, runtime.Maximum)) Fail("Attribute is unavailable.");
                }
            }
        }

    [Serializable]
        public sealed class SetAttributeAction : GameAction
        {
            [SerializeField, Tooltip("Attribute set on the character stats component.")] AttributeDefinition attribute;
            [SerializeField, Tooltip("New current value, clamped by the attribute range.")] float value;
            public override string Summary => $"Set {(attribute != null ? attribute.DisplayName : "missing attribute")} = {value:0.##}";
            public override GameActionRuntime CreateRuntime() => new Runtime();
            sealed class Runtime : InstantActionRuntime
            {
                protected override void Execute()
                {
                    SetAttributeAction data = (SetAttributeAction)Definition;
                    CharacterStats stats = Context.Get<CharacterRuntimeContext>().Resolve<CharacterStats>();
                    if (stats == null || !stats.Set(data.attribute, data.value)) Fail("Attribute is unavailable.");
                }
            }
        }
}
