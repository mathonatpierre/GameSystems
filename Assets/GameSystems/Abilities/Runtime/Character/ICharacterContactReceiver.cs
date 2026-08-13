using UnityEngine;
using GameSystems.Abilities;

namespace GameSystems.Abilities
{
    public interface ICharacterContactReceiver
    {
        void ReceiveCharacterContact(CharacterAbilityController character, Vector3 point, Vector3 normal);
    }
}
