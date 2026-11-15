# VOIDBOUND

*A first-person parkour journey through floating ruins in a night-sky void.*

> **Development status:** In progress — Phases 01, 02, 03, 04, and 05 implemented. The complete seven-section course and standalone build are not yet available.

## Overview

Voidbound is a single-player, first-person 3D obstacle-course game made in Unity for the Cambodia Academy of Digital Technology (CADT) Game Development mid-term group project. Cross suspended stone platforms, survive moving obstacles, solve physics-based challenges, and eventually reach the final portal.

The visual direction is a minimalist, low-poly world of floating stone structures, blue-violet lighting, and a deep night-space backdrop. The visual presentation is still being developed.

## Objective and rules

Reach the final extraction portal by completing seven sections in sequence. The game starts with **three lives**. Falling into the void or touching a lethal obstacle costs one life. If lives remain, the player respawns at the latest activated checkpoint; after the third death, the game ends and can be restarted.

**Checkpoint locations:**
- **Initial spawn:** Beginning of Section 1; used until the first checkpoint is reached.
- **Checkpoint 1:** At the beginning of Section 4 (relocated from Section 3 per Phase 05 specification).
- **Checkpoint 2:** At the beginning of Section 6.

There are no other planned checkpoints (exactly two checkpoints total). The elapsed-time HUD tracks the run; the game does not currently require a countdown to win.

**Hammer behavior (revised after Phase 02):** The three swinging hammers knock the player around rather than guaranteeing immediate death on contact. A skilled player may recover and land back on the walkway. Falling into the void still causes death. Spikes and active lasers are planned as instant-death hazards.

## Controls

| Input | Action |
|---|---|
| `W`, `A`, `S`, `D` | Move |
| Mouse | Look around |
| `Space` | Jump |
| `Left Shift` | Sprint |
| `E` | Interact with nearby objects / Reset crates |
| `Esc` | Pause / resume |
| On-screen buttons | Start, resume, restart (as available) |

## Level design

| Section | Challenge | Status |
|---|---|---|
| 1. Movement Tutorial | Learn movement and jumping; interact with a test terminal | Implemented |
| 2. Hammer Swing | Cross three swinging hammers with safe resting areas | Implemented; manually traversed |
| 3. Moving Platform | Stand on a pressure plate to summon a platform; release it and jump aboard as it returns | Implemented |
| 4. Spike Path | Navigate spike formations on a narrow bridge | Implemented |
| 5. Laser Corridor | Time movement around active and inactive lasers | Implemented |
| 6. Physics Chamber | Push two crates from opposite sides onto two plates to open a door | Implemented |
| 7. Final Gauntlet | Combine learned obstacles and reach the goal portal | Planned |

The temporary prototype finish marker from Phase 01 has been removed from the playable scene in Phase 03 cleanup; victory will be awarded by the extraction portal at the end of Section 7.

## Current implementation

**Phase 01 — Foundation:** Main `Voidbound` scene; first-person `CharacterController`; Unity Input System movement; start screen with blurred background; crosshair, lives, elapsed timer and section label; interaction terminal; pause and game-over screens; fall detection and respawning. Antigravity's Phase 01 report records 29 passing automated assertions and no compilation errors.

**Phase 02 — Swinging Hammers:** Three swinging hammer obstacles with different phase offsets, two rest areas, section trigger, reusable hazard code, and physics components. Antigravity's original Phase 02 report records 30 passing automated assertions and no compilation errors. **Those results predate a manual remake of the hammer behavior**, so they should not be treated as verification of the current modified collision behavior. The updated section has been manually confirmed traversable.

**Phase 03 — Pressure-Plate Moving Platform & Checkpoint 1:** Section 3 departure and arrival platforms; Checkpoint 1 initially at Section 3 entrance with visual indicator light/emission and respawn point; depressible pressure plate situated close to the void ledge; kinematic moving platform traversing a 20m lethal void gap at 6.5 m/s with return delay; reliable zero-slip CharacterController player transport via unified single-move physics; two-tier gravity architecture (normal platform jump gravity -20 m/s² with 1.8m jump apex, accelerated -45 m/s² void plunge gravity below platform level); eliminated mid-air jump spam exploit; restored on-screen notification text exclusively for checkpoint activation while keeping general event reporting banners silenced; 89 passing automated assertions with zero compilation errors and full Phase 01/02 regression pass (161 total passing assertions). Scene cleanup performed: Terminal Alpha reoriented to face player spawn, temporary Phase 01 finish marker removed from scene, and Checkpoint 1 positioned inward for prominence without blocking player path.

**Phase 04 — Spike Path & Laser Corridor:** Section 4 narrow stone bridge over lethal void with single edge spikes, a 6-spike hurdle cluster jumping challenge, and three staggered offset precision barriers with safe corridors; Section 5 corridor featuring three laser challenges: Challenge 1 single pulsing laser with amber warning telegraphs; Challenge 2 continuous vertical moving lasers oscillating between 0.4m (jumpable) and 2.4m (runnable underneath) out of phase; Challenge 3 dual circular floating platforms over the void with central rotating dual-beam laser turrets (Platform 1 rotating clockwise at +35°/s, Platform 2 rotating counter-clockwise at -48°/s); immediate lethal contact detection on all lasers with single-death debounce guards; deaths in Section 4 and 5 reliably respawn at Checkpoint 1; expanded lethal void kill volume across all sections; 65 passing automated assertions with zero compilation errors and full Phase 01/02/03 regression pass (226 total passing assertions across the project).

**Phase 05 — Physics Chamber & Checkpoint 2:** Relocated Checkpoint 1 from Section 3 entrance to Section 4 entrance (approximately Z=107.5m) to maintain exactly two checkpoints total; placed Checkpoint 2 at Section 6 entrance (Z=228.0m); implemented Section 6 Physics Chamber featuring two pushable dynamic Rigidbody crates (tuned at 25kg mass, linear damping 4.0, angular damping 5.0, rotation X/Z constraints to eliminate tumbling), two crate-detecting pressure plates with depression animation and emissive feedback, and a locked sci-fi exit portal featuring dual sliding blast doors that unlock only when both plates are simultaneously active; integrated lost crate anti-softlock systems via automatic void fall recovery and an interactive `CrateResetTerminal` (E-key interactable); enhanced `PlayerController` to push dynamic physics objects via `OnControllerColliderHit`; extended kill volume pit coverage through Section 6 (length 550m); 65 automated test assertions passing in `VoidboundPhase05Tests.cs` and 65 regression test assertions passing in `VoidboundPhase04Tests.cs` with zero compilation errors.

Future work includes Section 7 (Final Gauntlet), extraction goal portal, full victory flow, atmospheric visual polish, regression testing, and a standalone build.

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
