# VOIDBOUND — Game Design & Development Specification

Version: 1.0 | Target: Unity 6000.3.8f1, URP 17.3.0, Unity Input System 1.18.0

## 1. Purpose and assessment
Voidbound is a single-player first-person 3D parkour obstacle-course game, designed for the CADT mid-term Unity project. The player crosses floating stone structures in a deep-blue, star-filled void to reach an extraction portal. One main scene and level suffice. The game must demonstrate clear objective and level design, responsive input, physics using Rigidbody and colliders, collision/trigger events, win/failure/restart logic, and player feedback. Assessment weights: game design 20%, controls 20%, physics/interactions 20%, logic/playability 20%, repository/documentation 10%, teamwork/contributions 10%.

## 2. Design pillars
- Simple, readable low-poly floating platforms, stone bridges and arches, cyan/violet lighting, deep cobalt-blue starfield, distant silhouettes, subtle fog/bloom.
- Gameplay readability takes precedence over atmospheric effects. Hazard surfaces and safe landing zones must be clear.
- First-person movement with WASD, mouse look, Space jump, Left Shift sprint, E interact (where applicable), Escape pause, and UI buttons for Start/Restart.
- A compact, achievable course with seven sections in order. Avoid combat, enemy AI, multiplayer, procedural generation, multiple levels, and asset-heavy art.

## 3. Sections
1. **Movement Tutorial:** Safe starting platform; controls and short jump, with visible next destination.
2. **Hammer Swing:** Three separate overhead vertically suspended pendulum hammers swing across the path. Safe breathing spaces between swings. Collision causes immediate death.
3. **Pressure-Plate Moving Platform:** Checkpoint 1 automatically activates at the *very beginning* of the section. A platform begins on the far side of a gap. Standing on a plate near the departure ledge summons it; releasing the plate makes it return toward the far side. The player must wait for its arrival, step off, quickly jump aboard, ride it and jump off at the destination. Platform motion and speed must permit this solution; player must be carried reliably while riding it. Avoid unintended parenting or jitter.
4. **Spike Path:** Fixed spike formations on a narrow stone bridge, like the provided reference. Touching spikes immediately kills the player; gaps between spikes must be traversable.
5. **Laser Corridor:** Visually clear pulsing or moving laser hazards with safe timing windows; only active lasers kill on contact. Give visible/audio warning before activation where feasible.
6. **Physics Chamber:** Checkpoint 2 automatically activates at the *very beginning* of the section. Two movable Rigidbody crates begin on opposite sides (left and right). Each must be pushed onto its own pressure plate. Both must remain activated simultaneously to unlock the exit door. Crates must remain recoverable if misplaced; avoid softlocks.
7. **Final Gauntlet:** A shorter sequence combining previously learned platforming/hazard timing, leading to a visibly distinct glowing extraction portal. Triggering portal wins.

## 4. Life, checkpoints, and game state
- Three lives at the beginning of a new run. Any contact with lethal hammers, spikes, active lasers, or falling below the kill plane consumes exactly one life.
- If lives remain, respawn at the latest activated checkpoint; before Section 3, respawn at the Section 1 starting spawn. Checkpoint 1 is the entrance to Section 3, Checkpoint 2 the entrance to Section 6. No other checkpoints.
- Use a death guard to prevent multiple life deductions from overlapping collision events. Restore player velocity/pose, hazard state as appropriate, and puzzle objects needed for a fair retry.
- On third death show Game Over and Restart; Restart resets lives, timer, checkpoint, puzzle and obstacle states, and player to Section 1.
- Win on entering final portal; show victory screen, completion time, and restart option. Timer can measure elapsed run time rather than impose an unrequested countdown; a countdown is optional and must not undermine playability.
- Pause stops gameplay, shows Resume and Restart, and releases cursor; starting/resuming locks cursor, and main menu uses unlocked cursor.

