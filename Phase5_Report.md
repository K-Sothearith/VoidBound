# Voidbound — Phase 05 Implementation Report

**Project:** Voidbound  
**Phase:** 05 — Section 6 (Physics Chamber & Checkpoint 2) + Checkpoint 1 Relocation  
**Target Engine:** Unity 6000.3.8f1 (Universal Render Pipeline 17.3.0, Unity Input System 1.18.0)  
**Git Branch:** `section4-5`  
**Working Directory:** `/Users/kongsothearith/Projects/VoidBound`  
**Date:** 2026-11-15  

---

## 1. Executive Summary & Objective

Phase 05 of the Voidbound project introduces **Section 6 (Physics Chamber & Checkpoint 2)** and executes **Part A (Checkpoint 1 Relocation)**, extending the floating void obstacle course from the Section 5 arrival portal arch ($Z = 226.5\,\text{m}$) through to Section 6's exit platform ($Z = 274.0\,\text{m}$).

Key objectives accomplished in Phase 05:
1. **Part A — Checkpoint 1 Relocation:** Relocated Checkpoint 1 from the Section 3 entrance ($Z = 65.0\,\text{m}$) to the Section 4 entrance ($Z = 107.5\,\text{m}$) on the departure apron `Platform_07_Section4_Apron`. Checkpoint 1 was removed from Section 3, establishing the required design of **exactly two checkpoints total** throughout the entire game: Checkpoint 1 at Section 4 entrance and Checkpoint 2 at Section 6 entrance.
2. **Part B — Section 6 Physics Chamber ($Z = 227.0\,\text{m} \rightarrow 274.0\,\text{m}$):**
   - **Checkpoint 2:** Placed at the entrance of Section 6 ($Z = 228.0\,\text{m}$) on `Platform_15_Section6_Entrance` with a complete visual monument, glowing diamond indicator, point light, and automatic trigger activation.
   - **Pushable Dynamic Rigidbody Crates:** Implemented `PushableCrate.cs` attached to two dynamic Rigidbody crates (`Crate_Left` and `Crate_Right`) starting on opposite sides of the chamber ($X = -4.5\,\text{m}$ and $X = +4.5\,\text{m}$, $Z = 239.0\,\text{m}$). Tuned physics with $25.0\,\text{kg}$ mass, linear damping $4.0$, angular damping $5.0$, `Continuous` collision detection, and constrained `FreezeRotationX | FreezeRotationZ` to ensure responsive, reliable sliding without tumbling or flipping.
   - **Crate Pressure Plates:** Implemented `CratePressurePlate.cs` on two plates (`Plate_Left` and `Plate_Right` at $Z = 255.0\,\text{m}$) featuring base housings, depressible pads ($0.08\,\text{m}$ depression depth), crate filtering, and emissive color transitions.
   - **Locked Exit Door:** Implemented `PuzzleDoor.cs` controlling dual sliding blast doors at $Z = 263.0\,\text{m}$. The door is physically blocked by colliders and marked by a red status beacon while locked; it unlocks and opens smoothly **only when both plates are simultaneously active**.
   - **Lost Crate Recovery & Anti-Softlock:** Implemented multi-tier crate recovery:
     - Automatic void fall detection in `PushableCrate.FixedUpdate` ($Y < -5.0\,\text{m}$ or `KillVolume` trigger contact) instantly returning lost crates to origin with zeroed velocity.
     - Interactive `CrateResetTerminal.cs` situated near the chamber entrance ($Z = 235.0\,\text{m}$), implementing `IInteractable` to provide an on-screen prompt (`Press E to Reset Puzzle Crates`) for manual puzzle resets.
     - Automatic puzzle reset on player death/respawn and game restart.
   - **Player Physics Interaction:** Extended `PlayerController.OnControllerColliderHit` to apply controlled horizontal forces to `PushableCrate` objects and dynamic Rigidbodies, allowing smooth, continuous pushing.
3. **Part C — Integration & Verification:**
   - Preserved Sections 1 through 5 intact without regressions.
   - Extended `KillVolume_Pit` to length $550.0\,\text{m}$ ($Z \in [-125\,\text{m}, +425\,\text{m}]$), fully covering the void under Section 6.
   - Built a comprehensive automated test suite in `VoidboundPhase05Tests.cs` covering 65 assertions: **65 PASSED, 0 FAILED**.
   - Validated Phase 04 regression suite in `VoidboundPhase04Tests.cs` with updated Checkpoint 1 location: **65 PASSED, 0 FAILED** (total 130 passing assertions in the regression cycle).

