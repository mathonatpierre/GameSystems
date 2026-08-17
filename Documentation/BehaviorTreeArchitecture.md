# Character Behavior Trees

## Responsibilities

- `GameSystems.Core.Editor.NodeEditor` owns reusable graph layout, ports, selection and framing.
- `GameSystems.Characters.AI` owns tree assets, nodes, runtime state, sensors and the blackboard.
- `GameSystems.Characters.AI.Editor` adapts character trees to the generic node editor.
- `CharacterAIController` is a thin Unity adapter: it samples sensors, ticks the tree and submits
  the resulting ability requests and horizontal input.

Gameplay decisions do not belong in the controller. They are assembled in tree assets from
conditions and action sequences. Traversal planning, input intent and ability requests are exposed
as serialized actions so their order and parameters remain visible and reusable.

## Runtime Model

`CharacterBehaviorTree` stores a flat managed-reference node array. Every node has a stable GUID;
composites store ordered child GUIDs. `CharacterBehaviorTreeRuntime` owns mutable execution state per
controller, leaving tree assets immutable and reusable by multiple characters.

`CharacterAIBlackboard` contains the sampled character state and explicit working state shared by
nodes: target, direction, distance, grounding, velocity, wall contacts, horizontal intent, accepted
abilities and planned traversal landing. `CharacterAISensors` performs world queries and writes
their results to the blackboard before each tree tick.

Statuses are `Inactive`, `Running`, `Success` and `Failure`. Runtime state also records the last tick,
diagnostic message, child cursor, timer and node-specific data. Cycles fail explicitly instead of
recursing indefinitely.

## Nodes

Available structural nodes:

- `Selector`: evaluates children in priority order until one succeeds or runs.
- `Sequence`: evaluates children in order until one fails or runs.
- `Inverter`: reverses success and failure.
- `Cooldown`: rate-limits its child.
- `Condition`: evaluates an ordered list of generic `GameCondition` instances.
- `Action Sequence`: runs an ordered list of generic `GameAction` instances.
- `Wait`: remains running for a duration.
- `Subtree`: delegates to another reusable tree asset.

Conditions and actions use the same serialized visual-scripting primitives as sequencing. AI-specific
primitives expose only atomic character operations such as testing an ability, checking a capsule
trajectory, planning a reachable landing, setting horizontal intent or queueing an ability request.
Complex behavior is expressed by composing those primitives in the graph.

## Editor And Debugging

Open a tree asset and choose **Open Behavior Tree Editor** in its inspector. Right-click the graph to
create nodes, connect ports to define ordered children, drag nodes to position them, and delete nodes
or edges normally. Generated trees use branch-aware spacing so their structure remains readable.

Nodes have icons and colors by type. Their embedded conditions and actions can be expanded directly
inside the node, while selection exposes the complete serialized data in the right inspector. In
Play Mode, yellow means Running, green Success and red Failure; runtime diagnostics are available in
node tooltips.

## Lennie Title

`BTREE_Lennie_Title` is the authoritative Title AI. Its branches compose the same generic conditions
and actions used elsewhere to:

1. Finish reachable enemies, steer through the airborne approach, then resume rightward progression.
2. Enter, maintain and leave wall-jump traversal while validating capsule clearance.
3. Traverse generated gaps and moving landings with planned airborne steering.
4. Use long spline-glider chunks, including loops.
5. Fall back to continuous rightward locomotion.

Concrete Ball and Slime also reference behavior-tree assets. The former decision-array and hidden
traversal-operation paths have been removed; behavior ordering and parameters live in inspectable
tree nodes and action sequences.
