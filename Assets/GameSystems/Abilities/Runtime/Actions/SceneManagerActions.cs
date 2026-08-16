using System;
using GameSystems.Sequencing;
using UnityEngine.SceneManagement;

namespace GameSystems.Abilities.Actions
{
    [Serializable]
    public sealed class ReloadCurrentSceneAction : GameAction
    {
        public override string Summary => "Reload current scene";
        public override GameActionRuntime CreateRuntime() => new Runtime();
        sealed class Runtime : InstantActionRuntime
        {
            protected override void Execute()
            {
                UnityEngine.Time.timeScale = 1f;
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
        }
    }
}
