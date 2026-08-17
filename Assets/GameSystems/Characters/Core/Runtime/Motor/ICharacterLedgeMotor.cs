using UnityEngine;

namespace GameSystems.Characters
{
    public readonly struct CharacterLedgeAnchor
    {
        readonly Collider collider;
        readonly Transform support;
        readonly Vector3 hangPosition;
        readonly Vector3 standPosition;
        readonly Vector3 surfaceNormal;
        readonly Vector3 wallNormal;
        readonly Vector3 gripPoint;

        public Collider Collider => collider;
        public Transform Support => support;
        public bool IsValid => collider != null && collider.enabled &&
                               collider.gameObject.activeInHierarchy;
        public Vector3 HangPosition => ToWorldPoint(hangPosition);
        public Vector3 StandPosition => ToWorldPoint(standPosition);
        public Vector3 SurfaceNormal => ToWorldDirection(surfaceNormal);
        public Vector3 WallNormal => ToWorldDirection(wallNormal);
        public Vector3 GripPoint => ToWorldPoint(gripPoint);

        public CharacterLedgeAnchor(Collider collider, Vector3 hangPosition,
            Vector3 standPosition, Vector3 surfaceNormal, Vector3 wallNormal,
            Vector3 gripPoint)
        {
            this.collider = collider;
            support = collider != null ? collider.transform : null;
            this.hangPosition = support != null ? support.InverseTransformPoint(hangPosition) : hangPosition;
            this.standPosition = support != null ? support.InverseTransformPoint(standPosition) : standPosition;
            this.surfaceNormal = support != null ? support.InverseTransformDirection(surfaceNormal) : surfaceNormal;
            this.wallNormal = support != null ? support.InverseTransformDirection(wallNormal) : wallNormal;
            this.gripPoint = support != null ? support.InverseTransformPoint(gripPoint) : gripPoint;
        }

        Vector3 ToWorldPoint(Vector3 value) => support != null
            ? support.TransformPoint(value) : value;

        Vector3 ToWorldDirection(Vector3 value)
        {
            Vector3 direction = support != null ? support.TransformDirection(value) : value;
            return direction.sqrMagnitude > .0001f ? direction.normalized : direction;
        }
    }

    public interface ICharacterLedgeMotor
    {
        bool IsLedgeAnchored { get; }
        CharacterLedgeAnchor LedgeAnchor { get; }
        bool TryFindLedge(out CharacterLedgeAnchor anchor);
        void SetLedgeAnchor(in CharacterLedgeAnchor anchor);
        void MoveLedgeAnchor(Vector3 position);
        void SetLedgeClimbing(bool value);
        void SetLedgeClimbProgress(float value);
        void ClearLedgeAnchor();
    }
}
