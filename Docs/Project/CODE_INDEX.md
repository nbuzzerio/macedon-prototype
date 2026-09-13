# Code Index

## Combat

### Health.cs
Shared health component used by player and enemies. Tracks current/max health, invokes death events, handles death behavior.

### PlayerCombat.cs
Handles player punch input, punch animation trigger, movement lock during punch, and punch hitbox timing.

### PunchHitbox.cs
Trigger collider damage dealer. Damages objects with `Health`.

### WolfChase.cs
Wolf enemy AI. Handles roaming, detection, howl, chase, attack, leash return, damage reaction, and death animation trigger.

### DeathFall.cs
Simple placeholder death fall animation helper.

### UIHealthColors.cs
Shared helper for health text color thresholds.

## Player

### CombatMovementController.cs
Custom player movement controller. Handles mouse turning, forward movement, strafing, diagonal movement, backpedal, sprint, jump, animator parameters, and the global steep-slope traversal rule.

### SteepSlopeRules.cs
Deterministic player-traversal math for slope classification, jump authorization, uphill-input suppression, and downhill direction. `CombatMovementController` applies it from a CharacterController-sized ground probe and recent collision normals.

## Character Visual Tooling

### CharacterVisualVariantsWindow.cs / VarangianVisualVariantLogic.cs
Editor-only `Tools > MACEDON > Character Visual Variants` workflow for the current single-material Varangian asset. It previews independent, deterministic curated recolors of red-dominant fabric and blue paint within the known round-shield UV island, then creates one private texture/material pair and assigns it to the explicitly selected character with Undo. Both channels are prototype-oriented color-threshold masks over one atlas, not semantic character customization. Source materials/textures are never edited.

## Dialogue / Quest

### NPCDialogue.cs
Trigger-based villager interaction and dialogue UI toggling. With no profile it preserves the original Wolf quest-giver flow; with a `VillagerProfile` it selects deterministic cycling dialogue from Ambient or RecruitReady pools based on Wolf encounter completion and exposes a scene-authored Home anchor.

### VillagerProfile.cs / VillagerDialogueLogic.cs
Reusable recruitment dialogue data and deterministic selection logic. Three profile assets independently author Ambient, RecruitReady, and Following lines without bespoke scripts.

### VillagerRecruitmentState.cs / VillagerParty.cs / VillagerPartyRegistry.cs
Recruitment state is separate from dialogue/world state. The small party coordinator holds the explicit Player Transform, registers followers idempotently, assigns stable reusable slot indices, and supports removal for the later abandonment pass.

### VillagerFollower.cs / VillagerFormationLogic.cs
NavMeshAgent-based following for recruited villagers. Distinct local formation offsets rotate with Player yaw; destination updates are throttled, sampled onto compatible NavMesh, and never issued while an agent is off-mesh. No combat, return-home, or route-deviation behavior is included.

### NpcLocomotionAnimator.cs / NpcLocomotionAnimationLogic.cs
Reusable humanoid NPC presentation driver. It maps horizontal NavMeshAgent velocity to the existing `Speed` and `MotionSpeed` parameters, maintains grounded locomotion without faking jumps, disables root motion, and can assign the existing Starter Assets controller when a child visual Animator has none. It always binds the NavMeshAgent on its own root and rejects Animator references outside that root's hierarchy, auto-selecting the local Animator when exactly one exists. Jump/traversal and attack triggering remain future gameplay responsibilities.

### NpcAnimationEventReceiver.cs
No-op NPC-side receiver for the `OnFootstep(AnimationEvent)` events embedded in the shared Starter Assets walk/run clips. It belongs beside the visual Animator, prevents missing-receiver errors, and leaves a focused future hook for NPC footstep effects without adding Player-specific components.

### WolfQuest.cs
Starts the wolf quest and activates the wolf encounter.

## Encounters

### WolfEncounterState.cs / WolfEncounterController.cs
Own the idempotent `NotStarted → Active → Completed` Wolf encounter lifecycle. The controller activates temporary boundary collision and exposes Unity/C# events so quest logic and presentation can respond without `Health` knowing about them.

### EncounterStartTrigger.cs / EncounterBoundary.cs
Optionally commits a tagged player on volume entry and enables only the explicitly assigned encounter blockers while the fight is active.

### LocalCameraShake.cs
Reusable local positional camera shake with a decaying envelope. It removes its previous offset before applying the next one and restores the target when finished or disabled.

### WolfEncounterCinematic.cs / WolfCinematicSequenceState.cs
Small Wolf-death presentation sequence. It locally takes player camera/movement input, frames the animated Wolf death, triggers impact shake, turns toward an authored tree focus, starts the tree/exit reveal, releases deferred encounter collision, and returns camera ownership without snapping. The timeline is deterministic and presentation-only.

### EncounterTreeFallResponder.cs / EncounterExitResponder.cs
Inspector-configured, one-shot completion presentation: stagger selected scene-tree rotations and enable/disable route objects or collision. These do not alter Terrain-painted tree data.

## UI / Game

### PlayerHealthUI.cs
Displays player health and health color.

### WolfHealthUI.cs
Displays wolf boss health after aggro and fades after death.

### GameStateUI.cs
Controls death screen, victory screen, cursor unlock, time pause, and restart scene reload.
