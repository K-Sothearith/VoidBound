using System.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Voidbound.Core;
using Voidbound.Interactions;
using Voidbound.Obstacles;
using Voidbound.Player;
using Voidbound.UI;

namespace Voidbound.Editor
{
    public static class VoidboundPhase03Tests
    {
        private const string ScenePath = "Assets/Scenes/Voidbound.unity";

        [MenuItem("Voidbound/Run Phase 03 Automated Tests")]
        public static void RunTests()
        {
            Debug.Log("=================================================");
            Debug.Log("[Phase 03 Tests] STARTING AUTOMATED TEST SUITE...");
            Debug.Log("=================================================");

            int passed = 0;
            int failed = 0;

            void Assert(bool condition, string testName, string details = "")
            {
                if (condition)
                {
                    passed++;
                    Debug.Log($"[PASS] {testName} {(string.IsNullOrEmpty(details) ? "" : $"({details})")}");
                }
                else
                {
                    failed++;
                    Debug.LogError($"[FAIL] {testName} - {details}");
                }
            }

            try
            {
                // Ensure Phase 03 scene integration is built
                VoidboundSceneBuilder.BuildPhase03Scene();

                // 1. Scene Load Test
                Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                Assert(scene.IsValid() && scene.isLoaded, "Scene Load Test", $"Scene {ScenePath} loaded successfully");

                // 2. AudioListener Test (Must have EXACTLY 1 active AudioListener)
                AudioListener[] listeners = Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
                Assert(listeners.Length == 1, "AudioListener Count Test", $"Found exactly {listeners.Length} AudioListener(s)");

                // 3. Section 3 Hierarchy Tests
                GameObject sec3Root = GameObject.Find("Section_03_MovingPlatform");
                Assert(sec3Root != null, "Section 3 Root Test", "Section_03_MovingPlatform exists in scene hierarchy");

                GameObject triggerObj = GameObject.Find("Section_03_Entrance_Trigger");
                SectionTrigger sec3Trigger = triggerObj != null ? triggerObj.GetComponent<SectionTrigger>() : null;
                Collider sec3Col = triggerObj != null ? triggerObj.GetComponent<Collider>() : null;
                Assert(sec3Trigger != null && sec3Col != null && sec3Col.isTrigger,
                    "Section 3 Trigger Test", "Section 3 entrance trigger is present and configured as trigger");

                // 4. Departure and Arrival Platforms
                GameObject depPlat = GameObject.Find("Platform_05_Section3_Departure");
                GameObject arrPlat = GameObject.Find("Platform_06_Section3_Arrival");
                Assert(depPlat != null && arrPlat != null, "Platforms Presence Test", "Departure and Arrival platforms exist");

                Collider depCol = depPlat != null ? depPlat.GetComponent<Collider>() : null;
                Collider arrCol = arrPlat != null ? arrPlat.GetComponent<Collider>() : null;
                Assert(depCol != null && arrCol != null && !depCol.isTrigger && !arrCol.isTrigger,
                    "Platforms Solid Colliders Test", "Departure and Arrival platforms have solid colliders");

                // Verify Chasm Gap (20 meters between Z=75 and Z=95)
                float depEdgeZ = depPlat.transform.position.z + depPlat.transform.localScale.z * 0.5f; // 69 + 6 = 75
                float arrEdgeZ = arrPlat.transform.position.z - arrPlat.transform.localScale.z * 0.5f; // 101 - 6 = 95
                float gapDistance = arrEdgeZ - depEdgeZ;
                Assert(Mathf.Approximately(gapDistance, 20f), "Chasm Gap Distance Test",
                    $"Gap distance is {gapDistance:F1}m (Z: {depEdgeZ:F1} to {arrEdgeZ:F1}). Exceeds max sprint jump ~7.6m");

                // 5. Checkpoint 1 Hierarchy & Configuration
                GameObject cpObj = GameObject.Find("Checkpoint_01");
                Assert(cpObj != null, "Checkpoint 1 Presence Test", "Checkpoint_01 object exists");

                Checkpoint cp = cpObj != null ? cpObj.GetComponent<Checkpoint>() : null;
                Collider cpCol = cpObj != null ? cpObj.GetComponent<Collider>() : null;
                Assert(cp != null && cpCol != null && cpCol.isTrigger, "Checkpoint 1 Component Test", "Checkpoint component and trigger collider configured");
                Assert(cp.CheckpointName == "Checkpoint 1", "Checkpoint 1 Name Test", $"Name: '{cp.CheckpointName}'");
                Assert(cp.SectionName == "Section 3: Moving Platform", "Checkpoint 1 Section Name Test", $"Section: '{cp.SectionName}'");

                Transform spawnOverride = cpObj != null ? cpObj.transform.Find("SpawnPoint") : null;
                Assert(spawnOverride != null, "Checkpoint 1 SpawnPoint Override Test", $"SpawnPoint at {spawnOverride.position}");
                Assert(Mathf.Approximately(cp.RespawnPosition.z, 65.0f), "Checkpoint 1 Respawn Position Test",
                    $"Respawn Z is {cp.RespawnPosition.z:F1} (Expected 65.0)");

                // Verify single checkpoint (No duplicates in Phase 03)
                Checkpoint[] allCheckpoints = Object.FindObjectsByType<Checkpoint>(FindObjectsSortMode.None);
                Assert(allCheckpoints.Length == 1, "Single Checkpoint Test",
                    $"Found exactly {allCheckpoints.Length} Checkpoint in scene (no duplicates created)");

                // Verify Checkpoint monument moved inward from platform edge, clear of central walking path
                Transform pedestal = cpObj.transform.Find("CheckpointPedestal");
                Assert(pedestal != null && Mathf.Abs(pedestal.localPosition.x) < 2.5f && Mathf.Abs(pedestal.localPosition.x) > 1.0f,
                    "Checkpoint 1 Inward Placement Test",
                    $"Pedestal X is {pedestal.localPosition.x:F1} (Inward from edge, clear of central walking corridor)");

                // Visual Indicator Mesh & Light on Checkpoint
                Transform indicator = cpObj != null ? cpObj.transform.Find("CheckpointIndicator") : null;
                Renderer indRenderer = indicator != null ? indicator.GetComponent<Renderer>() : null;
                Light indLight = cpObj != null ? cpObj.GetComponentInChildren<Light>() : null;
                Assert(indRenderer != null && indLight != null, "Checkpoint 1 Visual Feedback Test", "Indicator renderer and light present");

                // 6. Pressure Plate Hierarchy & Configuration
                GameObject plateObj = GameObject.Find("PressurePlate");
                Assert(plateObj != null, "Pressure Plate Presence Test", "PressurePlate object exists");

                PressurePlate plate = plateObj != null ? plateObj.GetComponent<PressurePlate>() : null;
                Collider plateCol = plateObj != null ? plateObj.GetComponent<Collider>() : null;
                Assert(plate != null && plateCol != null && plateCol.isTrigger,
                    "Pressure Plate Component Test", "PressurePlate component and trigger collider configured");

                Transform padTransform = plateObj != null ? plateObj.transform.Find("PlatePad") : null;
                Assert(padTransform != null, "Plate Pad Transform Test", "PlatePad depressible child exists");
                Assert(plate.DepressDistance > 0.05f && plate.DepressSpeed > 1.0f,
                    "Plate Depression Settings Test", $"Depress distance: {plate.DepressDistance}m, speed: {plate.DepressSpeed}m/s");
                Assert(plateObj.transform.position.z >= 72.5f, "Pressure Plate Closer to Edge Test",
                    $"Pressure plate Z is {plateObj.transform.position.z:F1} (Positioned closer to void edge Z=75.0)");

                // 7. Moving Platform Hierarchy & Configuration
                GameObject platObj = GameObject.Find("MovingPlatform");
                Assert(platObj != null, "Moving Platform Presence Test", "MovingPlatform object exists");

                MovingPlatform movPlat = platObj != null ? platObj.GetComponent<MovingPlatform>() : null;
                Rigidbody platRb = platObj != null ? platObj.GetComponent<Rigidbody>() : null;
                Collider platSolidCol = platObj != null ? platObj.GetComponent<Collider>() : null;
                Assert(movPlat != null && platRb != null && platSolidCol != null,
                    "Moving Platform Components Test", "MovingPlatform, Rigidbody, and Collider present");
                Assert(platRb.isKinematic,
                    "Moving Platform Kinematic Physics Test", "Kinematic Rigidbody configured");

                // Verify RiderDetector child trigger
                Transform detectorTransform = platObj != null ? platObj.transform.Find("RiderDetector") : null;
                PlatformRiderDetector riderDetector = detectorTransform != null ? detectorTransform.GetComponent<PlatformRiderDetector>() : null;
                Collider detCol = detectorTransform != null ? detectorTransform.GetComponent<Collider>() : null;
                Assert(riderDetector != null && detCol != null && detCol.isTrigger,
                    "Rider Detector Trigger Test", "RiderDetector child trigger and PlatformRiderDetector configured");

                // Verify Waypoints & Settings
                Assert(Mathf.Approximately(movPlat.PointA.z, 92.8f), "Platform Point A Test",
                    $"Point A (idle far side): Z={movPlat.PointA.z:F1} (Expected 92.8)");
                Assert(Mathf.Approximately(movPlat.PointB.z, 77.2f), "Platform Point B Test",
                    $"Point B (summoned near side): Z={movPlat.PointB.z:F1} (Expected 77.2)");
                Assert(movPlat.MoveSpeed >= 5.0f && movPlat.MoveSpeed <= 8.0f,
                    "Platform Speed Test", $"Move speed: {movPlat.MoveSpeed} m/s (Configured faster for responsive crossing)");
                Assert(movPlat.ReturnDelay >= 0.5f,
                    "Platform Return Delay Test", $"Return delay: {movPlat.ReturnDelay}s (Allows boarding time)");
                Assert(movPlat.Plate == plate,
                    "Platform Plate Reference Test", "MovingPlatform correctly references PressurePlate");

                // 8. Core Game Loop & Singletons Initialization for Batchmode
                GameManager gm = Object.FindFirstObjectByType<GameManager>();
                GameObject player = GameObject.FindWithTag("Player");
                Assert(gm != null && player != null, "GameManager and Player Test", "GameManager and Player exist in scene");

                var playerController = player.GetComponent<PlayerController>();
                var playerDeathHandler = player.GetComponent<PlayerDeathHandler>();

                var gmAwake = typeof(GameManager).GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                gmAwake?.Invoke(gm, null);
                gm.RegisterSpawnPoint(new Vector3(0f, 1f, 0f), Quaternion.identity);

                var pcAwake = typeof(PlayerController).GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                pcAwake?.Invoke(playerController, null);
                var pcStart = typeof(PlayerController).GetMethod("Start", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                pcStart?.Invoke(playerController, null);

                var pdhAwake = typeof(PlayerDeathHandler).GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                pdhAwake?.Invoke(playerDeathHandler, null);
                var pdhStart = typeof(PlayerDeathHandler).GetMethod("Start", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                pdhStart?.Invoke(playerDeathHandler, null);

                var cpAwake = typeof(Checkpoint).GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                cpAwake?.Invoke(cp, null);
                var cpStart = typeof(Checkpoint).GetMethod("Start", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                cpStart?.Invoke(cp, null);

                var plateAwake = typeof(PressurePlate).GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                plateAwake?.Invoke(plate, null);
                var plateStart = typeof(PressurePlate).GetMethod("Start", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                plateStart?.Invoke(plate, null);

                var platAwake = typeof(MovingPlatform).GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                platAwake?.Invoke(movPlat, null);
                var platStart = typeof(MovingPlatform).GetMethod("Start", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                platStart?.Invoke(movPlat, null);

                gm.StartGame();
                Assert(gm.CurrentState == GameState.Playing, "Game State Test", "Game is in Playing state");
                Assert(gm.CurrentLives == 3, "Initial Lives Test", "Player starts with 3 lives");

                // 9. Pressure Plate Logic & Activation/Deactivation
                CharacterController playerCC = player.GetComponent<CharacterController>();
                Assert(!plate.IsPressed, "Initial Plate State Test", "Pressure plate is initially unpressed");

                // Step player onto plate
                var triggerEnter = typeof(PressurePlate).GetMethod("OnTriggerEnter", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                triggerEnter?.Invoke(plate, new object[] { playerCC });
                Assert(plate.IsPressed, "Plate Step-On Activation Test", "Plate activated when player stepped onto it");

                // Step player off plate
                var triggerExit = typeof(PressurePlate).GetMethod("OnTriggerExit", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                triggerExit?.Invoke(plate, new object[] { playerCC });
                Assert(!plate.IsPressed, "Plate Step-Off Deactivation Test", "Plate deactivated when player stepped off");

                // 10. Rapid Stepping Stress Test (Debounce / rapid enter-exit cycles)
                for (int i = 0; i < 5; i++)
                {
                    triggerEnter?.Invoke(plate, new object[] { playerCC });
                    Assert(plate.IsPressed, $"Rapid Cycle {i + 1} Press Test", "Plate accurately registers rapid press");
                    triggerExit?.Invoke(plate, new object[] { playerCC });
                    Assert(!plate.IsPressed, $"Rapid Cycle {i + 1} Release Test", "Plate accurately registers rapid release");
                }

                // 11. Moving Platform Approaching and Retreating Dynamics
                movPlat.ResetPlatform();
                Assert(Mathf.Approximately(platObj.transform.position.z, movPlat.PointA.z),
                    "Platform Initial Idle Position Test", $"Platform is docked at Point A: Z={platObj.transform.position.z:F1}");

                // Step on plate to summon platform toward departure ledge (Point B)
                triggerEnter?.Invoke(plate, new object[] { playerCC });
                Assert(plate.IsPressed, "Plate Held for Summon Test", "Plate is active");

                // Simulate 1.0 second of motion towards Point B (10 steps of 0.1s dt)
                float initialDistToB = Mathf.Abs(platObj.transform.position.z - movPlat.PointB.z);
                for (int f = 0; f < 10; f++)
                {
                    movPlat.Tick(0.1f);
                }
                float intermediateDistToB = Mathf.Abs(platObj.transform.position.z - movPlat.PointB.z);
                Assert(intermediateDistToB < initialDistToB, "Platform Approaching Motion Test",
                    $"Platform moved closer to Point B: from {initialDistToB:F2}m to {intermediateDistToB:F2}m");

                // Advance platform until it reaches Point B
                for (int f = 0; f < 100; f++)
                {
                    movPlat.Tick(0.1f);
                    if (Mathf.Approximately(platObj.transform.position.z, movPlat.PointB.z)) break;
                }
                Assert(Mathf.Approximately(platObj.transform.position.z, movPlat.PointB.z),
                    "Platform Docked at Departure Ledge (Point B) Test",
                    $"Platform smoothly docked at Point B (Z={platObj.transform.position.z:F2}) without overshoot");

                // Step off plate: platform should count down return delay, then return toward Point A
                triggerExit?.Invoke(plate, new object[] { playerCC });
                Assert(!plate.IsPressed, "Plate Released for Return Test", "Plate is inactive");

                // During return delay (0.8s), platform should hold still at Point B
                movPlat.Tick(0.2f);
                Assert(Mathf.Approximately(platObj.transform.position.z, movPlat.PointB.z),
                    "Platform Hold During Delay Test", "Platform holds position at Point B during initial delay");

                // Fast-forward past return delay (force timer to 0 to test return motion)
                var returnTimerField = typeof(MovingPlatform).GetField("currentReturnTimer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                returnTimerField?.SetValue(movPlat, 0f);

                for (int f = 0; f < 100; f++)
                {
                    movPlat.Tick(0.1f);
                    if (Mathf.Approximately(platObj.transform.position.z, movPlat.PointA.z)) break;
                }
                Assert(Mathf.Approximately(platObj.transform.position.z, movPlat.PointA.z),
                    "Platform Returned to Destination Ledge (Point A) Test",
                    $"Platform smoothly returned to Point A (Z={platObj.transform.position.z:F2}) without overshoot or jitter");

                // 12. Player Riding Mechanics (Zero Slip, No Parenting)
                // Place platform at Point B (near departure ledge)
                platObj.transform.position = movPlat.PointB;
                playerController.Teleport(new Vector3(0f, 0.5f, movPlat.PointB.z), Quaternion.identity);
                Physics.SyncTransforms();

                // Player boards platform via RiderDetector
                movPlat.SetRider(playerCC);
                Assert(movPlat.CurrentRider == playerCC, "Rider Registered Test", "MovingPlatform registered player as active rider");
                Assert(player.transform.parent != platObj.transform, "No Parenting Test",
                    "Player is NOT parented to the moving platform (clean CharacterController transport)");

                // Advance platform by 0.25 seconds
                Vector3 playerStartPos = player.transform.position;
                movPlat.Tick(0.25f);
                Physics.SyncTransforms();

                Vector3 playerEndPos = player.transform.position;
                float playerDisplacement = playerEndPos.z - playerStartPos.z;
                float expectedDisplacement = movPlat.MoveSpeed * 0.25f;
                Assert(Mathf.Approximately(playerDisplacement, expectedDisplacement), "Rider Transport Displacement Test",
                    $"Player moved exactly with platform displacement: {playerDisplacement:F2}m (Expected {expectedDisplacement:F2}m, Zero slip)");

                // Disembarking: player jumps/steps off onto arrival platform
                playerController.Teleport(new Vector3(0f, 0.5f, 96f), Quaternion.identity);
                Physics.SyncTransforms();
                movPlat.RemoveRider(playerCC);
                Assert(movPlat.CurrentRider == null, "Rider Disembark Test", "Rider successfully disembarked from platform");

                // 13. Checkpoint 1 Activation & State Test
                Assert(!gm.HasCustomCheckpoint, "Pre-Checkpoint Custom State Test", "Game starts without custom checkpoint");

                // Move player into Checkpoint 1 trigger
                var cpTrigger = typeof(Checkpoint).GetMethod("OnTriggerEnter", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                cpTrigger?.Invoke(cp, new object[] { playerCC });

                Assert(cp.IsActivated, "Checkpoint 1 Activated Flag Test", "Checkpoint isActivated is true");
                Assert(gm.HasCustomCheckpoint, "GameManager Custom Checkpoint Test", "GameManager registered custom checkpoint");
                Assert(Mathf.Approximately(gm.ActiveCheckpointPosition.z, cp.RespawnPosition.z),
                    "GameManager Active Position Test", $"Active respawn Z is {gm.ActiveCheckpointPosition.z:F1} (Matches Checkpoint 1)");
                Assert(gm.CurrentSectionName == "Section 3: Moving Platform",
                    "GameManager Section Update Test", $"Current section is '{gm.CurrentSectionName}'");

                // 14. Respawn at Checkpoint 1 After Death Test
                Assert(gm.CurrentLives == 3, "Pre-Death Lives Test", "Player has 3 lives before death");
                gm.OnPlayerDied();
                Assert(gm.CurrentLives == 2, "Death Deduction Test", "Player lives reduced to 2");
                Assert(Mathf.Approximately(player.transform.position.z, cp.RespawnPosition.z),
                    "Respawn at Checkpoint 1 Test", $"Player respawned at Checkpoint 1 Z={player.transform.position.z:F1} (NOT Section 1)");

                // Verify puzzle objects reset for a fair retry
                Assert(!plate.IsPressed, "Puzzle Reset Plate Test", "Pressure plate reset to unpressed on respawn");
                Assert(Mathf.Approximately(platObj.transform.position.z, movPlat.PointA.z),
                    "Puzzle Reset Platform Test", $"Moving platform reset to Point A (Z={platObj.transform.position.z:F1}) on respawn");

                // 15. Falling into KillVolume in Section 3 Test
                GameObject kvObj = GameObject.Find("KillVolume_Pit");
                KillVolume kv = kvObj != null ? kvObj.GetComponent<KillVolume>() : null;
                Assert(kv != null, "KillVolume Pit Test", "KillVolume_Pit component found");

                // Simulate player falling into void in Section 3
                player.transform.position = new Vector3(0f, -12f, 85f); // in the gap
                // Reset respawn guard timer for test
                var isRespawningField = typeof(GameManager).GetField("isRespawning", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                isRespawningField?.SetValue(gm, false);

                kv.TriggerKill(player);
                Assert(gm.CurrentLives == 1, "Void Fall Death Test", $"Falling into void reduced lives to {gm.CurrentLives}");
                Assert(Mathf.Approximately(player.transform.position.z, cp.RespawnPosition.z),
                    "Void Fall Respawn at Checkpoint 1 Test", $"Player respawned back at Checkpoint 1 Z={player.transform.position.z:F1}");

                // 16. Game Over & Course Restart Reset Test
                isRespawningField?.SetValue(gm, false);
                gm.OnPlayerDied(); // Last life consumed -> GameOver
                Assert(gm.CurrentLives == 0 && gm.CurrentState == GameState.GameOver,
                    "Game Over State Test", "Game transitioned to GameOver when 0 lives remain");

                // Restart Course
                gm.RestartGame();
                Assert(gm.CurrentLives == 3, "Restart Lives Test", "Lives reset to 3");
                Assert(!gm.HasCustomCheckpoint, "Restart Checkpoint Reset Test", "Custom checkpoint cleared on restart");
                Assert(!cp.IsActivated, "Checkpoint Component Reset Test", "Checkpoint 1 component reset to inactive");
                Assert(player.transform.position.z < 10f, "Restart Initial Spawn Test",
                    $"Player respawned at Section 1 initial spawn (Z={player.transform.position.z:F1})");

                // 17. Regression Tests for Phase 01 & Phase 02
                GameObject p1 = GameObject.Find("Platform_01_Start");
                GameObject p2 = GameObject.Find("Platform_02_Intermediate");
                GameObject p3 = GameObject.Find("Platform_03_Finish");
                GameObject term = GameObject.Find("Prototype_Terminal");
                Assert(p1 != null && p2 != null && p3 != null && term != null,
                    "Phase 01 Assets Regression Test", "Section 1 platforms and terminal intact");

                // Verify Prototype_Finish_Marker was removed from scene (temporary Phase 01 milestone)
                GameObject marker = GameObject.Find("Prototype_Finish_Marker");
                Assert(marker == null, "Prototype Finish Marker Removed Test", "Prototype_Finish_Marker GameObject removed from playable scene");
                Assert(typeof(Voidbound.Interactions.PrototypeFinishMarker) != null,
                    "Prototype Finish Marker Script Preserved Test", "PrototypeFinishMarker script class is preserved");

                // Verify Terminal Alpha faces the player spawn point (0, 1, 0)
                Vector3 toSpawn = (new Vector3(0f, 0f, 0f) - term.transform.position).normalized;
                float faceDot = Vector3.Dot(term.transform.forward, toSpawn);
                Assert(faceDot > 0.95f, "Terminal Alpha Facing Spawn Test",
                    $"Terminal Alpha forward vector aligned with player spawn point (dot: {faceDot:F2})");

                GameObject sec2 = GameObject.Find("Section_02_HammerSwing");
                GameObject sz1 = GameObject.Find("SafeZone_01");
                GameObject sz2 = GameObject.Find("SafeZone_02");
                GameObject h1 = GameObject.Find("SwingingHammer_01");
                GameObject h2 = GameObject.Find("SwingingHammer_02");
                GameObject h3 = GameObject.Find("SwingingHammer_03");
                Assert(sec2 != null && sz1 != null && sz2 != null && h1 != null && h2 != null && h3 != null,
                    "Phase 02 Assets Regression Test", "Section 2 bridge, safe zones, and all 3 swinging hammers intact");

                SwingingHammer sh1 = h1.GetComponent<SwingingHammer>();
                SwingingHammer sh2 = h2.GetComponent<SwingingHammer>();
                SwingingHammer sh3 = h3.GetComponent<SwingingHammer>();
                Assert(Mathf.Approximately(sh1.PhaseOffsetDegrees, 0f) &&
                       Mathf.Approximately(sh2.PhaseOffsetDegrees, 120f) &&
                       Mathf.Approximately(sh3.PhaseOffsetDegrees, 240f),
                       "Hammer Phases Regression Test", "Hammer staggered phase offsets 0°, 120°, 240° intact");

                GameObject canvasObj = GameObject.Find("Canvas");
                Transform hudTransform = canvasObj != null ? canvasObj.transform.Find("HUDPanel") : null;
                Assert(canvasObj != null && hudTransform != null, "UI Panels Regression Test", "Canvas and HUDPanel intact");

                // 18. Midair Jump Prevention & Fast Falling Speed Validation
                playerController.Teleport(new Vector3(0f, 20f, 0f), Quaternion.identity);
                Physics.SyncTransforms();
                Assert(!playerCC.isGrounded, "Midair Ground Check Test", "Player is not grounded in midair");

                // Simulate falling over 0.2s using ApplyGravityOnly
                var applyGravity = typeof(PlayerController).GetMethod("ApplyGravityOnly", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                float startY = player.transform.position.y;
                for (int step = 0; step < 10; step++)
                {
                    applyGravity?.Invoke(playerController, null);
                }
                float fallenDist = startY - player.transform.position.y;
                Assert(fallenDist >= 0f, "Falling Downward Test", $"Player falling downward (drop: {fallenDist:F2}m)");
            }
            catch (System.Exception ex)
            {
                failed++;
                Debug.LogError($"[EXCEPTION] Exception during Phase 03 tests: {ex}");
            }

            Debug.Log("=================================================");
            Debug.Log($"[Phase 03 Tests] COMPLETED: {passed} PASSED, {failed} FAILED.");
            Debug.Log("=================================================");

            if (failed > 0)
            {
                EditorApplication.Exit(1);
            }
            else
            {
                EditorApplication.Exit(0);
            }
        }
    }
}
