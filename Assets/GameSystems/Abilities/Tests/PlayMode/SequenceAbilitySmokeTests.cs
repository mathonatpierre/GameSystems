using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using GameSystems.Stats;
using GameSystems.Hooks;
using GameSystems.Playables;
using GameSystems.Feedbacks;
#if UNITY_EDITOR
using UnityEditor;
#endif

using GameSystems.Characters;

namespace GameSystems.Abilities.Tests
{
    [DefaultExecutionOrder(31000)]
    public sealed class LateRotationProbe : MonoBehaviour
    {
        Transform target;
        Quaternion previous;
        public float AccumulatedDegrees { get; private set; }

        public void Configure(Transform value)
        {
            target = value;
            previous = target != null ? target.localRotation : Quaternion.identity;
        }

        void LateUpdate()
        {
            if (target == null) return;
            AccumulatedDegrees += Quaternion.Angle(previous, target.localRotation);
            previous = target.localRotation;
        }
    }

    public sealed class SequenceAbilitySmokeTests
    {
        [UnityTest]
        public IEnumerator InterruptedFreeze_RestoresTimeScale()
        {
            object owner = new();
            Time.timeScale = 1f;
            IEnumerator freeze = FeedbackTime.Freeze(owner, .06f, 10f);
            Assert.That(freeze.MoveNext(), Is.True);
            Assert.That(Time.timeScale, Is.EqualTo(.06f).Within(.001f));
            FeedbackTime.ReleaseAll(owner);
            Assert.That(Time.timeScale, Is.EqualTo(1f).Within(.001f));
            yield return null;
        }

        [UnityTest]
        public IEnumerator GeneratedConcreteBalls_AllPatrolAndAnimate()
        {
            yield return SceneManager.LoadSceneAsync(0, LoadSceneMode.Single);
            yield return null;

            CharacterAIController[] controllers = Object.FindObjectsByType<CharacterAIController>();
            var balls = new System.Collections.Generic.List<CharacterAIController>();
            for (int i = 0; i < controllers.Length; i++)
            {
                CharacterAbilityController candidate = controllers[i].GetComponent<CharacterAbilityController>();
                if (candidate?.AbilitySet != null && candidate.AbilitySet.name == "ABILITYSET_ConcreteBall")
                    balls.Add(controllers[i]);
            }
            Assert.That(balls.Count, Is.GreaterThan(0));

            float landingDeadline = Time.realtimeSinceStartup + 3f;
            while (Time.realtimeSinceStartup < landingDeadline)
            {
                bool allGrounded = true;
                for (int i = 0; i < balls.Count; i++)
                    allGrounded &= balls[i].GetComponent<CharacterAbilityController>().Motor.Result.Ground.IsGrounded;
                if (allGrounded) break;
                yield return null;
            }

            var positions = new Vector3[balls.Count];
            var rotationProbes = new LateRotationProbe[balls.Count];
            for (int i = 0; i < balls.Count; i++)
            {
                positions[i] = balls[i].transform.position;
                rotationProbes[i] = balls[i].gameObject.AddComponent<LateRotationProbe>();
                rotationProbes[i].Configure(balls[i].GetComponent<PlayableAnimationBindings>()
                    .Resolve("Body"));
            }

            yield return new WaitForSeconds(.2f);

            for (int i = 0; i < balls.Count; i++)
            {
                CharacterAIController ai = balls[i];
                CharacterAbilityController brain = ai.GetComponent<CharacterAbilityController>();
                ICharacterPatrolArea area = ai.GetComponent(typeof(ICharacterPatrolArea)) as ICharacterPatrolArea;
                UnityPlayableAnimationPlayer player = ai.GetComponent<UnityPlayableAnimationPlayer>();
                float moved = Mathf.Abs(positions[i].x - ai.transform.position.x);
                Debug.Log($"[Ball Diagnostic] {ai.name}: movedX={moved:0.###}, " +
                    $"grounded={brain.Motor.Result.Ground.IsGrounded}, velocity={brain.Motor.Result.Velocity}, " +
                    $"decision={ai.CurrentDecision?.Label ?? "none"}, current={player.Current?.name ?? "none"}, " +
                    $"evaluations={player.EvaluationCount}, applied={player.Context.GetFloat("ProceduralAppliedRotation"):0.##}, " +
                    $"bounds=[{area?.MinimumX:0.##},{area?.MaximumX:0.##}]");
                Assert.That(moved, Is.GreaterThan(.05f),
                    $"{ai.name}: moved={moved:0.###}, grounded={brain.Motor.Result.Ground.IsGrounded}, " +
                    $"velocity={brain.Motor.Result.Velocity}, decision={ai.CurrentDecision?.Label ?? "none"}, " +
                    $"bounds=[{area?.MinimumX:0.##},{area?.MaximumX:0.##}]");
                Assert.That(player.Context.GetFloat("ProceduralAppliedRotation"), Is.GreaterThan(2f),
                    $"{ai.name}: procedural Body did not receive a visible rotation; velocity={brain.Motor.Result.Velocity}");
                Assert.That(rotationProbes[i].AccumulatedDegrees, Is.GreaterThan(.2f),
                    $"{ai.name}: rendered Body rotation stayed unchanged after LateUpdate; " +
                    $"velocity={brain.Motor.Result.Velocity}, proceduralAngle={player.Context.GetFloat("ProceduralRotationAngle"):0.##}");
            }
        }

