using GameSystems.Characters;
using GameSystems.Sequencing.Values;
using GameSystems.Sequencing;
using System;
using UnityEngine;

namespace GameSystems.Abilities.Actions
{
    [Serializable]
        public sealed class AddMotorVelocityAction : GameAction
        {
            [SerializeField] ComponentTarget<CharacterAbilityController> target =
                new(new SelfGameObjectValue(), ComponentSearchScope.InParents);
            [SerializeField, Tooltip("World-space velocity added to the current motor velocity.")] Vector3 velocity;
            [SerializeReference] Vector3Value velocityValue;
            public override string Summary => $"Add motor velocity {velocityValue?.Summary ?? velocity.ToString()}";
            public override GameActionRuntime CreateRuntime() => new Runtime();
            sealed class Runtime : InstantActionRuntime
            {
                protected override void Execute()
                {
                    AddMotorVelocityAction data = (AddMotorVelocityAction)Definition;
                    CharacterAbilityController abilities = data.target.Get(Context);
                    ICharacterMotorControl motor = abilities?.Motor as ICharacterMotorControl;
                    if (motor == null && Context.TryGet(out CharacterRuntimeContext character))
                        motor = character.Motor as ICharacterMotorControl;
                    if (motor == null) { Fail("Motor cannot be controlled."); return; }
                    motor.SetVelocity(motor.Velocity +
                                      (data.velocityValue?.Get(Context) ?? data.velocity));
                }
            }
        }

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

    [Serializable]
        public sealed class PlaceCharacterOnGroundAction : GameAction
        {
            [SerializeReference] GameObjectValue target = new SelfGameObjectValue();
            [SerializeField] float castHeight = 2f;
            [SerializeField, Min(0f)] float castDistance = 6f;
            public override string Summary => $"Place {target?.Summary ?? "character"} on ground";
            public override GameActionRuntime CreateRuntime() => new Runtime();
            sealed class Runtime : InstantActionRuntime
            {
                protected override void Execute()
                {
                    PlaceCharacterOnGroundAction data = (PlaceCharacterOnGroundAction)Definition;
                    GameObject target = data.target?.Get(Context);
                    if (target == null) { Fail("Missing character target."); return; }
                    CharacterGroundPlacement.PlaceOnGround(target.transform,
                        target.GetComponent<CharacterController>(), data.castHeight, data.castDistance);
                }
            }
        }

        [Serializable]
        public sealed class SetGroundedPositionAction : GameAction
        {
            [SerializeReference] GameObjectValue target = new SelfGameObjectValue();
            [SerializeReference] Vector3Value position = new ConstantVector3Value(Vector3.zero);
            [SerializeField] float castHeight = 2f;
            [SerializeField, Min(0f)] float castDistance = 6f;

            public SetGroundedPositionAction() { }
            public SetGroundedPositionAction(GameObjectValue target, Vector3Value position,
                float castHeight = 2f, float castDistance = 6f)
            {
                this.target = target ?? new SelfGameObjectValue();
                this.position = position ?? new ConstantVector3Value(Vector3.zero);
                this.castHeight = castHeight;
                this.castDistance = castDistance;
            }

            public override string Summary =>
                $"Set {target?.Summary ?? "character"} grounded at {position?.Summary ?? "position"}";
            public override GameActionRuntime CreateRuntime() => new Runtime();

            sealed class Runtime : InstantActionRuntime
            {
                protected override void Execute()
                {
                    SetGroundedPositionAction data = (SetGroundedPositionAction)Definition;
                    GameObject character = data.target?.Get(Context);
                    if (character == null || data.position == null)
                    { Fail("Missing character or grounded position."); return; }
                    CharacterController controller = character.GetComponent<CharacterController>();
                    bool enabled = controller != null && controller.enabled;
                    if (enabled) controller.enabled = false;
                    character.transform.position = data.position.Get(Context);
                    if (enabled) controller.enabled = true;
                    if (!CharacterGroundPlacement.PlaceOnGround(character.transform, controller,
                            data.castHeight, data.castDistance))
                        Fail("No valid ground below the requested position.");
                }
            }
        }
    
