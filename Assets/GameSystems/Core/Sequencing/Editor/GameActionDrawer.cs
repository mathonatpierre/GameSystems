#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
namespace GameSystems.Actions.Editor
{
    [CustomPropertyDrawer(typeof(GameAction), true)]
    public sealed class GameActionDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) => ManagedReferenceDrawerUtility.GetHeight(property);
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) => ManagedReferenceDrawerUtility.Draw(position, property, label);
    }
}
#endif
