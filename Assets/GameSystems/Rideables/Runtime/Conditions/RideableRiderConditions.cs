using System;
using GameSystems.Sequencing;
using UnityEngine;

namespace GameSystems.Rideables.Conditions
{
    [Serializable]
    public sealed class RiderMountedCondition : GameCondition
    {
        [SerializeField, Tooltip("Optional rider. Uses the sequence owner when empty.")]
        RideableRider rider;
        [SerializeField, Tooltip("Optional rideable required by this condition.")]
        RideableController rideable;
        [SerializeField] bool expected = true;

        public override string Summary =>
            $"{(rider != null ? rider.name : "owner")} mounted" +
            (rideable != null ? $" on {rideable.name}" : string.Empty) +
            $" = {expected.ToString().ToLowerInvariant()}";

        protected override bool OnEvaluate(in GameActionContext context)
        {
            RideableRider target = rider;
            if (target == null)
                target = GameActionContextUtility.OwnerGameObject(context)
                    ?.GetComponent<RideableRider>();
            bool mounted = target != null && target.IsMounted &&
                           (rideable == null || target.Rideable == rideable);
            return mounted == expected;
        }
    }
}
