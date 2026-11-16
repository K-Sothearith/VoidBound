using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Voidbound.Editor
{
    public static class VoidboundPassBValidation
    {
        [MenuItem("Voidbound/Capture Pass B Native Visual Validation")]
        public static void Capture() => Begin(false);
        public static void CaptureBefore() => Begin(true);
        public static void CaptureEffects()
        {
            SessionState.SetBool("Voidbound.PassB.Effects",true);
            Begin(false);
        }
        private static void Begin(bool before)
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Voidbound.unity");
            if(!before) VoidboundPassBPolish.VerifySavedProfile();
            SessionState.SetBool("Voidbound.PassB.Capture",true);
            SessionState.SetBool("Voidbound.PassB.Before",before);
            var view=EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));view.Show();view.Focus();
            EditorApplication.isPlaying=true;
        }
        public static void Build()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Voidbound.unity");
            VoidboundPassBPolish.VerifySavedProfile();
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{
                scenes=new[]{"Assets/Scenes/Voidbound.unity"},target=BuildTarget.StandaloneOSX,
                locationPathName="/tmp/VoidBoundPassB-Build/VoidBound.app",options=BuildOptions.Development});
            File.WriteAllText("/tmp/VoidBound-PassB-build-summary.txt",
                $"{report.summary.result} errors={report.summary.totalErrors} warnings={report.summary.totalWarnings} bytes={report.summary.totalSize} duration={report.summary.totalTime}\n"+
                string.Join("\n",report.steps.SelectMany(s=>s.messages).Select(m=>m.type+": "+m.content)));
            EditorApplication.Exit(report.summary.result==BuildResult.Succeeded?0:1);
        }
    }

    // This check runs on the actual scene copy serialized into a player build, not just the editor's open scene.
    public sealed class VoidboundVisualBuildGuard : IProcessSceneWithReport
    {
        public int callbackOrder=>100;
        public void OnProcessScene(UnityEngine.SceneManagement.Scene scene,BuildReport report)
        {
            if(report==null || scene.name!="Voidbound")return;
            var volume=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<UnityEngine.Rendering.Volume>(true)).Single();
            if(volume.sharedProfile==null || volume.sharedProfile.components.Count!=5 || volume.sharedProfile.components.Any(c=>c==null))
                throw new BuildFailedException("Pass B: missing persisted Volume effects in the build scene.");
            var camera=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Camera>(true)).Single();
            if(!camera.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>().renderPostProcessing)
                throw new BuildFailedException("Pass B: build scene camera lost post-processing.");
            Debug.Log("[Pass B build guard] PASS: five persisted Volume effects and camera post-processing in serialized build scene.");
        }
    }
}
