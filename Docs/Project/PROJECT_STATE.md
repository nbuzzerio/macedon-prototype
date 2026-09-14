# Project State

## Current Milestone

End of Week 2 / Week 2 v2.

MACEDON now has a small playable vertical slice with a beginning, middle, and end.

## Player

- Custom combat movement controller
- Mouse turning
- Forward movement
- Strafing
- Diagonal movement
- Slower backpedal
- Sprint
- Jump
- Punch attack
- Health
- Death handling
- Varangian / Shieldbearer visual replacing Starter Assets mannequin

## Combat

- Shared `Health` system
- Punch hitbox
- Wolf damage reactions
- Player death
- Wolf death
- Death/victory UI flow

## Wolf Encounter

- Wolf starts inactive
- Villager dialogue starts quest
- Wolf activates in clearing
- Wolf roams around home point
- Wolf detects player
- Wolf faces player and howls
- Wolf chases player
- Wolf attacks in range
- Wolf leashes back home if pulled too far
- Wolf resumes roaming after returning home
- Boss health UI appears on aggro
- Wolf defeat triggers victory screen
- Reusable Wolf encounter state, temporary-boundary control, local camera shake, selected-tree fall, and exit reveal components are implemented in code
- Scene-specific crater blockers, tree GameObjects/pivots, camera target, and completion-event references still require manual Unity placement and tuning
- Wolf-death cinematic orchestration is implemented in code with Inspector-authored Wolf/tree focus points; final camera references, event rewiring, stagger tuning, and Play Mode framing remain manual

## Villagers / Recruitment

- Shared `NPCDialogue` interaction now supports optional data-driven villager profiles while preserving the original Wolf quest-giver behavior.
- Three prototype profiles provide distinct ambient and post-Wolf RecruitReady dialogue pools.
- Recruitable villagers reference the encounter completion state rather than Wolf health and expose an authored Home Transform for later movement behavior.
- RecruitReady interaction now recruits each villager independently and idempotently into a small shared party registry.
- Up to three villagers follow simultaneously in stable left-rear, right-rear, and far-rear slots using throttled, sampled NavMesh destinations.
- Following and rejoin dialogue remain profile-authored. A separate route-deviation component supports queued warning lines, independent abandonment, NavMesh return to the authored Home, and re-recruitment.
- A reusable NPC locomotion driver maps each follower's NavMeshAgent velocity into the existing Starter Assets idle/walk/run Animator parameters; the agent remains authoritative and root motion stays disabled.
- A reusable authored traversal coordinator can suspend formation ownership and move followers one-by-one over explicit bidirectional jump points with deterministic arcs and safe NavMesh restoration. The river route and triggers require manual scene wiring and QA.
- Authored raid-route corridor volumes are implemented in code but require generous manual placement from village through river/Wolf space to the fort. Ally combat remains pending.

## UI

- Player health text
- Wolf health text
- Death panel
- Victory panel
- Restart button reloads scene

## Current Build

Week 2 v2 WebGL build created for itch.io.
