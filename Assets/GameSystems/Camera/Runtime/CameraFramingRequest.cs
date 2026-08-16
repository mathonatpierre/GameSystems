using UnityEngine;

namespace GameSystems.Camera
{
    [DisallowMultipleComponent]
    public sealed class CameraFramingRequest : MonoBehaviour
    {
        [SerializeField] CameraFramingController controller;
        [SerializeField] CameraFramingDefinition definition;
        [SerializeField] Transform target;
        [SerializeField] int priority;

        public void Configure(CameraFramingController cameraController,
            CameraFramingDefinition framing, Transform framingTarget, int requestPriority = 0)
        {
            controller = cameraController;
            definition = framing;
            target = framingTarget;
            priority = requestPriority;
        }

        public void Activate()
        {
            if (controller != null)
                controller.Request(this, definition, target != null ? target : transform, priority);
        }

        public void Deactivate()
        {
            if (controller != null) controller.Release(this);
        }
        void OnDisable() => Deactivate();
    }
}
