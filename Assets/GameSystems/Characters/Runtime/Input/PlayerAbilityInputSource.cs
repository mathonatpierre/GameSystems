using UnityEngine;
using UnityEngine.InputSystem;
using System;
using GameSystems.Abilities;
using UnityEngine.Scripting.APIUpdating;

namespace GameSystems.Characters
{
    [MovedFrom(true, "GameSystems.Abilities", "GameSystems.Abilities", "PlayerAbilityInputSource")]
    [DefaultExecutionOrder(-400)]
    public sealed class PlayerAbilityInputSource : MonoBehaviour, ICharacterCommandSource,
        IHorizontalInputProvider, IDirectionalInputProvider, IAbilityInputState, IAbilityRequestObserver
    {
        [SerializeField] PlayerAbilityInputMap inputMap;
        double[] bufferedUntil;
        Action<InputAction.CallbackContext>[] performedHandlers;
        Action<InputAction.CallbackContext>[] canceledHandlers;
        readonly CharacterRequestBuffer pending = new();

        public float Horizontal => IsGameplayLocked ? 0f : inputMap != null && inputMap.horizontal != null
            ? inputMap.horizontal.action.ReadValue<float>() : Directional.x;
        public Vector2 Directional => IsGameplayLocked ? Vector2.zero :
            inputMap?.movement != null ? inputMap.movement.action.ReadValue<Vector2>() :
            new Vector2(inputMap?.horizontal != null ? inputMap.horizontal.action.ReadValue<float>() : 0f, 0f);
        bool IsGameplayLocked
        {
            get
            {
                if (GetComponent<CharacterAbilityController>()?.IsAbilityLocked == true) return true;
                MonoBehaviour[] behaviours = GetComponents<MonoBehaviour>();
                for (int i = 0; i < behaviours.Length; i++)
                    if (behaviours[i] is ICharacterInputGate gate && gate.BlocksCharacterInput)
                        return true;
                return false;
            }
        }
        public bool AnyAbilityHeld => AnyMappedAbilityHeld;
        public bool AnyMappedAbilityHeld
        {
            get
            {
                if (inputMap?.bindings == null) return false;
                for (int i = 0; i < inputMap.bindings.Length; i++)
                    if (inputMap.bindings[i].action != null && inputMap.bindings[i].action.action.IsPressed()) return true;
                return false;
            }
        }

        public void Configure(PlayerAbilityInputMap value)
        {
            if (isActiveAndEnabled) UnbindInput();
            inputMap = value;
            if (isActiveAndEnabled) BindInput();
        }

        void OnEnable() => BindInput();

        void BindInput()
        {
            if (inputMap?.horizontal != null) inputMap.horizontal.action.Enable();
            if (inputMap?.movement != null) inputMap.movement.action.Enable();
            int count = inputMap?.bindings?.Length ?? 0;
            bufferedUntil = new double[count];
            performedHandlers = new Action<InputAction.CallbackContext>[count];
            canceledHandlers = new Action<InputAction.CallbackContext>[count];
            for (int i = 0; i < count; i++)
            {
                int index = i;
                InputAction action = inputMap.bindings[i].action?.action;
                if (action == null) continue;
                if (inputMap.bindings[i].phase == AbilityInputPhase.Pressed)
                {
                    performedHandlers[i] = _ => Buffer(index);
                    action.performed += performedHandlers[i];
                }
                else if (inputMap.bindings[i].phase == AbilityInputPhase.Released)
                {
                    canceledHandlers[i] = _ => Buffer(index);
                    action.canceled += canceledHandlers[i];
                }
                action.Enable();
            }
        }

        void OnDisable() => UnbindInput();

        void UnbindInput()
        {
            if (inputMap?.horizontal != null) inputMap.horizontal.action.Disable();
            if (inputMap?.movement != null) inputMap.movement.action.Disable();
            if (inputMap?.bindings == null) return;
            for (int i = 0; i < inputMap.bindings.Length; i++)
            {
                InputAction action = inputMap.bindings[i].action?.action;
                if (action == null) continue;
                if (performedHandlers != null && performedHandlers[i] != null) action.performed -= performedHandlers[i];
                if (canceledHandlers != null && canceledHandlers[i] != null) action.canceled -= canceledHandlers[i];
                action.Disable();
            }
            bufferedUntil = null;
            performedHandlers = null;
            canceledHandlers = null;
        }

        void Buffer(int index)
        {
            if (inputMap?.bindings == null || index < 0 || index >= inputMap.bindings.Length) return;
            PlayerAbilityBinding binding = inputMap.bindings[index];
            if (binding.phase == AbilityInputPhase.Held) return;
            bufferedUntil[index] = Time.timeAsDouble + Mathf.Max(.001f, binding.bufferDuration);
        }

        public void Consume(InputAction consumedAction)
        {
            if (consumedAction == null || inputMap?.bindings == null || bufferedUntil == null) return;
            for (int i = 0; i < inputMap.bindings.Length && i < bufferedUntil.Length; i++)
            {
                PlayerAbilityBinding binding = inputMap.bindings[i];
                if (binding.phase != AbilityInputPhase.Held &&
                    binding.action?.action == consumedAction)
                    bufferedUntil[i] = 0d;
            }
        }

        public void CollectCommands(CharacterRuntimeContext context, CharacterRequestBuffer requests)
        {
            if (IsGameplayLocked) return;
            if (inputMap?.bindings == null) return;
            double now = Time.timeAsDouble;
            for (int i = 0; i < inputMap.bindings.Length; i++)
            {
                PlayerAbilityBinding binding = inputMap.bindings[i];
                InputAction action = binding.action != null ? binding.action.action : null;
                if (action == null || binding.ability == null) continue;
                bool triggeredThisFrame = binding.phase switch
                {
                    AbilityInputPhase.Pressed => action.WasPressedThisFrame(),
                    AbilityInputPhase.Held => action.IsPressed(),
                    AbilityInputPhase.Released => action.WasReleasedThisFrame(),
                    _ => false
                };
                if (triggeredThisFrame) bufferedUntil[i] = now + Mathf.Max(.001f, binding.bufferDuration);
                bool buffered = bufferedUntil[i] >= now;
                bool shouldRequest = binding.phase == AbilityInputPhase.Held ? action.IsPressed() : buffered;
                if (shouldRequest)
                    requests.Add(new AbilityRequest(binding.ability, this, action.ReadValue<float>(), now));
            }
        }

        void Update()
        {
            CharacterAbilityController abilities = GetComponent<CharacterAbilityController>();
            if (abilities == null) return;
            pending.Clear();
            CollectCommands(abilities.Context, pending);
            for (int i = 0; i < pending.Requests.Count; i++) abilities.Submit(pending.Requests[i]);
        }

        public bool IsHeld(AbilityDefinition ability)
        {
            if (inputMap?.bindings == null) return false;
            for (int i = 0; i < inputMap.bindings.Length; i++)
                if (inputMap.bindings[i].ability == ability && inputMap.bindings[i].action != null &&
                    inputMap.bindings[i].action.action.IsPressed()) return true;
            return false;
        }

        public void OnAbilityRequestResolved(in AbilityRequest request, AbilityRequestResult result)
        {
            if (result != AbilityRequestResult.Accepted || inputMap?.bindings == null) return;
            InputAction consumedAction = null;
            for (int i = 0; i < inputMap.bindings.Length; i++)
            {
                PlayerAbilityBinding binding = inputMap.bindings[i];
                if (binding.ability == request.Ability && binding.phase != AbilityInputPhase.Held)
                {
                    consumedAction = binding.action?.action;
                    break;
                }
            }
            Consume(consumedAction);
        }
    }
}
