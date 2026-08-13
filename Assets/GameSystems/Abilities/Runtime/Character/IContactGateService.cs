namespace GameSystems.Abilities
{
    public interface IContactGateService
    {
        bool IsOpen(string channel);
        void Close(string channel, float duration);
    }
}
