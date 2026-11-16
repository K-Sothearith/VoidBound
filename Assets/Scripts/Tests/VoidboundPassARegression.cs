#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Voidbound.Core;
using Voidbound.Player;
using Voidbound.Interactions;
using Voidbound.Obstacles;
using Voidbound.UI;

namespace Voidbound.Tests
{
    /// <summary>Opt-in Play Mode regressions against the saved scene. Never rebuilds or saves the course.</summary>
    public sealed class VoidboundPassARegression : MonoBehaviour
    {
        private const string SessionKey = "Voidbound.PassA.Regression";
        private StreamWriter log;
        private int passed, failed;
        private GameManager manager;
        private PlayerController player;
        private Keyboard keyboard;
        private int oldFrameRate;
        private InputSettings.BackgroundBehavior oldBackground;
        private InputSettings.EditorInputBehaviorInPlayMode oldEditorInput;
        private bool oldRunInBackground;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            if (!SessionState.GetBool(SessionKey, false)) return;
            SessionState.SetBool(SessionKey, false);
            new GameObject("Pass A Regression Runner").AddComponent<VoidboundPassARegression>();
        }

        private void Write(string message)
        {
            log.WriteLine(message);
            log.Flush();
            Debug.Log("[Pass A regression] " + message);
        }

        private void Check(bool result, string name, string detail = "")
        {
            if (result) passed++; else failed++;
            Write((result ? "PASS " : "FAIL ") + name + " " + detail);
        }

        private IEnumerator Wait(float duration)
        {
            float end = Time.realtimeSinceStartup + duration;
            while (Time.realtimeSinceStartup < end) yield return null;
        }

        private void Keys(params Key[] keys) => InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
        private T[] FindAll<T>() where T : UnityEngine.Object => FindObjectsByType<T>(FindObjectsSortMode.None);
        private void Park(Vector3 position) => player.Teleport(position, Quaternion.identity);
        private IEnumerator FreshRun()
        {
            Keys();
            manager.RestartGame();
            yield return Wait(.15f);
        }

        // Drives the real controller via input; no teleporting between takeoff and landing.
        private IEnumerator JumpTo(Func<Vector3> destination, string name)
        {
            float startY = player.transform.position.y;
            float deadline = Time.realtimeSinceStartup + 1.6f;
            bool airborne = false;
            bool jumpQueued = false;
            while (Time.realtimeSinceStartup < deadline)
            {
                Vector3 direction = destination() - player.transform.position;
                direction.y = 0;
                if (direction.sqrMagnitude > .04f)
                    player.transform.rotation = Quaternion.LookRotation(direction);
                if (!jumpQueued)
                {
                    Keys(Key.W, Key.LeftShift, Key.Space);
                    jumpQueued = true;
                }
                else if (direction.sqrMagnitude > .16f) Keys(Key.W, Key.LeftShift);
                else Keys();
                yield return null;
                airborne |= player.transform.position.y > startY + .3f;
                if (airborne && player.IsGrounded && Time.realtimeSinceStartup < deadline - .1f) break;
            }
            Keys();
            yield return Wait(.08f);
            Vector3 delta = destination() - player.transform.position;
            Check(airborne && player.IsGrounded && new Vector2(delta.x, delta.z).magnitude < 2.4f && Mathf.Abs(delta.y) < .35f,
                name, $"position={player.transform.position} target={destination()} grounded={player.IsGrounded}");
        }

