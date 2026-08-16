using System;
using GameSystems.Sequencing;
using GameSystems.Sequencing.Values;
using UnityEngine;

namespace GameSystems.Feedbacks.Actions
{
    [Serializable]
    public sealed class PlayParticleSystemAction : GameAction
    {
        [SerializeReference] GameObjectValue target = new SelfGameObjectValue();
        public PlayParticleSystemAction() { }
        public PlayParticleSystemAction(GameObjectValue target) => this.target = target;
        public override string Summary => $"Play ParticleSystem on {target?.Summary}";
        public override GameActionRuntime CreateRuntime() => new Runtime();
        sealed class Runtime : InstantActionRuntime
        {
            protected override void Execute()
            { ParticleSystem particles = FeedbackActionUtility.Resolve<ParticleSystem>(((PlayParticleSystemAction)Definition).target, Context, true); if (particles == null) { Fail("Missing ParticleSystem."); return; } particles.Play(); }
        }
    }
}
