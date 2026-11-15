# Voidbound — Comprehensive Phase 03 & Section 3 Implementation Report

**Project:** Voidbound (single-player first-person 3D parkour obstacle course)  
**Target Environment:** Unity 6000.3.8f1, Universal Render Pipeline (URP) 17.3.0, Unity Input System 1.18.0  
**Phase:** Phase 03 — Pressure-Plate Moving Platform & Checkpoint 1 (Full Iteration History)  
**Status:** Completed, Fully Polished & Verified  

---

## 1. Executive Summary

Phase 03 of Voidbound implements Section 3 ("Moving Platform") as specified in `VOIDBOUND_GAME_DESIGN.md`, alongside critical scene cleanup, traversal tuning, and bug fixes identified across multiple testing cycles.

Section 3 introduces the player to mechanical puzzle interaction and checkpoint persistence:
1. **Checkpoint 1:** Automatically activates at the entrance of Section 3 ($Z = 65.0\text{m}$), storing the player's respawn position at $(0, 0.1, 65.0)$ so subsequent deaths respawn the player in Section 3 rather than the Section 1 tutorial spawn.
2. **Pressure Plate:** A depressible stone-and-rune plate on the Section 3 Departure Platform situated near the void ledge ($Z = 73.2\text{m}$). Depressing the plate summons the moving platform across the void; releasing the plate initiates a return delay before the platform retreats.
3. **Moving Platform:** A $4.0\text{m} \times 0.8\text{m} \times 4.0\text{m}$ stone platform traversing a $20.0$-meter lethal void chasm between Point A (far destination ledge, $Z = 92.8\text{m}$) and Point B (near departure ledge, $Z = 77.2\text{m}$) at a brisk $6.5\text{m/s}$.
4. **Stable Rider Transport (Single-Move Physics):** The player stands completely still on the moving platform with zero sliding, zero pushing, and zero jitter. Transport is handled by unifying player input, platform delta, and vertical gravity into a single per-frame `CharacterController.Move()` call.
5. **Two-Tier Gravity Architecture:** Solved the conflict between fast void falling and platform jump clearance. Normal platform jumps enjoy a comfortable, athletic air time ($g = -20\,\text{m/s}^2$, $h = 1.8\,\text{m}$, air time $0.85\,\text{s}$, clearing $5.1\,\text{m}$ walk / $8.1\,\text{m}$ sprint), while falling below platform level into the void ($Y < -0.3\,\text{m}$) triggers accelerated void plunge gravity ($-45.0\,\text{m/s}^2$), dropping to the kill pit in $\approx 0.7\,\text{s}$.
6. **Exploit Elimination & Immersion Polish:** Eliminated a legacy self-colliding `CheckSphere` bug that previously enabled infinite mid-air jump spam across the void; removed all immersion-breaking on-screen event popups ("Entering Section 2", "Knocked by Hammer!", etc.); reoriented Terminal Alpha to face spawn; and cleaned the temporary Phase 01 milestone marker.

All existing Phase 01 and Phase 02 systems (player movement, camera look, sprint, jumping, swinging pendulum hammers, knockback physics, safe zones, pause menu, lives HUD, elapsed timer, and void kill volume) were verified with zero regressions across 151 automated test assertions.

---

## 2. Chronological Iterations & Problem Solved

### Iteration 1: Initial Section 3 Construction & Scene Cleanup
- **Terminal Alpha Reorientation:** Reoriented `Environment/Prototype_Terminal` from yaw $0^\circ \rightarrow 129.8^\circ$ so that the terminal body directly faces the player spawn point at $(0, 1, 0)$, and tilted `ConsoleScreen` upward by $-25.0^\circ$ toward player eye level ($Y = 1.6\text{m}$).
- **Prototype Finish Marker Removal:** Removed the temporary Phase 01 `Prototype_Finish_Marker` GameObject and its child visual hierarchy (`Pedestal`, `BeaconMesh`, `BeaconLight`) from the scene, while preserving `Assets/Scripts/Interactions/PrototypeFinishMarker.cs`.
- **Checkpoint 1 Repositioning:** Moved the monument inward from the platform boundary ($X = -3.4\text{m} \rightarrow X = -1.8\text{m}, Z = 65.5\text{m}$), ensuring prominent visibility past the archway while keeping the central player corridor ($X \in [-1.0, +1.0]$) clear.
- **Single Checkpoint Verification:** Confirmed that exactly 1 checkpoint exists in the scene hierarchy; no duplicate checkpoints were created.
- **Section 3 Core Setup:** Built Departure Platform ($Z: 63-75\text{m}$), Arrival Platform ($Z: 95-107\text{m}$), 20m void gap ($Z: 75-95\text{m}$), and completion gateway arch at $Z = 106.8\text{m}$.

