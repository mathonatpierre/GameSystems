using UnityEngine;

namespace GameSystems.Playables
{
    [DisallowMultipleComponent]
    public sealed class HumanoidLookAtPostProcessor : MonoBehaviour, IPlayablePostProcessor
    {
        [SerializeField] Animator animator;
        [SerializeField] UnityPlayableAnimationPlayer player;
        [SerializeField] string horizontalParameter = "LookHorizontal";
        [SerializeField] string verticalParameter = "LookVertical";
        [SerializeField, Range(0f, 90f)] float maximumYaw = 38f;
        [SerializeField, Range(0f, 60f)] float maximumPitch = 24f;
        [SerializeField, Range(0f, 1f)] float chestWeight = .18f;
        [SerializeField, Range(0f, 1f)] float neckWeight = .3f;
        [SerializeField, Range(0f, 1f)] float headWeight = .52f;
        [SerializeField, Min(.01f)] float sharpness = 12f;
        Vector2 smoothedDirection;

        public int Order => 400;

        public void Configure(Animator targetAnimator, UnityPlayableAnimationPlayer animationPlayer,
            string horizontal, string vertical)
        {
            animator = targetAnimator;
            player = animationPlayer;
            horizontalParameter = horizontal;
            verticalParameter = vertical;
        }

        public void ApplyPlayablePostProcess()
        {
            if (animator == null || player == null || !animator.isHuman) return;
            Vector2 requested = new(player.Context.GetFloat(horizontalParameter),
                player.Context.GetFloat(verticalParameter));
            smoothedDirection = Vector2.Lerp(smoothedDirection,
                Vector2.ClampMagnitude(requested, 1f),
                1f - Mathf.Exp(-sharpness * Time.deltaTime));
            Quaternion worldOffset = Quaternion.AngleAxis(
                                         smoothedDirection.x * maximumYaw, transform.up) *
                                     Quaternion.AngleAxis(
                                         -smoothedDirection.y * maximumPitch, transform.right);
            Apply(HumanBodyBones.Chest, worldOffset, chestWeight);
            Apply(HumanBodyBones.Neck, worldOffset, neckWeight);
            Apply(HumanBodyBones.Head, worldOffset, headWeight);
        }

        void Apply(HumanBodyBones boneId, Quaternion offset, float weight)
        {
            Transform bone = animator.GetBoneTransform(boneId);
            if (bone == null || weight <= 0f) return;
            bone.rotation = Quaternion.Slerp(bone.rotation, offset * bone.rotation, weight);
        }
    }
}
