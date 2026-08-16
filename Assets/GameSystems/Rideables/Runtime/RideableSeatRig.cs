using GameSystems.Playables;
using UnityEngine;

namespace GameSystems.Rideables
{
    [DisallowMultipleComponent]
    public sealed class RideableSeatRig : MonoBehaviour
    {
        [SerializeField] Transform anchor;
        [SerializeField] TransformPoseFollower follower;
        [SerializeField] Transform leftFoot;
        [SerializeField] Transform rightFoot;
        [SerializeField] Transform leftHand;
        [SerializeField] Transform rightHand;
        [SerializeField] Transform mountPoint;
        [SerializeField] Transform dismountPoint;
        [SerializeField] RideableRider occupant;
        [SerializeField, Min(.005f)] float gizmoSize = .045f;

        public Transform Anchor => anchor;
        public Transform LeftFoot => leftFoot;
        public Transform RightFoot => rightFoot;
        public Transform LeftHand => leftHand;
        public Transform RightHand => rightHand;
        public Transform MountPoint => mountPoint != null ? mountPoint : dismountPoint;
        public Transform DismountPoint => dismountPoint;
        public RideableRider Occupant => occupant;
        public bool IsAvailable => occupant == null;

        public void Configure(Transform seatAnchor, TransformPoseFollower poseFollower,
            Transform leftFootTarget, Transform rightFootTarget,
            Transform leftHandTarget, Transform rightHandTarget,
            Transform entryPoint, Transform exitPoint)
        {
            anchor = seatAnchor;
            follower = poseFollower;
            leftFoot = leftFootTarget;
            rightFoot = rightFootTarget;
            leftHand = leftHandTarget;
            rightHand = rightHandTarget;
            mountPoint = entryPoint;
            dismountPoint = exitPoint;
            SyncFollower();
        }

        internal bool TryOccupy(RideableRider rider)
        {
            if (rider == null || (occupant != null && occupant != rider)) return false;
            occupant = rider;
            return true;
        }

        internal void Release(RideableRider rider)
        {
            if (occupant == rider) occupant = null;
        }

        public void SyncFollower()
        {
            if (follower != null && anchor != null) follower.Capture(anchor);
        }

        void OnDrawGizmos()
        {
            DrawPoint(transform.position, new Color(1f, .72f, .12f), gizmoSize * 1.35f);
            DrawTarget(leftFoot, new Color(.25f, .65f, 1f));
            DrawTarget(rightFoot, new Color(.2f, 1f, .75f));
            DrawTarget(leftHand, new Color(1f, .35f, .7f));
            DrawTarget(rightHand, new Color(1f, .55f, .25f));
            DrawTarget(mountPoint, new Color(.35f, 1f, .45f));
            DrawTarget(dismountPoint, new Color(.85f, .85f, .85f));
        }

        void DrawTarget(Transform target, Color color)
        {
            if (target == null) return;
            Gizmos.color = new Color(color.r, color.g, color.b, .45f);
            Gizmos.DrawLine(transform.position, target.position);
            DrawPoint(target.position, color, gizmoSize);
        }

        static void DrawPoint(Vector3 position, Color color, float size)
        {
            Gizmos.color = color;
            Gizmos.DrawSphere(position, size);
            Gizmos.DrawWireSphere(position, size * 1.45f);
        }
    }
}
