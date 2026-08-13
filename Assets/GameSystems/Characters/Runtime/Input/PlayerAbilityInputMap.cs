using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Scripting.APIUpdating;

namespace GameSystems.Characters
{
    [MovedFrom(true, "GameSystems.Abilities", "GameSystems.Abilities", "PlayerAbilityInputMap")]
    [CreateAssetMenu(menuName = "Game Systems/Input/Ability Input Map", fileName = "INPUTMAP_")]
    public sealed class PlayerAbilityInputMap : ScriptableObject
    {
        public InputActionReference horizontal;
        public PlayerAbilityBinding[] bindings;
    }
}
