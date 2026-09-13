# MACEDON Gameplay Roadmap

This is a living, outcome-oriented roadmap. Difficulty is a rough implementation estimate: **1** tiny, **2** small, **3** medium, **4** substantial, **5** major or multi-session. Status describes the repository today, not aspiration.

## Top Milestone: First External-Playtest Vertical Slice

```text
Village → Wolf encounter → Recruit 3 allies → Travel to / assault raider fort
→ Group fight → Boss courtyard cutscene → Boss fight → Return to village → Quest complete
```

The immediate product goal is a coherent itch.io playtest of this complete loop. Prefer work that makes this route playable, readable, and satisfying end-to-end. Target weekly-ish playable releases, with roughly two-week flexibility during tooling-heavy periods.

## DONE

| Item | Difficulty | Current result |
| --- | ---: | --- |
| Player movement/combat prototype | 3 | Custom movement, mouse turning, strafe/backpedal/sprint/jump, punch, health, and death flow exist. |
| Current vertical camera look | 2 | Custom clamped vertical pitch is implemented on the player camera transform. |
| Current Wolf encounter | 3 | Inactive-until-quest Wolf, roam/detection/howl/chase/attack/leash, health UI, damage response, death, and victory flow exist. |
| Current river and terrain playable space | 4 | Sculpted 1000 m terrain and spline river prototype provide the current village, encounter, valley, and mountain play space. |
| Current navigation workflow | 3 | Persistent NavMesh rebuild and selected-agent validation tooling exists for terrain/world changes. |
| Terrain/world production progress | 4 | Riverbed/banks, rolling terrain, Wolf crater, village valley, and fjord-like ranges are established enough for continued traversal and encounter work. |

## CURRENT VERTICAL SLICE

| Item | Difficulty | Definition of done / note |
| --- | ---: | --- |
| Reduce fog for terrain visibility | 1 | Tune the active scene fog so terrain forms and intended travel route read during the external build. |
| Anti-Spider-Man traversal | 3 | Global surface-normal rule is implemented: severe contacts deny/cancel uphill jumps, suppress uphill input, and add a modest downhill bias. Manual terrain tuning/QA remains. This stays separate from encounter-only crater containment and will eventually be movement-authoritative in multiplayer. |
| Wolf encounter containment / crater cage | 2 | Reusable boundary/state components are implemented; visually place and wire only the required temporary crater blockers, then verify they release on completion. |
| Wolf death camera shake | 2 | Reusable local positional shake is implemented; wire and tune the player-camera shake pivot for a heavy landing. Camera remains local-only in multiplayer. |
| Falling-tree sequence | 2 | Configurable staggered tree responders are implemented; replace only chosen painted trees with scene objects if necessary, establish sensible pivots, and art-direct the fall. |
| Reveal/open route after Wolf death | 2 | Configurable activation/collision responder is implemented; choose the exact blocker/route objects and visually validate the reveal. |
| Wolf completion/state signaling | 2 | Idempotent Not Started → Active → Completed state and events are implemented; finish scene wiring and connect downstream quest/UI behavior. Encounter state and Wolf death will require authority/synchronization later. |
| Recruit 3 allies | 4 | Make three recruitment beats legible, persist them through the slice, and add party feedback. Recruitment and quest state must become authoritative/synchronized. |
| Ally AI | 4 | Allies follow, navigate, choose hostile targets, fight, recover, and avoid obvious obstruction failures. AI decisions/state will need synchronization. |
| NPC visual variation | 2 | Produce readable prototype variation without breaking rigs, combat identity, or dialogue roles. |
| Bandit enemy AI | 3 | Use/evolve current humanoid NavMesh combat behavior into reliable fort defenders. |
| Multi-enemy and faction combat | 4 | Player/allies/bandits select legal targets and resolve group fights through explicit combat sides. Combat state and faction membership are multiplayer-sensitive. |
| Everyone can punch for now | 2 | Give all required slice participants a functional, consistently signaled placeholder melee attack. |
| Boss courtyard in-engine cutscene | 4 | Stage an interruptible/recoverable arrival beat using in-engine actors and local camera presentation. Shared cutscene state needs authority; camera execution stays local. |
| Boss encounter | 4 | Deliver a readable boss loop, failure/retry behavior, and completion signal. |
| Return-to-village quest completion | 3 | Recognize return after boss defeat, conclude dialogue/state, and clearly communicate completion. Quest state must eventually synchronize. |
| First external itch.io playtest build | 3 | Ship the entire route with start/end, basic onboarding, restart/recovery, acceptable performance, and collected feedback. |

## NEAR TERM

| Item | Difficulty | Direction |
| --- | ---: | --- |
| Improved attack animations | 3 | Replace placeholder punches where they most improve readability. |
| Hit reactions and combat feel | 3 | Add impact timing, feedback, sound, recovery, and restrained local camera effects. |
| Water visual flow | 3 | Make direction and speed readable without depending on physics. |
| River current pushes characters downstream | 4 | Add testable volume/flow behavior; character movement effects will require authoritative multiplayer handling. |
| Broader ally/enemy AI evolution | 4 | Improve formations, threat choice, navigation recovery, encounter roles, and debugging after slice feedback. |
| Multiplayer-safe seams | 4 | Separate authority from presentation for movement, combat, quest/encounter progression, and AI/factions before those systems harden. Do not mistake this for implemented multiplayer. |

## LATER

| Item | Difficulty | Direction |
| --- | ---: | --- |
| Playable multiplayer slice | 5 | The game must support playing with friends. Establish host/server authority, replication, join/leave handling, and a tested cooperative slice. |
| Expanded quests and content | 5 | Add locations, encounter varieties, allies, enemies, and quest chains after the Raider quest proves the core loop. |
| Advanced cooperative combat/AI | 5 | Evolve targeting, revival/failure rules, coordination, and scalable encounter behavior around real multiplayer tests. |

## Multiplayer-Sensitive Contract

Networking is not implemented. Future multiplayer work must make movement authority, damage/health/death, quest stage, encounter active/completed state, boundary release, Wolf death, tree-fall trigger, and AI/faction state authoritative and synchronized. Camera shake, camera framing, screen UI, and other presentation remain local to each player; replicated gameplay events may trigger those local effects.
