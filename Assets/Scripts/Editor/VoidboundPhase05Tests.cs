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
    public static class VoidboundPhase05Tests
    {
        private const string ScenePath = "Assets/Scenes/Voidbound.unity";

        [MenuItem("Voidbound/Run Phase 05 Automated Tests")]
        public static void RunTests()
        {
            Debug.Log("=================================================");
            Debug.Log("[Phase 05 Tests] STARTING AUTOMATED TEST SUITE...");
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
                // Ensure Phase 05 scene integration is built
                VoidboundSceneBuilder.BuildPhase05Scene();

                // 1. Scene Load Test
                Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                Assert(scene.IsValid() && scene.isLoaded, "Scene Load Test", $"Scene {ScenePath} loaded successfully");

                // 2. AudioListener Test (Must have EXACTLY 1 active AudioListener in camera)
                AudioListener[] listeners = Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
                Assert(listeners.Length == 1, "AudioListener Count Test", $"Found exactly {listeners.Length} AudioListener(s)");

                // 3. Exactly Two Checkpoints Total in the Entire Game
                Checkpoint[] allCheckpoints = Object.FindObjectsByType<Checkpoint>(FindObjectsSortMode.None);
                Assert(allCheckpoints.Length == 2, "Exact Checkpoint Count Test",
                    $"Found exactly {allCheckpoints.Length} Checkpoints in scene (Preserves requirement of exactly 2 checkpoints)");

                // Find Checkpoint 1 and Checkpoint 2
                Checkpoint cp1 = null;
                Checkpoint cp2 = null;
                foreach (var cp in allCheckpoints)
                {
                    if (cp.CheckpointName == "Checkpoint 1") cp1 = cp;
                    else if (cp.CheckpointName == "Checkpoint 2") cp2 = cp;
                }

                Assert(cp1 != null && cp2 != null, "Checkpoint Identification Test",
                    $"Both Checkpoint 1 (found: {cp1 != null}) and Checkpoint 2 (found: {cp2 != null}) exist");

                // 4. Checkpoint Relocation Test: Checkpoint 1 relocated to Section 4 entrance (Z ≈ 107.5)
                GameObject sec3Root = GameObject.Find("Section_03_MovingPlatform");
                Transform oldSec3Cp1 = sec3Root != null ? sec3Root.transform.Find("Checkpoint_01") : null;
                Assert(oldSec3Cp1 == null, "Checkpoint 1 Absent From Section 3 Test",
                    "Section 3 entrance no longer contains Checkpoint 1");

                GameObject sec4Root = GameObject.Find("Section_04_SpikePath");
                Transform sec4Cp1 = sec4Root != null ? sec4Root.transform.Find("Checkpoint_01") : null;
                Assert(sec4Cp1 != null, "Checkpoint 1 Present In Section 4 Test",
                    "Checkpoint_01 is placed at Section 4 entrance");

                Assert(cp1 != null && Mathf.Approximately(cp1.RespawnPosition.z, 107.5f), "Checkpoint 1 Relocation Coordinate Test",
                    $"Checkpoint 1 respawn Z is {cp1?.RespawnPosition.z:F1}m (Section 4 entrance, approx Z=107)");
                Assert(cp1 != null && cp1.SectionName == "Section 4: Spike Path", "Checkpoint 1 Section Name Test",
                    $"Checkpoint 1 section is '{cp1?.SectionName}'");

                // 5. Checkpoint 2 Test: Checkpoint 2 placed at Section 6 entrance (Z ≈ 228.0)
                GameObject sec6Root = GameObject.Find("Section_06_PhysicsChamber");
                Assert(sec6Root != null, "Section 6 Root Test", "Section_06_PhysicsChamber exists in scene hierarchy");

                Transform sec6Cp2 = sec6Root != null ? sec6Root.transform.Find("Checkpoint_02") : null;
                Assert(sec6Cp2 != null, "Checkpoint 2 Present In Section 6 Test",
                    "Checkpoint_02 is placed at Section 6 entrance");

                Assert(cp2 != null && Mathf.Approximately(cp2.RespawnPosition.z, 228.0f), "Checkpoint 2 Coordinate Test",
                    $"Checkpoint 2 respawn Z is {cp2?.RespawnPosition.z:F1}m (Section 6 entrance, Z=228)");
                Assert(cp2 != null && cp2.SectionName == "Section 6: Physics Chamber", "Checkpoint 2 Section Name Test",
                    $"Checkpoint 2 section is '{cp2?.SectionName}'");

                // Checkpoint 2 Visual Feedback & Monument
                Transform cp2Pedestal = cp2 != null ? cp2.transform.Find("CheckpointPedestal") : null;
                Transform cp2Indicator = cp2 != null ? cp2.transform.Find("CheckpointIndicator") : null;
                Transform cp2Light = cp2 != null ? cp2.transform.Find("CheckpointLight") : null;
                Assert(cp2Pedestal != null && cp2Indicator != null && cp2Light != null, "Checkpoint 2 Monument Feedback Test",
                    "Checkpoint 2 includes pedestal, glowing indicator diamond, and light");

                // 6. Section 6 Entrance Trigger & Platforms
                GameObject sec6TriggerObj = GameObject.Find("Section_06_Entrance_Trigger");
                SectionTrigger sec6Trigger = sec6TriggerObj != null ? sec6TriggerObj.GetComponent<SectionTrigger>() : null;
                Collider sec6Col = sec6TriggerObj != null ? sec6TriggerObj.GetComponent<Collider>() : null;
                Assert(sec6Trigger != null && sec6Col != null && sec6Col.isTrigger, "Section 6 Trigger Test",
                    "Section 6 entrance trigger is present and configured as trigger");

                GameObject sec6EntrancePlat = GameObject.Find("Platform_15_Section6_Entrance");
                GameObject chamberFloor = GameObject.Find("Chamber_Floor");
                GameObject wallLeft = GameObject.Find("Wall_Left");
                GameObject wallRight = GameObject.Find("Wall_Right");
                GameObject sec6ExitPlat = GameObject.Find("Platform_16_Section6_Exit");
                Assert(sec6EntrancePlat != null && chamberFloor != null && wallLeft != null && wallRight != null && sec6ExitPlat != null,
                    "Section 6 Geometry Test", "Entrance platform, chamber floor, side walls, and exit platform exist");

                // 7. Pushable Crates Configuration & Physics Tuning
                GameObject crateLeftObj = GameObject.Find("Crate_Left");
                GameObject crateRightObj = GameObject.Find("Crate_Right");
                Assert(crateLeftObj != null && crateRightObj != null, "Pushable Crates Presence Test",
                    "Both Crate_Left and Crate_Right exist in Section 6");

                PushableCrate crateLeft = crateLeftObj != null ? crateLeftObj.GetComponent<PushableCrate>() : null;
                PushableCrate crateRight = crateRightObj != null ? crateRightObj.GetComponent<PushableCrate>() : null;
                Rigidbody rbLeft = crateLeftObj != null ? crateLeftObj.GetComponent<Rigidbody>() : null;
                Rigidbody rbRight = crateRightObj != null ? crateRightObj.GetComponent<Rigidbody>() : null;

                crateLeft?.InitializeCrate();
                crateRight?.InitializeCrate();

                Assert(crateLeft != null && crateRight != null, "PushableCrate Component Test",
                    "PushableCrate components attached to both crates");
                Assert(rbLeft != null && !rbLeft.isKinematic && rbRight != null && !rbRight.isKinematic,
                    "Rigidbody Physics Dynamic Test", "Both crates have non-kinematic Rigidbodies");

                // Verify physics tuning: mass, damping, rotation freeze constraints
                Assert(Mathf.Approximately(rbLeft.mass, 25.0f) && Mathf.Approximately(rbRight.mass, 25.0f),
                    "Crate Mass Tuning Test", $"Crate mass tuned to {rbLeft?.mass}kg");
                Assert(rbLeft.linearDamping >= 3.0f && rbRight.linearDamping >= 3.0f,
                    "Crate Linear Damping Test", $"Crate linear damping is {rbLeft?.linearDamping}");
                Assert((rbLeft.constraints & RigidbodyConstraints.FreezeRotationX) != 0 &&
                       (rbLeft.constraints & RigidbodyConstraints.FreezeRotationZ) != 0,
                    "Crate Rotation Constraints Test", "Crates freeze rotation X and Z to avoid tipping over");

                // Verify crates start on opposite sides (left X < 0, right X > 0)
                Assert(crateLeftObj.transform.position.x < -3.0f && crateRightObj.transform.position.x > 3.0f,
                    "Crates Opposite Sides Test",
                    $"Left crate X={crateLeftObj.transform.position.x:F1}, Right crate X={crateRightObj.transform.position.x:F1}");

                // 8. Crate Pressure Plates Configuration
                GameObject plateLeftObj = GameObject.Find("Plate_Left");
                GameObject plateRightObj = GameObject.Find("Plate_Right");
                Assert(plateLeftObj != null && plateRightObj != null, "Pressure Plates Presence Test",
                    "Both Plate_Left and Plate_Right exist in Section 6");

                CratePressurePlate plateLeft = plateLeftObj != null ? plateLeftObj.GetComponent<CratePressurePlate>() : null;
                CratePressurePlate plateRight = plateRightObj != null ? plateRightObj.GetComponent<CratePressurePlate>() : null;
                Assert(plateLeft != null && plateRight != null, "CratePressurePlate Component Test",
                    "CratePressurePlate components attached to both plates");

                Assert(!plateLeft.IsPressed && !plateRight.IsPressed, "Plates Initial State Test",
                    "Both pressure plates start unpressed / inactive");

                Assert(plateLeft.DepressDistance > 0.04f && plateLeft.DepressSpeed > 0f, "Plate Depression Tuning Test",
                    $"Plate depress distance: {plateLeft.DepressDistance:F2}m, speed: {plateLeft.DepressSpeed:F1}m/s");

                // 9. Locked Exit Door Configuration
                GameObject doorObj = GameObject.Find("Door_Exit_Puzzle");
                Assert(doorObj != null, "Exit Door Object Test", "Door_Exit_Puzzle object exists");

                PuzzleDoor puzzleDoor = doorObj != null ? doorObj.GetComponent<PuzzleDoor>() : null;
                puzzleDoor?.InitializeDoorPositions();
                Assert(puzzleDoor != null, "PuzzleDoor Component Test", "PuzzleDoor component attached to door");

                Assert(!puzzleDoor.IsUnlocked && !puzzleDoor.IsOpen, "Door Initial Locked State Test",
                    "Exit door starts LOCKED and CLOSED");

                Transform leafL = doorObj.transform.Find("DoorLeaf_Left");
                Transform leafR = doorObj.transform.Find("DoorLeaf_Right");
                Assert(leafL != null && leafR != null, "Door Leaves Test", "Dual sliding blast door leaves present");

                Collider colL = leafL != null ? leafL.GetComponent<Collider>() : null;
                Collider colR = leafR != null ? leafR.GetComponent<Collider>() : null;
                Assert(colL != null && colL.enabled && colR != null && colR.enabled, "Door Colliders Blocking Test",
                    "Door colliders enabled to physically block passage while locked");

                // 10. Puzzle Logic: Door Unlocks ONLY When Both Plates Are Simultaneously Active
                // Test single plate left activation
                plateLeft.SetPressedState(true);
                puzzleDoor.CheckPlateConditions();
                Assert(!puzzleDoor.IsUnlocked, "Plate Left Alone Test",
                    "Door remains LOCKED when only Plate Left is pressed");

                // Test release plate left, press plate right alone
                plateLeft.SetPressedState(false);
                plateRight.SetPressedState(true);
                puzzleDoor.CheckPlateConditions();
                Assert(!puzzleDoor.IsUnlocked, "Plate Right Alone Test",
                    "Door remains LOCKED when only Plate Right is pressed");

                // Test SIMULTANEOUS activation of both plates
                plateLeft.SetPressedState(true);
                puzzleDoor.CheckPlateConditions();
                Assert(puzzleDoor.IsUnlocked && puzzleDoor.IsOpen, "Dual Plate Simultaneous Unlock Test",
                    "Door successfully UNLOCKS and OPENS when both plates are simultaneously active!");

                // Advance door opening animation
                puzzleDoor.Tick(2.0f);
                Assert(leafL.localPosition.x < -3.0f && leafR.localPosition.x > 3.0f, "Dual Sliding Door Open Motion Test",
                    $"Left leaf slid to {leafL.localPosition.x:F2}, Right leaf slid to {leafR.localPosition.x:F2}");

                // Test door reset
                puzzleDoor.ResetDoor();
                Assert(!puzzleDoor.IsUnlocked && !puzzleDoor.IsOpen, "Door Reset Test",
                    "Door resets to locked and closed");
                plateLeft.ResetPlate();
                plateRight.ResetPlate();

                // 11. Physics Pushing Behavior
                Vector3 initialLeftPos = crateLeft.InitialPosition;
                crateLeft.ApplyPushForce(Vector3.forward, 3.0f);
                Assert(rbLeft.linearVelocity.z > 1.5f, "Crate Push Velocity Test",
                    $"Crate received forward push velocity: {rbLeft.linearVelocity.z:F1} m/s");

                // 12. Lost Crate Reset & Anti-Softlock (Void Recovery & Terminal Reset)
                // Test Void Fall Auto-Recovery
                crateLeftObj.transform.position = new Vector3(-4.5f, -6.0f, 240.0f); // dropped into void below -5.0f
                var fixedUpdateMethod = typeof(PushableCrate).GetMethod("FixedUpdate", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                fixedUpdateMethod?.Invoke(crateLeft, null);

                Assert(Mathf.Approximately(crateLeftObj.transform.position.z, initialLeftPos.z) &&
                       Mathf.Approximately(crateLeftObj.transform.position.y, initialLeftPos.y),
                    "Void Fall Crate Recovery Test",
                    $"Lost crate automatically recovered to origin {crateLeftObj.transform.position}");
                Assert(rbLeft.linearVelocity == Vector3.zero, "Recovered Crate Velocity Zeroed Test",
                    "Recovered crate linear velocity is zeroed");

                // Test Crate Reset Terminal
                GameObject terminalObj = GameObject.Find("Crate_Reset_Terminal");
                Assert(terminalObj != null, "Crate Reset Terminal Test", "Crate_Reset_Terminal exists in Section 6");

                CrateResetTerminal resetTerminal = terminalObj != null ? terminalObj.GetComponent<CrateResetTerminal>() : null;
                Assert(resetTerminal != null && resetTerminal is IInteractable, "Terminal IInteractable Test",
                    "CrateResetTerminal implements IInteractable");

                // Displace both crates
                crateLeftObj.transform.position = new Vector3(-2.0f, 0.65f, 245.0f);
                crateRightObj.transform.position = new Vector3(2.0f, 0.65f, 245.0f);
                resetTerminal.ResetCrates();

                Assert(Mathf.Approximately(crateLeftObj.transform.position.z, initialLeftPos.z) &&
                       Mathf.Approximately(crateRightObj.transform.position.z, crateRight.InitialPosition.z),
                    "Terminal Manual Crate Reset Test",
                    "Interacting with CrateResetTerminal restored both crates to starting positions");

                // 13. Checkpoint Activation and Respawn Progression Integration
                GameManager gm = Object.FindFirstObjectByType<GameManager>();
                GameObject player = GameObject.FindWithTag("Player");
                Assert(gm != null && player != null, "GameManager and Player Test", "GameManager and Player present");

                var playerController = player.GetComponent<PlayerController>();
                var playerDeathHandler = player.GetComponent<PlayerDeathHandler>();
                CharacterController playerCC = player.GetComponent<CharacterController>();

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

                var cp1Awake = typeof(Checkpoint).GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                cp1Awake?.Invoke(cp1, null);
                var cp1Start = typeof(Checkpoint).GetMethod("Start", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                cp1Start?.Invoke(cp1, null);

                var cp2Awake = typeof(Checkpoint).GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                cp2Awake?.Invoke(cp2, null);
                var cp2Start = typeof(Checkpoint).GetMethod("Start", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                cp2Start?.Invoke(cp2, null);

                gm.StartGame();

                // (A) Initial state: Death before Checkpoint 1 respawns at Section 1 (Z ≈ 0)
                Assert(!gm.HasCustomCheckpoint, "Initial Run No Custom Checkpoint Test", "Game starts without custom checkpoint");
                gm.OnPlayerDied();
                Assert(Mathf.Approximately(player.transform.position.z, 0f), "Pre-Checkpoint Death Respawn Test",
                    $"Player respawned at Section 1 spawn Z={player.transform.position.z:F1}");

                // Reset respawn guard
                var isRespawningField = typeof(GameManager).GetField("isRespawning", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                isRespawningField?.SetValue(gm, false);
                var pdhIsDead = typeof(PlayerDeathHandler).GetField("isDead", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                pdhIsDead?.SetValue(playerDeathHandler, false);

                // (B) Crossing Checkpoint 1 at Section 4 Entrance (Z ≈ 107.5)
                string lastNotif = "";
                gm.OnNotification += (msg, dur) => lastNotif = msg;

                var cpTrigger = typeof(Checkpoint).GetMethod("OnTriggerEnter", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                cpTrigger?.Invoke(cp1, new object[] { playerCC });

                Assert(cp1.IsActivated, "Checkpoint 1 Activated Flag Test", "Checkpoint 1 isActivated is true");
                Assert(gm.HasCustomCheckpoint, "GameManager Custom Checkpoint 1 Test", "GameManager registered custom checkpoint");
                Assert(Mathf.Approximately(gm.ActiveCheckpointPosition.z, 107.5f), "GameManager Checkpoint 1 Active Z Test",
                    $"Active checkpoint Z is {gm.ActiveCheckpointPosition.z:F1}m");
                Assert(lastNotif == "Checkpoint Activated: Checkpoint 1", "Checkpoint 1 Notification Test",
                    $"Notification fired: '{lastNotif}'");

                // Death in Section 4/5 respawns at Checkpoint 1
                gm.OnPlayerDied();
                Assert(Mathf.Approximately(player.transform.position.z, 107.5f), "Respawn At Checkpoint 1 Test",
                    $"Player respawned at Checkpoint 1 Z={player.transform.position.z:F1} (Section 4 entrance)");

                isRespawningField?.SetValue(gm, false);
                pdhIsDead?.SetValue(playerDeathHandler, false);
                // Ensure player has lives remaining for Checkpoint 2 respawn test
                var currentLivesField = typeof(GameManager).GetField("currentLives", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                currentLivesField?.SetValue(gm, 3);

                // (C) Crossing Checkpoint 2 at Section 6 Entrance (Z ≈ 228.0)
                cpTrigger?.Invoke(cp2, new object[] { playerCC });

                Assert(cp2.IsActivated, "Checkpoint 2 Activated Flag Test", "Checkpoint 2 isActivated is true");
                Assert(Mathf.Approximately(gm.ActiveCheckpointPosition.z, 228.0f), "GameManager Checkpoint 2 Active Z Test",
                    $"Active checkpoint updated to Z={gm.ActiveCheckpointPosition.z:F1}m (Checkpoint 2)");
                Assert(lastNotif == "Checkpoint Activated: Checkpoint 2", "Checkpoint 2 Notification Test",
                    $"Notification fired: '{lastNotif}'");

                // Death in Section 6 respawns at Checkpoint 2
                gm.OnPlayerDied();
                Assert(Mathf.Approximately(player.transform.position.z, 228.0f), "Respawn At Checkpoint 2 Test",
                    $"Player respawned at Checkpoint 2 Z={player.transform.position.z:F1} (Section 6 entrance)");

                isRespawningField?.SetValue(gm, false);
                pdhIsDead?.SetValue(playerDeathHandler, false);

                // (D) Restart resets everything cleanly
                gm.RestartGame();
                Assert(!gm.HasCustomCheckpoint, "Restart Custom Checkpoint Cleared Test", "Custom checkpoint cleared on restart");
                Assert(!cp1.IsActivated && !cp2.IsActivated, "Restart Checkpoints Inactive Test", "Both checkpoints reset to inactive");
                Assert(Mathf.Approximately(player.transform.position.z, 0f), "Restart Section 1 Spawn Test",
                    $"Player returned to Section 1 spawn Z={player.transform.position.z:F1}");
                Assert(!puzzleDoor.IsUnlocked && !puzzleDoor.IsOpen, "Restart Door Locked Test",
                    "Door reset to locked on restart");
                Assert(!plateLeft.IsPressed && !plateRight.IsPressed, "Restart Plates Unpressed Test",
                    "Plates reset to unpressed on restart");

                // 14. Regressions: Sections 1-5 Intact
                GameObject p1 = GameObject.Find("Platform_01_Start");
                GameObject p2 = GameObject.Find("Platform_02_Intermediate");
                GameObject p3 = GameObject.Find("Platform_03_Finish");
                GameObject term = GameObject.Find("Prototype_Terminal");
                GameObject sec2Obj = GameObject.Find("Section_02_HammerSwing");
                GameObject sec3Obj = GameObject.Find("Section_03_MovingPlatform");
                GameObject sec4Obj = GameObject.Find("Section_04_SpikePath");
                GameObject sec5Obj = GameObject.Find("Section_05_LaserCorridor");

                Assert(p1 != null && p2 != null && p3 != null && term != null, "Section 1 Tutorial Regression Test", "Section 1 platforms and terminal intact");
                Assert(sec2Obj != null, "Section 2 Hammer Swing Regression Test", "Section 2 is intact");
                Assert(sec3Obj != null, "Section 3 Moving Platform Regression Test", "Section 3 is intact");
                Assert(sec4Obj != null, "Section 4 Spike Path Regression Test", "Section 4 is intact");
                Assert(sec5Obj != null, "Section 5 Laser Corridor Regression Test", "Section 5 is intact");

                // Verify KillVolume pit covers up to Section 6 exit
                GameObject kvPit = GameObject.Find("KillVolume_Pit");
                BoxCollider kvBc = kvPit != null ? kvPit.GetComponent<BoxCollider>() : null;
                Assert(kvBc != null && kvBc.size.z >= 500f, "Kill Volume Extended Coverage Test",
                    $"Kill volume size Z is {kvBc?.size.z:F1}m, covering through Section 6");

                Debug.Log("=================================================");
                Debug.Log($"[Phase 05 Tests] COMPLETED: {passed} PASSED, {failed} FAILED.");
                Debug.Log("=================================================");
            }
            catch (System.Exception ex)
            {
                failed++;
                Debug.LogError($"[Phase 05 Tests] EXCEPTION ENCOUNTERED: {ex.Message}\n{ex.StackTrace}");
            }

            if (failed > 0)
            {
                EditorApplication.Exit(1);
            }
        }
    }
}
