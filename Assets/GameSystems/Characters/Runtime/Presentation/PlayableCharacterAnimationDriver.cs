using GameSystems.Playables;
using GameSystems.Abilities;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace GameSystems.Characters
{
    [MovedFrom(true, "GameSystems.Abilities", "GameSystems.Abilities", "PlayableCharacterAnimationDriver")]
    // Character-specific bridge only. Graph construction, clips and blending live
    // entirely in GameSystems.Playables.UnityPlayableAnimationPlayer.
    [DefaultExecutionOrder(50)]
    [RequireComponent(typeof(CharacterAbilityController), typeof(UnityPlayableAnimationPlayer))]
    public sealed class PlayableCharacterAnimationDriver : MonoBehaviour
    {
        [SerializeField] Animator animator;
        [SerializeField] Transform visualRoot;
        [SerializeField, Min(0f)] float facingDeadZone = .04f;
        [SerializeField, Min(.01f)] float turnSharpness = 24f;
        [SerializeField] bool startFacingRight = true;

        CharacterAbilityController abilities;
        UnityPlayableAnimationPlayer player;
        AbilityDefinition displayedAbility;

        public void Configure(Animator output, Transform visual)
        {
            animator = output;
            visualRoot = visual;
        }

        void Awake()
        {
            abilities = GetComponent<CharacterAbilityController>();
            player = GetComponent<UnityPlayableAnimationPlayer>();
            if (player == null) player = gameObject.AddComponent<UnityPlayableAnimationPlayer>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (visualRoot == null && animator != null) visualRoot = animator.transform;
            player.Configure(animator);
            if (visualRoot != null)
                visualRoot.localRotation = Quaternion.Euler(0f, startFacingRight ? 90f : -90f, 0f);
        }

        void Start() => RefreshPresentation();

        void Update()
        {
            RefreshPresentation();
        }

        void RefreshPresentation()
        {
            if (abilities?.Motor == null || player == null) return;
            CharacterMotorResult motor = abilities.Motor.Result;
            PlayableAnimationContext context = player.Context;
            context.SetFloat("Speed", Mathf.Abs(motor.Velocity.x));
            context.SetFloat("HorizontalSpeed", motor.Velocity.x);
            context.SetFloat("VerticalSpeed", motor.Velocity.y);
            context.SetFloat("Grounded", motor.Ground.IsGrounded ? 1f : 0f);
            context.SetFloat("JustLanded", motor.JustLanded ? 1f : 0f);
            context.SetFloat("JustLeftGround", motor.JustLeftGround ? 1f : 0f);
            context.SetFloat("AirTime", motor.AirTime);

            AbilityDefinition winner = FindPresentationWinner();
            if (winner != displayedAbility)
            {
                displayedAbility = winner;
                AbilityAnimationIntent intent = winner?.AnimationIntent;
                // Several abilities may intentionally share one presentation asset
                // (Jump, Bounce, AirLocomotion, Fall). Changing gameplay ownership
                // must not restart the same playable and create an airborne pop.
                if (intent?.Animation != null && player.Current != intent.Animation)
                    player.Play(intent.Animation, intent.BlendDuration);
            }

            UpdateFacing(motor.Velocity.x);
        }

        AbilityDefinition FindPresentationWinner()
        {
            if (abilities == null) return null;
            AbilityDefinition winner = null;
            int winnerPriority = int.MinValue;
            var active = abilities.ActiveAbilities;
            for (int i = 0; i < active.Count; i++)
            {
                AbilityDefinition candidate = active[i].Definition;
                AbilityAnimationIntent intent = candidate.AnimationIntent;
                if (intent == null || !intent.IsValid) continue;
                int priority = candidate.Priority + intent.PriorityOffset;
                if (winner != null && priority <= winnerPriority) continue;
                winner = candidate;
                winnerPriority = priority;
            }
            return winner;
        }

        void UpdateFacing(float horizontalVelocity)
        {
            if (visualRoot == null || Mathf.Abs(horizontalVelocity) <= facingDeadZone) return;
            Quaternion target = Quaternion.Euler(0f, horizontalVelocity > 0f ? 90f : -90f, 0f);
            visualRoot.localRotation = Quaternion.Slerp(visualRoot.localRotation, target,
                1f - Mathf.Exp(-turnSharpness * Time.deltaTime));
        }
    }
}
