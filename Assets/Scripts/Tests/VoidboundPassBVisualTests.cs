#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Voidbound.Core;
using Voidbound.Player;
using Voidbound.Interactions;
using Voidbound.UI;

namespace Voidbound.Tests
{
    /// <summary>Opt-in native Game-view captures including ScreenSpaceOverlay UI. No scene/asset saves.</summary>
    public sealed class VoidboundPassBVisualTests : MonoBehaviour
    {
        private GameManager manager; private PlayerController player; private UIManager ui;
        private StreamWriter log; private string folder; private bool before; private int failed;
        private object sizeGroup; private int originalCustomCount,originalSizeIndex;
        private EditorWindow gameView; private readonly Dictionary<Vector2Int,int> resolutions=new Dictionary<Vector2Int,int>();
        private Mouse mouse;
        private static readonly BindingFlags Flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
        private int previousFrameRate; private bool previousBackground;
        private bool exitQueued;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            if(!SessionState.GetBool("Voidbound.PassB.Capture",false))return;
            SessionState.SetBool("Voidbound.PassB.Capture",false);
            new GameObject("Pass B Visual Validation").AddComponent<VoidboundPassBVisualTests>();
        }
        private void Write(string message){log.WriteLine(message);log.Flush();Debug.Log("[Pass B visual] "+message);}
        private void Check(bool result,string message){if(!result)failed++;Write((result?"PASS ":"FAIL ")+message);}
        private IEnumerator Wait(float seconds){float end=Time.realtimeSinceStartup+seconds;while(Time.realtimeSinceStartup<end)yield return null;}
        private void References(){manager=GameManager.Instance;player=FindFirstObjectByType<PlayerController>();ui=UIManager.Instance;}

