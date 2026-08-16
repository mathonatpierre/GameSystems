namespace GameSystems.Playables
{
    public interface IPlayablePostProcessor
    {
        int Order { get; }
        void ApplyPlayablePostProcess();
    }
}
