using GameSystems.Playables;
using UnityEngine;

namespace GameSystems.Rideables
{
    [DisallowMultipleComponent]
    public sealed class HumanoidRiderSeatFitter : MonoBehaviour, IPlayablePostProcessor
    {
        public int Order => 100;

        [SerializeField] Animator animator;
        [SerializeField] Transform riderRoot;
        [SerializeField] Transform seat;
        [SerializeField, Range(0f, 1f)] float positionWeight = 1f;
        [SerializeField, Range(0f, 1f)] float rotationWeight = 1f;
        [SerializeField] Quaternion visualRotationOffset = Quaternion.identity;

        public void Configure(Animator riderAnimator, Transform root, Transform seatTarget,
            float weight = 1f, Quaternion? rotationOffset = null)
        {
            animator = riderAnimator;
            riderRoot = root;
            seat = seatTarget;
            positionWeight = Mathf.Clamp01(weight);
            rotationWeight = Mathf.Clamp01(weight);
            visualRotationOffset = rotationOffset ?? Quaternion.identity;
        }

        public void ApplyPlayablePostProcess()
        {
            if (animator == null || riderRoot == null || seat == null || !animator.isHuman) return;
            Transform visualRoot = animator.transform;
            Quaternion targetRotation = seat.rotation * visualRotationOffset;
            visualRoot.rotation = Quaternion.Slerp(visualRoot.rotation, targetRotation, rotationWeight);

            Transform hips = animator.GetBoneTransform(HumanBodyBones.Hips);
            if (hips == null) return;
            Vector3 correction = seat.position - hips.position;
            riderRoot.position += correction * positionWeight;
        }
    }
}
