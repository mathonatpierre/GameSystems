using UnityEngine;

namespace GameSystems.Playables
{
    [DisallowMultipleComponent]
    public sealed class AnimatorStateSynchronizer : MonoBehaviour
    {
        [SerializeField] Animator source;
        [SerializeField] Animator target;
        [SerializeField, Min(0)] int layer;
        public void Configure(Animator sourceAnimator, Animator targetAnimator, int animatorLayer = 0)
        {
            source = sourceAnimator;
            target = targetAnimator;
            layer = Mathf.Max(0, animatorLayer);
        }

        void LateUpdate()
        {
            if (source == null || target == null || !source.isActiveAndEnabled || !target.isActiveAndEnabled)
                return;
            AnimatorStateInfo state = source.GetCurrentAnimatorStateInfo(layer);
            if (state.fullPathHash == 0) return;
            target.speed = 0f;
            target.Play(state.fullPathHash, layer, Mathf.Repeat(state.normalizedTime, 1f));
            target.Update(0f);
        }
    }
}
