using System;
using GameSystems.Sequencing;
using UnityEngine;

namespace GameSystems.Rideables.Conditions
{
    [Serializable]
    public sealed class RailFlightFinishedCondition : GameCondition
    {
        [SerializeField] RailFlightPathConstraint flight;

        public override string Summary => $"Rail flight {flight?.name ?? "missing"} finished";

        protected override bool OnEvaluate(in GameActionContext context)
        {
            RailFlightPathConstraint target = flight;
            if (target == null)
                target = GameActionContextUtility.OwnerGameObject(context)
                    ?.GetComponent<RailFlightPathConstraint>();
            return target == null || target.Finished;
        }
    }
}