### Iteration 2: Immersion Polish, Pressure Plate Adjustment & Riding Fixes
- **Removal of Event Reporting Pop-ups:**
  - Removed on-screen notification text that reported gameplay events ("Entering Section 2: Hammer Swing", "Entering Section 3: Moving Platform", "Knocked by Hammer!", "Checkpoint Activated").
  - Silenced `ShowNotification` calls across `SectionTrigger`, `HammerKnockback`, `Checkpoint`, and `GameManager`.
  - Deactivated `NotificationText` in `UIManager` to keep the center of the viewport clean.
- **Pressure Plate Re-anchoring:**
  - Moved the pressure plate closer to the departure ledge ($Z = 70.5\text{m} \rightarrow Z = 73.2\text{m}$).
  - With platform front edge at $Z = 75.0\text{m}$ and plate radius $1.1\text{m}$, the front lip rests just $0.7\text{m}$ from the edge, enabling an intuitive step-on/step-off boarding flow.
- **Faster Platform Transit:**
  - Increased platform speed from $4.0\text{m/s}$ to $6.5\text{m/s}$, reducing transit time across the chasm from $3.9\text{s}$ to $2.4\text{s}$.
- **Resolution of Platform "Pushing / Can't Stand" Issue:**
  - *Root Cause:* Multiple `CharacterController.Move()` calls per frame (one horizontal by platform, one vertical by player controller) reset Unity's `isGrounded` flag to `false` during horizontal updates, accumulating downward gravitational acceleration. In addition, `ContinuousSpeculative` collision detection on the kinematic Rigidbody caused PhysX to generate predictive collision impulses that physically shoved the player capsule.
  - *Fix:* Switched platform Rigidbody to `Discrete` with `Interpolation = None`; implemented ground-based platform tracking via `OnControllerColliderHit`; and unified movement into **exactly ONE `CharacterController.Move()` call per frame** (`horizontalMovement + platformDelta + verticalMovement`).

### Iteration 3: Mid-air Jump Spam Fix & Two-Tier Gravity Architecture
- **Elimination of Mid-air Jump Exploit:**
  - *Root Cause:* A legacy ground check in `PlayerController.cs` (`Physics.CheckSphere(transform.position + Vector3.up * 0.1f, 0.34f, ~0)`) checked all layers without excluding the player's own capsule. Because the sphere origin was inside the capsule, `isGrounded` evaluated to `true` 100% of the time, everywhere in the world. Players could spam Space in mid-air to fly across the void and bypass the entire course.
  - *Fix:* Removed the self-colliding `CheckSphere` call and switched to native `CharacterController.isGrounded`. Enforced strict jump conditions (`jumpPressed && isGrounded && velocity.y <= 0.1f`), resetting `isGrounded = false` immediately upon launch.
- **Two-Tier Gravity Architecture (Fixing Section 1 Jump & Fast Void Fall):**
  - *Root Cause:* When falling speed was initially increased, applying harsh gravity ($-36\text{m/s}^2$ with a fall multiplier) during normal jumps reduced air time to $\approx 0.5\text{s}$, making the 4.0-meter Section 1 tutorial jump physically impossible to clear. Furthermore, when adjusting the C# default to `-10.0f`, Unity ignored the change because serialized Inspector values stored in `Assets/Scenes/Voidbound.unity` override C# field initializers.
  - *Fix:* Implemented a **Two-Tier Gravity System**:
    1. **Platforming Jump Gravity ($g = -20.0\,\text{m/s}^2$, $h = 1.8\,\text{m}$):** Operates during normal jumps while above the walking plane ($Y \ge -0.3\text{m}$). Air time is $\approx 0.85\text{s}$. At normal walking speed ($6.0\text{m/s}$), horizontal jump distance is $5.1\text{m}$ (easily clears 4.0m Section 1 gap with $+1.1\text{m}$ margin); at sprint speed ($9.5\text{m/s}$), jump distance is $8.1\text{m}$ ($+4.1\text{m}$ margin).
    2. **Void Plunge Gravity ($g_{\text{void}} = -45.0\,\text{m/s}^2$):** Engages automatically when the player falls below platform level ($Y < -0.3\text{m}$ in midair), plunging the player into the kill volume ($Y = -12\text{m}$) in $\approx 0.7\text{s}$.
    3. **Inspector & Code Safeguard:** Synchronized `Voidbound.unity` with the tuned values and added an auto-clamp safeguard in `PlayerController.Awake()`.

