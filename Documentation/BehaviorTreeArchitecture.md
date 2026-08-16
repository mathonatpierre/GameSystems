# Character Behavior Trees

## Modules

- `GameSystems.Core.Editor.NodeEditor` owns generic graph presentation and editing.
- `GameSystems.Characters.AI` owns behavior-tree assets, nodes, runtime state and execution.
- `GameSystems.Characters.AI.Editor` adapts behavior trees to the generic node editor.
- `CharacterAIController` owns character sensing, traversal planning and command submission.

The node editor must not reference character or behavior-tree types. Character movement and
physics planning stay in `CharacterAIController`; nodes orchestrate reusable conditions, actions
and controller functions.

## Runtime

`CharacterBehaviorTree` stores a flat managed-reference node array. Nodes have stable GUIDs and
composites store ordered child GUIDs. `CharacterBehaviorTreeRuntime` stores all mutable state per
controller, so tree assets remain immutable and reusable by multiple characters.

Statuses are `Inactive`, `Running`, `Success` and `Failure`. Runtime state also records the last
tick, diagnostic message, cursor, timer and node-specific data. Cycles fail explicitly instead of
recursing indefinitely.

Available foundations:

- Composites: Selector, Sequence.
- Decorator: Inverter.
- Leaves: Condition, Action Sequence, Request Ability, Wait, Subtree.

Condition and Action Sequence leaves embed the existing `GameCondition` and `GameAction` types.
Subtrees reference another `CharacterBehaviorTree` asset.

## Editor

Open a tree asset and use **Open Behavior Tree Editor** in its inspector. Right-click the graph to
create nodes, connect ports to define ordered children, drag nodes to position them, and delete
nodes or edges normally. Selecting a node exposes its complete serialized data in the right pane.

In Play Mode, nodes update live: yellow is Running, green is Success and red is Failure. Runtime
diagnostics appear in node tooltips.

## Lennie Title

`BTREE_Lennie_Title` replaces the legacy priority decision array:

1. Try jumping onto a reachable character.
2. Otherwise, try jumping across a reachable gap.

The existing ballistic landing planner, moving-platform prediction and airborne steering remain
owned by `CharacterAIController`.
