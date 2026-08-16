using System.Collections.Generic;
using UnityEngine;

namespace GameSystems.Camera
{
    public readonly struct CameraDynamicFrame
    {
        public readonly Vector3 FollowPoint;
        public readonly Vector3 LookPoint;
        public readonly float PositionSmoothTime;
        public readonly float RotationSharpness;

        public CameraDynamicFrame(Vector3 followPoint, Vector3 lookPoint,
            float positionSmoothTime, float rotationSharpness)
        {
            FollowPoint = followPoint;
            LookPoint = lookPoint;
            PositionSmoothTime = positionSmoothTime;
            RotationSharpness = rotationSharpness;
        }
    }

    public interface ICameraDynamicFramingProvider
    {
        bool TryGetCameraFrame(GameObject target, CameraFramingDefinition definition,
            out CameraDynamicFrame frame);
    }

    public static class CameraDynamicFramingRegistry
    {
        static readonly Dictionary<GameObject, ICameraDynamicFramingProvider> Providers = new();

        public static void Register(GameObject target, ICameraDynamicFramingProvider provider)
        {
            if (target == null || provider == null) return;
            Providers[target] = provider;
        }

        public static void Unregister(GameObject target, ICameraDynamicFramingProvider provider)
        {
            if (target == null) return;
            if (Providers.TryGetValue(target, out ICameraDynamicFramingProvider current) &&
                ReferenceEquals(current, provider))
                Providers.Remove(target);
        }

        public static bool TryGet(GameObject target, out ICameraDynamicFramingProvider provider)
        {
            provider = null;
            return target != null && Providers.TryGetValue(target, out provider) && provider != null;
        }
    }
}
