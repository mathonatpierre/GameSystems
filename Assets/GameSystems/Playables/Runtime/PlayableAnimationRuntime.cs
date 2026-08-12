using UnityEngine.Playables;

namespace GameSystems.Playables
{
    public abstract class PlayableAnimationRuntime
    {
        protected PlayableAnimationRuntime(Playable playable) => Playable = playable;

        public Playable Playable { get; }
        public abstract void Evaluate(PlayableAnimationContext context);
        public abstract void Restart();
    }
}