---

## 2. Architecture & Design Principles

### 2.1 Reusing Existing Systems
In adherence to the core requirement to reuse existing architecture rather than introducing duplicate managers:
- **Game State & Checkpoint Management:** Utilized `GameManager.Instance.SetCheckpoint(position, rotation, name)` and `GameManager.Instance.OnPlayerDied()` directly. No secondary checkpoint or game manager was created.
- **Player Interaction System:** `CrateResetTerminal` implements `IInteractable`, hooking directly into `PlayerInteractor.cs` and the existing HUD prompt pipeline without modifying UI controllers.
- **Notification Pipeline:** Checkpoint activations and crate recovery events route through `GameManager.Instance.ShowNotification()`, which triggers the on-screen checkpoint banners.
- **Lifecycle Events:** Crates, plates, and the puzzle door subscribe directly to `GameManager.Instance.OnGameReset` and `GameManager.Instance.OnRespawnRequested`.

### 2.2 Idempotent Procedural Generation
The procedural scene builder method `VoidboundSceneBuilder.BuildPhase05Scene()`:
1. Inspects the existing `Voidbound.unity` scene hierarchy.
2. Destroys any legacy `Checkpoint_01` under `Section_03_MovingPlatform`.
3. Ensures `Checkpoint_01` is properly rooted under `Section_04_SpikePath` at $Z = 107.5\,\text{m}$.
4. Removes any prior `Section_06_PhysicsChamber` to prevent duplicate colliders or stale scripts.
5. Builds Section 6 fresh, expands the kill volume, and saves the scene.

---

## 3. Part A — Checkpoint Relocation

### 3.1 Relocation Specifications
- **Previous Location:** Section 3 Entrance ($Z = 65.0\,\text{m}$) on `Platform_05_Section3_Departure`.
- **New Location:** Section 4 Entrance ($Z = 107.5\,\text{m}$) on `Platform_07_Section4_Apron`.
- **Monument Placement:** $X = -1.8\,\text{m}$, $Z = 108.0\,\text{m}$ (inward on the departure apron, clearly visible while keeping the central $1.8\,\text{m}$ walking path completely unobstructed).
- **Spawn Point:** $(0.0, 0.1, 107.5)$, rotation `Quaternion.identity` (facing $+Z$).
- **Trigger Collider:** BoxCollider trigger spanning $(6.0, 3.5, 2.0)\,\text{m}$ centered at $(0.0, 1.5, 0.0)$.
- **Configuration:**
  - `checkpointName`: `"Checkpoint 1"`
  - `sectionName`: `"Section 4: Spike Path"`
  - Visuals: Stone cylindrical pedestal, $45^\circ$-rotated diamond indicator cube with `M_Neon_Cyan` emission, and local point light (range $5.0\,\text{m}$, intensity $0.6$).

### 3.2 Global Checkpoint Hierarchy
With Phase 05 implemented, the course features **strictly two checkpoints**:
1. **Initial Spawn:** $Z = 0.0\,\text{m}$ (Tutorial Start Platform). Active for Sections 1, 2, and 3.
2. **Checkpoint 1:** $Z = 107.5\,\text{m}$ (Section 4 Entrance). Active for Sections 4 and 5.
3. **Checkpoint 2:** $Z = 228.0\,\text{m}$ (Section 6 Entrance). Active for Section 6.

Deaths occurring in Section 1, 2, or 3 respawn at the initial spawn point. Deaths occurring in Section 4 or 5 respawn at Checkpoint 1. Deaths occurring in Section 6 respawn at Checkpoint 2.

---

## 4. Part B — Section 6 Physics Chamber Implementation

### 4.1 Level Layout & Geometry ($Z = 227.0\,\text{m} \rightarrow 274.0\,\text{m}$)

