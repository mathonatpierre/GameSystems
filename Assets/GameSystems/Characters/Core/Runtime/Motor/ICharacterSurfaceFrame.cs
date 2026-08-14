using UnityEngine;

namespace GameSystems.Characters
{
    public interface ICharacterSurfaceFrame
    {
        Vector3 SurfaceUp { get; }
        Vector3 SurfaceForward { get; }
    }

    public interface ICharacterSurfaceAlignmentControl
    {
        void SetFollowSurfaceForward(bool value);
        void SetSurfaceFrame(Vector3 up, Vector3 forward);
        void SetSurfaceGround(Collider collider, Vector3 point, Vector3 normal);
        void ClearSurfaceGround();
        void SetSurfaceConstraint(bool value);
        void SetCollisionFrame(bool aligned, Vector3 up, Vector3 forward);
        void MoveConstrained(Vector3 targetPosition);
    }
}