---

## 3. Implemented Components & Mechanics Detail

### 3.1 Checkpoint 1 (`Checkpoint_01`)
- **Hierarchy:** `Environment/Section_03_MovingPlatform/Checkpoint_01`
- **Trigger Volume:** Trigger `BoxCollider` ($7.6\text{m} \times 3.5\text{m} \times 2.5\text{m}$) centered at $(0.0, 1.5, 65.0)$ spanning the walkway threshold.
- **Spawn Point Override:** Child transform `SpawnPoint` at $(0.0, 0.1, 65.0)$ facing $+Z$.
- **Monument Visuals:**
  - `CheckpointPedestal`: Stone cube ($0.8\text{m} \times 0.4\text{m} \times 0.8\text{m}$) at $X = -1.8\text{m}, Z = 65.5\text{m}$.
  - `CheckpointIndicator`: Diamond mesh (cube rotated $45^\circ$ on X and Z) floating at $Y = 1.4\text{m}$.
  - `CheckpointLight`: Point light at $Y = 1.4\text{m}$.
  - *Inactive State:* Dim slate color (`#4D4D66`), no emission, 0.5 light intensity.
  - *Active State:* Electric cyan (`#1AF2FF`), 1.5x emission boost, 2.5 light intensity.
- **Lifecycle & Persistence:** Interacts with `GameManager.Instance.SetCheckpoint()`. Subscribes to `GameManager.OnGameReset` to reset to inactive upon full run restart.

### 3.2 Pressure Plate (`PressurePlate`)
- **Hierarchy:** `Environment/Section_03_MovingPlatform/PressurePlate`
- **Position:** $(0.0, 0.0, 73.2)$ on `Platform_05_Section3_Departure`.
- **Occupancy Detection:** Trigger `BoxCollider` ($1.8\text{m} \times 1.0\text{m} \times 1.8\text{m}$) with `HashSet<Collider>` occupancy tracking and per-frame validity pruning.
- **Mechanical Feedback:**
  - Pad (`PlatePad`) depresses downward by $0.08\text{m}$ at $8.0\text{m/s}$ when stepped on, and smoothly returns when released.
  - Rune trim shifts from dim cyan (`#267399`) to electric cyan (`#1AF2FF`).
  - Point light shifts intensity from $0.5 \rightarrow 2.5$.
- **Deterministic Batchmode Hook:** Exposes `Tick(float dt)` so edit-mode tests can deterministically simulate plate mechanics independent of `Time.deltaTime`.

### 3.3 Moving Platform (`MovingPlatform`)
- **Hierarchy:** `Environment/Section_03_MovingPlatform/MovingPlatform`
- **Geometry:** $4.0\text{m} \times 0.8\text{m} \times 4.0\text{m}$ stone platform with neon trim strips and underside spotlight.
- **Waypoints:**
  - `Point A` (Resting far ledge across void): $(0.0, -0.5, 92.8)$
  - `Point B` (Summoned near departure ledge): $(0.0, -0.5, 77.2)$
- **Motion Parameters:**
  - `moveSpeed`: $6.5\text{m/s}$
  - `returnDelay`: $0.8\text{s}$
  - Movement uses `Vector3.MoveTowards`, stopping precisely at waypoints with zero overshoot or endpoint jitter.
- **Kinematic Physics:**
  - Rigidbody set to `isKinematic = true`, `interpolation = None`, `collisionDetectionMode = Discrete`.
  - Eliminates speculative contact forces and physics fighting against the CharacterController.

### 3.4 Player Movement & Riding Architecture (`PlayerController`)
- **Platform Detection:** In `OnControllerColliderHit`, upward contact normals (`hit.normal.y > 0.5f`) detect `MovingPlatform` directly underneath the player's feet.
- **Unified Single-Move Transport:**
  - Horizontal WASD input, knockback velocity, and platform displacement delta (`platformDelta`) are combined with vertical gravity displacement into **ONE `characterController.Move()` call per frame**.
  - `characterController.isGrounded` remains continuously `true` while standing on the platform.
  - Zero slip, zero pushing, zero camera jitter, and full ability to sprint, jump, or turn while riding.
