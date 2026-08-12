namespace GameSystems.Stats
{
    public interface IAttributeProvider
    {
        RuntimeAttribute GetAttribute(AttributeDefinition attribute);
    }
}
