using GameSystems.Abilities;
using GameSystems.Hooks;
using UnityEngine;

namespace GameSystems.Characters
{
    public sealed class CharacterAISensors
    {
        readonly CharacterAIController owner;

        public CharacterAISensors(CharacterAIController value) => owner = value;

        public Transform ResolveTarget(in CharacterRuntimeContext context,
            CharacterAIDefinition definition, Transform targetOverride)
        {
            if (targetOverride != null) return targetOverride;
            if (definition.TargetHook != null)
                return HookRegistry.Get(definition.TargetHook)?.transform;
            Transform best = null;
            float bestDistance = definition.DetectionRadius;
            foreach (CharacterAbilityController candidate in CharacterAbilityRegistry.Controllers)
            {
                if (candidate == null || candidate.gameObject == context.Owner) continue;
                if (owner.Blackboard.Traversal.IsIgnoredTarget(candidate.transform)) continue;
                float distance = Vector3.Distance(context.Transform.position, candidate.transform.position);
                if (distance > bestDistance) continue;
                best = candidate.transform;
                bestDistance = distance;
            }
            return best;
        }

        public bool HasLineOfSight(in CharacterRuntimeContext context, Transform target,
            LayerMask mask)
        {
            if (target == null) return false;
            Vector3 delta = target.position - context.Transform.position;
            if (delta.sqrMagnitude < .0001f) return true;
            if (!Physics.Raycast(context.Transform.position, delta.normalized, out RaycastHit hit,
                    delta.magnitude, mask, QueryTriggerInteraction.Ignore)) return true;
            return hit.transform == target || hit.transform.IsChildOf(target);
        }

        public bool TryFindNearbyWall(float maximumDistance, float probeHeight,
            LayerMask mask, float preferredDirection, out float direction)
        {
            direction = 0f;
            float preferred = Mathf.Sign(preferredDirection);
            if (Mathf.Approximately(preferred, 0f)) preferred = 1f;
            if (CastWall(preferred, maximumDistance, probeHeight, mask))
            { direction = preferred; return true; }
            if (CastWall(-preferred, maximumDistance, probeHeight, mask))
            { direction = -preferred; return true; }
            return false;
        }

        bool CastWall(float direction, float distance, float probeHeight, LayerMask mask)
        {
            Collider body = owner.GetComponent<Collider>();
            Vector3 center = body != null ? body.bounds.center :
                owner.transform.position + Vector3.up * probeHeight;
            float halfHeight = body != null ? body.bounds.extents.y : probeHeight;
            float[] offsets = { -halfHeight * .5f, 0f, halfHeight * .5f };
            for (int i = 0; i < offsets.Length; i++)
            {
                Vector3 origin = center + Vector3.up * offsets[i];
                if (!Physics.Raycast(origin, Vector3.right * direction, out RaycastHit hit,
                        distance, mask, QueryTriggerInteraction.Ignore)) continue;
                if (hit.transform == owner.transform || hit.transform.IsChildOf(owner.transform)) continue;
                if (hit.collider.GetComponentInParent<CharacterAbilityController>() != null) continue;
                if (Mathf.Abs(Vector3.Dot(hit.normal, Vector3.up)) < .3f) return true;
            }
            return false;
        }
    }
}
