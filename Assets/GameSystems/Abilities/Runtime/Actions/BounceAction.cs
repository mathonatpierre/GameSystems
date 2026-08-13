using System;
using GameSystems.Sequencing;
using UnityEngine;

using GameSystems.Characters;

namespace GameSystems.Abilities.Actions
{
    [Serializable]
    public sealed class BounceAction : GameAction
    {
        [SerializeField, Min(0f), Tooltip("Extra upward velocity when any jump input is held.")] float heldJumpBonus = 1.9f;
        public override string Summary => $"Bounce from request value, held bonus = {heldJumpBonus:0.##}";
        public override GameActionRuntime CreateRuntime() => new Runtime();
        sealed class Runtime : GameActionRuntime
        {
            bool pendingImpulse;
            float velocity;
            BounceAction Data => (BounceAction)Definition;
            protected override void OnEnter()
            {
                base.OnEnter();
                CharacterRuntimeContext character = Context.Get<CharacterRuntimeContext>();
                AbilityRuntime ability = Context.Get<AbilityRuntime>();
                IAbilityInputState input = character.Resolve<IAbilityInputState>();
                velocity = Mathf.Max(0f, ability.LastRequest.Value) + (input != null && input.AnyAbilityHeld ? Data.heldJumpBonus : 0f);
                pendingImpulse = true;
            }
            protected override bool Tick(float deltaTime)
            {
                ICharacterMotor motor = Context.Get<CharacterRuntimeContext>().Motor;
                if (motor == null) { Fail("Missing character motor."); return true; }
                if (!pendingImpulse) return true;
                CharacterMotorCommands commands = motor.Commands;
                commands.HasVerticalOverride = true;
                commands.VerticalOverride = velocity;
                motor.Commands = commands;
                pendingImpulse = false;
                return true;
            }
        }
    }
}
