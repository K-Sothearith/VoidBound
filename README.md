# VOIDBOUND

*A first-person parkour journey through floating ruins in a night-sky void.*

> **Development status:** In progress — Phases 01 and 02 implemented. The complete seven-section course and standalone build are not yet available.

## Overview

Voidbound is a single-player, first-person 3D obstacle-course game made in Unity for the Cambodia Academy of Digital Technology (CADT) Game Development mid-term group project. Cross suspended stone platforms, survive moving obstacles, solve physics-based challenges, and eventually reach the final portal.

The visual direction is a minimalist, low-poly world of floating stone structures, blue-violet lighting, and a deep night-space backdrop. The visual presentation is still being developed.

## Objective and rules

Reach the final extraction portal by completing seven sections in sequence. The game starts with **three lives**. Falling into the void or touching a lethal obstacle costs one life. If lives remain, the player respawns at the latest activated checkpoint; after the third death, the game ends and can be restarted.

**Checkpoint locations:**
- **Initial spawn:** Beginning of Section 1; used until the first checkpoint is reached.
- **Checkpoint 1:** At the beginning of Section 3.
- **Checkpoint 2:** At the beginning of Section 6.

There are no other planned checkpoints. The elapsed-time HUD tracks the run; the game does not currently require a countdown to win.

**Hammer behavior (revised after Phase 02):** The three swinging hammers knock the player around rather than guaranteeing immediate death on contact. A skilled player may recover and land back on the walkway. Falling into the void still causes death. Spikes and active lasers are planned as instant-death hazards.

## Controls

| Input | Action |
|---|---|
| `W`, `A`, `S`, `D` | Move |
| Mouse | Look around |
| `Space` | Jump |
| `Left Shift` | Sprint |
| `E` | Interact with nearby objects |
| `Esc` | Pause / resume |
| On-screen buttons | Start, resume, restart (as available) |

## Level design

| Section | Challenge | Status |
|---|---|---|
| 1. Movement Tutorial | Learn movement and jumping; interact with a test terminal | Implemented |
| 2. Hammer Swing | Cross three swinging hammers with safe resting areas | Implemented; manually traversed |
| 3. Moving Platform | Stand on a pressure plate to summon a platform; release it and jump aboard as it returns | Planned |
| 4. Spike Path | Navigate spike formations on a narrow bridge | Planned |
| 5. Laser Corridor | Time movement around active and inactive lasers | Planned |
| 6. Physics Chamber | Push two crates from opposite sides onto two plates to open a door | Planned |
| 7. Final Gauntlet | Combine learned obstacles and reach the goal portal | Planned |

The prototype finish marker at the end of the tutorial is **not** the final victory condition.

## Current implementation

**Phase 01 — Foundation:** Main `Voidbound` scene; first-person `CharacterController`; Unity Input System movement; start screen with blurred background; crosshair, lives, elapsed timer and section label; interaction terminal; pause and game-over screens; fall detection and respawning. Antigravity's Phase 01 report records 29 passing automated assertions and no compilation errors.

**Phase 02 — Swinging Hammers:** Three swinging hammer obstacles with different phase offsets, two rest areas, section trigger, reusable hazard code, and physics components. Antigravity's original Phase 02 report records 30 passing automated assertions and no compilation errors. **Those results predate a manual remake of the hammer behavior**, so they should not be treated as verification of the current modified collision behavior. The updated section has been manually confirmed traversable.

Future work includes the two checkpoints, remaining obstacles, full victory flow, atmospheric visual polish, regression testing, and a standalone build.

## Requirements and setup

- **Unity Editor:** `6000.3.8f1`
- **Render pipeline:** Universal Render Pipeline (URP), package `17.3.0`
- **Input:** Unity Input System, package `1.18.0`
- **Primary development platform:** macOS
- **Target standalone build:** macOS or Windows (to be confirmed and tested)

### Open and play in Unity

1. Clone the repository:
   ```bash
   git clone <REPOSITORY_URL>
   cd <REPOSITORY_FOLDER>
   ```
2. Open **Unity Hub** and select **Add project from disk**, pointing it to the cloned project folder.
3. Use Unity Editor **6000.3.8f1** and allow the project packages and assets to import.
4. Open `Assets/Scenes/Voidbound.unity` if it is not already open.
5. Press **Play** in the Unity Editor.
6. Click **Initiate Expedition** on the start screen, then use the controls above.

The standalone build is not yet published. When available, its ZIP must include all files required to launch the game, not just the executable.

## Repository structure

```text
Assets/
  Scenes/                 # Main Voidbound scene
  Scripts/
    Core/                 # Game state, checkpoints, section triggers, kill volumes
    Player/               # Movement, interaction, death handling
    Interactions/         # Interactable objects and tutorial marker
    Obstacles/            # Hammer and hazard behavior
    UI/                   # Menu and HUD
    Editor/               # Scene-building tools and tests
  Materials/
  Settings/               # URP post-processing assets
Packages/                 # Package manifest and lockfile
ProjectSettings/          # Unity project settings
```

Keep Unity `.meta` files in Git. Do not commit generated `Library/`, `Temp/`, or `Obj/` folders.

## Team and contributions

> Complete before submission. Every member should be able to explain their work and have meaningful Git history.

| Team member | Contribution |
|---|---|
| [Member 1] | [Specific work and commits] |
| [Member 2] | [Specific work and commits] |
| [Member 3] | [Specific work and commits] |
| [Member 4, if applicable] | [Specific work and commits] |

## Credits and development assistance

- **Engine and packages:** Unity 6, Universal Render Pipeline, Unity Input System.
- **External assets, music, fonts, tutorials:** [List names, creators, licenses and links for anything used; write “None” if applicable.]
- **AI assistance:** ChatGPT assisted with game planning, specification and documentation. Antigravity assisted with Unity implementation and automated validation. Team members are responsible for reviewing, testing and understanding submitted work. [Expand or adjust to reflect actual use and course academic-integrity rules.]

## Submission links

| Deliverable | Link / status |
|---|---|
| Source repository | [Add GitHub repository URL] |
| Gameplay recording (MP4) | [Add publicly accessible YouTube or Google Drive URL] |
| Standalone build (ZIP) | [Add publicly accessible Google Drive or GitHub Releases URL] |

**Submission checklist:** Verify that the Unity project opens, the standalone build runs outside the Editor, the gameplay video shows controls/obstacles/physics/success or failure/restart, all links are accessible without permission requests, and all team contributions are credited.

---

*CADT Game Development — Mid-Term Group Project: Playable Obstacle Course.*