| Element | Transform / Position | Scale / Dimensions | Material / Component |
|---|---|---|---|
| `Section_06_Entrance_Trigger` | $(0.0, 1.5, 227.5)$ | Size $(8.0, 3.5, 1.5)\,\text{m}$ | `SectionTrigger` ("Section 6: Physics Chamber") |
| `Checkpoint_02` | $(0.0, 0.0, 228.0)$ | Trigger $(7.6, 3.5, 2.5)\,\text{m}$ | `Checkpoint` ("Checkpoint 2"), monument at $(-2.2, 0.4, 228.5)$ |
| `Platform_15_Section6_Entrance` | $(0.0, -0.5, 230.0)$ | $(10.0, 1.0, 6.0)\,\text{m}$ | `M_Stone_Platform`, dark trim, flanking pillars |
| `Chamber_Floor` | $(0.0, -0.5, 248.5)$ | $(16.0, 1.0, 31.0)\,\text{m}$ | `M_Stone_Platform`, edge trims, center dividing line |
| `Wall_Left` / `Wall_Right` | $(\pm 8.2, 2.5, 248.5)$ | $(0.4, 5.0, 31.0)\,\text{m}$ | `M_Dark_Trim`, neon horizontal accent stripes |
| Wall Pillars (4 pairs) | $(\pm 7.8, 2.5, Z \in \{236, 244, 252, 260\})$ | $(0.8, 5.0, 0.8)\,\text{m}$ | `M_Stone_Platform`, `M_Dark_Trim` |
| Bay Dividing Pillars | $(0.0, 1.5, 242.0)$ & $(0.0, 1.5, 250.0)$ | $(0.8, 3.0, 0.8)\,\text{m}$ | `M_Stone_Platform`, `M_Neon_Cyan` |
| Chamber Atmospheric Lights | $(\pm 4.5, 3.5, 246.0)$ | Range $12.0\,\text{m}$, Intensity $1.8$ | Soft cyan/blue point illumination |
| `Crate_Reset_Terminal` | $(0.0, 0.0, 235.0)$ | Trigger $(2.5, 2.5, 2.5)\,\text{m}$ | `CrateResetTerminal` (`IInteractable`) |
| `Crate_Left` | $(-4.5, 0.65, 239.0)$ | $(1.3, 1.3, 1.3)\,\text{m}$ | `PushableCrate`, Rigidbody, `M_Crate_Base` |
| `Crate_Right` | $(4.5, 0.65, 239.0)$ | $(1.3, 1.3, 1.3)\,\text{m}$ | `PushableCrate`, Rigidbody, `M_Crate_Base` |
| `Plate_Left` | $(-4.5, 0.0, 255.0)$ | Housing $(2.4, 0.06, 2.4)\,\text{m}$ | `CratePressurePlate`, pad depression $0.08\,\text{m}$ |
| `Plate_Right` | $(4.5, 0.0, 255.0)$ | Housing $(2.4, 0.06, 2.4)\,\text{m}$ | `CratePressurePlate`, pad depression $0.08\,\text{m}$ |
| `Door_Exit_Puzzle` | $(0.0, 0.0, 263.0)$ | Opening $(6.0 \times 3.4)\,\text{m}$ | `PuzzleDoor`, dual sliding blast leaves |
| `Platform_16_Section6_Exit` | $(0.0, -0.5, 269.0)$ | $(8.0, 1.0, 10.0)\,\text{m}$ | `M_Stone_Platform`, 4 pillars, Exit Gateway Arch |

### 4.2 Pushable Crates & Physics Tuning (`PushableCrate.cs`)
- **Mass:** Tuned to $25.0\,\text{kg}$. Heavy enough to convey physical weight without feeling sluggish.
- **Linear Damping (Drag):** Tuned to $4.0$. Provides smooth braking when the player stops pushing, preventing runaway momentum.
- **Angular Damping:** Tuned to $5.0$.
- **Constraints:** `FreezeRotationX | FreezeRotationZ`. Prevents crates from pitching forward, tipping over, or wedging awkwardly into corners.
- **Collision Detection & Interpolation:** `CollisionDetectionMode.Continuous` and `RigidbodyInterpolation.Interpolate` eliminate tunneling or jitter against walls and plates.
- **Player Controller Push Pipeline:** In `PlayerController.OnControllerColliderHit`:
  ```csharp
  if (hit.moveDirection.y > -0.3f)
  {
      var crate = hit.collider.GetComponentInParent<PushableCrate>();
      if (crate != null)
      {
          float currentSpeed = (Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed) ? sprintSpeed : walkSpeed;
          crate.HandlePush(hit, currentSpeed);
      }
      ...
  }
  ```
  `PushableCrate.HandlePush` extracts horizontal push direction and applies a target velocity clamped between $1.8\,\text{m/s}$ and $3.2\,\text{m/s}$, preserving vertical gravity for natural edge drops.