        [Serializable]
        public sealed class CopyCharacterPatrolAreaAction : GameAction
        {
            [SerializeReference] GameObjectValue source = new SelfGameObjectValue();
            [SerializeField] ComponentSearchScope sourceSearch = ComponentSearchScope.OnObject;
            [SerializeReference] GameObjectValue target = new TargetGameObjectValue();
            [SerializeField] ComponentSearchScope targetSearch = ComponentSearchScope.OnObject;
            public override string Summary => $"Copy patrol area from {source?.Summary} to {target?.Summary}";
            public override GameActionRuntime CreateRuntime() => new Runtime();
            sealed class Runtime : InstantActionRuntime
            {
                protected override void Execute()
                {
                    CopyCharacterPatrolAreaAction data = (CopyCharacterPatrolAreaAction)Definition;
                    ICharacterPatrolArea area = Resolve<ICharacterPatrolArea>(
                        data.source?.Get(Context), data.sourceSearch);
                    ICharacterPatrolAreaReceiver receiver = Resolve<ICharacterPatrolAreaReceiver>(
                        data.target?.Get(Context), data.targetSearch);
                    if (area == null || receiver == null)
                    { Fail("Missing patrol area source or receiver."); return; }
                    receiver.ConfigurePatrolArea(area.MinimumX, area.MaximumX,
                        area.Direction, area.ReferenceFrame);
                }
    
                static T Resolve<T>(GameObject source, ComponentSearchScope scope) where T : class
                {
                    if (source == null) return null;
                    return scope switch
                    {
                        ComponentSearchScope.InParents =>
                            source.GetComponentInParent(typeof(T), true) as T,
                        ComponentSearchScope.InChildren =>
                            source.GetComponentInChildren(typeof(T), true) as T,
                        _ => source.GetComponent(typeof(T)) as T
                    };
                }
            }
        }

    [Serializable]
        public sealed class ClimbLedgeAction : GameAction
        {
            [SerializeField, Min(.05f)] float duration = .62f;
            [SerializeField] AnimationCurve motion = new(
                new Keyframe(0f, 0f), new Keyframe(.35f, .18f),
                new Keyframe(.72f, .82f), new Keyframe(1f, 1f));
    
            public override string Summary => $"Climb ledge in {duration:0.##}s";
            public override GameActionRuntime CreateRuntime() => new Runtime();
    
            sealed class Runtime : GameActionRuntime
            {
                ICharacterLedgeMotor motor;
                IAbilityLockService abilityLock;
                CharacterLedgeAnchor anchor;
                float elapsed;
                ClimbLedgeAction Data => (ClimbLedgeAction)Definition;
    
                protected override void OnEnter()
                {
                    base.OnEnter();
                    motor = Context.Get<CharacterRuntimeContext>().Motor as ICharacterLedgeMotor;
                    if (motor == null || !motor.IsLedgeAnchored)
                    {
                        Fail("Character is not hanging.");
                        return;
                    }
                    CharacterLedgeAnchor detected = motor.LedgeAnchor;
                    Vector3 currentPosition = Context.Get<CharacterRuntimeContext>().Owner.transform.position;
                    anchor = new CharacterLedgeAnchor(detected.Collider, currentPosition,
                        detected.StandPosition, detected.SurfaceNormal, detected.WallNormal,
                        detected.GripPoint);
                    motor.SetLedgeClimbing(true);
                    abilityLock = Context.Get<CharacterRuntimeContext>().Resolve<IAbilityLockService>();
                    abilityLock?.BeginAbilityLock(true);
                }
    