## 5. Main menu and HUD
- One main gameplay scene. On launch, show a centered VOIDBOUND title and Start button over a blurred view from the starting camera. Blur should be implemented with an affordable URP-compatible approach, with fallback if depth-of-field is impractical. Movement and hazards should not run before Start.
- HUD: crosshair, contextual interaction prompts, three-life indicator, section/progress label, and elapsed time. Show controls in tutorial and clear checkpoint notifications.
- Use UGUI/TextMeshPro if available, without introducing unnecessary packages.

## 6. Technical architecture (suggested, adaptable)
- GameManager: Menu/Playing/Paused/Dead-or-respawning/GameOver/Victory state transitions.
- PlayerController: Input System movement, camera look, jump, sprint, gravity, grounded detection, CharacterController.
- PlayerInteractor + IInteractable: raycast E interaction for switches or optional puzzle controls.
- PlayerDeathHandler, Checkpoint, KillVolume: single-death guard and respawn.
- PendulumHammer: predictable motion with collision-based hazard; use suitable Rigidbody/collider approach and ensure correct collision handling.
- MovingPlatform + PressurePlate: summon/return behavior and reliable player transport.
- SpikeHazard, LaserHazard: contact detection; laser active state gates lethality.
- PushableCrate, CratePressurePlate, PuzzleDoor: physics-driven two-plate puzzle.
- GoalPortal, HUDController, MenuController.
- Favor small focused reusable components and inspector-configurable speeds/distances. Avoid monolithic scripts.

## 7. Project structure
Suggested folders under Assets: Scenes, Scripts/Core, Scripts/Player, Scripts/Obstacles, Scripts/Interactions, Scripts/UI, Prefabs, Materials, Audio, Art. Create Facility.unity or Voidbound.unity as the main scene. Preserve existing SampleScene and IntegrationTest unless explicitly authorized to remove them. Add main scene to build list.

## 8. Phased implementation
**Phase 01 — Foundation:** Inspect existing Unity project; create main scene and minimal tutorial greybox; first-person controller; one test interaction; launch/start menu with blurred or graceful fallback scene view; basic HUD; short tutorial jump; prototype finish marker that does not represent final game victory. Implement core state scaffolding without building later obstacles. Check compilation and test Play Mode when possible.
**Phase 02:** Three swinging hammers with separated timing and kill contact.
**Phase 03:** Checkpoint 1 and pressure-plate moving platform, including ride behavior.
**Phase 04:** Spike path and timed laser corridor.
**Phase 05:** Checkpoint 2, two Rigidbody crates, dual plates, unlockable door.
**Phase 06:** Final gauntlet, goal portal, full lives/respawn, pause, win/loss/restart, and course reset integration.
**Phase 07:** Environment art, lighting, stars, fog, bloom, sound, and UI polish.
**Phase 08:** Full Play Mode regression, macOS standalone build, README, recording, ZIP, links and submission verification.

## 9. Validation and quality gates
For every phase: list created/modified files; check compilation; test relevant interactions in Play Mode where tool access permits; explicitly distinguish tested from untested behavior; save scene/assets; avoid unrelated changes; explain manual verification steps. Verify exactly one active AudioListener. Test keyboard/mouse on macOS, trigger/collision edge cases, player carrying on moving platforms, repeated respawns, puzzle reset, win/failure/restart, and standalone build. Do not claim tests passed without executing them.

## 10. Deliverables and academic integrity
Repository must include complete Unity project (Assets, Packages, ProjectSettings, necessary .meta files; do not commit Library). README: title, objective, rules, controls, Unity version, setup/play, members/contributions, credits/tutorial/AI assistance, gameplay video and build links. MP4 recording must demonstrate controls, obstacles, physics, success/failure, restart. Supply working Windows or macOS standalone build as complete ZIP; test it outside Editor and ensure public links work. Maintain meaningful Git history per team member and follow lecturer academic-integrity requirements.

## 11. Unresolved implementation choices
Exact course dimensions, jump distances, hammer timings, platform speed, crate mass, laser timings, HUD typography, music and starfield method are intentionally delegated to iterative prototyping and must be tuned for a fair, completable game. No extra checkpoints. Three lives. Instant death on hazard contact.