### 4.3 Crate Pressure Plates (`CratePressurePlate.cs`)
- **Detection Logic:** Triggers specifically on `PushableCrate` colliders. Player contact alone does not satisfy the plate condition.
- **Pad Depression:** When occupied, the inner pad smoothly translates down by $0.08\,\text{m}$ at $8.0\,\text{m/s}$.
- **Visual Feedback:** Pad color and emission shift from inactive dark slate (`#267399`) to active bright neon emerald/cyan (`#1AF266`). Light intensity boosts from $0.5$ to $2.5$.
- **Presence Verification:** Periodic `Physics.OverlapBox` checks verify the crate remains on the pad, automatically releasing the plate if the crate is pushed off or reset.

### 4.4 Locked Exit Door (`PuzzleDoor.cs`)
- **Blast Door Geometry:** Dual leaves (`DoorLeaf_Left` and `DoorLeaf_Right`) each measuring $(3.05, 3.4, 0.35)\,\text{m}$, framed by stone portal walls and a dark lintel header.
- **Physical Blocking:** Solid `BoxCollider` components on both leaves remain active while closed, preventing player and crates from passing through.
- **Status Indicator:** Centered above the doorway: a diamond panel and point light illuminate red (`#FF2626`) while locked, transitioning to green (`#1AF266`) upon unlock.
- **Dual-Plate Logic:** Subscribes to `plateLeft.OnStateChanged` and `plateRight.OnStateChanged`:
  - Plate Left only active $\rightarrow$ Door remains locked.
  - Plate Right only active $\rightarrow$ Door remains locked.
  - Both plates simultaneously active $\rightarrow$ Door unlocks, plays unlock audio, broadcasts notification `"Chamber Unlocked — Exit Access Granted"`, and smoothly slides both leaves outward by $3.0\,\text{m}$ at $3.5\,\text{m/s}$.
  - Colliders disable when leaves reach open bounds, allowing smooth passage onto `Platform_16_Section6_Exit`.

### 4.5 Anti-Softlock & Lost Crate Recovery
To ensure the player is never trapped or softlocked:
1. **Void Fall Recovery:** If a crate falls below $Y = -5.0\,\text{m}$ or contacts `KillVolume_Pit`, `PushableCrate.FixedUpdate` automatically resets it to its initial position and orientation with zero velocity, displaying `"Lost crate recovered to origin."`
2. **Interactive Terminal (`CrateResetTerminal.cs`):** A dedicated pedestal terminal near the entrance allows the player to press `E` at any time to return both crates to their starting coordinates (`"Puzzle Crates Reset to Starting Coordinates."`).
3. **Course & Player Reset:** Respawning after death or restarting the expedition resets all crates, de-presses plates, and re-locks the blast door.

---

## 5. Part C — Integration & Verification

### 5.1 Compilation
All scripts compiled cleanly via Unity's internal Roslyn compiler:
- `Assembly-CSharp.dll`: 0 errors, 0 warnings.
- `Assembly-CSharp-Editor.dll`: 0 errors, 0 warnings.

### 5.2 Test Execution Results

#### 1. Phase 05 Test Suite (`VoidboundPhase05Tests.cs`)
Command executed via Unity batchmode:
```bash
Unity -batchmode -quit -projectPath /Users/kongsothearith/Projects/VoidBound \
      -executeMethod Voidbound.Editor.VoidboundPhase05Tests.RunTests -logFile -
```
**Results: 65 PASSED, 0 FAILED (Exit Code 0)**

