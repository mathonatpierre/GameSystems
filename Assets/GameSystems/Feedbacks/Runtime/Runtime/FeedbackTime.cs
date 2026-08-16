using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameSystems.Feedbacks
{
    public static class FeedbackTime
    {
        public sealed class FreezeLease { internal object owner; internal float scale; }
        static readonly List<FreezeLease> freezes = new();
        static float baseScale = 1f;

        public static IEnumerator Freeze(object owner, float scale, float duration)
        {
            if (freezes.Count == 0) baseScale = Time.timeScale;
            FreezeLease lease = new() { owner = owner, scale = Mathf.Clamp(scale, 0f, 1f) };
            freezes.Add(lease);
            Apply();
            try
            {
                float end = Time.unscaledTime + Mathf.Max(0f, duration);
                while (Time.unscaledTime < end) yield return null;
            }
            finally { Release(lease); }
        }

        public static FreezeLease Acquire(object owner, float scale)
        {
            if (freezes.Count == 0) baseScale = Time.timeScale;
            FreezeLease lease = new() { owner = owner, scale = Mathf.Clamp(scale, 0f, 1f) };
            freezes.Add(lease);
            Apply();
            return lease;
        }

        public static void Release(FreezeLease lease)
        {
            if (lease == null) return;
            freezes.Remove(lease);
            Apply();
        }

        public static void ReleaseAll(object owner)
        {
            for (int i = freezes.Count - 1; i >= 0; i--)
                if (ReferenceEquals(freezes[i].owner, owner)) freezes.RemoveAt(i);
            Apply();
        }

        static void Apply()
        {
            if (freezes.Count == 0) { Time.timeScale = baseScale; return; }
            float scale = baseScale;
            for (int i = 0; i < freezes.Count; i++) scale = Mathf.Min(scale, freezes[i].scale);
            Time.timeScale = scale;
        }
    }
}
