namespace GameSystems.Stats
{
    public interface IStatProvider
    {
        StatDefinition FindStat(string id);
        RuntimeStat GetStat(StatDefinition stat);
        float GetStatValue(StatDefinition stat);
    }
}
