using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Voidbound.Core;
using Voidbound.Interactions;
using Voidbound.Player;
using Voidbound.UI;

namespace Voidbound.Editor
{
    public static class VoidboundPhase01Tests
    {
        private const string ScenePath = "Assets/Scenes/Voidbound.unity";

        [MenuItem("Voidbound/Run Phase 01 Automated Tests")]
        public static void RunTests()
        {
            Debug.Log("=================================================");
            Debug.Log("[Phase 01 Tests] STARTING AUTOMATED TEST SUITE...");
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
                // 1. Scene Loading Test
                Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                Assert(scene.IsValid() && scene.isLoaded, "Scene Load Test", $"Scene {ScenePath} loaded successfully");

                // 2. AudioListener Test (Must have EXACTLY 1 active AudioListener)
                AudioListener[] listeners = Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
                Assert(listeners.Length == 1, "AudioListener Count Test", $"Found {listeners.Length} AudioListener(s)");

                // 3. Player Hierarchy & Component Tests
                GameObject player = GameObject.FindWithTag("Player");
                Assert(player != null, "Player GameObject Test", "Player object exists with 'Player' tag");

                CharacterController cc = player != null ? player.GetComponent<CharacterController>() : null;
                Assert(cc != null && cc.height > 1.5f && cc.radius > 0.3f, "CharacterController Test", "Valid CharacterController configured");

                PlayerController pc = player != null ? player.GetComponent<PlayerController>() : null;
                Assert(pc != null, "PlayerController Test", "PlayerController component present");

                PlayerDeathHandler pdh = player != null ? player.GetComponent<PlayerDeathHandler>() : null;
                Assert(pdh != null, "PlayerDeathHandler Test", "PlayerDeathHandler component present");

                PlayerInteractor pi = player != null ? player.GetComponent<PlayerInteractor>() : null;
                Assert(pi != null, "PlayerInteractor Test", "PlayerInteractor component present");

                Camera cam = player != null ? player.GetComponentInChildren<Camera>() : null;
                Assert(cam != null && cam.CompareTag("MainCamera"), "Player Camera Test", "MainCamera child present with MainCamera tag");

                // 4. GameManager Tests
                GameManager gm = Object.FindFirstObjectByType<GameManager>();
                Assert(gm != null, "GameManager Instance Test", "GameManager object present in scene");
                Assert(gm.MaxLives == 3, "GameManager MaxLives Test", $"Max lives configured to {gm.MaxLives}");

                // 5. Environment & Tutorial Platform Tests
                GameObject pStart = GameObject.Find("Platform_01_Start");
                Assert(pStart != null && pStart.GetComponent<Collider>() != null, "Platform_01_Start Test", "Start platform present with collider");

                GameObject pIntermediate = GameObject.Find("Platform_02_Intermediate");
                Assert(pIntermediate != null && pIntermediate.GetComponent<Collider>() != null, "Platform_02_Intermediate Test", "Intermediate platform present with collider");

                GameObject pFinish = GameObject.Find("Platform_03_Finish");
                Assert(pFinish != null && pFinish.GetComponent<Collider>() != null, "Platform_03_Finish Test", "Finish platform present with collider");

                // 6. Kill Volume Test
                GameObject killVolumeObj = GameObject.Find("KillVolume_Pit");
                KillVolume kv = killVolumeObj != null ? killVolumeObj.GetComponent<KillVolume>() : null;
                Collider kvCol = killVolumeObj != null ? killVolumeObj.GetComponent<Collider>() : null;
                Assert(kv != null && kvCol != null && kvCol.isTrigger, "KillVolume Test", "Kill volume trigger present below platforms");

                // 7. Prototype Terminal Interaction Test
                GameObject terminalObj = GameObject.Find("Prototype_Terminal");
                PrototypeTerminal terminal = terminalObj != null ? terminalObj.GetComponent<PrototypeTerminal>() : null;
                IInteractable interactable = terminal;
                Assert(terminal != null && interactable != null, "Prototype Terminal Component Test", "Terminal implements IInteractable");

                if (terminal != null)
                {
                    string initialPrompt = terminal.GetInteractionPrompt();
                    Assert(!string.IsNullOrEmpty(initialPrompt), "Terminal Prompt Test", $"Prompt: '{initialPrompt}'");

                    terminal.Interact(player);
                    Assert(terminal.IsActivated, "Terminal Interaction (Activate) Test", "Terminal switched to online state");

                    terminal.Interact(player);
                    Assert(!terminal.IsActivated, "Terminal Interaction (Deactivate) Test", "Terminal toggled back to offline state");
                }

                // 8. Prototype Finish Marker Test
                GameObject finishMarkerObj = GameObject.Find("Prototype_Finish_Marker");
                PrototypeFinishMarker finishMarker = finishMarkerObj != null ? finishMarkerObj.GetComponent<PrototypeFinishMarker>() : null;
                Collider markerCol = finishMarkerObj != null ? finishMarkerObj.GetComponent<Collider>() : null;
                Assert(finishMarker != null && markerCol != null && markerCol.isTrigger, "Prototype Finish Marker Test", "Finish marker trigger present");

                // 9. UI & HUD Tests
                UIManager ui = Object.FindFirstObjectByType<UIManager>();
                Assert(ui != null, "UIManager Test", "UIManager present in scene");

                Transform canvasTr = ui != null ? ui.transform : null;
                Transform startMenu = canvasTr != null ? canvasTr.Find("StartMenuPanel") : null;
                Transform hud = canvasTr != null ? canvasTr.Find("HUDPanel") : null;
                Transform pauseMenu = canvasTr != null ? canvasTr.Find("PauseMenuPanel") : null;
                Transform gameOver = canvasTr != null ? canvasTr.Find("GameOverPanel") : null;
                Assert(startMenu != null && hud != null && pauseMenu != null && gameOver != null, "UI Panels Test", "All 4 required UI panels exist on Canvas");

                // 10. Game State Flow & Lifecycle Tests
                gm.StartGame();
                Assert(gm.CurrentState == GameState.Playing, "StartGame State Test", "State transitioned to Playing");
                Assert(gm.CurrentLives == 3, "StartGame Lives Test", "Lives reset to 3");

                // Test Pause and Resume
                gm.PauseGame();
                Assert(gm.CurrentState == GameState.Paused && Mathf.Approximately(Time.timeScale, 0f), "PauseGame Test", "State is Paused and TimeScale is 0");

                gm.ResumeGame();
                Assert(gm.CurrentState == GameState.Playing && Mathf.Approximately(Time.timeScale, 1f), "ResumeGame Test", "State is Playing and TimeScale is 1");

                // Test Death Guard and Life Deduction
                player.transform.position = new Vector3(10f, 0f, 10f); // Move away
                gm.OnPlayerDied();
                Assert(gm.CurrentLives == 2, "Life Deduction Test", $"Lives reduced to {gm.CurrentLives}");

                // Immediate second call should be blocked by single-death guard
                gm.OnPlayerDied();
                Assert(gm.CurrentLives == 2, "Death Guard Test", "Overlapping death call prevented double life loss");

                // Test Game Over when lives reach 0
                gm.OnPlayerDied(); // If guard is on cooldown, force simulate
                // To bypass guard for unit test:
                var field = typeof(GameManager).GetField("isRespawning", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null) field.SetValue(gm, false);

                gm.OnPlayerDied(); // 1 life left
                if (field != null) field.SetValue(gm, false);

                gm.OnPlayerDied(); // 0 lives -> GameOver
                Assert(gm.CurrentState == GameState.GameOver, "GameOver State Test", "Game Over triggered when lives reach 0");

                // Test Course Restart
                gm.RestartGame();
                Assert(gm.CurrentState == GameState.Playing && gm.CurrentLives == 3, "RestartGame Test", "Course reset, lives restored to 3, playing resumed");
            }
            catch (System.Exception ex)
            {
                failed++;
                Debug.LogError($"[EXCEPTION] Exception during tests: {ex}");
            }

            Debug.Log("=================================================");
            Debug.Log($"[Phase 01 Tests] COMPLETED: {passed} PASSED, {failed} FAILED.");
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
