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

## Dialogue / Quest

### NPCDialogue.cs
Trigger-based villager interaction and dialogue UI toggling.

### WolfQuest.cs
Starts the wolf quest and activates the wolf encounter.

## Encounters

### WolfEncounterState.cs / WolfEncounterController.cs
Own the idempotent `NotStarted → Active → Completed` Wolf encounter lifecycle. The controller activates temporary boundary collision and exposes Unity/C# events so quest logic and presentation can respond without `Health` knowing about them.

### EncounterStartTrigger.cs / EncounterBoundary.cs
Optionally commits a tagged player on volume entry and enables only the explicitly assigned encounter blockers while the fight is active.

### LocalCameraShake.cs
Reusable local positional camera shake with a decaying envelope. It removes its previous offset before applying the next one and restores the target when finished or disabled.

### EncounterTreeFallResponder.cs / EncounterExitResponder.cs
Inspector-configured, one-shot completion presentation: stagger selected scene-tree rotations and enable/disable route objects or collision. These do not alter Terrain-painted tree data.

## UI / Game

### PlayerHealthUI.cs
Displays player health and health color.

### WolfHealthUI.cs
Displays wolf boss health after aggro and fades after death.

### GameStateUI.cs
Controls death screen, victory screen, cursor unlock, time pause, and restart scene reload.
