using System.Collections;
using GameSystems.Sequencing;
using GameSystems.Sequencing.Values;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GameSystems.Abilities.Tests
{
    public sealed class VisualScriptingCompositionTests
    {
        [Test]
        public void ContextValues_RebindSelfTargetAndComponentSearch()
        {
            GameObject root = new("Root");
            GameObject child = new("Child");
            child.transform.SetParent(root.transform);
            BoxCollider collider = root.AddComponent<BoxCollider>();
            try
            {
                GameActionContext context = new(child, child, root);
                Assert.That(new SelfGameObjectValue().Get(context), Is.EqualTo(child));
                Assert.That(new TargetGameObjectValue().Get(context), Is.EqualTo(root));
                ComponentTarget<BoxCollider> binding = new(
                    new SelfGameObjectValue(), ComponentSearchScope.InParents);
                Assert.That(binding.Get(context), Is.EqualTo(collider));
            }
            finally { Object.DestroyImmediate(root); }
        }

        [UnityTest]
        public IEnumerator SequenceAsset_RunsWithCallerBindings()
        {
            GameObject owner = new("Sequence Owner");
            GameActionSequenceAsset asset =
                ScriptableObject.CreateInstance<GameActionSequenceAsset>();
            asset.Configure(System.Array.Empty<GameCondition>(), new GameAction[]
            {
                new SetGameObjectActiveAction(false)
            });
            GameActionRunner runner = new();
            runner.Initialize(new GameAction[] { new RunActionSequenceAssetAction(asset) },
                new GameActionContext(owner, owner, null));
            runner.Start();
            runner.Tick(0f);
            Assert.That(owner.activeSelf, Is.False);
            Assert.That(runner.Failed, Is.False);
            Object.DestroyImmediate(owner);
            Object.DestroyImmediate(asset);
            yield return null;
        }
    }
}
