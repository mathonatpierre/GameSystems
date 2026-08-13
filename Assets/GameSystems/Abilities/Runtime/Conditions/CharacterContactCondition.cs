using System;
using GameSystems.Sequencing;
using GameSystems.Hooks;
using UnityEngine;

using GameSystems.Characters;

namespace GameSystems.Abilities.Conditions
{
    public enum CharacterContactOrientation { Any, Top, Side }

    [Serializable]
    public sealed class CharacterContactCondition : GameCondition
    {
        [SerializeField] CharacterContactOrientation orientation;
        [SerializeField] HookId otherHook;
        [SerializeField] bool requireOtherDescending;
        [SerializeField, Range(0f, 1f)] float topNormal = .2f;
        [SerializeField, Min(0f)] float topBandBelow = .16f;
        [SerializeField, Min(0f)] float topBandAbove = .2f;

        public override string Summary => $"Contact {orientation}" +
            (otherHook != null ? $" with {otherHook.name}" : string.Empty) +
            (requireOtherDescending ? ", descending" : string.Empty);

        protected override bool OnEvaluate(in GameActionContext context)
        {
            if (!context.TryGet(out CharacterContactContext contact) || contact.Other == null) return false;
            if (otherHook != null && HookRegistry.Get(otherHook) != contact.Other.gameObject) return false;
            if (requireOtherDescending && contact.Other.Motor != null &&
                contact.Other.Motor.Result.Velocity.y > .12f) return false;
            bool geometricallyAbove = IsGeometricallyAbove(contact);
            // CharacterController contact normals can briefly point sideways on a
            // curved body. Feet placement is the stable signal for a stomp.
            bool isTop = geometricallyAbove;
            return orientation switch
            {
                CharacterContactOrientation.Top => isTop,
                CharacterContactOrientation.Side => !isTop,
                _ => true
            };
        }

        bool IsGeometricallyAbove(in CharacterContactContext contact)
        {
            Collider selfCollider = contact.Self != null ? contact.Self.GetComponent<Collider>() : null;
            if (selfCollider == null || contact.Other == null) return false;
            Bounds bounds = selfCollider.bounds;
            CharacterController otherController = contact.Other.GetComponent<CharacterController>();
            float feetY = contact.Other.transform.position.y + (otherController != null
                ? otherController.center.y - otherController.height * .5f
                : 0f);
            float verticalVelocity = contact.Other.Motor?.Result.Velocity.y ?? 0f;
            float previousFeetY = feetY - verticalVelocity * Mathf.Max(Time.deltaTime, 1f / 120f);
            float radius = otherController != null ? otherController.radius : .18f;
            Vector3 position = contact.Other.transform.position;
            bool horizontalOverlap = position.x >= bounds.min.x - radius &&
                                     position.x <= bounds.max.x + radius &&
                                     position.z >= bounds.min.z - radius &&
                                     position.z <= bounds.max.z + radius;
            bool feetAboveCenter = Mathf.Max(feetY, previousFeetY) >= bounds.center.y;
            bool reachedTopBand = Mathf.Max(feetY, previousFeetY) >= bounds.max.y - topBandBelow;
            bool upperSurfaceContact = contact.Point.y >= bounds.center.y + bounds.extents.y * .25f &&
                                       contact.Normal.y >= -topNormal;
            return horizontalOverlap && feetAboveCenter && (reachedTopBand || upperSurfaceContact) &&
                   feetY <= bounds.max.y + topBandAbove;
        }
    }
}
