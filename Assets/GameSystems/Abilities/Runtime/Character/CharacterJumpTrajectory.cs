using UnityEngine;

namespace GameSystems.Abilities
{
    public readonly struct CharacterJumpTrajectory
    {
        readonly Vector2[] samples;

        public bool IsValid => samples != null && samples.Length > 1;

        public CharacterJumpTrajectory(Vector2[] trajectorySamples)
        {
            samples = trajectorySamples;
        }

        public bool TryGetLandingDistance(float verticalRise, out float distance)
            => TryGetLanding(verticalRise, out distance, out _);

        public bool TryGetHeightAtDistance(float distance, out float height)
        {
            height = 0f;
            if (!IsValid || distance < 0f || distance > samples[^1].x) return false;
            for (int i = 1; i < samples.Length; i++)
            {
                if (samples[i].x < distance) continue;
                float blend = Mathf.InverseLerp(samples[i - 1].x, samples[i].x, distance);
                height = Mathf.Lerp(samples[i - 1].y, samples[i].y, blend);
                return true;
            }
            return false;
        }

        public bool TryGetLanding(float verticalRise, out float distance, out float time)
        {
            distance = 0f;
            time = 0f;
            if (!IsValid) return false;

            for (int i = samples.Length - 1; i > 0; i--)
            {
                Vector2 previous = samples[i - 1];
                Vector2 current = samples[i];
                if (current.y > previous.y || verticalRise < current.y || verticalRise > previous.y)
                    continue;
                float blend = Mathf.InverseLerp(previous.y, current.y, verticalRise);
                distance = Mathf.Lerp(previous.x, current.x, blend);
                // Trajectories are sampled at 240 Hz by the capability resolver.
                time = (i - 1f + blend) / 240f;
                return true;
            }
            return false;
        }
    }
}
