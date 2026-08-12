using UnityEngine;

namespace GameSystems.Abilities
{
    public interface ICharacterPatrolAreaReceiver
    {
        void ConfigurePatrolArea(float minimumX, float maximumX, float direction,
            Transform referenceFrame = null);
    }
}
