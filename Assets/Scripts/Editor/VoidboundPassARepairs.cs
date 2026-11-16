using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Voidbound.Editor
{
    /// <summary>Applies the integration repairs to the saved course without rebuilding any section.</summary>
    public static class VoidboundPassARepairs
    {
        [MenuItem("Voidbound/Apply Pass A Scene Repairs")]
        public static void Apply()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Voidbound.unity");
            var transforms = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToArray();
            var colliders = transforms.SelectMany(t => t.GetComponents<Collider>()).ToArray();
            int repaired = 0;
            foreach (var trim in transforms.Where(t => t.name == "Trim" && t.parent != null))
            {
                var parent = trim.parent;
                Vector3 pos = trim.localPosition;
                Vector3 scale = trim.localScale;
                switch (parent.name)
                {
                    case "Platform_01_Start":
                        pos = new Vector3(0f, 0.51f, Mathf.Sign(pos.z) * 0.49f);
                        scale = new Vector3(0.98f, 0.02f, 0.02f);
                        break;
                    case "Platform_02_Intermediate":
                        pos = new Vector3(0f, 0.51f, 0.475f);
                        scale = new Vector3(0.95f, 0.02f, 0.05f);
                        break;
                    case "Platform_03_Finish":
                    case "Platform_04_Section2_End":
                        pos = new Vector3(0f, 0.51f, -0.4875f);
                        scale = new Vector3(0.975f, 0.02f, 0.025f);
                        break;
                    case "Overhead_Longitudinal_Spine":
                        pos = new Vector3(0f, -0.52f, 0f);
                        scale = new Vector3(0.67f, 0.05f, 0.99375f);
                        break;
                    case "Crossbeam":
                        // Keep the contributor's front/back offset where one was customized.
                        pos = new Vector3(0f, -0.52f, pos.z);
                        scale = new Vector3(0.95f, 0.05f, 0.2f);
                        break;
                    case "Bridge_Approach":
                    case "Bridge_Exit":
                        pos.x = Mathf.Sign(pos.x) * (0.5f - 0.05f / parent.localScale.x);
                        scale.x = 0.1f / parent.localScale.x;
                        break;
                    case "HammerHead":
                        scale = new Vector3(0.85f, 0.08f, 0.04f);
                        break;
                    default:
                        continue;
                }
                if (trim.localPosition != pos || trim.localScale != scale)
                {
                    trim.localPosition = pos;
                    trim.localScale = scale;
                    repaired++;
                }
            }

            var section2 = transforms.Single(t => t.name == "Section_02_HammerSwing");
            VoidboundSceneBuilder.RestoreSection02Decorations(section2);
            if (section2.Find("Section_02_Decorations").GetComponentsInChildren<Collider>(true).Length != 0)
                throw new InvalidOperationException("Recovered art must not add physical surfaces.");
            if (colliders.Any(c => c == null)) throw new InvalidOperationException("An existing course collider was removed.");
            foreach (string name in new[] { "Platform_01_Start", "Section_02_HammerSwing", "Section_03_MovingPlatform",
                         "Section_04_SpikePath", "Section_05_LaserCorridor", "Section_06_PhysicsChamber", "Section_07_FinalGauntlet" })
                if (!transforms.Any(t => t.name == name)) throw new InvalidOperationException("Missing section: " + name);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[Pass A] Saved all seven sections; repaired {repaired} trims; preserved {colliders.Length} original colliders; recovered Section 2 decorations.");
        }
    }
}
