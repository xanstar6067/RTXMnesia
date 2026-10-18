using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

// Run once with -executeMethod ConfigureHDRP.Apply, or from the Tools menu.
public static class ConfigureHDRP
{
    const string AssetPath = "Assets/Settings/HDRP_RenderPipeline.asset";
    const string ProfilePath = "Assets/Settings/HDRP_SampleSceneProfile.asset";

    [MenuItem("Tools/RTXMnesia/Configure HDRP")]
    public static void Apply()
    {
        PlayerSettings.colorSpace = ColorSpace.Linear;
        PlayerSettings.allowHDRDisplaySupport = true;
        PlayerSettings.useHDRDisplay = true; // Required: without it HDROutputSettings reports HDR unavailable. HplHdr then switches to the saved mode.
        var setLightmapQuality = typeof(PlayerSettings).GetMethod("SetLightmapEncodingQualityForPlatform",
            BindingFlags.Static | BindingFlags.NonPublic);
        if (setLightmapQuality != null)
        {
            var qualityType = setLightmapQuality.GetParameters()[1].ParameterType;
            foreach (var target in new[] { BuildTarget.StandaloneWindows, BuildTarget.StandaloneWindows64,
                         BuildTarget.StandaloneLinux64, BuildTarget.StandaloneOSX })
                setLightmapQuality.Invoke(null, new[] { (object)target, Enum.ToObject(qualityType, 2) });
        }
        foreach (var target in new[] { BuildTarget.StandaloneWindows, BuildTarget.StandaloneWindows64 })
        {
            PlayerSettings.SetUseDefaultGraphicsAPIs(target, false);
            PlayerSettings.SetGraphicsAPIs(target, new[] { GraphicsDeviceType.Direct3D12, GraphicsDeviceType.Direct3D11 });
        }

        var pipeline = AssetDatabase.LoadAssetAtPath<HDRenderPipelineAsset>(AssetPath);
        if (pipeline == null)
        {
            pipeline = ScriptableObject.CreateInstance<HDRenderPipelineAsset>();
            AssetDatabase.CreateAsset(pipeline, AssetPath);
        }

        // Use HDRP's own resource initialization, including default volumes and diffusion profiles.
        var globalType = typeof(HDRenderPipelineAsset).Assembly.GetType(
            "UnityEngine.Rendering.HighDefinition.HDRenderPipelineGlobalSettings", true);
        var ensure = globalType.GetMethod("Ensure", BindingFlags.Static | BindingFlags.NonPublic);
        if (ensure == null || ensure.Invoke(null, new object[] { true }) == null)
            throw new InvalidOperationException("HDRP global settings could not be initialized.");

        // Light layers: the hand-held lantern lights the hands through a near-field copy of its lights (PlayerHands).
        var serialized = new SerializedObject(pipeline);
        var lightLayers = serialized.FindProperty("m_RenderPipelineSettings.supportLightLayers");
        if (lightLayers != null) lightLayers.boolValue = true;
        // NVIDIA DLSS (module com.unity.modules.nvidia): dynamic resolution with DLSS as the only upscaler. Only cameras
        // that allow dynamic resolution use it; HplDlss allows it on the game cameras when the RTX tab turns DLSS on.
        const string drs = "m_RenderPipelineSettings.dynamicResolutionSettings.";
        SetBool(serialized, drs + "enabled", true);
        SetBool(serialized, drs + "enableDLSS", true);
        SetBool(serialized, drs + "DLSSUseOptimalSettings", true);
        SetBool(serialized, drs + "useMipBias", true);
        SetFloat(serialized, drs + "DLSSSharpness", 0f);
        var upscalers = serialized.FindProperty(drs + "advancedUpscalerNames");
        if (upscalers != null)
        {
            upscalers.arraySize = 1;
            upscalers.GetArrayElementAtIndex(0).stringValue = "DLSS";
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();

        GraphicsSettings.defaultRenderPipeline = pipeline;
        int initialQuality = QualitySettings.GetQualityLevel();
        for (int i = 0; i < QualitySettings.names.Length; i++)
        {
            QualitySettings.SetQualityLevel(i, false);
            QualitySettings.renderPipeline = pipeline;
            QualitySettings.shadows = ShadowQuality.All;
            QualitySettings.shadowmaskMode = ShadowmaskMode.DistanceShadowmask;
        }
        QualitySettings.SetQualityLevel(initialQuality, false);

        var scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (behaviour != null && behaviour.GetType().Namespace == "UnityEngine.Rendering.Universal")
                    UnityEngine.Object.DestroyImmediate(behaviour);
            }
            foreach (var camera in root.GetComponentsInChildren<Camera>(true))
            {
                var data = camera.GetComponent<HDAdditionalCameraData>() ?? camera.gameObject.AddComponent<HDAdditionalCameraData>();
                data.antialiasing = HDAdditionalCameraData.AntialiasingMode.TemporalAntialiasing;
            }
            foreach (var light in root.GetComponentsInChildren<Light>(true))
            {
                if (light.GetComponent<HDAdditionalLightData>() == null)
                    light.gameObject.AddComponent<HDAdditionalLightData>();
                if (light.type == LightType.Directional)
                {
                    light.intensity = 10000f;
                    light.shadows = LightShadows.Soft;
                }
            }
        }

        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, ProfilePath);
            var environment = profile.Add<VisualEnvironment>(true);
            environment.skyType.Override((int)SkyType.Gradient);
            var sky = profile.Add<GradientSky>(true);
            sky.multiplier.Override(10000f);
            var exposure = profile.Add<Exposure>(true);
            exposure.mode.Override(ExposureMode.Fixed);
            exposure.fixedExposure.Override(10f);
            var tonemapping = profile.Add<Tonemapping>(true);
            tonemapping.mode.Override(TonemappingMode.ACES);
            foreach (var component in profile.components)
                AssetDatabase.AddObjectToAsset(component, profile);
        }
        Volume volume = null;
        foreach (var root in scene.GetRootGameObjects())
            if (root.TryGetComponent<Volume>(out volume)) break;
        if (volume == null) volume = new GameObject("Global Volume").AddComponent<Volume>();
        volume.isGlobal = true;
        volume.sharedProfile = profile;
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        EditorUtility.SetDirty(pipeline);
        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();

        if (GraphicsSettings.defaultRenderPipeline != pipeline || QualitySettings.renderPipeline != pipeline)
            throw new InvalidOperationException("HDRP assignment verification failed.");
        File.WriteAllText("Logs/HDRP-Setup-Result.txt", "HDRP 17.6.0 configured successfully.\nLinear color, DX12/DX11, all quality levels, global resources, SampleScene camera/light/volume.\n");
        Debug.Log("RTXMnesia: HDRP setup completed successfully.");
    }

    static void SetBool(SerializedObject o, string path, bool value)
    {
        var p = o.FindProperty(path);
        if (p != null) p.boolValue = value;
        else Debug.LogWarning("RTXMnesia: HDRP setting not found: " + path);
    }

    static void SetFloat(SerializedObject o, string path, float value)
    {
        var p = o.FindProperty(path);
        if (p != null) p.floatValue = value;
        else Debug.LogWarning("RTXMnesia: HDRP setting not found: " + path);
    }
}
