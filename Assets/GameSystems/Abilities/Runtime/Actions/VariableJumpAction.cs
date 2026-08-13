using System;
using GameSystems.Sequencing;
using UnityEngine;

using GameSystems.Characters;

namespace GameSystems.Abilities.Actions
{
    [Serializable]
    public sealed class VariableJumpAction : GameAction
    {
        [SerializeField, Tooltip("Initial upward velocity.")] float initialVelocity = 6.35f;
        [SerializeField, Tooltip("Additional upward acceleration while jump remains held.")] float holdAcceleration = 10.5f;
        [SerializeField, Min(0f), Tooltip("Maximum duration of variable jump hold.")] float maximumHoldTime = .2f;
        [SerializeField, Tooltip("Gravity multiplier while descending.")] float fallGravityMultiplier = 1.25f;
        [SerializeField, Tooltip("Gravity multiplier when jump is released early.")] float jumpCutMultiplier = 1.45f;
        [SerializeField, Tooltip("Gravity multiplier around the jump apex.")] float apexGravityMultiplier = .62f;
        [SerializeField, Min(0f), Tooltip("Vertical speed range considered near the apex.")] float apexVelocityThreshold = 1.05f;
        public float InitialVelocity => initialVelocity;
        public float HoldAcceleration => holdAcceleration;
        public float MaximumHoldTime => maximumHoldTime;
        public float FallGravityMultiplier => fallGravityMultiplier;
        public float JumpCutMultiplier => jumpCutMultiplier;
        public float ApexGravityMultiplier => apexGravityMultiplier;
        public float ApexVelocityThreshold => apexVelocityThreshold;
        public override string Summary => $"Variable jump, velocity = {initialVelocity:0.##}, hold = {maximumHoldTime:0.###}s";
        public override GameActionRuntime CreateRuntime() => new Runtime();

        sealed class Runtime : GameActionRuntime
        {
            IAbilityInputState input;
            bool needsImpulse;
            float holdRemaining;
            VariableJumpAction Data => (VariableJumpAction)Definition;
            protected override void OnEnter()
            {
                base.OnEnter();
                input = Context.Get<CharacterRuntimeContext>().Resolve<IAbilityInputState>();
                needsImpulse = true;
                holdRemaining = Data.maximumHoldTime;
            }
            protected override bool Tick(float deltaTime)
            {
                CharacterRuntimeContext character = Context.Get<CharacterRuntimeContext>();
                AbilityRuntime ability = Context.Get<AbilityRuntime>();
                if (character.Motor == null) { Fail("Missing character motor."); return true; }
                CharacterMotorCommands commands = character.Motor.Commands;
                if (needsImpulse)
                {
                    commands.HasVerticalOverride = true;
                    commands.VerticalOverride = Data.initialVelocity;
                    needsImpulse = false;
                }
                else if (holdRemaining > 0f && input != null && input.IsHeld(ability.Definition) && character.Motor.Result.Velocity.y > 0f)
                {
                    commands.AdditiveImpulse.y += Data.holdAcceleration * deltaTime;
                    holdRemaining -= deltaTime;
                }
                else holdRemaining = 0f;
                float vertical = commands.HasVerticalOverride ? commands.VerticalOverride : character.Motor.Result.Velocity.y;
                bool held = input != null && input.IsHeld(ability.Definition);
                float gravity = vertical < 0f ? Data.fallGravityMultiplier : 1f;
                if (Mathf.Abs(vertical) < Data.apexVelocityThreshold) gravity *= Data.apexGravityMultiplier;
                if (vertical > 0f && !held) gravity *= Data.jumpCutMultiplier;
                commands.GravityMultiplier = gravity;
                character.Motor.Commands = commands;
                return false;
            }
            protected override bool TickLate()
            {
                CharacterRuntimeContext character = Context.Get<CharacterRuntimeContext>();
                AbilityRuntime ability = Context.Get<AbilityRuntime>();
                return ability.ActiveTime > .03f && character.Motor.Result.JustLanded;
            }
        }
    }
}
