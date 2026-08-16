using UnityEngine;

namespace GameSystems.Rideables
{
    [DisallowMultipleComponent]
    public sealed class RailFlightPathConstraint : MonoBehaviour
    {
        [SerializeField] RailFlightPath path;
        [SerializeField, Min(.1f)] float forwardSpeed = 15f;
        [SerializeField] Vector2 flightExtents = new(4.8f, 3.2f);
        [SerializeField, Min(.01f)] float inputSharpness = 11f;
        [SerializeField] Vector2 flightAcceleration = new(18f, 15f);
        [SerializeField, Min(0f)] float velocityDamping = 1.25f;
        [SerializeField, Min(.1f)] float maximumOffsetSpeed = 9f;
        [SerializeField, Range(.05f, .5f)] float boundarySoftZone = .2f;
        [SerializeField, Min(.1f)] float boundarySpring = 24f;
        [SerializeField, Range(0f, 70f)] float maximumRoll = 38f;
        [SerializeField, Range(0f, 60f)] float climbPitch = 34f;
        [SerializeField, Range(0f, 60f)] float divePitch = 42f;
        [SerializeField, Min(.01f)] float leanSmoothTime = .14f;
        [SerializeField, Tooltip("Yaw correction between the authored model forward and the character forward.")]
        float visualForwardOffset;
        [Header("Editor preview")]
        [SerializeField] bool showFlightPlan = true;
        [SerializeField, Range(6, 48)] int previewSections = 20;
        [SerializeField] Color previewColor = new(.15f, .8f, 1f, .7f);
        Vector2 filteredInput;
        Vector2 offset;
        Vector2 offsetVelocity;
        float speedMultiplier = 1f;
        float distance;
        float visualPitch;
        float visualPitchVelocity;
        float visualRoll;
        float visualRollVelocity;
        bool flightStarted;

        public float Distance => distance;
        public RailFlightPath Path => path;
        public Vector2 FlightExtents => flightExtents;
        public float ForwardSpeed => forwardSpeed;
        public Vector2 FlightAcceleration => flightAcceleration;
        public float MaximumOffsetSpeed => maximumOffsetSpeed;
        public bool FlightStarted => flightStarted;
        public bool Finished => flightStarted && path != null && path.Length > .01f &&
                                distance >= path.Length;
        public Vector2 FilteredInput => filteredInput;
        public float SpeedMultiplier { get => speedMultiplier; set => speedMultiplier = Mathf.Max(0f, value); }
        public void Configure(RailFlightPath value, float speed = 15f,
            Vector2 extents = default, float inputResponse = 4f,
            Vector2 acceleration = default, float damping = 1.25f,
            float lateralSpeed = 9f, float softZone = .2f, float spring = 24f)
        {
            path = value;
            forwardSpeed = Mathf.Max(.1f, speed);
            if (extents.sqrMagnitude > .001f) flightExtents = extents;
            inputSharpness = Mathf.Max(.01f, inputResponse);
            if (acceleration.sqrMagnitude > .001f) flightAcceleration = acceleration;
            velocityDamping = Mathf.Max(0f, damping);
            maximumOffsetSpeed = Mathf.Max(.1f, lateralSpeed);
            boundarySoftZone = Mathf.Clamp(softZone, .05f, .5f);
            boundarySpring = Mathf.Max(.1f, spring);
        }

        public void ConfigureAircraftLean(float roll, float climb, float dive,
            float smoothTime)
        {
            maximumRoll = Mathf.Clamp(roll, 0f, 70f);
            climbPitch = Mathf.Clamp(climb, 0f, 60f);
            divePitch = Mathf.Clamp(dive, 0f, 60f);
            leanSmoothTime = Mathf.Max(.01f, smoothTime);
        }

        public void ConfigureVisualForwardOffset(float yawDegrees)
        {
            visualForwardOffset = yawDegrees;
        }

        public bool Step(Vector2 input, float deltaTime, out Vector3 position,
            out Vector3 forward, out Vector3 up, out Quaternion visualLean)
        {
            if (!flightStarted || path == null || Finished)
            {
                position = transform.position; forward = transform.forward;
                up = transform.up; visualLean = Quaternion.identity; return false;
            }
            filteredInput = Vector2.Lerp(filteredInput, Vector2.ClampMagnitude(input, 1f),
                1f - Mathf.Exp(-inputSharpness * deltaTime));
            offsetVelocity += Vector2.Scale(filteredInput, flightAcceleration) * deltaTime;
            offsetVelocity *= Mathf.Exp(-velocityDamping * deltaTime);
            ApplySoftBoundary(ref offsetVelocity.x, offset.x, flightExtents.x, deltaTime);
            ApplySoftBoundary(ref offsetVelocity.y, offset.y, flightExtents.y, deltaTime);
            offsetVelocity = Vector2.ClampMagnitude(offsetVelocity, maximumOffsetSpeed);
            offset += offsetVelocity * deltaTime;
            ClampBoundary(ref offset.x, ref offsetVelocity.x, flightExtents.x);
            ClampBoundary(ref offset.y, ref offsetVelocity.y, flightExtents.y);
            distance = Mathf.Min(path.Length, distance + forwardSpeed * speedMultiplier * deltaTime);
            path.EvaluateDistance(distance, out Vector3 center, out forward, out up);
            Vector3 right = Vector3.Cross(up, forward).normalized;
            position = center + right * offset.x + up * offset.y;
            float targetPitch = filteredInput.y >= 0f
                ? -filteredInput.y * climbPitch
                : -filteredInput.y * divePitch;
            float targetRoll = -filteredInput.x * maximumRoll;
            visualPitch = Mathf.SmoothDampAngle(visualPitch, targetPitch,
                ref visualPitchVelocity, leanSmoothTime, Mathf.Infinity, deltaTime);
            visualRoll = Mathf.SmoothDampAngle(visualRoll, targetRoll,
                ref visualRollVelocity, leanSmoothTime, Mathf.Infinity, deltaTime);

            // Apply the authored-facing correction first, then aircraft pitch/bank in
            // the character frame so model orientation cannot rotate the control axes.
            visualLean = Quaternion.Euler(visualPitch, 0f, visualRoll) *
                         Quaternion.Euler(0f, visualForwardOffset, 0f);
            return true;
        }

