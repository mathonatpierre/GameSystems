using System.Collections.Generic;

namespace GameSystems.Abilities
{
    public static class CharacterAbilityRegistry
    {
        static readonly HashSet<CharacterAbilityController> controllers = new();

        public static IReadOnlyCollection<CharacterAbilityController> Controllers => controllers;

        internal static void Register(CharacterAbilityController controller)
        {
            if (controller != null) controllers.Add(controller);
        }

        internal static void Unregister(CharacterAbilityController controller)
        {
            if (controller != null) controllers.Remove(controller);
        }
    }
}
