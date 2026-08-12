using System;
using UnityEngine;

namespace GameSystems.Sequencing
{
    [Serializable]
    public sealed class SetTimeScaleAction : GameAction
    {
        [SerializeField, Min(0f), Tooltip("New global Unity time scale.")] float timeScale = 1f;
        public override string Summary => $"Set time scale = {timeScale:0.###}";
        public override GameActionRuntime CreateRuntime() => new Runtime();
        sealed class Runtime : InstantActionRuntime
        {
            protected override void Execute() => Time.timeScale = ((SetTimeScaleAction)Definition).timeScale;
        }
    }
}
