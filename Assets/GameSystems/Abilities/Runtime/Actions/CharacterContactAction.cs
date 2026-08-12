using System;
using GameSystems.Actions;
using GameSystems.Stats;
using UnityEngine;

namespace GameSystems.Abilities.Actions
{
    public enum CharacterContactTarget { Self, Other }

    [Serializable]
    public sealed class ModifyContactAttributeAction : GameAction
    {
        [SerializeField] CharacterContactTarget target = CharacterContactTarget.Other;
        [SerializeField] AttributeDefinition attribute;
        [SerializeField] float delta = -1f;
        public ModifyContactAttributeAction() { }
        public ModifyContactAttributeAction(AttributeDefinition value, float amount,
            CharacterContactTarget contactTarget = CharacterContactTarget.Other)
        { attribute = value; delta = amount; target = contactTarget; }
        public override string Summary => $"Modify {target} {(attribute != null ? attribute.DisplayName : "attribute")} by {delta:+0.##;-0.##;0}";
        public override GameActionRuntime CreateRuntime() => new Runtime();
        sealed class Runtime : InstantActionRuntime
        {
            protected override void Execute()
            {
                ModifyContactAttributeAction data = (ModifyContactAttributeAction)Definition;
                CharacterContactContext contact = Context.Get<CharacterContactContext>();
                CharacterAbilityController target = data.target == CharacterContactTarget.Self ? contact.Self : contact.Other;
                CharacterStats stats = target != null ? target.GetComponent<CharacterStats>() : null;
                if (stats == null || !stats.Change(data.attribute, data.delta))
                    Fail("Contact target attribute is unavailable.");
            }
        }
    }

    [Serializable]
    public sealed class KnockbackContactCharacterAction : GameAction
    {
        [SerializeField] CharacterContactTarget target = CharacterContactTarget.Other;
        [SerializeField] Vector2 velocity = new(4.8f, 3.15f);
        public KnockbackContactCharacterAction() { }
        public KnockbackContactCharacterAction(Vector2 value,
            CharacterContactTarget contactTarget = CharacterContactTarget.Other)
        { velocity = value; target = contactTarget; }
        public override string Summary => $"Knockback {target} by {velocity.x:0.##} / {velocity.y:0.##}";
        public override GameActionRuntime CreateRuntime() => new Runtime();
        sealed class Runtime : InstantActionRuntime
        {
            protected override void Execute()
            {
                KnockbackContactCharacterAction data = (KnockbackContactCharacterAction)Definition;
                CharacterContactContext contact = Context.Get<CharacterContactContext>();
                CharacterAbilityController target = data.target == CharacterContactTarget.Self ? contact.Self : contact.Other;
                CharacterAbilityController source = data.target == CharacterContactTarget.Self ? contact.Other : contact.Self;
                if (target?.Motor is not ICharacterMotorControl motor) { Fail("Contact target has no controllable motor."); return; }
                GameObject owner = GameActionContextUtility.OwnerGameObject(Context);
                float sourceX = source != null ? source.transform.position.x :
                    owner != null ? owner.transform.position.x : target.transform.position.x;
                float direction = target.transform.position.x >= sourceX ? 1f : -1f;
                motor.SetVelocity(new Vector3(direction * data.velocity.x, data.velocity.y, 0f));
            }
        }
    }

    [Serializable]
    public sealed class RequestContactReactionAction : GameAction
    {
        [SerializeField] CharacterContactTarget target = CharacterContactTarget.Self;
        [SerializeField] ReactionId reactionId = ReactionId.Custom;
        [SerializeField] string customReactionId;
        public RequestContactReactionAction() { }
        public RequestContactReactionAction(ReactionId id,
            CharacterContactTarget contactTarget = CharacterContactTarget.Other)
        { reactionId = id; target = contactTarget; }
        public override string Summary => $"Request {(reactionId == ReactionId.Custom ? customReactionId : reactionId.ToString())} on {target}";
        public override GameActionRuntime CreateRuntime() => new Runtime();
        sealed class Runtime : InstantActionRuntime
        {
            protected override void Execute()
            {
                RequestContactReactionAction data = (RequestContactReactionAction)Definition;
                CharacterContactContext contact = Context.Get<CharacterContactContext>();
                CharacterAbilityController target = data.target == CharacterContactTarget.Self ? contact.Self : contact.Other;
                bool accepted = target != null && (data.reactionId == ReactionId.Custom
                    ? target.RequestReaction(data.customReactionId, 1f, contact.Self)
                    : target.RequestReaction(data.reactionId, 1f, contact.Self));
                if (!accepted)
                    Fail("Contact reaction was rejected.");
            }
        }
    }

    [Serializable]
    public sealed class RequestContactAbilityAction : GameAction
    {
        [SerializeField] CharacterContactTarget target = CharacterContactTarget.Other;
        [SerializeField] AbilityDefinition ability;
        [SerializeField] float value = 1f;
        public override string Summary => $"Request {(ability != null ? ability.name : "missing ability")} on contact {target}";
        public override GameActionRuntime CreateRuntime() => new Runtime();
        sealed class Runtime : InstantActionRuntime
        {
            protected override void Execute()
            {
                RequestContactAbilityAction data = (RequestContactAbilityAction)Definition;
                CharacterContactContext contact = Context.Get<CharacterContactContext>();
                CharacterAbilityController target = data.target == CharacterContactTarget.Self ? contact.Self : contact.Other;
                if (target == null || !target.Request(data.ability, contact.Self, data.value))
                    Fail("Contact ability was rejected.");
            }
        }
    }
}
