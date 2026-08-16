#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GameSystems.Playables.EditorTools
{
    public static class GenericAnimationRetargeter
    {
        public sealed class Report
        {
            public readonly List<string> MissingBones = new();
            public readonly List<string> AmbiguousBones = new();
            public int RetargetedBones;
        }

        private readonly struct BoneRest
        {
            public readonly string Path;
            public readonly Quaternion Rotation;
            public readonly Quaternion ParentWorldRotation;

            public BoneRest(string path, Quaternion rotation, Quaternion parentWorldRotation)
            {
                Path = path;
                Rotation = rotation;
                ParentWorldRotation = parentWorldRotation;
            }
        }

        public static AnimationClip BuildClip(GameObject sourceSkeleton, GameObject targetSkeleton,
            AnimationClip sourceClip, string clipName, bool loop, out Report report)
        {
            if (sourceSkeleton == null) throw new ArgumentNullException(nameof(sourceSkeleton));
            if (targetSkeleton == null) throw new ArgumentNullException(nameof(targetSkeleton));
            if (sourceClip == null) throw new ArgumentNullException(nameof(sourceClip));

            report = new Report();
            Dictionary<string, BoneRest> sourceRest = ReadRestPose(sourceSkeleton, report);
            Dictionary<string, BoneRest> targetRest = ReadRestPose(targetSkeleton, report);
            var result = new AnimationClip
            {
                name = string.IsNullOrWhiteSpace(clipName) ? sourceClip.name : clipName,
                frameRate = sourceClip.frameRate
            };
            EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(sourceClip);
            foreach (var group in bindings
                         .Where(binding => binding.propertyName.StartsWith("m_LocalRotation", StringComparison.Ordinal))
                         .GroupBy(binding => binding.path))
            {
                string boneName = group.Key.Split('/').Last();
                if (!sourceRest.TryGetValue(boneName, out BoneRest sourceBone) ||
                    !targetRest.TryGetValue(boneName, out BoneRest targetBone))
                {
                    report.MissingBones.Add(boneName);
                    continue;
                }
                AnimationCurve x = FindCurve(sourceClip, group, "m_LocalRotation.x");
                AnimationCurve y = FindCurve(sourceClip, group, "m_LocalRotation.y");
                AnimationCurve z = FindCurve(sourceClip, group, "m_LocalRotation.z");
                AnimationCurve w = FindCurve(sourceClip, group, "m_LocalRotation.w");
                if (x == null || y == null || z == null || w == null) continue;
                WriteConvertedRotation(result, sourceClip, targetBone.Path, sourceBone,
                    targetBone, x, y, z, w);
                report.RetargetedBones++;
            }

            UnityEditor.AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(sourceClip);
            settings.loopTime = loop;
            settings.loopBlend = loop;
            settings.keepOriginalOrientation = true;
            settings.keepOriginalPositionXZ = true;
            settings.keepOriginalPositionY = true;
            AnimationUtility.SetAnimationClipSettings(result, settings);
            result.EnsureQuaternionContinuity();
            return result;
        }

        public static AnimationClip SaveClip(AnimationClip clip, string outputPath)
        {
            if (clip == null) throw new ArgumentNullException(nameof(clip));
            if (string.IsNullOrWhiteSpace(outputPath) || !outputPath.StartsWith("Assets/"))
                throw new ArgumentException("Output path must be inside Assets.", nameof(outputPath));
            string directory = Path.GetDirectoryName(outputPath)?.Replace('\\', '/');
            if (string.IsNullOrEmpty(directory) || !AssetDatabase.IsValidFolder(directory))
                throw new DirectoryNotFoundException(directory);
            AssetDatabase.DeleteAsset(outputPath);
            var saved = UnityEngine.Object.Instantiate(clip);
            saved.name = clip.name;
            AssetDatabase.CreateAsset(saved, outputPath);
            AssetDatabase.SaveAssets();
            return saved;
        }

        private static Dictionary<string, BoneRest> ReadRestPose(GameObject asset, Report report)
        {
            GameObject instance = UnityEngine.Object.Instantiate(asset);
            var result = new Dictionary<string, BoneRest>();
            foreach (IGrouping<string, Transform> group in instance.GetComponentsInChildren<Transform>(true)
                         .GroupBy(transform => transform.name))
            {
                if (group.Count() > 1)
                {
                    report.AmbiguousBones.Add(group.Key);
                    continue;
                }
                Transform bone = group.First();
                result.Add(group.Key, new BoneRest(
                    AnimationUtility.CalculateTransformPath(bone, instance.transform), bone.localRotation,
                    bone.parent != null ? bone.parent.rotation : Quaternion.identity));
            }
            UnityEngine.Object.DestroyImmediate(instance);
            return result;
        }

        private static void WriteConvertedRotation(AnimationClip result, AnimationClip source,
            string targetPath, BoneRest sourceBone, BoneRest targetBone,
            AnimationCurve x, AnimationCurve y, AnimationCurve z, AnimationCurve w)
        {
            int count = Mathf.Max(1, Mathf.CeilToInt(source.length * source.frameRate));
            var keys = new[] { new Keyframe[count + 1], new Keyframe[count + 1],
                new Keyframe[count + 1], new Keyframe[count + 1] };
            Quaternion previous = Quaternion.identity;
            for (var sample = 0; sample <= count; sample++)
            {
                float time = source.length * sample / count;
                Quaternion sourcePose = Normalize(new Quaternion(x.Evaluate(time), y.Evaluate(time),
                    z.Evaluate(time), w.Evaluate(time)));
                Quaternion sourceParentDelta = sourcePose * Quaternion.Inverse(sourceBone.Rotation);
                Quaternion sourceToTargetParent = Quaternion.Inverse(targetBone.ParentWorldRotation) *
                                                   sourceBone.ParentWorldRotation;
                Quaternion targetParentDelta = sourceToTargetParent * sourceParentDelta *
                                                Quaternion.Inverse(sourceToTargetParent);
                Quaternion targetPose = Normalize(targetParentDelta * targetBone.Rotation);
                if (sample > 0 && Quaternion.Dot(previous, targetPose) < 0f)
                    targetPose = new Quaternion(-targetPose.x, -targetPose.y, -targetPose.z, -targetPose.w);
                previous = targetPose;
                keys[0][sample] = new Keyframe(time, targetPose.x);
                keys[1][sample] = new Keyframe(time, targetPose.y);
                keys[2][sample] = new Keyframe(time, targetPose.z);
                keys[3][sample] = new Keyframe(time, targetPose.w);
            }
            string[] properties = { "m_LocalRotation.x", "m_LocalRotation.y",
                "m_LocalRotation.z", "m_LocalRotation.w" };
            for (var axis = 0; axis < properties.Length; axis++)
                AnimationUtility.SetEditorCurve(result,
                    EditorCurveBinding.FloatCurve(targetPath, typeof(Transform), properties[axis]),
                    new AnimationCurve(keys[axis]));
        }

        private static AnimationCurve FindCurve(AnimationClip clip,
            IEnumerable<EditorCurveBinding> bindings, string property)
        {
            EditorCurveBinding binding = bindings.FirstOrDefault(candidate => candidate.propertyName == property);
            return string.IsNullOrEmpty(binding.propertyName) ? null : AnimationUtility.GetEditorCurve(clip, binding);
        }

        private static Quaternion Normalize(Quaternion value)
        {
            float magnitude = Mathf.Sqrt(value.x * value.x + value.y * value.y +
                                         value.z * value.z + value.w * value.w);
            return magnitude < .00001f ? Quaternion.identity : new Quaternion(
                value.x / magnitude, value.y / magnitude, value.z / magnitude, value.w / magnitude);
        }
    }
}
#endif
