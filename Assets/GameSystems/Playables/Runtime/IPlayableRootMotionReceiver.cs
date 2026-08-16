using UnityEngine;

namespace GameSystems.Playables
{
    public interface IPlayableRootMotionReceiver
    {
        void ApplyPlayableRootMotion(Vector3 deltaPosition, Quaternion deltaRotation);
    }
}
