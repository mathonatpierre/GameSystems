using UnityEditor;
using UnityEngine;

namespace GameSystems.LevelGeneration.Editor
{
    [CustomEditor(typeof(PlatformTypeDefinition))]
    [CanEditMultipleObjects]
    public sealed class PlatformTypeDefinitionEditor : UnityEditor.Editor
    {
        SerializedProperty displayName;
        SerializedProperty type;
        SerializedProperty prefab;
        SerializedProperty selection;
        SerializedProperty geometry;
        SerializedProperty surface;
        SerializedProperty motion;
        SerializedProperty lifecycle;

        bool showSelection = true;
        bool showGeometry = true;
        bool showSurface = true;
        bool showMotion = true;
        bool showLifecycle = true;

        static readonly Color Accent = new(.48f, .28f, .92f, 1f);

        void OnEnable()
        {
            displayName = serializedObject.FindProperty("displayName");
            type = serializedObject.FindProperty("type");
            prefab = serializedObject.FindProperty("prefab");
            selection = serializedObject.FindProperty("selection");
            geometry = serializedObject.FindProperty("geometry");
            surface = serializedObject.FindProperty("surface");
            motion = serializedObject.FindProperty("motion");
            lifecycle = serializedObject.FindProperty("lifecycle");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawIdentity();
            EditorGUILayout.Space(4f);

            PlatformTypeId currentType = type.hasMultipleDifferentValues
                ? PlatformTypeId.Standard : (PlatformTypeId)type.enumValueIndex;
            DrawSelection();
            DrawGeometry();
            DrawSurface();

            bool movingType = currentType is PlatformTypeId.MovingHorizontal
                or PlatformTypeId.MovingVertical;
            bool lifecycleType = currentType is PlatformTypeId.Fragile
                or PlatformTypeId.Crusher;
            if (movingType || Child(motion, "enabled").boolValue) DrawMotion();
            if (lifecycleType || Child(lifecycle, "fragile").boolValue ||
                Child(lifecycle, "crusher").boolValue) DrawLifecycle(currentType);

            ValidateRanges();
            serializedObject.ApplyModifiedProperties();
            DrawDiagnostics(currentType);
        }

        void DrawIdentity()
        {
            Rect card = EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            Rect accent = new(card.x, card.y, 3f, card.height);
            EditorGUI.DrawRect(accent, Accent);
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PropertyField(type, GUIContent.none,
                    GUILayout.Width(135f));
                EditorGUILayout.PropertyField(displayName, GUIContent.none);
            }
            EditorGUILayout.PropertyField(prefab,
                new GUIContent("Archetype", "Optional prefab carrying behaviour, feedbacks and authoring defaults."));
            EditorGUILayout.EndVertical();
        }

        void DrawSelection()
        {
            SerializedProperty enabled = Child(selection, "enabled");
            if (!Section(ref showSelection, "Generation", enabled)) return;
            using (new EditorGUI.DisabledScope(!enabled.boolValue))
            {
                TwoFloats("Weight / Frequency", Child(selection, "weight"),
                    Child(selection, "frequency"));
                IntRange("Occurrences", Child(selection, "minimumOccurrences"),
                    Child(selection, "maximumOccurrences"), 0, 999);
                FloatRange("Level Progress", Child(selection, "minimumLevelProgress"),
                    Child(selection, "maximumLevelProgress"), 0f, 1f);
                EditorGUILayout.PropertyField(Child(selection,
                    "minimumDistanceFromPrevious"), new GUIContent("Min Spacing"));
                IntRange("Cluster Size", Child(selection, "minimumClusterSize"),
                    Child(selection, "maximumClusterSize"), 1, 99);
            }
            EndSection();
        }

        void DrawGeometry()
        {
            if (!Section(ref showGeometry, "Geometry")) return;
            IntRange("Length", Child(geometry, "minimumLength"),
                Child(geometry, "maximumLength"), 1, 100);
            IntRange("Depth", Child(geometry, "minimumDepth"),
                Child(geometry, "maximumDepth"), 1, 32);
            IntRange("Foundation", Child(geometry, "minimumFoundationDepth"),
                Child(geometry, "maximumFoundationDepth"), 1, 100);
            TwoFloats("Edge / Surface", Child(geometry, "edgeDamage"),
                Child(geometry, "surfaceIrregularity"));
            EndSection();
        }

