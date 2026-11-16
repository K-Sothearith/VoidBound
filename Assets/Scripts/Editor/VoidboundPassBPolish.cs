using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Voidbound.UI;

namespace Voidbound.Editor
{
    /// <summary>Targeted presentation changes only. Never rebuilds the level or creates physics/audio components.</summary>
    public static class VoidboundPassBPolish
    {
        public const string ProfilePath = "Assets/Settings/Voidbound_VolumeProfile.asset";
        private static readonly Color Ink = new Color(.018f,.035f,.065f,1);
        private static readonly Color Cyan = new Color(.24f,.91f,1f,1);

        [MenuItem("Voidbound/Apply Pass B Visual Polish")]
        public static void Apply()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Voidbound.unity");
            var protectedState = ProtectedState();
            var volume = UnityEngine.Object.FindFirstObjectByType<Volume>();
            if (volume == null) throw new InvalidOperationException("The saved scene has no Volume.");
            volume.sharedProfile = ConfigureProfile();
            volume.enabled = true; volume.isGlobal = true; volume.weight = 1; volume.priority = 10;
            var camera = Camera.main;
            var cameraData = camera.GetUniversalAdditionalCameraData();
            cameraData.renderPostProcessing = true;
            cameraData.volumeLayerMask |= 1 << volume.gameObject.layer;
            cameraData.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
            camera.allowHDR = true;
            camera.backgroundColor = new Color(.022f,.036f,.07f,1);
            var pipeline = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (pipeline == null || !pipeline.supportsHDR) throw new InvalidOperationException("An HDR URP asset is required.");

            ConfigureAtmosphere();
            ConfigureInterface();
            foreach (var pair in protectedState)
                if (pair.Value != State(pair.Key)) throw new InvalidOperationException("Presentation altered protected component " + pair.Key);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(ProfilePath, ImportAssetOptions.ForceSynchronousImport);
            EditorSceneManager.OpenScene(scene.path);
            VerifySavedProfile();
            Debug.Log("[Pass B] Saved and reopened visual polish; all protected gameplay/physics/audio components unchanged.");
        }

        private static Dictionary<Component,string> ProtectedState()
        {
            return UnityEngine.Object.FindObjectsByType<Component>(FindObjectsInactive.Include,FindObjectsSortMode.None)
                .Where(c=>c != null && (c is Collider || c is Rigidbody || c is AudioSource || c is AudioListener ||
                    c.GetType().Namespace == "Voidbound.Player" || c.GetType().Namespace == "Voidbound.Core" ||
                    c.GetType().Namespace == "Voidbound.Obstacles" || c.GetType().Namespace == "Voidbound.Interactions"))
                .ToDictionary(c=>c, State);
        }

        private static string State(Component c) => EditorJsonUtility.ToJson(c) +
            ((c is Collider || c is Rigidbody) ? c.transform.localToWorldMatrix.ToString("R") : "");

