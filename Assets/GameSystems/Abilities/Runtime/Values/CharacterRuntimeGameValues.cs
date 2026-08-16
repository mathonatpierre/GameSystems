using System;
using GameSystems.Characters;
using GameSystems.Abilities.Actions;
using GameSystems.Sequencing;
using GameSystems.Sequencing.Values;
using GameSystems.Stats;
using GameSystems.Hooks;
using UnityEngine;

namespace GameSystems.Abilities.Values
{
    public enum MotorVelocityAxis { Horizontal, Vertical, Magnitude }
    public enum AttributeValueMode { Current, Maximum, Normalized }

    [Serializable]
    public sealed class AbilityRequestFloatValue : FloatValue
    {
        public override string Summary => "Ability request value";
        public override float Get(in GameActionContext context) =>
            context.TryGet(out AbilityRuntime runtime) ? runtime.LastRequest.Value :
            context.TryGet(out AbilityEvaluationContext evaluation) ? evaluation.Request.Value : 0f;
    }

    [Serializable]
    public sealed class MotorVelocityFloatValue : FloatValue
    {
        [SerializeField] MotorVelocityAxis axis;
        public override string Summary => $"Motor {axis} velocity";
        public override float Get(in GameActionContext context)
        {
            if (!context.TryGet(out CharacterRuntimeContext character) || character.Motor == null)
                return 0f;
            Vector3 velocity = character.Motor.Result.Velocity;
            return axis switch
            {
                MotorVelocityAxis.Horizontal => Mathf.Abs(velocity.x),
                MotorVelocityAxis.Vertical => velocity.y,
                _ => velocity.magnitude
            };
        }
    }

    [Serializable]
    public sealed class MotorAirTimeFloatValue : FloatValue
    {
        public override string Summary => "Motor air time";
        public override float Get(in GameActionContext context) =>
            context.TryGet(out CharacterRuntimeContext character) && character.Motor != null
                ? character.Motor.Result.AirTime : 0f;
    }

    [Serializable]
    public sealed class AbilityFallDistanceFloatValue : FloatValue
    {
        public override string Summary => "Ability fall distance";
        public override float Get(in GameActionContext context)
        {
            if (!context.TryGet(out CharacterRuntimeContext character) ||
                !context.TryGet(out AbilityRuntime runtime)) return 0f;
            return Mathf.Max(0f, runtime.StartPosition.y - character.Transform.position.y);
        }
    }

    [Serializable]
    public sealed class CharacterStatFloatValue : FloatValue
    {
        [SerializeField] StatDefinition stat;
        public override string Summary => stat != null ? stat.DisplayName : "Missing stat";
        public override float Get(in GameActionContext context)
        {
            CharacterStats stats = ResolveStats(context);
            return stats != null && stat != null ? stats.GetStatValue(stat) : 0f;
        }

        internal static CharacterStats ResolveStats(in GameActionContext context)
        {
            if (context.TryGet(out CharacterRuntimeContext character))
                return character.Resolve<CharacterStats>();
            return GameActionContextUtility.OwnerGameObject(context)?.GetComponent<CharacterStats>();
        }
    }

    [Serializable]
    public sealed class CharacterAttributeFloatValue : FloatValue
    {
        [SerializeField] AttributeDefinition attribute;
        [SerializeField] AttributeValueMode mode;
        public CharacterAttributeFloatValue() { }
        public CharacterAttributeFloatValue(AttributeDefinition attribute,
            AttributeValueMode mode = AttributeValueMode.Current)
        { this.attribute = attribute; this.mode = mode; }
        public override string Summary => $"{attribute?.DisplayName ?? "Missing attribute"} {mode}";
        public override float Get(in GameActionContext context)
        {
            RuntimeAttribute value = CharacterStatFloatValue.ResolveStats(context)
                ?.GetAttribute(attribute);
            if (value == null) return 0f;
            return mode switch
            {
                AttributeValueMode.Maximum => value.Maximum,
                AttributeValueMode.Normalized => value.Maximum > 0f ? value.Current / value.Maximum : 0f,
                _ => value.Current
            };
        }
    }

    [Serializable]
    public sealed class ContactCharacterGameObjectValue : GameObjectValue
    {
        [SerializeField] CharacterContactTarget target = CharacterContactTarget.Other;
        public ContactCharacterGameObjectValue() { }
        public ContactCharacterGameObjectValue(CharacterContactTarget target) => this.target = target;
        public override string Summary => $"Contact {target}";
        public override GameObject Get(in GameActionContext context)
        {
            if (!context.TryGet(out CharacterContactContext contact)) return null;
            return (target == CharacterContactTarget.Self ? contact.Self : contact.Other)?.gameObject;
        }
    }

    [Serializable]
    public sealed class PlanarKnockbackVector3Value : Vector3Value
    {
        [SerializeReference] GameObjectValue target =
            new ContactCharacterGameObjectValue(CharacterContactTarget.Other);
        [SerializeReference] GameObjectValue source =
            new ContactCharacterGameObjectValue(CharacterContactTarget.Self);
        [SerializeReference] FloatValue horizontalSpeed = new ConstantFloatValue(4.8f);
        [SerializeReference] FloatValue verticalSpeed = new ConstantFloatValue(3.15f);
        public PlanarKnockbackVector3Value() { }
        public PlanarKnockbackVector3Value(float horizontalSpeed, float verticalSpeed)
        { this.horizontalSpeed = new ConstantFloatValue(horizontalSpeed);
          this.verticalSpeed = new ConstantFloatValue(verticalSpeed); }
        public override string Summary => "Planar knockback velocity";
        public override Vector3 Get(in GameActionContext context)
        {
            GameObject targetObject = target?.Get(context);
            GameObject sourceObject = source?.Get(context);
            float direction = targetObject == null || sourceObject == null ||
                              targetObject.transform.position.x >= sourceObject.transform.position.x
                ? 1f : -1f;
            return new Vector3(direction * (horizontalSpeed?.Get(context) ?? 0f),
                verticalSpeed?.Get(context) ?? 0f, 0f);
        }
    }

