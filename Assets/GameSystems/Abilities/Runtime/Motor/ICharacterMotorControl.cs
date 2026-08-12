using UnityEngine;
namespace GameSystems.Abilities
{
    public interface ICharacterMotorControl
    { Vector3 Velocity { get; } void SetVelocity(Vector3 value); void SetVerticalVelocity(float value); void Teleport(Vector3 position); }
}
