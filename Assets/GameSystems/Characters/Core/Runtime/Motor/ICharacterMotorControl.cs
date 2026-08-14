using UnityEngine;
namespace GameSystems.Characters
{
    public interface ICharacterMotorControl
    { Vector3 Velocity { get; } void SetVelocity(Vector3 value); void SetVerticalVelocity(float value); void Teleport(Vector3 position); }

    public interface ICharacterGravityFrame
    {
        Vector3 GravityDirection { get; }
        Vector3 UpDirection { get; }
        float UpwardSpeed { get; }
        void SetUpwardSpeed(float value);
    }
}
