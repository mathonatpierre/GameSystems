using System;
using GameSystems.Sequencing;
using GameSystems.Sequencing.Values;
using UnityEngine;

namespace GameSystems.Camera.Actions
{
    [Serializable]
    public sealed class RequestCameraFramingAction : GameAction
    {
        [SerializeField] ComponentTarget<CameraFramingController> controller =
            new(new SelfGameObjectValue(), ComponentSearchScope.InChildren);
        [SerializeField] CameraFramingDefinition definition;
        [SerializeReference] GameObjectValue target = new TargetGameObjectValue();
        [SerializeField] int priority;

        public RequestCameraFramingAction() { }
        public RequestCameraFramingAction(CameraFramingController controller,
            CameraFramingDefinition definition, GameObjectValue target, int priority = 0)
        {
            this.controller = new ComponentTarget<CameraFramingController>(controller);
            this.definition = definition;
            this.target = target ?? new TargetGameObjectValue();
            this.priority = priority;
        }

        public override string Summary =>
            $"Request camera framing [{definition?.name ?? "missing"}] priority {priority}";
        public override GameActionRuntime CreateRuntime() => new Runtime();

        sealed class Runtime : InstantActionRuntime
        {
            protected override void Execute()
            {
                RequestCameraFramingAction data = (RequestCameraFramingAction)Definition;
                CameraFramingController framing = data.controller.Get(Context);
                GameObject target = data.target?.Get(Context);
                if (framing == null) { Fail("Missing camera framing controller."); return; }
                if (data.definition == null) { Fail("Missing camera framing definition."); return; }
                if (target == null) { Fail("Missing camera framing target."); return; }
                framing.Request(this, data.definition, target.transform, data.priority);
            }
        }
    }

    [Serializable]
    public sealed class ActivateCameraFramingAction : GameAction
    {
        [SerializeField] CameraFramingRequest request;
        public ActivateCameraFramingAction() { }
        public ActivateCameraFramingAction(CameraFramingRequest request) => this.request = request;
        public override string Summary => $"Activate camera framing {request?.name ?? "missing"}";
        public override GameActionRuntime CreateRuntime() => new Runtime();

        sealed class Runtime : InstantActionRuntime
        {
            protected override void Execute()
            {
                CameraFramingRequest request =
                    ((ActivateCameraFramingAction)Definition).request;
                if (request == null) { Fail("Missing camera framing request."); return; }
                request.Activate();
            }
        }
    }

    [Serializable]
    public sealed class ReleaseCameraFramingAction : GameAction
    {
        [SerializeField] CameraFramingRequest request;
        public override string Summary => $"Release camera framing {request?.name ?? "missing"}";
        public override GameActionRuntime CreateRuntime() => new Runtime();

        sealed class Runtime : InstantActionRuntime
        {
            protected override void Execute()
            {
                CameraFramingRequest request =
                    ((ReleaseCameraFramingAction)Definition).request;
                if (request == null) { Fail("Missing camera framing request."); return; }
                request.Deactivate();
            }
        }
    }
}
