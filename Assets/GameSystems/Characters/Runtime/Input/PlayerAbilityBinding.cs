using System; using UnityEngine; using UnityEngine.InputSystem;
using GameSystems.Abilities;
namespace GameSystems.Characters
{
    [Serializable] public struct PlayerAbilityBinding
    { public InputActionReference action; public AbilityInputPhase phase; public AbilityDefinition ability; [Min(0f)] public float bufferDuration; }
}
