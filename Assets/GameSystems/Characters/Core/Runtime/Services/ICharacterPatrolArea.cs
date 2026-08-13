using UnityEngine;

namespace GameSystems.Characters
{
    public interface ICharacterPatrolArea
    {
        float MinimumX { get; }
        float MaximumX { get; }
        float Direction { get; set; }
        Transform ReferenceFrame { get; }
    }
}
