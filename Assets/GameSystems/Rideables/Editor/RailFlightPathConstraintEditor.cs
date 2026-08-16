#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace GameSystems.Rideables.EditorTools
{
    [CustomEditor(typeof(RailFlightPathConstraint))]
    public sealed class RailFlightPathConstraintEditor : UnityEditor.Editor
    {
        GUIStyle labelStyle;
        GUIStyle LabelStyle => labelStyle ??= new GUIStyle(EditorStyles.boldLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 12
        };

        void OnSceneGUI()
        {
            RailFlightPathConstraint constraint = (RailFlightPathConstraint)target;
            RailFlightPath path = constraint.Path;
            if (path == null) return;
            path.RebuildCache();
            if (path.Length <= .01f) return;

            path.EvaluateDistance(0f, out Vector3 start, out Vector3 startForward,
                out Vector3 startUp);
            path.EvaluateDistance(path.Length, out Vector3 finish, out Vector3 finishForward,
                out Vector3 finishUp);
            LabelStyle.normal.textColor = new Color(.25f, 1f, .5f);
            Handles.Label(start + startUp * (constraint.FlightExtents.y + .55f),
                "START  >", LabelStyle);
            LabelStyle.normal.textColor = new Color(1f, .35f, .8f);
            Handles.Label(finish + finishUp * (constraint.FlightExtents.y + .55f),
                ">  FINISH", LabelStyle);

            Handles.color = new Color(.15f, .8f, 1f, .9f);
            float arrowSize = HandleUtility.GetHandleSize(start) * .65f;
            Handles.ArrowHandleCap(0, start, Quaternion.LookRotation(startForward, startUp),
                arrowSize, EventType.Repaint);
            arrowSize = HandleUtility.GetHandleSize(finish) * .65f;
            Handles.ArrowHandleCap(0, finish, Quaternion.LookRotation(finishForward, finishUp),
                arrowSize, EventType.Repaint);
        }
    }
}
#endif