Key assertions verified:
1. `[PASS] Scene Load Test` — Scene loaded successfully.
2. `[PASS] AudioListener Count Test` — Exactly 1 AudioListener in scene.
3. `[PASS] Exact Checkpoint Count Test` — Exactly 2 checkpoints in scene.
4. `[PASS] Checkpoint Identification Test` — Checkpoint 1 and Checkpoint 2 both present.
5. `[PASS] Checkpoint 1 Absent From Section 3 Test` — Section 3 contains no checkpoint.
6. `[PASS] Checkpoint 1 Present In Section 4 Test` — Checkpoint 1 located in Section 4.
7. `[PASS] Checkpoint 1 Relocation Coordinate Test` — Checkpoint 1 respawn $Z = 107.5\,\text{m}$.
8. `[PASS] Checkpoint 1 Section Name Test` — Section name `"Section 4: Spike Path"`.
9. `[PASS] Checkpoint 2 Present In Section 6 Test` — Checkpoint 2 located in Section 6.
10. `[PASS] Checkpoint 2 Coordinate Test` — Checkpoint 2 respawn $Z = 228.0\,\text{m}$.
11. `[PASS] Checkpoint 2 Section Name Test` — Section name `"Section 6: Physics Chamber"`.
12. `[PASS] Checkpoint 2 Monument Feedback Test` — Pedestal, indicator, and light present.
13. `[PASS] Section 6 Trigger Test` — Entrance trigger present and configured.
14. `[PASS] Section 6 Geometry Test` — Entrance, floor, walls, and exit platforms exist.
15. `[PASS] Pushable Crates Presence Test` — Both `Crate_Left` and `Crate_Right` exist.
16. `[PASS] PushableCrate Component Test` — `PushableCrate` components configured.
17. `[PASS] Rigidbody Physics Dynamic Test` — Both crates non-kinematic.
18. `[PASS] Crate Mass Tuning Test` — Mass tuned to $25.0\,\text{kg}$.
19. `[PASS] Crate Linear Damping Test` — Linear damping $\ge 3.0$ ($4.0$).
20. `[PASS] Crate Rotation Constraints Test` — Constraints freeze rotation X and Z.
21. `[PASS] Crates Opposite Sides Test` — Left $X = -4.5\,\text{m}$, Right $X = +4.5\,\text{m}$.
22. `[PASS] Pressure Plates Presence Test` — Both plates exist.
23. `[PASS] CratePressurePlate Component Test` — `CratePressurePlate` attached.
24. `[PASS] Plates Initial State Test` — Both plates start unpressed.
25. `[PASS] Plate Depression Tuning Test` — Depress distance $0.08\,\text{m}$, speed $8.0\,\text{m/s}$.
26. `[PASS] Exit Door Object Test` — `Door_Exit_Puzzle` exists.
27. `[PASS] PuzzleDoor Component Test` — `PuzzleDoor` component attached.
28. `[PASS] Door Initial Locked State Test` — Door starts locked and closed.
29. `[PASS] Door Leaves Test` — Dual sliding leaves present.
30. `[PASS] Door Colliders Blocking Test` — Colliders block passage while locked.
31. `[PASS] Plate Left Alone Test` — Door remains locked when only Plate Left is pressed.
32. `[PASS] Plate Right Alone Test` — Door remains locked when only Plate Right is pressed.
33. `[PASS] Dual Plate Simultaneous Unlock Test` — Door unlocks and opens when both plates are pressed.
34. `[PASS] Dual Sliding Door Open Motion Test` — Leaves slide open to left and right.
35. `[PASS] Door Reset Test` — Door resets to locked and closed.
36. `[PASS] Crate Push Velocity Test` — Crate receives forward velocity when pushed.
37. `[PASS] Void Fall Crate Recovery Test` — Lost crate automatically recovered to origin.
38. `[PASS] Recovered Crate Velocity Zeroed Test` — Velocity zeroed upon recovery.
39. `[PASS] Crate Reset Terminal Test` — Terminal exists in Section 6.
40. `[PASS] Terminal IInteractable Test` — Terminal implements `IInteractable`.
41. `[PASS] Terminal Manual Crate Reset Test` — Interacting restores crates to origin.
42. `[PASS] Pre-Checkpoint Death Respawn Test` — Respawns at Section 1 ($Z = 0.0\,\text{m}$) before Checkpoint 1.
43. `[PASS] Checkpoint 1 Activated Flag Test` — Checkpoint 1 activation flag set.
44. `[PASS] GameManager Custom Checkpoint 1 Test` — Checkpoint 1 registered in GameManager.
45. `[PASS] GameManager Checkpoint 1 Active Z Test` — Active respawn $Z = 107.5\,\text{m}$.
46. `[PASS] Checkpoint 1 Notification Test` — Notification banner fired.
47. `[PASS] Respawn At Checkpoint 1 Test` — Player respawned at $Z = 107.5\,\text{m}$ on death.
48. `[PASS] Checkpoint 2 Activated Flag Test` — Checkpoint 2 activation flag set.
49. `[PASS] GameManager Checkpoint 2 Active Z Test` — Active respawn $Z = 228.0\,\text{m}$.
50. `[PASS] Checkpoint 2 Notification Test` — Notification banner fired.
51. `[PASS] Respawn At Checkpoint 2 Test` — Player respawned at $Z = 228.0\,\text{m}$ on death.
52. `[PASS] Restart Custom Checkpoint Cleared Test` — Custom checkpoints cleared on restart.
53. `[PASS] Restart Checkpoints Inactive Test` — Both checkpoints reset to inactive.
54. `[PASS] Restart Section 1 Spawn Test` — Player returned to Section 1 spawn.
55. `[PASS] Restart Door Locked Test` — Door re-locks on restart.
56. `[PASS] Restart Plates Unpressed Test` — Plates unpressed on restart.
57. `[PASS] Section 1 Tutorial Regression Test` — Section 1 platforms and terminal intact.
58. `[PASS] Section 2 Hammer Swing Regression Test` — Section 2 intact.
59. `[PASS] Section 3 Moving Platform Regression Test` — Section 3 intact.
60. `[PASS] Section 4 Spike Path Regression Test` — Section 4 intact.
61. `[PASS] Section 5 Laser Corridor Regression Test` — Section 5 intact.
62. `[PASS] Kill Volume Extended Coverage Test` — Kill volume covers through Section 6 ($550.0\,\text{m}$).

