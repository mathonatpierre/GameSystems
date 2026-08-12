#if UNITY_EDITOR
using UnityEngine;

namespace GameSystems.Abilities.Editor
{
    internal static class AbilityEditorStyles
    {
        public static Color CategoryColor(AbilityCategory category)
        {
            return category switch
            {
                AbilityCategory.Locomotion => new Color(.36f, .72f, .76f),
                AbilityCategory.Reaction => new Color(.95f, .58f, .36f),
                _ => new Color(.48f, .75f, .42f)
            };
        }
    }
}
#endif