- **Two-Tier Gravity Architecture:**
  - Normal platforming jump ($Y \ge -0.3\text{m}$): $g = -20.0\,\text{m/s}^2$, $h = 1.8\,\text{m}$, air time $0.85\,\text{s}$.
  - Void plunge ($Y < -0.3\text{m}$ and airborne descent): $g_{\text{void}} = -45.0\,\text{m/s}^2$, plunging to $Y = -12\text{m}$ in $\approx 0.7\,\text{s}$.
- **Grounded Verification & Jump Lock:**
  - Strictly relies on native `characterController.isGrounded` and `velocity.y <= 0.1f`.
  - Immediately resets `isGrounded = false` upon jumping.
  - Mid-air jumping and jump-spamming across the void are completely impossible.

### 3.5 Level Geometry & Hazards
- **Section 3 Departure Platform (`Platform_05_Section3_Departure`):** $8.0\text{m} \times 1.0\text{m} \times 12.0\text{m}$ stone platform spanning $Z = 63.0\text{m}$ to $75.0\text{m}$.
- **Void Chasm:** $20.0$-meter lethal open gap spanning $Z = 75.0\text{m}$ to $95.0\text{m}$. Cannot be crossed on foot without the moving platform (max sprint-jump is $\approx 8.1\text{m}$).
- **Section 3 Arrival Platform (`Platform_06_Section3_Arrival`):** $8.0\text{m} \times 1.0\text{m} \times 12.0\text{m}$ stone platform spanning $Z = 95.0\text{m}$ to $107.0\text{m}$, terminating in a completion gateway arch at $Z = 106.8\text{m}$.
- **Kill Volume (`KillVolume_Pit`):** Expanded trigger box spanning $Z = -120\text{m}$ to $+200\text{m}$ (length $320\text{m}$) at $Y = -12\text{m}$. Falling anywhere in Section 3 consumes one life and triggers respawn at Checkpoint 1.

---

## 4. Complete File Manifest

| File | Status | Description |
|---|---|---|
| `Assets/Scripts/Player/PlayerController.cs` | Modified | Two-tier gravity (jump -20m/s², void -45m/s²), jump apex 1.8m, unified single Move() call, moving platform detection via OnControllerColliderHit, native isGrounded jump gating. |
| `Assets/Scripts/Interactions/MovingPlatform.cs` | Created | Moving platform component with configurable waypoints, speed 6.5m/s, return delay 0.8s, Discrete Kinematic Rigidbody, and exposed FrameDelta. |
| `Assets/Scripts/Interactions/MovingPlatform.cs.meta` | Created | Unity asset metadata. |
| `Assets/Scripts/Interactions/PressurePlate.cs` | Created | Pressure plate component with trigger detection, depression animation (0.08m at 8m/s), emissive/light feedback, and reset handlers. |
| `Assets/Scripts/Interactions/PressurePlate.cs.meta` | Created | Unity asset metadata. |
| `Assets/Scripts/Interactions/PlatformRiderDetector.cs` | Created | Child trigger script relaying rider contact to MovingPlatform. |
| `Assets/Scripts/Interactions/PlatformRiderDetector.cs.meta` | Created | Unity asset metadata. |
| `Assets/Scripts/Core/Checkpoint.cs` | Modified | Added IsActivated, checkpoint name/section getters, removed toast notifications, and subscribed to GameManager.OnGameReset. |
| `Assets/Scripts/Core/GameManager.cs` | Modified | Added OnGameReset event, exposed HasCustomCheckpoint / active checkpoint getters, removed notification from SetCheckpoint. |
| `Assets/Scripts/Core/SectionTrigger.cs` | Modified | Removed notificationText and notification display calls. |
| `Assets/Scripts/Obstacles/HammerKnockback.cs` | Modified | Removed on-screen "Knocked by Hammer!" notification pop-up. |
| `Assets/Scripts/UI/UIManager.cs` | Modified | Silenced DisplayNotification and hidden NotificationText on HUD. |
| `Assets/Scripts/Editor/VoidboundSceneBuilder.cs` | Modified | BuildPhase03Scene tool with Terminal Alpha rotation, marker removal, inward Checkpoint 1, plate at Z=73.2, platform speed 6.5m/s, Discrete Rigidbody, player gravity. |
| `Assets/Scripts/Editor/VoidboundPhase01Tests.cs` | Modified | Preserved PrototypeFinishMarker class assertion following GameObject cleanup. |
| `Assets/Scripts/Editor/VoidboundPhase03Tests.cs` | Created | Automated test suite comprising 79 assertions covering Checkpoint 1, Pressure Plate, Moving Platform, Riding, Void Kill, Midair Jump Prevention, Fast Fall, and Regressions. |
| `Assets/Scripts/Editor/VoidboundPhase03Tests.cs.meta` | Created | Unity asset metadata. |
| `Assets/Scenes/Voidbound.unity` | Modified | Saved scene with Section 3, plate at Z=73.2, platform speed 6.5m/s, Discrete Rigidbody, player jumpHeight 1.8 / gravity -20 / voidGravity -45, and notification text disabled. |
| `README.md` | Modified | Updated progress to include Phase 03 as implemented with all latest adjustments. |
| `Phase3_Report.md` | Created | Comprehensive implementation, iteration, and validation report. |

