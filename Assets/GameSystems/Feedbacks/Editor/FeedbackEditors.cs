#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using GameSystems.Editor;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace GameSystems.Feedbacks.Editor
{
    [CustomPropertyDrawer(typeof(FeedbackCue))]
    public sealed class FeedbackCueDrawer : PropertyDrawer
    {
        const float Gap = 3f;
        static readonly Color[] FamilyColors =
        {
            new(.32f,.65f,1f), new(.48f,.84f,.95f), new(.55f,.82f,.48f),
            new(1f,.72f,.3f), new(.95f,.48f,.68f), new(.74f,.58f,1f)
        };

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!property.isExpanded) return EditorGUIUtility.singleLineHeight + 6f;
            int rows = 9;
            FeedbackKind kind = (FeedbackKind)property.FindPropertyRelative("kind").enumValueIndex;
            rows += TargetRows(kind) + PayloadRows(kind);
            return 10f + rows * (EditorGUIUtility.singleLineHeight + Gap);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            Rect header = new(position.x, position.y + 2f, position.width, EditorGUIUtility.singleLineHeight + 2f);
            FeedbackKind kind = (FeedbackKind)property.FindPropertyRelative("kind").enumValueIndex;
            EditorGUI.DrawRect(header, new Color(FamilyColors[Family(kind)].r, FamilyColors[Family(kind)].g, FamilyColors[Family(kind)].b, .22f));
            Rect fold = new(header.x + 5f, header.y, 16f, header.height);
            property.isExpanded = EditorGUI.Foldout(fold, property.isExpanded, GUIContent.none, true);
            SerializedProperty enabled = property.FindPropertyRelative("enabled");
            enabled.boolValue = EditorGUI.Toggle(new Rect(header.x + 22f, header.y, 18f, header.height), enabled.boolValue);
            SerializedProperty name = property.FindPropertyRelative("label");
            string title = string.IsNullOrWhiteSpace(name.stringValue) ? kind.ToString() : name.stringValue;
            EditorGUI.LabelField(new Rect(header.x + 43f, header.y, header.width - 47f, header.height), title, EditorStyles.boldLabel);
            if (!property.isExpanded) { EditorGUI.EndProperty(); return; }

            float y = header.yMax + Gap + 2f;
            Draw(ref y, position, property, "label", "Name");
            Draw(ref y, position, property, "kind", "Module");
            Draw(ref y, position, property, "bindingId", "Binding ID");
            DrawTwo(ref y, position, property, "initialDelay", "Delay", "duration", "Duration");
            DrawTwo(ref y, position, property, "chance", "Chance %", "cooldown", "Cooldown");
            DrawTwo(ref y, position, property, "repeats", "Repeats", "repeatDelay", "Repeat Delay");
            Draw(ref y, position, property, "timeMode", "Time Mode");
            Draw(ref y, position, property, "curve", "Curve");
            Draw(ref y, position, property, "restoreAfterPlay", "Restore After Play");
            DrawTargets(ref y, position, property, kind);
            DrawPayload(ref y, position, property, kind);
            EditorGUI.EndProperty();
        }

        static int Family(FeedbackKind kind) => kind switch
        {
            FeedbackKind.CameraShake => 0,
            FeedbackKind.Audio => 1,
            FeedbackKind.ParticleBurst => 2,
            FeedbackKind.LightIntensity or FeedbackKind.MaterialFloat or FeedbackKind.MaterialColor => 3,
            FeedbackKind.TimeScale => 4,
            _ => 5
        };

        static int TargetRows(FeedbackKind kind) => kind switch
        {
            FeedbackKind.ParticleBurst => 1, FeedbackKind.Audio => 2,
            FeedbackKind.LightIntensity => 1,
            FeedbackKind.MaterialFloat or FeedbackKind.MaterialColor => 2,
            FeedbackKind.SetActive or FeedbackKind.AnimatorTrigger or FeedbackKind.RigidbodyImpulse or FeedbackKind.NestedPlayer => 0,
            FeedbackKind.CameraZoom => 1, FeedbackKind.ScreenFlash => 1,
            FeedbackKind.AudioRandomized => 3, FeedbackKind.InstantiatePooled => 1,
            FeedbackKind.URPBloom or FeedbackKind.URPChromaticAberration or FeedbackKind.URPLensDistortion or FeedbackKind.URPVignette or FeedbackKind.URPColorAdjustments => 1,
            FeedbackKind.RendererBlink or FeedbackKind.MaterialFloatHierarchy => 0,
            FeedbackKind.UnityEvent or FeedbackKind.CameraShake or FeedbackKind.TimeScale => 0,
            _ => 1
        };

        static int PayloadRows(FeedbackKind kind) => kind switch
        {
            FeedbackKind.TransformShake => 3, FeedbackKind.Scale => 1,
            FeedbackKind.Position or FeedbackKind.Rotation => 1,
            FeedbackKind.CameraShake or FeedbackKind.Audio or FeedbackKind.LightIntensity or FeedbackKind.TimeScale => 1,
            FeedbackKind.MaterialFloat or FeedbackKind.MaterialColor => 1,
            FeedbackKind.AnimatorTrigger => 1, FeedbackKind.RigidbodyImpulse => 2,
            FeedbackKind.SetActive => 1, FeedbackKind.UnityEvent => 1,
            FeedbackKind.PositionSpring or FeedbackKind.RotationSpring or FeedbackKind.ScaleSpring or FeedbackKind.SquashStretchSpring => 2,
            FeedbackKind.FreezeFrame or FeedbackKind.CameraZoom or FeedbackKind.ScreenFlash => 1,
            FeedbackKind.AudioRandomized => 1,
            FeedbackKind.URPBloom or FeedbackKind.URPChromaticAberration or FeedbackKind.URPLensDistortion or FeedbackKind.URPVignette or FeedbackKind.URPColorAdjustments => 1,
            FeedbackKind.RendererBlink => 1, FeedbackKind.MaterialFloatHierarchy => 2,
            _ => 0
        };

        static void DrawTargets(ref float y, Rect area, SerializedProperty p, FeedbackKind kind)
        {
            switch (kind)
            {
                case FeedbackKind.ParticleBurst: Draw(ref y, area, p, "particles", "Fallback Particles"); break;
                case FeedbackKind.Audio: Draw(ref y, area, p, "audioSource", "Fallback Source"); Draw(ref y, area, p, "audioClip", "Clip"); break;
                case FeedbackKind.LightIntensity: Draw(ref y, area, p, "light", "Fallback Light"); break;
                case FeedbackKind.MaterialFloat:
                case FeedbackKind.MaterialColor: Draw(ref y, area, p, "renderer", "Fallback Renderer"); Draw(ref y, area, p, "propertyName", "Shader Property"); break;
                case FeedbackKind.TransformShake:
                case FeedbackKind.Scale:
                case FeedbackKind.Position:
                case FeedbackKind.Rotation:
                case FeedbackKind.PositionSpring:
                case FeedbackKind.RotationSpring:
                case FeedbackKind.ScaleSpring:
                case FeedbackKind.SquashStretchSpring: Draw(ref y, area, p, "target", "Fallback Transform"); break;
                case FeedbackKind.CameraZoom: Draw(ref y, area, p, "camera", "Fallback Camera"); break;
                case FeedbackKind.ScreenFlash: Draw(ref y, area, p, "canvasGroup", "Fallback Canvas Group"); break;
                case FeedbackKind.AudioRandomized: Draw(ref y, area, p, "audioSource", "Fallback Source"); Draw(ref y, area, p, "audioClips", "Clips"); Draw(ref y, area, p, "pitchRange", "Pitch Range"); break;
                case FeedbackKind.InstantiatePooled: Draw(ref y, area, p, "prefab", "Prefab"); break;
                case FeedbackKind.URPBloom:
                case FeedbackKind.URPChromaticAberration:
                case FeedbackKind.URPLensDistortion:
                case FeedbackKind.URPVignette:
                case FeedbackKind.URPColorAdjustments: Draw(ref y, area, p, "volume", "Fallback Volume"); break;
            }
        }

        static void DrawPayload(ref float y, Rect area, SerializedProperty p, FeedbackKind kind)
        {
            switch (kind)
            {
                case FeedbackKind.TransformShake: Draw(ref y, area, p, "vector", "Amplitude"); Draw(ref y, area, p, "frequency", "Frequency"); Draw(ref y, area, p, "amount", "Intensity"); break;
                case FeedbackKind.Scale: Draw(ref y, area, p, "vectorB", "Target Multiplier"); break;
                case FeedbackKind.Position: Draw(ref y, area, p, "vector", "Local Offset"); break;
                case FeedbackKind.Rotation: Draw(ref y, area, p, "vector", "Euler Offset"); break;
                case FeedbackKind.CameraShake:
                case FeedbackKind.Audio:
                case FeedbackKind.LightIntensity:
                case FeedbackKind.TimeScale: Draw(ref y, area, p, "amount", "Amount"); break;
                case FeedbackKind.MaterialFloat: Draw(ref y, area, p, "amount", "Value"); break;
                case FeedbackKind.MaterialColor: Draw(ref y, area, p, "color", "Color"); break;
                case FeedbackKind.AnimatorTrigger: Draw(ref y, area, p, "animatorParameter", "Trigger"); break;
                case FeedbackKind.RigidbodyImpulse: Draw(ref y, area, p, "vector", "Impulse"); Draw(ref y, area, p, "forceMode", "Force Mode"); break;
                case FeedbackKind.SetActive: Draw(ref y, area, p, "amount", "Active (> 0)"); break;
                case FeedbackKind.UnityEvent: Draw(ref y, area, p, "unityEvent", "Event"); break;
                case FeedbackKind.PositionSpring:
                case FeedbackKind.RotationSpring: Draw(ref y, area, p, "vector", "Impulse"); DrawTwo(ref y, area, p, "springStrength", "Strength", "damping", "Damping"); break;
                case FeedbackKind.ScaleSpring: Draw(ref y, area, p, "vectorB", "Target Multiplier"); DrawTwo(ref y, area, p, "springStrength", "Strength", "damping", "Damping"); break;
                case FeedbackKind.SquashStretchSpring: Draw(ref y, area, p, "vector", "Squash Amount"); DrawTwo(ref y, area, p, "springStrength", "Strength", "damping", "Damping"); break;
                case FeedbackKind.FreezeFrame: Draw(ref y, area, p, "amount", "Frozen Time Scale"); break;
                case FeedbackKind.CameraZoom: Draw(ref y, area, p, "amount", "Target FOV"); break;
                case FeedbackKind.ScreenFlash: Draw(ref y, area, p, "amount", "Target Alpha"); break;
                case FeedbackKind.AudioRandomized: Draw(ref y, area, p, "volumeRange", "Volume Range"); break;
                case FeedbackKind.InstantiatePooled: break;
                case FeedbackKind.URPBloom:
                case FeedbackKind.URPChromaticAberration:
                case FeedbackKind.URPLensDistortion:
                case FeedbackKind.URPVignette:
                case FeedbackKind.URPColorAdjustments: Draw(ref y, area, p, "amount", "Target Value"); break;
                case FeedbackKind.RendererBlink: Draw(ref y, area, p, "frequency", "Blink Frequency"); break;
                case FeedbackKind.MaterialFloatHierarchy: Draw(ref y, area, p, "propertyName", "Shader Property"); Draw(ref y, area, p, "amount", "Target Value"); break;
            }
        }

        static void Draw(ref float y, Rect area, SerializedProperty root, string name, string label)
        {
            Rect row = new(area.x + 12f, y, area.width - 18f, EditorGUIUtility.singleLineHeight);
            EditorGUI.PropertyField(row, root.FindPropertyRelative(name), new GUIContent(label), true);
            y += EditorGUIUtility.singleLineHeight + Gap;
        }

        static void DrawTwo(ref float y, Rect area, SerializedProperty root, string a, string la, string b, string lb)
        {
            float width = (area.width - 24f) * .5f;
            EditorGUI.PropertyField(new Rect(area.x + 12f, y, width, EditorGUIUtility.singleLineHeight), root.FindPropertyRelative(a), new GUIContent(la));
            EditorGUI.PropertyField(new Rect(area.x + 18f + width, y, width, EditorGUIUtility.singleLineHeight), root.FindPropertyRelative(b), new GUIContent(lb));
            y += EditorGUIUtility.singleLineHeight + Gap;
        }
    }

    [CustomEditor(typeof(FeedbackPlayer))]
    public sealed class FeedbackPlayerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawStatus();
            DrawPropertiesExcluding(serializedObject, "m_Script");
            serializedObject.ApplyModifiedProperties();
        }

        void DrawStatus()
        {
            var player = (FeedbackPlayer)target;
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                GameSystemsInspectorUI.Pill(player.IsPlaying ? "PLAYING" : "READY",
                    player.IsPlaying ? new Color(.25f, .7f, .42f) : new Color(.42f, .55f, .95f));
                EditorGUILayout.LabelField("Feedback Player", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                GUI.enabled = Application.isPlaying;
                if (GUILayout.Button("Play", GUILayout.Width(52))) player.PlayFeedbacks();
                if (GUILayout.Button("Stop", GUILayout.Width(52))) player.StopFeedbacks(true);
                if (GUILayout.Button("Reset", GUILayout.Width(52))) player.ResetFeedbacks();
                GUI.enabled = true;
            }
        }

    }

    [CustomEditor(typeof(FeedbackAsset))]
    public sealed class FeedbackAssetEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            bool embedded = AssetDatabase.IsSubAsset(target);
            GameSystemsInspectorUI.Header(embedded ? "Embedded Feedback" : "Shared Feedback",
                embedded ? "Owned by its sequence." : "Reusable by multiple sequences.");
            DrawDefaultInspector();
        }
    }

    [CustomEditor(typeof(FeedbackSequence))]
    public sealed class FeedbackSequenceEditor : UnityEditor.Editor
    {
        bool showTimeline = true;
        bool showDiagnostics = true;

        public override void OnInspectorGUI()
        {
            FeedbackSequence sequence = (FeedbackSequence)target;
            float totalDuration = EstimateDuration(sequence);
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                GameSystemsInspectorUI.Pill(sequence.PlayMode.ToString(), new Color(.42f, .55f, .95f));
                GameSystemsInspectorUI.Pill(sequence.Concurrency.ToString(), new Color(.52f, .74f, .44f));
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField($"{sequence.Feedbacks.Count} entries", GameSystemsInspectorUI.SmallMutedStyle, GUILayout.Width(70f));
                EditorGUILayout.LabelField($"{totalDuration:0.00}s", GameSystemsInspectorUI.SmallMutedStyle, GUILayout.Width(54f));
            }
            serializedObject.Update();
            DrawSequencePropertiesInline();
            serializedObject.ApplyModifiedProperties();

            DrawTimeline(sequence, totalDuration);
            DrawDiagnostics(sequence);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Add Embedded")) ShowEmbeddedMenu(sequence);
                if (GUILayout.Button("Ping Embedded"))
                    foreach (FeedbackAsset asset in sequence.Feedbacks)
                        if (asset != null && AssetDatabase.IsSubAsset(asset)) EditorGUIUtility.PingObject(asset);
                using (new EditorGUI.DisabledScope(!Application.isPlaying))
                    if (GUILayout.Button("Preview"))
                        FeedbackService.Play(sequence, FeedbackContext.From(Selection.activeGameObject));
            }
        }

        void DrawSequencePropertiesInline()
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("description"));
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("playMode"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("concurrency"));
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("maximumInstances"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("channel"));
            }
            GameSystemsInspectorUI.InlineScriptableObjectList(
                serializedObject.FindProperty("feedbacks"),
                typeof(FeedbackAsset),
                "Feedbacks");
        }

        void DrawTimeline(FeedbackSequence sequence, float totalDuration)
        {
            string summary = sequence.Feedbacks.Count == 0
                ? "empty"
                : $"{sequence.Feedbacks.Count} cues · {totalDuration:0.00}s";
            if (!GameSystemsInspectorUI.Foldout(ref showTimeline, "Timeline Preview", summary)) return;

            if (sequence.Feedbacks.Count == 0)
            {
                EditorGUILayout.LabelField("No feedback assets in this sequence.", GameSystemsInspectorUI.SmallMutedStyle);
                GameSystemsInspectorUI.EndFoldout();
                return;
            }

            float cursor = 0f;
            float scaleDuration = Mathf.Max(.001f, totalDuration);
            Rect ruler = EditorGUILayout.GetControlRect(false, 18f);
            DrawRuler(ruler, scaleDuration);

            for (int i = 0; i < sequence.Feedbacks.Count; i++)
            {
                FeedbackAsset asset = sequence.Feedbacks[i];
                FeedbackCue cue = asset != null ? asset.Cue : null;
                float start = cue != null ? cursor + cue.initialDelay : cursor;
                float duration = cue != null ? EffectiveDuration(cue) : 0f;
                Rect row = EditorGUILayout.GetControlRect(false, 24f);
                DrawTimelineRow(row, i, asset, cue, start, duration, scaleDuration);
                if (sequence.PlayMode == FeedbackPlayMode.Sequential)
                    cursor = start + duration;
            }

            GameSystemsInspectorUI.EndFoldout();
        }

        void DrawDiagnostics(FeedbackSequence sequence)
        {
            int missing = 0;
            int disabled = 0;
            int chanceSkipped = 0;
            for (int i = 0; i < sequence.Feedbacks.Count; i++)
            {
                FeedbackAsset asset = sequence.Feedbacks[i];
                FeedbackCue cue = asset != null ? asset.Cue : null;
                if (asset == null || cue == null) { missing++; continue; }
                if (!cue.enabled) disabled++;
                if (cue.chance < 100f) chanceSkipped++;
            }

            if (!GameSystemsInspectorUI.Foldout(ref showDiagnostics, "Diagnostics",
                    missing == 0 && disabled == 0 ? "clean" : $"{missing} missing · {disabled} disabled")) return;
            if (missing == 0 && disabled == 0 && chanceSkipped == 0)
                EditorGUILayout.HelpBox("Sequence looks ready.", MessageType.Info);
            if (missing > 0) EditorGUILayout.HelpBox($"{missing} feedback slot(s) are missing.", MessageType.Error);
            if (disabled > 0) EditorGUILayout.HelpBox($"{disabled} feedback cue(s) are disabled.", MessageType.Warning);
            if (chanceSkipped > 0) EditorGUILayout.HelpBox($"{chanceSkipped} feedback cue(s) have chance below 100%.", MessageType.Info);
            if (sequence.PlayMode == FeedbackPlayMode.Sequential && sequence.Concurrency == FeedbackConcurrency.RestartExisting)
                EditorGUILayout.HelpBox("Sequential + RestartExisting can make repeated triggers restart before the full sequence is visible.", MessageType.Info);
            GameSystemsInspectorUI.EndFoldout();
        }

        static void DrawRuler(Rect rect, float duration)
        {
            EditorGUI.DrawRect(rect, EditorGUIUtility.isProSkin ? new Color(.12f, .12f, .12f) : new Color(.82f, .82f, .82f));
            EditorGUI.LabelField(new Rect(rect.x + 4f, rect.y, 70f, rect.height), "0.00s", GameSystemsInspectorUI.SmallMutedStyle);
            EditorGUI.LabelField(new Rect(rect.xMax - 70f, rect.y, 66f, rect.height), $"{duration:0.00}s", GameSystemsInspectorUI.SmallMutedStyle);
        }

        static void DrawTimelineRow(Rect row, int index, FeedbackAsset asset, FeedbackCue cue,
            float start, float duration, float totalDuration)
        {
            Rect label = new(row.x, row.y + 2f, 124f, 20f);
            Rect track = new(label.xMax + 6f, row.y + 5f, row.width - label.width - 10f, 14f);
            string title = cue == null
                ? $"{index + 1}. Missing"
                : $"{index + 1}. {ShortName(cue)}";
            EditorGUI.LabelField(label, title, cue != null && cue.enabled ? EditorStyles.miniLabel : EditorStyles.miniBoldLabel);
            EditorGUI.DrawRect(track, EditorGUIUtility.isProSkin ? new Color(.18f, .18f, .18f) : new Color(.75f, .75f, .75f));
            if (cue == null) return;

            float start01 = Mathf.Clamp01(start / totalDuration);
            float width01 = Mathf.Clamp01(Mathf.Max(.02f, duration / totalDuration));
            Rect bar = new(track.x + track.width * start01, track.y, Mathf.Max(3f, track.width * width01), track.height);
            Color color = ColorFor(cue.kind);
            if (!cue.enabled) color = new Color(.45f, .45f, .45f, .7f);
            EditorGUI.DrawRect(bar, color);
            if (cue.chance < 100f)
                EditorGUI.LabelField(new Rect(track.xMax - 42f, row.y + 2f, 42f, 20f), $"{cue.chance:0}%", GameSystemsInspectorUI.SmallMutedStyle);
        }

        static float EstimateDuration(FeedbackSequence sequence)
        {
            float cursor = 0f;
            float maximum = 0f;
            for (int i = 0; i < sequence.Feedbacks.Count; i++)
            {
                FeedbackCue cue = sequence.Feedbacks[i] != null ? sequence.Feedbacks[i].Cue : null;
                if (cue == null) continue;
                float start = sequence.PlayMode == FeedbackPlayMode.Sequential ? cursor + cue.initialDelay : cue.initialDelay;
                float end = start + EffectiveDuration(cue);
                maximum = Mathf.Max(maximum, end);
                if (sequence.PlayMode == FeedbackPlayMode.Sequential) cursor = end;
            }
            return maximum;
        }

        static float EffectiveDuration(FeedbackCue cue)
            => Mathf.Max(.001f, cue.duration) * Mathf.Max(1, cue.repeats + 1) +
               Mathf.Max(0f, cue.repeatDelay) * Mathf.Max(0, cue.repeats);

        static string ShortName(FeedbackCue cue)
            => string.IsNullOrWhiteSpace(cue.label) ? cue.kind.ToString() : cue.label;

        static Color ColorFor(FeedbackKind kind) => kind switch
        {
            FeedbackKind.CameraShake or FeedbackKind.CameraZoom => new Color(.34f, .58f, .96f),
            FeedbackKind.Audio or FeedbackKind.AudioRandomized => new Color(.42f, .76f, .82f),
            FeedbackKind.ParticleBurst or FeedbackKind.InstantiatePooled => new Color(.44f, .76f, .42f),
            FeedbackKind.LightIntensity or FeedbackKind.ScreenFlash => new Color(.98f, .73f, .28f),
            FeedbackKind.TimeScale or FeedbackKind.FreezeFrame => new Color(.94f, .44f, .62f),
            FeedbackKind.TransformShake or FeedbackKind.Position or FeedbackKind.Rotation or FeedbackKind.Scale => new Color(.72f, .54f, .95f),
            _ => new Color(.58f, .62f, .68f)
        };

        static void ShowEmbeddedMenu(FeedbackSequence sequence)
        {
            var menu = new GenericMenu();
            foreach (FeedbackKind kind in Enum.GetValues(typeof(FeedbackKind)))
            {
                FeedbackKind captured = kind;
                menu.AddItem(new GUIContent(captured.ToString()), false, () =>
                {
                    Undo.RecordObject(sequence, "Add Embedded Feedback");
                    FeedbackAsset created = sequence.AddEmbedded(captured);
                    Selection.activeObject = created;
                });
            }
            menu.ShowAsContext();
        }
    }
}
#endif
