using System.Collections.Generic;
using GameSystems.Core;
using GameSystems.Feedbacks;
using GameSystems.Hooks;
using GameSystems.Abilities;
using UnityEngine;

namespace GameSystems.Camera
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UnityEngine.Camera))]
    public sealed class CameraFramingController : MonoBehaviour, ICameraVertigoPulse,
        ICameraShakeReceiver
    {
        sealed class FramingRequestEntry
        {
            public object Owner;
            public CameraFramingDefinition Definition;
            public Transform Target;
            public int Priority;
            public int Order;
        }

        [SerializeField] CameraFramingDefinition defaultFraming;
        [SerializeField, Tooltip("Scene target resolved through GameSystems.Hooks.")]
        HookId defaultTargetHook;
        readonly List<FramingRequestEntry> requests = new();
        UnityEngine.Camera controlledCamera;
        CameraFramingDefinition activeDefinition;
        Transform activeTarget;
        Vector3 positionVelocity;
        Vector3 transitionPosition;
        Quaternion transitionRotation;
        float transitionFov;
        float transitionElapsed;
        float transitionDuration;
        float vertigoElapsed;
        float vertigoDuration;
        float vertigoFovBoost;
        float vertigoDollyDistance;
        float shakeRemaining;
        float shakeIntensity;
        float shakeDuration;
        float currentZoomOut;
        float idleTimer;
        float idleCloseupBlend;
        float dynamicFov;
        int requestOrder;
        Vector3 basePosition;
        bool hasBasePosition;

        public CameraFramingDefinition ActiveDefinition => activeDefinition;
        public Transform ActiveTarget => activeTarget;

        public void Configure(CameraFramingDefinition definition, HookId targetHook)
        {
            defaultFraming = definition;
            defaultTargetHook = targetHook;
            SelectBestRequest();
        }

        public void Request(object owner, CameraFramingDefinition definition,
            Transform target, int priority = 0)
        {
            if (owner == null || definition == null || target == null) return;
            FramingRequestEntry request = requests.Find(item => ReferenceEquals(item.Owner, owner));
            if (request == null)
            {
                request = new FramingRequestEntry { Owner = owner };
                requests.Add(request);
            }
            request.Definition = definition;
            request.Target = target;
            request.Priority = priority;
            request.Order = ++requestOrder;
            SelectBestRequest();
        }

        public void Release(object owner)
        {
            requests.RemoveAll(item => ReferenceEquals(item.Owner, owner));
            if (isActiveAndEnabled) SelectBestRequest();
        }

        public void PulseVertigo(float fovBoost = 10f, float duration = .45f,
            float dollyDistance = .65f)
        {
            vertigoElapsed = 0f;
            vertigoDuration = Mathf.Max(.05f, duration);
            vertigoFovBoost = Mathf.Max(0f, fovBoost);
            vertigoDollyDistance = Mathf.Max(0f, dollyDistance);
        }

        public void AddImpactShake(float intensity, float duration)
        {
            shakeIntensity = Mathf.Max(shakeIntensity, intensity);
            shakeRemaining = Mathf.Max(shakeRemaining, duration);
            shakeDuration = Mathf.Max(shakeDuration, duration);
        }

        void Awake()
        {
            controlledCamera = GetComponent<UnityEngine.Camera>();
            basePosition = transform.position;
            hasBasePosition = true;
            SelectBestRequest();
        }

        void LateUpdate()
        {
            if (activeTarget == null && requests.Count == 0)
                SetActive(defaultFraming, ResolveDefaultTarget());
            if (activeDefinition == null || activeTarget == null) return;

            Vector3 desiredPosition = activeDefinition.TargetRelative
                ? activeTarget.TransformPoint(activeDefinition.PositionOffset)
                : activeTarget.position + activeDefinition.PositionOffset;
            Vector3 desiredLook = activeDefinition.TargetRelative
                ? activeTarget.TransformPoint(activeDefinition.LookOffset)
                : activeTarget.position + activeDefinition.LookOffset;
            float activeSmoothTime = activeDefinition.PositionSmoothTime;
            float activeRotationSharpness = activeDefinition.RotationSharpness;
            float baseFov = activeDefinition.FieldOfView;
            ApplyPlatformDynamics(ref desiredPosition, ref desiredLook,
                ref activeSmoothTime, ref activeRotationSharpness, ref baseFov);
            float vertigo = EvaluateVertigo();
            Vector3 dollyDirection = desiredLook - desiredPosition;
            if (dollyDirection.sqrMagnitude > .001f)
                desiredPosition += dollyDirection.normalized * vertigoDollyDistance * vertigo;
            Vector3 up = activeDefinition.TargetRelative ? activeTarget.up : Vector3.up;
            Quaternion desiredRotation = Quaternion.LookRotation(desiredLook - desiredPosition, up);

            if (transitionElapsed < transitionDuration)
            {
                transitionElapsed += Time.deltaTime;
                float t = transitionDuration <= 0f ? 1f :
                    Mathf.SmoothStep(0f, 1f, transitionElapsed / transitionDuration);
                basePosition = Vector3.Lerp(transitionPosition, desiredPosition, t);
                transform.SetPositionAndRotation(ApplyShake(basePosition),
                    Quaternion.Slerp(transitionRotation, desiredRotation, t));
                if (controlledCamera != null)
                    controlledCamera.fieldOfView = Mathf.Lerp(transitionFov,
                        baseFov, t) + vertigoFovBoost * vertigo;
                return;
            }

            basePosition = Vector3.SmoothDamp(basePosition, desiredPosition,
                ref positionVelocity, activeSmoothTime);
            transform.position = ApplyShake(basePosition);
            Vector3 direction = desiredLook - basePosition;
            if (direction.sqrMagnitude > .001f)
                transform.rotation = Quaternion.Slerp(transform.rotation,
                    Quaternion.LookRotation(direction, up),
                    1f - Mathf.Exp(-activeRotationSharpness * Time.deltaTime));
            if (controlledCamera != null)
            {
                float desiredFov = baseFov + vertigoFovBoost * vertigo;
                controlledCamera.fieldOfView = Mathf.MoveTowards(controlledCamera.fieldOfView,
                    desiredFov, Time.deltaTime * (vertigo > 0f ? 70f : 18f));
                if (activeDefinition.UsePlatformDynamics) ApplyHorizontalSafetyFraming();
            }
        }

        void ApplyPlatformDynamics(ref Vector3 desiredPosition, ref Vector3 desiredLook,
            ref float smoothTime, ref float rotationSharpness, ref float fov)
        {
            if (!activeDefinition.UsePlatformDynamics) return;
            CharacterAbilityController controller =
                activeTarget.GetComponent<CharacterAbilityController>();
            Vector3 velocity = controller?.Motor != null ? controller.Motor.Result.Velocity : Vector3.zero;
            bool grounded = controller?.Motor != null && controller.Motor.Result.Ground.IsGrounded;
            CameraDynamicFrame frame = default;
            bool dynamic = CameraDynamicFramingRegistry.TryGet(activeTarget.gameObject,
                               out ICameraDynamicFramingProvider provider) &&
                           provider.TryGetCameraFrame(activeTarget.gameObject, activeDefinition,
                               out frame);
            if (dynamic)
            {
                desiredPosition = frame.FollowPoint + BuildDynamicOffset(controller, velocity, grounded);
                desiredLook = frame.LookPoint;
                smoothTime = frame.PositionSmoothTime;
                rotationSharpness = frame.RotationSharpness;
            }
            else
            {
                float direction = Mathf.Abs(velocity.x) > .001f ? Mathf.Sign(velocity.x) : 0f;
                Vector3 offset = BuildDynamicOffset(controller, velocity, grounded);
                desiredPosition = activeTarget.position + offset +
                                  Vector3.right * direction * activeDefinition.LookAhead;
                desiredLook = activeTarget.position + activeDefinition.LookOffset +
                              Vector3.right * direction * activeDefinition.LookAhead * .45f;
            }
            fov = dynamicFov;
        }

        Vector3 BuildDynamicOffset(CharacterAbilityController controller, Vector3 velocity,
            bool grounded)
        {
            float speedRatio = controller != null ? Mathf.Clamp01(velocity.magnitude / 8f) : 0f;
            bool idle = speedRatio < .025f && grounded;
            idleTimer = idle ? idleTimer + Time.deltaTime : 0f;
            float requestedIdleBlend = idleTimer >= activeDefinition.IdleCloseupDelay ? 1f : 0f;
            float idleBlendSpeed = requestedIdleBlend > idleCloseupBlend ? .7f : 3.8f;
            idleCloseupBlend = Mathf.MoveTowards(idleCloseupBlend, requestedIdleBlend,
                Time.deltaTime * idleBlendSpeed);
            float requestedZoom = speedRatio * activeDefinition.MovingZoomOut +
                                  (controller != null && !grounded ? activeDefinition.AirborneZoomOut : 0f);
            currentZoomOut = Mathf.MoveTowards(currentZoomOut, requestedZoom,
                Time.deltaTime * (requestedZoom > currentZoomOut ? 5.5f : 2.4f));
            float aspect = controlledCamera != null ? controlledCamera.aspect : activeDefinition.ReferenceAspect;
            float narrowZoom = Mathf.Min(activeDefinition.MaximumNarrowZoomOut,
                Mathf.Max(0f, activeDefinition.ReferenceAspect / Mathf.Max(.1f, aspect) - 1f) *
                activeDefinition.NarrowAspectZoomOut);
            dynamicFov = Mathf.Lerp(activeDefinition.ActionFieldOfView,
                activeDefinition.FieldOfView, idleCloseupBlend);
            return activeDefinition.PositionOffset + Vector3.back * (currentZoomOut + narrowZoom) +
                   Vector3.forward * (activeDefinition.IdleCloseupDistance * idleCloseupBlend);
        }

        float EvaluateVertigo()
        {
            if (vertigoElapsed >= vertigoDuration || vertigoDuration <= 0f) return 0f;
            vertigoElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(vertigoElapsed / vertigoDuration);
            const float attack = .18f;
            return t < attack
                ? Mathf.SmoothStep(0f, 1f, t / attack)
                : 1f - Mathf.SmoothStep(0f, 1f, (t - attack) / (1f - attack));
        }

        Vector3 ApplyShake(Vector3 position)
        {
            if (shakeRemaining <= 0f || shakeIntensity <= 0f) return position;
            shakeRemaining -= Time.deltaTime;
            float fade = Mathf.Clamp01(shakeRemaining / Mathf.Max(.001f, shakeDuration));
            Vector2 offset = Random.insideUnitCircle * shakeIntensity * fade;
            if (shakeRemaining <= 0f)
            {
                shakeIntensity = 0f;
                shakeDuration = 0f;
            }
            return position + transform.right * offset.x + transform.up * offset.y;
        }

        void ApplyHorizontalSafetyFraming()
        {
            if (controlledCamera == null || activeTarget == null) return;
            Vector3 viewport = controlledCamera.WorldToViewportPoint(
                activeTarget.position + activeDefinition.LookOffset);
            if (viewport.z <= .01f) return;
            float clampedX = Mathf.Clamp(viewport.x, activeDefinition.MinimumTargetViewportX,
                activeDefinition.MaximumTargetViewportX);
            float viewportCorrection = viewport.x - clampedX;
            if (Mathf.Abs(viewportCorrection) < .0001f) return;
            float visibleHeight = 2f * viewport.z *
                                  Mathf.Tan(controlledCamera.fieldOfView * .5f * Mathf.Deg2Rad);
            float visibleWidth = visibleHeight * controlledCamera.aspect;
            Vector3 correction = transform.right * (viewportCorrection * visibleWidth);
            basePosition += correction;
            transform.position += correction;
        }

        void SelectBestRequest()
        {
            FramingRequestEntry best = null;
            for (int i = 0; i < requests.Count; i++)
                if (best == null || requests[i].Priority > best.Priority ||
                    requests[i].Priority == best.Priority && requests[i].Order > best.Order)
                    best = requests[i];
            SetActive(best?.Definition ?? defaultFraming,
                best?.Target != null ? best.Target : ResolveDefaultTarget());
        }

        Transform ResolveDefaultTarget()
        {
            GameObject target = defaultTargetHook != null ? HookRegistry.Get(defaultTargetHook) : null;
            return target != null ? target.transform : null;
        }

        void SetActive(CameraFramingDefinition definition, Transform target)
        {
            if (definition == activeDefinition && target == activeTarget) return;
            if (!hasBasePosition)
            {
                basePosition = transform.position;
                hasBasePosition = true;
            }
            // Begin from the pose that was actually rendered. basePosition intentionally
            // excludes transient safety framing and shake offsets, so using it here caused
            // a visible one-frame snap whenever a higher-priority framing took over.
            transitionPosition = transform.position;
            basePosition = transitionPosition;
            transitionRotation = transform.rotation;
            transitionFov = controlledCamera != null ? controlledCamera.fieldOfView : 60f;
            activeDefinition = definition;
            activeTarget = target;
            transitionElapsed = 0f;
            transitionDuration = activeDefinition != null ? activeDefinition.TransitionDuration : 0f;
            positionVelocity = Vector3.zero;
            currentZoomOut = 0f;
            idleTimer = 0f;
            idleCloseupBlend = 0f;
            dynamicFov = activeDefinition != null ? activeDefinition.FieldOfView : 60f;
        }
    }
}
