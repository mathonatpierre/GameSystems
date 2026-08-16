using UnityEngine;

namespace GameSystems.Characters
{
    public static class CharacterGroundPlacement
    {
        public static Vector3 GetSupportPoint(Transform character, CharacterController controller,
            Vector3 surfaceNormal)
        {
            if (character == null || controller == null) return character != null
                ? character.position : Vector3.zero;
            surfaceNormal.Normalize();
            Vector3 scale = character.lossyScale;
            float radiusScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
            float radius = controller.radius * radiusScale;
            float height = Mathf.Max(controller.height * Mathf.Abs(scale.y), radius * 2f);
            float segmentHalf = Mathf.Max(0f, height * .5f - radius);
            Vector3 center = character.TransformPoint(controller.center);
            float extent = radius + segmentHalf * Mathf.Abs(Vector3.Dot(surfaceNormal, character.up));
            return center - surfaceNormal * extent;
        }

        public static Vector3 GetRootPositionOnSurface(Transform character,
            CharacterController controller, Vector3 surfacePoint, Vector3 surfaceNormal,
            float clearance = -1f)
        {
            if (character == null || controller == null) return surfacePoint;
            surfaceNormal.Normalize();
            float resolvedClearance = clearance >= 0f
                ? clearance
                : Mathf.Max(.015f, controller.skinWidth);
            Vector3 support = GetSupportPoint(character, controller, surfaceNormal);
            return character.position + surfacePoint + surfaceNormal * resolvedClearance - support;
        }

        public static bool PlaceOnGround(Transform character, CharacterController controller,
            float castHeight = 8f, float castDistance = 20f, float minimumGroundNormal = .62f,
            Transform ignoredHierarchy = null)
        {
            if (character == null) return false;
            Physics.SyncTransforms();
            Vector3 origin = character.position + Vector3.up * castHeight;
            RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, castDistance, ~0,
                QueryTriggerInteraction.Ignore);
            RaycastHit best = default;
            float nearest = float.PositiveInfinity;
            for (int i = 0; i < hits.Length; i++)
            {
                RaycastHit hit = hits[i];
                if (hit.collider == null || hit.transform.IsChildOf(character) ||
                    ignoredHierarchy != null && (hit.transform == ignoredHierarchy ||
                        hit.transform.IsChildOf(ignoredHierarchy)) ||
                    hit.normal.y < minimumGroundNormal || hit.distance >= nearest) continue;
                best = hit;
                nearest = hit.distance;
            }

            if (best.collider == null) return false;
            Vector3 position = controller != null
                ? GetRootPositionOnSurface(character, controller, best.point, best.normal)
                : new Vector3(character.position.x, best.point.y + .025f, character.position.z);
            bool wasEnabled = controller != null && controller.enabled;
            if (wasEnabled) controller.enabled = false;
            character.position = position;
            if (wasEnabled) controller.enabled = true;
            Physics.SyncTransforms();
            return true;
        }
    }
}
