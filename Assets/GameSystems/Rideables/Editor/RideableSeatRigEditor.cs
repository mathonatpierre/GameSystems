using GameSystems.Rideables;
using UnityEditor;
using UnityEngine;

namespace GameSystems.Rideables.Editor
{
    [CustomEditor(typeof(RideableSeatRig))]
    public sealed class RideableSeatRigEditor : UnityEditor.Editor
    {
        static readonly Color SeatColor = new(1f, .72f, .12f);

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var rig = (RideableSeatRig)target;
            if (GUILayout.Button("Sync Seat Follower"))
            {
                Undo.RecordObject(rig, "Sync Rideable Seat");
                rig.SyncFollower();
                EditorUtility.SetDirty(rig);
            }
        }

        void OnSceneGUI()
        {
            var rig = (RideableSeatRig)target;
            DrawHandle(rig.transform, "Seat", SeatColor, true, rig);
            DrawHandle(rig.LeftFoot, "Left Foot", new Color(.25f, .65f, 1f), false, rig);
            DrawHandle(rig.RightFoot, "Right Foot", new Color(.2f, 1f, .75f), false, rig);
            DrawHandle(rig.LeftHand, "Left Hand", new Color(1f, .35f, .7f), false, rig);
            DrawHandle(rig.RightHand, "Right Hand", new Color(1f, .55f, .25f), false, rig);
            DrawHandle(rig.MountPoint, "Mount Left", new Color(.35f, 1f, .45f), false, rig);
            DrawHandle(rig.DismountPoint, "Dismount", new Color(.85f, .85f, .85f), false, rig);
        }

        static void DrawHandle(Transform point, string label, Color color,
            bool syncFollower, RideableSeatRig rig)
        {
            if (point == null) return;
            Handles.color = color;
            Handles.Label(point.position + Vector3.up * .06f, label);
            EditorGUI.BeginChangeCheck();
            Vector3 position = Handles.PositionHandle(point.position, point.rotation);
            Quaternion rotation = Handles.RotationHandle(point.rotation, position);
            if (!EditorGUI.EndChangeCheck()) return;
            Undo.RecordObject(point, $"Move {label}");
            point.SetPositionAndRotation(position, rotation);
            if (syncFollower) rig.SyncFollower();
            EditorUtility.SetDirty(point);
            EditorUtility.SetDirty(rig);
        }
    }
}
