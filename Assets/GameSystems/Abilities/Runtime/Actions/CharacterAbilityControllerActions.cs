using GameSystems.Abilities;
using GameSystems.Characters;
using GameSystems.Sequencing.Values;
using GameSystems.Sequencing;
using System;
using UnityEngine;

namespace GameSystems.Abilities.Actions
{
    [Serializable]
        public sealed class BeginAbilityLockAction : GameAction
        {
            [SerializeField, Tooltip("Continue motor simulation while character input and abilities are locked.")]
            bool keepSimulatingMotor;
            [SerializeField] ComponentTarget<CharacterAbilityController> target =
                new(new TargetGameObjectValue(), ComponentSearchScope.InParents);

            public BeginAbilityLockAction() { }
            public BeginAbilityLockAction(bool simulateMotor,
                ComponentTarget<CharacterAbilityController> target = null)
            { keepSimulatingMotor = simulateMotor; if (target != null) this.target = target; }
    
            public override string Summary => $"Begin ability lock, simulate motor = {keepSimulatingMotor.ToString().ToLowerInvariant()}";
            public override GameActionRuntime CreateRuntime() => new Runtime();
    
            sealed class Runtime : InstantActionRuntime
            {
                protected override void Execute()
                {
                    BeginAbilityLockAction data = (BeginAbilityLockAction)Definition;
                    CharacterAbilityController abilities = data.target.Get(Context);
                    if (abilities == null && Context.TryGet(out CharacterRuntimeContext character))
                        abilities = character.Abilities;
                    if (abilities == null) { Fail("Missing ability controller."); return; }
                    abilities.BeginAbilityLock(data.keepSimulatingMotor);
                }
            }
        }

    [Serializable]
        public sealed class CancelAbilityAction : GameAction
        {
            public override string Summary => "Cancel current ability";
            public override GameActionRuntime CreateRuntime() => new Runtime();
            sealed class Runtime : InstantActionRuntime
            {
                protected override void Execute() => Context.Get<AbilityRuntime>().Cancel();
            }
        }

    [Serializable]
        public sealed class CompleteAbilityAction : GameAction
        {
            public override string Summary => "Complete ability";
            public override GameActionRuntime CreateRuntime() => new Runtime();
    
            sealed class Runtime : InstantActionRuntime
            {
                protected override void Execute()
                {
                    Context.Get<AbilityRuntime>().Complete();
                }
            }
        }

    [Serializable]
        public sealed class EndAbilityLockAction : GameAction
        {
            public override string Summary => "End ability lock";
            public override GameActionRuntime CreateRuntime() => new Runtime();
    
            sealed class Runtime : InstantActionRuntime
            {
                protected override void Execute()
                {
                    Context.Get<CharacterRuntimeContext>().Resolve<IAbilityLockService>()?.EndAbilityLock();
                }
            }
        }

    [Serializable]
        public sealed class RequestAbilityAction : GameAction
        {
            [SerializeField, Tooltip("Ability requested from the current character controller.")] AbilityDefinition ability;
            [SerializeField, Tooltip("Numeric request payload passed to the ability.")] float value = 1f;
            [SerializeReference] FloatValue valueBinding;
            [SerializeField, Tooltip("Optional explicit character. Uses the sequence character or owner when empty.")]
            CharacterAbilityController target;
            [SerializeField] ComponentTarget<CharacterAbilityController> binding =
                new(new SelfGameObjectValue(), ComponentSearchScope.InParents);
            [SerializeReference] GameObjectValue source = new SelfGameObjectValue();
            public RequestAbilityAction() { }
            public RequestAbilityAction(AbilityDefinition ability, float value,
                ComponentTarget<CharacterAbilityController> target,
                GameObjectValue source = null)
            { this.ability = ability; this.value = value; this.binding = target;
              if (source != null) this.source = source; }
            public override string Summary => $"Request ability {(ability != null ? ability.name : "missing")}, value = {value:0.##}";
            public override GameActionRuntime CreateRuntime() => new Runtime();
            sealed class Runtime : InstantActionRuntime
            {
                protected override void Execute()
                {
                    RequestAbilityAction data = (RequestAbilityAction)Definition;
                    CharacterAbilityController abilities = data.target;
                    if (abilities == null) abilities = data.binding.Get(Context);
                    GameObject source = data.source?.Get(Context);
                    if (abilities == null && Context.TryGet(out CharacterRuntimeContext character))
                        abilities = character.Abilities;
                    if (abilities == null && source != null)
                        abilities = source.GetComponent<CharacterAbilityController>();
                    if (abilities == null || !abilities.Request(data.ability, source,
                            data.valueBinding?.Get(Context) ?? data.value))
                        Fail("Ability request was rejected.");
                }
            }
        }

