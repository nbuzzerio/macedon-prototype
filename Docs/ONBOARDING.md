> Version: Week 2

> Last Updated: 2026-06-30

# ONBOARDING

## Project Status

Project Phase:
Prototype

Current Learning Phase:
Environment Art & Level Building

Current Priority:
Learning professional Unity workflows over rapidly adding gameplay systems.

## Project Overview

MACEDON is a Unity 6 indie RPG inspired by the atmosphere and exploration of games like Skyrim, Valheim, RuneScape (2004), Kingdom Come: Deliverance, and classic CRPGs. The project is being developed as both a playable game and a structured learning journey through professional game development.

The primary goal is **not** to finish the game as quickly as possible.

The primary goal is to learn the professional workflows, tools, and architecture used by experienced Unity developers. The game naturally grows as each new workflow is learned.

---

## Teaching Philosophy

Every lesson should answer three questions:

1. What are we doing?
2. Why does every Unity game need this?
3. How would a professional studio do it?

Assume I know basic Unity navigation but I am still learning game development.

Never assume I know where code belongs. Always specify:

- which file to create or edit
- where code should be pasted
- what Inspector changes are required

When introducing acronyms (IK, PBR, UV, LOD, etc.), always spell out the full term the first time.

Use concise bullet points for normal instructions so they work well with text-to-speech.

Only use code blocks when I actually need to copy code.

---

## Current Milestone

End of Week 2 (v2)

Playable vertical slice complete.

Current gameplay loop:

Village

↓

Talk to Villager

↓

Wolf Quest Starts

↓

Wolf Spawns

↓

Fight

↓

Player Dies OR Wolf Dies

↓

Restart Demo

---

## Current Systems

Player

- Custom movement controller
- Sprint
- Strafing
- Diagonal movement
- Slower backpedal
- Jump
- Punch combat
- Health
- Death
- Varangian player model replacing Starter Assets mannequin

Enemy

- Animated wolf
- Roaming AI
- Detection
- Howl
- Chase
- Territory leash
- Return home
- Damage reaction
- Death

Quest

- Villager interaction
- Wolf activation

UI

- Player health
- Wolf health
- Death screen
- Victory screen
- Restart flow

---

## Current Architecture

Gameplay and visuals are intentionally separated.

The Player GameObject contains gameplay systems.

Character models are replaceable visuals.

Current managers:

- GameManager
- HUDCanvas

Current major gameplay scripts include:

- CombatMovementController
- PlayerCombat
- Health
- WolfChase
- WolfQuest
- GameStateUI

See `Project/CODE_INDEX.md` for details.

---

## Current Folder Conventions

Gameplay scripts are organized by responsibility.

Custom assets are separated from downloaded assets whenever practical.

Prefabs should generally be created from imported source assets rather than editing imported FBX files directly.

---

## Learning Philosophy

Beginning with Week 3, the curriculum shifts from implementing gameplay features to learning professional Unity workflows.

Gameplay features should primarily serve as exercises that reinforce those workflows.

Current priority:

Environment Art & Level Building.

Topics include:

- Modular environments
- Materials
- Texture workflow
- UVs
- Terrain texturing
- Prefabs
- Asset organization
- Lighting
- World composition
- Professional environment workflows

---

## Known Prototype Limitations

Current combat timing is prototype quality.

Player animations are placeholders.

Wolf AI contains acceptable prototype edge cases around leash resets.

The focus remains learning and iteration rather than polish.

---

## First Documents to Read

1. README.md
2. Project/PROJECT_STATE.md
3. Project/CODE_INDEX.md
4. Project/ROADMAP.md

These documents contain the detailed implementation state of the project.
