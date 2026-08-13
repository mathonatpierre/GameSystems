using System;
using System.Collections.Generic;
using GameSystems.Sequencing;
using GameSystems.Stats;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using GameSystems.Abilities;

namespace GameSystems.Characters
{
    [Serializable]
    [MovedFrom(true, "GameSystems.Abilities", "GameSystems.Abilities", "StatChangeSequenceRule")]
    public sealed class StatChangeSequenceRule
    {
        [SerializeField] string label = "Reactive Rule";
        [SerializeField, Tooltip("Optional attribute filter.")] AttributeDefinition attribute;
        [SerializeField] bool runOnce = true;
        [SerializeField] GameActionSequence sequence = new();
        [NonSerialized] bool consumed;

        public string Label => label;
        public bool Matches(AttributeDefinition changed) => !consumed &&
            (attribute == null || attribute == changed);
        public GameActionRunner Start(in GameActionContext context)
        {
            if (!sequence.CanRun(context)) return null;
            if (runOnce) consumed = true;
            GameActionRunner runner = sequence.CreateRunner(context);
            runner.Start();
            return runner;
        }
    }

    [DisallowMultipleComponent]
    [MovedFrom(true, "GameSystems.Abilities", "GameSystems.Abilities", "StatChangeSequenceController")]
    public sealed class StatChangeSequenceController : MonoBehaviour
    {
        [SerializeField] StatChangeSequenceRule[] rules;
        readonly List<GameActionRunner> runners = new();
        CharacterAbilityController abilities;
        CharacterStats stats;

        void Awake()
        {
            abilities = GetComponent<CharacterAbilityController>();
            stats = GetComponent<CharacterStats>();
        }

        void OnEnable()
        {
            if (stats == null) stats = GetComponent<CharacterStats>();
            if (stats != null) stats.AttributeChanged += OnAttributeChanged;
        }

        void OnDisable()
        {
            if (stats != null) stats.AttributeChanged -= OnAttributeChanged;
            runners.Clear();
        }

        void Update()
        {
            for (int i = runners.Count - 1; i >= 0; i--)
                if (runners[i].Tick(Time.deltaTime)) runners.RemoveAt(i);
        }

        void OnAttributeChanged(AttributeDefinition attribute, float previous, float current)
        {
            if (abilities?.Context == null || rules == null) return;
            AbilityEvaluationContext evaluation = new(abilities.Context, null, default,
                abilities.Motor != null ? abilities.Motor.Result : default);
            GameActionContext context = new(gameObject, abilities.Context, evaluation, this);
            for (int i = 0; i < rules.Length; i++)
            {
                if (rules[i] == null || !rules[i].Matches(attribute)) continue;
                GameActionRunner runner = rules[i].Start(context);
                if (runner != null && runner.IsRunning) runners.Add(runner);
            }
        }
    }
}
