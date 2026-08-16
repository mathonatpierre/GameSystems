# Visual Scripting Architecture

> **Status: official GameSystems architecture.** Actions are serialized calls to
> Unity APIs or stable module APIs. Gameplay scenarios belong in sequences.

## Design rule

An action is valid when its name can be read as one function call:

```text
Set Transform Position
Request Ability
Begin Rail Flight
Try Mount Rideable
Play Rideable Animation
```

An action must not hide a scenario such as instantiate + copy state + place on
ground + destroy, or mount + animate + interpolate + enable pose.

Continuous actions are allowed when they are drivers for a stable module loop,
for example horizontal locomotion, variable jump, rail flight or spline surface
locomotion. Their runtime state belongs to the driver, not to scene content.

## Context and rebinding

`GameActionContext` exposes three identities:

- `Owner`: object that owns the runner.
- `Self`: logical receiver of the current sequence.
- `Target`: logical argument supplied by the caller.

Every action parameter that identifies an object should use a `GameObjectValue`
or `ComponentTarget<T>`. A component target combines:

1. A rebindable source: constant, Owner, Self, Target, variable, Hook, contact,
   AI target, rideable occupant, parent, root or child path.
2. A component search scope: `OnObject`, `InParents` or `InChildren`.

Actions must not implement private fallback chains with `GetComponent` calls.

## Typed values

Values are polymorphic managed references selected in the Inspector:

- `GameObjectValue`
- `FloatValue`
- `BoolValue`
- `Vector3Value`
- `QuaternionValue`

Modules extend these types without changing Sequencing. Current module values
include motor velocity, air time, fall distance, stats, attributes, ability
request values, contacts, AI targets, patrol bounds and rideable occupants.

Values should be pure reads. State changes are actions.

## Local variables

Every runner owns a `GameActionBlackboard`. Nested and parallel sequences share
the caller blackboard. Actions can store spawned objects or calculated values,
and later actions can read them through variable values.

Variables are local to one top-level execution and never stored in a
ScriptableObject.

## Reusable sequence assets

`GameActionSequenceAsset` stores reusable conditions and ordered actions.
`RunActionSequenceAssetAction` calls an asset with the caller context and can
override its Target. Nested calls are depth-limited.

Reusable assets must not contain scene references. They receive objects through
Self, Target, Hook or variables.

Current shared assets:

- `ACTIONSEQ_RideableMount`
- `ACTIONSEQ_RideableDismount`

## Control flow

- `RunActionSequenceAssetAction`: call a reusable function.
- `RunConditionalAction`: choose a true or false sequence.
- `RunParallelAction`: tick multiple sequence branches concurrently.
- `WaitUntilConditionAction`: suspend until a predicate succeeds.
- `DelayAction`: suspend for time.
- Every condition can be negated.

## Migrated scenarios

### Rideable mount

```text
Try Mount Rideable
Play Rideable Animation "Mount"
Follow Rider Transition Animation
Set Rider Mounted Pose true
```

### Rideable dismount

```text
Prepare Rider Dismount
Play Rideable Animation "Dismount"
Follow Rider Transition Animation
Dismount Rider
```

### Replace character

```text
Set Collider Enabled false
Instantiate Game Object -> Replacement
Place Replacement On Ground
Copy Patrol Area Self -> Replacement
Destroy Self
```

### Contact evade

```text
Set Collider Enabled false
Move Character Along Arc to Clamp(AwayFrom(Contact Other), Patrol Area)
Reset Motor
Set Collider Enabled true
```

### Rail dash

```text
Pulse Camera Vertigo
Set Trail Emission true
Set Rail Speed 1.75
Delay 0.45
Set Rail Speed 1
Set Trail Emission false
```

## Removed composite types

- `MountRideableAction`
- `DismountRideableAction`
- `ReplaceCharacterAction`
- legacy multi-mode `MoveCharacterAlongArcAction`
- four contact-specific action variants
- composite `CharacterContactCondition`
- specialized numeric conditions for velocity, air time, fall distance, request
  value, stat, attribute and AI target distance

## Authoring checklist

Before adding an action:

1. Identify the Unity or module method/property represented by the action.
2. Use typed values for every dynamic argument.
3. Use `ComponentTarget<T>` for every component receiver.
4. Put waits, restoration and follow-up effects in the sequence.
5. Prefer an asset when the same ordered behavior can be used by multiple objects.
6. Add a module value instead of creating a new condition for one numeric field.
7. Keep reflection-based method invocation as an escape hatch only.

## Editor diagnostics and source layout

Every action and condition row exposes its live state in Play Mode: yellow while
executing or evaluating, green on success, and red on failure. Runtime failures
and caught exceptions are displayed directly below the corresponding row,
including when the row is collapsed.

Runtime primitives are grouped by the Unity or GameSystems API they model, rather
than one source file per serialized type. Prefer names such as
`GameObjectActions.cs`, `AnimatorActions.cs`, `CharacterMotorActions.cs`,
`GameObjectValues.cs`, and `MotorValues.cs`. Moving a type must preserve its full
type name so existing `SerializeReference` data remains valid.

## Reference model

The architecture is inspired by Game Creator 2's Instructions, `Args`
Self/Target context and polymorphic Properties:

- https://docs.gamecreator.io/gamecreator/visual-scripting/actions/
- https://docs.gamecreator.io/gamecreator/advanced/properties/
- https://docs.gamecreator.io/gamecreator/visual-scripting/actions/custom-instructions/

Lennie deliberately uses typed module adapters as the normal path and keeps
free-form method invocation secondary.
