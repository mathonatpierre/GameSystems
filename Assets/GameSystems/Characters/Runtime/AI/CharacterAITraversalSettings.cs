using System;
using UnityEngine;
using GameSystems.Abilities;

namespace GameSystems.Characters
{
    [Serializable]
    public sealed class CharacterAITraversalSettings
    {
        [SerializeField, Min(.1f)] float edgeProbeDistance = .72f;
        [SerializeField, Min(.5f)] float maximumJumpReach = 3.6f;
        [SerializeField, Min(.5f)] float landingProbeDepth = 4f;
        [SerializeField, Min(.1f)] float characterLookAhead = 2.2f;
        [SerializeField] AbilityDefinition airborneTargetAbility;

        public float EdgeProbeDistance => edgeProbeDistance;
        public float MaximumJumpReach => maximumJumpReach;
        public float LandingProbeDepth => landingProbeDepth;
        public float CharacterLookAhead => characterLookAhead;
        public AbilityDefinition AirborneTargetAbility => airborneTargetAbility;

        public void Configure(float edgeDistance, float jumpReach, float probeDepth,
            float characterDistance, AbilityDefinition targetAbility = null)
        {
            edgeProbeDistance = Mathf.Max(.1f, edgeDistance);
            maximumJumpReach = Mathf.Max(.5f, jumpReach);
            landingProbeDepth = Mathf.Max(.5f, probeDepth);
            characterLookAhead = Mathf.Max(.1f, characterDistance);
            airborneTargetAbility = targetAbility;
        }
    }
}
