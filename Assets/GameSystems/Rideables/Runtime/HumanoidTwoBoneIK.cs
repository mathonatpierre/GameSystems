using UnityEngine;

namespace GameSystems.Rideables
{
    static class HumanoidTwoBoneIK
    {
        public static void Solve(Animator animator, HumanBodyBones upperBone,
            HumanBodyBones lowerBone, HumanBodyBones endBone, Transform target,
            Vector3 poleDirection, float weight)
        {
            if (animator == null || target == null || weight <= 0f) return;
            Transform upper = animator.GetBoneTransform(upperBone);
            Transform lower = animator.GetBoneTransform(lowerBone);
            Transform end = animator.GetBoneTransform(endBone);
            if (upper == null || lower == null || end == null) return;

            Vector3 origin = upper.position;
            Vector3 destination = target.position;
            float upperLength = Vector3.Distance(origin, lower.position);
            float lowerLength = Vector3.Distance(lower.position, end.position);
            Vector3 toTarget = destination - origin;
            float distance = Mathf.Clamp(toTarget.magnitude, .001f,
                Mathf.Max(.001f, upperLength + lowerLength - .001f));
            Vector3 direction = toTarget.normalized;
            Vector3 pole = Vector3.ProjectOnPlane(poleDirection, direction).normalized;
            if (pole.sqrMagnitude < .001f)
                pole = Vector3.ProjectOnPlane(Vector3.up, direction).normalized;
            float along = (upperLength * upperLength - lowerLength * lowerLength +
                           distance * distance) / (2f * distance);
            float bend = Mathf.Sqrt(Mathf.Max(0f, upperLength * upperLength - along * along));
            Vector3 desiredJoint = origin + direction * along + pole * bend;

            Quaternion upperStart = upper.rotation;
            Quaternion upperSolved = Quaternion.FromToRotation(lower.position - origin,
                desiredJoint - origin) * upperStart;
            upper.rotation = Quaternion.Slerp(upperStart, upperSolved, weight);

            Quaternion lowerStart = lower.rotation;
            Quaternion lowerSolved = Quaternion.FromToRotation(end.position - lower.position,
                destination - lower.position) * lowerStart;
            lower.rotation = Quaternion.Slerp(lowerStart, lowerSolved, weight);
        }
    }
}
