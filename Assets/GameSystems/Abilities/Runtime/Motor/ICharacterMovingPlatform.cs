using UnityEngine;

namespace GameSystems.Abilities
{
    public interface ICharacterMovingPlatform
    {
        Vector3 FrameDelta { get; }
        Transform PlatformTransform { get; }
        Vector3 TravelVector { get; }
        Vector3 PredictDisplacement(float secondsAhead);
    }
}
