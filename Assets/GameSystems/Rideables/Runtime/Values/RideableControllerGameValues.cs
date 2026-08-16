using System;
using GameSystems.Sequencing;
using GameSystems.Sequencing.Values;
using UnityEngine;

namespace GameSystems.Rideables.Values
{
    public enum RideableSeatPoint { Mount, Seat, Dismount }

    [Serializable]
    public sealed class RideableSeatPointGameObjectValue : GameObjectValue
    {
        [SerializeReference] GameObjectValue rideable = new SelfGameObjectValue();
        [SerializeField, Min(0)] int seatIndex;
        [SerializeField] RideableSeatPoint point = RideableSeatPoint.Mount;

        public RideableSeatPointGameObjectValue() { }
        public RideableSeatPointGameObjectValue(RideableSeatPoint value) => point = value;

        public override string Summary => $"{point} point of {rideable?.Summary ?? "rideable"}";
        public override GameObject Get(in GameActionContext context)
        {
            RideableController controller = rideable?.Get(context)
                ?.GetComponentInParent<RideableController>(true);
            RideableSeatRig[] seats = controller?.Seats;
            if (seats == null || seatIndex < 0 || seatIndex >= seats.Length || seats[seatIndex] == null)
                return null;
            Transform target = point switch
            {
                RideableSeatPoint.Mount => seats[seatIndex].MountPoint,
                RideableSeatPoint.Dismount => seats[seatIndex].DismountPoint,
                _ => seats[seatIndex].transform
            };
            return target != null ? target.gameObject : null;
        }
    }

    [Serializable]
    public sealed class RideableOccupantGameObjectValue : GameObjectValue
    {
        [SerializeReference] GameObjectValue rideable = new SelfGameObjectValue();
        public override string Summary => $"Occupant of {rideable?.Summary ?? "rideable"}";
        public override GameObject Get(in GameActionContext context)
        {
            RideableController controller = rideable?.Get(context)
                ?.GetComponentInParent<RideableController>(true);
            RideableSeatRig[] seats = controller?.Seats;
            if (seats == null) return null;
            for (int i = 0; i < seats.Length; i++)
                if (seats[i]?.Occupant != null) return seats[i].Occupant.gameObject;
            return null;
        }
    }
}
