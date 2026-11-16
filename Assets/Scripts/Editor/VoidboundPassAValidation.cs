using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using Voidbound.Core;
using Voidbound.Interactions;
using Voidbound.Obstacles;
using Voidbound.Player;

namespace Voidbound.Editor
{
    public static class VoidboundPassAValidation
    {
        public const string SessionKey = "Voidbound.PassA.Regression";

        [MenuItem("Voidbound/Capture Pass A Repairs and Repeat Puzzle Resets")]
        public static void Capture()
        {
            SessionState.SetBool(SessionKey + ".Capture", true);
            Run();
        }

        [MenuItem("Voidbound/Run Pass A Play Mode Regressions")]
        public static void Run()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before starting regression tests.");
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Voidbound.unity");
            var roots = scene.GetRootGameObjects();
            var all = roots.SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToArray();
            Require(all.Sum(t => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)) == 0, "No missing scripts");
            Require(UnityEngine.Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None).Length == 1, "One player");
            Require(UnityEngine.Object.FindObjectsByType<GameManager>(FindObjectsSortMode.None).Length == 1, "One manager");
            Require(UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Count(l => l.enabled) == 1, "One listener");
            Require(UnityEngine.Object.FindObjectsByType<SectionTrigger>(FindObjectsSortMode.None).Length == 6, "Six entrances plus tutorial");
            Require(UnityEngine.Object.FindObjectsByType<Checkpoint>(FindObjectsSortMode.None).Length == 2, "Two checkpoints");
            Require(UnityEngine.Object.FindObjectsByType<Hazard>(FindObjectsSortMode.None).Length == 25, "All 25 spike/laser hazards");
            Require(UnityEngine.Object.FindObjectsByType<MovingPlatform>(FindObjectsSortMode.None).Length == 4, "Four moving platforms");
            Require(UnityEngine.Object.FindObjectsByType<CollapsingPlatform>(FindObjectsSortMode.None).Length == 4, "Four collapsing platforms");
            var decorations = all.Single(t => t.name == "Section_02_Decorations");
            Require(decorations.GetComponentsInChildren<LEDNode>(true).Length == 16, "Sixteen restored animated LEDs");
            Require(decorations.GetComponentsInChildren<Collider>(true).Length == 0, "Restored decoration has no gameplay colliders");
            Require(all.SelectMany(t => t.GetComponents<Collider>()).Count() == 246, "All original collision geometry retained");
            Require(all.Where(t => t.name == "Trim").All(t => t.lossyScale.x < 32f && t.lossyScale.z < 40f), "Trim bounds normalized");
            Require(all.Single(t => t.name == "Overhead_Longitudinal_Spine").Find("Trim").lossyScale.z < 32f, "Overhead strip confined to Section 2");
            SessionState.SetBool(SessionKey, true);
            EditorApplication.isPlaying = true;
        }

        public static void Build()
        {
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/Voidbound.unity" },
                locationPathName = "/tmp/VoidBoundPassA-Build/VoidBound.app",
                target = BuildTarget.StandaloneOSX,
                options = BuildOptions.Development
            });
            File.WriteAllText("/tmp/VoidBound-PassA-build-summary.txt",
                $"{report.summary.result} errors={report.summary.totalErrors} warnings={report.summary.totalWarnings} bytes={report.summary.totalSize} duration={report.summary.totalTime}\n" +
                string.Join("\n", report.steps.SelectMany(s => s.messages).Select(m => m.type + ": " + m.content)));
            EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
        }

        private static void Require(bool result, string name)
        {
            if (!result) throw new InvalidOperationException("Pass A scene validation: " + name);
            Debug.Log("[Pass A static PASS] " + name);
        }
    }
}