        [UnityTest]
        public IEnumerator Lennie_MotorAbilitiesAndRespawn_RunAsSequences()
        {
            yield return SceneManager.LoadSceneAsync(0, LoadSceneMode.Single);
            yield return null;
            HookId playerHook = AssetDatabase.LoadAssetAtPath<HookId>(
                "Assets/Lennie/Data/Identity/HOOK_Player.asset");
            Assert.That(playerHook, Is.Not.Null, "HOOK_Player asset was not found.");
            CharacterAbilityController brain = HookRegistry.GetComponent<CharacterAbilityController>(playerHook);
            Assert.That(brain, Is.Not.Null, "HOOK_Player does not resolve to an AbilityController.");
            PlayerAbilityInputSource input = brain.GetComponent<PlayerAbilityInputSource>();
            Assert.That(input, Is.Not.Null, "Player Character has no input source.");
            input.enabled = false;

            AbilitySet set = brain.AbilitySet;
            foreach (AbilityDefinition ability in set.Abilities)
                Assert.That(ability, Is.InstanceOf<SequenceAbilityDefinition>(), $"{ability.name} is not sequence-based.");

            yield return null;
            yield return null;
            Assert.That(brain.CountActive(AbilityCategory.Locomotion), Is.GreaterThan(0), "No automatic locomotion sequence started.");

            CharacterAIController concreteBallAI = null;
            GameObject concreteBall = null;
#if UNITY_EDITOR
            GameObject concreteBallPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Lennie/Prefabs/Characters/PREF_ConcreteBall.prefab");
            Assert.That(concreteBallPrefab, Is.Not.Null, "Concrete Ball Character prefab is missing.");
            CharacterAIController[] sceneAI = Object.FindObjectsByType<CharacterAIController>();
            for (int i = 0; i < sceneAI.Length; i++)
            {
                CharacterAbilityController candidate = sceneAI[i].GetComponent<CharacterAbilityController>();
                if (candidate?.AbilitySet == null || !candidate.AbilitySet.name.Contains("ConcreteBall")) continue;
                concreteBallAI = sceneAI[i];
                concreteBall = concreteBallAI.gameObject;
                break;
            }
            if (concreteBallAI == null)
            {
                concreteBall = Object.Instantiate(concreteBallPrefab,
                    brain.transform.position + Vector3.right * 10f + Vector3.up, Quaternion.identity);
                concreteBall.name = "TEST_ConcreteBallCharacter";
                concreteBallAI = concreteBall.GetComponent<CharacterAIController>();
            }
            yield return null;
#endif
            Assert.That(concreteBallAI, Is.Not.Null, "No generated Concrete Ball Character AI was found.");
            CharacterAbilityController concreteBallBrain = concreteBallAI.GetComponent<CharacterAbilityController>();
            float landingDeadline = Time.realtimeSinceStartup + 2f;
            while (!concreteBallBrain.Motor.Result.Ground.IsGrounded && Time.realtimeSinceStartup < landingDeadline)
                yield return null;
            Assert.That(concreteBallBrain.Motor.Result.Ground.IsGrounded, Is.True,
                "Concrete Ball CharacterController motor did not land on the ground.");
            Transform concreteBody = concreteBallBrain.GetComponent<PlayableAnimationBindings>().Resolve("Body");
            float concreteBallStartX = concreteBallAI.transform.position.x;
            yield return new WaitForSecondsRealtime(.35f);
            Assert.That(concreteBallAI.CurrentDecision?.Label, Is.EqualTo("Patrol"),
                "Concrete Ball AI did not select Patrol.");
            Assert.That(Mathf.Abs(concreteBallAI.transform.position.x - concreteBallStartX), Is.GreaterThan(.005f),
                "Concrete Ball Patrol ability did not move its CharacterController motor.");
            UnityPlayableAnimationPlayer concreteAnimationPlayer = concreteBallBrain.GetComponent<UnityPlayableAnimationPlayer>();
            Assert.That(concreteAnimationPlayer.Current, Is.Not.Null,
                "Concrete Ball Patrol did not select its procedural animation intent.");
            Assert.That(concreteAnimationPlayer.Current.name, Is.EqualTo("PLAYABLE_ConcreteBall"));
            Assert.That(concreteAnimationPlayer.EvaluationCount, Is.GreaterThan(0),
                "Concrete Ball procedural animation was selected but never evaluated.");
            Assert.That(concreteAnimationPlayer.CurrentWeight, Is.GreaterThan(.1f),
                "Concrete Ball procedural animation never received blend weight.");
            Assert.That(concreteAnimationPlayer.Context.ResolveBinding("Body"), Is.EqualTo(concreteBody),
                "Concrete Ball procedural animation context did not resolve its Body binding.");
            concreteBallAI.enabled = false;
            concreteBallBrain.GetComponent<PlayableCharacterAnimationDriver>().enabled = false;
            concreteAnimationPlayer.Context.SetFloat("HorizontalSpeed", 1f);
            concreteAnimationPlayer.EvaluateNow();
            yield return null;
            Assert.That(((ProceduralAnimationClip)concreteAnimationPlayer.Current).TrackCount, Is.EqualTo(1));
            Assert.That(concreteAnimationPlayer.Context.GetFloat("HorizontalSpeed"), Is.EqualTo(1f));
            Assert.That(concreteAnimationPlayer.Context.GetFloat("ProceduralTrackEvaluations"), Is.GreaterThan(0f));
            Assert.That(concreteAnimationPlayer.Context.GetFloat("ProceduralResolvedTracks"), Is.GreaterThan(0f));
            Assert.That(concreteAnimationPlayer.Context.GetFloat("ProceduralRotationAngle"), Is.Not.EqualTo(0f));
            Assert.That(concreteAnimationPlayer.Context.GetFloat("ProceduralAppliedRotation"), Is.GreaterThan(.5f));

#if UNITY_EDITOR
            concreteBall = Object.Instantiate(concreteBallPrefab,
                brain.transform.position + Vector3.right * 10f + Vector3.up, Quaternion.identity);
            concreteBall.name = "TEST_ConcreteBallContacts";
            concreteBallAI = concreteBall.GetComponent<CharacterAIController>();
            concreteBallBrain = concreteBall.GetComponent<CharacterAbilityController>();
            yield return null;
#endif
            AbilityDefinition jump = Find(set, "Jump_Modular");
            Assert.That(brain.Request(jump, brain), Is.True, "Jump request was rejected.");
            yield return null;
            Assert.That(brain.Motor.Result.Velocity.y, Is.GreaterThan(0f), "Jump action did not write upward velocity.");

            AbilityDefinition bounce = Find(set, "Bounce");
            Assert.That(bounce.GetType(), Is.EqualTo(typeof(SequenceAbilityDefinition)),
                "Bounce must remain a generic sequence ability.");
            Assert.That(((SequenceAbilityDefinition)bounce).Sequence.Actions,
                Has.Some.InstanceOf<GameSystems.Abilities.Actions.BounceAction>(),
                "Bounce sequence does not contain its bounce action.");
            Assert.That(brain.Request(bounce, brain, 7.8f),
                Is.True, "Bounce action request was rejected.");
            yield return null;
            Assert.That(brain.Motor.Result.Velocity.y, Is.GreaterThan(0f), "Bounce action did not write upward velocity.");

            AbilityDefinition stomp = Find(set, "Stomp_Modular");
            Assert.That(brain.Request(stomp, brain), Is.True, "Stomp request was rejected while airborne.");
            yield return null;
            Assert.That(brain.Motor.Result.Velocity.y, Is.LessThan(0f), "Stomp action did not write downward velocity.");

            CharacterContactController contactController = concreteBallAI.GetComponent<CharacterContactController>();
            Assert.That(contactController, Is.Not.Null, "Concrete Ball has no unified contact controller.");
            ICharacterMotorControl playerMotor = brain.Motor as ICharacterMotorControl;
            Assert.That(playerMotor, Is.Not.Null);
            Collider concreteBallCollider = concreteBallAI.GetComponent<Collider>();
            CharacterController playerController = brain.GetComponent<CharacterController>();
            float playerFeetOffset = playerController.center.y - playerController.height * .5f;
            playerMotor.Teleport(new Vector3(concreteBallAI.transform.position.x,
                concreteBallCollider.bounds.max.y - playerFeetOffset + .05f, concreteBallAI.transform.position.z));
            playerMotor.SetVelocity(Vector3.down * 2f);
            yield return null;
            contactController.ReceiveCharacterContact(brain, concreteBallAI.transform.position + Vector3.up,
                Vector3.right);
            yield return new WaitForSecondsRealtime(.15f);
            Assert.That(concreteBall == null, Is.True,
                "Purified Concrete Ball was not replaced and destroyed.");
            CharacterAbilityController slimeBrain = FindCharacter("CHAR_Slime");
            Assert.That(slimeBrain, Is.Not.Null, "Purification did not instantiate the Slime Character prefab.");
            CharacterContactController slimeContacts = slimeBrain.GetComponent<CharacterContactController>();
            Collider slimeCollider = slimeBrain.GetComponent<Collider>();
            Transform slimeModel = slimeBrain.GetComponentInChildren<Renderer>().transform;
            yield return new WaitForSecondsRealtime(.5f);
            Assert.That(slimeModel.localPosition.y, Is.EqualTo(-.3f).Within(.01f),
                "Slime reveal feedback left its visual model floating above its rest pose.");
            UnityPlayableAnimationPlayer slimeAnimationPlayer = slimeBrain.GetComponent<UnityPlayableAnimationPlayer>();
            Assert.That(slimeAnimationPlayer.Current, Is.Not.Null,
                "Slime Patrol did not select its procedural animation intent.");
            Assert.That(slimeAnimationPlayer.Current.name, Is.EqualTo("PLAYABLE_Slime"));
            Transform slimeVisual = slimeBrain.GetComponent<PlayableAnimationBindings>().Resolve("Body");
            Vector3 slimeVisualPosition = slimeVisual.localPosition;
            yield return new WaitForSecondsRealtime(.08f);
            Assert.That(Vector3.Distance(slimeVisualPosition, slimeVisual.localPosition), Is.GreaterThan(.005f),
                "Slime procedural animation clip did not animate its Body binding.");
            playerMotor.Teleport(new Vector3(slimeBrain.transform.position.x,
                slimeCollider.bounds.max.y - playerFeetOffset + .05f, slimeBrain.transform.position.z));
            playerMotor.SetVelocity(Vector3.down * 2f);
            Assert.That(HookRegistry.Get(playerHook), Is.EqualTo(brain.gameObject), "HOOK_Player no longer resolves to Lennie.");
            Assert.That(brain.Motor.Result.Velocity.y, Is.LessThanOrEqualTo(.12f), "Lennie is not descending onto the slime.");
            float slimeFeetY = brain.transform.position.y + playerFeetOffset;
            Assert.That(slimeFeetY, Is.InRange(slimeCollider.bounds.max.y - .16f,
                slimeCollider.bounds.max.y + .2f), "Lennie's feet are outside the slime top-contact band.");
            slimeContacts.ReceiveCharacterContact(brain, slimeCollider.bounds.max, Vector3.up);
            Assert.That(slimeContacts.Rules[0].Sequence.Conditions[0].DebugResult, Is.True,
                "Purified Stomp rejected the top-contact condition.");
            Assert.That(brain.LastRequestedAbility, Is.EqualTo(bounce),
                "Purified Stomp did not request Lennie's Bounce ability.");
            Assert.That(brain.LastRequestResult, Is.EqualTo(AbilityRequestResult.Accepted),
                "Purified Stomp requested Bounce, but the ability controller rejected it.");
            yield return null;
            Assert.That(brain.Motor.Result.Velocity.y, Is.GreaterThan(0f),
                "A purified slime stomp must bounce Lennie.");

            float slimeStartX = slimeBrain.transform.localPosition.x;
            playerMotor.Teleport(slimeBrain.transform.position + Vector3.left * .3f);
            playerMotor.SetVelocity(Vector3.zero);
            slimeContacts.ReceiveCharacterContact(brain, slimeBrain.transform.position, Vector3.right);
            yield return new WaitForSecondsRealtime(.65f);
            CharacterController slimeController = slimeBrain.GetComponent<CharacterController>();
            Assert.That(Mathf.Abs(slimeBrain.transform.localPosition.x - slimeStartX), Is.GreaterThan(.05f),
                "Slime Evade ability did not move away from the contact.");
            Assert.That(slimeCollider.enabled, Is.True, "Slime Evade did not restore its collider.");
            Assert.That(slimeController.enabled, Is.True, "Slime Evade did not restore its CharacterController.");
            float slimeLandingDeadline = Time.realtimeSinceStartup + 1.5f;
            while (!slimeBrain.Motor.Result.Ground.IsGrounded && Time.realtimeSinceStartup < slimeLandingDeadline)
                yield return null;
            Assert.That(slimeBrain.Motor.Result.Ground.IsGrounded, Is.True,
                "Slime did not fall back to the ground after its evade arc.");

            Assert.That(brain.RequestReaction(ReactionId.Respawn), Is.True, "Respawn reaction was rejected.");
            yield return new WaitForSecondsRealtime(1.45f);
            yield return null;
            Assert.That(brain.IsAbilityLocked, Is.False, "Respawn sequence did not release its lock.");
            Assert.That(ContainsActive(brain, ReactionId.Respawn), Is.False,
                "Respawn sequence did not complete.");
            Assert.That(brain.CountActive(AbilityCategory.Locomotion), Is.GreaterThan(0),
                "Locomotion did not restart after respawn.");

            CharacterStats brainStats = brain.GetComponent<CharacterStats>();
            RuntimeAttribute health = brainStats.GetAttribute(brainStats.Definition.PrimaryHealth);
            GameObject sideBall = Object.Instantiate(concreteBallPrefab,
                brain.transform.position + Vector3.right * .45f, Quaternion.identity);
            yield return null;
            CharacterAbilityController sideBallBrain = sideBall.GetComponent<CharacterAbilityController>();
            CharacterContactController sideContacts = sideBall.GetComponent<CharacterContactController>();
            float healthBeforeSideContact = health.Current;
            sideContacts.ReceiveCharacterContact(brain, brain.transform.position, Vector3.right);
            yield return null;
            Assert.That(health.Current, Is.EqualTo(healthBeforeSideContact - 1f).Within(.001f),
                "A side contact must damage Lennie exactly once.");
            Assert.That(ContainsActive(sideBallBrain, "Purify"), Is.False,
                "A side contact must not trigger Concrete Ball purification.");
            Object.Destroy(sideBall);
            yield return new WaitForSecondsRealtime(.75f);

            float healthBeforeFall = health.Current;
            Assert.That(playerMotor, Is.Not.Null);
            playerMotor.Teleport(brain.transform.position + Vector3.up * 12f);
            playerMotor.SetVelocity(Vector3.down * 4f);
            yield return new WaitForSecondsRealtime(3f);
            yield return null;
            Assert.That(health.Current, Is.EqualTo(healthBeforeFall - 1f).Within(.001f),
                "A lethal fall must apply exactly one point of damage.");
            Assert.That(brain.IsAbilityLocked, Is.False, "Fall respawn did not release its lock.");
        }

