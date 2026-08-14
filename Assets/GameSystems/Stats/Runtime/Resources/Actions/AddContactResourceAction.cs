using System;
using GameSystems.Sequencing;
using UnityEngine;

namespace GameSystems.Stats.Actions
{
    [Serializable]
    public sealed class AddContactResourceAction : GameAction
    {
        [SerializeField] ResourceDefinition resource;
        [SerializeField, Min(1)] int amount = 1;
        public AddContactResourceAction() { }
        public AddContactResourceAction(ResourceDefinition value, int quantity)
        { resource = value; amount = Mathf.Max(1, quantity); }
        public override string Summary => $"Add {amount} {(resource != null ? resource.DisplayName : "resource")} to trigger contact";
        public override GameActionRuntime CreateRuntime() => new Runtime();

        sealed class Runtime : InstantActionRuntime
        {
            protected override void Execute()
            {
                AddContactResourceAction data = (AddContactResourceAction)Definition;
                if (!Context.TryGet(out Collider contact) || contact == null)
                { Fail("Missing trigger contact."); return; }
                CharacterResources resources = contact.GetComponentInParent<CharacterResources>();
                if (resources == null || resources.Add(data.resource, data.amount) <= 0)
                    Fail("Contact cannot receive this resource.");
            }
        }
    }
}
