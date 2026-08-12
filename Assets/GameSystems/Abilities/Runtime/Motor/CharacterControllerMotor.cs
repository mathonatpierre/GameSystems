using UnityEngine;

namespace GameSystems.Abilities
{
    [DefaultExecutionOrder(-200)]
    [RequireComponent(typeof(CharacterController))]
    public sealed class CharacterControllerMotor : MonoBehaviour, ICharacterMotor, ICharacterMotorControl
    {
        [SerializeField, Min(0f)] float groundProbeDistance = .045f;
        [SerializeField, Range(0f, 1f)] float minimumGroundNormal = .62f;
        [SerializeField, Min(0f)] float wallProbeDistance = .09f;
        [SerializeField, Range(0f, 1f)] float maximumWallNormalY = .28f;
        [SerializeField, Min(0f), Tooltip("Gravity used when no active action supplies one.")]
        float defaultGravity = 18f;
        [SerializeField, Min(0f)] float defaultMaximumFallSpeed = 24f;

        CharacterController controller;
        CharacterMotorCommands commands;
        CharacterMotorResult result;
        Vector3 velocity;
        float airTime;
        CharacterAbilityController abilities;
        [SerializeField] CharacterCheckpointService checkpoints = new();

        public CharacterMotorCommands Commands { get => commands; set => commands = value; }
        public CharacterMotorResult Result => result;
        public Vector3 Velocity => velocity;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            abilities = GetComponent<CharacterAbilityController>();
            checkpoints ??= new CharacterCheckpointService();
            checkpoints.Configure(transform, this);
            abilities?.Context?.Bind<ICharacterCheckpointService>(checkpoints);
            commands.Reset();
            result = new CharacterMotorResult(Vector3.zero, default, default, false, 0f, 0f);
            InitializeGroundContact();
        }

        // Run a second ground initialization after every scene object's Awake. This
        // covers platforms generated during another component's initialization.
        void Start() => InitializeGroundContact();

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

            // A freshly loaded character used to start with its feet exactly on the
            // platform plane. Depending on the first physics update this could be
            // interpreted as either overlap or no contact, rejecting Jump until an
            // external impulse lifted Lennie and made her land again.
            bool wasEnabled = controller.enabled;
            controller.enabled = false;
            Physics.SyncTransforms();

            Vector3 feet = transform.position + Vector3.up *
                (controller.center.y - controller.height * .5f);
            RaycastHit[] hits = Physics.RaycastAll(feet + Vector3.up * .6f,
                Vector3.down, 1.25f, ~0, QueryTriggerInteraction.Ignore);
            RaycastHit nearest = default;
            float nearestDistance = float.PositiveInfinity;
            for (int i = 0; i < hits.Length; i++)
            {
                RaycastHit hit = hits[i];
                if (hit.collider == null || hit.transform.IsChildOf(transform) ||
                    hit.normal.y < minimumGroundNormal || hit.distance >= nearestDistance)
                    continue;
                nearest = hit;
                nearestDistance = hit.distance;
            }

            if (nearest.collider != null)
            {
                const float skinClearance = .025f;
                float feetOffset = controller.center.y - controller.height * .5f;
                Vector3 position = transform.position;
                position.y = nearest.point.y - feetOffset + skinClearance;
                transform.position = position;
                result = new CharacterMotorResult(Vector3.zero,
                    new GroundContact(true, nearest.collider, nearest.point, nearest.normal),
                    default, true, 0f, 0f);
            }