    [Serializable]
    public sealed class PositionAwayFromGameObjectValue : Vector3Value
    {
        [SerializeReference] GameObjectValue origin = new SelfGameObjectValue();
        [SerializeReference] GameObjectValue obstacle = new TargetGameObjectValue();
        [SerializeReference] FloatValue distance = new ConstantFloatValue(1f);
        [SerializeField] Vector3 axis = Vector3.right;
        [SerializeField] Vector3 offset;
        public override string Summary => $"{origin?.Summary} away from {obstacle?.Summary}";
        public override Vector3 Get(in GameActionContext context)
        {
            GameObject source = origin?.Get(context);
            GameObject other = obstacle?.Get(context);
            if (source == null) return offset;
            Vector3 directionAxis = axis.sqrMagnitude > .001f ? axis.normalized : Vector3.right;
            float side = other == null || Vector3.Dot(source.transform.position -
                other.transform.position, directionAxis) >= 0f ? 1f : -1f;
            return source.transform.position + directionAxis * side *
                   (distance?.Get(context) ?? 0f) + offset;
        }
    }

    [Serializable]
    public sealed class ClampPositionToPatrolAreaValue : Vector3Value
    {
        [SerializeReference] Vector3Value value = new TransformPositionValue();
        [SerializeReference] GameObjectValue character = new SelfGameObjectValue();
        public override string Summary => $"Clamp {value?.Summary} to patrol area";
        public override Vector3 Get(in GameActionContext context)
        {
            Vector3 result = value?.Get(context) ?? Vector3.zero;
            ICharacterPatrolArea patrol = character?.Get(context)
                ?.GetComponentInParent(typeof(ICharacterPatrolArea), true) as ICharacterPatrolArea;
            if (patrol != null) result.x = Mathf.Clamp(result.x, patrol.MinimumX, patrol.MaximumX);
            return result;
        }
    }

    [Serializable]
    public sealed class ContactOtherHookBoolValue : BoolValue
    {
        [SerializeField] HookId hook;
        public ContactOtherHookBoolValue() { }
        public ContactOtherHookBoolValue(HookId hook) => this.hook = hook;
        public override string Summary => $"Contact other is {hook?.name ?? "missing hook"}";
        public override bool Get(in GameActionContext context) =>
            context.TryGet(out CharacterContactContext contact) && contact.Other != null &&
            HookRegistry.Get(hook) == contact.Other.gameObject;
    }

    [Serializable]
    public sealed class ContactOtherVerticalVelocityFloatValue : FloatValue
    {
        public override string Summary => "Contact other vertical velocity";
        public override float Get(in GameActionContext context) =>
            context.TryGet(out CharacterContactContext contact) && contact.Other?.Motor != null
                ? contact.Other.Motor.Result.Velocity.y : 0f;
    }

    [Serializable]
    public sealed class ContactIsTopBoolValue : BoolValue
    {
        [SerializeField, Range(0f, 1f)] float minimumNormalY = .2f;
        [SerializeField, Min(0f)] float topBandBelow = .16f;
        [SerializeField, Min(0f)] float topBandAbove = .2f;
        public ContactIsTopBoolValue() { }
        public ContactIsTopBoolValue(float minimumNormalY, float topBandBelow,
            float topBandAbove)
        { this.minimumNormalY = minimumNormalY; this.topBandBelow = topBandBelow;
          this.topBandAbove = topBandAbove; }
        public override string Summary => "Contact other is on top";
        public override bool Get(in GameActionContext context)
        {
            if (!context.TryGet(out CharacterContactContext contact) || contact.Other == null)
                return false;
            Collider selfCollider = contact.Self != null
                ? contact.Self.GetComponent<Collider>() : null;
            if (selfCollider == null) return false;
            Bounds bounds = selfCollider.bounds;
            CharacterController otherController =
                contact.Other.GetComponent<CharacterController>();
            float feetY = contact.Other.transform.position.y + (otherController != null
                ? otherController.center.y - otherController.height * .5f : 0f);
            float verticalVelocity = contact.Other.Motor?.Result.Velocity.y ?? 0f;
            float previousFeetY = feetY - verticalVelocity *
                                  Mathf.Max(Time.deltaTime, 1f / 120f);
            float radius = otherController != null ? otherController.radius : .18f;
            Vector3 position = contact.Other.transform.position;
            bool overlap = position.x >= bounds.min.x - radius &&
                           position.x <= bounds.max.x + radius &&
                           position.z >= bounds.min.z - radius &&
                           position.z <= bounds.max.z + radius;
            bool feetAboveCenter = Mathf.Max(feetY, previousFeetY) >= bounds.center.y;
            bool reachedTopBand = Mathf.Max(feetY, previousFeetY) >=
                                  bounds.max.y - topBandBelow;
            bool upperSurfaceContact = contact.Point.y >=
                                       bounds.center.y + bounds.extents.y * .25f &&
                                       contact.Normal.y >= -minimumNormalY;
            return overlap && feetAboveCenter && (reachedTopBand || upperSurfaceContact) &&
                   feetY <= bounds.max.y + topBandAbove;
        }
    }
}