        private IEnumerator Start()
        {
            before=SessionState.GetBool("Voidbound.PassB.Before",false);
            bool effectsOnly=SessionState.GetBool("Voidbound.PassB.Effects",false);
            SessionState.SetBool("Voidbound.PassB.Effects",false);
            folder="/tmp/VoidBound-PassB-"+(effectsOnly?"effect-check":before?"before":"after");Directory.CreateDirectory(folder);
            log=new StreamWriter(folder+"/validation.txt");
            Application.logMessageReceived+=HandleException;
            previousFrameRate=Application.targetFrameRate;previousBackground=Application.runInBackground;
            Application.runInBackground=true;Application.targetFrameRate=60;
            DontDestroyOnLoad(gameObject);
            yield return null;yield return null;References();
            Write("Native ScreenCapture from focused Game view; Unity="+Application.unityVersion+" GPU="+SystemInfo.graphicsDeviceName);
            SetResolution(1280,720);yield return Wait(.8f);
            if(effectsOnly)
            {
                yield return CheckRenderedBloom();
                Cleanup();Write("COMPLETE failed="+failed+" frames="+Time.frameCount);
                log.Dispose();log=null;EditorApplication.Exit(failed==0?0:1);yield break;
            }
            yield return Capture("ui-start-1280x720");
            if(!before)
            {
                CheckProfile("initial scene load");
                var group=ui.StartMenuPanel.GetComponent<CanvasGroup>();Check(group!=null&&group.alpha>.99f,"start menu fade reaches full opacity");
                EventSystem.current.SetSelectedGameObject(null);
                mouse=InputSystem.AddDevice<Mouse>();InputSystem.EnableDevice(mouse);
                var button=ui.StartMenuPanel.GetComponentsInChildren<UnityEngine.UI.Button>().First();
                var corners=new Vector3[4];button.GetComponent<RectTransform>().GetWorldCorners(corners);
                InputSystem.QueueStateEvent(mouse,new MouseState{position=(corners[0]+corners[2])*.5f});
                yield return Wait(.2f);yield return Capture("ui-button-hover-1280x720");
                Check(button.colors.highlightedColor!=button.colors.normalColor,"button hover/normal colors differ");
            }
            ui.StartMenuPanel.GetComponentsInChildren<UnityEngine.UI.Button>().First().onClick.Invoke();yield return Wait(.35f);
            float[] z={0,26,65,108,165,234,277};
            for(int i=0;i<z.Length;i++)
            {
                if(i>0)
                {
                    var entrance=FindObjectsByType<SectionTrigger>(FindObjectsSortMode.None).Single(t=>t.SectionName.StartsWith("Section "+(i+1)+":"));
                    player.Teleport(entrance.transform.position,Quaternion.identity);yield return Wait(.1f);
                }
                player.Teleport(new Vector3(0,.1f,z[i]),Quaternion.identity);yield return Wait(.25f);
                yield return Capture("section-"+(i+1));
            }
            if(!before)
            {
                player.Teleport(new Vector3(0,.1f,377),Quaternion.identity);yield return Wait(.2f);yield return Capture("extraction-sanctuary");
                foreach(var collapse in FindObjectsByType<CollapsingPlatform>(FindObjectsSortMode.None).OrderBy(c=>c.name))
                {
                    manager.RestartGame();yield return Wait(.1f);
                    player.Teleport(collapse.InitialPosition+new Vector3(0,.85f,-.8f),Quaternion.identity);
                    InputSystem.QueueDeltaStateEvent(mouse.delta,new Vector2(0,-285));
                    yield return Wait(.3f);
                    Check(collapse.CurrentState==CollapsingPlatformState.Warning,"rendered warning state "+collapse.name);
                    yield return Capture("warning-"+collapse.name);
                }
            }
            foreach(var resolution in before?new[]{new Vector2Int(1280,720)}:new[]{new Vector2Int(1280,720),new Vector2Int(1024,768),new Vector2Int(1920,1080)})
            {
                SetResolution(resolution.x,resolution.y);
                UnityEngine.SceneManagement.SceneManager.LoadScene("Voidbound");yield return Wait(.8f);References();
                if(!before){CheckProfile("reloaded "+resolution);yield return Capture("ui-start-"+resolution.x+"x"+resolution.y);}
                ui.StartMenuPanel.GetComponentsInChildren<UnityEngine.UI.Button>().First().onClick.Invoke();yield return Wait(.35f);
                CheckUIBounds("HUD "+resolution);
                yield return Capture("ui-hud-"+resolution.x+"x"+resolution.y);
                if(!before&&resolution.x==1280)
                {
                    manager.PauseGame();float time=manager.ElapsedTime;var group=ui.PauseMenuPanel.GetComponent<CanvasGroup>();float early=group.alpha;
                    yield return Wait(.35f);Check(group.alpha>.99f&&early<.5f&&manager.ElapsedTime==time,"pause fade advances with paused game time");
                    yield return Capture("ui-pause-1280x720");manager.ResumeGame();
                    var checkpoint=FindObjectsByType<Checkpoint>(FindObjectsSortMode.None).OrderBy(c=>c.transform.position.z).First();
                    player.Teleport(checkpoint.transform.position,Quaternion.identity);yield return Wait(.25f);
                    Check(checkpoint.IsActivated,"checkpoint activation retained");yield return Capture("ui-checkpoint-1280x720");
                    manager.OnPlayerDied();yield return Wait(.08f);yield return Capture("ui-respawn-1280x720");
                    manager.ResetRespawnGuard();manager.OnPlayerDied();manager.ResetRespawnGuard();manager.OnPlayerDied();yield return Wait(.3f);
                    Check(manager.CurrentState==GameState.GameOver,"game over state retained");yield return Capture("ui-gameover-1280x720");
                    Check(ui.transform.Find("RespawnStatus").GetComponent<UnityEngine.UI.Text>().color.a==0,"terminal menu clears transient respawn text");
                    ui.GameOverPanel.GetComponentsInChildren<UnityEngine.UI.Button>().First().onClick.Invoke();yield return Wait(.3f);
                    Check(manager.IsPlaying&&manager.CurrentLives==3,"game over restart retained");
                    Check(!ui.HudPanel.transform.Find("CheckpointNotice").gameObject.activeSelf,"restart clears stale checkpoint presentation");
                }
                var portal=FindFirstObjectByType<ExtractionPortal>();player.Teleport(portal.transform.position,Quaternion.identity);yield return Wait(.35f);
                Check(manager.CurrentState==GameState.Victory,"portal victory "+resolution);
                CheckUIBounds("victory "+resolution);
                yield return Capture("ui-victory-"+resolution.x+"x"+resolution.y);
            }
            Cleanup();Write("COMPLETE failed="+failed+" frames="+Time.frameCount);
            log.Dispose();log=null;EditorApplication.Exit(failed==0?0:1);
        }