---

## 5. Test Results & Verification

### 5.1 Automated Test Suites Summary

The project includes custom Unity Editor test runners accessible both via batch mode and the Unity Editor top menu (`Voidbound -> Run Phase 0X Automated Tests`):

| Test Suite | Menu / Method | Assertions Passed | Assertions Failed | Result |
|---|---|:---:|:---:|:---:|
| **Phase 03 Test Suite** | `VoidboundPhase03Tests.RunTests` | **79** | **0** | **PASS** |
| **Phase 02 Regression** | `VoidboundPhase02Tests.RunTests` | **43** | **0** | **PASS** |
| **Phase 01 Regression** | `VoidboundPhase01Tests.RunTests` | **29** | **0** | **PASS** |
| **Total** | | **151** | **0** | **ALL PASS** |

### 5.2 Key Validated Scenarios

1. **Scene Integrity & AudioListener:** Verified `Assets/Scenes/Voidbound.unity` loaded validly with exactly 1 active `AudioListener` located on the player camera.
2. **Platform & Gap Geometry:** Verified Departure Platform ($Z$: 63 to 75m) and Arrival Platform ($Z$: 95 to 107m) have solid colliders and frame a 20.0m void chasm that cannot be jumped on foot.
3. **Section 1 Traversal Margin:** Verified jump apex of $1.8\text{m}$ and platforming gravity of $-20.0\,\text{m/s}^2$ provides $\approx 0.85\text{s}$ air time, comfortably clearing the $4.0\text{m}$ gap in Section 1 at walking speed ($5.1\text{m}$) and sprinting speed ($8.1\text{m}$).
4. **Fast Void Descent:** Verified falling below platform level ($Y < -0.3\text{m}$) engages $-45.0\,\text{m/s}^2$ void gravity, plunging into the kill volume in $\approx 0.7\text{s}$.
5. **Mid-air Jump Prevention:** Verified `isGrounded` is strictly `false` in mid-air; verified that jump input in mid-air cannot re-trigger vertical jump velocity.
6. **Clean HUD Display:** Verified no event notification text appears on screen during section transitions, hammer hits, or checkpoint activations.
7. **Platform Speed:** Verified moving platform speed is $6.5\text{m/s}$ (fast, responsive transit across the 20m gap).
8. **Plate Placement:** Verified pressure plate is at $Z = 73.2\text{m}$ (close to departure ledge edge at $Z = 75.0\text{m}$).
9. **Riding Stability (Zero Push / Zero Slip):** Verified player CharacterController moves smoothly with platform displacement delta ($1.625\text{m}$ per $0.25\text{s}$) with zero slip, no parenting, and no collision fighting.
10. **Checkpoint 1 Architecture:** Verified trigger at $Z = 65.0\text{m}$, inward monument at $X = -1.8\text{m}$, single checkpoint count, and respawn at $Z = 65.0\text{m}$.
11. **Puzzle Reset & Course Restart:** Verified plate unpresses, platform resets to Point A on respawn and restart, and initial spawn restored on full course reset.
12. **Regressions:** Phase 01 (movement, jump, terminal, UI) and Phase 02 (hammers, safe zones, knockback) verified with zero regressions.

---

## 6. Remaining Work (Future Phases)

- **Phase 04 — Spike Path & Laser Corridor:** Section 4 narrow bridge with lethal spikes; Section 5 corridor with timed pulsing laser hazards.
- **Phase 05 — Physics Chamber & Checkpoint 2:** Section 6 entrance Checkpoint 2; pushable Rigidbody crates, dual pressure plates, and exit door unlock logic.
- **Phase 06 — Final Gauntlet & Full Game Loop:** Section 7 obstacle gauntlet, extraction portal trigger, victory flow, and final run statistics.
- **Phase 07 — Visual & Audio Polish:** Skybox/starfield tuning, bloom/fog adjustments, and sound effects integration.
- **Phase 08 — Standalone Build & Final Verification:** macOS and Windows standalone builds, recording, and submission bundle.
