using System.Collections.Generic;
using GameSystems.Characters;
using GameSystems.Playables;
using UnityEngine;

namespace GameSystems.Rideables
{
    [DefaultExecutionOrder(29900)]
    [DisallowMultipleComponent]
    public sealed class RideableRider : MonoBehaviour
    {
        [SerializeField] Animator animator;
        [SerializeField] UnityPlayableAnimationPlayer animationPlayer;
        [SerializeField] CharacterController characterController;
        [SerializeField] Behaviour[] disableWhileMounted;

        readonly List<(Behaviour Behaviour, bool Enabled)> behaviourStates = new();
        RideableController rideable;
        RideableSeatRig seat;
        HumanoidRiderSeatFitter seatFitter;
        HumanoidRiderLegIK legIK;
        HumanoidRiderArmIK armIK;
        HumanoidLookAtPostProcessor lookAt;
        bool controllerWasEnabled;
        bool transitioning;
        bool hasGameplayPlane;
        Vector3 gameplayPlaneNormal;
        float gameplayPlaneDistance;
        Quaternion gameplayRotation;
        Vector3 transitionGroundPosition;
        Quaternion transitionGroundRotation;

        public RideableController Rideable => rideable;
        public RideableSeatRig Seat => seat;
        public bool IsMounted => rideable != null && seat != null;
        public float AnimationNormalizedTime => animationPlayer != null
            ? animationPlayer.NormalizedTime : 0f;

        public void Configure(Animator riderAnimator, UnityPlayableAnimationPlayer player,
            CharacterController controller, Behaviour[] behaviours)
        {
            animator = riderAnimator;
            animationPlayer = player;
            characterController = controller;
            disableWhileMounted = behaviours;
        }

        public bool TryMount(RideableController targetRideable, RideableSeatRig targetSeat,
            bool fitPoseImmediately = true)
        {
            if (IsMounted || targetRideable == null || targetSeat == null ||
                !targetSeat.TryOccupy(this)) return false;

            ResolveReferences();
            CaptureGameplayFrame();
            rideable = targetRideable;
            seat = targetSeat;
            transitioning = !fitPoseImmediately;
            if (fitPoseImmediately)
            {
                CacheAndDisableControls();
                SnapToSeat();
            }
            else
            {
                transitionGroundPosition = transform.position;
                transitionGroundRotation = transform.rotation;
                CacheAndDisableControls();
            }
            ConfigurePose();
            if (fitPoseImmediately) SetPoseEnabled(true);
            else SetPoseEnabled(false);
            rideable.SetRider(animationPlayer);
            return true;
        }

        public bool Dismount()
        {
            if (!IsMounted) return false;
            RideableSeatRig previousSeat = seat;
            Vector3 finalPosition = transitionGroundPosition;
            Quaternion finalRotation = transitionGroundRotation;
            ReleaseSeat();
            SetPoseEnabled(false);
            if (finalPosition == default && previousSeat.DismountPoint != null)
                finalPosition = previousSeat.DismountPoint.position;
            transform.SetPositionAndRotation(finalPosition, finalRotation);
            RestoreGameplayFrame(finalRotation);
            RestoreControls();
            bool grounded = CharacterGroundPlacement.PlaceOnGround(transform, characterController,
                1.5f, 4f, ignoredHierarchy:
                previousSeat.GetComponentInParent<RideableController>()?.transform);
            if (GetComponent(typeof(ICharacterMotorControl)) is ICharacterMotorControl motor)
                motor.SetVelocity(Vector3.zero);
            return grounded;
        }

        void ResolveReferences()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>(true);
            if (animationPlayer == null) animationPlayer = GetComponent<UnityPlayableAnimationPlayer>();
            if (characterController == null) characterController = GetComponent<CharacterController>();
        }

        void ConfigurePose()
        {
            seatFitter = GetOrAdd<HumanoidRiderSeatFitter>();
            legIK = GetOrAdd<HumanoidRiderLegIK>();
            armIK = GetOrAdd<HumanoidRiderArmIK>();
            lookAt = GetOrAdd<HumanoidLookAtPostProcessor>();
            seatFitter.Configure(animator, transform, seat.transform, 1f);
            legIK.Configure(animator, seat.LeftFoot, seat.RightFoot, seat.transform, 1f);
            armIK.Configure(animator, seat.LeftHand, seat.RightHand, seat.transform, 1f);
            lookAt.Configure(animator, animationPlayer, "FlightHorizontal", "FlightVertical");
            SetPoseEnabled(true);
            animationPlayer?.Configure(animator, false);
        }

        T GetOrAdd<T>() where T : Component => GetComponent<T>() ?? gameObject.AddComponent<T>();

        public void SetMountedPoseEnabled(bool value)
        {
            if (!IsMounted) return;
            transitioning = !value;
            if (value) SnapToSeat();
            SetPoseEnabled(value);
        }

