using System.Collections.Generic;
using UnityEngine;

namespace GameSystems.Rideables
{
    public sealed class RailFlightPath : MonoBehaviour
    {
        [SerializeField] Transform[] points;
        [SerializeField, Range(8, 64)] int samplesPerSegment = 24;
        readonly List<Vector3> samples = new();
        readonly List<float> distances = new();

        public float Length { get; private set; }

        public void Configure(IReadOnlyList<Vector3> worldPoints)
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
                DestroyImmediate(transform.GetChild(i).gameObject);
            points = new Transform[worldPoints?.Count ?? 0];
            for (int i = 0; i < points.Length; i++)
            {
                Transform point = new GameObject($"Point {i:00}").transform;
                point.SetParent(transform, true);
                point.position = worldPoints[i];
                points[i] = point;
            }
            RebuildCache();
        }

        public void RebuildCache()
        {
            samples.Clear();
            distances.Clear();
            Length = 0f;
            if (points == null || points.Length < 2) return;
            int count = Mathf.Max(2, (points.Length - 1) * samplesPerSegment + 1);
            for (int i = 0; i < count; i++)
            {
                Vector3 point = EvaluateNormalized(i / (count - 1f));
                if (samples.Count > 0) Length += Vector3.Distance(samples[^1], point);
                samples.Add(point);
                distances.Add(Length);
            }
        }

        public void EvaluateDistance(float distance, out Vector3 position, out Vector3 forward,
            out Vector3 up)
        {
            if (samples.Count < 2) RebuildCache();
            if (samples.Count < 2)
            {
                position = transform.position; forward = transform.forward; up = transform.up;
                return;
            }
            float clamped = Mathf.Clamp(distance, 0f, Length);
            position = SamplePosition(clamped);
            float tangentRange = Mathf.Min(.75f, Length * .01f);
            Vector3 before = SamplePosition(Mathf.Max(0f, clamped - tangentRange));
            Vector3 after = SamplePosition(Mathf.Min(Length, clamped + tangentRange));
            forward = (after - before).normalized;
            if (forward.sqrMagnitude < .001f) forward = transform.forward;
            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
            if (right.sqrMagnitude < .01f) right = transform.right;
            up = Vector3.Cross(forward, right).normalized;
        }

        Vector3 SamplePosition(float distance)
        {
            float clamped = Mathf.Clamp(distance, 0f, Length);
            int high = distances.BinarySearch(clamped);
            if (high < 0) high = ~high;
            high = Mathf.Clamp(high, 1, samples.Count - 1);
            int low = high - 1;
            float t = Mathf.InverseLerp(distances[low], distances[high], clamped);
            return Vector3.Lerp(samples[low], samples[high], t);
        }

        Vector3 EvaluateNormalized(float normalized)
        {
            float scaled = Mathf.Clamp01(normalized) * (points.Length - 1);
            int segment = Mathf.Min(points.Length - 2, Mathf.FloorToInt(scaled));
            float t = scaled - segment;
            Vector3 p0 = points[Mathf.Max(0, segment - 1)].position;
            Vector3 p1 = points[segment].position;
            Vector3 p2 = points[segment + 1].position;
            Vector3 p3 = points[Mathf.Min(points.Length - 1, segment + 2)].position;
            return .5f * ((2f * p1) + (-p0 + p2) * t +
                (2f * p0 - 5f * p1 + 4f * p2 - p3) * t * t +
                (-p0 + 3f * p1 - 3f * p2 + p3) * t * t * t);
        }

        void OnDrawGizmos()
        {
            RebuildCache();
            Gizmos.color = new Color(.2f, .8f, 1f, .8f);
            for (int i = 1; i < samples.Count; i++) Gizmos.DrawLine(samples[i - 1], samples[i]);
        }
    }
}