                protected override bool Tick(float deltaTime)
                {
                    if (motor == null || !motor.IsLedgeAnchored) return true;
                    elapsed += deltaTime;
                    float t = Mathf.Clamp01(elapsed / Mathf.Max(.05f, Data.duration));
                    motor.SetLedgeClimbProgress(t);
                    Vector3 displacement = anchor.StandPosition - anchor.HangPosition;
                    Vector3 up = anchor.SurfaceNormal.sqrMagnitude > .5f
                        ? anchor.SurfaceNormal.normalized : Vector3.up;
                    Vector3 vertical = up * Vector3.Dot(displacement, up);
                    Vector3 inward = displacement - vertical;
    
                    // Mixamo's climb root advances into the platform too early. Remap
                    // its normalized progression into ledge space: clear the lip first,
                    // then move the controller inward while the legs come over it.
                    float rise = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, .72f, t));
                    float mantle = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.3f, 1f, t));
                    motor.MoveLedgeAnchor(anchor.HangPosition + vertical * rise + inward * mantle);
                    return t >= 1f;
                }
    
                protected override void OnExit()
                {
                    motor?.SetLedgeClimbing(false);
                    if (motor != null && motor.IsLedgeAnchored)
                        motor.MoveLedgeAnchor(anchor.StandPosition);
                    motor?.ClearLedgeAnchor();
                    abilityLock?.EndAbilityLock();
                    if (Context.Get<CharacterRuntimeContext>().Motor is ICharacterMotorControl control)
                        control.SetVelocity(Vector3.zero);
                    base.OnExit();
                }
            }
        }

    [Serializable]
        public sealed class FollowTargetWithinPatrolBoundsAction : GameAction
        {
            [SerializeField, Min(0f)] float speed = .8f;
            [SerializeField, Min(0f)] float stoppingDistance = .3f;
            [SerializeField, Min(0f)] float boundaryPadding = .18f;
            [SerializeField, Min(0f)] float acceleration = 7f;
    
            public override string Summary => $"Follow AI target within patrol bounds at {speed:0.##}m/s";
            public override GameActionRuntime CreateRuntime() => new Runtime();
    
            sealed class Runtime : GameActionRuntime
            {
                FollowTargetWithinPatrolBoundsAction Data =>
                    (FollowTargetWithinPatrolBoundsAction)Definition;
                ICharacterTargetProvider targetProvider;
                ICharacterPatrolArea patrol;
    
                protected override void OnEnter()
                {
                    base.OnEnter();
                    CharacterRuntimeContext character = Context.Get<CharacterRuntimeContext>();
                    targetProvider = character.Resolve<ICharacterTargetProvider>();
                    patrol = character.Resolve<ICharacterPatrolArea>();
                    if (targetProvider == null || patrol == null) Fail("Follow requires target and patrol area providers.");
                }
    
                protected override bool Tick(float deltaTime)
                {
                    CharacterRuntimeContext character = Context.Get<CharacterRuntimeContext>();
                    if (Failed || character.Motor == null) return true;
                    Transform target = targetProvider.CurrentTarget;
                    float min = patrol.MinimumX + Data.boundaryPadding;
                    float max = patrol.MaximumX - Data.boundaryPadding;
                    float targetX = target != null ? Mathf.Clamp(target.position.x, min, max)
                        : Mathf.Clamp(character.Transform.position.x, min, max);
                    float delta = targetX - character.Transform.position.x;
                    float direction = Mathf.Abs(delta) <= Data.stoppingDistance ? 0f : Mathf.Sign(delta);
                    if (direction != 0f) patrol.Direction = direction;
    
                    CharacterMotorCommands commands = character.Motor.Commands;
                    commands.HasHorizontalTarget = true;
                    commands.HorizontalTarget = direction * Data.speed;
                    commands.GroundAcceleration = Data.acceleration;
                    commands.GroundDeceleration = Data.acceleration * 1.4f;
                    commands.GroundTurnAcceleration = Data.acceleration * 1.6f;
                    commands.AirAcceleration = Data.acceleration;
                    commands.AirDeceleration = Data.acceleration;
                    commands.AirTurnAcceleration = Data.acceleration;
                    character.Motor.Commands = commands;
                    return false;
                }
            }
        }

    [Serializable]
        public sealed class GoToTargetAction : GameAction
        {
            [SerializeField, Tooltip("Optional explicit character. Uses the sequence character or owner when empty.")]
            CharacterAbilityController target;
            [SerializeField] ComponentTarget<CharacterAbilityController> character =
                new(new SelfGameObjectValue(), ComponentSearchScope.InParents);
            [SerializeField, Tooltip("World target to approach.")]
            Transform destination;
            [SerializeReference] GameObjectValue destinationBinding = new TargetGameObjectValue();
            [SerializeField, Tooltip("World-space offset from the destination transform.")]
            Vector3 destinationOffset;
            [SerializeField, Range(0f, 1f), Tooltip("Maximum normalized locomotion input while approaching the target.")]
            float maximumInput = 1f;
            [SerializeField, Min(0f), Tooltip("Distance where the action starts easing down.")]
            float slowDownDistance = 1.1f;
            [SerializeField, Min(0f), Tooltip("Distance considered close enough to complete.")]
            float stoppingDistance = .12f;
            [SerializeField, Min(0f), Tooltip("Maximum duration. Zero means no timeout.")]
            float timeout = 2f;
            [SerializeField, Tooltip("Zero motor velocity after arrival or timeout.")]
            bool stopOnComplete = true;
    
            public override string Summary =>
                $"Go to {(destination != null ? destination.name : destinationBinding?.Summary ?? "missing target")}";
    
            public override GameActionRuntime CreateRuntime() => new Runtime();
    
            sealed class Runtime : GameActionRuntime, IHorizontalInputProvider
            {
                CharacterAbilityController abilities;
                CharacterRuntimeContext characterContext;
                ICharacterMotorControl control;
                Transform destination;
                float elapsed;
                float horizontal;
    
                GoToTargetAction Data => (GoToTargetAction)Definition;
                public float Horizontal => horizontal;
    
                protected override void OnEnter()
                {
                    base.OnEnter();
                    elapsed = 0f;
                    horizontal = 0f;
                    abilities = Data.target;
                    if (abilities == null) abilities = Data.character.Get(Context);
                    if (abilities == null && Context.TryGet(out CharacterRuntimeContext contextCharacter))
                        abilities = contextCharacter.Abilities;
                    if (abilities == null &&
                        GameActionContextUtility.OwnerGameObject(Context) is GameObject owner)
                        abilities = owner.GetComponent<CharacterAbilityController>();
                    characterContext = abilities != null ? abilities.Context : null;
                    control = characterContext?.Motor as ICharacterMotorControl;
                    destination = Data.destination != null ? Data.destination :
                        Data.destinationBinding?.Get(Context)?.transform;
                    if (abilities == null || characterContext?.Motor == null || destination == null)
                        Fail("Missing character, motor or destination.");
                    else
                        characterContext.Bind<IHorizontalInputProvider>(this);
                }
    
                protected override bool Tick(float deltaTime)
                {
                    if (Failed) return true;
                    elapsed += deltaTime;
                    Vector3 axis = ResolveHorizontalAxis(characterContext.Motor);
                    Vector3 delta = destination.position + Data.destinationOffset -
                                    abilities.transform.position;
                    float distance = Vector3.Dot(delta, axis);
                    float absoluteDistance = Mathf.Abs(distance);
                    if (absoluteDistance <= Data.stoppingDistance ||
                        (Data.timeout > 0f && elapsed >= Data.timeout))
                    {
                        StopMotor();
                        return true;
                    }
    
                    float easedInput = Data.maximumInput;
                    if (Data.slowDownDistance > Data.stoppingDistance)
                    {
                        float t = Mathf.InverseLerp(Data.stoppingDistance,
                            Data.slowDownDistance, absoluteDistance);
                        easedInput *= Mathf.Clamp01(t);
                    }
                    horizontal = Mathf.Sign(distance) * Mathf.Max(.05f, easedInput);
                    return false;
                }
    
                void StopMotor()
                {
                    horizontal = 0f;
                    if (Data.stopOnComplete && control != null) control.SetVelocity(Vector3.zero);
                }
    
                protected override void OnExit()
                {
                    StopMotor();
                    if (characterContext != null)
                        characterContext.Bind<IHorizontalInputProvider>(null);
                    base.OnExit();
                }
    
                static Vector3 ResolveHorizontalAxis(ICharacterMotor motor)
                {
                    Vector3 up = motor is ICharacterGravityFrame gravityFrame
                        ? gravityFrame.UpDirection : Vector3.up;
                    if (up.sqrMagnitude < .001f) up = Vector3.up;
                    up.Normalize();
                    Vector3 planeNormal = motor is ICharacterMovementPlane movementPlane
                        ? movementPlane.MovementPlaneNormal : Vector3.forward;
                    if (planeNormal.sqrMagnitude < .001f) planeNormal = Vector3.forward;
                    planeNormal.Normalize();
                    Vector3 axis = Vector3.Cross(up, planeNormal);
                    if (axis.sqrMagnitude < .001f) axis = Vector3.ProjectOnPlane(Vector3.right, up);
                    return axis.sqrMagnitude > .001f ? axis.normalized : Vector3.right;
                }
            }
        }

    [Serializable]
        public sealed class HangFromLedgeAction : GameAction
        {
            public override string Summary => "Hang from detected ledge";
            public override GameActionRuntime CreateRuntime() => new Runtime();
    
            sealed class Runtime : GameActionRuntime
            {
                ICharacterLedgeMotor ledgeMotor;
    
                protected override void OnEnter()
                {
                    base.OnEnter();
                    ledgeMotor = Context.Get<CharacterRuntimeContext>().Motor as ICharacterLedgeMotor;
                    if (ledgeMotor == null || !ledgeMotor.TryFindLedge(out CharacterLedgeAnchor anchor))
                    {
                        Fail("No reachable ledge.");
                        return;
                    }
                    ledgeMotor.SetLedgeAnchor(anchor);
                }
    
                protected override bool Tick(float deltaTime) => ledgeMotor == null || !ledgeMotor.IsLedgeAnchored;
            }
        }

    [Serializable]
        public sealed class HorizontalLocomotionAction : GameAction
        {
            [SerializeField, Min(0f), Tooltip("Maximum horizontal movement speed.")] float maximumSpeed = 4.8f;
            [SerializeField, Min(0f), Tooltip("Acceleration toward the target speed.")] float acceleration = 24f;
            [SerializeField, Min(0f), Tooltip("Deceleration when input is released.")] float deceleration = 32f;
            [SerializeField, Min(0f), Tooltip("Acceleration used when reversing direction.")] float turnAcceleration = 46f;
            [SerializeField, Min(0f), Tooltip("Base gravity written to motor commands.")] float gravity = 18.5f;
            [SerializeField, Min(0f), Tooltip("Maximum downward speed.")] float maximumFallSpeed = 24f;
            public float MaximumSpeed => maximumSpeed;
            public float Acceleration => acceleration;
            public float Gravity => gravity;
            public override string Summary => $"Horizontal locomotion, speed = {maximumSpeed:0.##}, accel = {acceleration:0.##}";
            public override GameActionRuntime CreateRuntime() => new Runtime();
    
            public HorizontalLocomotionAction() { }
    
            public HorizontalLocomotionAction(float maximumSpeed, float acceleration,
                float deceleration, float turnAcceleration)
            {
                this.maximumSpeed = Mathf.Max(0f, maximumSpeed);
                this.acceleration = Mathf.Max(0f, acceleration);
                this.deceleration = Mathf.Max(0f, deceleration);
                this.turnAcceleration = Mathf.Max(0f, turnAcceleration);
            }
    
            sealed class Runtime : GameActionRuntime
            {
                HorizontalLocomotionAction Data => (HorizontalLocomotionAction)Definition;
    
                protected override bool Tick(float deltaTime)
                {
                    CharacterRuntimeContext character = Context.Get<CharacterRuntimeContext>();
                    ICharacterMotor motor = character.Motor;
                    IHorizontalInputProvider input = character.Resolve<IHorizontalInputProvider>();
                    if (motor == null) { Fail("Missing character motor."); return true; }
                    CharacterMotorCommands commands = motor.Commands;
                    commands.HasHorizontalTarget = true;
                    commands.HorizontalTarget = Mathf.Clamp(input?.Horizontal ?? 0f, -1f, 1f) * Data.maximumSpeed;
                    commands.GroundAcceleration = Data.acceleration;
                    commands.GroundDeceleration = Data.deceleration;
                    commands.GroundTurnAcceleration = Data.turnAcceleration;
                    commands.AirAcceleration = Data.acceleration;
                    commands.AirDeceleration = Data.deceleration;
                    commands.AirTurnAcceleration = Data.turnAcceleration;
                    commands.Gravity = Data.gravity;
                    commands.MaximumFallSpeed = Data.maximumFallSpeed;
                    motor.Commands = commands;
                    return false;
                }
            }
        }

    [Serializable]
        public sealed class MoveCharacterToPositionAlongArcAction : GameAction
        {
            [SerializeField] ComponentTarget<CharacterAbilityController> target =
                new(new SelfGameObjectValue(), ComponentSearchScope.InParents);
            [SerializeReference] Vector3Value destination = new TransformPositionValue();
            [SerializeField, Min(.05f)] float duration = .6f;
            [SerializeField, Min(0f)] float arcHeight = 1f;
            [SerializeField] bool setRotation;
            [SerializeReference] QuaternionValue rotation = new TransformRotationValue();
            public override string Summary => $"Move character on arc to {destination?.Summary}";
            public override GameActionRuntime CreateRuntime() => new Runtime();
            sealed class Runtime : GameActionRuntime
            {
                CharacterAbilityController character;
                ICharacterMotorControl motor;
                Vector3 start;
                Vector3 end;
                Vector3 up;
                Quaternion startRotation;
                Quaternion endRotation;
                float elapsed;
                MoveCharacterToPositionAlongArcAction Data =>
                    (MoveCharacterToPositionAlongArcAction)Definition;
                protected override void OnEnter()
                {
                    base.OnEnter();
                    character = Data.target.Get(Context);
                    motor = character?.Motor as ICharacterMotorControl;
                    if (character == null) { Fail("Missing character target."); return; }
                    start = character.transform.position;
                    end = Data.destination?.Get(Context) ?? start;
                    up = character.Motor is ICharacterGravityFrame gravity
                        ? gravity.UpDirection.normalized : Vector3.up;
                    startRotation = character.transform.rotation;
                    endRotation = Data.rotation?.Get(Context) ?? startRotation;
                }
                protected override bool Tick(float deltaTime)
                {
                    if (Failed) return true;
                    elapsed += deltaTime;
                    float t = Mathf.Clamp01(elapsed / Mathf.Max(.05f, Data.duration));
                    float eased = Mathf.SmoothStep(0f, 1f, t);
                    Vector3 position = Vector3.Lerp(start, end, eased) +
                                       up * (4f * t * (1f - t) * Data.arcHeight);
                    if (motor != null) motor.Teleport(position);
                    else character.transform.position = position;
                    if (Data.setRotation)
                        character.transform.rotation = Quaternion.Slerp(
                            startRotation, endRotation, eased);
                    return t >= 1f;
                }
            }
        }

    [Serializable]
        public sealed class PatrolBoundsAction : GameAction
        {
            [SerializeField, Min(0f)] float speed = 1f;
            [SerializeField, Min(0f)] float acceleration = 8f;
            [SerializeField, Min(0f)] float turnAcceleration = 14f;
            public override string Summary => $"Patrol bounds at {speed:0.##}m/s";
            public override GameActionRuntime CreateRuntime() => new Runtime();
    
            sealed class Runtime : GameActionRuntime
            {
                ICharacterPatrolArea patrol;
                PatrolBoundsAction Data => (PatrolBoundsAction)Definition;
    
                protected override void OnEnter()
                {
                    base.OnEnter();
                    patrol = Context.Get<CharacterRuntimeContext>().Resolve<ICharacterPatrolArea>();
                    if (patrol == null) Fail("Missing patrol area provider.");
                }
    
                protected override bool Tick(float deltaTime)
                {
                    CharacterRuntimeContext character = Context.Get<CharacterRuntimeContext>();
                    ICharacterMotor motor = character.Motor;
                    if (patrol == null || motor == null) { Fail("Patrol requires an area and a motor."); return true; }
                    float x = character.Transform.position.x;
                    if (x >= patrol.MaximumX) patrol.Direction = -1f;
                    else if (x <= patrol.MinimumX) patrol.Direction = 1f;
                    CharacterMotorCommands commands = motor.Commands;
                    commands.HasHorizontalTarget = true;
                    commands.HorizontalTarget = Mathf.Sign(patrol.Direction) * Data.speed;
                    commands.GroundAcceleration = Data.acceleration;
                    commands.GroundDeceleration = Data.acceleration;
                    commands.GroundTurnAcceleration = Data.turnAcceleration;
                    commands.AirAcceleration = Data.acceleration;
                    commands.AirDeceleration = Data.acceleration;
                    commands.AirTurnAcceleration = Data.turnAcceleration;
                    motor.Commands = commands;
                    return false;
                }
    
            }
        }

    [Serializable]
        public sealed class ResetMotorAction : GameAction
        {
            [SerializeField] ComponentTarget<CharacterAbilityController> target =
                new(new SelfGameObjectValue(), ComponentSearchScope.InParents);
    
            public ResetMotorAction() { }
            public ResetMotorAction(ComponentTarget<CharacterAbilityController> target)
            {
                if (target != null) this.target = target;
            }
    
            public override string Summary => $"Reset motor on {target?.Summary ?? "None"}";
            public override GameActionRuntime CreateRuntime() => new Runtime();
            sealed class Runtime : InstantActionRuntime
            {
                protected override void Execute()
                {
                    ResetMotorAction data = (ResetMotorAction)Definition;
                    CharacterAbilityController abilities = data.target?.Get(Context);
                    if (abilities == null && Context.TryGet(out CharacterRuntimeContext character))
                        abilities = character.Abilities;
    
                    ICharacterMotor motor = abilities?.Motor;
                    if (motor == null) { Fail("Missing character motor."); return; }
                    motor.ResetMotor();
                }
            }
        }

    [Serializable]
        public sealed class SetGravityDirectionAction : GameAction
        {
            [SerializeField, Tooltip("World-space direction in which gravity pulls.")]
            Vector3 direction = Vector3.down;
    
            public override string Summary => $"Set gravity direction = {direction}";
            public override GameActionRuntime CreateRuntime() => new Runtime();
    
            sealed class Runtime : GameActionRuntime
            {
                protected override bool Tick(float deltaTime)
                {
                    ICharacterMotor motor = Context.Get<CharacterRuntimeContext>().Motor;
                    if (motor == null) { Fail("Missing character motor."); return true; }
                    Vector3 direction = ((SetGravityDirectionAction)Definition).direction;
                    if (direction.sqrMagnitude < .0001f)
                    { Fail("Gravity direction cannot be zero."); return true; }
                    CharacterMotorCommands commands = motor.Commands;
                    commands.HasGravityDirection = true;
                    commands.GravityDirection = direction.normalized;
                    motor.Commands = commands;
                    return false;
                }
            }
        }

    [Serializable]
        public sealed class SetMotorVerticalVelocityAction : GameAction
        {
            [SerializeField] ComponentTarget<CharacterAbilityController> target =
                new(new SelfGameObjectValue(), ComponentSearchScope.InParents);
            [SerializeReference] FloatValue velocity = new ConstantFloatValue();
    
            public override string Summary => $"Set motor vertical velocity = {velocity?.Summary ?? "0"}";
            public override GameActionRuntime CreateRuntime() => new Runtime();
    
            sealed class Runtime : InstantActionRuntime
            {
                protected override void Execute()
                {
                    SetMotorVerticalVelocityAction data =
                        (SetMotorVerticalVelocityAction)Definition;
                    CharacterAbilityController abilities = data.target.Get(Context);
                    if (abilities == null && Context.TryGet(out CharacterRuntimeContext character))
                        abilities = character.Abilities;
                    if (abilities?.Motor is not ICharacterMotorControl motor)
                    { Fail("Motor cannot be controlled."); return; }
                    Vector3 value = motor.Velocity;
                    value.y = data.velocity?.Get(Context) ?? 0f;
                    motor.SetVelocity(value);
                }
            }
        }
    
        [Serializable]
        public sealed class SetMotorVelocityAction : GameAction
        {
            [SerializeField, Tooltip("Optional explicit character. Uses the sequence character or owner when empty.")]
            CharacterAbilityController target;
            [SerializeField] ComponentTarget<CharacterAbilityController> binding =
                new(new SelfGameObjectValue(), ComponentSearchScope.InParents);
            [SerializeField, Tooltip("World-space velocity assigned to the character motor.")] Vector3 velocity;
            [SerializeReference] Vector3Value velocityValue;
            public SetMotorVelocityAction() { }
            public SetMotorVelocityAction(Vector3Value velocity,
                ComponentTarget<CharacterAbilityController> target = null)
            { velocityValue = velocity; if (target != null) binding = target; }
            public override string Summary => $"Set motor velocity = {velocityValue?.Summary ?? velocity.ToString()}";
            public override GameActionRuntime CreateRuntime() => new Runtime();
            sealed class Runtime : InstantActionRuntime
            {
                protected override void Execute()
                {
                    SetMotorVelocityAction data = (SetMotorVelocityAction)Definition;
                    CharacterAbilityController abilities = data.target;
                    if (abilities == null) abilities = data.binding.Get(Context);
                    if (abilities == null && Context.TryGet(out CharacterRuntimeContext character))
                        abilities = character.Abilities;
                    if (abilities == null &&
                        GameActionContextUtility.OwnerGameObject(Context) is GameObject owner)
                        abilities = owner.GetComponent<CharacterAbilityController>();
                    if (abilities?.Motor is not ICharacterMotorControl motor)
                    {
                        Fail("Motor cannot be controlled.");
                        return;
                    }
                    motor.SetVelocity(data.velocityValue?.Get(Context) ?? data.velocity);
                }
            }
        }

    [Serializable]
        public sealed class TeleportCharacterAction : GameAction
        {
            [SerializeField] ComponentTarget<CharacterAbilityController> target =
                new(new SelfGameObjectValue(), ComponentSearchScope.InParents);
            [SerializeField, Tooltip("World position or offset used for teleportation.")] Vector3 position;
            [SerializeReference] Vector3Value positionValue;
            [SerializeField, Tooltip("Treat Position as an offset from the current character position.")] bool relative;
            public override string Summary => $"Teleport character {(relative ? "by" : "to")} {positionValue?.Summary ?? position.ToString()}";
            public override GameActionRuntime CreateRuntime() => new Runtime();
            sealed class Runtime : InstantActionRuntime
            {
                protected override void Execute()
                {
                    TeleportCharacterAction data = (TeleportCharacterAction)Definition;
                    CharacterAbilityController abilities = data.target.Get(Context);
                    if (abilities == null && Context.TryGet(out CharacterRuntimeContext character))
                        abilities = character.Abilities;
                    if (abilities?.Motor is not ICharacterMotorControl motor)
                    { Fail("Motor cannot teleport."); return; }
                    Vector3 value = data.positionValue?.Get(Context) ?? data.position;
                    motor.Teleport(data.relative ? abilities.transform.position + value : value);
                }
            }
        }

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
                    ICharacterGravityFrame gravityFrame = character.Motor as ICharacterGravityFrame;
                    Vector3 up = gravityFrame?.UpDirection ?? Vector3.up;
                    CharacterMotorCommands commands = character.Motor.Commands;
                    if (needsImpulse)
                    {
                        commands.HasVerticalOverride = true;
                        commands.VerticalOverride = Data.initialVelocity;
                        needsImpulse = false;
                    }
                    else if (holdRemaining > 0f && input != null && input.IsHeld(ability.Definition) &&
                             Vector3.Dot(character.Motor.Result.Velocity, up) > 0f)
                    {
                        commands.AdditiveImpulse += up * (Data.holdAcceleration * deltaTime);
                        holdRemaining -= deltaTime;
                    }
                    else holdRemaining = 0f;
                    float vertical = commands.HasVerticalOverride ? commands.VerticalOverride :
                        Vector3.Dot(character.Motor.Result.Velocity, up);
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

    [Serializable]
        public sealed class WallGripAction : GameAction
        {
            [SerializeField, Min(0f), Tooltip("Maximum downward speed while gripping the wall. Zero holds position.")]
            float wallSlideSpeed = .28f;
    
            public float WallSlideSpeed => wallSlideSpeed;
            public override string Summary => wallSlideSpeed <= .001f
                ? "Grip wall without sliding"
                : $"Grip wall, slide = {wallSlideSpeed:0.##}m/s";
            public override GameActionRuntime CreateRuntime() => new Runtime();
    
            sealed class Runtime : GameActionRuntime
            {
                WallGripAction Data => (WallGripAction)Definition;
    
                protected override bool Tick(float deltaTime)
                {
                    ICharacterMotor motor = Context.Get<CharacterRuntimeContext>().Motor;
                    if (motor == null) { Fail("Missing character motor."); return true; }
                    CharacterMotorCommands commands = motor.Commands;
                    commands.HasVerticalOverride = true;
                    commands.VerticalOverride = -Data.wallSlideSpeed;
                    motor.Commands = commands;
                    return false;
                }
    
                protected override bool TickLate()
                {
                    CharacterMotorResult result = Context.Get<CharacterRuntimeContext>().Motor.Result;
                    return result.Ground.IsGrounded || !result.Wall.IsTouching;
                }
            }
        }

    [Serializable]
        public sealed class WallJumpAction : GameAction
        {
            [SerializeField, Min(0f), Tooltip("Initial upward velocity.")] float verticalVelocity = 7.2f;
            [SerializeField, Min(0f), Tooltip("Horizontal velocity away from the wall.")] float horizontalVelocity = 5.2f;
            [SerializeField, Min(0f), Tooltip("Duration of forced movement away from the wall.")] float horizontalControlLock = .16f;
            [SerializeField, Min(0f), Tooltip("Maximum duration before the action succeeds.")] float completionDelay = .18f;
            public float VerticalVelocity => verticalVelocity;
            public float HorizontalVelocity => horizontalVelocity;
            public override string Summary => $"Wall jump, velocity = ({horizontalVelocity:0.##}, {verticalVelocity:0.##}), lock = {horizontalControlLock:0.###}s";
            public override GameActionRuntime CreateRuntime() => new Runtime();
            sealed class Runtime : GameActionRuntime
            {
                float outwardDirection;
                float controlLockRemaining;
                bool needsImpulse;
                WallJumpAction Data => (WallJumpAction)Definition;
                protected override void OnEnter()
                {
                    base.OnEnter();
                    ICharacterMotor motor = Context.Get<CharacterRuntimeContext>().Motor;
                    CharacterMotorResult result = motor.Result;
                    Vector3 wallNormal = result.Wall.Normal;
                    if (motor is ICharacterLedgeMotor { IsLedgeAnchored: true } ledgeMotor)
                    {
                        wallNormal = ledgeMotor.LedgeAnchor.WallNormal;
                        ledgeMotor.ClearLedgeAnchor();
                    }
                    outwardDirection = Mathf.Abs(wallNormal.x) > .01f ? Mathf.Sign(wallNormal.x) : -Mathf.Sign(result.Velocity.x);
                    if (Mathf.Approximately(outwardDirection, 0f)) outwardDirection = 1f;
                    controlLockRemaining = Data.horizontalControlLock;
                    needsImpulse = true;
                }
                protected override bool Tick(float deltaTime)
                {
                    ICharacterMotor motor = Context.Get<CharacterRuntimeContext>().Motor;
                    if (motor == null) { Fail("Missing character motor."); return true; }
                    CharacterMotorCommands commands = motor.Commands;
                    if (needsImpulse) { commands.HasVerticalOverride = true; commands.VerticalOverride = Data.verticalVelocity; needsImpulse = false; }
                    if (controlLockRemaining > 0f)
                    {
                        commands.HasHorizontalTarget = true;
                        commands.HorizontalTarget = outwardDirection * Data.horizontalVelocity;
                        commands.AirAcceleration = Mathf.Max(commands.AirAcceleration, 80f);
                        commands.AirTurnAcceleration = Mathf.Max(commands.AirTurnAcceleration, 80f);
                        controlLockRemaining -= deltaTime;
                    }
                    motor.Commands = commands;
                    return false;
                }
                protected override bool TickLate()
                {
                    CharacterRuntimeContext character = Context.Get<CharacterRuntimeContext>();
                    return character.Motor.Result.JustLanded || Context.Get<AbilityRuntime>().ActiveTime >= Data.completionDelay;
                }
            }
        }
}