        void ApplySoftBoundary(ref float velocity, float position, float extent,
            float deltaTime)
        {
            extent = Mathf.Max(.01f, extent);
            float boundaryStart = extent * (1f - boundarySoftZone);
            float penetration = Mathf.Abs(position) - boundaryStart;
            if (penetration <= 0f) return;

            float direction = Mathf.Sign(position);
            float normalized = Mathf.Clamp01(penetration / (extent - boundaryStart));
            float pressure = normalized * normalized;
            velocity -= direction * boundarySpring * pressure * deltaTime;

            // Absorb only velocity travelling farther out. Tangential/free movement
            // and inward counter-steering remain fully under player control.
            if (velocity * direction > 0f)
                velocity *= Mathf.Exp(-boundarySpring * .12f * normalized * deltaTime);
        }

        static void ClampBoundary(ref float position, ref float velocity, float extent)
        {
            extent = Mathf.Max(.01f, extent);
            if (position > extent)
            {
                position = extent;
                if (velocity > 0f) velocity = 0f;
            }
            else if (position < -extent)
            {
                position = -extent;
                if (velocity < 0f) velocity = 0f;
            }
        }

        public bool BeginFlight()
        {
            if (path == null) return false;
            path.RebuildCache();
            if (path.Length <= .01f) return false;
            filteredInput = offset = offsetVelocity = Vector2.zero;
            visualPitch = visualPitchVelocity = visualRoll = visualRollVelocity = 0f;
            speedMultiplier = 1f;
            distance = 0f;
            flightStarted = true;
            return true;
        }

        public void ResetFlight()
        {
            filteredInput = offset = offsetVelocity = Vector2.zero;
            visualPitch = visualPitchVelocity = visualRoll = visualRollVelocity = 0f;
            speedMultiplier = 1f;
            distance = 0f;
            flightStarted = false;
        }

        void OnDrawGizmos()
        {
            if (!showFlightPlan || path == null) return;
            path.RebuildCache();
            if (path.Length <= .01f) return;

            int sections = Mathf.Max(2, previewSections);
            Vector3 previousCenter = default;
            Vector3 previousLeft = default;
            Vector3 previousRight = default;
            Vector3 previousTop = default;
            Vector3 previousBottom = default;
            for (int i = 0; i <= sections; i++)
            {
                float planDistance = path.Length * i / sections;
                path.EvaluateDistance(planDistance, out Vector3 center,
                    out Vector3 forward, out Vector3 up);
                Vector3 right = Vector3.Cross(up, forward).normalized;
                Vector3 leftPoint = center - right * flightExtents.x;
                Vector3 rightPoint = center + right * flightExtents.x;
                Vector3 topPoint = center + up * flightExtents.y;
                Vector3 bottomPoint = center - up * flightExtents.y;

                Gizmos.color = new Color(previewColor.r, previewColor.g, previewColor.b, .38f);
                Gizmos.DrawLine(leftPoint, topPoint);
                Gizmos.DrawLine(topPoint, rightPoint);
                Gizmos.DrawLine(rightPoint, bottomPoint);
                Gizmos.DrawLine(bottomPoint, leftPoint);
                if (i > 0)
                {
                    Gizmos.DrawLine(previousLeft, leftPoint);
                    Gizmos.DrawLine(previousRight, rightPoint);
                    Gizmos.DrawLine(previousTop, topPoint);
                    Gizmos.DrawLine(previousBottom, bottomPoint);
                    Gizmos.color = previewColor;
                    Gizmos.DrawLine(previousCenter, center);
                }

                if (i < sections && i % 3 == 0)
                {
                    float arrowSize = Mathf.Min(flightExtents.x, flightExtents.y) * .22f;
                    Vector3 arrowTip = center + forward * arrowSize;
                    Gizmos.color = previewColor;
                    Gizmos.DrawLine(center, arrowTip);
                    Gizmos.DrawLine(arrowTip, arrowTip - forward * arrowSize * .45f + right * arrowSize * .3f);
                    Gizmos.DrawLine(arrowTip, arrowTip - forward * arrowSize * .45f - right * arrowSize * .3f);
                }

                previousCenter = center;
                previousLeft = leftPoint;
                previousRight = rightPoint;
                previousTop = topPoint;
                previousBottom = bottomPoint;
            }

            path.EvaluateDistance(0f, out Vector3 start, out _, out _);
            path.EvaluateDistance(path.Length, out Vector3 end, out _, out _);
            Gizmos.color = new Color(.2f, 1f, .45f, .9f);
            Gizmos.DrawSphere(start, .35f);
            Gizmos.color = new Color(1f, .25f, .75f, .9f);
            Gizmos.DrawSphere(end, .35f);
        }
    }
}
