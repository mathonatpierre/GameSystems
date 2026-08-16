using UnityEngine;

namespace GameSystems.Rideables
{
    [DefaultExecutionOrder(-50)]
    public sealed class RailFlightCameraAnchor : MonoBehaviour
    {
        [SerializeField] RailFlightPathConstraint flight;
        [SerializeField, Min(0f)] float lookAheadDistance = 5f;
        [SerializeField, Range(0f, 1f), Tooltip("How strongly the camera follows the rider's free movement inside the flight corridor.")]
        float riderTracking = 1f;

        public void Configure(RailFlightPathConstraint value, float lookAhead = 5f)
        {
            flight = value;
            lookAheadDistance = Mathf.Max(0f, lookAhead);
            Snap();
        }

        void LateUpdate() => Snap();

        void Snap()
        {
            if (flight == null || flight.Path == null) return;
            float distance = Mathf.Min(flight.Path.Length, flight.Distance + lookAheadDistance);
            flight.Path.EvaluateDistance(distance, out Vector3 position,
                out Vector3 forward, out Vector3 up);
            Vector3 riderPosition = flight.transform.position;
            Vector3 riderLookAhead = riderPosition + forward * lookAheadDistance;
            position = Vector3.Lerp(position, riderLookAhead, riderTracking);
            transform.SetPositionAndRotation(position, Quaternion.LookRotation(forward, up));
        }
    }
}
