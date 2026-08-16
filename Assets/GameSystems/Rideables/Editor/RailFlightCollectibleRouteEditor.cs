#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace GameSystems.Rideables.EditorTools
{
    [CustomEditor(typeof(RailFlightCollectibleRoute))]
    public sealed class RailFlightCollectibleRouteEditor : UnityEditor.Editor
    {
        void OnSceneGUI()
        {
            RailFlightCollectibleRoute route = (RailFlightCollectibleRoute)target;
            if (route.FlightPlan == null || route.Waypoints == null) return;
            Vector2 extents = route.FlightPlan.FlightExtents * .82f;
            for (int i = 0; i < route.Waypoints.Count; i++)
            {
                RailFlightCollectibleWaypoint waypoint = route.Waypoints[i];
                route.GetFrame(waypoint.distance, out Vector3 center,
                    out Vector3 right, out Vector3 up);
                Vector3 position = center + right * waypoint.offset.x * extents.x +
                                   up * waypoint.offset.y * extents.y;
                EditorGUI.BeginChangeCheck();
                Vector3 moved = Handles.PositionHandle(position,
                    Quaternion.LookRotation(Vector3.Cross(right, up), up));
                if (!EditorGUI.EndChangeCheck()) continue;
                Undo.RecordObject(route, "Move collectible route waypoint");
                Vector3 delta = moved - center;
                Vector2 offset = new(
                    Vector3.Dot(delta, right) / Mathf.Max(.01f, extents.x),
                    Vector3.Dot(delta, up) / Mathf.Max(.01f, extents.y));
                route.SetWaypointOffset(i, offset);
                EditorUtility.SetDirty(route);
            }
        }
    }
}
#endif
