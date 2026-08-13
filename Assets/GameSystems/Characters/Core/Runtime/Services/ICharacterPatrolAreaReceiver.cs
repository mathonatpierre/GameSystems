using UnityEngine;

namespace GameSystems.Characters
{
    public interface ICharacterPatrolAreaReceiver
    {
        void ConfigurePatrolArea(float minimumX, float maximumX, float direction,
            Transform referenceFrame = null);
    }
}
