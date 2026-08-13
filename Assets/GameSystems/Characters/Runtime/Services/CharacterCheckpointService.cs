using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace GameSystems.Characters
{
    [System.Serializable]
    [MovedFrom(true, "GameSystems.Abilities", "GameSystems.Abilities", "CharacterCheckpointService")]
    public sealed class CharacterCheckpointService : ICharacterCheckpointService
    {
        [SerializeField, Min(0f)] float horizontalEdgeMargin = .58f;
        [SerializeField, Min(0f)] float depthEdgeMargin = .12f;
        ICharacterMotor motor;
        Transform owner;
        Vector3 checkpoint;
        Transform support;
        Vector3 localPosition;

        public void Configure(Transform character, ICharacterMotor characterMotor)
        {
            owner = character;
            motor = characterMotor;
            checkpoint = owner != null ? owner.position : Vector3.zero;
        }

        public void Observe(in CharacterMotorResult result)
        {
            if (!result.Ground.IsGrounded) return;
            Collider collider = result.Ground.Collider;
            if (collider == null ||
                collider.GetComponentInParent(typeof(ICheckpointExcludedSurface)) != null) return;
            Bounds bounds = collider.bounds;
            float minX = bounds.min.x + horizontalEdgeMargin;
            float maxX = bounds.max.x - horizontalEdgeMargin;
            if (owner == null) return;
            Vector3 safe = owner.position;
            safe.x = minX <= maxX ? Mathf.Clamp(safe.x, minX, maxX) : bounds.center.x;
            float minZ = bounds.min.z + depthEdgeMargin;
            float maxZ = bounds.max.z - depthEdgeMargin;
            safe.z = minZ <= maxZ ? Mathf.Clamp(safe.z, minZ, maxZ) : bounds.center.z;
            checkpoint = safe;
            ICharacterMovingPlatform movingPlatform = collider
                .GetComponentInParent(typeof(ICharacterMovingPlatform)) as ICharacterMovingPlatform;
            support = movingPlatform?.PlatformTransform;
            localPosition = support != null ? support.InverseTransformPoint(safe) : Vector3.zero;
        }

        public void Respawn()
        {
            if (motor == null) return;
            foreach (MonoBehaviour behaviour in Object.FindObjectsByType<MonoBehaviour>(
                         FindObjectsInactive.Include))
                if (behaviour is ICharacterRespawnResettable resettable)
                    resettable.ResetForCharacterRespawn();
            motor.ResetMotor();
            Vector3 destination = support != null ? support.TransformPoint(localPosition) : checkpoint;
            if (motor is ICharacterMotorControl control) control.Teleport(destination);
            if (motor is CharacterControllerMotor controllerMotor) controllerMotor.ReinitializeGroundContact();
        }
    }

}
