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
Custom player movement controller. Handles mouse turning, forward movement, strafing, diagonal movement, backpedal, sprint, jump, and animator parameters.

## Dialogue / Quest

### NPCDialogue.cs
Trigger-based villager interaction and dialogue UI toggling.

### WolfQuest.cs
Starts the wolf quest and activates the wolf encounter.

## UI / Game

### PlayerHealthUI.cs
Displays player health and health color.

### WolfHealthUI.cs
Displays wolf boss health after aggro and fades after death.

### GameStateUI.cs
Controls death screen, victory screen, cursor unlock, time pause, and restart scene reload.