using UnityEngine;

namespace GameSystems.Abilities
{
    [DisallowMultipleComponent]
    public sealed class CharacterPatrolArea : MonoBehaviour, ICharacterPatrolArea, ICharacterPatrolAreaReceiver
    {
        [SerializeField] float minimumX;
        [SerializeField] float maximumX;
        [SerializeField] float direction = 1f;
        [SerializeField, Min(.1f)] float fallbackHalfWidth = 2.5f;
        [SerializeField] Transform referenceFrame;

        public float MinimumX => referenceFrame != null
            ? referenceFrame.TransformPoint(new Vector3(minimumX, 0f, 0f)).x
            : minimumX;
        public float MaximumX => referenceFrame != null
            ? referenceFrame.TransformPoint(new Vector3(maximumX, 0f, 0f)).x
            : maximumX;
        public float Direction { get => direction; set => direction = Mathf.Sign(value == 0f ? 1f : value); }
        public Transform ReferenceFrame => referenceFrame;

        void Awake()
        {
            if (MaximumX - MinimumX > .01f) return;
            float center = transform.position.x;
            minimumX = center - fallbackHalfWidth;
            maximumX = center + fallbackHalfWidth;
            referenceFrame = null;
        }

        public void ConfigurePatrolArea(float min, float max, float initialDirection,
            Transform frame = null)
        {
            referenceFrame = frame;
            if (referenceFrame != null)
            {
                minimumX = referenceFrame.InverseTransformPoint(new Vector3(min, 0f, 0f)).x;
                maximumX = referenceFrame.InverseTransformPoint(new Vector3(max, 0f, 0f)).x;
            }
            else
            {
                minimumX = min;
                maximumX = max;
            }
            Direction = initialDirection;
            Vector3 world = transform.position;
            world.x = Mathf.Clamp(world.x, MinimumX, MaximumX);
            transform.position = world;
        }
    }
}
