#if UNITY_EDITOR || RTXMNESIA_LAMP_TEST
using System;
using System.Collections;
using System.IO;
using System.Linq;
using RTXMnesia.Hpl;
using RTXMnesia.Props;
using RTXMnesia.ScriptApi;
using RTXMnesia.Ui;
using RTXMnesia.World;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace RTXMnesia.Dev
{
    public sealed class HplLampProbe : MonoBehaviour
    {
        IEnumerator Start()
        {
            Application.runInBackground = true;
            var settings = HplUserSettings.ReadSaved();
            settings.textures = 0;
            settings.antialiasing = (int)HplAntialiasing.Off;
            HplGraphics.Apply(settings);
            yield return null;
            var map = FindAnyObjectByType<HplMap>();
            var lamps = map.Entities.OfType<PropLamp>().Where(p => p.Name.StartsWith("torch_static01_")).ToArray();
            foreach (var lamp in lamps) Dump(lamp, "initial");
            var target = lamps.First(p => !p.Lit);
            target.SetLit(true, true);
            for (int i = 0; i < 300; i++) yield return null;
            Dump(target, "ignited");
            target.SetLit(false, false);
            for (int i = 0; i < 10; i++) yield return null;
            Dump(target, "off");
            var camera = map.player.playerCamera;
            string folder = Path.GetFullPath("Logs/LampCheck/PlayerOutput");
            Directory.CreateDirectory(folder);
            map.enabled = false;
            map.player.enabled = false;
            var ceilings = map.Entities.OfType<PropLamp>().Where(p => p.Name.StartsWith("chandelier_simple_short_")).ToArray();
            Vector3 roomCenter = (ceilings[0].WorldCenterPos + ceilings[1].WorldCenterPos) * 0.5f;
            camera.transform.position = roomCenter + new Vector3(6, -1.6f, -7);
            camera.transform.LookAt(roomCenter + Vector3.down * 0.7f);
            settings.lighting = 0;
            settings.dlssMode = 0;
            HplGraphics.Apply(settings);
            foreach (bool legacy in new[] { true, false })
            {
                foreach (var lamp in ceilings)
                    foreach (var emission in lamp.GameObject.GetComponentsInChildren<HplEmission>()) emission.SetAmount(legacy ? 1 : 0);
                yield return Capture(camera, folder, legacy ? "room_lit_material_without_flames" : "room_fixed_classic");
            }
            // Isolate the real emissive torch mesh: no scene light, fog, bloom or player camera movement.
            foreach (var light in FindObjectsByType<Light>(FindObjectsInactive.Include)) light.enabled = false;
            foreach (var volume in FindObjectsByType<Volume>(FindObjectsInactive.Include))
                if (volume != HplGraphics.SettingsVolume) volume.enabled = false;
            foreach (var pass in FindObjectsByType<CustomPassVolume>(FindObjectsInactive.Include)) pass.enabled = false;
            var exposureVolume = new GameObject("Lamp test fixed exposure").AddComponent<Volume>();
            exposureVolume.isGlobal = true;
            exposureVolume.priority = 10000;
            exposureVolume.sharedProfile = ScriptableObject.CreateInstance<VolumeProfile>();
            var exposure = exposureVolume.sharedProfile.Add<Exposure>(true);
            exposure.mode.Override(ExposureMode.Fixed);
            exposure.fixedExposure.Override(0);
            var fog = exposureVolume.sharedProfile.Add<Fog>(true);
            fog.enabled.Override(false);
            var source = target.GameObject.GetComponentsInChildren<MeshRenderer>().First(r => r.sharedMaterial != null && r.sharedMaterial.HasProperty("_EmissiveColor"));
            var litMaterial = lamps.First(p => p.Lit).GameObject.GetComponentsInChildren<MeshRenderer>().First(r => r.sharedMaterial != null && r.sharedMaterial.HasProperty("_EmissiveColor")).sharedMaterial;
            var stage = new GameObject("Emission test mesh");
            stage.AddComponent<MeshFilter>().sharedMesh = source.GetComponent<MeshFilter>().sharedMesh;
            stage.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            stage.transform.localScale = Vector3.one;
            stage.layer = 29;
            var stageRenderer = stage.AddComponent<MeshRenderer>();
            var material = new Material(litMaterial);
            material.SetColor("_BaseColor", Color.black);
            material.SetColor("_SpecularColor", Color.black);
            material.SetFloat("_Metallic", 0);
            material.SetFloat("_CoatMask", 0);
            stageRenderer.sharedMaterial = material;
            camera.cullingMask = 1 << 29;
            var center = stageRenderer.bounds.center;
            camera.transform.position = center + new Vector3(0, 0.5f, 2);
            camera.transform.LookAt(center);
            var rt = new RenderTexture(960, 540, 24, RenderTextureFormat.ARGB32);
            camera.targetTexture = rt;
            foreach (int mode in new[] { 0, 1, 2 })
            {
                settings.lighting = mode == 0 ? 0 : 1;
                settings.rtPathTracing = mode == 2 ? 1 : 0;
                settings.dlssMode = 0;
                settings.bloom = false;
                settings.screenMode = 2;
                HplGraphics.Apply(settings);
                foreach (string state in new[] { "material_off", "indexed_off", "global_off", "on" })
                {
                    stageRenderer.SetPropertyBlock(null);
                    stageRenderer.SetPropertyBlock(null, 0);
                    Color onColor = litMaterial.GetColor("_EmissiveColor");
                    material.SetColor("_EmissiveColor", state == "material_off" ? Color.black : onColor);
                    var block = new MaterialPropertyBlock();
                    block.SetColor("_EmissiveColor", state == "on" ? onColor : Color.black);
                    if (state == "indexed_off") stageRenderer.SetPropertyBlock(block, 0);
                    if (state == "global_off") stageRenderer.SetPropertyBlock(block);
                    if (RenderPipelineManager.currentPipeline is HDRenderPipeline hdrp) hdrp.ResetPathTracing();
                    for (int i = 0; i < 40; i++)
                    {
                        yield return null;
                        camera.Render();
                        AsyncGPUReadback.Request(rt, 0, 0, 1, 0, 1, 0, 1).WaitForCompletion();
                    }
                    var texture = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
                    var previous = RenderTexture.active;
                    RenderTexture.active = rt;
                    texture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                    texture.Apply();
                    RenderTexture.active = previous;
                    double brightness = texture.GetPixels().Sum(c => (double)c.r + c.g + c.b) / (rt.width * rt.height * 3);
                    Debug.Log($"[LampPlayer] mode={mode} state={state} brightness={brightness:F6} RT={HplRayTracing.Active} PT={HplRayTracing.PathTracing}");
                    File.WriteAllBytes(Path.Combine(folder, mode + "_" + state + ".png"), texture.EncodeToPNG());
                    Destroy(texture);
                }
            }
            Application.Quit();
        }

        static IEnumerator Capture(Camera camera, string folder, string name)
        {
            var rt = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
            camera.targetTexture = rt;
            for (int frame = 0; frame < 30; frame++)
            {
                yield return null;
                camera.Render();
                AsyncGPUReadback.Request(rt, 0, 0, 1, 0, 1, 0, 1).WaitForCompletion();
            }
            var texture = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
            var previous = RenderTexture.active;
            RenderTexture.active = rt;
            texture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            texture.Apply();
            RenderTexture.active = previous;
            File.WriteAllBytes(Path.Combine(folder, name + ".png"), texture.EncodeToPNG());
            camera.targetTexture = null;
            Destroy(texture);
            rt.Release();
            Destroy(rt);
        }

        static void Dump(PropLamp lamp, string state)
        {
            Debug.Log($"[LampPlayer] {state} {lamp.Name}: Lit={lamp.Lit} alpha={lamp.EffectsAlpha}");
            var block = new MaterialPropertyBlock();
            foreach (var renderer in lamp.GameObject.GetComponentsInChildren<MeshRenderer>(true))
                for (int i = 0; i < renderer.sharedMaterials.Length; i++)
                {
                    var material = renderer.sharedMaterials[i];
                    if (material == null || !material.HasProperty("_EmissiveColor")) continue;
                    renderer.GetPropertyBlock(block, i);
                    Debug.Log($"[LampPlayer] {renderer.name}[{i}] material={material.name} emission={material.GetColor("_EmissiveColor")} block={block.GetColor("_EmissiveColor")} empty={block.isEmpty} staticBatch={renderer.isPartOfStaticBatch}");
                }
            foreach (var light in lamp.GameObject.GetComponentsInChildren<AmnesiaLight>())
                Debug.Log($"[LampPlayer] light {light.name}: current={light.CurrentColor} flicker={light.FlickerActive}");
        }
    }
}
#endif
