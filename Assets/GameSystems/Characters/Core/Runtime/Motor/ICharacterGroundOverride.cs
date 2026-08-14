namespace GameSystems.Characters
{
    public interface ICharacterGroundOverride
    {
        bool TryGetGround(out GroundContact ground);
    }
}
