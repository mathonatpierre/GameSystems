using UnityEngine;

namespace GameSystems.Characters
{
    public readonly struct CharacterLedgeAnchor
    {
        public readonly Collider Collider;
        public readonly Vector3 HangPosition;
        public readonly Vector3 StandPosition;
        public readonly Vector3 SurfaceNormal;
        public readonly Vector3 WallNormal;
        public readonly Vector3 GripPoint;

        public CharacterLedgeAnchor(Collider collider, Vector3 hangPosition,
            Vector3 standPosition, Vector3 surfaceNormal, Vector3 wallNormal,
            Vector3 gripPoint)
        {
            Collider = collider;
            HangPosition = hangPosition;
            StandPosition = standPosition;
            SurfaceNormal = surfaceNormal;
            WallNormal = wallNormal;
            GripPoint = gripPoint;
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
