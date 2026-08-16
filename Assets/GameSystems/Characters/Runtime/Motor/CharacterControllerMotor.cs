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
        IPlayablePostProcessor, IPlayableRootMotionReceiver
        , ICharacterLedgeMotor, ICharacterMovementPlane
    {
        public int Order => 100;

        [SerializeField, Min(0f)] float groundProbeDistance = .045f;
        [SerializeField, Range(0f, 1f)] float minimumGroundNormal = .62f;
        [SerializeField, Min(0f)] float wallProbeDistance = .09f;
        [SerializeField, Range(0f, 1f)] float maximumWallNormalY = .28f;
        [SerializeField, Range(.15f, .75f), Tooltip("Minimum body height that must still face a continuous wall.")]
        float minimumWallGripHeight = .34f;
        [SerializeField, Min(0f), Tooltip("Keeps a valid wall contact stable across brief probe gaps.")]
        float wallContactGrace = .1f;
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
        [Header("Ledge Detection")]
        [SerializeField, Min(.05f)] float ledgeReach = .5f;
        [SerializeField, Range(.35f, .95f)] float ledgeChestHeight = .68f;
        [SerializeField, Min(.05f)] float ledgeTopProbeHeight = .48f;
        [SerializeField, Range(.45f, 1f)] float ledgeHangHeight = .68f;
        [SerializeField, Min(0f)] float ledgeStandInset = .22f;
        [SerializeField, Min(.1f), Tooltip("Maximum speed used to attract the animated hands to a detected ledge.")]
        float ledgeMagnetSpeed = 4.5f;
        [SerializeField] Transform ledgeLeftHand;
        [SerializeField] Transform ledgeRightHand;
        [SerializeField] Transform ledgeLeftFoot;
        [SerializeField] Transform ledgeRightFoot;

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
        Quaternion surfaceVisualOffset = Quaternion.identity;
        GroundContact forcedGround;
        bool hasForcedGround;
        bool surfaceConstrained;
        bool ledgeAnchored;
        bool ledgeClimbing;
        float ledgeClimbProgress;
        Vector3 ledgeClimbFootCorrection;
        CharacterLedgeAnchor ledgeAnchor;
        WallContact recentWallContact;
        float wallContactGraceRemaining;
        Quaternion defaultRootRotation;
        float airTime;
        CharacterAbilityController abilities;
        UnityPlayableAnimationPlayer animationPlayer;
        [SerializeField] CharacterCheckpointService checkpoints = new();

        public CharacterMotorCommands Commands { get => commands; set => commands = value; }
        public CharacterMotorResult Result => result;
        public Vector3 Velocity => velocity;
        public Vector3 MovementPlaneNormal => movementPlaneNormal.sqrMagnitude > .001f
            ? movementPlaneNormal.normalized : Vector3.forward;
        public Vector3 GravityDirection => gravityDirection;
        public Vector3 UpDirection => -gravityDirection;
        public float UpwardSpeed => Vector3.Dot(velocity, UpDirection);
        public bool IsLedgeAnchored => ledgeAnchored;
        public CharacterLedgeAnchor LedgeAnchor => ledgeAnchor;
        public void ConfigureVisualRoot(Transform value) => visualRoot = value;
        public void SetFollowSurfaceForward(bool value) => followSurfaceForward = value;
        public void SetSurfaceFrame(Vector3 up, Vector3 forward)
        {
            if (up.sqrMagnitude > .5f) forcedSurfaceUp = up.normalized;
            if (forward.sqrMagnitude > .5f) forcedSurfaceForward = forward.normalized;
        }
        public void SetSurfaceVisualOffset(Quaternion offset) => surfaceVisualOffset = offset;
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
            Animator humanoid = GetComponentInChildren<Animator>();
            if (humanoid != null && humanoid.isHuman)
            {
                if (ledgeLeftHand == null)
                    ledgeLeftHand = humanoid.GetBoneTransform(HumanBodyBones.LeftHand);
                if (ledgeRightHand == null)
                    ledgeRightHand = humanoid.GetBoneTransform(HumanBodyBones.RightHand);
                if (ledgeLeftFoot == null)
                    ledgeLeftFoot = humanoid.GetBoneTransform(HumanBodyBones.LeftFoot);
                if (ledgeRightFoot == null)
                    ledgeRightFoot = humanoid.GetBoneTransform(HumanBodyBones.RightFoot);
            }
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
            if (ledgeAnchored)
            {
                velocity = Vector3.zero;
                result = new CharacterMotorResult(Vector3.zero, default, default,
                    result.Ground.IsGrounded, result.AirTime, 0f);
                return;
            }
            gravityDirection = commands.HasGravityDirection
                ? NormalizeGravity(commands.GravityDirection)
                : NormalizeGravity(defaultGravityDirection);
            Vector3 up = UpDirection;
            bool wasGrounded = result.Ground.IsGrounded;

            float requestedUpwardSpeed = commands.HasVerticalOverride
                ? commands.VerticalOverride
                : Vector3.Dot(velocity + commands.AdditiveImpulse, up);
            bool launchingFromSurface = requestedUpwardSpeed > .08f;
            if (surfaceConstrained && hasForcedGround && !launchingFromSurface)
            {
                velocity = Vector3.zero;
                result = new CharacterMotorResult(Vector3.zero, forcedGround, default,
                    wasGrounded, 0f, 0f);
                return;
            }
            if (launchingFromSurface)
            {
                surfaceConstrained = false;
                ClearSurfaceGround();
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
            if (wall.IsTouching)
            {
                recentWallContact = wall;
                wallContactGraceRemaining = wallContactGrace;
            }
            else if (!ground.IsGrounded && wallContactGraceRemaining > 0f)
            {
                wallContactGraceRemaining -= deltaTime;
                wall = recentWallContact;
            }
            else
            {
                recentWallContact = default;
                wallContactGraceRemaining = 0f;
            }

            float completedAirTime = airTime;
            airTime = ground.IsGrounded ? 0f : airTime + deltaTime;
            if (ground.IsGrounded && Vector3.Dot(velocity, up) < 0f) SetUpwardSpeed(-2f);
            result = new CharacterMotorResult(velocity, ground, wall, wasGrounded,
                ground.IsGrounded ? completedAirTime : airTime, fallingSpeedBeforeMove);
        }

        public bool TryFindLedge(out CharacterLedgeAnchor anchor)
        {
            anchor = default;
            if (!CanMove() || result.Ground.IsGrounded) return false;
            Vector3 up = UpDirection;
            Vector3 forward = GetHorizontalAxis(up) * facingSign;
            float height = controller.height * Mathf.Abs(controller.transform.lossyScale.y);
            float radius = controller.radius * Mathf.Max(
                Mathf.Abs(controller.transform.lossyScale.x), Mathf.Abs(controller.transform.lossyScale.z));
            Vector3 feet = transform.position + transform.rotation * controller.center - up * height * .5f;
            Vector3 chest = feet + up * (height * ledgeChestHeight);
            if (!Physics.SphereCast(chest, radius * .72f, forward, out RaycastHit wall,
                    ledgeReach, ~0, QueryTriggerInteraction.Ignore) ||
                Mathf.Abs(Vector3.Dot(wall.normal, up)) > maximumWallNormalY) return false;
            Vector3 topOrigin = wall.point + forward * (radius + .04f) + up * ledgeTopProbeHeight;
            if (!Physics.Raycast(topOrigin, -up, out RaycastHit top,
                    ledgeTopProbeHeight * 2f, ~0, QueryTriggerInteraction.Ignore) ||
                Vector3.Dot(top.normal, up) < minimumGroundNormal) return false;
            Vector3 hang = top.point - up * (height * ledgeHangHeight) - forward * (radius + .025f);
            Vector3 stand = top.point + up * .035f + forward * (radius + ledgeStandInset);
            Vector3 gripPoint = top.point - wall.normal * .015f;
            anchor = new CharacterLedgeAnchor(top.collider, hang, stand, top.normal,
                wall.normal, gripPoint);
            return true;
        }

        public void SetLedgeAnchor(in CharacterLedgeAnchor anchor)
        {
            ledgeAnchor = anchor;
            ledgeAnchored = true;
            velocity = Vector3.zero;
        }

        public void MoveLedgeAnchor(Vector3 position)
        {
            if (!CanMove()) return;
            if (!ledgeClimbing)
            {
                controller.Move(position - transform.position);
                return;
            }

            // A mantle intentionally crosses the platform lip. CharacterController.Move
            // blocks against that lip and releases all visible motion at once when the
            // capsule clears it, which looks like a teleport. Keep the controller itself
            // on the authored path while collisions are temporarily suspended.
            controller.enabled = false;
            transform.position = position + ledgeClimbFootCorrection;
            controller.enabled = true;
        }

        public void SetLedgeClimbing(bool value)
        {
            ledgeClimbing = value;
            if (value) { ledgeClimbProgress = 0f; ledgeClimbFootCorrection = Vector3.zero; }
        }
        public void SetLedgeClimbProgress(float value) =>
            ledgeClimbProgress = Mathf.Clamp01(value);

        public void ClearLedgeAnchor()
        {
            ledgeAnchored = false;
            ledgeClimbing = false;
            ledgeClimbProgress = 0f;
            ledgeClimbFootCorrection = Vector3.zero;
            ledgeAnchor = default;
        }

        public void ApplyPlayableRootMotion(Vector3 deltaPosition, Quaternion deltaRotation)
        {
            if (!CanMove()) return;
            // Ledge climb remaps animation progress into the detected ledge frame.
            // Applying the raw Mixamo delta as well would move into the platform.
            if (ledgeAnchored) return;
            controller.Move(deltaPosition);
            if (deltaRotation != Quaternion.identity)
                transform.rotation = deltaRotation * transform.rotation;
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
            Vector3 feet = bottom - up * radius;
            float fullHeight = Mathf.Max(.01f, Vector3.Dot(top - bottom, up) + radius * 2f);
            Vector3 gripProbe = feet + up * (fullHeight * minimumWallGripHeight);
            if (!Physics.SphereCast(gripProbe, radius * .32f, direction,
                    out RaycastHit gripHit, wallProbeDistance + radius * .72f,
                    ~0, QueryTriggerInteraction.Ignore) ||
                gripHit.collider != hit.collider ||
                Mathf.Abs(Vector3.Dot(gripHit.normal, up)) > maximumWallNormalY)
                return default;
            float height01 = Vector3.Dot(gripHit.point - feet, up) / fullHeight;
            return new WallContact(true, gripHit.collider, gripHit.point,
                gripHit.normal, height01);
        }

        public void ResetMotor()
        {
            velocity = Vector3.zero;
            airTime = 0f;
            commands.Reset();
            result = new CharacterMotorResult(Vector3.zero, default, default, false, 0f, 0f);
            recentWallContact = default;
            wallContactGraceRemaining = 0f;
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
                                Quaternion.LookRotation(forward.normalized, up) *
                                surfaceVisualOffset;
            visualRoot.rotation = followSurfaceForward
                ? target
                : Quaternion.Slerp(visualRoot.rotation, target,
                    1f - Mathf.Exp(-surfaceAlignmentSharpness * Time.deltaTime));
            AlignHandsToLedge();
            AlignClimbFeetToLedge();
        }

        void AlignClimbFeetToLedge()
        {
            if (!ledgeAnchored || !ledgeClimbing || ledgeClimbProgress < .52f ||
                ledgeLeftFoot == null || ledgeRightFoot == null || !CanMove()) return;
            Vector3 up = ledgeAnchor.SurfaceNormal.sqrMagnitude > .5f
                ? ledgeAnchor.SurfaceNormal.normalized : UpDirection;
            float leftHeight = Vector3.Dot(ledgeLeftFoot.position - ledgeAnchor.GripPoint, up);
            float rightHeight = Vector3.Dot(ledgeRightFoot.position - ledgeAnchor.GripPoint, up);
            float lowestHeight = Mathf.Min(leftHeight, rightHeight);
            Vector3 desiredCorrection = up * -lowestHeight;
            float weight = Mathf.SmoothStep(0f, 1f,
                Mathf.InverseLerp(.52f, .76f, ledgeClimbProgress));
            desiredCorrection *= weight;
            Vector3 delta = desiredCorrection - ledgeClimbFootCorrection;
            ledgeClimbFootCorrection = desiredCorrection;
            if (delta.sqrMagnitude <= .000001f) return;
            controller.enabled = false;
            transform.position += delta;
            controller.enabled = true;
        }

        void AlignHandsToLedge()
        {
            if (!ledgeAnchored || ledgeClimbing || ledgeLeftHand == null ||
                ledgeRightHand == null || !CanMove()) return;
            Vector3 hands = (ledgeLeftHand.position + ledgeRightHand.position) * .5f;
            Vector3 correction = ledgeAnchor.GripPoint - hands;
            if (correction.sqrMagnitude <= .000001f) return;
            Vector3 magneticStep = Vector3.ClampMagnitude(correction,
                ledgeMagnetSpeed * Mathf.Max(Time.deltaTime, .001f));
            controller.Move(magneticStep);
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
