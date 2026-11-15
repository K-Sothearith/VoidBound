using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using Voidbound.Core;
using Voidbound.Interactions;
using Voidbound.Obstacles;
using Voidbound.Player;
using Voidbound.UI;

namespace Voidbound.Editor
{
    public static class VoidboundSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/Voidbound.unity";
        private const string MaterialsFolder = "Assets/Materials";

        [MenuItem("Voidbound/Build Phase 01 Scene")]
        public static void BuildPhase01Scene()
        {
            BuildScene(false);
        }

        [MenuItem("Voidbound/Build Phase 02 Scene (Hammer Swing)")]
        public static void BuildPhase02Scene()
        {
            BuildScene(true);
        }

        [MenuItem("Voidbound/Build Phase 03 Scene (Moving Platform & Checkpoint 1)")]
        public static void BuildPhase03Scene()
        {
            Debug.Log("[VoidboundSceneBuilder] Integrating Phase 03 into existing Voidbound scene...");

            EnsureDirectories();
            Material stoneMat = GetOrCreateMaterial("M_Stone_Platform", new Color(0.38f, 0.42f, 0.48f), 0.2f);
            Material neonMat = GetOrCreateMaterial("M_Neon_Cyan", new Color(0.1f, 0.9f, 1f), 0.8f, true, new Color(0.1f, 0.9f, 1f) * 2.5f);
            Material screenMat = GetOrCreateMaterial("M_Terminal_Screen", new Color(1f, 0.6f, 0.1f), 0.5f, true, new Color(1f, 0.6f, 0.1f) * 2.0f);
            Material beaconMat = GetOrCreateMaterial("M_Beacon_Finish", new Color(0.2f, 1f, 0.8f), 0.8f, true, new Color(0.2f, 1f, 0.8f) * 3.0f);
            Material darkTrimMat = GetOrCreateMaterial("M_Dark_Trim", new Color(0.20f, 0.24f, 0.30f), 0.4f);

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError($"[VoidboundSceneBuilder] Could not open scene at {ScenePath}!");
                return;
            }

            GameObject envRoot = GameObject.Find("Environment");
            if (envRoot == null)
            {
                Debug.LogError("[VoidboundSceneBuilder] 'Environment' root not found in scene!");
                return;
            }

            // 1. Reorient Terminal Alpha to face the player spawn point (0, 1, 0)
            Transform terminalTr = envRoot.transform.Find("Prototype_Terminal");
            if (terminalTr != null)
            {
                Vector3 toSpawn = new Vector3(0f, 0f, 0f) - terminalTr.position;
                toSpawn.y = 0f;
                terminalTr.rotation = Quaternion.LookRotation(toSpawn.normalized, Vector3.up);

                Transform screenTr = terminalTr.Find("ConsoleScreen");
                if (screenTr != null)
                {
                    screenTr.localRotation = Quaternion.Euler(-25f, 0f, 0f);
                }
                Debug.Log("[VoidboundSceneBuilder] Reoriented Prototype_Terminal to face player spawn point.");
            }

            // 2. Remove Prototype_Finish_Marker GameObject from the playable scene (temporary Phase 01 milestone)
            Transform finishMarkerTr = envRoot.transform.Find("Prototype_Finish_Marker");
            if (finishMarkerTr != null)
            {
                Object.DestroyImmediate(finishMarkerTr.gameObject);
                Debug.Log("[VoidboundSceneBuilder] Removed Prototype_Finish_Marker GameObject from scene.");
            }

            // 3. Remove existing Section 3 if present to allow clean idempotent integration (prevents duplicate checkpoints)
            Transform existingSec3 = envRoot.transform.Find("Section_03_MovingPlatform");
            if (existingSec3 != null)
            {
                Object.DestroyImmediate(existingSec3.gameObject);
            }

            // 4. Create Section 3 without rebuilding or disturbing existing sections
            CreateSection03MovingPlatform(envRoot.transform, stoneMat, neonMat, darkTrimMat);

            // Ensure Kill Volume Pit spans the entire void under all sections (center Z = 40, length = 320m)
            GameObject kvObj = GameObject.Find("KillVolume_Pit");
            if (kvObj != null)
            {
                BoxCollider bc = kvObj.GetComponent<BoxCollider>();
                if (bc != null)
                {
                    bc.size = new Vector3(200f, 4f, 320f);
                    kvObj.transform.position = new Vector3(0f, -12f, 40f);
                }
            }

