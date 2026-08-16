using System;
using UnityEngine;

namespace GameSystems.Sequencing.Values
{
    public enum ComponentSearchScope { OnObject, InParents, InChildren }

    [Serializable]
    public abstract class ComponentTarget { }

    [Serializable]
    public sealed class ComponentTarget<T> : ComponentTarget where T : Component
    {
        [SerializeField] T explicitTarget;
        [SerializeField] bool useExplicitTarget;
        [SerializeReference] GameObjectValue source = new SelfGameObjectValue();
        [SerializeField] ComponentSearchScope searchScope;
        [SerializeField] bool includeInactive = true;

        public ComponentTarget() { }
        public ComponentTarget(T explicitTarget)
        { this.explicitTarget = explicitTarget; useExplicitTarget = true; }
        public ComponentTarget(GameObjectValue source, ComponentSearchScope searchScope)
        {
            this.source = source;
            this.searchScope = searchScope;
        }

        public string Summary => explicitTarget != null || useExplicitTarget
            ? explicitTarget != null ? explicitTarget.name : "Missing reference"
            : $"{source?.Summary ?? "None"} {searchScope}";

        public T Get(in GameActionContext context)
        {
            if (explicitTarget != null || useExplicitTarget) return explicitTarget;
            GameObject gameObject = source?.Get(context);
            if (gameObject == null) return null;
            return searchScope switch
            {
                ComponentSearchScope.InParents =>
                    gameObject.GetComponentInParent<T>(includeInactive),
                ComponentSearchScope.InChildren =>
                    gameObject.GetComponentInChildren<T>(includeInactive),
                _ => gameObject.GetComponent<T>()
            };
        }
    }
}
