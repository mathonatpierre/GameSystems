using System;
using UnityEngine;

namespace GameSystems.Core
{
    [Serializable]
    public struct FloatRange
    {
        [SerializeField] float minimum;
        [SerializeField] float maximum;

        public FloatRange(float minimum, float maximum)
        {
            this.minimum = minimum;
            this.maximum = Mathf.Max(minimum, maximum);
        }

        public float Minimum => minimum;
        public float Maximum => Mathf.Max(minimum, maximum);

        public float Lerp(float t) => Mathf.Lerp(Minimum, Maximum, Mathf.Clamp01(t));
        public float Clamp(float value) => Mathf.Clamp(value, Minimum, Maximum);
        public bool Contains(float value) => value >= Minimum && value <= Maximum;
    }
}
