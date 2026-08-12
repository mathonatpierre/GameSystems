using System;
using UnityEngine;

namespace GameSystems.LevelGeneration
{
    [Serializable]
    public sealed class PlatformLifecycleRules
    {
        [SerializeField] bool fragile;
        [SerializeField, Min(0f)] float warningDelay = .9f;
        [SerializeField, Min(0f)] float respawnDelay = 3.2f;
        [SerializeField] bool crusher;
        [SerializeField, Min(0f)] float crusherWarningDelay = 1f;
        [SerializeField, Min(0f)] float crusherTravel = 2.5f;

        public bool Fragile => fragile;
        public float WarningDelay => warningDelay;
        public float RespawnDelay => respawnDelay;
        public bool Crusher => crusher;
        public float CrusherWarningDelay => crusherWarningDelay;
        public float CrusherTravel => crusherTravel;

        public void ConfigureFragile(float warning, float respawn)
        {
            fragile = true;
            crusher = false;
            warningDelay = Mathf.Max(0f, warning);
            respawnDelay = Mathf.Max(0f, respawn);
        }

        public void ConfigureCrusher(float warning, float travel)
        {
            crusher = true;
            fragile = false;
            crusherWarningDelay = Mathf.Max(0f, warning);
            crusherTravel = Mathf.Max(0f, travel);
        }
    }
}