        public static VolumeProfile ConfigureProfile()
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile,ProfilePath);
            }
            profile.components.RemoveAll(c=>c == null);
            var bloom = Effect<Bloom>(profile);
            bloom.threshold.Override(1.1f); bloom.intensity.Override(.24f); bloom.scatter.Override(.45f);
            bloom.clamp.Override(6f); bloom.highQualityFiltering.Override(false); bloom.maxIterations.Override(4);
            var vignette = Effect<Vignette>(profile);
            vignette.intensity.Override(.16f); vignette.smoothness.Override(.5f); vignette.color.Override(new Color(.005f,.012f,.025f));
            var tone = Effect<Tonemapping>(profile); tone.mode.Override(TonemappingMode.Neutral);
            var color = Effect<ColorAdjustments>(profile);
            color.postExposure.Override(.05f); color.contrast.Override(5f); color.saturation.Override(-4f);
            var depth = Effect<DepthOfField>(profile);
            depth.mode.Override(DepthOfFieldMode.Gaussian); depth.gaussianStart.Override(8f);
            depth.gaussianEnd.Override(22f); depth.gaussianMaxRadius.Override(.65f);
            depth.active = false; // UIManager enables the runtime clone only on the start menu.
            EditorUtility.SetDirty(profile);
            foreach(var effect in profile.components) EditorUtility.SetDirty(effect);
            return profile;
        }

        private static T Effect<T>(VolumeProfile profile) where T : VolumeComponent
        {
            if (!profile.TryGet<T>(out var component)) component = profile.Add<T>(true);
            component.active = true;
            if (!AssetDatabase.Contains(component)) AssetDatabase.AddObjectToAsset(component,profile);
            return component;
        }

        public static void VerifySavedProfile()
        {
            var volume = UnityEngine.Object.FindFirstObjectByType<Volume>();
            var profile = volume.sharedProfile;
            if (profile == null || profile.components.Count != 5 || profile.components.Any(c=>c == null))
                throw new InvalidOperationException("Volume effects are not persisted as five subassets: " +
                    (profile == null ? "profile null" : $"count={profile.components.Count}; " + string.Join("; ",profile.components.Select(c=>c==null?"NULL":c.name+" subasset="+AssetDatabase.IsSubAsset(c)+" path="+AssetDatabase.GetAssetPath(c)))));
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(profile,out string profileGuid,out long profileId);
            foreach(var component in profile.components)
            {
                if(!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(component,out string guid,out long id) ||
                    guid!=profileGuid || id==0 || id==profileId)
                    throw new InvalidOperationException("Unpersisted Volume override: "+component.name);
                Debug.Log($"[Pass B] Persisted override {component.name}: guid={guid} localId={id}");
            }
            if (!Camera.main.GetUniversalAdditionalCameraData().renderPostProcessing)
                throw new InvalidOperationException("Saved camera post-processing is disabled.");
            int emissiveCount=0;
            foreach(var path in AssetDatabase.GetDependencies("Assets/Scenes/Voidbound.unity",true)
                .Where(p=>p.StartsWith("Assets/Materials/M_")&&p.EndsWith(".mat")))
            {
                var material=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(!material.HasProperty("_EmissionColor"))continue;
                var color=material.GetColor("_EmissionColor");
                if(color.r+color.g+color.b<=.001f)continue;
                if(!material.IsKeywordEnabled("_EMISSION"))throw new InvalidOperationException("Emission lost on import: "+path);
                emissiveCount++;
            }
            Debug.Log("[Pass B] Persisted emissive material keywords verified: "+emissiveCount);
            Debug.Log("[Pass B] Persistence PASS: assigned shared profile, five persisted overrides, camera post-processing enabled.");
        }

        private static void ConfigureAtmosphere()
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.22f,.28f,.4f);
            RenderSettings.ambientEquatorColor = new Color(.14f,.18f,.25f);
            RenderSettings.ambientGroundColor = new Color(.065f,.09f,.13f);
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(.028f,.045f,.085f);
            RenderSettings.fogStartDistance = 35; RenderSettings.fogEndDistance = 180;
            var sun = GameObject.Find("Directional Light").GetComponent<Light>();
            sun.color = new Color(.74f,.84f,1f); sun.intensity = 1.1f;
            SetMaterialColor("M_Stone_Platform",new Color(.28f,.34f,.43f));
            SetMaterialColor("M_Dark_Trim",new Color(.085f,.12f,.18f));
            // Several authored emission colors were saved with emission disabled. Keep their colors,
            // URP requires an emissive GI flag to preserve _EMISSION on import.
            // BakedEmissive is metadata; this pass does not bake lighting or enable realtime GI.
            foreach(var material in AssetDatabase.GetDependencies("Assets/Scenes/Voidbound.unity",true)
                .Where(path=>path.StartsWith("Assets/Materials/M_")&&path.EndsWith(".mat"))
                .Select(AssetDatabase.LoadAssetAtPath<Material>).Where(m=>m!=null))
            {
                if(!AssetDatabase.GetAssetPath(material).StartsWith("Assets/Materials/M_") || !material.HasProperty("_EmissionColor"))continue;
                Color emission=material.GetColor("_EmissionColor");
                if(emission.r+emission.g+emission.b<=.001f)continue;
                material.globalIlluminationFlags=MaterialGlobalIlluminationFlags.BakedEmissive;
                material.EnableKeyword("_EMISSION");EditorUtility.SetDirty(material);
            }
            // Existing static light accents are restrained; animated hazard/checkpoint lights remain authored.
            foreach(var light in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
                if(light.name.StartsWith("ChamberLight") || light.name.StartsWith("SanctuaryLight"))
                    light.intensity = 1.35f;

            var dark = Material("M_Void_Structure",new Color(.045f,.07f,.11f),Color.black);
            var cyan = Material("M_Path_Inlay",new Color(.08f,.48f,.58f),Color.black,true);
            var star = Material("M_Void_Stars",new Color(.34f,.5f,.66f),Color.black,true);
            var existing = GameObject.Find("Voidbound_Atmosphere_Details");
            if (existing != null) return;
            var root = new GameObject("Voidbound_Atmosphere_Details").transform;
            root.SetParent(GameObject.Find("Environment").transform,false);
            var random = new System.Random(711);
            var mesh = new Mesh { name = "Voidbound_Starfield" };
            var vertices = new List<Vector3>(); var triangles = new List<int>();
            for(int i=0;i<160;i++)
            {
                Vector3 center = new Vector3((i%2==0?-1:1)*(16f+(float)random.NextDouble()*30f),
                    5f+(float)random.NextDouble()*35f,-20f+(float)random.NextDouble()*440f);
                float radius = .035f+(float)random.NextDouble()*.055f;
                int first = vertices.Count;
                foreach(var point in new[]{Vector3.up,Vector3.down,Vector3.left,Vector3.right,Vector3.forward,Vector3.back})
                    vertices.Add(center+point*radius);
                int[] faces = {0,4,3,0,2,4,0,5,2,0,3,5,1,3,4,1,4,2,1,2,5,1,5,3};
                triangles.AddRange(faces.Select(index=>first+index));
            }
            mesh.SetVertices(vertices); mesh.SetTriangles(triangles,0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh,"Assets/Settings/Voidbound_Starfield.asset");
            var stars = new GameObject("Distant_Starfield",typeof(MeshFilter),typeof(MeshRenderer));
            stars.transform.SetParent(root,false); stars.GetComponent<MeshFilter>().sharedMesh = mesh;
            stars.GetComponent<MeshRenderer>().sharedMaterial = star; DecorationRenderer(stars.GetComponent<MeshRenderer>());
            float[] depths = {3,35,76,125,192,246,332};
            for(int i=0;i<depths.Length;i++)
                foreach(int side in new[]{-1,1})
                {
                    float x = side*(22+i%3*4);
                    Box(root,$"Sector_{i+1}_Distant_Pylon_{side}",new Vector3(x,-7f,depths[i]),new Vector3(2.6f,17f+i%3*3f,3f),dark);
                    Box(root,$"Sector_{i+1}_Pylon_Seam_{side}",new Vector3(x,-4f,depths[i]-1.52f),new Vector3(.08f,8f,.03f),cyan);
                }
            string[] platforms = {"Platform_01_Start","Platform_05_Section3_Departure","Platform_07_Section4_Apron",
                "Platform_13A_LaserChallenge1_Floor","Platform_15_Section6_Entrance","Platform_17A_GauntletEntrance","Platform_20_ExtractionSanctuary"};
            foreach(string name in platforms)
            {
                var platform = GameObject.Find(name); var bounds = platform.GetComponent<Renderer>().bounds;
                foreach(int side in new[]{-1,1})
                {
                    float x = bounds.center.x + side*(bounds.extents.x-.25f);
                    Box(root,name+"_Underslung_Rib_"+side,new Vector3(x,bounds.min.y-.4f,bounds.center.z),new Vector3(.18f,.8f,Mathf.Min(5f,bounds.size.z*.7f)),dark);
                    for(int i=-1;i<=1;i++)
                        Box(root,name+$"_Landing_Inlay_{side}_{i}",new Vector3(x,bounds.max.y+.015f,bounds.center.z+i*.8f),new Vector3(.11f,.018f,.35f),cyan);
                }
            }
        }

        private static Material Material(string name,Color color,Color emission,bool unlit=false)
        {
            string path="Assets/Materials/"+name+".mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null){material=new Material(Shader.Find(unlit?"Universal Render Pipeline/Unlit":"Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,path);}
            material.shader=Shader.Find(unlit?"Universal Render Pipeline/Unlit":"Universal Render Pipeline/Lit");
            material.enableInstancing=true;
            material.SetColor("_BaseColor",color);
            if(unlit)material.DisableKeyword("_EMISSION");
            if(!unlit){material.SetFloat("_Smoothness",.25f);if(emission!=Color.black){material.EnableKeyword("_EMISSION");material.SetColor("_EmissionColor",emission);}}
            EditorUtility.SetDirty(material);return material;
        }

        private static void SetMaterialColor(string name,Color color)
        {
            var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/"+name+".mat");
            material.SetColor("_BaseColor",color);material.SetColor("_Color",color);EditorUtility.SetDirty(material);
        }

        private static void Box(Transform parent,string name,Vector3 position,Vector3 scale,Material material)
        {
            var box=GameObject.CreatePrimitive(PrimitiveType.Cube);box.name=name;box.transform.SetParent(parent,false);
            box.transform.position=position;box.transform.localScale=scale;
            UnityEngine.Object.DestroyImmediate(box.GetComponent<Collider>());
            var renderer=box.GetComponent<Renderer>();renderer.sharedMaterial=material;DecorationRenderer(renderer);
        }

        private static void DecorationRenderer(Renderer renderer)
        {
            renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            renderer.lightProbeUsage=LightProbeUsage.Off;renderer.reflectionProbeUsage=ReflectionProbeUsage.Off;
        }

        private static void ConfigureInterface()
        {
            var canvas=GameObject.Find("Canvas").transform;
            var ui=canvas.GetComponent<UIManager>();
            Font font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            string[] menus={"StartMenuPanel","PauseMenuPanel","GameOverPanel","VictoryPanel"};
            var groups=new List<CanvasGroup>();
            foreach(string name in menus)
            {
                var panel=canvas.Find(name);panel.GetComponent<UnityEngine.UI.Image>().color=new Color(.008f,.018f,.038f,.88f);
                var group=panel.GetComponent<CanvasGroup>();
                if(group==null)group=panel.gameObject.AddComponent<CanvasGroup>();
                groups.Add(group);
                var card=Image(panel,"PresentationCard",Vector2.zero,new Vector2(1040,620),new Color(.018f,.035f,.065f,.97f));card.transform.SetAsFirstSibling();
                var outline=card.GetComponent<UnityEngine.UI.Outline>()??card.gameObject.AddComponent<UnityEngine.UI.Outline>();
                outline.effectColor=new Color(.15f,.48f,.57f,.45f);outline.effectDistance=new Vector2(1,-1);
                Image(panel,"HeaderRule",new Vector2(0,218),new Vector2(120,3),Cyan);
                Label(panel,"SectorEyebrow",name=="StartMenuPanel"?"EXPEDITION CONTROL  /  07 SECTORS":name=="VictoryPanel"?"MISSION COMPLETE":name=="GameOverPanel"?"SIGNAL TERMINATED":"EXPEDITION ON HOLD",font,16,new Vector2(0,248),new Vector2(900,28),new Color(.5f,.7f,.8f));
                Label(panel,"PresentationFooter","VOIDBOUND  //  DEEP VOID EXPEDITION",font,14,new Vector2(0,-265),new Vector2(900,24),new Color(.45f,.6f,.7f));
            }
            var title=canvas.Find("StartMenuPanel/Title").GetComponent<UnityEngine.UI.Text>();title.fontSize=84;title.color=Cyan;title.rectTransform.anchoredPosition=new Vector2(0,145);
            var subtitle=canvas.Find("StartMenuPanel/Subtitle").GetComponent<UnityEngine.UI.Text>();subtitle.text="SEVEN SECTORS  /  ONE EXTRACTION";subtitle.fontSize=22;subtitle.rectTransform.anchoredPosition=new Vector2(0,77);
            var controls=canvas.Find("StartMenuPanel/ControlsInfo").GetComponent<UnityEngine.UI.Text>();controls.fontSize=20;controls.rectTransform.anchoredPosition=new Vector2(0,-165);controls.rectTransform.sizeDelta=new Vector2(860,120);
            canvas.Find("StartMenuPanel/StartButton").GetComponent<RectTransform>().anchoredPosition=new Vector2(0,-35);
            var lost=canvas.Find("GameOverPanel/GameOverTitle").GetComponent<UnityEngine.UI.Text>();lost.text="EXPEDITION LOST";lost.fontSize=56;
            canvas.Find("GameOverPanel/GameOverSubtitle").GetComponent<UnityEngine.UI.Text>().text="All vessels consumed. Begin a new expedition.";
            foreach(var button in canvas.GetComponentsInChildren<UnityEngine.UI.Button>(true))
            {
                var rect=button.GetComponent<RectTransform>();rect.sizeDelta=new Vector2(340,60);
                button.GetComponent<UnityEngine.UI.Image>().color=Color.white;
                var colors=button.colors;bool secondary=button.name=="QuitButton"||button.name=="RestartButton";
                colors.normalColor=secondary?new Color(.14f,.09f,.14f):new Color(.055f,.22f,.29f);
                colors.highlightedColor=secondary?new Color(.29f,.16f,.22f):new Color(.09f,.38f,.46f);
                colors.selectedColor=colors.highlightedColor;colors.pressedColor=new Color(.04f,.17f,.22f);
                colors.disabledColor=new Color(.06f,.09f,.12f,.6f);colors.colorMultiplier=1;colors.fadeDuration=.12f;
                button.colors=colors;button.transition=UnityEngine.UI.Selectable.Transition.ColorTint;
                var border=button.GetComponent<UnityEngine.UI.Outline>()??button.gameObject.AddComponent<UnityEngine.UI.Outline>();border.effectColor=new Color(.2f,.72f,.8f,.6f);border.effectDistance=new Vector2(1,-1);
            }
            foreach(var text in canvas.GetComponentsInChildren<UnityEngine.UI.Text>(true))
            {
                text.raycastTarget=false;
                var shadow=text.GetComponent<UnityEngine.UI.Shadow>()??text.gameObject.AddComponent<UnityEngine.UI.Shadow>();
                shadow.effectColor=new Color(0,0,0,.65f);shadow.effectDistance=new Vector2(1,-1);
            }
            var hud=canvas.Find("HUDPanel");
            var telemetry=Image(hud,"TelemetryBackdrop",new Vector2(24,-18),new Vector2(590,104),new Color(.008f,.02f,.04f,.88f));
            Anchor(telemetry.rectTransform,new Vector2(0,1),new Vector2(0,1));telemetry.transform.SetAsFirstSibling();
            var timer=hud.Find("TimerText").GetComponent<UnityEngine.UI.Text>();timer.fontSize=27;timer.color=Color.white;timer.rectTransform.anchoredPosition=new Vector2(44,-42);
            var lives=hud.Find("LivesText").GetComponent<UnityEngine.UI.Text>();lives.fontSize=27;lives.color=Cyan;lives.rectTransform.anchoredPosition=new Vector2(320,-42);
            var section=hud.Find("SectionText").GetComponent<UnityEngine.UI.Text>();section.fontSize=21;section.color=new Color(.72f,.83f,.9f);section.rectTransform.anchoredPosition=new Vector2(44,-82);section.rectTransform.sizeDelta=new Vector2(610,32);
            var hints=hud.Find("HintsText").GetComponent<UnityEngine.UI.Text>();hints.fontSize=19;hints.color=new Color(.8f,.88f,.94f);hints.rectTransform.sizeDelta=new Vector2(570,64);
            var help=Image(hud,"ControlsBackdrop",new Vector2(-24,-18),new Vector2(580,82),new Color(.008f,.02f,.04f,.72f));Anchor(help.rectTransform,new Vector2(1,1),new Vector2(1,1));help.transform.SetAsFirstSibling();
            var prompt=hud.Find("InteractionPromptText").GetComponent<UnityEngine.UI.Text>();
            Anchor(prompt.rectTransform,new Vector2(.5f,0),new Vector2(.5f,.5f));prompt.rectTransform.anchoredPosition=new Vector2(0,110);prompt.fontSize=24;prompt.rectTransform.sizeDelta=new Vector2(860,54);
            var promptBackground=Image(hud,"InteractionBackdrop",new Vector2(0,110),new Vector2(900,68),new Color(.008f,.02f,.04f,.88f));Anchor(promptBackground.rectTransform,new Vector2(.5f,0),new Vector2(.5f,.5f));promptBackground.transform.SetSiblingIndex(prompt.transform.GetSiblingIndex());
            var notice=Image(hud,"CheckpointNotice",new Vector2(0,-162),new Vector2(780,70),new Color(.015f,.12f,.14f,.94f));Anchor(notice.rectTransform,new Vector2(.5f,1),new Vector2(.5f,.5f));
            var notification=hud.GetComponentsInChildren<UnityEngine.UI.Text>(true).Single(t=>t.name=="NotificationText");notification.transform.SetParent(notice.transform,false);
            notification.rectTransform.anchorMin=Vector2.zero;notification.rectTransform.anchorMax=Vector2.one;notification.rectTransform.offsetMin=notification.rectTransform.offsetMax=Vector2.zero;
            notification.fontSize=26;notification.color=new Color(.55f,1f,.87f);
            var noticeGroup=notice.GetComponent<CanvasGroup>();
            if(noticeGroup==null)noticeGroup=notice.gameObject.AddComponent<CanvasGroup>();
            var wash=Image(canvas,"RespawnWash",Vector2.zero,Vector2.zero,Color.clear);wash.rectTransform.anchorMin=Vector2.zero;wash.rectTransform.anchorMax=Vector2.one;wash.rectTransform.sizeDelta=Vector2.zero;
            var respawn=Label(canvas,"RespawnStatus","",font,22,new Vector2(0,175),new Vector2(1000,45),new Color(1,.65f,.6f,0));Anchor(respawn.rectTransform,new Vector2(.5f,0),new Vector2(.5f,.5f));
            var feedback=canvas.GetComponent<UIVisualFeedback>()??canvas.gameObject.AddComponent<UIVisualFeedback>();
            var serialized=new SerializedObject(feedback);var menuArray=serialized.FindProperty("menuGroups");menuArray.arraySize=groups.Count;
            for(int i=0;i<groups.Count;i++)menuArray.GetArrayElementAtIndex(i).objectReferenceValue=groups[i];
            serialized.FindProperty("feedbackWash").objectReferenceValue=wash;serialized.FindProperty("respawnText").objectReferenceValue=respawn;
            serialized.FindProperty("checkpointNotice").objectReferenceValue=noticeGroup;serialized.FindProperty("notificationText").objectReferenceValue=notification;
            serialized.FindProperty("interactionBackdrop").objectReferenceValue=promptBackground;serialized.FindProperty("interactionText").objectReferenceValue=prompt;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Anchor(RectTransform rect,Vector2 anchor,Vector2 pivot){rect.anchorMin=rect.anchorMax=anchor;rect.pivot=pivot;}
        private static UnityEngine.UI.Image Image(Transform parent,string name,Vector2 position,Vector2 size,Color color)
        {
            var existing=parent.Find(name);var go=existing!=null?existing.gameObject:new GameObject(name,typeof(RectTransform),typeof(UnityEngine.UI.Image));
            go.transform.SetParent(parent,false);var image=go.GetComponent<UnityEngine.UI.Image>();image.color=color;image.raycastTarget=false;
            var rect=image.rectTransform;rect.anchoredPosition=position;rect.sizeDelta=size;return image;
        }
        private static UnityEngine.UI.Text Label(Transform parent,string name,string content,Font font,int size,Vector2 position,Vector2 dimensions,Color color)
        {
            var existing=parent.Find(name);var go=existing!=null?existing.gameObject:new GameObject(name,typeof(RectTransform),typeof(UnityEngine.UI.Text));go.transform.SetParent(parent,false);
            var text=go.GetComponent<UnityEngine.UI.Text>();text.text=content;text.font=font;text.fontSize=size;text.alignment=TextAnchor.MiddleCenter;text.color=color;text.raycastTarget=false;
            text.rectTransform.anchoredPosition=position;text.rectTransform.sizeDelta=dimensions;return text;
        }
    }
}
