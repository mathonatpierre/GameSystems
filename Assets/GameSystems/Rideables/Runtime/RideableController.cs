using GameSystems.Playables;
using GameSystems.Characters;
using GameSystems.Sequencing;
using UnityEngine;

namespace GameSystems.Rideables
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(29000)]
    public sealed class RideableController : MonoBehaviour, IGameTriggerContactProxy
    {
        [SerializeField] UnityPlayableAnimationPlayer vehiclePlayer;
        [SerializeField] RideableAnimationDefinition animations;
        [SerializeField] UnityPlayableAnimationPlayer riderPlayer;
        [SerializeField] string initialAnimation;
        [SerializeField] RideableSeatRig[] seats;
        [SerializeField] string groundedSpeedParameter = "GroundSpeed";
        [SerializeField, Min(.01f)] float groundedSpeedSmoothTime = .12f;
        string currentAnimation;
        RideableAnimationPair currentPair;
        ICharacterMotorControl groundMotor;
        MonoBehaviour groundMotionSource;
        float smoothedGroundSpeed;
        float groundSpeedVelocity;
        PlayableAnimationAsset synchronizedVehicleAnimation;

        public string CurrentAnimation => currentAnimation;
        public float NormalizedTime => vehiclePlayer != null ? vehiclePlayer.NormalizedTime : 0f;
        public RideableSeatRig[] Seats => seats;

        public GameObject ResolveTriggerTarget(Collider contact)
        {
            if (contact == null || !contact.transform.IsChildOf(transform)) return null;
            if (seats == null) return null;
            for (int i = 0; i < seats.Length; i++)
            {
                RideableRider occupant = seats[i] != null ? seats[i].Occupant : null;
                if (occupant != null) return occupant.gameObject;
            }
            return null;
        }

        public void Configure(UnityPlayableAnimationPlayer vehicle,
            RideableAnimationDefinition definition, string initialId = null)
        {
            vehiclePlayer = vehicle;
            animations = definition;
            initialAnimation = initialId;
        }

        public void SetRider(UnityPlayableAnimationPlayer rider)
        {
            riderPlayer = rider;
            if (!string.IsNullOrEmpty(currentAnimation)) Play(currentAnimation, true);
        }

        public void ConfigureSeats(params RideableSeatRig[] value) => seats = value;

        public void ConfigureGroundedLocomotion(MonoBehaviour motionSource,
            string animationId = "GroundLocomotion", string speedParameter = "GroundSpeed",
            float smoothTime = .12f)
        {
            groundMotionSource = motionSource;
            groundMotor = motionSource as ICharacterMotorControl;
            groundedSpeedParameter = speedParameter;
            groundedSpeedSmoothTime = Mathf.Max(.01f, smoothTime);
        }

        public void SetAnimationFloat(string id, float value)
        {
            vehiclePlayer?.Context.SetFloat(id, value);
            riderPlayer?.Context.SetFloat(id, value);
        }

        public bool TryMount(RideableRider rider, RideableSeatRig seat = null,
            bool fitPoseImmediately = true)
        {
            if (rider == null) return false;
            seat ??= FindAvailableSeat();
            return seat != null && rider.TryMount(this, seat, fitPoseImmediately);
        }

        public bool Dismount(RideableRider rider) => rider != null && rider.Dismount();

        RideableSeatRig FindAvailableSeat()
        {
            if (seats == null) return null;
            for (int i = 0; i < seats.Length; i++)
                if (seats[i] != null && seats[i].IsAvailable) return seats[i];
            return null;
        }

        public bool Play(string id, bool forceRestart = false)
        {
            RideableAnimationPair pair = animations != null ? animations.Find(id) : null;
            if (pair == null || vehiclePlayer == null || pair.Vehicle == null) return false;
            currentAnimation = pair.Id;
            currentPair = pair;
            synchronizedVehicleAnimation = pair.Vehicle;
            vehiclePlayer.Play(pair.Vehicle, pair.BlendDuration, forceRestart);
            if (riderPlayer != null && pair.Rider != null)
            {
                riderPlayer.Play(pair.Rider, pair.BlendDuration, forceRestart);
                if (pair.SynchronizePhase)
                    riderPlayer.SeekNormalized(vehiclePlayer.NormalizedTime);
            }
            return true;
        }

        void Start()
        {
            ResolveGroundMotor();
            if (!string.IsNullOrEmpty(initialAnimation)) Play(initialAnimation, true);
        }

        void Update()
        {
            ResolveGroundMotor();
            SyncRiderToVehicleAnimation();
            UpdateGroundSpeedParameter();
        }

        void UpdateGroundSpeedParameter()
        {
            if (groundMotor == null) return;
            Vector3 up = groundMotionSource != null ? groundMotionSource.transform.up : Vector3.up;
            float targetSpeed = Vector3.ProjectOnPlane(groundMotor.Velocity, up).magnitude;
            smoothedGroundSpeed = Mathf.SmoothDamp(smoothedGroundSpeed, targetSpeed,
                ref groundSpeedVelocity, groundedSpeedSmoothTime);
            SetAnimationFloat(groundedSpeedParameter, smoothedGroundSpeed);
        }

        void SyncRiderToVehicleAnimation()
        {
            if (vehiclePlayer == null || riderPlayer == null || animations == null) return;
            PlayableAnimationAsset vehicleAnimation = vehiclePlayer.Current;
            if (vehicleAnimation == null || vehicleAnimation == synchronizedVehicleAnimation) return;
            RideableAnimationPair pair = animations.FindByVehicle(vehicleAnimation);
            if (pair == null || pair.Rider == null) return;
            synchronizedVehicleAnimation = vehicleAnimation;
            currentAnimation = pair.Id;
            currentPair = pair;
            riderPlayer.Play(pair.Rider, pair.BlendDuration);
            if (pair.SynchronizePhase)
                riderPlayer.SeekNormalized(vehiclePlayer.NormalizedTime);
        }

        void ResolveGroundMotor()
        {
            if (groundMotor != null) return;
            if (groundMotionSource != null)
                groundMotor = groundMotionSource as ICharacterMotorControl;
            if (groundMotor == null)
            {
                groundMotor = GetComponentInParent(typeof(ICharacterMotorControl)) as
                    ICharacterMotorControl;
                groundMotionSource = groundMotor as MonoBehaviour;
            }
        }

        void LateUpdate()
        {
            if (riderPlayer == null || vehiclePlayer == null || currentPair == null ||
                !currentPair.SynchronizePhase) return;
            // Both graphs are evaluated immediately after this component. Updating only their
            // shared phase here avoids evaluating the rider twice and keeps blended loops locked.
            riderPlayer.SeekNormalized(Mathf.Repeat(vehiclePlayer.NormalizedTime, 1f), false);
        }
    }
}
