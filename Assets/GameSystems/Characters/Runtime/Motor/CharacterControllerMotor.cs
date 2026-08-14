using UnityEngine;
using GameSystems.Abilities;
using UnityEngine.Scripting.APIUpdating;
using GameSystems.Playables;

namespace GameSystems.Characters
{
    [MovedFrom(true, "GameSystems.Abilities", "GameSystems.Abilities", "CharacterControllerMotor")]
    [DefaultExecutionOrder(-200)]
    [RequireComponent(typeof(CharacterController))]
    public sealed class CharacterControllerMotor : MonoBehaviour, ICharacterMotor, ICharacterMotorControl,
        ICharacterGravityFrame, ICharacterSurfaceFrame, ICharacterSurfaceAlignmentControl,
        IPlayablePostProcessor
    {
        [SerializeField, Min(0f)] float groundProbeDistance = .045f;
        [SerializeField, Range(0f, 1f)] float minimumGroundNormal = .62f;
        [SerializeField, Min(0f)] float wallProbeDistance = .09f;
        [SerializeField, Range(0f, 1f)] float maximumWallNormalY = .28f;
        [SerializeField, Min(0f), Tooltip("Gravity used when no active action supplies one.")]
        float defaultGravity = 18f;
        [SerializeField, Min(0f)] float defaultMaximumFallSpeed = 24f;
        [SerializeField, Tooltip("World-space direction used when actions do not supply gravity.")]
        Vector3 defaultGravityDirection = Vector3.down;
        [SerializeField] Transform visualRoot;
        [SerializeField, Tooltip("Normal of the 2.5D movement plane used to derive a stable lateral axis.")]
        Vector3 movementPlaneNormal = Vector3.forward;
        [SerializeField, Min(.01f)] float surfaceAlignmentSharpness = 18f;
        [SerializeField, Min(0f)] float facingDeadZone = .04f;

        CharacterController controller;
        CharacterMotorCommands commands;
        CharacterMotorResult result;
        Vector3 velocity;
        Vector3 gravityDirection = Vector3.down;
        Vector3 surfaceForward = Vector3.right;
        float facingSign = 1f;
        bool followSurfaceForward;
        Vector3 forcedSurfaceUp = Vector3.up;
        Vector3 forcedSurfaceForward = Vector3.right;
        GroundContact forcedGround;
        bool hasForcedGround;
        bool surfaceConstrained;
        Quaternion defaultRootRotation;
        float airTime;
        CharacterAbilityController abilities;
        UnityPlayableAnimationPlayer animationPlayer;
        [SerializeField] CharacterCheckpointService checkpoints = new();

        public CharacterMotorCommands Commands { get => commands; set => commands = value; }
        public CharacterMotorResult Result => result;
        public Vector3 Velocity => velocity;
        public Vector3 GravityDirection => gravityDirection;
        public Vector3 UpDirection => -gravityDirection;
        public float UpwardSpeed => Vector3.Dot(velocity, UpDirection);
        public void ConfigureVisualRoot(Transform value) => visualRoot = value;
        public void SetFollowSurfaceForward(bool value) => followSurfaceForward = value;
        public void SetSurfaceFrame(Vector3 up, Vector3 forward)
        {
            if (up.sqrMagnitude > .5f) forcedSurfaceUp = up.normalized;
            if (forward.sqrMagnitude > .5f) forcedSurfaceForward = forward.normalized;
        }
        public void SetSurfaceGround(Collider collider, Vector3 point, Vector3 normal)
        {
            forcedGround = new GroundContact(true, collider, point, normal);
            hasForcedGround = true;
        }
        public void ClearSurfaceGround() { hasForcedGround = false; forcedGround = default; }
        public void SetSurfaceConstraint(bool value) => surfaceConstrained = value;
        public void SetCollisionFrame(bool aligned, Vector3 up, Vector3 forward)
        {
            Quaternion rotation = aligned && up.sqrMagnitude > .5f && forward.sqrMagnitude > .5f
                ? Quaternion.LookRotation(forward.normalized, up.normalized)
                : defaultRootRotation;
            bool enabledBefore = controller != null && controller.enabled;
            if (enabledBefore) controller.enabled = false;
            transform.rotation = rotation;
            if (enabledBefore) controller.enabled = true;
        }
        public void MoveConstrained(Vector3 targetPosition)
        {
            if (!CanMove()) return;
            controller.Move(targetPosition - transform.position);
        }
        public Vector3 SurfaceUp => followSurfaceForward ? forcedSurfaceUp :
            result.Ground.IsGrounded && result.Ground.Normal.sqrMagnitude > .5f
            ? result.Ground.Normal.normalized : UpDirection;
        public Vector3 SurfaceForward
        {
            get
            {
                Vector3 direction = Vector3.ProjectOnPlane(
                    followSurfaceForward ? forcedSurfaceForward : surfaceForward, SurfaceUp);
                if (direction.sqrMagnitude < .001f)
                    direction = Vector3.ProjectOnPlane(transform.right, SurfaceUp);
                return direction.sqrMagnitude > .001f ? direction.normalized : transform.right;
            }
        }

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            defaultRootRotation = transform.rotation;
            abilities = GetComponent<CharacterAbilityController>();
            animationPlayer = GetComponent<UnityPlayableAnimationPlayer>();
            gravityDirection = NormalizeGravity(defaultGravityDirection);
            checkpoints ??= new CharacterCheckpointService();
            checkpoints.Configure(transform, this);
            abilities?.Context?.Bind<ICharacterCheckpointService>(checkpoints);
            commands.Reset();
            result = new CharacterMotorResult(Vector3.zero, default, default, false, 0f, 0f);
            InitializeGroundContact();
        }