        public void SetTransitionPoseEnabled()
        {
            if (!IsMounted) return;
            transitioning = true;
            SetPoseEnabled(false);
            Transform point = seat.DismountPoint;
            if (point != null)
            {
                Vector3 mountedPosition = transform.position;
                Quaternion mountedRotation = transform.rotation;
                transform.SetPositionAndRotation(point.position, point.rotation);
                CharacterGroundPlacement.PlaceOnGround(transform, characterController,
                    1.5f, 4f, ignoredHierarchy: rideable.transform);
                transitionGroundPosition = transform.position;
                transitionGroundRotation = GetGroundFacingRotation(point);
                transform.SetPositionAndRotation(mountedPosition, mountedRotation);
            }
            else
            {
                transitionGroundPosition = transform.position;
                transitionGroundRotation = GetGroundFacingRotation(null);
            }
        }

        public void SetTransitionProgress(float normalizedTime, bool mounting)
        {
            if (!IsMounted || seat == null) return;
            transitioning = true;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(normalizedTime));
            if (!mounting) t = 1f - t;
            transform.SetPositionAndRotation(
                Vector3.Lerp(transitionGroundPosition, seat.transform.position, t),
                Quaternion.Slerp(transitionGroundRotation, seat.transform.rotation, t));
        }

        void SetPoseEnabled(bool value)
        {
            if (seatFitter != null) seatFitter.enabled = value;
            if (legIK != null) legIK.enabled = value;
            if (armIK != null) armIK.enabled = value;
            if (lookAt != null) lookAt.enabled = value;
        }

        void CacheAndDisableControls()
        {
            behaviourStates.Clear();
            if (disableWhileMounted != null)
            {
                for (int i = 0; i < disableWhileMounted.Length; i++)
                {
                    Behaviour behaviour = disableWhileMounted[i];
                    if (behaviour == null || behaviour == this || behaviour == animationPlayer) continue;
                    behaviourStates.Add((behaviour, behaviour.enabled));
                    behaviour.enabled = false;
                }
            }
            if (characterController != null)
            {
                controllerWasEnabled = characterController.enabled;
                characterController.enabled = false;
            }
        }

        void RestoreControls()
        {
            for (int i = 0; i < behaviourStates.Count; i++)
                if (behaviourStates[i].Behaviour != null)
                    behaviourStates[i].Behaviour.enabled = behaviourStates[i].Enabled;
            behaviourStates.Clear();
            if (characterController != null) characterController.enabled = controllerWasEnabled;
        }

        void ReleaseSeat()
        {
            RideableController previousRideable = rideable;
            RideableSeatRig previousSeat = seat;
            rideable = null;
            seat = null;
            transitioning = false;
            previousRideable?.SetRider(null);
            previousSeat?.Release(this);
        }

        void LateUpdate()
        {
            if (IsMounted && !transitioning) SnapToSeat();
        }

        void SnapToSeat()
        {
            if (seat == null) return;
            transform.SetPositionAndRotation(seat.transform.position, seat.transform.rotation);
        }

        void CaptureGameplayFrame()
        {
            gameplayRotation = transform.rotation;
            ICharacterMovementPlane movementPlane =
                GetComponent(typeof(ICharacterMovementPlane)) as ICharacterMovementPlane;
            hasGameplayPlane = movementPlane != null;
            if (!hasGameplayPlane) return;
            gameplayPlaneNormal = movementPlane.MovementPlaneNormal;
            if (gameplayPlaneNormal.sqrMagnitude < .001f)
            {
                hasGameplayPlane = false;
                return;
            }
            gameplayPlaneNormal.Normalize();
            gameplayPlaneDistance = Vector3.Dot(transform.position, gameplayPlaneNormal);
        }

        void RestoreGameplayFrame(Quaternion rotation)
        {
            Vector3 position = transform.position;
            if (hasGameplayPlane)
                position += gameplayPlaneNormal *
                    (gameplayPlaneDistance - Vector3.Dot(position, gameplayPlaneNormal));
            transform.SetPositionAndRotation(position, rotation);
        }

        Quaternion GetGroundFacingRotation(Transform point)
        {
            if (seat == null) return gameplayRotation;
            Vector3 origin = point != null ? point.position : transitionGroundPosition;
            Vector3 up = seat.transform.up.sqrMagnitude > .5f ? seat.transform.up : Vector3.up;
            Vector3 towardSeat = Vector3.ProjectOnPlane(seat.transform.position - origin, up);
            return towardSeat.sqrMagnitude > .001f
                ? Quaternion.LookRotation(towardSeat.normalized, up.normalized)
                : gameplayRotation;
        }

        void OnDisable()
        {
            if (!IsMounted) return;
            ReleaseSeat();
            SetPoseEnabled(false);
        }
    }
}
