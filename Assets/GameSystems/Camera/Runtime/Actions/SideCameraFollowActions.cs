using System;
using GameSystems.Core;
using GameSystems.Sequencing;
using UnityEngine;

namespace GameSystems.Camera.Actions
{
    [Serializable]
    public sealed class PulseCameraVertigoAction : GameAction
    {
        [SerializeField] UnityEngine.Camera target;
        [SerializeField, Range(0f, 25f)] float fovBoost = 10f;
        [SerializeField, Min(.05f)] float duration = .45f;
        [SerializeField, Range(0f, 2f)] float dollyDistance = .65f;

        public override string Summary =>
            $"Pulse camera vertigo +{fovBoost:0.#} FOV for {duration:0.##}s";

        public override GameActionRuntime CreateRuntime() => new Runtime();

        sealed class Runtime : InstantActionRuntime
        {
            protected override void Execute()
            {
                PulseCameraVertigoAction data = (PulseCameraVertigoAction)Definition;
                UnityEngine.Camera camera = data.target != null ? data.target : UnityEngine.Camera.main;
                ICameraVertigoPulse vertigo = camera != null
                    ? camera.GetComponent(typeof(ICameraVertigoPulse)) as ICameraVertigoPulse
                    : null;
                if (vertigo == null)
                {
                    Fail("Missing camera vertigo receiver.");
                    return;
                }
                vertigo.PulseVertigo(data.fovBoost, data.duration, data.dollyDistance);
            }
        }
    }
}