#### 2. Phase 04 Regression Test Suite (`VoidboundPhase04Tests.cs`)
Command executed via Unity batchmode:
```bash
Unity -batchmode -quit -projectPath /Users/kongsothearith/Projects/VoidBound \
      -executeMethod Voidbound.Editor.VoidboundPhase04Tests.RunTests -logFile -
```
**Results: 65 PASSED, 0 FAILED (Exit Code 0)**

---

## 6. Changed & Created Files Inventory

### 6.1 Created Scripts
- [`PushableCrate.cs`](file:///Users/kongsothearith/Projects/VoidBound/Assets/Scripts/Interactions/PushableCrate.cs): Rigidbody physics handling, push force application, void fall recovery, and level reset hooks.
- [`CratePressurePlate.cs`](file:///Users/kongsothearith/Projects/VoidBound/Assets/Scripts/Interactions/CratePressurePlate.cs): Crate-detecting pressure plate with physical depression animation, emissive materials, and event dispatching.
- [`PuzzleDoor.cs`](file:///Users/kongsothearith/Projects/VoidBound/Assets/Scripts/Interactions/PuzzleDoor.cs): Dual sliding blast doors, red/green status indicator beacon, and simultaneous dual-plate unlock logic.
- [`CrateResetTerminal.cs`](file:///Users/kongsothearith/Projects/VoidBound/Assets/Scripts/Interactions/CrateResetTerminal.cs): Interactive terminal implementing `IInteractable` allowing manual puzzle crate resets.
- [`VoidboundPhase05Tests.cs`](file:///Users/kongsothearith/Projects/VoidBound/Assets/Scripts/Editor/VoidboundPhase05Tests.cs): Complete 65-assertion automated test suite for Phase 05.

### 6.2 Modified Scripts & Scene
- [`PlayerController.cs`](file:///Users/kongsothearith/Projects/VoidBound/Assets/Scripts/Player/PlayerController.cs): Added Rigidbody push physics in `OnControllerColliderHit` for `PushableCrate` and dynamic bodies.
- [`VoidboundSceneBuilder.cs`](file:///Users/kongsothearith/Projects/VoidBound/Assets/Scripts/Editor/VoidboundSceneBuilder.cs):
  - Removed Checkpoint 1 from Section 3.
  - Added Checkpoint 1 to Section 4 entrance ($Z = 107.5\,\text{m}$).
  - Added `CreateSection06PhysicsChamber` and helpers.
  - Added `BuildPhase05Scene()` menu item and procedure.
  - Extended `KillVolume_Pit` to $550.0\,\text{m}$ ($Z = 150.0\,\text{m}$, bounds $[-125\,\text{m}, +425\,\text{m}]$).
- [`VoidboundPhase04Tests.cs`](file:///Users/kongsothearith/Projects/VoidBound/Assets/Scripts/Editor/VoidboundPhase04Tests.cs): Updated Checkpoint 1 expected coordinates from $65.0\,\text{m}$ to relocated position $107.5\,\text{m}$.
- [`Voidbound.unity`](file:///Users/kongsothearith/Projects/VoidBound/Assets/Scenes/Voidbound.unity): Scene file saved with Section 6, Checkpoint 1 relocated, and Checkpoint 2 placed.
- [`README.md`](file:///Users/kongsothearith/Projects/VoidBound/README.md): Documented Phase 05 implementation, Section 6 status, and relocated checkpoint details.

---

## 7. Known Issues & Minor Limitations

1. **Unity Batchmode Licensing IPC:** On macOS, launching Unity in batchmode requires accessing licensing sockets (`LicenseClient-kongsothearith`), which requires sandbox elevation. Running with sandbox bypass executes cleanly with exit code 0.
2. **Audio Clips:** Audio properties on `PushableCrate`, `CratePressurePlate`, and `PuzzleDoor` gracefully support optional clips. No missing audio exceptions occur when clips are unassigned.

---

## 8. Deviations & Implementation Decisions

1. **Checkpoint Relocation Justification:** Checkpoint 1 was moved from Section 3 to Section 4 per explicit project requirement ("Move Checkpoint 1 from Section 3 entrance to Section 4 entrance. Exactly two checkpoints total"). This improves level pacing by treating Sections 1–3 as an early mastery gauntlet, followed by Checkpoint 1 at Section 4, and Checkpoint 2 at Section 6.
2. **Dual Sliding Blast Door Design:** Instead of a single vertical rising slab, dual horizontal sliding doors were implemented. This sci-fi blast door aesthetic fits the chamber visual language and ensures wide, clear egress.
3. **Rotation Constraint on Crates:** Constraining rotation around X and Z axes ensures that crates slide cleanly along the floor and onto the plates without tumbling onto their sides or flipping upside down, creating a consistent and predictable physics puzzle.

---

## 9. Verification & Manual Play Guide

### 9.1 In Unity Editor
1. Open `Assets/Scenes/Voidbound.unity`.
2. Press **Play**.
3. Click **Initiate Expedition** on the start menu.
4. Advance through Section 1 (Tutorial), Section 2 (Hammers), and Section 3 (Moving Platform).
5. At Section 4 entrance ($Z = 107.5\,\text{m}$), observe Checkpoint 1 indicator light activate cyan and display the notification: `"Checkpoint Activated: Checkpoint 1"`.
6. Traverse Section 4 (Spikes) and Section 5 (Lasers).
7. At Section 6 entrance ($Z = 228.0\,\text{m}$), observe Checkpoint 2 indicator light activate and display: `"Checkpoint Activated: Checkpoint 2"`.
8. Enter the Physics Chamber:
   - Walk up to the left crate and push it forward into the left bay onto `Plate_Left`. Observe the pad depress and glow bright green.
   - Observe that the exit door remains closed and its status indicator stays red.
   - Push the right crate forward onto `Plate_Right`.
   - Observe that once both plates are active, the blast door unlocks, the status beacon turns green, and both doors slide open.
   - Walk through the doorway onto the exit platform `Platform_16_Section6_Exit`.
9. Test Crate Recovery:
   - Push a crate into the void or look at `Crate_Reset_Terminal` and press `E`. Observe both crates reset to origin.

### 9.2 Via Automated Batchmode Tests
Run the automated test suite from the terminal:
```bash
/Applications/Unity/Hub/Editor/6000.3.8f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -quit -projectPath /Users/kongsothearith/Projects/VoidBound \
  -executeMethod Voidbound.Editor.VoidboundPhase05Tests.RunTests -logFile -
```
Verify the output log concludes with:
`[Phase 05 Tests] COMPLETED: 65 PASSED, 0 FAILED.`