        void Update()
        {
            if (abilities != null && abilities.IsAbilityLocked && !abilities.SimulateMotorWhileLocked) return;
            StepMotor(Time.deltaTime);
            abilities?.OnMotorStepped(result);
            checkpoints.Observe(result);
            commands.Reset();
        }

        void InitializeGroundContact()
        {
            if (controller == null) return;
            if (!CharacterGroundPlacement.PlaceOnGround(transform, controller,
                    minimumGroundNormal: minimumGroundNormal)) return;
            GroundContact ground = ProbeGround(true);
            result = new CharacterMotorResult(Vector3.zero, ground, default,
                ground.IsGrounded, 0f, 0f);
        }

        public void ReinitializeGroundContact() => InitializeGroundContact();

        public void StepMotor(float deltaTime)
        {
            if (!CanMove()) return;
            gravityDirection = commands.HasGravityDirection
                ? NormalizeGravity(commands.GravityDirection)
                : NormalizeGravity(defaultGravityDirection);
            Vector3 up = UpDirection;
            bool wasGrounded = result.Ground.IsGrounded;

            if (surfaceConstrained && hasForcedGround)
            {
                velocity = Vector3.zero;
                result = new CharacterMotorResult(Vector3.zero, forcedGround, default,
                    wasGrounded, 0f, 0f);
                return;
            }

            if (wasGrounded && result.Ground.Collider != null)
            {
                ICharacterMovingPlatform support = result.Ground.Collider
                    .GetComponentInParent(typeof(ICharacterMovingPlatform)) as ICharacterMovingPlatform;
                if (support != null && support.FrameDelta.sqrMagnitude > 0f)
                {
                    controller.Move(support.FrameDelta);
                    if (!CanMove()) return;
                }
            }

            if (commands.HasHorizontalTarget)
            {
                Vector3 horizontalAxis = GetHorizontalAxis(up);
                float horizontalSpeed = Vector3.Dot(velocity, horizontalAxis);
                bool hasInput = Mathf.Abs(commands.HorizontalTarget) > .001f;
                bool reversing = hasInput && Mathf.Abs(horizontalSpeed) > .08f &&
                                 Mathf.Sign(horizontalSpeed) != Mathf.Sign(commands.HorizontalTarget);
                float acceleration;
                if (wasGrounded)
                    acceleration = !hasInput ? commands.GroundDeceleration :
                        reversing ? commands.GroundTurnAcceleration : commands.GroundAcceleration;
                else
                    acceleration = !hasInput ? commands.AirDeceleration :
                        reversing ? commands.AirTurnAcceleration : commands.AirAcceleration;
                float nextHorizontalSpeed = Mathf.MoveTowards(horizontalSpeed,
                    commands.HorizontalTarget, acceleration * deltaTime);
                velocity += horizontalAxis * (nextHorizontalSpeed - horizontalSpeed);
            }

            if (commands.HasVerticalOverride) SetUpwardSpeed(commands.VerticalOverride);
            velocity += commands.AdditiveImpulse;
            float upwardSpeed = Vector3.Dot(velocity, up);
            if (!wasGrounded || upwardSpeed > 0f)
            {
                float gravity = commands.Gravity > 0f ? commands.Gravity : defaultGravity;
                float gravityMultiplier = commands.Gravity > 0f
                    ? Mathf.Max(0f, commands.GravityMultiplier)
                    : 1f;
                float maximumFallSpeed = commands.MaximumFallSpeed > 0f
                    ? commands.MaximumFallSpeed
                    : defaultMaximumFallSpeed;
                float nextUpwardSpeed = Mathf.Max(-maximumFallSpeed,
                    upwardSpeed - gravity * gravityMultiplier * deltaTime);
                velocity += up * (nextUpwardSpeed - upwardSpeed);
            }

            float fallingSpeedBeforeMove = Mathf.Max(0f, -Vector3.Dot(velocity, up));
            if (!CanMove()) return;
            CollisionFlags flags = controller.Move(velocity * deltaTime);
            if (!CanMove()) return;
            GroundContact ground = ProbeGround((flags & CollisionFlags.Below) != 0);
            if (hasForcedGround) ground = forcedGround;
            ICharacterGroundOverride groundOverride = GetComponent(typeof(ICharacterGroundOverride))
                as ICharacterGroundOverride;
            if (groundOverride != null && groundOverride.TryGetGround(out GroundContact overriddenGround))
                ground = overriddenGround;
            else
            {
                Vector3 movementForward = Vector3.ProjectOnPlane(velocity, ground.IsGrounded
                    ? ground.Normal : up);
                if (movementForward.sqrMagnitude > .001f) surfaceForward = movementForward.normalized;
            }
            WallContact wall = ground.IsGrounded ? default : ProbeWall();

            float completedAirTime = airTime;
            airTime = ground.IsGrounded ? 0f : airTime + deltaTime;
            if (ground.IsGrounded && Vector3.Dot(velocity, up) < 0f) SetUpwardSpeed(-2f);
            result = new CharacterMotorResult(velocity, ground, wall, wasGrounded,
                ground.IsGrounded ? completedAirTime : airTime, fallingSpeedBeforeMove);
        }

