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
            Debug.Log("[VoidboundSceneBuilder] Starting Phase 01 Scene Generation...");

            EnsureDirectories();
            Material stoneMat = GetOrCreateMaterial("M_Stone_Platform", new Color(0.12f, 0.14f, 0.18f), 0.2f);
            Material neonMat = GetOrCreateMaterial("M_Neon_Cyan", new Color(0.1f, 0.9f, 1f), 0.8f, true, new Color(0.1f, 0.9f, 1f) * 2.5f);
            Material screenMat = GetOrCreateMaterial("M_Terminal_Screen", new Color(1f, 0.6f, 0.1f), 0.5f, true, new Color(1f, 0.6f, 0.1f) * 2.0f);
            Material beaconMat = GetOrCreateMaterial("M_Beacon_Finish", new Color(0.2f, 1f, 0.8f), 0.8f, true, new Color(0.2f, 1f, 0.8f) * 3.0f);
            Material darkTrimMat = GetOrCreateMaterial("M_Dark_Trim", new Color(0.06f, 0.08f, 0.12f), 0.4f);

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

            // 5. Prototype Terminal
            CreatePrototypeTerminal(envRoot.transform, stoneMat, screenMat, neonMat);

            // 6. Prototype Finish Marker
            CreatePrototypeFinishMarker(envRoot.transform, beaconMat);

            // 7. Kill Volume
            CreateKillVolume(envRoot.transform);

            // 8. UI Hierarchy (Canvas, Start Menu, HUD, Pause, GameOver)
            CreateUI(volume);

            // Save Scene
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[VoidboundSceneBuilder] Scene successfully saved to {ScenePath}");

            // Register in Build Settings
            UpdateBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[VoidboundSceneBuilder] Phase 01 Scene Build Complete!");
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
            light.color = new Color(0.72f, 0.85f, 1f);
            light.intensity = 1.0f;
            light.shadows = LightShadows.Soft;
            lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            lightObj.AddComponent<UniversalAdditionalLightData>();

            // Dark void ambient and fog
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.04f, 0.06f, 0.12f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.02f, 0.03f, 0.08f);
            RenderSettings.fogStartDistance = 25f;
            RenderSettings.fogEndDistance = 120f;
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
            dof.active = true; // Active on start menu

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
            cam.backgroundColor = new Color(0.01f, 0.02f, 0.05f, 1f);
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

            // Platform 3: Finish Platform (8 x 1 x 8) at Z = 20 (5m gap)
            GameObject p3 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            p3.name = "Platform_03_Finish";
            p3.transform.SetParent(parent);
            p3.transform.position = new Vector3(0f, -0.5f, 20f);
            p3.transform.localScale = new Vector3(8f, 1f, 8f);
            p3.GetComponent<Renderer>().sharedMaterial = stoneMat;
            CreateTrim(p3.transform, new Vector3(0f, 0.51f, -3.9f), new Vector3(7.8f, 0.02f, 0.2f), neonMat);

            // Stone Arch signaling transition to Section 2 (Hammer Swing) in future phase
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
            GameObject arch = new GameObject("Gateway_Arch_Preview_Sec2");
            arch.transform.SetParent(parent);
            arch.transform.position = position;

            // Left Post
            GameObject left = GameObject.CreatePrimitive(PrimitiveType.Cube);
            left.transform.SetParent(arch.transform);
            left.transform.localPosition = new Vector3(-2f, 2f, 0f);
            left.transform.localScale = new Vector3(0.6f, 4f, 0.6f);
            left.GetComponent<Renderer>().sharedMaterial = stoneMat;

            // Right Post
            GameObject right = GameObject.CreatePrimitive(PrimitiveType.Cube);
            right.transform.SetParent(arch.transform);
            right.transform.localPosition = new Vector3(2f, 2f, 0f);
            right.transform.localScale = new Vector3(0.6f, 4f, 0.6f);
            right.GetComponent<Renderer>().sharedMaterial = stoneMat;

            // Lintel
            GameObject top = GameObject.CreatePrimitive(PrimitiveType.Cube);
            top.transform.SetParent(arch.transform);
            top.transform.localPosition = new Vector3(0f, 4.2f, 0f);
            top.transform.localScale = new Vector3(4.8f, 0.5f, 0.8f);
            top.GetComponent<Renderer>().sharedMaterial = stoneMat;

            // Neon Trim on Lintel
            GameObject neonLintel = GameObject.CreatePrimitive(PrimitiveType.Cube);
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
            terminal.transform.rotation = Quaternion.Euler(0f, 30f, 0f);

            // Stand
            GameObject stand = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stand.name = "Stand";
            stand.transform.SetParent(terminal.transform);
            stand.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            stand.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            stand.GetComponent<Renderer>().sharedMaterial = baseMat;

            // Screen / Interface Head
            GameObject screen = GameObject.CreatePrimitive(PrimitiveType.Cube);
            screen.name = "ConsoleScreen";
            screen.transform.SetParent(terminal.transform);
            screen.transform.localPosition = new Vector3(0f, 1.1f, 0f);
            screen.transform.localRotation = Quaternion.Euler(30f, 0f, 0f);
            screen.transform.localScale = new Vector3(0.7f, 0.4f, 0.15f);
            Renderer screenRenderer = screen.GetComponent<Renderer>();
            screenRenderer.sharedMaterial = screenMat;

            // Point Light for status glow
            GameObject lightObj = new GameObject("TerminalLight");
            lightObj.transform.SetParent(terminal.transform);
            lightObj.transform.localPosition = new Vector3(0f, 1.3f, 0.1f);
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

            // Use SerializedObject to set private serialized fields
            SerializedObject so = new SerializedObject(terminalComp);
            so.FindProperty("screenRenderer").objectReferenceValue = screenRenderer;
            so.FindProperty("statusLight").objectReferenceValue = light;
            so.FindProperty("terminalName").stringValue = "Void Terminal Alpha";
            so.ApplyModifiedProperties();
        }

        private static void CreatePrototypeFinishMarker(Transform parent, Material beaconMat)
        {
            GameObject marker = new GameObject("Prototype_Finish_Marker");
            marker.transform.SetParent(parent);
            marker.transform.position = new Vector3(0f, 1.2f, 20f);

            // Visual Diamond (Cube rotated 45 deg)
            GameObject diamond = GameObject.CreatePrimitive(PrimitiveType.Cube);
            diamond.name = "BeaconMesh";
            diamond.transform.SetParent(marker.transform);
            diamond.transform.localPosition = Vector3.zero;
            diamond.transform.localRotation = Quaternion.Euler(45f, 45f, 45f);
            diamond.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);
            Renderer r = diamond.GetComponent<Renderer>();
            r.sharedMaterial = beaconMat;
            Object.DestroyImmediate(diamond.GetComponent<Collider>());

            // Light
            GameObject lightObj = new GameObject("BeaconLight");
            lightObj.transform.SetParent(marker.transform);
            lightObj.transform.localPosition = Vector3.zero;
            Light light = lightObj.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = beaconMat.color;
            light.range = 6.0f;
            light.intensity = 3.0f;

            // Trigger
            SphereCollider sc = marker.AddComponent<SphereCollider>();
            sc.isTrigger = true;
            sc.radius = 1.5f;

            PrototypeFinishMarker markerComp = marker.AddComponent<PrototypeFinishMarker>();
            SerializedObject so = new SerializedObject(markerComp);
            so.FindProperty("beaconRenderer").objectReferenceValue = r;
            so.FindProperty("beaconLight").objectReferenceValue = light;
            so.FindProperty("markerTitle").stringValue = "Tutorial Platforming Area";
            so.ApplyModifiedProperties();
        }

        private static void CreateKillVolume(Transform parent)
        {
            GameObject kv = new GameObject("KillVolume_Pit");
            kv.transform.SetParent(parent);
            kv.transform.position = new Vector3(0f, -12f, 10f);

            BoxCollider bc = kv.AddComponent<BoxCollider>();
            bc.isTrigger = true;
            bc.size = new Vector3(200f, 4f, 200f);

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

            // Top Left Info (Lives & Section)
            Text livesText = CreateText(hud.transform, "LivesText", "LIVES: ❤️ ❤️ ❤️", defaultFont, 24, FontStyle.Bold, new Color(0.2f, 0.95f, 1f), new Vector2(180, -50), new Vector2(320, 40), TextAnchor.MiddleLeft);
            Text sectionText = CreateText(hud.transform, "SectionText", "SECTION 1: MOVEMENT TUTORIAL", defaultFont, 18, FontStyle.Normal, new Color(0.7f, 0.8f, 0.9f), new Vector2(230, -85), new Vector2(420, 35), TextAnchor.MiddleLeft);

            // Top Right Info (Timer)
            Text timerText = CreateText(hud.transform, "TimerText", "TIME: 00:00.0", defaultFont, 24, FontStyle.Bold, new Color(0.2f, 0.95f, 1f), new Vector2(-150, -50), new Vector2(260, 40), TextAnchor.MiddleRight);

            // Bottom Center (Prompt & Notifications)
            Text promptText = CreateText(hud.transform, "InteractionPromptText", "", defaultFont, 22, FontStyle.Bold, new Color(1f, 0.85f, 0.3f), new Vector2(0, -180), new Vector2(800, 45));
            Text notifText = CreateText(hud.transform, "NotificationText", "", defaultFont, 22, FontStyle.Bold, new Color(0.3f, 1f, 0.85f), new Vector2(0, 260), new Vector2(900, 50));

            // Bottom Left Hints
            Text hintsText = CreateText(hud.transform, "HintsText", "[WASD] Move   [SHIFT] Sprint   [SPACE] Jump   [E] Interact", defaultFont, 16, FontStyle.Normal, new Color(0.6f, 0.7f, 0.8f, 0.7f), new Vector2(240, 40), new Vector2(460, 30), TextAnchor.MiddleLeft);

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

        private static Text CreateText(Transform parent, string name, string content, Font font, int fontSize, FontStyle style, Color color, Vector2 pos, Vector2 size, TextAnchor alignment = TextAnchor.MiddleCenter)
        {
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(parent, false);
            RectTransform rt = textObj.AddComponent<RectTransform>();
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
