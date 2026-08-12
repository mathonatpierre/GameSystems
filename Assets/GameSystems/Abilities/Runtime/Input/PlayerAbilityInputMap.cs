using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GameSystems.Abilities
{
    [CreateAssetMenu(menuName = "Game Systems/Input/Ability Input Map", fileName = "INPUTMAP_")]
    public sealed class PlayerAbilityInputMap : ScriptableObject
    {
        public InputActionReference horizontal;
        public PlayerAbilityBinding[] bindings;
    }
}
