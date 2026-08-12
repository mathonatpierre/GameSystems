using System;
using UnityEngine;

namespace GameSystems.LevelGeneration
{
    [Serializable]
    public sealed class PlatformSurfaceRules
    {
        [SerializeField] Material primaryMaterial;
        [SerializeField] Material secondaryMaterial;
        [SerializeField] bool castsShadows = true;

        public Material PrimaryMaterial => primaryMaterial;
        public Material SecondaryMaterial => secondaryMaterial != null
            ? secondaryMaterial : primaryMaterial;
        public bool CastsShadows => castsShadows;

        public void Configure(Material primary, Material secondary)
        {
            primaryMaterial = primary;
            secondaryMaterial = secondary;
        }
    }
}