        static AbilityDefinition Find(AbilitySet set, string namePart)
        {
            foreach (AbilityDefinition ability in set.Abilities)
                if (ability.name.Contains(namePart)) return ability;
            Assert.Fail($"Ability containing '{namePart}' was not found.");
            return null;
        }

        static CharacterAbilityController FindCharacter(string definitionName)
        {
            CharacterAbilityController[] characters = Object.FindObjectsByType<CharacterAbilityController>();
            for (int i = 0; i < characters.Length; i++)
                if (characters[i].AbilitySet != null &&
                    characters[i].AbilitySet.name == definitionName.Replace("CHAR_", "ABILITYSET_"))
                    return characters[i];
            return null;
        }

        static bool ContainsActive(CharacterAbilityController controller, ReactionId id)
        {
            for (int i = 0; i < controller.ActiveAbilities.Count; i++)
                if (controller.ActiveAbilities[i].Definition is ReactionDefinition reaction && reaction.Matches(id))
                    return true;
            return false;
        }

        static bool ContainsActive(CharacterAbilityController controller, string id)
        {
            for (int i = 0; i < controller.ActiveAbilities.Count; i++)
                if (controller.ActiveAbilities[i].Definition is ReactionDefinition reaction && reaction.Matches(id))
                    return true;
            return false;
        }
    }
}