        private void CheckProfile(string context)
        {
            var volume=FindFirstObjectByType<Volume>();var camera=Camera.main.GetUniversalAdditionalCameraData();
            Check(volume.sharedProfile!=null&&volume.sharedProfile.components.Count==5&&volume.sharedProfile.components.All(c=>c!=null),"saved Volume persists "+context);
            Check(camera.renderPostProcessing&&((camera.volumeLayerMask.value&(1<<volume.gameObject.layer))!=0),"camera post-processing and layer mask "+context);
            Check(volume.profile.TryGet<Bloom>(out var bloom)&&bloom.intensity.overrideState&&Mathf.Approximately(bloom.intensity.value,.24f),"restrained bloom override "+context);
            Check(volume.profile.TryGet<DepthOfField>(out var depth)&&depth.active==(manager.CurrentState==GameState.StartMenu),"depth of field only on start menu "+context);
        }

        private IEnumerator CheckRenderedBloom()
        {
            manager.StartGame();yield return Wait(.3f);manager.PauseGame();
            var canvas=ui.GetComponent<Canvas>();canvas.enabled=false;
            var profile=FindFirstObjectByType<Volume>().profile;profile.TryGet<Bloom>(out var bloom);
            bool original=bloom.active;
            bloom.active=false;yield return Wait(.2f);yield return Capture("bloom-disabled");
            bloom.active=true;yield return Wait(.2f);yield return Capture("bloom-enabled");
            var a=new Texture2D(2,2);var b=new Texture2D(2,2);
            a.LoadImage(File.ReadAllBytes(folder+"/bloom-disabled.png"));b.LoadImage(File.ReadAllBytes(folder+"/bloom-enabled.png"));
            var pixelsA=a.GetPixels32();var pixelsB=b.GetPixels32();double difference=0;
            for(int i=0;i<pixelsA.Length;i++)difference+=Math.Abs(pixelsA[i].r-pixelsB[i].r)+Math.Abs(pixelsA[i].g-pixelsB[i].g)+Math.Abs(pixelsA[i].b-pixelsB[i].b);
            difference/=pixelsA.Length*3d;
            Check(difference>.01,"frozen scene has rendered bloom contribution; mean RGB byte difference="+difference.ToString("F4"));
            Destroy(a);Destroy(b);bloom.active=original;canvas.enabled=true;manager.ResumeGame();
        }

        private void CheckUIBounds(string context)
        {
            Canvas.ForceUpdateCanvases();
            var canvas=ui.GetComponent<Canvas>();
            foreach(var text in canvas.GetComponentsInChildren<UnityEngine.UI.Text>().Where(t=>t.gameObject.activeInHierarchy&&!string.IsNullOrEmpty(t.text)))
            {
                var corners=new Vector3[4];text.rectTransform.GetWorldCorners(corners);
                bool inside=corners.All(p=>p.x>=-1&&p.y>=-1&&p.x<=Screen.width+1&&p.y<=Screen.height+1);
                Check(inside,"text stays within native viewport "+text.name+" "+context);
                Check(text.preferredHeight<=text.rectTransform.rect.height+2,"text has sufficient height "+text.name+" "+context);
            }
        }

