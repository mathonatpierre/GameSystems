using UnityEngine;

namespace GameSystems.Feedbacks
{
    [CreateAssetMenu(fileName = "FB_", menuName = "Game Systems/Feedbacks/Feedback Asset")]
    public sealed class FeedbackAsset : ScriptableObject
    {
        [SerializeField] FeedbackCue cue = new();
        public FeedbackCue Cue => cue;

#if UNITY_EDITOR
        public void Configure(FeedbackCue value)
        {
            cue = value ?? new FeedbackCue();
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