        void DrawSurface()
        {
            if (!Section(ref showSurface, "Surface")) return;
            EditorGUILayout.PropertyField(Child(surface, "primaryMaterial"),
                new GUIContent("Primary"));
            EditorGUILayout.PropertyField(Child(surface, "secondaryMaterial"),
                new GUIContent("Secondary", "Falls back to Primary when empty."));
            EditorGUILayout.PropertyField(Child(surface, "castsShadows"),
                new GUIContent("Cast Shadows"));
            EndSection();
        }

        void DrawMotion()
        {
            SerializedProperty enabled = Child(motion, "enabled");
            if (!Section(ref showMotion, "Motion", enabled)) return;
            using (new EditorGUI.DisabledScope(!enabled.boolValue))
            {
                EditorGUILayout.PropertyField(Child(motion, "localAxis"),
                    new GUIContent("Local Axis"));
                FloatRange("Distance", Child(motion, "minimumDistance"),
                    Child(motion, "maximumDistance"), 0f, 100f);
                FloatRange("Speed", Child(motion, "minimumSpeed"),
                    Child(motion, "maximumSpeed"), .01f, 30f, false);
                EditorGUILayout.PropertyField(Child(motion, "waitAtEnds"),
                    new GUIContent("Endpoint Pause"));
            }
            EndSection();
        }

