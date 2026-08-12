using System.Collections.Generic;
using UnityEngine;

namespace GameSystems.Feedbacks
{
    [CreateAssetMenu(fileName = "SEQ_", menuName = "Game Systems/Feedbacks/Feedback Sequence")]
    public sealed class FeedbackSequence : ScriptableObject
    {
        [SerializeField] string description;
        [SerializeField] FeedbackPlayMode playMode = FeedbackPlayMode.Parallel;
        [SerializeField] FeedbackConcurrency concurrency = FeedbackConcurrency.AllowMultiple;
        [SerializeField, Min(1)] int maximumInstances = 4;
        [SerializeField] string channel;
        [SerializeField] List<FeedbackAsset> feedbacks = new();

        public FeedbackPlayMode PlayMode => playMode;
        public FeedbackConcurrency Concurrency => concurrency;
        public int MaximumInstances => Mathf.Max(1, maximumInstances);
        public string Channel => channel;
        public string Description => description;
        public IReadOnlyList<FeedbackAsset> Feedbacks => feedbacks;

#if UNITY_EDITOR
        public void Configure(FeedbackPlayMode mode, FeedbackConcurrency policy, int limit = 4, string sequenceChannel = null)
        {
            playMode = mode; concurrency = policy; maximumInstances = Mathf.Max(1, limit); channel = sequenceChannel;
            UnityEditor.EditorUtility.SetDirty(this);
        }

        public void ReplaceFeedbacks(IEnumerable<FeedbackAsset> assets)
        {
            feedbacks.Clear();
            if (assets != null) feedbacks.AddRange(assets);
            UnityEditor.EditorUtility.SetDirty(this);
        }

        public FeedbackAsset AddEmbedded(FeedbackKind kind)
        {
            var asset = CreateInstance<FeedbackAsset>();
            asset.name = "EMBED_" + kind;
            asset.Configure(new FeedbackCue { kind = kind, label = kind.ToString() });
            UnityEditor.AssetDatabase.AddObjectToAsset(asset, this);
            feedbacks.Add(asset);
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.SaveAssets();
            return asset;
        }

        public void AddReference(FeedbackAsset asset)
        {
            if (asset == null) return;
            feedbacks.Add(asset);
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }

}
