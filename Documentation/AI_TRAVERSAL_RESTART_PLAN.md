# Reconstruction de l'IA de traversée

## Décision

L'ancien agent de traversée est abandonné. Aucun de ses solveurs, routes,
actions, règles de secours ou heuristiques ne doit être réutilisé.

Le contrôleur joueur et les Character Abilities restent la source de vérité du
mouvement. L'IA ne reçoit aucun mouvement privilégié : elle produit les mêmes
inputs qu'un joueur.

## Règles d'architecture

1. Un simulateur de mouvement unique est partagé par le générateur et l'IA.
2. Une trajectoire est simulée avec les paramètres réels des abilities et du motor.
3. Un plan validé est immuable pendant son exécution.
4. L'observation, la planification et l'exécution sont trois systèmes séparés.
5. Aucun correctif spécifique à un obstacle dans le planificateur de saut.
6. Chaque étape possède une scène de test et des tests automatisés avant la suivante.
7. Un échec annule le plan et relance une planification ; il ne modifie jamais la
   trajectoire engagée avec une nouvelle heuristique.

## Phase 0 — Contrat mesurable du personnage

- Extraire depuis `CharacterDefinition` et les abilities : taille, accélérations,
  vitesses, gravité, impulsion et durée de maintien du saut.
- Créer un `CharacterMotionSnapshot` immuable.
- Vérifier par test que la simulation reproduit le motor réel avec une tolérance
  définie sur 30, 60 et 120 images.
- Livrable : courbes position/vitesse superposées simulation contre jeu réel.

Critère de sortie : erreur de position inférieure à 5 cm sur un saut complet.

## Phase 1 — Simulateur de saut pur

- Entrées : état initial, séquence d'inputs, pas temporel, géométrie statique.
- Sorties : échantillons de trajectoire, premier contact, point et vitesse de
  réception, raison d'échec.
- Aucun `MonoBehaviour`, aucune scène et aucune décision IA.
- Tester : saut sans élan, avec élan, court, long, changement de direction aérien.

Critère de sortie : tous les tests déterministes passent et correspondent au motor.

## Phase 2 — Une transition fixe

- Scène dédiée contenant seulement Lennie et deux plateformes fixes.
- Énumérer des candidats composés de : point de départ, instant, durée de maintien,
  direction d'input et point de réception.
- Retenir uniquement les candidats dont tout le collider de Lennie se réceptionne
  dans une zone sûre.
- L'exécuteur rejoint le point de départ, s'immobilise si nécessaire, puis rejoue
  exactement les inputs du candidat sans recalculer celui-ci.

Critère de sortie : 1 000 traversées consécutives sans chute sur un jeu paramétré
de distances et hauteurs.

## Phase 3 — Plateforme mobile unique

- La plateforme expose une fonction de pose future complète : position et vitesse.
- Le simulateur déplace sa géométrie à chaque pas.
- Chaque candidat possède un instant absolu de départ et un point local de réception
  sur la plateforme.
- L'IA se place, attend, puis saute seulement si la simulation complète reste valide
  au moment de l'engagement.
- Afficher dans la scène : trajectoire, instant de départ, point de réception et
  marge de sécurité.

Critère de sortie : 1 000 transferts pour chaque sens et chaque phase du cycle,
sans départ prématuré ni attente infinie.

## Phase 4 — Graphe de surfaces

- Une surface est un support physique identifié, pas un GameObject arbitraire.
- Une arête contient un plan de traversée validé par le simulateur.
- Le générateur construit ses obstacles uniquement à partir d'arêtes validées.
- L'IA parcourt exactement ce même graphe ; aucune seconde règle de faisabilité.

Critère de sortie : toute seed acceptée par le générateur est terminée par l'agent
dans une scène sans ennemis.

## Phase 5 — Séquences temporelles

- Ascenseur vers plateforme fixe.
- Plateforme verticale vers plateforme horizontale.
- Chaînes de plateformes fragiles.
- Le planificateur réserve une fenêtre pour toute la séquence avant de poser le
  premier pied sur une surface temporaire.

Critère de sortie : tests exhaustifs sur les phases initiales des plateformes.

## Phase 6 — Capacités spéciales

- Wall-jump comme primitive simulée par le même moteur.
- Stomp et bounce comme transitions avec état initial et impulsion de sortie connus.
- Accroupissement uniquement si une ability correspondante existe.

Critère de sortie : chaque primitive possède ses propres tests isolés avant son
apparition dans un niveau généré.

## Phase 7 — Ennemis

- Les ennemis sont des obstacles dynamiques avec trajectoire prédite et incertitude.
- Un stomp n'est choisi que si la réception après rebond est elle-même validée.
- Sinon l'agent attend, évite ou saute au-dessus selon les plans disponibles.

Critère de sortie : aucune attaque ne peut être engagée sans plan de sortie sûr.

## Phase 8 — Nouveau menu titre

- Recréer le menu seulement après validation de toutes les phases nécessaires.
- Seed surveillée, watchdog et changement de seed si aucune solution n'existe.
- Aucun Game Over visible : la démonstration se réinitialise hors écran.

## Discipline de développement

- Une branche ou sauvegarde avant chaque phase.
- Aucun fichier de plus de 300 lignes sans justification.
- Un script par fichier et namespaces `GameSystems.Traversal.*`.
- Pas de dépendance du système générique vers `Lennie.*`.
- Journal de test conservant seed, obstacle, plan choisi et raison d'échec.
- Aucun passage à la phase suivante tant que son critère de sortie n'est pas atteint.