        bool CanMove() => controller != null && controller.enabled &&
            controller.gameObject.activeInHierarchy && isActiveAndEnabled;

        GroundContact ProbeGround(bool collisionBelow)
        {
            Vector3 up = UpDirection;
            Vector3 down = gravityDirection;
            if (Vector3.Dot(velocity, up) > .12f) return default;
            float radius = controller.radius * .78f;
            // Start above small procedural surface offsets. A sphere cast that starts
            // already overlapping concrete returns no hit, which previously left the
            // character permanently "airborne" at spawn until a knockback made her land.
            const float probeLift = .1f;
            Vector3 feet = CharacterGroundPlacement.GetSupportPoint(transform, controller, up);
            Vector3 origin = feet + up * (radius + probeLift);
            if (Physics.SphereCast(origin, radius, down, out RaycastHit hit,
                    probeLift + groundProbeDistance, ~0, QueryTriggerInteraction.Ignore) &&
                hit.collider != controller && !hit.transform.IsChildOf(transform) &&
                Vector3.Dot(hit.normal, up) >= minimumGroundNormal)
                return new GroundContact(true, hit.collider, hit.point, hit.normal);

            if (collisionBelow)
                return new GroundContact(true, null, transform.position, up);
            return default;
        }

        WallContact ProbeWall()
        {
            Vector3 up = UpDirection;
            Vector3 horizontalAxis = GetHorizontalAxis(up);
            float radius = controller.radius * .82f;
            Vector3 center = transform.TransformPoint(controller.center);
            float halfSegment = Mathf.Max(0f, controller.height * .5f - controller.radius - .08f);
            Vector3 bottom = center - up * halfSegment;
            Vector3 top = center + up * halfSegment;
            WallContact left = CastWall(bottom, top, radius, -horizontalAxis, up);
            WallContact right = CastWall(bottom, top, radius, horizontalAxis, up);
            if (!left.IsTouching) return right;
            if (!right.IsTouching) return left;
            return Vector3.Dot(velocity, horizontalAxis) >= 0f ? right : left;
        }