    [Serializable]
        public sealed class RequestReactionAction : GameAction
        {
            [SerializeField] ComponentTarget<CharacterAbilityController> target =
                new(new SelfGameObjectValue(), ComponentSearchScope.InParents);
            [SerializeReference] GameObjectValue source = new SelfGameObjectValue();
            [SerializeField] ReactionDefinition reaction;
            [SerializeField, Tooltip("Numeric request payload passed to the reaction.")] float value = 1f;
            [SerializeReference] FloatValue valueBinding;
            public RequestReactionAction() { }
            public RequestReactionAction(ReactionDefinition reaction,
                ComponentTarget<CharacterAbilityController> target,
                GameObjectValue source = null, float value = 1f)
            { this.reaction = reaction; if (target != null) this.target = target; this.value = value;
              if (source != null) this.source = source; }
            public override string Summary => $"Request reaction {(reaction != null ? reaction.name : "missing")}";
            public override GameActionRuntime CreateRuntime() => new Runtime();
            sealed class Runtime : InstantActionRuntime
            {
                protected override void Execute()
                {
                    RequestReactionAction data = (RequestReactionAction)Definition;
                    CharacterAbilityController abilities = data.target.Get(Context);
                    if (abilities == null && Context.TryGet(out CharacterRuntimeContext character))
                        abilities = character.Abilities;
                    if (abilities == null) { Fail("Missing ability controller."); return; }
                    GameObject source = data.source?.Get(Context);
                    float value = data.valueBinding?.Get(Context) ?? data.value;
                    bool accepted = abilities.RequestReaction(data.reaction, value, source);
                    if (!accepted) Fail("Reaction request was rejected.");
                }
            }
        }

    [Serializable]
        public sealed class ResetAbilitiesForRespawnAction : GameAction
        {
            public override string Summary => "Reset abilities for respawn";
            public override GameActionRuntime CreateRuntime() => new Runtime();
    
            sealed class Runtime : InstantActionRuntime
            {
                protected override void Execute()
                {
                    CharacterRuntimeContext character = Context.Get<CharacterRuntimeContext>();
                    AbilityRuntime preservedAbility = Context.Get<AbilityRuntime>();
                    if (character.Abilities == null) { Fail("Missing ability controller."); return; }
                    character.Abilities.ResetForRespawn(preservedAbility);
                }
            }
        }
    
        [Serializable]
        public sealed class RespawnAtCheckpointAction : GameAction
        {
            public override string Summary => "Respawn at checkpoint";
            public override GameActionRuntime CreateRuntime() => new Runtime();
    
            sealed class Runtime : InstantActionRuntime
            {
                protected override void Execute()
                {
                    CharacterRuntimeContext character = Context.Get<CharacterRuntimeContext>();
                    ICharacterCheckpointService checkpoints =
                        character.Resolve<ICharacterCheckpointService>();
                    if (checkpoints == null) { Fail("Missing checkpoint service."); return; }
                    checkpoints.Respawn();
                }
            }
        }
}