        private IEnumerator Start()
        {
            bool captureOnly = SessionState.GetBool(SessionKey + ".Capture", false);
            SessionState.SetBool(SessionKey + ".Capture", false);
            log = new StreamWriter(captureOnly ? "/tmp/VoidBound-PassA-visual-checks.txt" : "/tmp/VoidBound-PassA-results.txt");
            oldFrameRate = Application.targetFrameRate;
            oldRunInBackground = Application.runInBackground;
            oldBackground = InputSystem.settings.backgroundBehavior;
            oldEditorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
            Application.targetFrameRate = 60;
            Application.runInBackground = true;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            keyboard = InputSystem.AddDevice<Keyboard>();
            InputSystem.EnableDevice(keyboard);
            yield return null;
            yield return null;
            manager = GameManager.Instance;
            player = FindFirstObjectByType<PlayerController>();
            Write($"Unity={Application.unityVersion} graphics={SystemInfo.graphicsDeviceName}; saved-scene Play Mode at target 60 FPS");
            Check(manager.CurrentState == GameState.StartMenu, "start menu");
            UIManager.Instance.StartMenuPanel.GetComponentsInChildren<UnityEngine.UI.Button>()[0].onClick.Invoke();
            yield return Wait(.35f);
            if (captureOnly)
            {
                yield return CaptureAndRepeatResets();
                Complete();
                yield break;
            }
            int initialFrame = Time.frameCount;
            float initialTime = manager.ElapsedTime;
            yield return Wait(.25f);
            Check(Time.frameCount > initialFrame + 3 && manager.ElapsedTime > initialTime, "frames and timer advance");
            Vector3 origin = player.transform.position;
            Keys(Key.W); yield return Wait(.4f); Keys(); yield return null;
            Check(player.transform.position.z > origin.z + 1.5f, "W input moves controller");
            yield return FreshRun(); yield return Wait(.25f);
            float standing = player.transform.position.y;
            float apex = standing;
            for (int i = 0; i < 16; i++)
            {
                Keys(i % 2 == 0 ? new[] { Key.Space } : new Key[0]);
                yield return Wait(.035f);
                apex = Mathf.Max(apex, player.transform.position.y);
            }
            Check(apex - standing > 1.4f && apex - standing < 2f, "ground jump and rapid midair input", $"apex={apex-standing}");
            Keys(); yield return Wait(.6f);
            manager.PauseGame(); origin = player.transform.position; initialTime = manager.ElapsedTime;
            Keys(Key.W); yield return Wait(.2f); Keys();
            Check(player.transform.position == origin && manager.ElapsedTime == initialTime, "pause freezes movement/time");
            manager.ResumeGame();

            var triggers = FindAll<SectionTrigger>().OrderBy(t => t.transform.position.z).ToArray();
            var checkpoints = FindAll<Checkpoint>().OrderBy(c => c.transform.position.z).ToArray();
            for (int run = 0; run < 2; run++)
            {
                yield return FreshRun();
                Check(manager.CurrentSectionName == GameManager.InitialSectionName, "restart section label " + run);
                foreach (var trigger in triggers)
                {
                    Park(trigger.transform.position); yield return Wait(.1f);
                    Check(manager.CurrentSectionName == trigger.SectionName, "section entry run " + run + " " + trigger.name);
                }
            }
            foreach (var checkpoint in checkpoints)
            {
                yield return FreshRun(); Park(checkpoint.transform.position); yield return Wait(.15f);
                Check(checkpoint.IsActivated, "physical checkpoint entry " + checkpoint.name);
                manager.SetSection("Section 7: Final Gauntlet");
                Park(new Vector3(12f, .1f, checkpoint.transform.position.z + 4f)); yield return Wait(1.4f);
                Check(manager.CurrentLives == 2 && Vector3.Distance(player.transform.position, checkpoint.RespawnPosition) < .3f,
                    "void death respawn " + checkpoint.name, "position=" + player.transform.position);
                Check(manager.CurrentSectionName == checkpoint.SectionName, "checkpoint section label " + checkpoint.name);
                var next = triggers.FirstOrDefault(t => t.transform.position.z > checkpoint.transform.position.z + 2f);
                if (next != null) { Park(next.transform.position); yield return Wait(.12f); Check(manager.CurrentSectionName == next.SectionName, "revisit section after checkpoint respawn " + next.name); }
            }

            var junk = new GameObject("Non-player hazard filter probe");
            var hazards = FindAll<Hazard>().OrderBy(h => h.name).ToArray();
            foreach (var hazard in hazards)
            {
                yield return FreshRun(); manager.ResetRespawnGuard();
                if (hazard is LaserHazard pulse) pulse.SetState(LaserState.Active);
                hazard.ProcessHit(junk);
                Check(manager.CurrentLives == 3 && !player.GetComponent<PlayerDeathHandler>().IsDead, "non-player ignored " + hazard.name);
                Collider collider = hazard is SpikeHazard ? hazard.GetComponent<Collider>() :
                    (Collider)new SerializedObject(hazard).FindProperty("beamCollider").objectReferenceValue;
                Park(collider.bounds.center - Vector3.up * .8f);
                yield return Wait(.12f);
                Check(manager.CurrentLives == 2, "physical hazard after non-player " + hazard.name, "lives=" + manager.CurrentLives);
            }
            Destroy(junk);
            var spike = GameObject.Find("Spike_Narrow_01").GetComponent<SpikeHazard>();
            yield return FreshRun();
            manager.OnPlayerDied();
            Park(spike.transform.position + Vector3.up * .2f);
            yield return Wait(.15f);
            Check(manager.CurrentLives == 2 && !player.GetComponent<PlayerDeathHandler>().IsDead, "rejected cooldown contact does not freeze player");
            yield return Wait(1.05f);
            Check(manager.CurrentLives == 1, "staying inside spike becomes lethal after immunity", "lives=" + manager.CurrentLives);
            Park(new Vector3(2f,.1f,113f)); yield return Wait(1.05f);
            Park(spike.transform.position + Vector3.up * .2f); yield return Wait(.15f);
            Check(manager.CurrentLives == 0 && manager.CurrentState == GameState.GameOver, "spike reentry still lethal after another respawn");
            var pulseTest = FindAll<LaserHazard>().First();
            foreach (var state in new[] { LaserState.Inactive, LaserState.Warning })
            {
                yield return FreshRun(); pulseTest.SetState(state);
                var beam = (Collider)new SerializedObject(pulseTest).FindProperty("beamCollider").objectReferenceValue;
                // Bounds are empty when disabled; the beam transform remains the physical reference.
                Park(beam.transform.position - Vector3.up * .8f); yield return Wait(.15f);
                Check(manager.CurrentLives == 3, "pulsed laser safe in " + state);
            }
            yield return FreshRun();
            var hammer = FindFirstObjectByType<HammerKnockback>(); Park(hammer.transform.position - Vector3.up * .7f);
            yield return Wait(.1f);
            Check(manager.CurrentLives == 3 && player.CurrentKnockback.sqrMagnitude > 1, "hammer collision knocks back without instant death");

            foreach (var collapse in FindAll<CollapsingPlatform>().OrderBy(c => c.name))
            {
                yield return FreshRun(); Park(collapse.transform.position + Vector3.up * .8f);
                yield return Wait(.35f);
                Check(collapse.CurrentState == CollapsingPlatformState.Warning, "physical landing starts warning " + collapse.name);
                Park(new Vector3(0,.1f,343)); yield return Wait(collapse.WarnDuration + .05f);
                Check(collapse.CurrentState == CollapsingPlatformState.Collapsed && !collapse.IsSolidColliderEnabled, "collapse disables collision " + collapse.name);
                manager.OnPlayerDied(); yield return Wait(.1f);
                Check(collapse.CurrentState == CollapsingPlatformState.Idle && collapse.IsSolidColliderEnabled && collapse.transform.position == collapse.InitialPosition,
                    "respawn restores collapsed platform " + collapse.name);
            }

            var summoned = FindAll<MovingPlatform>().Single(p => !p.AutoOscillate);
            var plate = FindFirstObjectByType<PressurePlate>();
            yield return FreshRun(); Park(plate.transform.position + Vector3.up * .1f);
            yield return Wait(2.65f);
            Check(plate.IsPressed && Vector3.Distance(summoned.transform.position, summoned.PointB) < .1f, "section3 plate summons platform to departure");
            Park(summoned.transform.position + Vector3.up * .65f); yield return Wait(.25f);
            Vector3 relative = player.transform.position - summoned.transform.position;
            Vector3 platformOrigin = summoned.transform.position;
            yield return Wait(1.5f);
            Vector3 drift = player.transform.position - summoned.transform.position - relative;
            Check(!plate.IsPressed && Vector3.Distance(platformOrigin,summoned.transform.position) > 3f && drift.magnitude < .25f && player.IsGrounded,
                "section3 release and actual return carrying", $"drift={drift} platformTravel={Vector3.Distance(platformOrigin,summoned.transform.position)}");
            yield return Wait(2f);
            yield return JumpTo(() => new Vector3(0,.08f,97f), "section3 moving platform to arrival jump");

            foreach (int fps in new[] { 30, 60, 120 })
            {
                Application.targetFrameRate = fps;
                foreach (var platform in FindAll<MovingPlatform>().Where(p => p.AutoOscillate).OrderBy(p => p.name))
                {
                    yield return FreshRun(); Park(platform.transform.position + Vector3.up * .65f); yield return Wait(.3f);
                    relative = player.transform.position - platform.transform.position; platformOrigin = platform.transform.position;
                    yield return Wait(.8f); drift = player.transform.position - platform.transform.position - relative;
                    Check(drift.magnitude < .3f && Vector3.Distance(platformOrigin,platform.transform.position) > .2f && player.IsGrounded,
                        "autonomous carrying " + fps + "fps " + platform.name, "drift=" + drift);
                }
            }
            Application.targetFrameRate = 60;
            yield return FreshRun();
            var moving = FindAll<MovingPlatform>().Where(p => p.AutoOscillate).OrderBy(p => p.name).ToArray();
            Park(moving[0].transform.position + Vector3.up * .65f); yield return Wait(.3f);
            // Walk to the forward edge before the legitimate moving-platform transfer.
            player.transform.rotation = Quaternion.identity; Keys(Key.W); yield return Wait(.22f); Keys(); yield return Wait(.1f);
            float deadline = Time.realtimeSinceStartup + 6f;
            while (Time.realtimeSinceStartup < deadline && !(moving[1].transform.position.z < 324f && moving[1].FrameDelta.z < 0f)) yield return null;
            yield return JumpTo(() => moving[1].transform.position + Vector3.up * .48f, "autonomous platform 1 to 2 input jump");
            if (player.transform.position.z > 319f && player.IsGrounded)
            {
                player.transform.rotation = Quaternion.identity; Keys(Key.W); yield return Wait(.18f); Keys(); yield return Wait(.08f);
                deadline = Time.realtimeSinceStartup + 5f;
                while(Time.realtimeSinceStartup < deadline && moving[1].transform.position.z < 326f) yield return null;
                yield return JumpTo(() => moving[2].transform.position + Vector3.up * .48f, "autonomous platform 2 to 3 input jump");
                if (player.transform.position.z > 332f && player.IsGrounded)
                    yield return JumpTo(() => new Vector3(0,.08f,341.5f), "autonomous platform 3 to rest haven input jump");
                else Check(false, "autonomous platform 3 to haven prerequisite");
            }
            else Check(false, "autonomous transfers 2 to 3 and 3 to haven prerequisite");

            var crates = FindAll<PushableCrate>().OrderBy(c => c.transform.position.x).ToArray();
            var plates = FindAll<CratePressurePlate>().OrderBy(p => p.transform.position.x).ToArray();
            var door = FindFirstObjectByType<PuzzleDoor>();
            yield return FreshRun();
            crates[0].Rigidbody.position = plates[0].transform.position + Vector3.up * .7f;
            yield return Wait(.3f); Check(plates[0].IsPressed && !door.IsOpen, "one crate cannot open dual plate door");
            crates[1].Rigidbody.position = plates[1].transform.position + Vector3.up * .7f;
            yield return Wait(1.1f); Check(plates.All(p=>p.IsPressed) && door.IsOpen, "two physical crates open door");
            FindFirstObjectByType<CrateResetTerminal>().Interact(player.gameObject); yield return Wait(.4f);
            Check(crates.All(c=>Vector3.Distance(c.transform.position,c.InitialPosition)<.15f) && plates.All(p=>!p.IsPressed),
                "terminal resets crates and plate occupancy", string.Join("; ", crates.Select(c=>$"{c.name} render={c.transform.position} physics={c.Rigidbody.position} origin={c.InitialPosition}")) +
                " plates=" + string.Join(",", plates.Select(p=>p.IsPressed)));
            Check(door.IsOpen, "latched door remains open until course/respawn reset");
            manager.OnPlayerDied(); yield return Wait(.25f);
            Check(!door.IsOpen && plates.All(p=>!p.IsPressed) && crates.All(c=>Vector3.Distance(c.transform.position,c.InitialPosition)<.15f), "death resets complete puzzle");
            Park(new Vector3(0,.1f,228)); crates[0].Rigidbody.position = new Vector3(-4.5f,-6,239); yield return Wait(.4f);
            Check(Vector3.Distance(crates[0].transform.position,crates[0].InitialPosition)<.15f, "lost crate recovery");
            for(int cycle=0;cycle<12;cycle++)
            {
                yield return FreshRun(); manager.ResetRespawnGuard();
                spike.ProcessHit(player.gameObject); pulseTest.SetState(LaserState.Active); pulseTest.ProcessHit(player.gameObject); spike.ProcessHit(player.gameObject);
                Check(manager.CurrentLives==2 && manager.DeathCount==1, "overlapping hazard single deduction cycle " + cycle);
                manager.ResetRespawnGuard(); manager.OnPlayerDied(); manager.ResetRespawnGuard(); manager.OnPlayerDied();
                Check(manager.CurrentState==GameState.GameOver && UIManager.Instance.GameOverPanel.activeInHierarchy, "game over/reset cycle " + cycle);
                yield return FreshRun();
                Check(manager.CurrentLives==3 && manager.DeathCount==0 && !manager.HasCustomCheckpoint && manager.CurrentSectionName==GameManager.InitialSectionName && !door.IsOpen,
                    "full restart cycle " + cycle);
            }
            var portal = FindFirstObjectByType<ExtractionPortal>();
            Park(portal.transform.position); yield return Wait(.15f);
            Check(manager.CurrentState==GameState.Victory && UIManager.Instance.VictoryPanel.activeInHierarchy, "physical portal victory");
            UIManager.Instance.VictoryRestartButton.onClick.Invoke(); yield return Wait(.2f);
            Check(manager.IsPlaying && manager.CurrentLives==3 && !portal.HasTriggeredVictory && manager.CurrentSectionName==GameManager.InitialSectionName, "victory restart restores run");
            Complete();
        }

