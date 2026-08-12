using System;
using UnityEngine;

namespace GameSystems.Core
{
    [Serializable]
    public struct IntRange
    {
        [SerializeField] int minimum;
        [SerializeField] int maximum;

        public IntRange(int minimum, int maximum)
        {
            this.minimum = minimum;
            this.maximum = Mathf.Max(minimum, maximum);
        }

        public int Minimum => minimum;
        public int Maximum => Mathf.Max(minimum, maximum);

        public int Clamp(int value) => Mathf.Clamp(value, Minimum, Maximum);
        public bool Contains(int value) => value >= Minimum && value <= Maximum;
    }
}
