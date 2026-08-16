using UnityEngine.Playables;

namespace GameSystems.Playables
{
    public abstract class PlayableAnimationRuntime
    {
        protected PlayableAnimationRuntime(Playable playable) => Playable = playable;

        public Playable Playable { get; }
        public virtual float NormalizedTime
        {
            get
            {
                if (!Playable.IsValid()) return 0f;
                double duration = Playable.GetDuration();
                return duration > .0001 && !double.IsInfinity(duration)
                    ? (float)(Playable.GetTime() / duration)
                    : 0f;
            }
        }
        public abstract void Evaluate(PlayableAnimationContext context);
        public abstract void Restart();

        public virtual void SeekNormalized(float normalizedTime)
        {
            if (!Playable.IsValid()) return;
            double duration = Playable.GetDuration();
            if (duration > .0001 && !double.IsInfinity(duration))
                Playable.SetTime(duration * normalizedTime);
        }
    }
}
