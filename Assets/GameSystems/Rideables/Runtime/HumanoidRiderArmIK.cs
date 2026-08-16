using GameSystems.Playables;
using UnityEngine;

namespace GameSystems.Rideables
{
    [DisallowMultipleComponent]
    public sealed class HumanoidRiderArmIK : MonoBehaviour, IPlayablePostProcessor
    {
        [SerializeField] Animator animator;
        [SerializeField] Transform leftHandTarget;
        [SerializeField] Transform rightHandTarget;
        [SerializeField] Transform riderFrame;
        [SerializeField, Range(0f, 1f)] float weight = 1f;

        public int Order => 300;

        public void Configure(Animator riderAnimator, Transform leftTarget,
            Transform rightTarget, Transform frame, float influence = 1f)
        {
            animator = riderAnimator;
            leftHandTarget = leftTarget;
            rightHandTarget = rightTarget;
            riderFrame = frame;
            weight = Mathf.Clamp01(influence);
        }

        public void ApplyPlayablePostProcess()
        {
            if (animator == null || !animator.isHuman || weight <= 0f) return;
            Vector3 outward = riderFrame != null ? riderFrame.right : transform.right;
            HumanoidTwoBoneIK.Solve(animator, HumanBodyBones.LeftUpperArm,
                HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand,
                leftHandTarget, -outward, weight);
            HumanoidTwoBoneIK.Solve(animator, HumanBodyBones.RightUpperArm,
                HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand,
                rightHandTarget, outward, weight);
        }
    }
}