            EditorSceneManager.SaveScene(scene, ScenePath);
            UpdateBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[VoidboundSceneBuilder] Phase 03 integration complete and scene saved!");
        }

        [MenuItem("Voidbound/Build Phase 04 Scene (Spike Path & Laser Corridor)")]
        public static void BuildPhase04Scene()
        {
            Debug.Log("[VoidboundSceneBuilder] Integrating Phase 04 (Section 4 & 5) into existing Voidbound scene...");

            EnsureDirectories();
            Material stoneMat = GetOrCreateMaterial("M_Stone_Platform", new Color(0.38f, 0.42f, 0.48f), 0.2f);
            Material neonMat = GetOrCreateMaterial("M_Neon_Cyan", new Color(0.1f, 0.9f, 1f), 0.8f, true, new Color(0.1f, 0.9f, 1f) * 2.5f);
            Material darkTrimMat = GetOrCreateMaterial("M_Dark_Trim", new Color(0.20f, 0.24f, 0.30f), 0.4f);
            Material spikeMat = GetOrCreateMaterial("M_Spike_Hazard", new Color(0.14f, 0.15f, 0.18f), 0.6f, true, new Color(1.0f, 0.15f, 0.15f) * 2.0f);
            Material laserBeamActiveMat = GetOrCreateMaterial("M_Laser_Beam_Active", new Color(1.0f, 0.12f, 0.12f, 1f), 0.9f, true, new Color(1.0f, 0.1f, 0.1f) * 3.5f);
            Material laserBeamWarningMat = GetOrCreateMaterial("M_Laser_Beam_Warning", new Color(1.0f, 0.65f, 0.1f, 0.7f), 0.8f, true, new Color(1.0f, 0.6f, 0.05f) * 1.8f);
            Material laserEmitterMat = GetOrCreateMaterial("M_Laser_Emitter", new Color(0.18f, 0.20f, 0.25f), 0.5f, true, new Color(0.1f, 0.9f, 1f) * 0.8f);

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError($"[VoidboundSceneBuilder] Could not open scene at {ScenePath}!");
                return;
            }

            GameObject envRoot = GameObject.Find("Environment");
            if (envRoot == null)
            {
                Debug.LogError("[VoidboundSceneBuilder] 'Environment' root not found in scene!");
                return;
            }

            // Clean up existing Section 4 and 5 if present for clean idempotent rebuild
            Transform existingSec4 = envRoot.transform.Find("Section_04_SpikePath");
            if (existingSec4 != null)
            {
                Object.DestroyImmediate(existingSec4.gameObject);
            }

            Transform existingSec5 = envRoot.transform.Find("Section_05_LaserCorridor");
            if (existingSec5 != null)
            {
                Object.DestroyImmediate(existingSec5.gameObject);
            }

            // Create Section 4: Spike Path
            CreateSection04SpikePath(envRoot.transform, stoneMat, neonMat, darkTrimMat, spikeMat);

            // Create Section 5: Laser Corridor
            CreateSection05LaserCorridor(envRoot.transform, stoneMat, neonMat, darkTrimMat, laserBeamActiveMat, laserBeamWarningMat, laserEmitterMat);

            // Expand Kill Volume Pit to cover all sections through Section 5 (length = 450m, center Z = 100m => Z from -125m to +325m)
            GameObject kvObj = GameObject.Find("KillVolume_Pit");
            if (kvObj != null)
            {
                BoxCollider bc = kvObj.GetComponent<BoxCollider>();
                if (bc != null)
                {
                    bc.size = new Vector3(200f, 4f, 450f);
                    kvObj.transform.position = new Vector3(0f, -12f, 100f);
                }
            }

            EditorSceneManager.SaveScene(scene, ScenePath);
            UpdateBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[VoidboundSceneBuilder] Phase 04 integration complete and scene saved!");
        }

        public static void BuildScene(bool includePhase02)
        {
            Debug.Log($"[VoidboundSceneBuilder] Starting Scene Generation (Include Phase 02: {includePhase02})...");

            EnsureDirectories();
            Material stoneMat = GetOrCreateMaterial("M_Stone_Platform", new Color(0.38f, 0.42f, 0.48f), 0.2f);
            Material neonMat = GetOrCreateMaterial("M_Neon_Cyan", new Color(0.1f, 0.9f, 1f), 0.8f, true, new Color(0.1f, 0.9f, 1f) * 2.5f);
            Material screenMat = GetOrCreateMaterial("M_Terminal_Screen", new Color(1f, 0.6f, 0.1f), 0.5f, true, new Color(1f, 0.6f, 0.1f) * 2.0f);
            Material beaconMat = GetOrCreateMaterial("M_Beacon_Finish", new Color(0.2f, 1f, 0.8f), 0.8f, true, new Color(0.2f, 1f, 0.8f) * 3.0f);
            Material darkTrimMat = GetOrCreateMaterial("M_Dark_Trim", new Color(0.20f, 0.24f, 0.30f), 0.4f);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Lighting & Environment
            SetupLightingAndFog();
            Volume volume = SetupGlobalVolume();

            // 2. Game Manager
            GameObject gmObj = new GameObject("GameManager");
            GameManager gm = gmObj.AddComponent<GameManager>();

            // 3. Player
            GameObject player = CreatePlayer();

            // 4. Level Geometry (Tutorial Platforming Area)
            GameObject envRoot = new GameObject("Environment");
            CreateTutorialPlatforms(envRoot.transform, stoneMat, neonMat, darkTrimMat);

            // 5. Prototype Terminal (faces player spawn point)
            CreatePrototypeTerminal(envRoot.transform, stoneMat, screenMat, neonMat);

            // 6. Prototype Finish Marker was removed in Phase 03 cleanup (temporary Phase 01 milestone)

            // 7. Phase 02: Section 02 Hammer Swing Obstacle
            if (includePhase02)
            {
                CreateSection02HammerSwing(envRoot.transform, stoneMat, neonMat, darkTrimMat);
            }

            // 8. Kill Volume (Spanning entire void under both sections)
            CreateKillVolume(envRoot.transform, includePhase02 ? 30f : 10f, includePhase02 ? 300f : 200f);

            // 9. UI Hierarchy (Canvas, Start Menu, HUD, Pause, GameOver)
            CreateUI(volume);

            // Save Scene
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[VoidboundSceneBuilder] Scene successfully saved to {ScenePath}");

            // Register in Build Settings
            UpdateBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[VoidboundSceneBuilder] Scene Build Complete! (Phase 02 Included: {includePhase02})");
        }

        private static void EnsureDirectories()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Scenes")) AssetDatabase.CreateFolder("Assets", "Scenes");
            if (!AssetDatabase.IsValidFolder("Assets/Materials")) AssetDatabase.CreateFolder("Assets", "Materials");
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs")) AssetDatabase.CreateFolder("Assets", "Prefabs");
        }

        private static Material GetOrCreateMaterial(string name, Color baseColor, float smoothness, bool emission = false, Color emissionColor = default)
        {
            string path = $"{MaterialsFolder}/{name}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");
                mat = new Material(shader);
                mat.color = baseColor;
                if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);

                if (emission)
                {
                    mat.EnableKeyword("_EMISSION");
                    if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", emissionColor);
                }
                AssetDatabase.CreateAsset(mat, path);
            }
            return mat;
        }

        private static void SetupLightingAndFog()
        {
            // Directional Light (Moonlight cyan)
            GameObject lightObj = new GameObject("Directional Light");
            Light light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(0.85f, 0.92f, 1f);
            light.intensity = 1.5f;
            light.shadows = LightShadows.Soft;
            lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            lightObj.AddComponent<UniversalAdditionalLightData>();

            // Luminous ambient and atmospheric deep void fog
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.24f, 0.28f, 0.38f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.08f, 0.12f, 0.22f);
            RenderSettings.fogStartDistance = 45f;
            RenderSettings.fogEndDistance = 250f;
        }

        private static Volume SetupGlobalVolume()
        {
            GameObject volObj = new GameObject("Global Volume");
            Volume volume = volObj.AddComponent<Volume>();
            volume.isGlobal = true;

            VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = "Voidbound_VolumeProfile";

            // Bloom
            Bloom bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(0.9f);
            bloom.intensity.Override(1.2f);
            bloom.scatter.Override(0.7f);

            // Depth of Field (used for Start Menu background blur)
            DepthOfField dof = profile.Add<DepthOfField>(true);
            dof.mode.Override(DepthOfFieldMode.Gaussian);
            dof.gaussianStart.Override(1.0f);
            dof.gaussianEnd.Override(15.0f);
            dof.active = true;

            // Vignette
            Vignette vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(0.35f);
            vignette.smoothness.Override(0.4f);

            AssetDatabase.CreateAsset(profile, "Assets/Settings/Voidbound_VolumeProfile.asset");
            volume.profile = profile;
            return volume;
        }

        private static GameObject CreatePlayer()
        {
            GameObject player = new GameObject("Player");
            player.tag = "Player";
            player.transform.position = new Vector3(0f, 1.0f, 0f);

            CharacterController cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.4f;
            cc.center = new Vector3(0f, 0.9f, 0f);
            cc.stepOffset = 0.3f;
            cc.slopeLimit = 45f;

            PlayerController pc = player.AddComponent<PlayerController>();
            PlayerDeathHandler pdh = player.AddComponent<PlayerDeathHandler>();
            PlayerInteractor pi = player.AddComponent<PlayerInteractor>();
            AudioSource audio = player.AddComponent<AudioSource>();
            audio.playOnAwake = false;

            // Camera (Exactly 1 active AudioListener in scene)
            GameObject camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            camObj.transform.SetParent(player.transform);
            camObj.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            camObj.transform.localRotation = Quaternion.identity;

            Camera cam = camObj.AddComponent<Camera>();
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 500f;
            cam.fieldOfView = 65f;
            cam.backgroundColor = new Color(0.08f, 0.12f, 0.22f, 1f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            camObj.AddComponent<AudioListener>();
            camObj.AddComponent<UniversalAdditionalCameraData>();

            return player;
        }

        private static void CreateTutorialPlatforms(Transform parent, Material stoneMat, Material neonMat, Material darkTrimMat)
        {
            // Platform 1: Start Platform (10 x 1 x 10)
            GameObject p1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            p1.name = "Platform_01_Start";
            p1.transform.SetParent(parent);
            p1.transform.position = new Vector3(0f, -0.5f, 0f);
            p1.transform.localScale = new Vector3(10f, 1f, 10f);
            p1.GetComponent<Renderer>().sharedMaterial = stoneMat;

            // Trims for Platform 1
            CreateTrim(p1.transform, new Vector3(0f, 0.51f, 4.9f), new Vector3(9.8f, 0.02f, 0.2f), neonMat);
            CreateTrim(p1.transform, new Vector3(0f, 0.51f, -4.9f), new Vector3(9.8f, 0.02f, 0.2f), darkTrimMat);

            // Tutorial decorative stone pillars on start platform
            CreatePillar(parent, new Vector3(-4.5f, 1.5f, -4.5f), stoneMat, darkTrimMat);
            CreatePillar(parent, new Vector3(4.5f, 1.5f, -4.5f), stoneMat, darkTrimMat);

            // Platform 2: Intermediate Jump (4 x 1 x 4) at Z = 11 (6m center-to-center gap)
            GameObject p2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            p2.name = "Platform_02_Intermediate";
            p2.transform.SetParent(parent);
            p2.transform.position = new Vector3(0f, -0.5f, 11f);
            p2.transform.localScale = new Vector3(4f, 1f, 4f);
            p2.GetComponent<Renderer>().sharedMaterial = stoneMat;
            CreateTrim(p2.transform, new Vector3(0f, 0.51f, 1.9f), new Vector3(3.8f, 0.02f, 0.2f), neonMat);

            // Platform 3: Finish Platform (8 x 1 x 8) at Z = 20
            GameObject p3 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            p3.name = "Platform_03_Finish";
            p3.transform.SetParent(parent);
            p3.transform.position = new Vector3(0f, -0.5f, 20f);
            p3.transform.localScale = new Vector3(8f, 1f, 8f);
            p3.GetComponent<Renderer>().sharedMaterial = stoneMat;
            CreateTrim(p3.transform, new Vector3(0f, 0.51f, -3.9f), new Vector3(7.8f, 0.02f, 0.2f), neonMat);

            // Stone Arch signaling transition to Section 2 (Hammer Swing)
            CreateGatewayArch(parent, new Vector3(0f, 0f, 23.5f), stoneMat, neonMat);
        }

        private static void CreateTrim(Transform parent, Vector3 localPos, Vector3 localScale, Material mat)
        {
            GameObject trim = GameObject.CreatePrimitive(PrimitiveType.Cube);
            trim.name = "Trim";
            trim.transform.SetParent(parent);
            trim.transform.localPosition = localPos;
            trim.transform.localScale = localScale;
            trim.GetComponent<Renderer>().sharedMaterial = mat;
            Object.DestroyImmediate(trim.GetComponent<Collider>());
        }

        private static void CreatePillar(Transform parent, Vector3 position, Material stoneMat, Material trimMat)
        {
            GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pillar.name = "Pillar";
            pillar.transform.SetParent(parent);
            pillar.transform.position = position;
            pillar.transform.localScale = new Vector3(0.8f, 3f, 0.8f);
            pillar.GetComponent<Renderer>().sharedMaterial = stoneMat;
        }

        private static void CreateGatewayArch(Transform parent, Vector3 position, Material stoneMat, Material neonMat)
        {
            GameObject arch = new GameObject("Gateway_Arch");
            arch.transform.SetParent(parent);
            arch.transform.position = position;

            // Left Post
            GameObject left = GameObject.CreatePrimitive(PrimitiveType.Cube);
            left.name = "Post_Left";
            left.transform.SetParent(arch.transform);
            left.transform.localPosition = new Vector3(-2f, 2f, 0f);
            left.transform.localScale = new Vector3(0.6f, 4f, 0.6f);
            left.GetComponent<Renderer>().sharedMaterial = stoneMat;

            // Right Post
            GameObject right = GameObject.CreatePrimitive(PrimitiveType.Cube);
            right.name = "Post_Right";
            right.transform.SetParent(arch.transform);
            right.transform.localPosition = new Vector3(2f, 2f, 0f);
            right.transform.localScale = new Vector3(0.6f, 4f, 0.6f);
            right.GetComponent<Renderer>().sharedMaterial = stoneMat;

            // Lintel
            GameObject top = GameObject.CreatePrimitive(PrimitiveType.Cube);
            top.name = "Lintel";
            top.transform.SetParent(arch.transform);
            top.transform.localPosition = new Vector3(0f, 4.2f, 0f);
            top.transform.localScale = new Vector3(4.8f, 0.5f, 0.8f);
            top.GetComponent<Renderer>().sharedMaterial = stoneMat;

            // Neon Trim on Lintel
            GameObject neonLintel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            neonLintel.name = "NeonLintel";
            neonLintel.transform.SetParent(arch.transform);
            neonLintel.transform.localPosition = new Vector3(0f, 3.9f, 0f);
            neonLintel.transform.localScale = new Vector3(4.4f, 0.08f, 0.1f);
            neonLintel.GetComponent<Renderer>().sharedMaterial = neonMat;
            Object.DestroyImmediate(neonLintel.GetComponent<Collider>());
        }

        private static void CreatePrototypeTerminal(Transform parent, Material baseMat, Material screenMat, Material neonMat)
        {
            GameObject terminal = new GameObject("Prototype_Terminal");
            terminal.transform.SetParent(parent);
            terminal.transform.position = new Vector3(-3f, 0f, 2.5f);
            Vector3 toSpawn = new Vector3(0f, 0f, 0f) - terminal.transform.position;
            toSpawn.y = 0f;
            terminal.transform.rotation = Quaternion.LookRotation(toSpawn.normalized, Vector3.up);

            // Stand
            GameObject stand = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stand.name = "Stand";
            stand.transform.SetParent(terminal.transform);
            stand.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            stand.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            stand.GetComponent<Renderer>().sharedMaterial = baseMat;

            // Screen / Interface Head (tilted upward toward player eye level)
            GameObject screen = GameObject.CreatePrimitive(PrimitiveType.Cube);
            screen.name = "ConsoleScreen";
            screen.transform.SetParent(terminal.transform);
            screen.transform.localPosition = new Vector3(0f, 1.1f, 0f);
            screen.transform.localRotation = Quaternion.Euler(-25f, 0f, 0f);
            screen.transform.localScale = new Vector3(0.7f, 0.4f, 0.15f);
            Renderer screenRenderer = screen.GetComponent<Renderer>();
            screenRenderer.sharedMaterial = screenMat;

            // Point Light for status glow
            GameObject lightObj = new GameObject("TerminalLight");
            lightObj.transform.SetParent(terminal.transform);
            lightObj.transform.localPosition = new Vector3(0f, 1.25f, 0.15f);
            Light light = lightObj.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = screenMat.color;
            light.range = 3.5f;
            light.intensity = 1.5f;

            // Interaction Trigger Collider
            BoxCollider trigger = terminal.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0f, 1.0f, 0f);
            trigger.size = new Vector3(1.5f, 2.0f, 1.5f);

            PrototypeTerminal terminalComp = terminal.AddComponent<PrototypeTerminal>();

            SerializedObject so = new SerializedObject(terminalComp);
            so.FindProperty("screenRenderer").objectReferenceValue = screenRenderer;
            so.FindProperty("statusLight").objectReferenceValue = light;
            so.FindProperty("terminalName").stringValue = "Void Terminal Alpha";
            so.ApplyModifiedProperties();
        }

        private static void CreatePrototypeFinishMarker(Transform parent, Material beaconMat, Material stoneMat, Material trimMat)
        {
            GameObject markerRoot = new GameObject("Prototype_Finish_Marker");
            markerRoot.transform.SetParent(parent);
            // Positioned to the right side of Platform 3 so the center path into Section 2 is open!
            markerRoot.transform.position = new Vector3(3.0f, 0f, 19.5f);

            // Stone Pedestal base
            GameObject pedestal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pedestal.name = "Pedestal";
            pedestal.transform.SetParent(markerRoot.transform);
            pedestal.transform.localPosition = new Vector3(0f, 0.4f, 0f);
            pedestal.transform.localScale = new Vector3(0.8f, 0.4f, 0.8f);
            pedestal.GetComponent<Renderer>().sharedMaterial = stoneMat;

            // Visual Diamond (Cube rotated 45 deg)
            GameObject diamond = GameObject.CreatePrimitive(PrimitiveType.Cube);
            diamond.name = "BeaconMesh";
            diamond.transform.SetParent(markerRoot.transform);
            diamond.transform.localPosition = new Vector3(0f, 1.3f, 0f);
            diamond.transform.localRotation = Quaternion.Euler(45f, 45f, 45f);
            diamond.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);
            Renderer r = diamond.GetComponent<Renderer>();
            r.sharedMaterial = beaconMat;
            Object.DestroyImmediate(diamond.GetComponent<Collider>());

            // Light
            GameObject lightObj = new GameObject("BeaconLight");
            lightObj.transform.SetParent(markerRoot.transform);
            lightObj.transform.localPosition = new Vector3(0f, 1.3f, 0f);
            Light light = lightObj.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = beaconMat.color;
            light.range = 5.0f;
            light.intensity = 2.5f;

            // Trigger
            SphereCollider sc = markerRoot.AddComponent<SphereCollider>();
            sc.isTrigger = true;
            sc.center = new Vector3(0f, 1.0f, 0f);
            sc.radius = 1.6f;

            PrototypeFinishMarker markerComp = markerRoot.AddComponent<PrototypeFinishMarker>();
            SerializedObject so = new SerializedObject(markerComp);
            so.FindProperty("beaconRenderer").objectReferenceValue = r;
            so.FindProperty("beaconLight").objectReferenceValue = light;
            so.FindProperty("markerTitle").stringValue = "Tutorial Platforming Area";
            so.ApplyModifiedProperties();
        }

        private static void CreateSection02HammerSwing(Transform parent, Material stoneMat, Material neonMat, Material darkTrimMat)
        {
            GameObject sec2Root = new GameObject("Section_02_HammerSwing");
            sec2Root.transform.SetParent(parent);

            // 1. Entrance Section Trigger at Z = 24
            GameObject triggerObj = new GameObject("Section_02_Entrance_Trigger");
            triggerObj.transform.SetParent(sec2Root.transform);
            triggerObj.transform.position = new Vector3(0f, 1.5f, 24f);
            BoxCollider triggerCol = triggerObj.AddComponent<BoxCollider>();
            triggerCol.isTrigger = true;
            triggerCol.size = new Vector3(4f, 3f, 1.5f);
            SectionTrigger secTrigger = triggerObj.AddComponent<SectionTrigger>();

            // 2. Suspended Stone Bridge
            GameObject bridgeRoot = new GameObject("Bridge");
            bridgeRoot.transform.SetParent(sec2Root.transform);

            // Approach segment (Z: 24 to 28, center Z = 26)
            CreateBridgeSegment(bridgeRoot.transform, "Bridge_Approach", new Vector3(0f, -0.5f, 26f), new Vector3(2.6f, 1f, 4f), stoneMat, darkTrimMat);

            // Swing Zone 1 (Hammer 1 at Z = 30; segment Z: 28 to 32, center Z = 30)
            CreateSwingZoneFloor(bridgeRoot.transform, "SwingZone_01_Floor", new Vector3(0f, -0.5f, 30f), new Vector3(2.6f, 1f, 4f), stoneMat, neonMat);

            // Safe Breathing Zone 1 (Z: 32 to 38, center Z = 35, length 6m, width 4.2m)
            CreateSafeZone(bridgeRoot.transform, "SafeZone_01", new Vector3(0f, -0.5f, 35f), new Vector3(4.2f, 1f, 6f), stoneMat, darkTrimMat, neonMat);

            // Swing Zone 2 (Hammer 2 at Z = 40; segment Z: 38 to 42, center Z = 40)
            CreateSwingZoneFloor(bridgeRoot.transform, "SwingZone_02_Floor", new Vector3(0f, -0.5f, 40f), new Vector3(2.6f, 1f, 4f), stoneMat, neonMat);

            // Safe Breathing Zone 2 (Z: 42 to 48, center Z = 45, length 6m, width 4.2m)
            CreateSafeZone(bridgeRoot.transform, "SafeZone_02", new Vector3(0f, -0.5f, 45f), new Vector3(4.2f, 1f, 6f), stoneMat, darkTrimMat, neonMat);

            // Swing Zone 3 (Hammer 3 at Z = 50; segment Z: 48 to 52, center Z = 50)
            CreateSwingZoneFloor(bridgeRoot.transform, "SwingZone_03_Floor", new Vector3(0f, -0.5f, 50f), new Vector3(2.6f, 1f, 4f), stoneMat, neonMat);

            // Exit segment (Z: 52 to 55, center Z = 53.5)
            CreateBridgeSegment(bridgeRoot.transform, "Bridge_Exit", new Vector3(0f, -0.5f, 53.5f), new Vector3(2.6f, 1f, 3f), stoneMat, darkTrimMat);

            // Landing / End Platform for Section 2 (Z: 55 to 63, center Z = 59, size 8 x 1 x 8)
            GameObject endPlatform = GameObject.CreatePrimitive(PrimitiveType.Cube);
            endPlatform.name = "Platform_04_Section2_End";
            endPlatform.transform.SetParent(sec2Root.transform);
            endPlatform.transform.position = new Vector3(0f, -0.5f, 59f);
            endPlatform.transform.localScale = new Vector3(8f, 1f, 8f);
            endPlatform.GetComponent<Renderer>().sharedMaterial = stoneMat;

            CreateTrim(endPlatform.transform, new Vector3(0f, 0.51f, -3.9f), new Vector3(7.8f, 0.02f, 0.2f), neonMat);
            CreatePillar(sec2Root.transform, new Vector3(-3.5f, 1.5f, 62.5f), stoneMat, darkTrimMat);
            CreatePillar(sec2Root.transform, new Vector3(3.5f, 1.5f, 62.5f), stoneMat, darkTrimMat);
            CreateGatewayArch(sec2Root.transform, new Vector3(0f, 0f, 62.8f), stoneMat, neonMat);

            // 3. Overhead Support Structure & Swinging Hammers
            GameObject hammersRoot = new GameObject("Hammers");
            hammersRoot.transform.SetParent(sec2Root.transform);

            // Overhead Longitudinal Support Girder along Y = 7.5m (Z = 24 to 56, length = 32m)
            GameObject girder = GameObject.CreatePrimitive(PrimitiveType.Cube);
            girder.name = "Overhead_Longitudinal_Spine";
            girder.transform.SetParent(hammersRoot.transform);
            girder.transform.position = new Vector3(0f, 7.5f, 40f);
            girder.transform.localScale = new Vector3(1.2f, 0.6f, 32f);
            girder.GetComponent<Renderer>().sharedMaterial = stoneMat;
            CreateTrim(girder.transform, new Vector3(0f, -0.31f, 0f), new Vector3(0.8f, 0.05f, 31.8f), neonMat);

            // Arches moved into the middles between the hammers / at boundaries (Z = 25, 35, 45, 55)
            // Away from hammers at Z = 30, 40, 50 so pillars NEVER save a player knocked by a hammer!
            CreateStoneArch(hammersRoot.transform, "Arch_01_Entrance", 25f, stoneMat, darkTrimMat, neonMat);
            CreateStoneArch(hammersRoot.transform, "Arch_02_Between_H1_H2", 35f, stoneMat, darkTrimMat, neonMat);
            CreateStoneArch(hammersRoot.transform, "Arch_03_Between_H2_H3", 45f, stoneMat, darkTrimMat, neonMat);
            CreateStoneArch(hammersRoot.transform, "Arch_04_Exit", 55f, stoneMat, darkTrimMat, neonMat);

            // Three Swinging Hammers (Pivoting at Z = 30, 40, 50 - 5 meters away from each arch!)
            // Hammer 1: Phase 0° (crosses center at t=0)
            CreateSwingingHammer(hammersRoot.transform, 1, 30f, 0f, 2.8f, 55f, stoneMat, darkTrimMat, neonMat);

            // Hammer 2: Phase 120°
            CreateSwingingHammer(hammersRoot.transform, 2, 40f, 120f, 2.8f, 55f, stoneMat, darkTrimMat, neonMat);

            // Hammer 3: Phase 240°
            CreateSwingingHammer(hammersRoot.transform, 3, 50f, 240f, 2.8f, 55f, stoneMat, darkTrimMat, neonMat);
        }

        private static void CreateSection03MovingPlatform(Transform parent, Material stoneMat, Material neonMat, Material darkTrimMat)
        {
            GameObject sec3Root = new GameObject("Section_03_MovingPlatform");
            sec3Root.transform.SetParent(parent);

            // 1. Entrance Section Trigger at Z = 63.5
            GameObject triggerObj = new GameObject("Section_03_Entrance_Trigger");
            triggerObj.transform.SetParent(sec3Root.transform);
            triggerObj.transform.position = new Vector3(0f, 1.5f, 63.5f);
            BoxCollider triggerCol = triggerObj.AddComponent<BoxCollider>();
            triggerCol.isTrigger = true;
            triggerCol.size = new Vector3(6f, 3f, 1.5f);
            SectionTrigger secTrigger = triggerObj.AddComponent<SectionTrigger>();
            SerializedObject soSecTrigger = new SerializedObject(secTrigger);
            soSecTrigger.FindProperty("sectionName").stringValue = "Section 3: Moving Platform";
            soSecTrigger.ApplyModifiedProperties();

            // 2. Checkpoint 1 at Entrance to Section 3 (Z = 65.0, monument at X = -1.8 for high visibility without blocking central path)
            GameObject cpObj = new GameObject("Checkpoint_01");
            cpObj.transform.SetParent(sec3Root.transform);
            cpObj.transform.position = new Vector3(0f, 0f, 65.0f);
            BoxCollider cpCol = cpObj.AddComponent<BoxCollider>();
            cpCol.isTrigger = true;
            cpCol.center = new Vector3(0f, 1.5f, 0f);
            cpCol.size = new Vector3(7.6f, 3.5f, 2.5f);

            GameObject spawnPointObj = new GameObject("SpawnPoint");
            spawnPointObj.transform.SetParent(cpObj.transform);
            spawnPointObj.transform.position = new Vector3(0f, 0.1f, 65.0f);
            spawnPointObj.transform.rotation = Quaternion.identity;

            // Visual Checkpoint Monument placed at X = -1.8, Z = 65.5 (prominently visible, clear of central walking path)
            GameObject cpPedestal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cpPedestal.name = "CheckpointPedestal";
            cpPedestal.transform.SetParent(cpObj.transform);
            cpPedestal.transform.localPosition = new Vector3(-1.8f, 0.4f, 0.5f);
            cpPedestal.transform.localScale = new Vector3(0.8f, 0.4f, 0.8f);
            cpPedestal.GetComponent<Renderer>().sharedMaterial = stoneMat;

            GameObject cpDiamond = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cpDiamond.name = "CheckpointIndicator";
            cpDiamond.transform.SetParent(cpObj.transform);
            cpDiamond.transform.localPosition = new Vector3(-1.8f, 1.4f, 0.5f);
            cpDiamond.transform.localRotation = Quaternion.Euler(45f, 45f, 45f);
            cpDiamond.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            Renderer cpRenderer = cpDiamond.GetComponent<Renderer>();
            cpRenderer.sharedMaterial = neonMat;
            Object.DestroyImmediate(cpDiamond.GetComponent<Collider>());

            GameObject cpLightObj = new GameObject("CheckpointLight");
            cpLightObj.transform.SetParent(cpObj.transform);
            cpLightObj.transform.localPosition = new Vector3(-1.8f, 1.4f, 0.5f);
            Light cpLight = cpLightObj.AddComponent<Light>();
            cpLight.type = LightType.Point;
            cpLight.color = neonMat.color;
            cpLight.range = 5.0f;
            cpLight.intensity = 0.6f;

            Checkpoint cpComp = cpObj.AddComponent<Checkpoint>();
            SerializedObject soCp = new SerializedObject(cpComp);
            soCp.FindProperty("checkpointName").stringValue = "Checkpoint 1";
            soCp.FindProperty("sectionName").stringValue = "Section 3: Moving Platform";
            soCp.FindProperty("spawnPointOverride").objectReferenceValue = spawnPointObj.transform;
            soCp.FindProperty("indicatorRenderer").objectReferenceValue = cpRenderer;
            soCp.FindProperty("indicatorLight").objectReferenceValue = cpLight;
            soCp.FindProperty("inactiveColor").colorValue = new Color(0.3f, 0.3f, 0.4f, 1f);
            soCp.FindProperty("activeColor").colorValue = new Color(0.1f, 0.95f, 1f, 1f);
            soCp.ApplyModifiedProperties();

            // 3. Platform 5: Departure Platform (8 x 1 x 12, Z: 63 to 75, center Z = 69)
            GameObject depPlat = GameObject.CreatePrimitive(PrimitiveType.Cube);
            depPlat.name = "Platform_05_Section3_Departure";
            depPlat.transform.SetParent(sec3Root.transform);
            depPlat.transform.position = new Vector3(0f, -0.5f, 69f);
            depPlat.transform.localScale = new Vector3(8f, 1f, 12f);
            depPlat.GetComponent<Renderer>().sharedMaterial = stoneMat;

            // Trims on Departure Platform
            CreateTrim(depPlat.transform, new Vector3(-0.49f, 0.51f, 0f), new Vector3(0.02f, 0.02f, 0.98f), darkTrimMat);
            CreateTrim(depPlat.transform, new Vector3(0.49f, 0.51f, 0f), new Vector3(0.02f, 0.02f, 0.98f), darkTrimMat);
            CreateTrim(depPlat.transform, new Vector3(0f, 0.51f, 0.49f), new Vector3(0.98f, 0.02f, 0.02f), neonMat); // Front ledge
            CreateTrim(depPlat.transform, new Vector3(0f, 0.51f, -0.49f), new Vector3(0.98f, 0.02f, 0.02f), darkTrimMat); // Entrance seam

            // Decorative Pillars on Departure Platform
            CreatePillar(sec3Root.transform, new Vector3(-3.8f, 1.5f, 66f), stoneMat, darkTrimMat);
            CreatePillar(sec3Root.transform, new Vector3(3.8f, 1.5f, 66f), stoneMat, darkTrimMat);
            CreatePillar(sec3Root.transform, new Vector3(-3.8f, 1.5f, 74f), stoneMat, darkTrimMat);
            CreatePillar(sec3Root.transform, new Vector3(3.8f, 1.5f, 74f), stoneMat, darkTrimMat);

            // 4. Pressure Plate on Departure Platform at Z = 73.2 (closer to the void edge at Z = 75.0)
            GameObject plateObj = new GameObject("PressurePlate");
            plateObj.transform.SetParent(sec3Root.transform);
            plateObj.transform.position = new Vector3(0f, 0f, 73.2f);

            // Base housing
            GameObject plateHousing = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plateHousing.name = "BaseHousing";
            plateHousing.transform.SetParent(plateObj.transform);
            plateHousing.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            plateHousing.transform.localScale = new Vector3(2.2f, 0.06f, 2.2f);
            plateHousing.GetComponent<Renderer>().sharedMaterial = darkTrimMat;
            Object.DestroyImmediate(plateHousing.GetComponent<Collider>());

            // Depressible pad
            GameObject padObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            padObj.name = "PlatePad";
            padObj.transform.SetParent(plateObj.transform);
            padObj.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            padObj.transform.localScale = new Vector3(1.8f, 0.1f, 1.8f);
            padObj.GetComponent<Renderer>().sharedMaterial = stoneMat;
            Object.DestroyImmediate(padObj.GetComponent<Collider>());

            // Glowing Rune trim on pad
            GameObject runeObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            runeObj.name = "RuneTrim";
            runeObj.transform.SetParent(padObj.transform);
            runeObj.transform.localPosition = new Vector3(0f, 0.51f, 0f);
            runeObj.transform.localScale = new Vector3(0.85f, 0.02f, 0.85f);
            Renderer runeRenderer = runeObj.GetComponent<Renderer>();
            runeRenderer.sharedMaterial = neonMat;
            Object.DestroyImmediate(runeObj.GetComponent<Collider>());

            // Plate Light
            GameObject plateLightObj = new GameObject("PlateLight");
            plateLightObj.transform.SetParent(plateObj.transform);
            plateLightObj.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            Light plLight = plateLightObj.AddComponent<Light>();
            plLight.type = LightType.Point;
            plLight.color = neonMat.color;
            plLight.range = 3.5f;
            plLight.intensity = 0.5f;

            // Trigger collider
            BoxCollider plateCol = plateObj.AddComponent<BoxCollider>();
            plateCol.isTrigger = true;
            plateCol.center = new Vector3(0f, 0.5f, 0f);
            plateCol.size = new Vector3(1.8f, 1.0f, 1.8f);

            PressurePlate plateComp = plateObj.AddComponent<PressurePlate>();
            SerializedObject soPlate = new SerializedObject(plateComp);
            soPlate.FindProperty("platePad").objectReferenceValue = padObj.transform;
            soPlate.FindProperty("plateRenderer").objectReferenceValue = runeRenderer;
            soPlate.FindProperty("plateLight").objectReferenceValue = plLight;
            soPlate.FindProperty("depressDistance").floatValue = 0.08f;
            soPlate.FindProperty("depressSpeed").floatValue = 8.0f;
            soPlate.FindProperty("inactiveColor").colorValue = new Color(0.15f, 0.45f, 0.6f, 1f);
            soPlate.FindProperty("activeColor").colorValue = new Color(0.1f, 0.95f, 1f, 1f);
            soPlate.FindProperty("inactiveLightIntensity").floatValue = 0.5f;
            soPlate.FindProperty("activeLightIntensity").floatValue = 2.5f;
            soPlate.ApplyModifiedProperties();

            // 5. Moving Platform (Starts at Point A: Z = 92.8 across the chasm)
            GameObject platObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            platObj.name = "MovingPlatform";
            platObj.transform.SetParent(sec3Root.transform);
            platObj.transform.position = new Vector3(0f, -0.5f, 92.8f);
            platObj.transform.localScale = new Vector3(4.0f, 0.8f, 4.0f);
            platObj.GetComponent<Renderer>().sharedMaterial = stoneMat;

            // Neon perimeter edge strips on moving platform
            CreateTrim(platObj.transform, new Vector3(0f, 0.51f, 0.48f), new Vector3(0.96f, 0.02f, 0.04f), neonMat);
            CreateTrim(platObj.transform, new Vector3(0f, 0.51f, -0.48f), new Vector3(0.96f, 0.02f, 0.04f), neonMat);
            CreateTrim(platObj.transform, new Vector3(-0.48f, 0.51f, 0f), new Vector3(0.04f, 0.02f, 0.96f), neonMat);
            CreateTrim(platObj.transform, new Vector3(0.48f, 0.51f, 0f), new Vector3(0.04f, 0.02f, 0.96f), neonMat);

            // Underside glow light
            GameObject platLightObj = new GameObject("PlatformLight");
            platLightObj.transform.SetParent(platObj.transform);
            platLightObj.transform.localPosition = new Vector3(0f, -0.3f, 0f);
            Light platLight = platLightObj.AddComponent<Light>();
            platLight.type = LightType.Point;
            platLight.color = neonMat.color;
            platLight.range = 6.0f;
            platLight.intensity = 1.8f;

            // Rigidbody
            Rigidbody platRb = platObj.AddComponent<Rigidbody>();
            platRb.isKinematic = true;
            platRb.interpolation = RigidbodyInterpolation.None;
            platRb.collisionDetectionMode = CollisionDetectionMode.Discrete;

            // Rider detector child trigger
            GameObject detectorObj = new GameObject("RiderDetector");
            detectorObj.transform.SetParent(platObj.transform);
            detectorObj.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            detectorObj.transform.localScale = Vector3.one;
            BoxCollider detCol = detectorObj.AddComponent<BoxCollider>();
            detCol.isTrigger = true;
            detCol.center = new Vector3(0f, 1.25f, 0f);
            detCol.size = new Vector3(0.99f, 2.5f, 0.99f); // Local to platform (4, 0.8, 4) -> 3.96m x 2.0m x 3.96m
            PlatformRiderDetector detectorComp = detectorObj.AddComponent<PlatformRiderDetector>();

            MovingPlatform platComp = platObj.AddComponent<MovingPlatform>();
            detectorComp.Initialize(platComp);

            SerializedObject soMovingPlat = new SerializedObject(platComp);
            soMovingPlat.FindProperty("pointA").vector3Value = new Vector3(0f, -0.5f, 92.8f);
            soMovingPlat.FindProperty("pointB").vector3Value = new Vector3(0f, -0.5f, 77.2f);
            soMovingPlat.FindProperty("moveSpeed").floatValue = 6.5f;
            soMovingPlat.FindProperty("returnDelay").floatValue = 0.8f;
            soMovingPlat.FindProperty("pressurePlate").objectReferenceValue = plateComp;
            soMovingPlat.ApplyModifiedProperties();

            // 6. Platform 6: Arrival Platform (8 x 1 x 12, Z: 95 to 107, center Z = 101)
            GameObject arrPlat = GameObject.CreatePrimitive(PrimitiveType.Cube);
            arrPlat.name = "Platform_06_Section3_Arrival";
            arrPlat.transform.SetParent(sec3Root.transform);
            arrPlat.transform.position = new Vector3(0f, -0.5f, 101f);
            arrPlat.transform.localScale = new Vector3(8f, 1f, 12f);
            arrPlat.GetComponent<Renderer>().sharedMaterial = stoneMat;

            // Trims on Arrival Platform
            CreateTrim(arrPlat.transform, new Vector3(-0.49f, 0.51f, 0f), new Vector3(0.02f, 0.02f, 0.98f), darkTrimMat);
            CreateTrim(arrPlat.transform, new Vector3(0.49f, 0.51f, 0f), new Vector3(0.02f, 0.02f, 0.98f), darkTrimMat);
            CreateTrim(arrPlat.transform, new Vector3(0f, 0.51f, -0.49f), new Vector3(0.98f, 0.02f, 0.02f), neonMat); // Near landing edge
            CreateTrim(arrPlat.transform, new Vector3(0f, 0.51f, 0.49f), new Vector3(0.98f, 0.02f, 0.02f), darkTrimMat); // Exit boundary

            // Decorative Pillars on Arrival Platform
            CreatePillar(sec3Root.transform, new Vector3(-3.8f, 1.5f, 96f), stoneMat, darkTrimMat);
            CreatePillar(sec3Root.transform, new Vector3(3.8f, 1.5f, 96f), stoneMat, darkTrimMat);
            CreatePillar(sec3Root.transform, new Vector3(-3.8f, 1.5f, 106f), stoneMat, darkTrimMat);
            CreatePillar(sec3Root.transform, new Vector3(3.8f, 1.5f, 106f), stoneMat, darkTrimMat);

            // Completion Gateway Arch at Z = 106.8
            CreateGatewayArch(sec3Root.transform, new Vector3(0f, 0f, 106.8f), stoneMat, neonMat);
        }

        private static void CreateSection04SpikePath(Transform parent, Material stoneMat, Material neonMat, Material darkTrimMat, Material spikeMat)
        {
            GameObject sec4Root = new GameObject("Section_04_SpikePath");
            sec4Root.transform.SetParent(parent);

            // 1. Entrance Trigger at Z = 107.5
            GameObject triggerObj = new GameObject("Section_04_Entrance_Trigger");
            triggerObj.transform.SetParent(sec4Root.transform);
            triggerObj.transform.position = new Vector3(0f, 1.5f, 107.5f);
            BoxCollider triggerCol = triggerObj.AddComponent<BoxCollider>();
            triggerCol.isTrigger = true;
            triggerCol.size = new Vector3(6f, 3f, 1.5f);
            SectionTrigger secTrigger = triggerObj.AddComponent<SectionTrigger>();
            SerializedObject soSecTrigger = new SerializedObject(secTrigger);
            soSecTrigger.FindProperty("sectionName").stringValue = "Section 4: Spike Path";
            soSecTrigger.ApplyModifiedProperties();

            // 2. Segment 4.1: Transition Apron & Narrow Traversal Bridge (Z: 107 to 123)
            CreateWalkwayPlatform(sec4Root.transform, "Platform_07_Section4_Apron", new Vector3(0f, -0.5f, 109f), new Vector3(4.5f, 1f, 4f), stoneMat, darkTrimMat, neonMat);
            CreateNarrowBridgeSegment(sec4Root.transform, "Bridge_Narrow_01", new Vector3(0f, -0.5f, 117f), new Vector3(2.2f, 1f, 12f), stoneMat, darkTrimMat, neonMat);

            CreateSpike(sec4Root.transform, "Spike_Narrow_01", new Vector3(0.5f, 0f, 115f), new Vector3(0.7f, 1.1f, 0.7f), spikeMat);
            CreateSpike(sec4Root.transform, "Spike_Narrow_02", new Vector3(-0.5f, 0f, 119f), new Vector3(0.7f, 1.1f, 0.7f), spikeMat);

            // 3. Segment 4.2: Safe Rest & Spike Clusters (Z: 123 to 137)
            CreateWalkwayPlatform(sec4Root.transform, "Platform_08_Cluster_Rest1", new Vector3(0f, -0.5f, 125f), new Vector3(4.5f, 1f, 4f), stoneMat, darkTrimMat, neonMat);
            CreateWalkwayPlatform(sec4Root.transform, "Platform_09_Cluster_Hurdle", new Vector3(0f, -0.5f, 130f), new Vector3(4.5f, 1f, 6f), stoneMat, darkTrimMat, neonMat);

            GameObject clusterRoot = new GameObject("SpikeCluster_01");
            clusterRoot.transform.SetParent(sec4Root.transform);
            float[] clusterX = new float[] { -1.0f, 0f, 1.0f };
            float[] clusterZ = new float[] { 129.2f, 130.8f };
            int spikeIdx = 1;
            for (int r = 0; r < clusterZ.Length; r++)
            {
                for (int c = 0; c < clusterX.Length; c++)
                {
                    CreateSpike(clusterRoot.transform, $"Spike_Cluster_{spikeIdx++}", new Vector3(clusterX[c], 0f, clusterZ[r]), new Vector3(0.75f, 1.15f, 0.75f), spikeMat);
                }
            }
            CreateClusterLight(clusterRoot.transform, new Vector3(0f, 0.6f, 130f), new Color(1f, 0.15f, 0.15f), 4.5f, 1.5f);

            CreateWalkwayPlatform(sec4Root.transform, "Platform_10_Cluster_Rest2", new Vector3(0f, -0.5f, 135f), new Vector3(4.5f, 1f, 4f), stoneMat, darkTrimMat, neonMat);

            // 4. Segment 4.3: Offset Precision Segment (Z: 137 to 155)
            CreateWalkwayPlatform(sec4Root.transform, "Platform_11_Offset_Precision", new Vector3(0f, -0.5f, 146f), new Vector3(4.5f, 1f, 18f), stoneMat, darkTrimMat, neonMat);

            GameObject barrier1 = new GameObject("SpikeBarrier_Offset_01");
            barrier1.transform.SetParent(sec4Root.transform);
            CreateSpike(barrier1.transform, "Spike_B1_1", new Vector3(-1.4f, 0f, 141.5f), new Vector3(0.7f, 1.1f, 0.7f), spikeMat);
            CreateSpike(barrier1.transform, "Spike_B1_2", new Vector3(-0.7f, 0f, 141.5f), new Vector3(0.7f, 1.1f, 0.7f), spikeMat);
            CreateSpike(barrier1.transform, "Spike_B1_3", new Vector3(0.0f, 0f, 141.5f), new Vector3(0.7f, 1.1f, 0.7f), spikeMat);

            GameObject barrier2 = new GameObject("SpikeBarrier_Offset_02");
            barrier2.transform.SetParent(sec4Root.transform);
            CreateSpike(barrier2.transform, "Spike_B2_1", new Vector3(0.0f, 0f, 146.5f), new Vector3(0.7f, 1.1f, 0.7f), spikeMat);
            CreateSpike(barrier2.transform, "Spike_B2_2", new Vector3(0.7f, 0f, 146.5f), new Vector3(0.7f, 1.1f, 0.7f), spikeMat);
            CreateSpike(barrier2.transform, "Spike_B2_3", new Vector3(1.4f, 0f, 146.5f), new Vector3(0.7f, 1.1f, 0.7f), spikeMat);

            GameObject barrier3 = new GameObject("SpikeBarrier_Offset_03");
            barrier3.transform.SetParent(sec4Root.transform);
            CreateSpike(barrier3.transform, "Spike_B3_1", new Vector3(-0.7f, 0f, 151.5f), new Vector3(0.7f, 1.1f, 0.7f), spikeMat);
            CreateSpike(barrier3.transform, "Spike_B3_2", new Vector3(0.0f, 0f, 151.5f), new Vector3(0.7f, 1.1f, 0.7f), spikeMat);
            CreateSpike(barrier3.transform, "Spike_B3_3", new Vector3(0.7f, 0f, 151.5f), new Vector3(0.7f, 1.1f, 0.7f), spikeMat);

            CreatePillar(sec4Root.transform, new Vector3(-2.4f, 1.5f, 139f), stoneMat, darkTrimMat);
            CreatePillar(sec4Root.transform, new Vector3(2.4f, 1.5f, 139f), stoneMat, darkTrimMat);
            CreatePillar(sec4Root.transform, new Vector3(-2.4f, 1.5f, 153f), stoneMat, darkTrimMat);
            CreatePillar(sec4Root.transform, new Vector3(2.4f, 1.5f, 153f), stoneMat, darkTrimMat);

            // 5. Segment 4.4: Section 4 Completion & Transition Platform (Z: 155 to 161)
            GameObject restPlat = GameObject.CreatePrimitive(PrimitiveType.Cube);
            restPlat.name = "Platform_12_Section4_Completion";
            restPlat.transform.SetParent(sec4Root.transform);
            restPlat.transform.position = new Vector3(0f, -0.5f, 158f);
            restPlat.transform.localScale = new Vector3(8f, 1f, 6f);
            restPlat.GetComponent<Renderer>().sharedMaterial = stoneMat;

            CreateTrim(restPlat.transform, new Vector3(-0.49f, 0.51f, 0f), new Vector3(0.02f, 0.02f, 0.98f), darkTrimMat);
            CreateTrim(restPlat.transform, new Vector3(0.49f, 0.51f, 0f), new Vector3(0.02f, 0.02f, 0.98f), darkTrimMat);
            CreateTrim(restPlat.transform, new Vector3(0f, 0.51f, -0.49f), new Vector3(0.98f, 0.02f, 0.02f), neonMat);
            CreateTrim(restPlat.transform, new Vector3(0f, 0.51f, 0.49f), new Vector3(0.98f, 0.02f, 0.02f), darkTrimMat);

            CreatePillar(sec4Root.transform, new Vector3(-3.8f, 1.5f, 156f), stoneMat, darkTrimMat);
            CreatePillar(sec4Root.transform, new Vector3(3.8f, 1.5f, 156f), stoneMat, darkTrimMat);
            CreatePillar(sec4Root.transform, new Vector3(-3.8f, 1.5f, 160f), stoneMat, darkTrimMat);
            CreatePillar(sec4Root.transform, new Vector3(3.8f, 1.5f, 160f), stoneMat, darkTrimMat);

            CreateGatewayArch(sec4Root.transform, new Vector3(0f, 0f, 160.8f), stoneMat, neonMat);
        }

        private static void CreateSection05LaserCorridor(Transform parent, Material stoneMat, Material neonMat, Material darkTrimMat, Material activeMat, Material warningMat, Material emitterMat)
        {
            GameObject sec5Root = new GameObject("Section_05_LaserCorridor");
            sec5Root.transform.SetParent(parent);

            // 1. Entrance Trigger at Z = 161.5
            GameObject triggerObj = new GameObject("Section_05_Entrance_Trigger");
            triggerObj.transform.SetParent(sec5Root.transform);
            triggerObj.transform.position = new Vector3(0f, 1.5f, 161.5f);
            BoxCollider triggerCol = triggerObj.AddComponent<BoxCollider>();
            triggerCol.isTrigger = true;
            triggerCol.size = new Vector3(6f, 3f, 1.5f);
            SectionTrigger secTrigger = triggerObj.AddComponent<SectionTrigger>();
            SerializedObject soSecTrigger = new SerializedObject(secTrigger);
            soSecTrigger.FindProperty("sectionName").stringValue = "Section 5: Laser Corridor";
            soSecTrigger.ApplyModifiedProperties();

            // 2. Corridor Walkway Platform (5 x 1 x 49) Z: 161 to 210 (center Z = 185.5)
            GameObject corridorPlat = GameObject.CreatePrimitive(PrimitiveType.Cube);
            corridorPlat.name = "Platform_13_LaserCorridor_Floor";
            corridorPlat.transform.SetParent(sec5Root.transform);
            corridorPlat.transform.position = new Vector3(0f, -0.5f, 185.5f);
            corridorPlat.transform.localScale = new Vector3(5f, 1f, 49f);
            corridorPlat.GetComponent<Renderer>().sharedMaterial = stoneMat;

            CreateTrim(corridorPlat.transform, new Vector3(-0.49f, 0.51f, 0f), new Vector3(0.02f, 0.02f, 0.98f), darkTrimMat);
            CreateTrim(corridorPlat.transform, new Vector3(0.49f, 0.51f, 0f), new Vector3(0.02f, 0.02f, 0.98f), darkTrimMat);

            // Decorative Corridor Columns along sides
            float[] columnZs = new float[] { 165f, 175f, 184f, 193f, 201f, 209f };
            for (int i = 0; i < columnZs.Length; i++)
            {
                CreatePillar(sec5Root.transform, new Vector3(-2.6f, 1.5f, columnZs[i]), stoneMat, darkTrimMat);
                CreatePillar(sec5Root.transform, new Vector3(2.6f, 1.5f, columnZs[i]), stoneMat, darkTrimMat);
            }

            // Overhead Archways between challenges
            CreateStoneArch(sec5Root.transform, "Arch_Laser_Rest1", 176f, stoneMat, darkTrimMat, neonMat);
            CreateStoneArch(sec5Root.transform, "Arch_Laser_Rest2", 191f, stoneMat, darkTrimMat, neonMat);

            // 3. Challenge 1: Single Pulsing Laser (Z = 170.0)
            CreateLaserHazard(sec5Root.transform, "Laser_Challenge1_Single", 170.0f, 1.0f, 2.5f, 1.0f, 1.5f, 0f, LaserState.Inactive, activeMat, warningMat, emitterMat);

            // 4. Challenge 2: Alternating Laser Pair (Z = 181.5 and Z = 186.5)
            CreateLaserHazard(sec5Root.transform, "Laser_Challenge2_PairA", 181.5f, 1.0f, 2.0f, 1.0f, 1.5f, 0f, LaserState.Active, activeMat, warningMat, emitterMat);
            CreateLaserHazard(sec5Root.transform, "Laser_Challenge2_PairB", 186.5f, 1.0f, 2.0f, 1.0f, 1.5f, 2.25f, LaserState.Active, activeMat, warningMat, emitterMat);

            // 5. Challenge 3: Sequential Laser Group (Z = 197.0, 201.0, 205.0)
            CreateLaserHazard(sec5Root.transform, "Laser_Challenge3_SeqA", 197.0f, 1.0f, 4.0f, 0.8f, 1.2f, 0.0f, LaserState.Warning, activeMat, warningMat, emitterMat);
            CreateLaserHazard(sec5Root.transform, "Laser_Challenge3_SeqB", 201.0f, 1.0f, 4.0f, 0.8f, 1.2f, 0.8f, LaserState.Warning, activeMat, warningMat, emitterMat);
            CreateLaserHazard(sec5Root.transform, "Laser_Challenge3_SeqC", 205.0f, 1.0f, 4.0f, 0.8f, 1.2f, 1.6f, LaserState.Warning, activeMat, warningMat, emitterMat);

            // 6. Section 5 Arrival / Transition Platform (8 x 1 x 12) Z: 210 to 222 (center Z = 216)
            GameObject arrivalPlat = GameObject.CreatePrimitive(PrimitiveType.Cube);
            arrivalPlat.name = "Platform_14_Section5_Arrival";
            arrivalPlat.transform.SetParent(sec5Root.transform);
            arrivalPlat.transform.position = new Vector3(0f, -0.5f, 216f);
            arrivalPlat.transform.localScale = new Vector3(8f, 1f, 12f);
            arrivalPlat.GetComponent<Renderer>().sharedMaterial = stoneMat;

            CreateTrim(arrivalPlat.transform, new Vector3(-0.49f, 0.51f, 0f), new Vector3(0.02f, 0.02f, 0.98f), darkTrimMat);
            CreateTrim(arrivalPlat.transform, new Vector3(0.49f, 0.51f, 0f), new Vector3(0.02f, 0.02f, 0.98f), darkTrimMat);
            CreateTrim(arrivalPlat.transform, new Vector3(0f, 0.51f, -0.49f), new Vector3(0.98f, 0.02f, 0.02f), neonMat);
            CreateTrim(arrivalPlat.transform, new Vector3(0f, 0.51f, 0.49f), new Vector3(0.98f, 0.02f, 0.02f), darkTrimMat);

            CreatePillar(sec5Root.transform, new Vector3(-3.8f, 1.5f, 211f), stoneMat, darkTrimMat);
            CreatePillar(sec5Root.transform, new Vector3(3.8f, 1.5f, 211f), stoneMat, darkTrimMat);
            CreatePillar(sec5Root.transform, new Vector3(-3.8f, 1.5f, 221f), stoneMat, darkTrimMat);
            CreatePillar(sec5Root.transform, new Vector3(3.8f, 1.5f, 221f), stoneMat, darkTrimMat);

            // Gateway Arch transitioning to Section 6 at Z = 221.5
            CreateGatewayArch(sec5Root.transform, new Vector3(0f, 0f, 221.5f), stoneMat, neonMat);
        }

        private static GameObject CreateWalkwayPlatform(Transform parent, string name, Vector3 pos, Vector3 scale, Material stoneMat, Material trimMat, Material neonMat)
        {
            GameObject plat = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plat.name = name;
            plat.transform.SetParent(parent);
            plat.transform.position = pos;
            plat.transform.localScale = scale;
            plat.GetComponent<Renderer>().sharedMaterial = stoneMat;

            CreateTrim(plat.transform, new Vector3(-0.49f, 0.51f, 0f), new Vector3(0.02f, 0.02f, 0.98f), trimMat);
            CreateTrim(plat.transform, new Vector3(0.49f, 0.51f, 0f), new Vector3(0.02f, 0.02f, 0.98f), trimMat);
            return plat;
        }

        private static GameObject CreateNarrowBridgeSegment(Transform parent, string name, Vector3 pos, Vector3 scale, Material stoneMat, Material trimMat, Material neonMat)
        {
            GameObject bridge = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bridge.name = name;
            bridge.transform.SetParent(parent);
            bridge.transform.position = pos;
            bridge.transform.localScale = scale;
            bridge.GetComponent<Renderer>().sharedMaterial = stoneMat;

            CreateTrim(bridge.transform, new Vector3(-0.48f, 0.51f, 0f), new Vector3(0.04f, 0.02f, 0.98f), trimMat);
            CreateTrim(bridge.transform, new Vector3(0.48f, 0.51f, 0f), new Vector3(0.04f, 0.02f, 0.98f), trimMat);
            return bridge;
        }

        private static GameObject CreateSpike(Transform parent, string name, Vector3 position, Vector3 size, Material spikeMat)
        {
            GameObject spikeObj = new GameObject(name);
            spikeObj.transform.SetParent(parent);
            spikeObj.transform.position = position;

            MeshFilter mf = spikeObj.AddComponent<MeshFilter>();
            mf.sharedMesh = GetOrCreatePyramidMesh(size.x, size.z, size.y);

            MeshRenderer mr = spikeObj.AddComponent<MeshRenderer>();
            mr.sharedMaterial = spikeMat;

            BoxCollider col = spikeObj.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.center = new Vector3(0f, size.y * 0.45f, 0f);
            col.size = new Vector3(size.x * 0.85f, size.y * 0.9f, size.z * 0.85f);

            GameObject lightObj = new GameObject("SpikeLight");
            lightObj.transform.SetParent(spikeObj.transform);
            lightObj.transform.localPosition = new Vector3(0f, size.y * 0.8f, 0f);
            Light l = lightObj.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(1.0f, 0.15f, 0.15f);
            l.range = 2.0f;
            l.intensity = 0.8f;

            SpikeHazard hazard = spikeObj.AddComponent<SpikeHazard>();
            SerializedObject so = new SerializedObject(hazard);
            so.FindProperty("hazardName").stringValue = "Impaled by Spikes";
            so.FindProperty("instantKill").boolValue = true;
            so.FindProperty("spikeRenderer").objectReferenceValue = mr;
            so.FindProperty("spikeLight").objectReferenceValue = l;
            so.ApplyModifiedProperties();

            return spikeObj;
        }

        private static Mesh GetOrCreatePyramidMesh(float width, float depth, float height)
        {
            Mesh mesh = new Mesh();
            mesh.name = $"Pyramid_{width}_{depth}_{height}";
            float hw = width * 0.5f;
            float hd = depth * 0.5f;

            Vector3 apex = new Vector3(0, height, 0);
            Vector3 p0 = new Vector3(-hw, 0, -hd);
            Vector3 p1 = new Vector3(hw, 0, -hd);
            Vector3 p2 = new Vector3(hw, 0, hd);
            Vector3 p3 = new Vector3(-hw, 0, hd);

            Vector3[] vertices = new Vector3[]
            {
                p0, p1, apex,
                p1, p2, apex,
                p2, p3, apex,
                p3, p0, apex,
                p0, p3, p2,
                p0, p2, p1
            };

            int[] triangles = new int[]
            {
                0, 2, 1,
                3, 5, 4,
                6, 8, 7,
                9, 11, 10,
                12, 13, 14,
                15, 16, 17
            };

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void CreateClusterLight(Transform parent, Vector3 pos, Color color, float range, float intensity)
        {
            GameObject lightObj = new GameObject("ClusterLight");
            lightObj.transform.SetParent(parent);
            lightObj.transform.position = pos;
            Light l = lightObj.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = color;
            l.range = range;
            l.intensity = intensity;
        }

        private static GameObject CreateLaserHazard(Transform parent, string name, float zPos, float height,
            float inactiveDur, float warningDur, float activeDur, float initialDelay, LaserState startState,
            Material activeMat, Material warningMat, Material emitterMat)
        {
            GameObject laserRoot = new GameObject(name);
            laserRoot.transform.SetParent(parent);
            laserRoot.transform.position = new Vector3(0f, height, zPos);

            // Left and Right emitter housings mounted on side posts
            GameObject emitterLeft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            emitterLeft.name = "Emitter_Left";
            emitterLeft.transform.SetParent(laserRoot.transform);
            emitterLeft.transform.localPosition = new Vector3(-2.35f, 0f, 0f);
            emitterLeft.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            emitterLeft.transform.localScale = new Vector3(0.35f, 0.15f, 0.35f);
            emitterLeft.GetComponent<Renderer>().sharedMaterial = emitterMat;
            Object.DestroyImmediate(emitterLeft.GetComponent<Collider>());

            GameObject emitterRight = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            emitterRight.name = "Emitter_Right";
            emitterRight.transform.SetParent(laserRoot.transform);
            emitterRight.transform.localPosition = new Vector3(2.35f, 0f, 0f);
            emitterRight.transform.localRotation = Quaternion.Euler(0f, 0f, -90f);
            emitterRight.transform.localScale = new Vector3(0.35f, 0.15f, 0.35f);
            emitterRight.GetComponent<Renderer>().sharedMaterial = emitterMat;
            Object.DestroyImmediate(emitterRight.GetComponent<Collider>());

            // Laser Beam (Cube spanning from -2.25 to +2.25 => width 4.5m)
            GameObject beamObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            beamObj.name = "BeamMesh";
            beamObj.transform.SetParent(laserRoot.transform);
            beamObj.transform.localPosition = Vector3.zero;
            beamObj.transform.localScale = new Vector3(4.5f, 0.12f, 0.12f);
            Renderer beamRenderer = beamObj.GetComponent<Renderer>();
            beamRenderer.sharedMaterial = activeMat;

            // Trigger BoxCollider with 0.5m depth in Z to reliably catch player at full sprint
            BoxCollider beamCol = beamObj.GetComponent<BoxCollider>();
            beamCol.isTrigger = true;
            beamCol.size = new Vector3(1.0f, 3.5f, 4.0f);

            // Central Beam Light
            GameObject lightObj = new GameObject("BeamLight");
            lightObj.transform.SetParent(laserRoot.transform);
            lightObj.transform.localPosition = Vector3.zero;
            Light beamLight = lightObj.AddComponent<Light>();
            beamLight.type = LightType.Point;
            beamLight.color = new Color(1f, 0.15f, 0.15f);
            beamLight.range = 5.0f;
            beamLight.intensity = 2.5f;

            // LaserHazard component
            LaserHazard hazard = laserRoot.AddComponent<LaserHazard>();
            SerializedObject so = new SerializedObject(hazard);
            so.FindProperty("inactiveDuration").floatValue = inactiveDur;
            so.FindProperty("warningDuration").floatValue = warningDur;
            so.FindProperty("activeDuration").floatValue = activeDur;
            so.FindProperty("initialDelay").floatValue = initialDelay;
            so.FindProperty("startingState").enumValueIndex = (int)startState;
            so.FindProperty("beamRenderer").objectReferenceValue = beamRenderer;
            so.FindProperty("beamCollider").objectReferenceValue = beamCol;
            so.FindProperty("beamLight").objectReferenceValue = beamLight;

            SerializedProperty emittersProp = so.FindProperty("emitterRenderers");
            emittersProp.arraySize = 2;
            emittersProp.GetArrayElementAtIndex(0).objectReferenceValue = emitterLeft.GetComponent<Renderer>();
            emittersProp.GetArrayElementAtIndex(1).objectReferenceValue = emitterRight.GetComponent<Renderer>();

            so.ApplyModifiedProperties();
            return laserRoot;
        }

        private static void CreateBridgeSegment(Transform parent, string name, Vector3 pos, Vector3 scale, Material stoneMat, Material trimMat)
        {
            GameObject seg = GameObject.CreatePrimitive(PrimitiveType.Cube);
            seg.name = name;
            seg.transform.SetParent(parent);
            seg.transform.position = pos;
            seg.transform.localScale = scale;
            seg.GetComponent<Renderer>().sharedMaterial = stoneMat;

            // Side edge trims
            float halfW = scale.x * 0.5f - 0.05f;
            CreateTrim(seg.transform, new Vector3(-halfW, 0.51f, 0f), new Vector3(0.1f, 0.02f, 0.98f), trimMat);
            CreateTrim(seg.transform, new Vector3(halfW, 0.51f, 0f), new Vector3(0.1f, 0.02f, 0.98f), trimMat);
        }

        private static void CreateSwingZoneFloor(Transform parent, string name, Vector3 pos, Vector3 scale, Material stoneMat, Material neonMat)
        {
            GameObject seg = GameObject.CreatePrimitive(PrimitiveType.Cube);
            seg.name = name;
            seg.transform.SetParent(parent);
            seg.transform.position = pos;
            seg.transform.localScale = scale;
            seg.GetComponent<Renderer>().sharedMaterial = stoneMat;

            // Glowing transverse neon hazard lines on floor to clearly demarcate the hammer strike zone
            CreateTrim(seg.transform, new Vector3(0f, 0.51f, 0.45f), new Vector3(0.95f, 0.02f, 0.05f), neonMat);
            CreateTrim(seg.transform, new Vector3(0f, 0.51f, -0.45f), new Vector3(0.95f, 0.02f, 0.05f), neonMat);
        }

        private static void CreateSafeZone(Transform parent, string name, Vector3 pos, Vector3 scale, Material stoneMat, Material trimMat, Material neonMat)
        {
            GameObject sz = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sz.name = name;
            sz.transform.SetParent(parent);
            sz.transform.position = pos;
            sz.transform.localScale = scale;
            sz.GetComponent<Renderer>().sharedMaterial = stoneMat;

            // Protective stone side bollards marking the safe boundary
            float halfW = scale.x * 0.5f - 0.35f;
            float halfL = scale.z * 0.5f - 0.6f;

            CreateBollard(sz.transform, new Vector3(-halfW, 0.8f, -halfL), trimMat, neonMat);
            CreateBollard(sz.transform, new Vector3(-halfW, 0.8f, halfL), trimMat, neonMat);
            CreateBollard(sz.transform, new Vector3(halfW, 0.8f, -halfL), trimMat, neonMat);
            CreateBollard(sz.transform, new Vector3(halfW, 0.8f, halfL), trimMat, neonMat);
        }

        private static void CreateBollard(Transform parent, Vector3 localPos, Material mat, Material neonMat)
        {
            GameObject b = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            b.name = "Bollard";
            b.transform.SetParent(parent);
            b.transform.localPosition = localPos;
            b.transform.localScale = new Vector3(0.25f, 0.4f, 0.25f);
            b.GetComponent<Renderer>().sharedMaterial = mat;

            GameObject lightObj = new GameObject("BollardLight");
            lightObj.transform.SetParent(b.transform);
            lightObj.transform.localPosition = new Vector3(0f, 1f, 0f);
            Light l = lightObj.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = neonMat.color;
            l.range = 3.0f;
            l.intensity = 1.2f;
        }

        private static void CreateStoneArch(Transform parent, string name, float zPos, Material stoneMat, Material darkTrimMat, Material neonMat)
        {
            GameObject archObj = new GameObject(name);
            archObj.transform.SetParent(parent);
            archObj.transform.position = new Vector3(0f, 0f, zPos);

            // Left Support Pillar placed wide at X = -3.8m to frame safe haven and keep swing clear
            GameObject leftPillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftPillar.name = "Pillar_Left";
            leftPillar.transform.SetParent(archObj.transform);
            leftPillar.transform.localPosition = new Vector3(-3.8f, 3.5f, 0f);
            leftPillar.transform.localScale = new Vector3(0.8f, 8f, 0.8f);
            leftPillar.GetComponent<Renderer>().sharedMaterial = stoneMat;

            // Right Support Pillar placed wide at X = 3.8m
            GameObject rightPillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rightPillar.name = "Pillar_Right";
            rightPillar.transform.SetParent(archObj.transform);
            rightPillar.transform.localPosition = new Vector3(3.8f, 3.5f, 0f);
            rightPillar.transform.localScale = new Vector3(0.8f, 8f, 0.8f);
            rightPillar.GetComponent<Renderer>().sharedMaterial = stoneMat;

            // Overhead Crossbeam / Lintel
            GameObject crossbeam = GameObject.CreatePrimitive(PrimitiveType.Cube);
            crossbeam.name = "Crossbeam";
            crossbeam.transform.SetParent(archObj.transform);
            crossbeam.transform.localPosition = new Vector3(0f, 7.5f, 0f);
            crossbeam.transform.localScale = new Vector3(8.4f, 0.8f, 1.0f);
            crossbeam.GetComponent<Renderer>().sharedMaterial = stoneMat;

            // Decorative Neon Trim on Crossbeam
            CreateTrim(crossbeam.transform, new Vector3(0f, -0.41f, 0f), new Vector3(8.0f, 0.05f, 0.2f), neonMat);
        }

        private static void CreateSwingingHammer(Transform parent, int index, float zPos, float phaseOffset, float period, float maxAngle, Material stoneMat, Material darkTrimMat, Material neonMat)
        {
            // Pivot Mount directly under the overhead spine
            GameObject pivotMount = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pivotMount.name = $"PivotMount_0{index}";
            pivotMount.transform.SetParent(parent);
            pivotMount.transform.position = new Vector3(0f, 7.1f, zPos);
            pivotMount.transform.localScale = new Vector3(1.2f, 0.5f, 0.8f);
            pivotMount.GetComponent<Renderer>().sharedMaterial = darkTrimMat;

            // --- SWINGING HAMMER HIERARCHY ---
            GameObject hammerRoot = new GameObject($"SwingingHammer_0{index}");
            hammerRoot.transform.SetParent(parent);
            hammerRoot.transform.position = new Vector3(0f, 7.1f, zPos);

            Rigidbody rb = hammerRoot.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

            SwingingHammer hammerComp = hammerRoot.AddComponent<SwingingHammer>();
            SerializedObject soHammer = new SerializedObject(hammerComp);
            soHammer.FindProperty("maxSwingAngle").floatValue = maxAngle;
            soHammer.FindProperty("swingPeriod").floatValue = period;
            soHammer.FindProperty("phaseOffsetDegrees").floatValue = phaseOffset;
            soHammer.FindProperty("swingAxis").vector3Value = Vector3.forward;
            soHammer.ApplyModifiedProperties();

            // Pendulum Arm (Shaft)
            GameObject arm = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            arm.name = "Arm";
            arm.transform.SetParent(hammerRoot.transform);
            arm.transform.localPosition = new Vector3(0f, -3.0f, 0f);
            arm.transform.localScale = new Vector3(0.2f, 3.0f, 0.2f);
            arm.GetComponent<Renderer>().sharedMaterial = darkTrimMat;
            Object.DestroyImmediate(arm.GetComponent<Collider>());

            // Hammer Head (Heavy push block)
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Cube);
            head.name = "HammerHead";
            head.transform.SetParent(hammerRoot.transform);
            head.transform.localPosition = new Vector3(0f, -5.9f, 0f);
            head.transform.localScale = new Vector3(1.4f, 1.2f, 1.4f);
            head.GetComponent<Renderer>().sharedMaterial = stoneMat;

            // HammerKnockback configuration on trigger collider to push player sideways off the bridge into the void
            BoxCollider triggerCol = head.GetComponent<BoxCollider>();
            triggerCol.isTrigger = true;
            HammerKnockback knockback = head.AddComponent<HammerKnockback>();
            SerializedObject soKnockback = new SerializedObject(knockback);
            soKnockback.FindProperty("horizontalKnockback").floatValue = 22.0f;
            soKnockback.FindProperty("upwardLift").floatValue = 3.5f;
            soKnockback.FindProperty("hitCooldown").floatValue = 0.35f;
            soKnockback.ApplyModifiedProperties();

            // Additional solid collider for physics/sweeps
            BoxCollider solidCol = head.AddComponent<BoxCollider>();
            solidCol.isTrigger = false;
            solidCol.size = new Vector3(1.35f, 1.15f, 1.35f);

            // Neon Trim stripes on Hammer Head
            CreateTrim(head.transform, new Vector3(0f, 0f, 0.51f), new Vector3(1.2f, 0.1f, 0.05f), neonMat);
            CreateTrim(head.transform, new Vector3(0f, 0f, -0.51f), new Vector3(1.2f, 0.1f, 0.05f), neonMat);

            // Dynamic Point Light on Hammer Head
            GameObject lightObj = new GameObject("HammerLight");
            lightObj.transform.SetParent(head.transform);
            lightObj.transform.localPosition = Vector3.zero;
            Light hl = lightObj.AddComponent<Light>();
            hl.type = LightType.Point;
            hl.color = neonMat.color;
            hl.range = 5.0f;
            hl.intensity = 2.0f;
        }

        private static void CreateKillVolume(Transform parent, float centerZ = 30f, float lengthZ = 300f)
        {
            GameObject kv = new GameObject("KillVolume_Pit");
            kv.transform.SetParent(parent);
            kv.transform.position = new Vector3(0f, -12f, centerZ);

            BoxCollider bc = kv.AddComponent<BoxCollider>();
            bc.isTrigger = true;
            bc.size = new Vector3(200f, 4f, lengthZ);

            kv.AddComponent<KillVolume>();
        }

        private static void CreateUI(Volume volume)
        {
            // 1. Event System
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<EventSystem>();
            esObj.AddComponent<InputSystemUIInputModule>();

            // 2. Canvas
            GameObject canvasObj = new GameObject("Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<GraphicRaycaster>();

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (defaultFont == null) defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");

            // Panel: Start Menu
            GameObject startMenu = CreatePanel(canvasObj.transform, "StartMenuPanel", new Color(0.02f, 0.04f, 0.10f, 0.88f), true);
            CreateText(startMenu.transform, "Title", "VOIDBOUND", defaultFont, 72, FontStyle.Bold, new Color(0.2f, 0.9f, 1f), new Vector2(0, 180), new Vector2(900, 120));
            CreateText(startMenu.transform, "Subtitle", "DEEP VOID PARKOUR EXPEDITION", defaultFont, 22, FontStyle.Normal, new Color(0.7f, 0.8f, 0.95f), new Vector2(0, 110), new Vector2(800, 40));

            GameObject startBtnObj = CreateButton(startMenu.transform, "StartButton", "INITIATE EXPEDITION", defaultFont, new Vector2(0, -10), new Vector2(320, 65), new Color(0.1f, 0.5f, 0.7f));
            Button startBtn = startBtnObj.GetComponent<Button>();

            CreateText(startMenu.transform, "ControlsInfo",
                "MISSION CONTROLS:\n[WASD] Move   |   [LEFT SHIFT] Sprint   |   [SPACE] Jump\n[E] Interact   |   [MOUSE] Look   |   [ESC] Pause",
                defaultFont, 20, FontStyle.Normal, new Color(0.6f, 0.75f, 0.9f), new Vector2(0, -160), new Vector2(800, 100));

            // Panel: HUD
            GameObject hud = CreatePanel(canvasObj.transform, "HUDPanel", Color.clear, false);

            // Crosshair
            GameObject crosshair = new GameObject("Crosshair");
            crosshair.transform.SetParent(hud.transform);
            RectTransform chRt = crosshair.AddComponent<RectTransform>();
            chRt.anchoredPosition = Vector2.zero;
            chRt.sizeDelta = new Vector2(6, 6);
            Image chImg = crosshair.AddComponent<Image>();
            chImg.color = new Color(1f, 1f, 1f, 0.85f);

            // Top Left Row: Time and Lives (anchored top-left)
            Vector2 topLeftAnchor = new Vector2(0f, 1f);
            Vector2 topLeftPivot = new Vector2(0f, 1f);

            Text timerText = CreateText(hud.transform, "TimerText", "TIME: 00:00.00", defaultFont, 24, FontStyle.Bold, new Color(0.2f, 0.95f, 1f), new Vector2(40, -35), new Vector2(240, 36), TextAnchor.MiddleLeft, topLeftAnchor, topLeftPivot);
            Text livesText = CreateText(hud.transform, "LivesText", "LIVES: ❤️ ❤️ ❤️", defaultFont, 24, FontStyle.Bold, new Color(0.2f, 0.95f, 1f), new Vector2(290, -35), new Vector2(280, 36), TextAnchor.MiddleLeft, topLeftAnchor, topLeftPivot);

            // Directly beneath Time: Section (anchored top-left)
            Text sectionText = CreateText(hud.transform, "SectionText", "SECTION 1: MOVEMENT TUTORIAL", defaultFont, 18, FontStyle.Normal, new Color(0.7f, 0.8f, 0.9f), new Vector2(40, -78), new Vector2(500, 32), TextAnchor.MiddleLeft, topLeftAnchor, topLeftPivot);

            // Top Right: Controls (anchored top-right)
            Vector2 topRightAnchor = new Vector2(1f, 1f);
            Vector2 topRightPivot = new Vector2(1f, 1f);
            Text hintsText = CreateText(hud.transform, "HintsText", "[WASD] Move   [SHIFT] Sprint   [SPACE] Jump\n[E] Interact   [ESC] Pause", defaultFont, 16, FontStyle.Normal, new Color(0.7f, 0.85f, 1f, 0.85f), new Vector2(-40, -35), new Vector2(650, 50), TextAnchor.UpperRight, topRightAnchor, topRightPivot);

            // Center / Bottom (Prompt & Notifications)
            Text promptText = CreateText(hud.transform, "InteractionPromptText", "", defaultFont, 22, FontStyle.Bold, new Color(1f, 0.85f, 0.3f), new Vector2(0, -180), new Vector2(800, 45));
            Text notifText = CreateText(hud.transform, "NotificationText", "", defaultFont, 22, FontStyle.Bold, new Color(0.3f, 1f, 0.85f), new Vector2(0, 260), new Vector2(900, 50));

            // Panel: Pause Menu
            GameObject pauseMenu = CreatePanel(canvasObj.transform, "PauseMenuPanel", new Color(0.02f, 0.04f, 0.10f, 0.92f), false);
            CreateText(pauseMenu.transform, "PauseTitle", "SYSTEM PAUSED", defaultFont, 56, FontStyle.Bold, new Color(0.2f, 0.9f, 1f), new Vector2(0, 140), new Vector2(600, 80));
            GameObject resumeBtnObj = CreateButton(pauseMenu.transform, "ResumeButton", "RESUME EXPEDITION", defaultFont, new Vector2(0, 20), new Vector2(300, 55), new Color(0.1f, 0.5f, 0.7f));
            GameObject pauseRestartBtnObj = CreateButton(pauseMenu.transform, "RestartButton", "RESTART RUN", defaultFont, new Vector2(0, -55), new Vector2(300, 55), new Color(0.6f, 0.2f, 0.25f));

            // Panel: Game Over Menu
            GameObject gameOverMenu = CreatePanel(canvasObj.transform, "GameOverPanel", new Color(0.12f, 0.02f, 0.04f, 0.94f), false);
            CreateText(gameOverMenu.transform, "GameOverTitle", "VESSEL LOST TO THE VOID", defaultFont, 52, FontStyle.Bold, new Color(1f, 0.3f, 0.3f), new Vector2(0, 120), new Vector2(800, 80));
            CreateText(gameOverMenu.transform, "GameOverSubtitle", "All Lives Consumed. Course Integrity Reset.", defaultFont, 22, FontStyle.Normal, new Color(0.9f, 0.7f, 0.7f), new Vector2(0, 50), new Vector2(600, 40));
            GameObject goRestartBtnObj = CreateButton(gameOverMenu.transform, "GameOverRestartButton", "TRY AGAIN", defaultFont, new Vector2(0, -50), new Vector2(280, 55), new Color(0.8f, 0.25f, 0.25f));

            // Hook up UIManager
            UIManager ui = canvasObj.AddComponent<UIManager>();
            SerializedObject so = new SerializedObject(ui);
            so.FindProperty("startMenuPanel").objectReferenceValue = startMenu;
            so.FindProperty("hudPanel").objectReferenceValue = hud;
            so.FindProperty("pauseMenuPanel").objectReferenceValue = pauseMenu;
            so.FindProperty("gameOverPanel").objectReferenceValue = gameOverMenu;

            so.FindProperty("startButton").objectReferenceValue = startBtn;
            so.FindProperty("resumeButton").objectReferenceValue = resumeBtnObj.GetComponent<Button>();
            so.FindProperty("pauseRestartButton").objectReferenceValue = pauseRestartBtnObj.GetComponent<Button>();
            so.FindProperty("gameOverRestartButton").objectReferenceValue = goRestartBtnObj.GetComponent<Button>();

            so.FindProperty("crosshairImage").objectReferenceValue = chImg;
            so.FindProperty("interactionPromptText").objectReferenceValue = promptText;
            so.FindProperty("livesText").objectReferenceValue = livesText;
            so.FindProperty("sectionText").objectReferenceValue = sectionText;
            so.FindProperty("timerText").objectReferenceValue = timerText;
            so.FindProperty("controlsTutorialText").objectReferenceValue = hintsText;
            so.FindProperty("notificationText").objectReferenceValue = notifText;

            so.FindProperty("postProcessVolume").objectReferenceValue = volume;
            so.ApplyModifiedProperties();
        }

        private static GameObject CreatePanel(Transform parent, string name, Color bgColor, bool active)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            RectTransform rt = panel.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;

            Image img = panel.AddComponent<Image>();
            img.color = bgColor;
            panel.SetActive(active);
            return panel;
        }

        private static Text CreateText(Transform parent, string name, string content, Font font, int fontSize, FontStyle style, Color color, Vector2 pos, Vector2 size, TextAnchor alignment = TextAnchor.MiddleCenter, Vector2? anchor = null, Vector2? pivot = null)
        {
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(parent, false);
            RectTransform rt = textObj.AddComponent<RectTransform>();
            if (anchor.HasValue)
            {
                rt.anchorMin = anchor.Value;
                rt.anchorMax = anchor.Value;
            }
            if (pivot.HasValue)
            {
                rt.pivot = pivot.Value;
            }
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            Text text = textObj.AddComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.text = content;
            text.color = color;
            text.alignment = alignment;
            return text;
        }

        private static GameObject CreateButton(Transform parent, string name, string label, Font font, Vector2 pos, Vector2 size, Color btnColor)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);
            RectTransform rt = btnObj.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            Image img = btnObj.AddComponent<Image>();
            img.color = btnColor;

            Button btn = btnObj.AddComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.highlightedColor = btnColor * 1.25f;
            cb.pressedColor = btnColor * 0.8f;
            btn.colors = cb;

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);
            RectTransform textRt = textObj.AddComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.sizeDelta = Vector2.zero;

            Text text = textObj.AddComponent<Text>();
            text.font = font;
            text.fontSize = 20;
            text.fontStyle = FontStyle.Bold;
            text.text = label;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;

            return btnObj;
        }

        private static void UpdateBuildSettings()
        {
            var currentScenes = EditorBuildSettings.scenes;
            bool found = false;
            foreach (var s in currentScenes)
            {
                if (s.path == ScenePath)
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                var newScenes = new EditorBuildSettingsScene[currentScenes.Length + 1];
                newScenes[0] = new EditorBuildSettingsScene(ScenePath, true);
                for (int i = 0; i < currentScenes.Length; i++)
                {
                    newScenes[i + 1] = currentScenes[i];
                }
                EditorBuildSettings.scenes = newScenes;
                Debug.Log($"[VoidboundSceneBuilder] Added {ScenePath} to Build Settings at index 0.");
            }
        }
    }
}
