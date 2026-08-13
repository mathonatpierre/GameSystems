# GameSystems Architecture Audit

Date: 2026-08-12

Cleanup status: legacy character definitions, the unused locomotion animation graph, the duplicate
horizontal movement action, the Rigidbody motor, dead state-provider types, and unused Core IDs have
been removed. Module-specific Editor assemblies are active. `GameSystems.Sequencing` is now a
first-class module, ability conditions share one namespace, and activation is represented by a single
`Auto Start` flag. Character responsibilities are now split into `GameSystems.Characters.Core`
(neutral motor/service contracts), `GameSystems.Abilities` (ability definitions and orchestration),
and `GameSystems.Characters` (Unity motor, AI, input, contacts, services and presentation adapters).
Remaining identity and Feedback migrations require dedicated serialized-content conversion.

## Executive summary

The runtime foundations are sound: ScriptableObject definitions, per-owner runtimes, reusable
conditions/actions, a motor abstraction, stat assets, hooks, and playable animation assets form a
coherent plugin base. The main problem is architectural drift. `GameSystems.Abilities` now owns an
entire character framework, `Core/Sequencing` compiles as `GameSystems.Sequencing`, and several old
identity and animation systems remain serialized beside their replacements.

The recommended direction is:

- Make `Sequencing` a first-class Core module with one stable vocabulary.
- Split character orchestration from ability definitions without merging their responsibilities.
- Keep stats, contacts, motor, commands, abilities, and presentation as explicit components.
- Replace duplicate enum/string identity systems with asset references.
- Delete legacy types only after migrating their serialized assets by GUID-safe editor scripts.
- Give every runtime and editor module an assembly boundary.

## Priority findings

### P0 - Remove misleading legacy systems after asset migration

1. `CharacterDefinition` duplicates data already owned by `CharacterAbilityController`,
   `CharacterStats`, and `CharacterController`. It has no runtime consumer, but three `CHAR_*`
   assets still serialize it. Migrate those assets, then delete the type and assets.
2. `PlayableLocomotionAnimationSet` and its node/state/transition model have no runtime consumer.
   The current path is `AbilityAnimationIntent -> PlayableAnimationAsset ->
   UnityPlayableAnimationPlayer`. Migrate/delete `ANIMSET_LennieLocomotion` and the orphan graph
   types.
3. `HorizontalMovementAction` duplicates `HorizontalLocomotionAction`. Only the old modular Lennie
   ability still serializes it. Migrate that asset or delete the unused ability, then remove the old
   action.
4. `RigidbodyCharacterMotor` is no longer used by prefabs and remains referenced only by migration
   tooling. Remove it once the migration tool no longer needs to recognize it.
5. `ICharacterStateProvider` and `CharacterStateCondition` have no provider implementation. Remove
   both or replace them with conditions against concrete motor/ability state.

### P1 - Fix module identity and ownership

1. `Assets/GameSystems/Core/Sequencing` compiling as `GameSystems.Sequencing` gives the same concept
   three names. Rename the assembly and namespace to `GameSystems.Sequencing`, or place it at
   `GameSystems/Core/Sequencing` under `GameSystems.Core.Sequencing`. The former is clearer as a
   reusable product module.
2. `GameSystems.Abilities` contains AI, input, motor, contacts, character services, animation glue,
   conditions, and actions. It is a character framework, not an ability-only assembly.
3. Conditions are split between `GameSystems.Abilities.Conditions`,
   `GameSystems.Abilities.Embedded`, and the root namespace. Use
   `GameSystems.Characters.Conditions` for character conditions and
   `GameSystems.Abilities.Conditions` only for ability lifecycle conditions.
4. `ReactiveSequenceController` is stat-specific: it listens only to `CharacterStats.AttributeChanged`.
   Rename it `StatChangeSequenceController`, or implement a genuinely generic event source/rule
   layer in Sequencing.
5. `PlayableCharacterAnimationDriver` is a useful adapter, but its name is broad and it also owns
   facing. Rename it `AbilityAnimationDriver`; move facing to a small presentation component or a
   procedural track if it must remain independently configurable.

### P1 - Remove duplicate identities and extensibility switches

1. Reactions currently have three identities: direct `ReactionDefinition`, `ReactionId`, and a
   custom string. Use the asset reference as identity. Optional aliases should be `HookId`-style
   assets, not enum-plus-string fallback.
2. `PlatformTypeDefinition` is already an extensible identity asset, while `PlatformTypeId` hardcodes
   the known platform catalogue. Replace enum checks with traits, capabilities, or direct definition
   references.
3. `FeedbackKind` drives a large runtime switch and matching editor switches. Adding a feedback
   requires editing central files. Move to polymorphic serialized feedback steps, each owning its
   runtime and editor metadata.
4. `NumberComparison`, `NumericComparison`, and `AnimationComparison` duplicate one closed concept.
   Keep one Core comparator, or use comparator condition subclasses if enum-free authoring is a firm
   requirement.

### P1 - Runtime and configuration robustness

1. `PlayerAbilityInputSource.Update` and `CharacterAIController.Update` allocate a new
   `CharacterRequestBuffer` repeatedly. Retain one buffer per component and clear it.
2. AI target fallback calls `FindObjectsByType<CharacterAbilityController>()` on every decision.
   Prefer hooks, an injected target provider, or a maintained character registry.
