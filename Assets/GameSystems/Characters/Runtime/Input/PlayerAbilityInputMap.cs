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
        [Tooltip("Optional 2D movement action. Horizontal-only characters may leave this empty.")]
        public InputActionReference movement;
        public PlayerAbilityBinding[] bindings;
    }
}