        private IEnumerator CaptureAndRepeatResets()
        {
            foreach (float z in new[] { 0f, 26f, 65f, 165f })
            {
                Park(new Vector3(0,.1f,z)); yield return Wait(.2f);
                var camera = Camera.main;
                var target = new RenderTexture(1280,720,24);
                var previous = RenderTexture.active;
                var previousTarget = camera.targetTexture;
                var texture = new Texture2D(1280,720,TextureFormat.RGB24,false);
                try
                {
                    camera.targetTexture = target;
                    camera.Render();
                    RenderTexture.active = target;
                    texture.ReadPixels(new Rect(0,0,1280,720),0,0);
                    texture.Apply();
                    File.WriteAllBytes($"/tmp/VoidBound-PassA-view-{z}.png",texture.EncodeToPNG());
                }
                finally
                {
                    camera.targetTexture = previousTarget;
                    RenderTexture.active = previous;
                    Destroy(target); Destroy(texture);
                }
                Check(true,"rendered camera capture at Z=" + z);
            }
            var crates = FindAll<PushableCrate>().OrderBy(c=>c.transform.position.x).ToArray();
            var plates = FindAll<CratePressurePlate>().OrderBy(p=>p.transform.position.x).ToArray();
            var terminal = FindFirstObjectByType<CrateResetTerminal>();
            for(int cycle=0;cycle<10;cycle++)
            {
                yield return FreshRun();
                for(int i=0;i<2;i++) crates[i].Rigidbody.position = plates[i].transform.position + Vector3.up * .7f;
                yield return Wait(.2f);
                Check(plates.All(p=>p.IsPressed),"repeat puzzle physical occupancy " + cycle);
                terminal.Interact(player.gameObject);
                yield return Wait(.2f);
                Check(crates.All(c=>Vector3.Distance(c.Rigidbody.position,c.InitialPosition)<.15f && Vector3.Distance(c.transform.position,c.InitialPosition)<.15f) && plates.All(p=>!p.IsPressed),
                    "repeat puzzle reset " + cycle);
            }
        }

        private void Complete()
        {
            Write($"COMPLETE passed={passed} failed={failed} frames={Time.frameCount}");
            log.Dispose(); log = null;
            RestoreInput();
            if (Application.isBatchMode) EditorApplication.Exit(failed == 0 ? 0 : 1);
            else EditorApplication.isPlaying = false;
        }

        private void RestoreInput()
        {
            if (keyboard != null) { InputSystem.RemoveDevice(keyboard); keyboard = null; }
            Application.targetFrameRate = oldFrameRate;
            Application.runInBackground = oldRunInBackground;
            InputSystem.settings.backgroundBehavior = oldBackground;
            InputSystem.settings.editorInputBehaviorInPlayMode = oldEditorInput;
        }

        private void OnDestroy()
        {
            if (keyboard != null) RestoreInput();
            log?.Dispose();
        }
    }
}
#endif
