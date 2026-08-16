using GameSystems.Sequencing.Values;
using GameSystems.Abilities.Actions;
using UnityEngine;

namespace GameSystems.Abilities.Values
{
    public static class ContactTargets
    {
        public static GameObjectValue GameObject(CharacterContactTarget target) =>
            new ContactCharacterGameObjectValue(target);

        public static ComponentTarget<T> Component<T>(CharacterContactTarget target)
            where T : UnityEngine.Component => new(
                new ContactCharacterGameObjectValue(target),
                ComponentSearchScope.InParents);
    }
}
