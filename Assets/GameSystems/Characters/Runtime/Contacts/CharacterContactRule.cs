using System;
using GameSystems.Sequencing;
using UnityEngine;

namespace GameSystems.Characters
{
    [Serializable]
    public sealed class CharacterContactRule
    {
        [SerializeField] string label = "Contact Rule";
        [SerializeField, Min(0f)] float cooldown = .1f;
        [SerializeField] GameActionSequence sequence = new();

        [NonSerialized] double nextAllowedAt;
        public string Label => label;
        public GameActionSequence Sequence => sequence ??= new GameActionSequence();

        public bool TryStart(in GameActionContext context)
        {
            if (Time.timeAsDouble < nextAllowedAt || !Sequence.CanRun(context)) return false;
            nextAllowedAt = Time.timeAsDouble + cooldown;
            return true;
        }
    }
}
