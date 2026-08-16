using System;
using GameSystems.Sequencing;
using GameSystems.Sequencing.Values;
using UnityEngine;

namespace GameSystems.Rideables.Actions
{
    [Serializable]
    public sealed class TryMountRideableAction : GameAction
    {
        [SerializeField] ComponentTarget<RideableController> rideable =
            new(new SelfGameObjectValue(), ComponentSearchScope.InParents);
        [SerializeField] ComponentTarget<RideableRider> rider =
            new(new TargetGameObjectValue(), ComponentSearchScope.InParents);
        [SerializeField] RideableSeatRig seat;
        [SerializeField] bool fitPoseImmediately;
        public override string Summary => "Try mount rideable";
        public override GameActionRuntime CreateRuntime() => new Runtime();
        sealed class Runtime : InstantActionRuntime
        {
            protected override void Execute()
            {
                TryMountRideableAction data = (TryMountRideableAction)Definition;
                RideableController mount = data.rideable.Get(Context);
                RideableRider rider = data.rider.Get(Context);
                if (mount == null || rider == null ||
                    !mount.TryMount(rider, data.seat, data.fitPoseImmediately))
                    Fail("Rideable refused the rider.");
            }
        }
    }

    [Serializable]
    public sealed class PlayRideableAnimationAction : GameAction
    {
        [SerializeField] ComponentTarget<RideableController> rideable =
            new(new SelfGameObjectValue(), ComponentSearchScope.InParents);
        [SerializeField] string animationId;
        [SerializeField] bool forceRestart = true;
        public override string Summary => $"Play rideable animation {animationId}";
        public override GameActionRuntime CreateRuntime() => new Runtime();
        sealed class Runtime : InstantActionRuntime
        {
            protected override void Execute()
            {
                PlayRideableAnimationAction data = (PlayRideableAnimationAction)Definition;
                RideableController target = data.rideable.Get(Context);
                if (target == null || !target.Play(data.animationId, data.forceRestart))
                    Fail($"Could not play rideable animation '{data.animationId}'.");
            }
        }
    }

    [Serializable]
    public sealed class SetRiderMountedPoseAction : GameAction
    {
        [SerializeField] ComponentTarget<RideableRider> rider =
            new(new TargetGameObjectValue(), ComponentSearchScope.InParents);
        [SerializeField] bool enabled = true;
        public override string Summary => $"Set rider mounted pose {enabled}";
        public override GameActionRuntime CreateRuntime() => new Runtime();
        sealed class Runtime : InstantActionRuntime
        {
            protected override void Execute()
            {
                SetRiderMountedPoseAction data = (SetRiderMountedPoseAction)Definition;
                RideableRider target = data.rider.Get(Context);
                if (target == null) { Fail("Missing rider."); return; }
                target.SetMountedPoseEnabled(data.enabled);
            }
        }
    }

    [Serializable]
    public sealed class SetRiderTransitionPoseAction : GameAction
    {
        [SerializeField] ComponentTarget<RideableRider> rider =
            new(new TargetGameObjectValue(), ComponentSearchScope.InParents);
        public override string Summary => "Set rider transition pose";
        public override GameActionRuntime CreateRuntime() => new Runtime();
        sealed class Runtime : InstantActionRuntime
        {
            protected override void Execute()
            {
                SetRiderTransitionPoseAction data = (SetRiderTransitionPoseAction)Definition;
                RideableRider target = data.rider.Get(Context);
                if (target == null || !target.IsMounted)
                { Fail("Rider is not mounted."); return; }
                target.SetTransitionPoseEnabled();
            }
        }
    }

    [Serializable]
    public sealed class SetRiderTransitionProgressFromAnimationAction : GameAction
    {
        [SerializeField] ComponentTarget<RideableRider> rider =
            new(new TargetGameObjectValue(), ComponentSearchScope.InParents);
        [SerializeField] bool mounting = true;
        [SerializeField, Range(0f, 1f)] float movementStart = .18f;
        [SerializeField, Range(0f, 1f)] float movementEnd = .92f;
        [SerializeField, Min(.05f)] float timeout = 2.125f;
        public override string Summary => $"Set rider transition progress from animation ({(mounting ? "mount" : "dismount")})";
        public override GameActionRuntime CreateRuntime() => new Runtime();
        sealed class Runtime : GameActionRuntime
        {
            RideableRider target;
            float remaining;
            SetRiderTransitionProgressFromAnimationAction Data =>
                (SetRiderTransitionProgressFromAnimationAction)Definition;
            protected override void OnEnter()
            {
                base.OnEnter();
                target = Data.rider.Get(Context);
                remaining = Data.timeout;
                if (target == null || !target.IsMounted) Fail("Rider is not mounted.");
            }
            protected override bool Tick(float deltaTime)
            {
                if (Failed) return true;
                remaining -= deltaTime;
                float progress = target.AnimationNormalizedTime;
                target.SetTransitionProgress(Remap01(progress,
                    Data.movementStart, Data.movementEnd), Data.mounting);
                return progress >= .985f || remaining <= 0f;
            }
            static float Remap01(float value, float start, float end) =>
                end <= start ? value >= end ? 1f : 0f :
                Mathf.Clamp01((value - start) / (end - start));
        }
    }

    [Serializable]
    public sealed class DismountRiderAction : GameAction
    {
        [SerializeField] ComponentTarget<RideableRider> rider =
            new(new TargetGameObjectValue(), ComponentSearchScope.InParents);
        public override string Summary => "Dismount rider";
        public override GameActionRuntime CreateRuntime() => new Runtime();
        sealed class Runtime : InstantActionRuntime
        {
            protected override void Execute()
            {
                DismountRiderAction data = (DismountRiderAction)Definition;
                RideableRider target = data.rider.Get(Context);
                if (target == null || !target.Dismount()) Fail("Rider could not dismount.");
            }
        }
    }

}
