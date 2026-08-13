using System;
using GameSystems.Sequencing;
using UnityEngine;

namespace GameSystems.Characters
{
    [Serializable]
    public sealed class AIGroundAheadCondition : GameCondition
    {
        [SerializeField, Min(.05f)] float forwardDistance = .65f;
        [SerializeField, Min(.05f)] float probeHeight = .35f;
        [SerializeField, Min(.1f)] float probeDistance = 1.2f;
        [SerializeField] bool expected = true;

        public AIGroundAheadCondition() { }
        public AIGroundAheadCondition(bool hasGround, float distance = .65f)
        { expected = hasGround; forwardDistance = distance; }

        public override string Summary => expected ? "AI has ground ahead" : "AI has a gap ahead";

        protected override bool OnEvaluate(in GameActionContext context)
        {
            if (!context.TryGet(out CharacterAIContext ai)) return false;
            float direction = Mathf.Abs(ai.Direction.x) > .01f ? Mathf.Sign(ai.Direction.x) : 1f;
            Vector3 origin = ai.Character.Transform.position +
                             Vector3.right * direction * forwardDistance + Vector3.up * probeHeight;
            bool found = Physics.Raycast(origin, Vector3.down, probeDistance,
                ai.Controller.Definition.LineOfSightMask, QueryTriggerInteraction.Ignore);
            return found == expected;
        }
    }
}
