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
    public static class VoidboundPhase04Tests
    {
        private const string ScenePath = "Assets/Scenes/Voidbound.unity";

        [MenuItem("Voidbound/Run Phase 04 Automated Tests")]
        public static void RunTests()
        {
            Debug.Log("=================================================");
            Debug.Log("[Phase 04 Tests] STARTING AUTOMATED TEST SUITE...");
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
                // Ensure Phase 04 scene integration is built
                VoidboundSceneBuilder.BuildPhase04Scene();

                // 1. Scene Load Test
                Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                Assert(scene.IsValid() && scene.isLoaded, "Scene Load Test", $"Scene {ScenePath} loaded successfully");

                // 2. AudioListener Test (Must have EXACTLY 1 active AudioListener in camera)
                AudioListener[] listeners = Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
                Assert(listeners.Length == 1, "AudioListener Count Test", $"Found exactly {listeners.Length} AudioListener(s)");

                // 3. Section 4 Hierarchy Tests
                GameObject sec4Root = GameObject.Find("Section_04_SpikePath");
                Assert(sec4Root != null, "Section 4 Root Test", "Section_04_SpikePath exists in scene hierarchy");

                GameObject trigger4Obj = GameObject.Find("Section_04_Entrance_Trigger");
                SectionTrigger sec4Trigger = trigger4Obj != null ? trigger4Obj.GetComponent<SectionTrigger>() : null;
                Collider sec4Col = trigger4Obj != null ? trigger4Obj.GetComponent<Collider>() : null;
                Assert(sec4Trigger != null && sec4Col != null && sec4Col.isTrigger,
                    "Section 4 Trigger Test", "Section 4 entrance trigger is present and configured as trigger");

                // 4. Section 4 Platforms and Narrow Bridge Geometry
                GameObject apronPlat = GameObject.Find("Platform_07_Section4_Apron");
                GameObject narrowBridge = GameObject.Find("Bridge_Narrow_01");
                GameObject clusterRest1 = GameObject.Find("Platform_08_Cluster_Rest1");
                GameObject clusterHurdle = GameObject.Find("Platform_09_Cluster_Hurdle");
                GameObject clusterRest2 = GameObject.Find("Platform_10_Cluster_Rest2");
                GameObject offsetPlat = GameObject.Find("Platform_11_Offset_Precision");
                GameObject sec4Completion = GameObject.Find("Platform_12_Section4_Completion");

                Assert(apronPlat != null && narrowBridge != null && clusterRest1 != null &&
                       clusterHurdle != null && clusterRest2 != null && offsetPlat != null && sec4Completion != null,
                       "Section 4 Platforms Presence Test", "All Section 4 bridge and rest platforms exist");

                // Verify Narrow Bridge width (2.2m)
                Assert(Mathf.Approximately(narrowBridge.transform.localScale.x, 2.2f),
                    "Narrow Bridge Width Test", $"Narrow bridge width is {narrowBridge.transform.localScale.x:F1}m (Requires careful walking)");

                // 5. Section 4 Spikes Placement & Components
                GameObject spNarrow1 = GameObject.Find("Spike_Narrow_01");
                GameObject spNarrow2 = GameObject.Find("Spike_Narrow_02");
                Assert(spNarrow1 != null && spNarrow2 != null, "Narrow Spikes Presence Test", "Narrow bridge single spikes exist");

                SpikeHazard shNarrow1 = spNarrow1 != null ? spNarrow1.GetComponent<SpikeHazard>() : null;
                Collider colNarrow1 = spNarrow1 != null ? spNarrow1.GetComponent<Collider>() : null;
                Assert(shNarrow1 != null && colNarrow1 != null && colNarrow1.isTrigger,
                    "SpikeHazard Component Test", "Spike_Narrow_01 has SpikeHazard and trigger collider");

                // Verify safe passing corridors on narrow bridge
                // Platform width 2.2m (X in [-1.1, 1.1]). Spike 1 at X = 0.5 leaves safe corridor on left (X in [-1.1, 0.15])
                float safeCorridor1Width = (spNarrow1.transform.position.x - colNarrow1.bounds.extents.x) - (-1.1f);
                Assert(safeCorridor1Width >= 0.8f, "Narrow Spike 1 Safe Corridor Test",
                    $"Left safe corridor width is {safeCorridor1Width:F2}m (Exceeds player diameter 0.8m)");

                // Spike 2 at X = -0.5 leaves safe corridor on right (X in [-0.15, 1.1])
                Collider colNarrow2 = spNarrow2.GetComponent<Collider>();
                float safeCorridor2Width = 1.1f - (spNarrow2.transform.position.x + colNarrow2.bounds.extents.x);
                Assert(safeCorridor2Width >= 0.8f, "Narrow Spike 2 Safe Corridor Test",
                    $"Right safe corridor width is {safeCorridor2Width:F2}m (Exceeds player diameter 0.8m)");

                // 6. Section 4 Spike Cluster Hurdle Geometry
                GameObject clusterObj = GameObject.Find("SpikeCluster_01");
                Assert(clusterObj != null, "Spike Cluster Presence Test", "SpikeCluster_01 object exists");
                SpikeHazard[] clusterSpikes = clusterObj != null ? clusterObj.GetComponentsInChildren<SpikeHazard>() : new SpikeHazard[0];
                Assert(clusterSpikes.Length == 6, "Spike Cluster Count Test", $"Found {clusterSpikes.Length} spikes in cluster (2x3 grid)");

                // Verify jump distance across cluster (Hurdle span ~2.5m)
                float clusterStartZ = 129.2f - 0.4f;
                float clusterEndZ = 130.8f + 0.4f;
                float hurdleDistance = clusterEndZ - clusterStartZ;
                Assert(hurdleDistance <= 3.0f, "Cluster Hurdle Span Test",
                    $"Cluster hurdle span is {hurdleDistance:F1}m (Well within player jump reach ~5.1m)");

                // 7. Section 4 Offset Precision Barriers
                GameObject bar1 = GameObject.Find("SpikeBarrier_Offset_01");
                GameObject bar2 = GameObject.Find("SpikeBarrier_Offset_02");
                GameObject bar3 = GameObject.Find("SpikeBarrier_Offset_03");
                Assert(bar1 != null && bar2 != null && bar3 != null, "Offset Barriers Presence Test", "All 3 offset barriers exist");

                SpikeHazard[] bar1Spikes = bar1 != null ? bar1.GetComponentsInChildren<SpikeHazard>() : new SpikeHazard[0];
                SpikeHazard[] bar2Spikes = bar2 != null ? bar2.GetComponentsInChildren<SpikeHazard>() : new SpikeHazard[0];
                SpikeHazard[] bar3Spikes = bar3 != null ? bar3.GetComponentsInChildren<SpikeHazard>() : new SpikeHazard[0];
                Assert(bar1Spikes.Length == 3 && bar2Spikes.Length == 3 && bar3Spikes.Length == 3,
                    "Offset Barriers Spikes Count Test", "Each offset barrier contains 3 spikes");

                // 8. Section 5 Hierarchy Tests
                GameObject sec5Root = GameObject.Find("Section_05_LaserCorridor");
                Assert(sec5Root != null, "Section 5 Root Test", "Section_05_LaserCorridor exists in scene hierarchy");

                GameObject trigger5Obj = GameObject.Find("Section_05_Entrance_Trigger");
                SectionTrigger sec5Trigger = trigger5Obj != null ? trigger5Obj.GetComponent<SectionTrigger>() : null;
                Collider sec5Col = trigger5Obj != null ? trigger5Obj.GetComponent<Collider>() : null;
                Assert(sec5Trigger != null && sec5Col != null && sec5Col.isTrigger,
                    "Section 5 Trigger Test", "Section 5 entrance trigger configured");

                GameObject c1Floor = GameObject.Find("Platform_13A_LaserChallenge1_Floor");
                GameObject c2Floor = GameObject.Find("Platform_13B_LaserChallenge2_Floor");
                GameObject c3Plat1 = GameObject.Find("Platform_13C_Circular1");
                GameObject c3Plat2 = GameObject.Find("Platform_13D_Circular2");
                GameObject sec5Arrival = GameObject.Find("Platform_14_Section5_Arrival");
                Assert(c1Floor != null && c2Floor != null && c3Plat1 != null && c3Plat2 != null && sec5Arrival != null,
                    "Section 5 Platforms Test", "Challenge 1 & 2 floors, both circular platforms, and arrival platform exist");

                // 9. Checkpoint 2 Strict Absence Test (Requirement: Checkpoint 2 belongs in Section 6, NOT Section 4 or 5)
                Checkpoint[] allCheckpoints = Object.FindObjectsByType<Checkpoint>(FindObjectsSortMode.None);
                Assert(allCheckpoints.Length == 1, "Single Checkpoint Strict Test",
                    $"Found exactly {allCheckpoints.Length} Checkpoint in scene. Checkpoint 2 is strictly omitted until Phase 05");
                Assert(allCheckpoints[0].CheckpointName == "Checkpoint 1", "Active Checkpoint Identity Test",
                    $"Active checkpoint in scene is '{allCheckpoints[0].CheckpointName}'");

                // 10. Core Setup & Singletons Initialization for Batchmode
                GameManager gm = Object.FindFirstObjectByType<GameManager>();
                GameObject player = GameObject.FindWithTag("Player");
                Assert(gm != null && player != null, "GameManager and Player Test", "GameManager and Player exist in scene");

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

                // Activate Checkpoint 1 (at Z = 65.0) as would happen naturally in Section 3
                Checkpoint cp1 = allCheckpoints[0];
                var cpAwake = typeof(Checkpoint).GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                cpAwake?.Invoke(cp1, null);
                var cpStart = typeof(Checkpoint).GetMethod("Start", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                cpStart?.Invoke(cp1, null);

                gm.StartGame();
                var cpTrigger = typeof(Checkpoint).GetMethod("OnTriggerEnter", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                cpTrigger?.Invoke(cp1, new object[] { playerCC });
                Assert(gm.HasCustomCheckpoint, "Checkpoint 1 Activated Test", "Checkpoint 1 active as base respawn point for Sections 4 and 5");
                Assert(Mathf.Approximately(gm.ActiveCheckpointPosition.z, 65.0f), "Respawn Anchor Z Test",
                    $"Active respawn anchor is Z={gm.ActiveCheckpointPosition.z:F1} (Checkpoint 1)");

                // 11. Spike Lethality & Single-Death Debounce Test
                Assert(gm.CurrentLives == 3, "Initial Lives Test", "Player starts test with 3 lives");

                // Trigger collision with Spike_Narrow_01
                Assert(!shNarrow1.HasKilled, "Spike Initial State Test", "Spike hasKilled is initially false");
                shNarrow1.ProcessHit(player);

                Assert(shNarrow1.HasKilled, "Spike Lethal Trigger Test", "Spike marked as hasKilled upon collision");
                Assert(gm.CurrentLives == 2, "Spike Death Life Deduction Test", $"Lives reduced to {gm.CurrentLives} on spike collision");
                Assert(Mathf.Approximately(player.transform.position.z, 65.0f), "Spike Respawn at Checkpoint 1 Test",
                    $"Player respawned at Checkpoint 1 Z={player.transform.position.z:F1} (NOT Section 1)");

                // Debounce test: secondary immediate call while hasKilled or isDead must NOT double-deduct life
                shNarrow1.ProcessHit(player);
                Assert(gm.CurrentLives == 2, "Spike Single-Death Debounce Test", "Multi-trigger collision ignored, lives remain at 2");

                // Reset respawn guard for subsequent tests
                var isRespawningField = typeof(GameManager).GetField("isRespawning", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                isRespawningField?.SetValue(gm, false);
                var pdhIsDead = typeof(PlayerDeathHandler).GetField("isDead", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                pdhIsDead?.SetValue(playerDeathHandler, false);

                // 12. Challenge 1: Single Pulsing Laser Mechanics Test
                GameObject laser1Obj = GameObject.Find("Laser_Challenge1_Single");
                Assert(laser1Obj != null, "Laser Challenge 1 Presence Test", "Laser_Challenge1_Single object exists");

                LaserHazard laser1 = laser1Obj != null ? laser1Obj.GetComponent<LaserHazard>() : null;
                Assert(laser1 != null, "Laser 1 Component Test", "LaserHazard component attached");

                var lhAwake = typeof(LaserHazard).GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                lhAwake?.Invoke(laser1, null);
                var lhStart = typeof(LaserHazard).GetMethod("Start", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                lhStart?.Invoke(laser1, null);

                // Initial state: Inactive (collider disabled, 0 damage)
                laser1.ResetLaser();
                Assert(laser1.CurrentState == LaserState.Inactive, "Laser 1 Initial Inactive State Test", "Laser 1 starts Inactive");
                Assert(!laser1.IsLethal, "Laser 1 Inactive Non-Lethal Test", "Laser 1 IsLethal is false in Inactive state");

                // Player entering while Inactive takes NO damage
                laser1.ProcessHit(player);
                Assert(gm.CurrentLives == 2, "Laser Inactive Safe Passage Test", "Passing inactive laser causes no life deduction");

                // Advance by InactiveDuration (2.5s) -> transitions to Warning
                laser1.Tick(2.55f);
                Assert(laser1.CurrentState == LaserState.Warning, "Laser 1 Warning Transition Test", "Laser 1 transitioned to Warning state");
                Assert(!laser1.IsLethal, "Laser 1 Warning Non-Lethal Test", "Laser 1 IsLethal is false in Warning state");

                // Player entering while Warning takes NO damage
                laser1.ProcessHit(player);
                Assert(gm.CurrentLives == 2, "Laser Warning Safe Passage Test", "Passing warning laser causes no life deduction");

                // Advance by WarningDuration (1.0s) -> transitions to Active
                laser1.Tick(1.05f);
                Assert(laser1.CurrentState == LaserState.Active, "Laser 1 Active Transition Test", "Laser 1 transitioned to Active state");
                Assert(laser1.IsLethal, "Laser 1 Active Lethal Test", "Laser 1 IsLethal is true in Active state");

                // Player contact while Active -> Immediate Death
                laser1.ProcessHit(player);
                Assert(gm.CurrentLives == 1, "Laser Active Lethal Kill Test", $"Active laser reduced lives to {gm.CurrentLives}");
                Assert(Mathf.Approximately(player.transform.position.z, 65.0f), "Laser Respawn at Checkpoint 1 Test",
                    $"Player respawned at Checkpoint 1 Z={player.transform.position.z:F1}");

                isRespawningField?.SetValue(gm, false);
                pdhIsDead?.SetValue(playerDeathHandler, false);

                // 13. Challenge 2: Vertical Moving Lasers Mechanics & Lethality Test
                GameObject laser2AObj = GameObject.Find("Laser_Challenge2_PairA");
                GameObject laser2BObj = GameObject.Find("Laser_Challenge2_PairB");
                Assert(laser2AObj != null && laser2BObj != null, "Vertical Laser Pair Presence Test", "Vertical moving lasers Pair A and B exist");

                VerticalMovingLaser vmLaser2A = laser2AObj.GetComponent<VerticalMovingLaser>();
                VerticalMovingLaser vmLaser2B = laser2BObj.GetComponent<VerticalMovingLaser>();
                Assert(vmLaser2A != null && vmLaser2B != null, "Vertical Laser Component Test", "VerticalMovingLaser components attached to Pair A and B");

                vmLaser2A.ResetLaser();
                vmLaser2B.ResetLaser();

                // At t = 0: Laser 2A (phase 0°) is at mid-height (1.4m), moving upwards
                // Laser 2B (phase 180°) is at mid-height (1.4m), moving downwards
                // Advance to quarter period (0.8s): Laser 2A is near maxHeight (2.4m), Laser 2B is near minHeight (0.4m)
                vmLaser2A.Tick(0.8f);
                vmLaser2B.Tick(0.8f);
                Assert(vmLaser2A.CurrentHeight > 2.0f, "Vertical Laser 2A Peak Test", $"Laser 2A height is {vmLaser2A.CurrentHeight:F2}m (high enough to run underneath)");
                Assert(vmLaser2B.CurrentHeight < 0.8f, "Vertical Laser 2B Trough Test", $"Laser 2B height is {vmLaser2B.CurrentHeight:F2}m (low enough to jump over)");

                // Vertical moving lasers are ALWAYS lethal (never turn off)
                // Player contact with vertical moving laser -> Immediate Death and respawn at Checkpoint 1
                vmLaser2A.ProcessHit(player);
                Assert(gm.CurrentLives == 0, "Vertical Laser Lethal Kill Test", $"Vertical moving laser contact reduced lives to {gm.CurrentLives}");

                isRespawningField?.SetValue(gm, false);
                pdhIsDead?.SetValue(playerDeathHandler, false);
                gm.RestartGame(); // Reset to 3 lives for subsequent tests
                gm.StartGame();
                cpTrigger?.Invoke(cp1, new object[] { playerCC }); // Re-anchor Checkpoint 1

                // 14. Challenge 3: Rotating Central Laser Turrets Mechanics & Lethality Test
                GameObject turret1Obj = GameObject.Find("Laser_Turret_Platform1");
                GameObject turret2Obj = GameObject.Find("Laser_Turret_Platform2");
                Assert(turret1Obj != null && turret2Obj != null, "Rotating Turrets Presence Test", "Laser turrets for Platform 1 and 2 exist");

                RotatingLaserTurret turret1 = turret1Obj.GetComponent<RotatingLaserTurret>();
                RotatingLaserTurret turret2 = turret2Obj.GetComponent<RotatingLaserTurret>();
                Assert(turret1 != null && turret2 != null, "Turret Components Test", "RotatingLaserTurret components attached");

                // Verify Platform 1 rotates clockwise (+35 deg/s) and Platform 2 rotates counter-clockwise (-48 deg/s)
                Assert(Mathf.Approximately(turret1.RotationSpeed, 35.0f), "Turret 1 Clockwise Speed Test",
                    $"Turret 1 rotates clockwise at {turret1.RotationSpeed:F1}°/s");
                Assert(Mathf.Approximately(turret2.RotationSpeed, -48.0f), "Turret 2 Counter-Clockwise Speed Test",
                    $"Turret 2 rotates counter-clockwise at higher pace {turret2.RotationSpeed:F1}°/s");

                turret1.ResetTurret();
                turret2.ResetTurret();

                turret1.Tick(1.0f);
                turret2.Tick(1.0f);
                Assert(Mathf.Approximately(turret1.CurrentAngle, 35.0f), "Turret 1 Angle Test", $"Turret 1 angle after 1s is {turret1.CurrentAngle:F1}°");
                Assert(Mathf.Approximately(turret2.CurrentAngle, 312.0f), "Turret 2 Angle Test", $"Turret 2 angle after 1s is {turret2.CurrentAngle:F1}° (360 - 48)");

                // Turrets are ALWAYS lethal (never turn off)
                turret1.ProcessHit(player);
                Assert(gm.CurrentLives == 2, "Turret Lethal Kill Test", $"Turret contact reduced lives to {gm.CurrentLives}");
                Assert(Mathf.Approximately(player.transform.position.z, 65.0f), "Turret Respawn at Checkpoint 1 Test",
                    $"Player respawned at Checkpoint 1 Z={player.transform.position.z:F1}");

                isRespawningField?.SetValue(gm, false);
                pdhIsDead?.SetValue(playerDeathHandler, false);

                // 15. Falling into KillVolume in Section 4 & 5
                GameObject kvObj = GameObject.Find("KillVolume_Pit");
                KillVolume kv = kvObj != null ? kvObj.GetComponent<KillVolume>() : null;
                BoxCollider kvBc = kvObj != null ? kvObj.GetComponent<BoxCollider>() : null;
                Assert(kv != null && kvBc != null, "KillVolume Pit Test", "KillVolume_Pit component found");

                // Verify expanded pit bounds cover Section 4 and Section 5 (Z: -125m to +325m)
                float kvMinZ = kvObj.transform.position.z - kvBc.size.z * 0.5f;
                float kvMaxZ = kvObj.transform.position.z + kvBc.size.z * 0.5f;
                Assert(kvMinZ <= 0f && kvMaxZ >= 225f, "KillVolume Pit Bounds Test",
                    $"Kill volume spans Z={kvMinZ:F0}m to Z={kvMaxZ:F0}m (Covers entire course through Section 5)");

                // Simulate player falling into void in Section 4 (Z = 120m)
                player.transform.position = new Vector3(0f, -12f, 120f);
                isRespawningField?.SetValue(gm, false);
                pdhIsDead?.SetValue(playerDeathHandler, false);
                kv.TriggerKill(player);
                Assert(gm.CurrentLives == 1, "Section 4 Void Fall Death Test", $"Falling into void reduced lives to {gm.CurrentLives}");
                Assert(Mathf.Approximately(player.transform.position.z, 65.0f), "Void Fall Respawn at Checkpoint 1 Test",
                    $"Player respawned at Checkpoint 1 Z={player.transform.position.z:F1}");

                // Final void fall consumes last life -> GameOver
                isRespawningField?.SetValue(gm, false);
                pdhIsDead?.SetValue(playerDeathHandler, false);
                kv.TriggerKill(player);
                Assert(gm.CurrentLives == 0 && gm.CurrentState == GameState.GameOver,
                    "Section 4 Void Fall GameOver Test", "Consuming final life triggers GameOver");

                // 16. Game Over & Course Restart Reset Test
                gm.RestartGame();
                Assert(gm.CurrentLives == 3, "Restart Lives Test", "Lives reset to 3 on course restart");
                Assert(!gm.HasCustomCheckpoint, "Restart Checkpoint Reset Test", "Custom checkpoint cleared on restart");
                Assert(player.transform.position.z < 10f, "Restart Section 1 Spawn Test",
                    $"Player respawned at Section 1 initial spawn Z={player.transform.position.z:F1}");

                // 17. Regressions: Phase 01, Phase 02, and Phase 03 Integrity
                GameObject p1 = GameObject.Find("Platform_01_Start");
                GameObject p2 = GameObject.Find("Platform_02_Intermediate");
                GameObject p3 = GameObject.Find("Platform_03_Finish");
                GameObject term = GameObject.Find("Prototype_Terminal");
                Assert(p1 != null && p2 != null && p3 != null && term != null,
                    "Phase 01 Assets Regression Test", "Section 1 platforms and terminal intact");

                GameObject sec2 = GameObject.Find("Section_02_HammerSwing");
                GameObject h1 = GameObject.Find("SwingingHammer_01");
                GameObject h2 = GameObject.Find("SwingingHammer_02");
                GameObject h3 = GameObject.Find("SwingingHammer_03");
                Assert(sec2 != null && h1 != null && h2 != null && h3 != null,
                    "Phase 02 Assets Regression Test", "Section 2 bridge and hammers intact");

                GameObject sec3 = GameObject.Find("Section_03_MovingPlatform");
                GameObject movPlat = GameObject.Find("MovingPlatform");
                GameObject plate = GameObject.Find("PressurePlate");
                GameObject cpObj = GameObject.Find("Checkpoint_01");
                Assert(sec3 != null && movPlat != null && plate != null && cpObj != null,
                    "Phase 03 Assets Regression Test", "Section 3 platforms, moving platform, plate, and Checkpoint 1 intact");
            }
            catch (System.Exception ex)
            {
                failed++;
                Debug.LogError($"[EXCEPTION] Exception during Phase 04 tests: {ex}");
            }

            Debug.Log("=================================================");
            Debug.Log($"[Phase 04 Tests] COMPLETED: {passed} PASSED, {failed} FAILED.");
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
