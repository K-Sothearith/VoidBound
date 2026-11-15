using System.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Voidbound.Core;
using Voidbound.Interactions;
using Voidbound.Obstacles;
using Voidbound.Player;
using Voidbound.UI;

namespace Voidbound.Editor
{
    public static class VoidboundPhase06Tests
    {
        private const string ScenePath = "Assets/Scenes/Voidbound.unity";

        [MenuItem("Voidbound/Run Phase 06 Automated Tests")]
        public static void RunTests()
        {
            Debug.Log("=================================================");
            Debug.Log("[Phase 06 Tests] STARTING AUTOMATED TEST SUITE...");
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
                // Ensure Phase 06 scene is built and saved
                VoidboundSceneBuilder.BuildPhase06Scene();

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

                Checkpoint cp1 = null;
                Checkpoint cp2 = null;
                foreach (var cp in allCheckpoints)
                {
                    if (cp.CheckpointName == "Checkpoint 1") cp1 = cp;
                    else if (cp.CheckpointName == "Checkpoint 2") cp2 = cp;
                }

                Assert(cp1 != null && cp2 != null, "Checkpoint Identification Test",
                    $"Checkpoint 1 (found: {cp1 != null}) and Checkpoint 2 (found: {cp2 != null}) both exist");
                Assert(cp1 != null && Mathf.Approximately(cp1.RespawnPosition.z, 107.5f), "Checkpoint 1 Coordinate Test",
                    $"Checkpoint 1 Z is {cp1?.RespawnPosition.z:F1}m (Section 4 entrance)");
                Assert(cp2 != null && Mathf.Approximately(cp2.RespawnPosition.z, 228.0f), "Checkpoint 2 Coordinate Test",
                    $"Checkpoint 2 Z is {cp2?.RespawnPosition.z:F1}m (Section 6 entrance)");

                // 4. Section 7 Has NO Checkpoints
                GameObject envRoot = GameObject.Find("Environment");
                Assert(envRoot != null, "Environment Root Test", "Environment GameObject exists in scene");

                Transform sec7Tr = envRoot != null ? envRoot.transform.Find("Section_07_FinalGauntlet") : null;
                Assert(sec7Tr != null, "Section 7 Hierarchy Root Test", "Section_07_FinalGauntlet exists under Environment");

                Checkpoint[] sec7Checkpoints = sec7Tr != null ? sec7Tr.GetComponentsInChildren<Checkpoint>() : new Checkpoint[0];
                Assert(sec7Checkpoints.Length == 0, "Section 7 Checkpoint Absence Test",
                    $"Found {sec7Checkpoints.Length} Checkpoints in Section 7 (Must be 0 so Section 7 deaths respawn at Checkpoint 2)");

                // 5. Section 7 Entrance Trigger Test
                Transform sec7TriggerTr = sec7Tr != null ? sec7Tr.Find("Section_07_Entrance_Trigger") : null;
                Assert(sec7TriggerTr != null, "Section 7 Entrance Trigger Test", "Section_07_Entrance_Trigger exists");
                SectionTrigger sec7Trigger = sec7TriggerTr != null ? sec7TriggerTr.GetComponent<SectionTrigger>() : null;
                Assert(sec7Trigger != null && sec7Trigger.SectionName == "Section 7: Final Gauntlet", "Section 7 Trigger Name Test",
                    $"Trigger section name is '{sec7Trigger?.SectionName}'");

                // 6. Kill Volume Pit Expansion Test
                GameObject kvObj = GameObject.Find("KillVolume_Pit");
                Assert(kvObj != null, "Kill Volume Pit Test", "KillVolume_Pit exists in scene");
                BoxCollider kvCol = kvObj != null ? kvObj.GetComponent<BoxCollider>() : null;
                Assert(kvCol != null && kvCol.size.z >= 600f, "Kill Volume Pit Size Test",
                    $"Kill volume size.z is {kvCol?.size.z:F1}m (covers beyond Z=387m extraction terminus)");
                float kvMaxZ = kvObj != null && kvCol != null ? kvObj.transform.position.z + kvCol.size.z * 0.5f : 0f;
                Assert(kvMaxZ >= 450f, "Kill Volume Max Z Reach Test",
                    $"Kill volume reaches Z={kvMaxZ:F1}m (fully encloses Section 7 void)");

                // 7. Challenge 1: Timed Laser Corridor Test
                Transform c1Floor = sec7Tr != null ? sec7Tr.Find("Platform_17B_LaserRunway") : null;
                Assert(c1Floor != null, "Challenge 1 Runway Test", "Platform_17B_LaserRunway exists");

                LaserHazard[] gauntletLasers = sec7Tr != null ? sec7Tr.GetComponentsInChildren<LaserHazard>() : new LaserHazard[0];
                Assert(gauntletLasers.Length == 3, "Challenge 1 Laser Count Test",
                    $"Found exactly {gauntletLasers.Length} timed LaserHazards in Challenge 1");

                for (int i = 0; i < gauntletLasers.Length; i++)
                {
                    LaserHazard lh = gauntletLasers[i];
                    Assert(lh.InstantKill, $"Laser {lh.gameObject.name} Lethality Test", "Laser is instant-kill lethal");
                    Assert(lh.ActiveDuration > 0f && lh.WarningDuration > 0f && lh.InactiveDuration > 0f,
                        $"Laser {lh.gameObject.name} Timing Parameters Test",
                        $"Active={lh.ActiveDuration}s, Warn={lh.WarningDuration}s, Inactive={lh.InactiveDuration}s");
                }

                // Verify Challenge 1 Safe Interval / Stagger
                Assert(gauntletLasers.Length >= 3 && gauntletLasers[1].InitialDelay > gauntletLasers[0].InitialDelay,
                    "Challenge 1 Laser Stagger Test",
                    $"Laser 1 delay={gauntletLasers[0].InitialDelay}s, Laser 2 delay={gauntletLasers[1].InitialDelay}s (provides traversable safe intervals)");

                // 8. Safe Haven 1 (Rest Platform 18) Test
                Transform rest1 = sec7Tr != null ? sec7Tr.Find("Platform_18_RestHaven1") : null;
                Assert(rest1 != null, "Safe Haven 1 Test", "Platform_18_RestHaven1 safe landing zone exists between Challenge 1 and 2");

                // 9. Challenge 2: Three Moving Platforms above the Void Test
                MovingPlatform[] sec7MovingPlats = sec7Tr != null ? sec7Tr.GetComponentsInChildren<MovingPlatform>() : new MovingPlatform[0];
                Assert(sec7MovingPlats.Length == 3, "Challenge 2 Moving Platform Count Test",
                    $"Found exactly {sec7MovingPlats.Length} MovingPlatforms in Challenge 2");

                for (int i = 0; i < sec7MovingPlats.Length; i++)
                {
                    MovingPlatform mp = sec7MovingPlats[i];
                    Assert(mp.AutoOscillate, $"Platform {mp.gameObject.name} Autonomous Oscillation Flag Test",
                        "AutoOscillate is enabled");
                    Assert(mp.PointA != mp.PointB, $"Platform {mp.gameObject.name} Distinct Endpoints Test",
                        $"PointA={mp.PointA}, PointB={mp.PointB}");
                    Assert(mp.MoveSpeed >= 3.0f && mp.MoveSpeed <= 8.0f, $"Platform {mp.gameObject.name} Speed Test",
                        $"Speed={mp.MoveSpeed}m/s (well within playable range)");

                    Rigidbody mprb = mp.GetComponent<Rigidbody>();
                    Assert(mprb != null && mprb.isKinematic, $"Platform {mp.gameObject.name} Kinematic Rigidbody Test",
                        "Platform has kinematic Rigidbody");

                    PlatformRiderDetector det = mp.GetComponentInChildren<PlatformRiderDetector>();
                    Assert(det != null, $"Platform {mp.gameObject.name} Rider Detector Test",
                        "PlatformRiderDetector child trigger is attached");
                }

                // 10. Moving Platform Autonomous Simulation Test
                MovingPlatform testMp = sec7MovingPlats.Length > 0 ? sec7MovingPlats[0] : null;
                if (testMp != null)
                {
                    testMp.ResetPlatform();
                    Vector3 startPos = testMp.transform.position;
                    Assert(testMp.transform.position == testMp.PointA, "Moving Platform Reset to Point A Test",
                        $"Position is {testMp.transform.position}");

                    // Tick platform forward by 0.5s
                    testMp.Tick(0.5f);
                    Assert(testMp.transform.position != startPos, "Moving Platform Autonomous Movement Test",
                        $"Platform moved from {startPos} to {testMp.transform.position}");
                    Assert(testMp.FrameDelta.sqrMagnitude > 0f, "Moving Platform FrameDelta Test",
                        $"FrameDelta is {testMp.FrameDelta}");

                    // Reset platform
                    testMp.ResetPlatform();
                    Assert(testMp.transform.position == testMp.PointA, "Moving Platform Post-Test Reset Test",
                        "Platform returned to Point A on Reset");
                }

                // 11. Safe Haven 2 (Rest Platform 19) Test
                Transform rest2 = sec7Tr != null ? sec7Tr.Find("Platform_19_RestHaven2") : null;
                Assert(rest2 != null, "Safe Haven 2 Test", "Platform_19_RestHaven2 safe landing zone exists between Challenge 2 and 3");

                // 12. Challenge 3: Collapsing Platforms Test
                CollapsingPlatform[] collapsingPlats = sec7Tr != null ? sec7Tr.GetComponentsInChildren<CollapsingPlatform>() : new CollapsingPlatform[0];
                Assert(collapsingPlats.Length == 4, "Challenge 3 Collapsing Platform Count Test",
                    $"Found exactly {collapsingPlats.Length} CollapsingPlatforms in Challenge 3");

                for (int i = 0; i < collapsingPlats.Length; i++)
                {
                    CollapsingPlatform cp = collapsingPlats[i];
                    Assert(cp.CurrentState == CollapsingPlatformState.Idle, $"Platform {cp.gameObject.name} Initial State Test",
                        "Initial state is Idle");
                    Assert(cp.IsSolidColliderEnabled, $"Platform {cp.gameObject.name} Initial Solid Collider Test",
                        "Solid collider is enabled initially");
                    Assert(cp.WarnDuration > 0.4f && cp.WarnDuration <= 1.5f, $"Platform {cp.gameObject.name} Warn Duration Test",
                        $"WarnDuration={cp.WarnDuration}s");
                    Assert(cp.FallSpeed >= 10f, $"Platform {cp.gameObject.name} Fall Speed Test",
                        $"FallSpeed={cp.FallSpeed}m/s");
                }

                // 13. Collapsing Platform Simulation & Reset Lifecycle Test
                CollapsingPlatform testCp = collapsingPlats.Length > 0 ? collapsingPlats[0] : null;
                if (testCp != null)
                {
                    testCp.ResetPlatform();
                    Vector3 cpInitialPos = testCp.InitialPosition;

                    // Trigger collapse warning
                    testCp.TriggerCollapse();
                    Assert(testCp.CurrentState == CollapsingPlatformState.Warning, "Collapsing Platform Warning State Test",
                        "State transitioned to Warning");

                    // Tick partially during warning phase
                    testCp.Tick(0.2f);
                    Assert(testCp.CurrentState == CollapsingPlatformState.Warning, "Collapsing Platform Sustained Warning Test",
                        "State remains Warning before warnDuration expires");
                    Assert(testCp.IsSolidColliderEnabled, "Collapsing Platform Solid Collider During Warning Test",
                        "Solid collider remains active during warning shake so player can still jump");

                    // Tick past warn duration (e.g. 1.0s total)
                    testCp.Tick(0.8f);
                    Assert(testCp.CurrentState == CollapsingPlatformState.Collapsed, "Collapsing Platform Collapsed State Test",
                        "State transitioned to Collapsed");
                    Assert(!testCp.IsSolidColliderEnabled, "Collapsing Platform Collider Disabled On Collapse Test",
                        "Solid collider is disabled on collapse so player falls through");

                    // Tick while collapsed: should plunge downwards
                    Vector3 collapsedPosBefore = testCp.transform.position;
                    testCp.Tick(0.5f);
                    Assert(testCp.transform.position.y < collapsedPosBefore.y, "Collapsing Platform Plunge Test",
                        $"Platform plunged from Y={collapsedPosBefore.y:F2} to Y={testCp.transform.position.y:F2}");

                    // Reset platform
                    testCp.ResetPlatform();
                    Assert(testCp.CurrentState == CollapsingPlatformState.Idle, "Collapsing Platform Reset State Test",
                        "State returned to Idle on ResetPlatform");
                    Assert(testCp.transform.position == cpInitialPos, "Collapsing Platform Reset Position Test",
                        $"Position returned to initial {cpInitialPos}");
                    Assert(testCp.IsSolidColliderEnabled, "Collapsing Platform Solid Collider Restored Test",
                        "Solid collider re-enabled on ResetPlatform");
                }

                // 14. Extraction Sanctuary & Extraction Portal Test
                Transform sanctuary = sec7Tr != null ? sec7Tr.Find("Platform_20_ExtractionSanctuary") : null;
                Assert(sanctuary != null, "Extraction Sanctuary Platform Test", "Platform_20_ExtractionSanctuary exists");

                ExtractionPortal portal = sec7Tr != null ? sec7Tr.GetComponentInChildren<ExtractionPortal>() : null;
                Assert(portal != null, "Extraction Portal Component Test", "ExtractionPortal exists in Section 7");
                BoxCollider portalTrigger = portal != null ? portal.GetComponent<BoxCollider>() : null;
                Assert(portalTrigger != null && portalTrigger.isTrigger, "Extraction Portal Trigger Test",
                    "Extraction portal has active trigger BoxCollider");

                // 15. GameManager Victory and Death Tracking Test
                GameManager gm = Object.FindFirstObjectByType<GameManager>();
                Assert(gm != null, "GameManager Instance Test", "GameManager exists in scene");

                if (gm != null)
                {
                    gm.StartGame();
                    Assert(gm.CurrentState == GameState.Playing, "GameManager StartGame State Test",
                        "GameManager state is Playing");
                    Assert(gm.CurrentLives == 3, "Initial Lives Test", $"Lives = {gm.CurrentLives}");
                    Assert(gm.DeathCount == 0, "Initial Death Count Test", $"DeathCount = {gm.DeathCount}");

                    // Test death count tracking
                    gm.OnPlayerDied();
                    Assert(gm.DeathCount == 1, "Death Count Increment Test", $"DeathCount = {gm.DeathCount}");
                    Assert(gm.CurrentLives == 2, "Lives Decrement Test", $"Lives remaining = {gm.CurrentLives}");

                    // Checkpoint 2 respawn verification:
                    // Set Checkpoint 2 active
                    if (cp2 != null)
                    {
                        gm.SetCheckpoint(cp2.RespawnPosition, cp2.RespawnRotation, cp2.CheckpointName);
                        Assert(gm.HasCustomCheckpoint, "Checkpoint 2 Activation Test", "Custom checkpoint is set");
                        Assert(gm.ActiveCheckpointPosition == cp2.RespawnPosition, "Checkpoint 2 Position Tracking Test",
                            $"Active checkpoint position matches Checkpoint 2 ({cp2.RespawnPosition})");

                        // Trigger death in Section 7: should respawn at Checkpoint 2
                        gm.ResetRespawnGuard();
                        gm.OnPlayerDied();
                        Assert(gm.ActiveCheckpointPosition == cp2.RespawnPosition, "Section 7 Death Respawn at Checkpoint 2 Test",
                            $"Respawn position remains Checkpoint 2 at Z={gm.ActiveCheckpointPosition.z:F1}m");
                        Assert(gm.DeathCount == 2, "Second Death Count Test", $"DeathCount = {gm.DeathCount}");
                    }

                    // Test Victory Trigger
                    portal.ResetPortal();
                    portal.TriggerExtraction();
                    Assert(portal.HasTriggeredVictory, "Portal HasTriggeredVictory Test", "Portal flagged victory");
                    Assert(gm.CurrentState == GameState.Victory, "GameManager Victory State Test",
                        "GameManager state transitioned to GameState.Victory");
                    Assert(Time.timeScale == 0f, "Victory TimeScale Paused Test", "Time.timeScale paused (0.0)");
                }

                // 16. Victory UI Test
                UIManager ui = Object.FindFirstObjectByType<UIManager>();
                Assert(ui != null, "UIManager Instance Test", "UIManager exists in scene");

                if (ui != null && gm != null)
                {
                    gm.OnGameStateChanged += ui.HandleGameStateChanged;
                    ui.HandleGameStateChanged(GameState.Victory);
                    Assert(ui.VictoryPanel != null && ui.VictoryPanel.activeSelf, "Victory Panel Active Test",
                        "VictoryPanel is active on GameState.Victory");
                    Assert(ui.VictoryTitleText != null && ui.VictoryTitleText.text == "EXTRACTION SUCCESSFUL",
                        "Victory Title Text Test", $"Title text is '{ui.VictoryTitleText?.text}'");
                    Assert(ui.VictoryTimeText != null && ui.VictoryTimeText.text.StartsWith("COMPLETION TIME:"),
                        "Victory Time Text Test", $"Time text is '{ui.VictoryTimeText?.text}'");
                    Assert(ui.VictoryDeathsText != null && ui.VictoryDeathsText.text.StartsWith("DEATHS:"),
                        "Victory Deaths Text Test", $"Deaths text is '{ui.VictoryDeathsText?.text}'");
                    Assert(ui.VictoryRestartButton != null, "Victory Restart Button Test", "Victory Restart button exists");
                    Assert(ui.VictoryQuitButton != null, "Victory Quit Button Test", "Victory Quit button exists");
                }

                // 17. Full Course Restart Integration Test
                if (gm != null)
                {
                    gm.RestartGame();
                    Assert(gm.CurrentState == GameState.Playing, "RestartGame State Test",
                        "GameManager state returned to Playing");
                    Assert(gm.CurrentLives == 3, "RestartGame Lives Restored Test", $"Lives restored to {gm.CurrentLives}");
                    Assert(gm.DeathCount == 0, "RestartGame Deaths Reset Test", $"DeathCount reset to {gm.DeathCount}");
                    Assert(gm.ElapsedTime == 0f, "RestartGame Timer Reset Test", $"ElapsedTime reset to {gm.ElapsedTime}");
                    Assert(!gm.HasCustomCheckpoint, "RestartGame Checkpoint Reset Test", "Custom checkpoints cleared");
                    Assert(Time.timeScale == 1.0f, "RestartGame TimeScale Restored Test", "Time.timeScale restored to 1.0");

                    // UI should update to Playing HUD
                    if (ui != null)
                    {
                        Assert(ui.VictoryPanel != null && !ui.VictoryPanel.activeSelf, "Victory Panel Deactivated On Restart Test",
                            "VictoryPanel is hidden after RestartGame");
                        Assert(ui.HudPanel != null && ui.HudPanel.activeSelf, "HUD Panel Active On Restart Test",
                            "HUDPanel is active after RestartGame");
                    }
                }

                // 18. Idempotent Scene Regeneration Test
                // Running BuildPhase06Scene a second time should succeed without duplicating Section 7 or throwing errors
                VoidboundSceneBuilder.BuildPhase06Scene();
                Scene reloadedScene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                Assert(reloadedScene.IsValid(), "Idempotent Rebuild Scene Validity Test", "Scene valid after second build");

                GameObject reloadedEnv = GameObject.Find("Environment");
                int sec7Count = 0;
                if (reloadedEnv != null)
                {
                    for (int i = 0; i < reloadedEnv.transform.childCount; i++)
                    {
                        if (reloadedEnv.transform.GetChild(i).name == "Section_07_FinalGauntlet")
                        {
                            sec7Count++;
                        }
                    }
                }
                Assert(sec7Count == 1, "Section 7 Idempotency Test", $"Found exactly {sec7Count} Section_07_FinalGauntlet under Environment");

                Checkpoint[] postRebuildCps = Object.FindObjectsByType<Checkpoint>(FindObjectsSortMode.None);
                Assert(postRebuildCps.Length == 2, "Idempotent Checkpoint Count Test",
                    $"Found exactly {postRebuildCps.Length} Checkpoints post-rebuild");

                // 19. Sections 1 through 6 Regression Verification
                Assert(GameObject.Find("Platform_01_Start") != null, "Section 1 Start Platform Test", "Platform_01_Start exists");
                Assert(GameObject.Find("Prototype_Terminal") != null, "Section 1 Terminal Test", "Prototype_Terminal exists");
                Assert(GameObject.Find("Section_02_HammerSwing") != null, "Section 2 Hammer Swing Test", "Section_02_HammerSwing exists");
                Assert(GameObject.Find("Section_03_MovingPlatform") != null, "Section 3 Moving Platform Test", "Section_03_MovingPlatform exists");
                Assert(GameObject.Find("Section_04_SpikePath") != null, "Section 4 Spike Path Test", "Section_04_SpikePath exists");
                Assert(GameObject.Find("Section_05_LaserCorridor") != null, "Section 5 Laser Corridor Test", "Section_05_LaserCorridor exists");
                Assert(GameObject.Find("Section_06_PhysicsChamber") != null, "Section 6 Physics Chamber Test", "Section_06_PhysicsChamber exists");
            }
            catch (System.Exception ex)
            {
                failed++;
                Debug.LogError($"[EXCEPTION] Exception in test execution: {ex}");
            }

            Debug.Log("=================================================");
            Debug.Log($"[Phase 06 Tests] COMPLETED: {passed} PASSED, {failed} FAILED");
            Debug.Log("=================================================");

            if (failed > 0)
            {
                throw new System.Exception($"Phase 06 Automated Tests encountered {failed} failure(s)!");
            }
        }
    }
}
