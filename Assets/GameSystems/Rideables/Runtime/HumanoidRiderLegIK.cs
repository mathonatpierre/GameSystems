using GameSystems.Playables;
using UnityEngine;

namespace GameSystems.Rideables
{
    [DisallowMultipleComponent]
    public sealed class HumanoidRiderLegIK : MonoBehaviour, IPlayablePostProcessor
    {
        public int Order => 200;

        [SerializeField] Animator animator;
        [SerializeField] Transform leftFootTarget;
        [SerializeField] Transform rightFootTarget;
        [SerializeField] Transform forwardReference;
        [SerializeField, Range(0f, 1f)] float weight = 1f;

        public void Configure(Animator riderAnimator, Transform leftTarget, Transform rightTarget,
            Transform vehicleForward, float influence = 1f)
        {
            animator = riderAnimator;
            leftFootTarget = leftTarget;
            rightFootTarget = rightTarget;
            forwardReference = vehicleForward;
            weight = Mathf.Clamp01(influence);
        }

        public void ApplyPlayablePostProcess()
        {
            if (animator == null || !animator.isHuman || weight <= 0f) return;
            Solve(HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg,
                HumanBodyBones.LeftFoot, leftFootTarget);
            Solve(HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg,
                HumanBodyBones.RightFoot, rightFootTarget);
        }

        void Solve(HumanBodyBones upperBone, HumanBodyBones lowerBone,
            HumanBodyBones footBone, Transform target)
        {
            Vector3 forward = forwardReference != null ? forwardReference.forward : transform.forward;
            HumanoidTwoBoneIK.Solve(animator, upperBone, lowerBone, footBone,
                target, forward, weight);
        }
    }
}
