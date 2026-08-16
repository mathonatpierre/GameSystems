using UnityEngine;

namespace GameSystems.Characters
{
    public interface IHorizontalInputProvider { float Horizontal { get; } }
    public interface IDirectionalInputProvider { Vector2 Directional { get; } }
}
