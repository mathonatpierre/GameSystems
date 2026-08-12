using System;
using UnityEngine;

namespace GameSystems.LevelGeneration
{
    [Serializable]
    public sealed class PlatformMotionRules
    {
        [SerializeField] bool enabled;
        [SerializeField] Vector3 localAxis = Vector3.right;
        [SerializeField, Min(0f)] float minimumDistance = 4f;
        [SerializeField, Min(0f)] float maximumDistance = 7f;
        [SerializeField, Min(.01f)] float minimumSpeed = 1f;
        [SerializeField, Min(.01f)] float maximumSpeed = 1.5f;
        [SerializeField, Min(0f)] float waitAtEnds = .8f;

        public bool Enabled => enabled;
        public Vector3 LocalAxis => localAxis.sqrMagnitude > .0001f
            ? localAxis.normalized : Vector3.right;
        public float MinimumDistance => minimumDistance;
        public float MaximumDistance => Mathf.Max(minimumDistance, maximumDistance);
        public float MinimumSpeed => minimumSpeed;
        public float MaximumSpeed => Mathf.Max(minimumSpeed, maximumSpeed);
        public float WaitAtEnds => waitAtEnds;

        public void Configure(Vector3 axis, float minDistance, float maxDistance,
            float minSpeed, float maxSpeed, float endpointWait)
        {
            enabled = true;
            localAxis = axis;
            minimumDistance = Mathf.Max(0f, minDistance);
            maximumDistance = Mathf.Max(minimumDistance, maxDistance);
            minimumSpeed = Mathf.Max(.01f, minSpeed);
            maximumSpeed = Mathf.Max(minimumSpeed, maxSpeed);
            waitAtEnds = Mathf.Max(0f, endpointWait);
        }
    }
}