        private IEnumerator Capture(string name)
        {
            gameView.Focus();gameView.Repaint();
            yield return new WaitForEndOfFrame();
            var texture=ScreenCapture.CaptureScreenshotAsTexture();
            Check(texture!=null&&texture.width==Screen.width&&texture.height==Screen.height,"native capture "+name+" "+Screen.width+"x"+Screen.height);
            if(texture!=null){File.WriteAllBytes(folder+"/"+name+".png",texture.EncodeToPNG());Destroy(texture);}
        }

        private void SetResolution(int width,int height)
        {
            var assembly=typeof(EditorWindow).Assembly;var viewType=assembly.GetType("UnityEditor.GameView");
            gameView=EditorWindow.GetWindow(viewType);gameView.Show();gameView.Focus();
            if(sizeGroup==null)
            {
                var sizesType=assembly.GetType("UnityEditor.GameViewSizes");
                var sizes=sizesType.BaseType.GetProperty("instance",BindingFlags.Public|BindingFlags.Static|BindingFlags.FlattenHierarchy).GetValue(null);
                var groupType=assembly.GetType("UnityEditor.GameViewSizeGroupType");
                sizeGroup=sizesType.GetMethod("GetGroup",Flags).Invoke(sizes,new[]{Enum.Parse(groupType,"Standalone")});
                originalCustomCount=(int)sizeGroup.GetType().GetMethod("GetCustomCount",Flags).Invoke(sizeGroup,null);
                originalSizeIndex=(int)viewType.GetProperty("selectedSizeIndex",Flags).GetValue(gameView);
            }
            var key=new Vector2Int(width,height);
            if(!resolutions.TryGetValue(key,out var index))
            {
                var sizeType=assembly.GetType("UnityEditor.GameViewSize");var enumType=assembly.GetType("UnityEditor.GameViewSizeType");
                var size=Activator.CreateInstance(sizeType,Flags,null,new object[]{Enum.Parse(enumType,"FixedResolution"),width,height,"Voidbound QA"},null);
                index=(int)sizeGroup.GetType().GetMethod("GetBuiltinCount",Flags).Invoke(sizeGroup,null)+(int)sizeGroup.GetType().GetMethod("GetCustomCount",Flags).Invoke(sizeGroup,null);
                sizeGroup.GetType().GetMethod("AddCustomSize",Flags).Invoke(sizeGroup,new[]{size});resolutions[key]=index;
            }
            viewType.GetProperty("selectedSizeIndex",Flags).SetValue(gameView,index);gameView.Repaint();
        }

        private void Cleanup()
        {
            if(mouse!=null){InputSystem.RemoveDevice(mouse);mouse=null;}
            Application.targetFrameRate=previousFrameRate;Application.runInBackground=previousBackground;
            if(sizeGroup==null)return;
            int builtins=(int)sizeGroup.GetType().GetMethod("GetBuiltinCount",Flags).Invoke(sizeGroup,null);
            gameView.GetType().GetProperty("selectedSizeIndex",Flags).SetValue(gameView,Mathf.Clamp(originalSizeIndex,0,builtins+originalCustomCount-1));
            int count=(int)sizeGroup.GetType().GetMethod("GetCustomCount",Flags).Invoke(sizeGroup,null);
            while(count>originalCustomCount){sizeGroup.GetType().GetMethod("RemoveCustomSize",Flags).Invoke(sizeGroup,new object[]{builtins+count-1});count--;}
            sizeGroup=null;
        }
        private void HandleException(string message,string stack,LogType type)
        {
            if(exitQueued || (type!=LogType.Exception && type!=LogType.Error) || !stack.Contains("Voidbound."))return;
            exitQueued=true;failed++;log?.WriteLine("FAIL exception: "+message);log?.Flush();
            EditorApplication.delayCall+=()=>{Cleanup();log?.Dispose();log=null;EditorApplication.Exit(1);};
        }
        private void OnDestroy(){Application.logMessageReceived-=HandleException;if(log!=null){Cleanup();log.Dispose();log=null;}}
    }
}
#endif
