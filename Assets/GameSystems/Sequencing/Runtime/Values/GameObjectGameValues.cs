using System;
using UnityEngine;

namespace GameSystems.Sequencing.Values
{
    [Serializable]
    public abstract class GameObjectValue : GameValue
    {
        public abstract GameObject Get(in GameActionContext context);
    }

    [Serializable]
    public sealed class ConstantGameObjectValue : GameObjectValue
    {
        [SerializeField] GameObject value;
        public ConstantGameObjectValue() { }
        public ConstantGameObjectValue(GameObject value) => this.value = value;
        public override string Summary => value != null ? value.name : "None";
        public override GameObject Get(in GameActionContext context) => value;
    }

    [Serializable]
    public sealed class OwnerGameObjectValue : GameObjectValue
    {
        public override string Summary => "Owner";
        public override GameObject Get(in GameActionContext context) => ToGameObject(context.Owner);
        internal static GameObject ToGameObject(UnityEngine.Object value) => value switch
        {
            GameObject gameObject => gameObject,
            Component component => component.gameObject,
            _ => null
        };
    }

    [Serializable]
    public sealed class SelfGameObjectValue : GameObjectValue
    {
        public override string Summary => "Self";
        public override GameObject Get(in GameActionContext context) =>
            OwnerGameObjectValue.ToGameObject(context.Self);
    }

    [Serializable]
    public sealed class TargetGameObjectValue : GameObjectValue
    {
        public override string Summary => "Target";
        public override GameObject Get(in GameActionContext context) =>
            OwnerGameObjectValue.ToGameObject(context.Target);
    }

    [Serializable]
    public sealed class GameObjectVariableValue : GameObjectValue
    {
        [SerializeField] string key;
        public GameObjectVariableValue() { }
        public GameObjectVariableValue(string key) => this.key = key;
        public override string Summary => $"[{key}]";
        public override GameObject Get(in GameActionContext context) =>
            context.TryGet(out GameActionBlackboard variables) &&
            variables.TryGet(key, out GameObject value) ? value : null;
    }

    [Serializable]
    public sealed class ParentGameObjectValue : GameObjectValue
    {
        [SerializeReference] GameObjectValue source = new SelfGameObjectValue();
        public override string Summary => $"Parent of {source?.Summary ?? "None"}";
        public override GameObject Get(in GameActionContext context) =>
            source?.Get(context)?.transform.parent?.gameObject;
    }

    [Serializable]
    public sealed class RootGameObjectValue : GameObjectValue
    {
        [SerializeReference] GameObjectValue source = new SelfGameObjectValue();
        public override string Summary => $"Root of {source?.Summary ?? "None"}";
        public override GameObject Get(in GameActionContext context) =>
            source?.Get(context)?.transform.root?.gameObject;
    }

    [Serializable]
    public sealed class ChildByNameGameObjectValue : GameObjectValue
    {
        [SerializeReference] GameObjectValue source = new SelfGameObjectValue();
        [SerializeField] string path;
        public override string Summary => $"{source?.Summary ?? "None"}/{path}";
        public override GameObject Get(in GameActionContext context) =>
            source?.Get(context)?.transform.Find(path)?.gameObject;
    }
}
