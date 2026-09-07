using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace AfterSignal.Editor
{
    public static partial class ProjectBuilder
    {
        [MenuItem("AFTERSIGNAL/Quality/Bake geometry batches (preserve colliders)")]
        public static void OptimizeAllScenes()
        {
            Directory.CreateDirectory(resourceRoot + "Geometry");
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64, new[] { GraphicsDeviceType.Direct3D11, GraphicsDeviceType.Direct3D12 });
            PlayerSettings.useFlipModelSwapchain = false;
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/PC_RPAsset.asset");
            pipeline.msaaSampleCount = 2;
            pipeline.shadowCascadeCount = 2;
            pipeline.shadowDistance = 40;
            pipeline.supportsCameraOpaqueTexture = false;
            pipeline.mainLightShadowmapResolution = 1024;
            EditorUtility.SetDirty(pipeline);
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/Settings/PC_Renderer.asset");
            foreach (var feature in renderer.rendererFeatures)
            {
                var serialized = new SerializedObject(feature);
                var down = serialized.FindProperty("m_Settings.Downsample");
                if (down != null)
                {
                    down.boolValue = true;
                    serialized.ApplyModifiedProperties();
                    EditorUtility.SetDirty(feature);
                }
            }

            var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            settings.FindProperty("enableFrameTimingStats").boolValue = true;
            settings.ApplyModifiedProperties();
            var report = new List<string>();
            foreach (var entry in EditorBuildSettings.scenes)
            {
                var scene = EditorSceneManager.OpenScene(entry.path);
                var manifest = Object.FindAnyObjectByType<SceneBatchManifest>();
                if (!manifest)
                    manifest = new GameObject("BAKED / geometry manifest").AddComponent<SceneBatchManifest>();
                if (manifest.sources != null)
                    foreach (var source in manifest.sources)
                        if (source)
                            source.enabled = true;
                if (manifest.generated != null)
                    foreach (var generated in manifest.generated)
                        if (generated)
                            Object.DestroyImmediate(generated);
                var groups = new Dictionary<string, List<MeshFilter>>();
                var roots = new Dictionary<string, Transform>();
                var sourceList = new List<MeshRenderer>();
                foreach (var filter in Object.FindObjectsByType<MeshFilter>())
                {
                    var r = filter.GetComponent<MeshRenderer>();
                    if (!r || !r.enabled || !filter.sharedMesh || !r.sharedMaterial || r.sharedMaterials.Length != 1 || r.sharedMaterial.renderQueue > 2500 || r.shadowCastingMode == ShadowCastingMode.ShadowsOnly)
                        continue;
                    if (filter.GetComponentInParent<CityTrafficSignal>() || filter.GetComponentInParent<UrbanInterior>() || filter.GetComponentInParent<CityVehicle>() || filter.GetComponentInParent<CityBuildingCutaway>() || filter.GetComponentInParent<FoliageOcclusion>() || filter.GetComponentInParent<HingedDoor>() || filter.GetComponentInParent<ResidentWalker>() || filter.GetComponentInParent<PixelActor>() || filter.GetComponentInParent<SpriteRenderer>() || filter.GetComponentInParent<GrappleAnchor>() || filter.GetComponentInParent<MovingLift>() || filter.GetComponentInParent<DistrictGate>() || filter.GetComponentInParent<RouteBeacon>() || filter.GetComponentInParent<BreakableGlass>())
                        continue;
                    var motion = filter.GetComponentInParent<RailMotion>();
                    Transform parent = motion ? motion.transform : manifest.transform;
                    string region = motion ? "motion" + motion.GetInstanceID() : Mathf.FloorToInt(filter.transform.position.x / 18) + "_" + Mathf.FloorToInt(filter.transform.position.z / 20);
                    string key = region + "_" + r.sharedMaterial.GetInstanceID() + "_" + (int)r.shadowCastingMode;
                    if (!groups.ContainsKey(key))
                    {
                        groups[key] = new List<MeshFilter>();
                        roots[key] = parent;
                    }

                    groups[key].Add(filter);
                }

                var built = new List<GameObject>();
                int index = 0;
                foreach (var pair in groups)
                {
                    if (pair.Value.Count < 2)
                        continue;
                    var parent = roots[pair.Key];
                    var first = pair.Value[0].GetComponent<MeshRenderer>();
                    var parts = new CombineInstance[pair.Value.Count];
                    for (int i = 0; i < parts.Length; i++)
                    {
                        parts[i] = new CombineInstance
                        {
                            mesh = pair.Value[i].sharedMesh,
                            transform = parent.worldToLocalMatrix * pair.Value[i].transform.localToWorldMatrix
                        };
                        var r = pair.Value[i].GetComponent<MeshRenderer>();
                        sourceList.Add(r);
                        r.enabled = false;
                    }

                    var mesh = new Mesh
                    {
                        name = scene.name + "_batch_" + index,
                        indexFormat = IndexFormat.UInt32
                    };
                    mesh.CombineMeshes(parts, true, true);
                    mesh.RecalculateBounds();
                    string path = resourceRoot + "Geometry/" + mesh.name + ".asset";
                    var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                    if (existing)
                    {
                        EditorUtility.CopySerialized(mesh, existing);
                        Object.DestroyImmediate(mesh);
                        mesh = existing;
                    }
                    else
                        AssetDatabase.CreateAsset(mesh, path);
                    var go = new GameObject("BATCH / " + first.sharedMaterial.name + " / " + index++, typeof(MeshFilter), typeof(MeshRenderer));
                    go.transform.SetParent(parent, false);
                    go.GetComponent<MeshFilter>().sharedMesh = mesh;
                    var rendererComponent = go.GetComponent<MeshRenderer>();
                    rendererComponent.sharedMaterial = first.sharedMaterial;
                    rendererComponent.shadowCastingMode = first.shadowCastingMode;
                    rendererComponent.receiveShadows = first.receiveShadows;
                    built.Add(go);
                }

                manifest.sources = sourceList.ToArray();
                manifest.generated = built.ToArray();
                manifest.originalRenderers = sourceList.Count;
                manifest.batches = built.Count;
                report.Add(scene.name + ": " + sourceList.Count + " source renderers -> " + built.Count + " geometry batches");
                EditorSceneManager.SaveScene(scene);
            }

            Directory.CreateDirectory("Artifacts/Expansion");
            File.WriteAllLines("Artifacts/Expansion/geometry-batches.txt", report);
            AssetDatabase.SaveAssets();
            GeneratedGeometryMaintenance.RemoveUnused();
        }

        public static void OptimizeAndBuild()
        {
            OptimizeAllScenes();
            BuildWindows();
        }
    }
}
