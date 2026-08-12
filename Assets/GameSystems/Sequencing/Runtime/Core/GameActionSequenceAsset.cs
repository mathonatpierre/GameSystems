using UnityEngine;

namespace GameSystems.Sequencing
{
    [CreateAssetMenu(menuName = "Game Systems/Core/Action Sequence", fileName = "ACTIONSEQ_New")]
    public sealed class GameActionSequenceAsset : ScriptableObject
    {
        [SerializeField] GameActionSequence sequence = new();

        public GameActionSequence Sequence => sequence ??= new GameActionSequence();
    }
}
