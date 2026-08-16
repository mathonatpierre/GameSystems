using GameSystems.Abilities;
using GameSystems.Characters;
using GameSystems.Playables;
using GameSystems.Sequencing;
using System;
using UnityEngine;

namespace GameSystems.Rideables.Actions
{
    [Serializable]
        public sealed class BeginRailFlightAction : GameAction
        {
            [SerializeField, Tooltip("Optional explicit flight constraint. Uses the character or owner when empty.")]
            RailFlightPathConstraint target;
    
            public override string Summary => "Begin rail flight";
            public override GameActionRuntime CreateRuntime() => new Runtime();
    
            sealed class Runtime : InstantActionRuntime
            {
                protected override void Execute()
                {
                    BeginRailFlightAction data = (BeginRailFlightAction)Definition;
                    RailFlightPathConstraint target = Resolve(data.target);
                    if (target == null)
                    {
                        Fail("Begin Rail Flight requires a rail flight constraint.");
                        return;
                    }
                    if (!target.BeginFlight()) Fail("The rail flight could not begin.");
                }
    
                RailFlightPathConstraint Resolve(RailFlightPathConstraint explicitTarget)
                {
                    if (explicitTarget != null) return explicitTarget;
                    if (Context.TryGet(out CharacterRuntimeContext character))
                        return character.Owner.GetComponent<RailFlightPathConstraint>();
                    return GameActionContextUtility.OwnerGameObject(Context)
                        ?.GetComponent<RailFlightPathConstraint>();
                }
            }
        }

    [Serializable]
        public sealed class RailFlightLocomotionAction : GameAction
        {
            public override string Summary => "Fly along rail using directional input";
            public override GameActionRuntime CreateRuntime() => new Runtime();
    
            sealed class Runtime : GameActionRuntime
            {
                IDirectionalInputProvider input;
                ICharacterSurfaceAlignmentControl motor;
                RailFlightPathConstraint constraint;
                RideableController rideable;
                Collider ownerCollider;
    
                protected override void OnEnter()
                {
                    base.OnEnter();
                    CharacterRuntimeContext character = Context.Get<CharacterRuntimeContext>();
                    input = character.Resolve<IDirectionalInputProvider>();
                    motor = character.Motor as ICharacterSurfaceAlignmentControl;
                    constraint = character.Owner.GetComponent<RailFlightPathConstraint>();
                    rideable = character.Owner.GetComponentInChildren<RideableController>(true);
                    ownerCollider = character.Owner.GetComponent<Collider>();
                    motor?.SetSurfaceConstraint(true);
                    motor?.SetFollowSurfaceForward(true);
                    motor?.SetSurfaceGround(ownerCollider, character.Owner.transform.position, Vector3.up);
                    if (input == null || motor == null || constraint == null)
                        Fail("Missing directional input, standard motor or rail constraint.");
                }
    
                protected override bool Tick(float deltaTime)
                {
                    if (constraint == null || !constraint.Step(input?.Directional ?? Vector2.zero,
                            deltaTime, out Vector3 position, out Vector3 forward,
                            out Vector3 up, out Quaternion lean)) return true;
                    motor.SetSurfaceFrame(up, forward);
                    motor.SetSurfaceGround(ownerCollider, position, up);
                    motor.SetCollisionFrame(true, up, forward);
                    motor.MoveConstrained(position);
                    motor.SetSurfaceVisualOffset(lean);
                    rideable?.SetAnimationFloat("FlightHorizontal", constraint.FilteredInput.x);
                    rideable?.SetAnimationFloat("FlightVertical", constraint.FilteredInput.y);
                    return false;
                }
    
                protected override void OnExit()
                {
                    motor?.SetSurfaceConstraint(false);
                    motor?.ClearSurfaceGround();
                    motor?.SetSurfaceVisualOffset(Quaternion.identity);
                    motor?.SetFollowSurfaceForward(false);
                    motor?.SetCollisionFrame(false, Vector3.up, Vector3.forward);
                }
            }
        }

    [Serializable]
        public sealed class ResetRailFlightAction : GameAction
        {
            [SerializeField, Tooltip("Optional explicit flight constraint. Uses the character or owner when empty.")]
            RailFlightPathConstraint target;
    
            public override string Summary => "Reset rail flight";
            public override GameActionRuntime CreateRuntime() => new Runtime();
    
            sealed class Runtime : InstantActionRuntime
            {
                protected override void Execute()
                {
                    ResetRailFlightAction data = (ResetRailFlightAction)Definition;
                    RailFlightPathConstraint target = Resolve(data.target);
                    if (target == null)
                    {
                        Fail("Reset Rail Flight requires a rail flight constraint.");
                        return;
                    }
                    target.ResetFlight();
                }
    
                RailFlightPathConstraint Resolve(RailFlightPathConstraint explicitTarget)
                {
                    if (explicitTarget != null) return explicitTarget;
                    if (Context.TryGet(out CharacterRuntimeContext character))
                        return character.Owner.GetComponent<RailFlightPathConstraint>();
                    return GameActionContextUtility.OwnerGameObject(Context)
                        ?.GetComponent<RailFlightPathConstraint>();
                }
            }
        }

    [Serializable]
        public sealed class SetRailFlightSpeedMultiplierAction : GameAction
        {
            [SerializeField] RailFlightPathConstraint flight;
            [SerializeField, Min(0f)] float multiplier = 1.75f;
    
            public override string Summary => $"Set rail flight speed x{multiplier:0.##}";
    
            public override GameActionRuntime CreateRuntime() => new Runtime();
    
            sealed class Runtime : InstantActionRuntime
            {
                protected override void Execute()
                {
                    SetRailFlightSpeedMultiplierAction data =
                        (SetRailFlightSpeedMultiplierAction)Definition;
                    RailFlightPathConstraint target = data.flight;
                    if (target == null && Context.TryGet(out CharacterRuntimeContext character))
                        target = character.Owner.GetComponent<RailFlightPathConstraint>();
                    if (target == null)
                    {
                        Fail("Missing rail flight constraint.");
                        return;
                    }
                    target.SpeedMultiplier = data.multiplier;
                }
            }
        }
}
