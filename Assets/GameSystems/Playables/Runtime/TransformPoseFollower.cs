using UnityEngine;

namespace GameSystems.Playables
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(10000)]
    public sealed class TransformPoseFollower : MonoBehaviour
    {
        [SerializeField] Transform anchor;
        [SerializeField] Vector3 localPositionOffset;
        [SerializeField] Quaternion localRotationOffset = Quaternion.identity;

        public void Capture(Transform source)
        {
            anchor = source;
            if (anchor == null) return;
            localPositionOffset = anchor.InverseTransformPoint(transform.position);
            localRotationOffset = Quaternion.Inverse(anchor.rotation) * transform.rotation;
            Apply();
        }

        void LateUpdate() => Apply();

        void Apply()
        {
            if (anchor == null) return;
            transform.SetPositionAndRotation(anchor.TransformPoint(localPositionOffset),
                anchor.rotation * localRotationOffset);
        }
    }
}