        void DrawLifecycle(PlatformTypeId currentType)
        {
            if (!Section(ref showLifecycle, "Lifecycle")) return;
            SerializedProperty fragile = Child(lifecycle, "fragile");
            SerializedProperty crusher = Child(lifecycle, "crusher");
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PropertyField(fragile, new GUIContent("Fragile"));
                EditorGUILayout.PropertyField(crusher, new GUIContent("Crusher"));
            }
            if (fragile.boolValue || currentType == PlatformTypeId.Fragile)
            {
                TwoFloats("Warn / Respawn", Child(lifecycle, "warningDelay"),
                    Child(lifecycle, "respawnDelay"));
            }
            if (crusher.boolValue || currentType == PlatformTypeId.Crusher)
            {
                TwoFloats("Warn / Travel", Child(lifecycle,
                    "crusherWarningDelay"), Child(lifecycle, "crusherTravel"));
            }
            EndSection();
        }

        bool Section(ref bool expanded, string label,
            SerializedProperty toggle = null)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            Rect row = EditorGUILayout.GetControlRect(false, 20f);
            expanded = EditorGUI.Foldout(new Rect(row.x, row.y,
                toggle == null ? row.width : row.width - 24f, row.height),
                expanded, label, true, EditorStyles.foldoutHeader);
            if (toggle != null)
                toggle.boolValue = EditorGUI.Toggle(new Rect(row.xMax - 18f,
                    row.y + 1f, 18f, 18f), toggle.boolValue);
            if (!expanded) EditorGUILayout.EndVertical();
            return expanded;
        }

        static void EndSection()
        {
            EditorGUILayout.Space(2f);
            EditorGUILayout.EndVertical();
        }

        static void TwoFloats(string label, SerializedProperty first,
            SerializedProperty second)
        {
            Rect row = EditorGUILayout.GetControlRect();
            row = EditorGUI.PrefixLabel(row, new GUIContent(label));
            float gap = 4f;
            float width = (row.width - gap) * .5f;
            EditorGUI.PropertyField(new Rect(row.x, row.y, width, row.height),
                first, GUIContent.none);
            EditorGUI.PropertyField(new Rect(row.x + width + gap, row.y,
                width, row.height), second, GUIContent.none);
        }

        static void IntRange(string label, SerializedProperty minimum,
            SerializedProperty maximum, int lower, int upper)
        {
            Rect row = EditorGUILayout.GetControlRect();
            row = EditorGUI.PrefixLabel(row, new GUIContent(label));
            const float fieldWidth = 46f;
            minimum.intValue = EditorGUI.IntField(new Rect(row.x, row.y,
                fieldWidth, row.height), minimum.intValue);
            maximum.intValue = EditorGUI.IntField(new Rect(row.xMax - fieldWidth,
                row.y, fieldWidth, row.height), maximum.intValue);
            float min = minimum.intValue;
            float max = maximum.intValue;
            EditorGUI.MinMaxSlider(new Rect(row.x + fieldWidth + 5f, row.y,
                row.width - fieldWidth * 2f - 10f, row.height), ref min, ref max,
                lower, upper);
            minimum.intValue = Mathf.RoundToInt(min);
            maximum.intValue = Mathf.RoundToInt(max);
        }

        static void FloatRange(string label, SerializedProperty minimum,
            SerializedProperty maximum, float lower, float upper,
            bool slider = true)
        {
            if (!slider)
            {
                TwoFloats(label, minimum, maximum);
                return;
            }
            Rect row = EditorGUILayout.GetControlRect();
            row = EditorGUI.PrefixLabel(row, new GUIContent(label));
            const float fieldWidth = 48f;
            minimum.floatValue = EditorGUI.FloatField(new Rect(row.x, row.y,
                fieldWidth, row.height), minimum.floatValue);
            maximum.floatValue = EditorGUI.FloatField(new Rect(row.xMax - fieldWidth,
                row.y, fieldWidth, row.height), maximum.floatValue);
            float min = minimum.floatValue;
            float max = maximum.floatValue;
            EditorGUI.MinMaxSlider(new Rect(row.x + fieldWidth + 5f, row.y,
                row.width - fieldWidth * 2f - 10f, row.height), ref min, ref max,
                lower, upper);
            minimum.floatValue = min;
            maximum.floatValue = max;
        }

        void ValidateRanges()
        {
            ClampPair(selection, "minimumOccurrences", "maximumOccurrences", 0);
            ClampPair(selection, "minimumClusterSize", "maximumClusterSize", 1);
            ClampPair(geometry, "minimumLength", "maximumLength", 1);
            ClampPair(geometry, "minimumDepth", "maximumDepth", 1);
            ClampPair(geometry, "minimumFoundationDepth",
                "maximumFoundationDepth", 1);
            ClampPair(motion, "minimumDistance", "maximumDistance", 0f);
            ClampPair(motion, "minimumSpeed", "maximumSpeed", .01f);
            SerializedProperty minProgress = Child(selection,
                "minimumLevelProgress");
            SerializedProperty maxProgress = Child(selection,
                "maximumLevelProgress");
            minProgress.floatValue = Mathf.Clamp01(minProgress.floatValue);
            maxProgress.floatValue = Mathf.Clamp(maxProgress.floatValue,
                minProgress.floatValue, 1f);
        }

        void DrawDiagnostics(PlatformTypeId currentType)
        {
            if (targets.Length != 1) return;
            bool requiresArchetype = currentType is PlatformTypeId.Fragile
                or PlatformTypeId.Crusher;
            if (requiresArchetype && prefab.objectReferenceValue == null)
                EditorGUILayout.HelpBox("This platform type needs an archetype prefab " +
                    "for its behaviour and feedbacks.", MessageType.Warning);
            if (Child(surface, "primaryMaterial").objectReferenceValue == null)
                EditorGUILayout.HelpBox("Primary material is missing.",
                    MessageType.Warning);
        }

        static SerializedProperty Child(SerializedProperty parent, string name)
            => parent.FindPropertyRelative(name);

        static void ClampPair(SerializedProperty parent, string minName,
            string maxName, int floor)
        {
            SerializedProperty min = Child(parent, minName);
            SerializedProperty max = Child(parent, maxName);
            min.intValue = Mathf.Max(floor, min.intValue);
            max.intValue = Mathf.Max(min.intValue, max.intValue);
        }

        static void ClampPair(SerializedProperty parent, string minName,
            string maxName, float floor)
        {
            SerializedProperty min = Child(parent, minName);
            SerializedProperty max = Child(parent, maxName);
            min.floatValue = Mathf.Max(floor, min.floatValue);
            max.floatValue = Mathf.Max(min.floatValue, max.floatValue);
        }
    }
}
