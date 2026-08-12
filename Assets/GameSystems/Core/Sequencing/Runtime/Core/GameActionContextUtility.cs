using UnityEngine;

namespace GameSystems.Actions
{
    public static class GameActionContextUtility
    {
        public static GameObject OwnerGameObject(in GameActionContext context) => context.Owner switch
        {
            GameObject gameObject => gameObject,
            Component component => component.gameObject,
            _ => null
        };
    }
}
