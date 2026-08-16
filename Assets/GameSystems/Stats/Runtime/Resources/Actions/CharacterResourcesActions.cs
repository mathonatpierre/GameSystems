using System;
using GameSystems.Sequencing;
using GameSystems.Sequencing.Values;
using UnityEngine;

namespace GameSystems.Stats.Actions
{
    [Serializable]
    public sealed class AddResourceAction : GameAction
    {
        [SerializeField] ComponentTarget<CharacterResources> target =
            new(new TargetGameObjectValue(), ComponentSearchScope.InParents);
        [SerializeField] ResourceDefinition resource;
        [SerializeField] int amount = 1;
        public AddResourceAction() { }
        public AddResourceAction(ResourceDefinition value, int quantity,
            ComponentTarget<CharacterResources> target = null)
        { resource = value; amount = Mathf.Max(1, quantity); if (target != null) this.target = target; }
        public override string Summary => $"Add {amount} {(resource != null ? resource.DisplayName : "resource")} to {target?.Summary}";
        public override GameActionRuntime CreateRuntime() => new Runtime();

        sealed class Runtime : InstantActionRuntime
        {
            protected override void Execute()
            {
                AddResourceAction data = (AddResourceAction)Definition;
                CharacterResources resources = data.target.Get(Context);
                if (resources == null || resources.Add(data.resource, data.amount) <= 0)
                    Fail("Target cannot receive this resource.");
            }
        }
    }

    [Serializable]
    public sealed class SetResourceAction : GameAction
    {
        [SerializeField] ComponentTarget<CharacterResources> target =
            new(new SelfGameObjectValue(), ComponentSearchScope.InParents);
        [SerializeField] ResourceDefinition resource;
        [SerializeField, Min(0)] int amount;

        public SetResourceAction() { }
        public SetResourceAction(ResourceDefinition value, int amount = 0,
            ComponentTarget<CharacterResources> target = null)
        { resource = value; this.amount = Mathf.Max(0, amount); if (target != null) this.target = target; }
        public override string Summary => $"Set {(resource != null ? resource.DisplayName : "resource")} = {amount} on {target?.Summary}";
        public override GameActionRuntime CreateRuntime() => new Runtime();

        sealed class Runtime : InstantActionRuntime
        {
            protected override void Execute()
            {
                SetResourceAction data = (SetResourceAction)Definition;
                CharacterResources resources = data.target.Get(Context);
                if (resources == null || data.resource == null)
                { Fail("Missing character resources or resource definition."); return; }
                resources.Set(data.resource, data.amount);
            }
        }
    }
}