            controller.enabled = wasEnabled;
            Physics.SyncTransforms();
        }

        public void ReinitializeGroundContact() => InitializeGroundContact();

        public void StepMotor(float deltaTime)
        {
            if (!CanMove()) return;
            bool wasGrounded = result.Ground.IsGrounded;

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
                bool hasInput = Mathf.Abs(commands.HorizontalTarget) > .001f;
                bool reversing = hasInput && Mathf.Abs(velocity.x) > .08f &&
                                 Mathf.Sign(velocity.x) != Mathf.Sign(commands.HorizontalTarget);
                float acceleration;
                if (wasGrounded)
                    acceleration = !hasInput ? commands.GroundDeceleration :
                        reversing ? commands.GroundTurnAcceleration : commands.GroundAcceleration;
                else
                    acceleration = !hasInput ? commands.AirDeceleration :
                        reversing ? commands.AirTurnAcceleration : commands.AirAcceleration;
                velocity.x = Mathf.MoveTowards(velocity.x, commands.HorizontalTarget, acceleration * deltaTime);
            }

            if (commands.HasVerticalOverride) velocity.y = commands.VerticalOverride;
            velocity += commands.AdditiveImpulse;
            if (!wasGrounded || velocity.y > 0f)
            {
                float gravity = commands.Gravity > 0f ? commands.Gravity : defaultGravity;
                float gravityMultiplier = commands.Gravity > 0f
                    ? Mathf.Max(0f, commands.GravityMultiplier)
                    : 1f;
                float maximumFallSpeed = commands.MaximumFallSpeed > 0f
                    ? commands.MaximumFallSpeed
                    : defaultMaximumFallSpeed;
                velocity.y = Mathf.Max(-maximumFallSpeed,
                    velocity.y - gravity * gravityMultiplier * deltaTime);
            }

            float fallingSpeedBeforeMove = Mathf.Max(0f, -velocity.y);
            if (!CanMove()) return;
            CollisionFlags flags = controller.Move(velocity * deltaTime);
            if (!CanMove()) return;
            GroundContact ground = ProbeGround((flags & CollisionFlags.Below) != 0);
            WallContact wall = ground.IsGrounded ? default : ProbeWall();

            float completedAirTime = airTime;
            airTime = ground.IsGrounded ? 0f : airTime + deltaTime;
            if (ground.IsGrounded && velocity.y < 0f) velocity.y = -2f;
            result = new CharacterMotorResult(velocity, ground, wall, wasGrounded,
                ground.IsGrounded ? completedAirTime : airTime, fallingSpeedBeforeMove);
        }

        bool CanMove() => controller != null && controller.enabled &&
            controller.gameObject.activeInHierarchy && isActiveAndEnabled;

        GroundContact ProbeGround(bool collisionBelow)
        {
            if (velocity.y > .12f) return default;
            float radius = controller.radius * .78f;
            // Start above small procedural surface offsets. A sphere cast that starts
            // already overlapping concrete returns no hit, which previously left the
            // character permanently "airborne" at spawn until a knockback made her land.
            const float probeLift = .1f;
            Vector3 feet = transform.position + Vector3.up *
                (controller.center.y - controller.height * .5f);
            Vector3 origin = feet + Vector3.up * (radius + probeLift);
            if (Physics.SphereCast(origin, radius, Vector3.down, out RaycastHit hit,
                    probeLift + groundProbeDistance, ~0, QueryTriggerInteraction.Ignore) &&
                hit.collider != controller && !hit.transform.IsChildOf(transform) &&
                hit.normal.y >= minimumGroundNormal)
                return new GroundContact(true, hit.collider, hit.point, hit.normal);

            if (collisionBelow)
                return new GroundContact(true, null, transform.position, Vector3.up);
            return default;
        }

        WallContact ProbeWall()
        {
            float radius = controller.radius * .82f;
            Vector3 center = transform.position + controller.center;
            float halfSegment = Mathf.Max(0f, controller.height * .5f - controller.radius - .08f);
            Vector3 bottom = center - Vector3.up * halfSegment;
            Vector3 top = center + Vector3.up * halfSegment;
            WallContact left = CastWall(bottom, top, radius, Vector3.left);
            WallContact right = CastWall(bottom, top, radius, Vector3.right);
            if (!left.IsTouching) return right;
            if (!right.IsTouching) return left;
            return velocity.x >= 0f ? right : left;
        }

        WallContact CastWall(Vector3 bottom, Vector3 top, float radius, Vector3 direction)
        {
            if (!Physics.CapsuleCast(bottom, top, radius, direction, out RaycastHit hit,
                    wallProbeDistance, ~0, QueryTriggerInteraction.Ignore) ||
                hit.collider == controller || hit.transform.IsChildOf(transform) ||
                Mathf.Abs(hit.normal.y) > maximumWallNormalY || Mathf.Abs(hit.normal.x) < .55f)
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
        public void SetVerticalVelocity(float value) => velocity.y = value;

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
