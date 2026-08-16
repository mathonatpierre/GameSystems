using System;
using UnityEngine;
using GameSystems.Playables;

namespace GameSystems.Rideables
{
    [Serializable]
    public sealed class RideableAnimationPair
    {
        [SerializeField] string id;
        [SerializeField] PlayableAnimationAsset vehicle;
        [SerializeField] PlayableAnimationAsset rider;
        [SerializeField, Min(0f)] float blendDuration = .08f;
        [SerializeField] bool synchronizePhase;

        public string Id => id;
        public PlayableAnimationAsset Vehicle => vehicle;
        public PlayableAnimationAsset Rider => rider;
        public float BlendDuration => blendDuration;
        public bool SynchronizePhase => synchronizePhase;

        public void Configure(string animationId, PlayableAnimationAsset vehicleAnimation,
            PlayableAnimationAsset riderAnimation, float transitionDuration = .08f,
            bool shouldSynchronizePhase = true)
        {
            id = animationId;
            vehicle = vehicleAnimation;
            rider = riderAnimation;
            blendDuration = Mathf.Max(0f, transitionDuration);
            synchronizePhase = shouldSynchronizePhase;
        }
    }

    [CreateAssetMenu(menuName = "Game Systems/Rideables/Animation Definition", fileName = "RIDEANIM_")]
    public sealed class RideableAnimationDefinition : ScriptableObject
    {
        [SerializeField] RideableAnimationPair[] pairs = Array.Empty<RideableAnimationPair>();

        public RideableAnimationPair[] Pairs => pairs;
        public void Configure(RideableAnimationPair[] values) => pairs = values ?? Array.Empty<RideableAnimationPair>();

        public RideableAnimationPair Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            for (var index = 0; index < pairs.Length; index++)
                if (pairs[index] != null && string.Equals(pairs[index].Id, id,
                        StringComparison.OrdinalIgnoreCase))
                    return pairs[index];
            return null;
        }

        public RideableAnimationPair FindByVehicle(PlayableAnimationAsset vehicle)
        {
            if (vehicle == null) return null;
            for (int index = 0; index < pairs.Length; index++)
                if (pairs[index] != null && pairs[index].Vehicle == vehicle)
                    return pairs[index];
            return null;
        }
    }
}
