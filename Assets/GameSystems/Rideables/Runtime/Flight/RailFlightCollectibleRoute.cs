using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameSystems.Rideables
{
    [Serializable]
    public struct RailFlightCollectibleWaypoint
    {
        [Range(0f, 1f)] public float distance;
        [Tooltip("Horizontal and vertical position inside the flight corridor, from -1 to 1.")]
        public Vector2 offset;

        public RailFlightCollectibleWaypoint(float normalizedDistance, Vector2 normalizedOffset)
        {
            distance = Mathf.Clamp01(normalizedDistance);
            offset = Vector2.ClampMagnitude(normalizedOffset, 1f);
        }
    }

    [DisallowMultipleComponent]
    public sealed class RailFlightCollectibleRoute : MonoBehaviour
    {
        [SerializeField] RailFlightPathConstraint flightPlan;
        [SerializeField, Min(.25f)] float collectibleSpacing = 1.35f;
        [SerializeField] RailFlightCollectibleWaypoint[] waypoints =
        {
            new(0f, Vector2.zero),
            new(.2f, new Vector2(-.55f, .35f)),
            new(.4f, new Vector2(.55f, -.2f)),
            new(.62f, new Vector2(.25f, .65f)),
            new(.82f, new Vector2(-.5f, -.35f)),
            new(1f, Vector2.zero)
        };
        [Header("Editor preview")]
        [SerializeField] bool showRoute = true;
        [SerializeField] Color routeColor = new(1f, .72f, .12f, .95f);

        public RailFlightPathConstraint FlightPlan => flightPlan;
        public IReadOnlyList<RailFlightCollectibleWaypoint> Waypoints => waypoints;
        public float CollectibleSpacing => collectibleSpacing;

        public void GetFrame(float normalizedDistance, out Vector3 center,
            out Vector3 right, out Vector3 up)
        {
            RailFlightPath path = flightPlan.Path;
            path.RebuildCache();
            path.EvaluateDistance(path.Length * Mathf.Clamp01(normalizedDistance),
                out center, out Vector3 forward, out up);
            right = Vector3.Cross(up, forward).normalized;
        }

        public void SetWaypointOffset(int index, Vector2 normalizedOffset)
        {
            if (waypoints == null || index < 0 || index >= waypoints.Length) return;
            waypoints[index].offset = Vector2.ClampMagnitude(normalizedOffset, 1f);
        }

        public void Configure(RailFlightPathConstraint plan,
            RailFlightCollectibleWaypoint[] route, float spacing = 1.35f)
        {
            flightPlan = plan;
            waypoints = route ?? Array.Empty<RailFlightCollectibleWaypoint>();
            collectibleSpacing = Mathf.Max(.25f, spacing);
            SortWaypoints();
            ConstrainToFlightCapabilities();
        }

        public void GetCollectiblePositions(List<Vector3> output)
        {
            output.Clear();
            if (!IsValid()) return;
            float routeLength = EstimateRouteLength();
            int count = Mathf.Max(2, Mathf.FloorToInt(routeLength / collectibleSpacing) + 1);
            for (int i = 0; i < count; i++)
                output.Add(Evaluate(i / (count - 1f)));
        }

        public Vector3 Evaluate(float normalizedDistance)
        {
            if (!IsValid()) return transform.position;
            float t = Mathf.Clamp01(normalizedDistance);
            RailFlightPath path = flightPlan.Path;
            path.RebuildCache();
            path.EvaluateDistance(path.Length * t, out Vector3 center,
                out Vector3 forward, out Vector3 up);
            Vector3 right = Vector3.Cross(up, forward).normalized;
            Vector2 routeOffset = EvaluateOffset(t);
            Vector2 extents = flightPlan.FlightExtents * .82f;
            return center + right * routeOffset.x * extents.x +
                   up * routeOffset.y * extents.y;
        }

        Vector2 EvaluateOffset(float normalizedDistance)
        {
            if (waypoints.Length == 1) return waypoints[0].offset;
            int high = 1;
            while (high < waypoints.Length - 1 && waypoints[high].distance < normalizedDistance)
                high++;
            int low = Mathf.Max(0, high - 1);
            float t = Mathf.InverseLerp(waypoints[low].distance,
                waypoints[high].distance, normalizedDistance);
            t = t * t * (3f - 2f * t);
            return Vector2.Lerp(waypoints[low].offset, waypoints[high].offset, t);
        }

        float EstimateRouteLength()
        {
            const int sampleCount = 96;
            float length = 0f;
            Vector3 previous = Evaluate(0f);
            for (int i = 1; i < sampleCount; i++)
            {
                Vector3 current = Evaluate(i / (sampleCount - 1f));
                length += Vector3.Distance(previous, current);
                previous = current;
            }
            return length;
        }

        bool IsValid() => flightPlan != null && flightPlan.Path != null &&
                          waypoints != null && waypoints.Length > 0;

        void OnValidate() => SortWaypoints();

        void SortWaypoints()
        {
            if (waypoints == null) return;
            Array.Sort(waypoints, (left, right) => left.distance.CompareTo(right.distance));
            for (int i = 0; i < waypoints.Length; i++)
                waypoints[i].offset = Vector2.ClampMagnitude(waypoints[i].offset, 1f);
        }

        void ConstrainToFlightCapabilities()
        {
            if (flightPlan == null || flightPlan.Path == null || waypoints == null ||
                waypoints.Length < 2) return;

            flightPlan.Path.RebuildCache();
            float forwardSpeed = Mathf.Max(.1f, flightPlan.ForwardSpeed);
            Vector2 extents = flightPlan.FlightExtents * .82f;
            Vector2 acceleration = flightPlan.FlightAcceleration;
            float maximumSpeed = flightPlan.MaximumOffsetSpeed;
            for (int i = 1; i < waypoints.Length; i++)
            {
                float segmentDistance = flightPlan.Path.Length *
                    Mathf.Max(.001f, waypoints[i].distance - waypoints[i - 1].distance);
                float availableTime = segmentDistance / forwardSpeed;
                Vector2 previous = waypoints[i - 1].offset;
                Vector2 target = waypoints[i].offset;
                target.x = ConstrainAxis(previous.x, target.x, extents.x,
                    acceleration.x, maximumSpeed, availableTime);
                target.y = ConstrainAxis(previous.y, target.y, extents.y,
                    acceleration.y, maximumSpeed, availableTime);
                waypoints[i].offset = Vector2.ClampMagnitude(target, 1f);
            }
        }

        static float ConstrainAxis(float previous, float target, float extent,
            float acceleration, float maximumSpeed, float time)
        {
            extent = Mathf.Max(.01f, extent);
            acceleration = Mathf.Max(.01f, acceleration);
            maximumSpeed = Mathf.Max(.01f, maximumSpeed);
            float accelerationTime = Mathf.Min(time, maximumSpeed / acceleration);
            float reachable = .5f * acceleration * accelerationTime * accelerationTime +
                              maximumSpeed * Mathf.Max(0f, time - accelerationTime);
            // The interpolation eases to zero velocity at both ends. Keep some margin
            // so the visible gem line is comfortably playable rather than theoretical.
            float normalizedReach = reachable * .62f / extent;
            return Mathf.Clamp(target, previous - normalizedReach, previous + normalizedReach);
        }

        void OnDrawGizmos()
        {
            if (!showRoute || !IsValid()) return;
            const int previewSamples = 80;
            Vector3 previous = Evaluate(0f);
            for (int i = 1; i < previewSamples; i++)
            {
                Vector3 current = Evaluate(i / (previewSamples - 1f));
                Gizmos.color = routeColor;
                Gizmos.DrawLine(previous, current);
                previous = current;
            }
            for (int i = 0; i < waypoints.Length; i++)
            {
                Gizmos.color = i == 0 || i == waypoints.Length - 1
                    ? new Color(1f, .25f, .75f, 1f) : routeColor;
                Gizmos.DrawSphere(Evaluate(waypoints[i].distance), .18f);
            }
        }
    }
}