        WallContact CastWall(Vector3 bottom, Vector3 top, float radius, Vector3 direction,
            Vector3 up)
        {
            if (!Physics.CapsuleCast(bottom, top, radius, direction, out RaycastHit hit,
                    wallProbeDistance, ~0, QueryTriggerInteraction.Ignore) ||
                hit.collider == controller || hit.transform.IsChildOf(transform) ||
                Mathf.Abs(Vector3.Dot(hit.normal, up)) > maximumWallNormalY ||
                Mathf.Abs(Vector3.Dot(hit.normal, direction)) < .55f)
                return default;
            return new WallContact(true, hit.collider, hit.point, hit.normal);
        }

        public void ResetMotor()
        {
            velocity = Vector3.zero;
            airTime = 0f;
            commands.Reset();
            result = new CharacterMotorResult(Vector3.zero, default, default, false, 0f, 0f);
        }

        public void SetVelocity(Vector3 value) => velocity = value;
        public void SetVerticalVelocity(float value) => SetUpwardSpeed(value);
        public void SetUpwardSpeed(float value)
        {
            Vector3 up = UpDirection;
            velocity += up * (value - Vector3.Dot(velocity, up));
        }

        static Vector3 NormalizeGravity(Vector3 value) => value.sqrMagnitude > .0001f
            ? value.normalized : Vector3.down;

        Vector3 GetHorizontalAxis(Vector3 up)
        {
            Vector3 planeNormal = movementPlaneNormal.sqrMagnitude > .001f
                ? movementPlaneNormal.normalized : Vector3.forward;
            Vector3 axis = Vector3.Cross(up, planeNormal);
            if (axis.sqrMagnitude < .001f) axis = Vector3.ProjectOnPlane(Vector3.right, up);
            if (axis.sqrMagnitude < .001f) axis = Vector3.Cross(up, Vector3.up);
            return axis.sqrMagnitude > .001f ? axis.normalized : Vector3.right;
        }

        public void ApplyPlayablePostProcess()
        {
            if (visualRoot == null) return;
            Vector3 up = SurfaceUp;
            Vector3 horizontalAxis = GetHorizontalAxis(up);
            float horizontalSpeed = Vector3.Dot(velocity, horizontalAxis);
            if (!followSurfaceForward && Mathf.Abs(horizontalSpeed) > facingDeadZone)
                facingSign = Mathf.Sign(horizontalSpeed);
            Vector3 forward = followSurfaceForward
                ? SurfaceForward
                : horizontalAxis * facingSign;
            if (forward.sqrMagnitude < .001f) forward = SurfaceForward * facingSign;
            if (forward.sqrMagnitude < .001f) return;
            float playableFacingOffset = animationPlayer?.Context
                .GetFloat("PlayableFacingOffset") ?? 0f;
            Quaternion target = Quaternion.AngleAxis(playableFacingOffset, up) *
                                Quaternion.LookRotation(forward.normalized, up);
            visualRoot.rotation = followSurfaceForward
                ? target
                : Quaternion.Slerp(visualRoot.rotation, target,
                    1f - Mathf.Exp(-surfaceAlignmentSharpness * Time.deltaTime));
        }

        public void Teleport(Vector3 position)
        {
            bool enabledBefore = controller != null && controller.enabled;
            if (enabledBefore) controller.enabled = false;
            transform.position = position;
            if (enabledBefore) controller.enabled = true;
            velocity = Vector3.zero;
            airTime = 0f;
            result = new CharacterMotorResult(Vector3.zero, default, default, false, 0f, 0f);
        }
    }
}
