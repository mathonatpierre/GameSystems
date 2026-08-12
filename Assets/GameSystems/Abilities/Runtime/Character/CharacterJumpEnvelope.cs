using UnityEngine;

namespace GameSystems.Abilities
{
    public readonly struct CharacterJumpEnvelope
    {
        readonly CharacterJumpTrajectory[] trajectories;
        readonly float[] holdDurations;

        public bool IsValid => trajectories != null && holdDurations != null &&
                               trajectories.Length == holdDurations.Length && trajectories.Length > 0;
        public int Count => IsValid ? trajectories.Length : 0;

        public CharacterJumpEnvelope(CharacterJumpTrajectory[] samples, float[] durations)
        {
            trajectories = samples;
            holdDurations = durations;
        }

        public bool TryFindMinimumHold(float horizontalDistance, float verticalRise,
            float safetyMargin, out float holdDuration)
        {
            holdDuration = 0f;
            if (!IsValid) return false;
            float required = Mathf.Abs(horizontalDistance) /
                             Mathf.Clamp(safetyMargin, .05f, 1f);
            for (int i = 0; i < trajectories.Length; i++)
            {
                if (!trajectories[i].TryGetLandingDistance(verticalRise, out float reach) ||
                    reach < required) continue;
                holdDuration = holdDurations[i];
                return true;
            }
            return false;
        }

        public bool TryGetMaximumReach(float verticalRise, out float distance)
        {
            distance = 0f;
            return IsValid && trajectories[^1].TryGetLandingDistance(verticalRise, out distance);
        }

        public bool TryGetCandidate(int index, float verticalRise,
            out float holdDuration, out float landingDistance)
        {
            holdDuration = 0f;
            landingDistance = 0f;
            if (!IsValid || index < 0 || index >= trajectories.Length) return false;
            holdDuration = holdDurations[index];
            return trajectories[index].TryGetLandingDistance(verticalRise, out landingDistance);
        }

        public bool TryGetCandidate(int index, float verticalRise,
            out float holdDuration, out float landingDistance, out float flightTime)
        {
            holdDuration = 0f;
            landingDistance = 0f;
            flightTime = 0f;
            if (!IsValid || index < 0 || index >= trajectories.Length) return false;
            holdDuration = holdDurations[index];
            return trajectories[index].TryGetLanding(verticalRise,
                out landingDistance, out flightTime);
        }
    }
}
