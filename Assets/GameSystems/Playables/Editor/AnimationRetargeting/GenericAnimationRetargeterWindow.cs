#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace GameSystems.Playables.EditorTools
{
    public sealed class GenericAnimationRetargeterWindow : EditorWindow
    {
        [SerializeField] GameObject sourceSkeleton;
        [SerializeField] GameObject targetSkeleton;
        [SerializeField] AnimationClip sourceClip;
        [SerializeField] string outputPath = "Assets/RetargetedAnimation.anim";
        [SerializeField] bool loop = true;
        GameObject previewInstance;
        AnimationClip previewClip;
        GenericAnimationRetargeter.Report report;
        float previewTime;
        bool playing;
        double previousTime;

        [MenuItem("Game Systems/Playables/Animation Retargeter")]
        public static void Open() => GetWindow<GenericAnimationRetargeterWindow>("Animation Retargeter");

        void OnEnable()
        {
            EditorApplication.update += Tick;
            previousTime = EditorApplication.timeSinceStartup;
        }

        void OnDisable()
        {
            EditorApplication.update -= Tick;
            DestroyPreview();
            if (previewClip != null && !AssetDatabase.Contains(previewClip)) DestroyImmediate(previewClip);
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("Generic Animation Retargeting", EditorStyles.boldLabel);
            sourceSkeleton = (GameObject)EditorGUILayout.ObjectField("Source Skeleton", sourceSkeleton,
                typeof(GameObject), false);
            targetSkeleton = (GameObject)EditorGUILayout.ObjectField("Target Skeleton", targetSkeleton,
                typeof(GameObject), false);
            sourceClip = (AnimationClip)EditorGUILayout.ObjectField("Source Clip", sourceClip,
                typeof(AnimationClip), false);
            loop = EditorGUILayout.Toggle("Loop Preview", loop);
            outputPath = EditorGUILayout.TextField("Output Asset", outputPath);

            using (new EditorGUI.DisabledScope(sourceSkeleton == null || targetSkeleton == null || sourceClip == null))
            {
                if (GUILayout.Button("Build Preview")) BuildPreview();
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button(playing ? "Pause" : "Play")) playing = !playing;
                    if (GUILayout.Button("Export .anim")) Export();
                }
            }

            if (previewClip != null)
            {
                previewTime = EditorGUILayout.Slider("Time", previewTime, 0f, previewClip.length);
                SamplePreview();
            }
            if (report != null)
            {
                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField($"Retargeted bones: {report.RetargetedBones}");
                EditorGUILayout.LabelField($"Missing: {report.MissingBones.Count}   Ambiguous: {report.AmbiguousBones.Count}");
                if (report.MissingBones.Count > 0)
                    EditorGUILayout.HelpBox(string.Join(", ", report.MissingBones), MessageType.Warning);
                if (report.AmbiguousBones.Count > 0)
                    EditorGUILayout.HelpBox(string.Join(", ", report.AmbiguousBones), MessageType.Error);
            }
        }

        void BuildPreview()
        {
            DestroyPreview();
            if (previewClip != null && !AssetDatabase.Contains(previewClip)) DestroyImmediate(previewClip);
            previewClip = GenericAnimationRetargeter.BuildClip(sourceSkeleton, targetSkeleton,
                sourceClip, sourceClip.name + "_Retargeted", loop, out report);
            previewInstance = (GameObject)PrefabUtility.InstantiatePrefab(targetSkeleton);
            if (previewInstance == null) previewInstance = Instantiate(targetSkeleton);
            previewInstance.name = "Retarget Preview (temporary)";
            previewInstance.hideFlags = HideFlags.DontSave;
            Selection.activeGameObject = previewInstance;
            SceneView.lastActiveSceneView?.FrameSelected();
            previewTime = 0f;
            SamplePreview();
        }

        void Export()
        {
            BuildPreview();
            GenericAnimationRetargeter.SaveClip(previewClip, outputPath);
            AssetDatabase.Refresh();
        }

        void Tick()
        {
            double now = EditorApplication.timeSinceStartup;
            float delta = (float)(now - previousTime);
            previousTime = now;
            if (!playing || previewClip == null) return;
            previewTime += delta;
            if (previewTime > previewClip.length)
                previewTime = loop ? previewTime % previewClip.length : previewClip.length;
            SamplePreview();
            Repaint();
        }

        void SamplePreview()
        {
            if (previewInstance == null || previewClip == null) return;
            previewClip.SampleAnimation(previewInstance, previewTime);
            SceneView.RepaintAll();
        }

        void DestroyPreview()
        {
            if (previewInstance != null) DestroyImmediate(previewInstance);
        }
    }
}
#endif