3. The frame pipeline depends on magic execution orders `-400`, `-300`, `-200`, and `50`. Keep the
   order annotations as a guard, but make one explicit character tick owner or document and test the
   phase contract (`commands -> abilities -> motor -> contacts -> presentation`).
4. Mandatory component relationships are often discovered with `GetComponent` and silently ignored.
   Add `RequireComponent` where dependencies are unconditional and `OnValidate` diagnostics where
   they are optional.
5. `CharacterAbilityController` has grown to 420 lines and owns runtime construction, requests,
   locks, transitions, grouping, and debug state. Extract plain C# collaborators for request
   arbitration and transition execution while keeping one public MonoBehaviour facade.

### P2 - File and editor hygiene

1. Add `GameSystems.<Module>.Editor.asmdef` assemblies. Editor code currently falls into the default
   project editor assembly, weakening package boundaries and increasing recompiles.
2. Split files containing multiple public serialized types. Priority candidates are
   `ProceduralAnimationClip.cs`, `CharacterContactAction.cs`, and `FeedbackEditors.cs`.
3. Rename `CharacterRuntimeServices.cs` to `CharacterCheckpointService.cs`; the old umbrella name no
   longer describes its content.
4. Replace icon selection by `type.Name` strings with a `GameSystemIconAttribute` or a typed editor
   registry. String matching silently loses icons after renames.
5. Normalize `CreateAssetMenu` paths and filenames. Use one catalogue such as
   `Game Systems/Characters/...`, `Game Systems/Sequencing/...`, and consistent prefixes (`ABILITY_`,
   `REACTION_`, `SEQUENCE_`, `STAT_`, `ATTRIBUTE_`, `PLATFORM_`).

## Target module layout

```text
Assets/GameSystems/
  Core/
    Runtime/                       ranges, service registry, shared primitives
    Editor/
  Sequencing/
    Runtime/Core/                  Action, Condition, Sequence, Runner, Context
    Runtime/Actions/               engine-generic actions
    Runtime/Conditions/            engine-generic conditions
    Editor/
    Tests/
  Stats/
    Runtime/Definitions/
    Runtime/Formula/
    Runtime/Runtime/
    Runtime/Sequencing/            stat actions and conditions
    Editor/
    Tests/
  Characters/
    Runtime/Core/                  context and composition contracts
    Runtime/Commands/              player and AI command sources
    Runtime/Motor/
    Runtime/Contacts/
    Runtime/Services/              checkpoints, registries
    Runtime/Presentation/          ability animation adapter, facing
    Runtime/Sequencing/            character actions and conditions
    Editor/
    Tests/
  Abilities/
    Runtime/Core/                  definitions, runtimes, requests, controller
    Runtime/Definitions/           locomotion, reaction, sequence ability
    Runtime/Transitions/
    Runtime/Sequencing/            ability lifecycle actions and conditions
    Editor/
    Tests/
  Playables/
    Runtime/Assets/
    Runtime/Runtime/
    Runtime/Tracks/                one procedural track per file
    Editor/
    Tests/
  Feedbacks/
    Runtime/Core/
    Runtime/Steps/                 polymorphic feedback implementations
    Runtime/Player/
    Editor/
    Tests/
  Hooks/
  LevelGeneration/
```

`Characters` may remain physically below `Abilities` during the first migration, but it should have
its own namespace immediately. Splitting the assembly can follow once references are clean.

## Naming rules

| Role | Pattern | Example |
|---|---|---|
| Authoring asset | `*Definition` only when it defines reusable data | `AbilityDefinition` |
| Runtime instance | `*Runtime` | `SequenceAbilityRuntime` |
| Scene coordinator | `*Controller` | `CharacterAbilityController` |
| Input producer | `*CommandSource` | `PlayerCommandSource`, `AICommandSource` |
| Unity-to-domain adapter | `*Driver` | `AbilityAnimationDriver` |
| Stateless operation | `*Service` only for a real service API | `FeedbackService` |
| Serializable sequence unit | `*Action`, `*Condition` | `TeleportCharacterAction` |
| Asset collection | `*Set` only for a curated reusable set | `AbilitySet` |

Avoid `Manager`, `System`, `Data`, and umbrella filenames unless they describe an actual boundary.
Avoid type names that promise generic behavior when the implementation is tied to stats or characters.

## Recommended migration order

1. Add tests and migration diagnostics for serialized managed references and ScriptableObject GUIDs.
2. Remove the confirmed legacy assets/types: CharacterDefinition, old animation graph, old horizontal
   movement, dead state provider, and unused rigidbody motor.
3. Normalize Sequencing folder, assembly, namespace, and editor assembly using Unity-aware moves.
4. Normalize `Abilities.Embedded` and split public types into correctly named files.
5. Remove reaction/platform duplicate identity systems with temporary compatibility migration code.
6. Extract the Characters namespace and clean the explicit frame pipeline.
7. Refactor Feedbacks to polymorphic steps; preserve existing assets through a one-shot converter.
8. Add per-module editor assemblies and tests, then remove migration shims.

Do not combine steps 2, 3, 5, and 7 in one migration. Each touches Unity type identity or managed
reference serialization and should be compiled, opened, and asset-validated independently.
