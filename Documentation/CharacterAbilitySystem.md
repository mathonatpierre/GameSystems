# Character Ability System

> **Statut : architecture officielle du projet Lennie.**  
> L’ancien `LennieController` est désormais une implémentation legacy à migrer,
> et non une seconde architecture à maintenir.

## Décision d’architecture

Le gameplay du personnage est piloté par des **Character Abilities**, jamais par
l’Animator. L’animation est une présentation Playables du contexte produit par
les abilities et par le motor.

```text
Command Source (joueur ou IA)
    → Ability Runner
    → Ability Runtime + Modules Runtime
    → Character Motor
    → AnimSet Playables
    → Animator (sortie uniquement)
```

## Assets de données

- `CharacterDefinition` : identité et loadout du personnage.
- `CharacterAbilityLoadout` : abilities disponibles.
- `ModularAbilityDefinition` : scheduling, tags, conditions et modules.
- `AbilityCondition` : prédicat réutilisable et sans état runtime.
- `AbilityModule` : paramètres réutilisables d’un comportement.
- `PlayerAbilityInputMap` : input action → demande d’ability.
- `PlayableLocomotionAnimationSet` : clips, blends et transitions visuelles.
- `FeedbackSequence` : FX, audio, caméra et game feel.

Les ScriptableObjects ne stockent aucun état de partie. Chaque ability et chaque
module créent leur runtime propre pour un personnage donné.

## Runtime

- `CharacterAgent` orchestre une frame.
- `CharacterAbilityController` arbitre les demandes, priorités, channels,
  authorities, interruptions et cooldowns.
- `CharacterRuntimeContext` contient les services et données propres à l’agent.
- `AbilityRuntime` porte la durée, la phase et les tags accordés.
- `AbilityModuleRuntime` porte l’état temporaire de chaque module.
- `ICharacterMotor` est remplaçable (platformer, FPS, nage, monture, etc.).

## Composition

Une ability générique peut combiner par exemple :

```text
Jump
  Conditions : tag MovementAllowed, pas de tag Stunned
  Modules    : VariableJump, ConsumeStamina
  Tags       : Airborne, Movement.Jump
  Feedbacks  : Jump / Land
```

Les premiers modules disponibles sont :

- `HorizontalMovementModule`
- `VariableJumpModule`

Les anciennes abilities spécialisées restent temporairement compatibles afin de
ne pas casser les scènes existantes. Le test isolé utilise désormais les
abilities modulaires.

## Interruptions

Chaque ability définit :

- sa priorité ;
- son channel sémantique ;
- les ressources techniques dont elle demande ou verrouille l’autorité ;
- les tags autorisés à l’interrompre ;
- les tags qui ne peuvent jamais l’interrompre ;
- son cooldown.

Une demande est refusée explicitement avec un résultat débogable : cooldown,
tag manquant, tag bloquant, priorité insuffisante ou interruption interdite.

## Animation sans doublon

Les clips restent exclusivement dans l’AnimSet. Une ability accorde des gameplay
tags pendant son exécution. Un paramètre d’AnimSet de type `GameplayTag` peut lire
un de ces tags et piloter ses transitions. Ainsi l’ability exprime l’intention
(`Movement.Jump`, `Hurt`, `Riding`) sans référencer ni dupliquer un clip.

## Extension prévue

1. Ajouter des modules génériques : impulse, dash, dégâts, stamina, ciblage,
   rotation, root motion et interaction.
2. Ajouter des conditions génériques : cooldown partagé, ressource, pente,
   cible, distance et ligne de vue.
3. Construire l’Ability Graph comme vue/éditeur des assets existants, sans créer
   une seconde logique de state machine.
4. Réutiliser le même runner pour le joueur, les ennemis et les montures ; seule
   la Command Source change.

## Migration du prototype principal

La migration se fait sans maintenir deux logiques concurrentes :

1. Ajouter les abilities manquantes à parité avec le prototype : stomp, bounce,
   hurt, death/respawn, nature et victory lock.
2. Exposer les événements et commandes de gameplay par des interfaces générales
   (`IDamageable`, `IBounceReceiver`, `IRespawnable`, etc.).
3. Migrer les systèmes actuellement couplés à `LennieController` : ennemis,
   hitboxes, crushers, plateformes fragiles, fin de niveau, purification, caméra,
   papillons, HUD et générateur.
4. Remplacer `LennieController` dans `ConcretePrototype` par `CharacterAgent`, le
   motor, la command source, le runner et le presenter Playables.
5. Comparer la parité fonctionnelle et le feeling dans la scène principale.
6. Supprimer `LennieController` et ses chemins de code seulement après validation.

Le code legacy reste donc temporairement compilable, mais aucune nouvelle feature
ne doit y être ajoutée.
