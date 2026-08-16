using UnityEngine;

namespace GameSystems.Characters
{
    public interface ICharacterTargetProvider
    {
        Transform CurrentTarget { get; }
    }
}
