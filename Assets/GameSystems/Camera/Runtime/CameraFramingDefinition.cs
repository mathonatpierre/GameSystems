using UnityEngine;

namespace GameSystems.Camera
{
    [CreateAssetMenu(menuName = "Game Systems/Camera/Framing Definition", fileName = "CAMERA_")]
    public sealed class CameraFramingDefinition : ScriptableObject
    {
        [SerializeField] Vector3 positionOffset = new(0f, 1.7f, -6.25f);
        [SerializeField] Vector3 lookOffset = new(0f, .75f, 0f);
        [SerializeField, Tooltip("Interpret offsets in the target's rotated space.")]
        bool targetRelative;
        [SerializeField, Range(10f, 100f)] float fieldOfView = 42f;
        [SerializeField, Min(.01f)] float positionSmoothTime = .14f;
        [SerializeField, Min(.01f)] float rotationSharpness = 8f;
        [SerializeField, Min(0f)] float transitionDuration = .65f;
        [Header("Platform dynamics")]
        [SerializeField] bool usePlatformDynamics;
        [SerializeField, Min(0f)] float lookAhead = .75f;
        [SerializeField, Min(0f)] float movingZoomOut = .5f;
        [SerializeField, Min(0f)] float airborneZoomOut = .8f;
        [SerializeField, Min(0f)] float idleCloseupDelay = 1.8f;
        [SerializeField, Min(0f)] float idleCloseupDistance = 1.25f;
        [SerializeField, Range(10f, 100f)] float actionFieldOfView = 42.5f;
        [Header("Glide dynamics")]
        [SerializeField, Min(0f)] float glideLookAheadSeconds = .42f;
        [SerializeField, Range(0f, 1f)] float glideDepthInfluence = .2f;
        [SerializeField, Min(.01f)] float glidePositionSmoothTime = .28f;
        [SerializeField, Min(.01f)] float glideLookSmoothTime = .2f;
        [SerializeField, Min(0f)] float glideRotationSharpness = 3.5f;
        [SerializeField, Range(.05f, .45f)] float minimumTargetViewportX = .24f;
        [SerializeField, Range(.55f, .95f)] float maximumTargetViewportX = .68f;
        [Header("Responsive framing")]
        [SerializeField, Min(1f)] float referenceAspect = 1.777778f;
        [SerializeField, Min(0f)] float narrowAspectZoomOut = 2.8f;
        [SerializeField, Min(0f)] float maximumNarrowZoomOut = 5.5f;

        public Vector3 PositionOffset => positionOffset;
        public Vector3 LookOffset => lookOffset;
        public bool TargetRelative => targetRelative;
        public float FieldOfView => fieldOfView;
        public float PositionSmoothTime => positionSmoothTime;
        public float RotationSharpness => rotationSharpness;
        public float TransitionDuration => transitionDuration;
        public bool UsePlatformDynamics => usePlatformDynamics;
        public float LookAhead => lookAhead;
        public float MovingZoomOut => movingZoomOut;
        public float AirborneZoomOut => airborneZoomOut;
        public float IdleCloseupDelay => idleCloseupDelay;
        public float IdleCloseupDistance => idleCloseupDistance;
        public float ActionFieldOfView => actionFieldOfView;
        public float GlideLookAheadSeconds => glideLookAheadSeconds;
        public float GlideDepthInfluence => glideDepthInfluence;
        public float GlidePositionSmoothTime => glidePositionSmoothTime;
        public float GlideLookSmoothTime => glideLookSmoothTime;
        public float GlideRotationSharpness => glideRotationSharpness;
        public float MinimumTargetViewportX => minimumTargetViewportX;
        public float MaximumTargetViewportX => maximumTargetViewportX;
        public float ReferenceAspect => referenceAspect;
        public float NarrowAspectZoomOut => narrowAspectZoomOut;
        public float MaximumNarrowZoomOut => maximumNarrowZoomOut;

        public void Configure(Vector3 cameraOffset, Vector3 targetLookOffset,
            bool useTargetSpace, float fov, float smoothTime, float rotationSpeed,
            float transitionTime)
        {
            positionOffset = cameraOffset;
            lookOffset = targetLookOffset;
            targetRelative = useTargetSpace;
            fieldOfView = Mathf.Clamp(fov, 10f, 100f);
            positionSmoothTime = Mathf.Max(.01f, smoothTime);
            rotationSharpness = Mathf.Max(.01f, rotationSpeed);
            transitionDuration = Mathf.Max(0f, transitionTime);
        }

        public void ConfigurePlatformDynamics(bool enabled)
        {
            usePlatformDynamics = enabled;
            actionFieldOfView = Mathf.Max(actionFieldOfView, fieldOfView);
        }
    }
}
