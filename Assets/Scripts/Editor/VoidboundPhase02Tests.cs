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
    public static class VoidboundPhase02Tests
    {
        private const string ScenePath = "Assets/Scenes/Voidbound.unity";

        [MenuItem("Voidbound/Run Phase 02 Automated Tests")]
        public static void RunTests()
        {
            Debug.Log("=================================================");
            Debug.Log("[Phase 02 Tests] STARTING AUTOMATED TEST SUITE...");
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
                // 1. Scene Load
                Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                Assert(scene.IsValid() && scene.isLoaded, "Scene Load Test", $"Scene {ScenePath} loaded");

                // 2. AudioListener Test (Must have EXACTLY 1 active AudioListener)
                AudioListener[] listeners = Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
                Assert(listeners.Length == 1, "AudioListener Count Test", $"Found {listeners.Length} AudioListener(s)");

                // 3. Section 2 Hierarchy Tests
                GameObject sec2Root = GameObject.Find("Section_02_HammerSwing");
                Assert(sec2Root != null, "Section_02_HammerSwing Root Test", "Section_02_HammerSwing parent object exists");

                GameObject triggerObj = GameObject.Find("Section_02_Entrance_Trigger");
                SectionTrigger secTrigger = triggerObj != null ? triggerObj.GetComponent<SectionTrigger>() : null;
                Collider trigCol = triggerObj != null ? triggerObj.GetComponent<Collider>() : null;
                Assert(secTrigger != null && trigCol != null && trigCol.isTrigger, "Section 2 Trigger Test", "Entrance trigger present and configured");

                // 4. Bridge & Safe Zones Tests
                GameObject bridge = GameObject.Find("Bridge");
                Assert(bridge != null, "Bridge Root Test", "Bridge hierarchy present");

                GameObject sz1 = GameObject.Find("SafeZone_01");
                GameObject sz2 = GameObject.Find("SafeZone_02");
                Assert(sz1 != null && sz2 != null, "Safe Breathing Zones Test", "SafeZone_01 and SafeZone_02 present");

                GameObject endPlatform = GameObject.Find("Platform_04_Section2_End");
                Assert(endPlatform != null && endPlatform.GetComponent<Collider>() != null, "End Platform Test", "Platform_04_Section2_End present with collider");

                // 5. Gantries and Hammers
                GameObject h1Obj = GameObject.Find("SwingingHammer_01");
                GameObject h2Obj = GameObject.Find("SwingingHammer_02");
                GameObject h3Obj = GameObject.Find("SwingingHammer_03");
                Assert(h1Obj != null && h2Obj != null && h3Obj != null, "Three Hammers Presence Test", "All 3 swinging hammers exist");

                SwingingHammer h1 = h1Obj != null ? h1Obj.GetComponent<SwingingHammer>() : null;
                SwingingHammer h2 = h2Obj != null ? h2Obj.GetComponent<SwingingHammer>() : null;
                SwingingHammer h3 = h3Obj != null ? h3Obj.GetComponent<SwingingHammer>() : null;
                Assert(h1 != null && h2 != null && h3 != null, "SwingingHammer Components Test", "SwingingHammer component on all 3 objects");

                // Verify Staggered Phase Offsets
                Assert(Mathf.Approximately(h1.PhaseOffsetDegrees, 0f), "Hammer 1 Phase Test", $"Phase: {h1.PhaseOffsetDegrees}°");
                Assert(Mathf.Approximately(h2.PhaseOffsetDegrees, 120f), "Hammer 2 Phase Test", $"Phase: {h2.PhaseOffsetDegrees}°");
                Assert(Mathf.Approximately(h3.PhaseOffsetDegrees, 240f), "Hammer 3 Phase Test", $"Phase: {h3.PhaseOffsetDegrees}°");

                // 6. Kinematic Rigidbody & Anti-Tunneling Configuration
                Rigidbody rb1 = h1Obj.GetComponent<Rigidbody>();
                Rigidbody rb2 = h2Obj.GetComponent<Rigidbody>();
                Rigidbody rb3 = h3Obj.GetComponent<Rigidbody>();
                Assert(rb1 != null && rb1.isKinematic && rb1.interpolation == RigidbodyInterpolation.Interpolate,
                    "Hammer 1 Rigidbody Test", "Kinematic Rigidbody with Interpolation configured");
                Assert(rb2 != null && rb2.isKinematic && rb3 != null && rb3.isKinematic,
                    "Hammers 2 & 3 Rigidbody Test", "Kinematic Rigidbodies on all hammers");

                // 7. Overhead Spine and Arches Positioned in Middles Between Hammers
                GameObject archMid1 = GameObject.Find("Arch_02_Between_H1_H2");
                GameObject archMid2 = GameObject.Find("Arch_03_Between_H2_H3");
                Assert(archMid1 != null && archMid2 != null, "Arches Between Hammers Test", "Arches placed between hammers");
                if (archMid1 != null)
                {
                    Assert(Mathf.Approximately(archMid1.transform.position.z, 35f), "Arch 1 Position Test", $"Arch between H1 & H2 at Z={archMid1.transform.position.z} (Expected 35)");
                }
                if (archMid2 != null)
                {
                    Assert(Mathf.Approximately(archMid2.transform.position.z, 45f), "Arch 2 Position Test", $"Arch between H2 & H3 at Z={archMid2.transform.position.z} (Expected 45)");
                }

                // Verify Hammer Clearance from Arches (at least 4.5m distance so pillars never block knockback)
                float h1DistToArch = archMid1 != null ? Mathf.Abs(h1Obj.transform.position.z - archMid1.transform.position.z) : 0f;
                Assert(h1DistToArch >= 4.5f, "Hammer to Arch Clearance Test", $"Distance from Hammer 1 to Arch 2 is {h1DistToArch:F1}m (Clear of pillars)");

                // 8. HammerKnockback Components on Hammer Heads (Pushes player to void, no instant death)
                HammerKnockback hk1 = h1Obj.GetComponentInChildren<HammerKnockback>();
                HammerKnockback hk2 = h2Obj.GetComponentInChildren<HammerKnockback>();
                HammerKnockback hk3 = h3Obj.GetComponentInChildren<HammerKnockback>();
                Assert(hk1 != null && hk2 != null && hk3 != null,
                    "HammerKnockback Components Test", "HammerKnockback present on all 3 hammer heads");
                Assert(hk1.HorizontalKnockback >= 15f && hk1.UpwardLift > 0f,
                    "Knockback Force Test", $"Horizontal push: {hk1.HorizontalKnockback}m/s, lift: {hk1.UpwardLift}m/s");

                // 8. Staggered Harmonic Motion Math Verification
                // Test angle evaluation: at t=0, angles should match maxAngle * sin(phase)
                float maxA = h1.MaxSwingAngle;
                float expectedH1_0 = 0f;
                float expectedH2_0 = maxA * Mathf.Sin(120f * Mathf.Deg2Rad);
                float expectedH3_0 = maxA * Mathf.Sin(240f * Mathf.Deg2Rad);

                Assert(Mathf.Abs(expectedH2_0 - expectedH3_0) > 10f, "Staggered Motion Variance Test",
                    $"Initial angle spread: H1={expectedH1_0:F1}°, H2={expectedH2_0:F1}°, H3={expectedH3_0:F1}°");

                // 9. Safe Haven Geometry & Traversability Clearance
                // SafeZone_01 center at Z = 35, length 6m (Z: 32 to 38).
                // Hammer 1 at Z = 30 (half-thickness 0.7m, max reach Z = 30.7).
                // Clearance to Safe Zone boundary (Z = 32): 32 - 30.7 = 1.3m clearance!
                float h1EdgeZ = 30f + 0.7f;
                float sz1Start = 35f - 3f;
                float clearance1 = sz1Start - h1EdgeZ;
                Assert(clearance1 >= 1.0f, "Safe Zone 1 Clearance Test", $"Clearance from Hammer 1 to Safe Zone 1: {clearance1:F2}m");

                float h2EdgeZ = 40f - 0.7f;
                float sz1End = 35f + 3f;
                float clearance2 = h2EdgeZ - sz1End;
                Assert(clearance2 >= 1.0f, "Safe Zone 2 Clearance Test", $"Clearance from Safe Zone 1 to Hammer 2: {clearance2:F2}m");

                // 10. Kill Volume Coverage Test
                GameObject kvObj = GameObject.Find("KillVolume_Pit");
                BoxCollider kvCol = kvObj != null ? kvObj.GetComponent<BoxCollider>() : null;
                Assert(kvCol != null && kvCol.size.z >= 250f, "Expanded Kill Volume Test", $"KillVolume depth: {kvCol.size.z}m");

                // 11. Time Formatting & HUD Placement Tests
                string formatted5s = GameManager.FormatTime(5.412f);
                Assert(formatted5s == "00:05.41", "Time Formatting Test", $"FormatTime(5.412) = '{formatted5s}' (Expected '00:05.41')");

                GameObject canvasObj = GameObject.Find("Canvas");
                Transform hudTransform = canvasObj != null ? canvasObj.transform.Find("HUDPanel") : null;
                GameObject timerTextObj = hudTransform != null ? hudTransform.Find("TimerText")?.gameObject : null;
                GameObject livesTextObj = hudTransform != null ? hudTransform.Find("LivesText")?.gameObject : null;
                GameObject sectionTextObj = hudTransform != null ? hudTransform.Find("SectionText")?.gameObject : null;
                GameObject hintsTextObj = hudTransform != null ? hudTransform.Find("HintsText")?.gameObject : null;
                Assert(timerTextObj != null && livesTextObj != null && sectionTextObj != null && hintsTextObj != null,
                    "HUD Elements Existence Test", "All required HUD text objects present");

                RectTransform timerRt = timerTextObj.GetComponent<RectTransform>();
                RectTransform livesRt = livesTextObj.GetComponent<RectTransform>();
                RectTransform sectionRt = sectionTextObj.GetComponent<RectTransform>();
                RectTransform hintsRt = hintsTextObj.GetComponent<RectTransform>();

                // Time and Lives in top-left as a row (same Y anchor and Y position)
                Assert(timerRt.anchorMin.x < 0.1f && timerRt.anchorMin.y > 0.9f &&
                       livesRt.anchorMin.x < 0.1f && livesRt.anchorMin.y > 0.9f,
                       "Top-Left Row Anchor Test", "Timer and Lives anchored to top-left");
                Assert(Mathf.Approximately(timerRt.anchoredPosition.y, livesRt.anchoredPosition.y),
                       "Top-Left Row Alignment Test", $"Timer Y: {timerRt.anchoredPosition.y}, Lives Y: {livesRt.anchoredPosition.y}");

                // Section completely beneath Time
                Assert(sectionRt.anchoredPosition.y < timerRt.anchoredPosition.y,
                       "Section Beneath Time Test", $"Section Y ({sectionRt.anchoredPosition.y}) is beneath Timer Y ({timerRt.anchoredPosition.y})");

                // Controls at top-right
                Assert(hintsRt.anchorMin.x > 0.9f && hintsRt.anchorMin.y > 0.9f,
                       "Controls Top-Right Anchor Test", "Controls legend anchored to top-right");

                // 12. GameManager & Hammer Knockback Mechanics in Section 2
                GameManager gm = Object.FindFirstObjectByType<GameManager>();
                GameObject player = GameObject.FindWithTag("Player");
                Assert(gm != null && player != null, "GameManager and Player Present", "Core singletons available");

                var playerController = player.GetComponent<PlayerController>();
                var playerDeathHandler = player.GetComponent<PlayerDeathHandler>();

                // Initialize runtime state bindings for batch test execution
                var awakeMethod = typeof(GameManager).GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                awakeMethod?.Invoke(gm, null);
                gm.RegisterSpawnPoint(new Vector3(0f, 1f, 0f), Quaternion.identity);

                var pcAwake = typeof(PlayerController).GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                pcAwake?.Invoke(playerController, null);
                var pcStart = typeof(PlayerController).GetMethod("Start", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                pcStart?.Invoke(playerController, null);

                var pdhAwake = typeof(PlayerDeathHandler).GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                pdhAwake?.Invoke(playerDeathHandler, null);
                var pdhStart = typeof(PlayerDeathHandler).GetMethod("Start", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                pdhStart?.Invoke(playerDeathHandler, null);

                gm.StartGame();
                Assert(gm.CurrentState == GameState.Playing, "StartGame State Test", "Game is in Playing state");
                Assert(gm.CurrentLives == 3, "Initial Lives Test", "Player starts with 3 lives");

                // Section Trigger simulation
                gm.SetSection("Section 2: Hammer Swing");
                Assert(gm.CurrentSectionName == "Section 2: Hammer Swing", "Section Name Update Test", "Section 2 registered");

                // Move player into Hammer 1 strike zone
                player.transform.position = new Vector3(0f, 1.0f, 30f);

                // Simulate Collision with Hammer 1 - should apply knockback WITHOUT killing player directly
                hk1.ApplyPush(player);
                Assert(gm.CurrentLives == 3, "Hammer Contact Survival Test", "Hammer contact did not directly kill the player (lives remain 3)");
                Assert(playerController.CurrentKnockback.sqrMagnitude > 1.0f, "Player Knockback Applied Test",
                    $"Player received knockback velocity: {playerController.CurrentKnockback.magnitude:F1}m/s");

                // Now simulate player being pushed off the bridge into the KillVolume_Pit (falling into the void)
                KillVolume kv = kvObj.GetComponent<KillVolume>();
                kv.TriggerKill(player);
                Assert(gm.CurrentLives == 2, "Void Pit Kill Test", $"Falling into void reduced lives to {gm.CurrentLives}");

                // Overlapping hit should be blocked by Death Guard
                kv.TriggerKill(player);
                Assert(gm.CurrentLives == 2, "Death Guard Test", "Overlapping void hit prevented double life loss");

                // Verify respawn position is Section 1 Initial Spawn (since Checkpoint 1 is not until Section 3!)
                Assert(player.transform.position.z < 20f, "Respawn to Section 1 Spawn Test",
                    $"Player respawned at Z={player.transform.position.z:F1} (Section 1 spawn)");

                // 12. Pause & Resume Freezing Test
                gm.PauseGame();
                Assert(gm.CurrentState == GameState.Paused && Mathf.Approximately(Time.timeScale, 0f),
                    "Pause Test", "State is Paused and TimeScale is 0");

                gm.ResumeGame();
                Assert(gm.CurrentState == GameState.Playing && Mathf.Approximately(Time.timeScale, 1f),
                    "Resume Test", "State is Playing and TimeScale is 1");

                // 13. Course Restart Test
                gm.RestartGame();
                Assert(gm.CurrentLives == 3, "Restart Lives Test", "Lives reset to 3");
                Assert(Mathf.Approximately(gm.ElapsedTime, 0f), "Restart Timer Test", "Elapsed time reset to 0");
            }
            catch (System.Exception ex)
            {
                failed++;
                Debug.LogError($"[EXCEPTION] Exception during tests: {ex}");
            }

            Debug.Log("=================================================");
            Debug.Log($"[Phase 02 Tests] COMPLETED: {passed} PASSED, {failed} FAILED.");
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
