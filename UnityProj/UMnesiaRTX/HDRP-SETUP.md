# HDRP setup

Open this folder with Unity **6000.6.2f1**.

- HDRP **17.6.0**, matching the editor's bundled rendering packages.
- Default pipeline: `Assets/Settings/HDRP_RenderPipeline.asset`.
- Both desktop quality levels use HDRP; Desktop High is the default for Windows.
- Linear color space, high-quality lightmap encoding, soft shadows and distance shadowmask.
- Windows graphics APIs: Direct3D 12, then Direct3D 11 as fallback.
- HDRP global resources, default volume and diffusion profiles are initialized by Unity.
- `SampleScene` has an HDRP camera with TAA, a directional light and a global volume
  with gradient sky, fixed exposure and ACES tonemapping.

This is a desktop HDRP baseline. Hardware ray tracing is not enabled by this setup.
The former URP template package and its unused settings assets have been removed.

To apply the baseline again, use **Tools > RTXMnesia > Configure HDRP**.
This also updates and saves `SampleScene`; save any scene edits before using it.

Batch setup:

```powershell
& 'C:/Program Files/Unity/Hub/Editor/6000.6.2f1/Editor/Unity.exe' `
  -batchmode -nographics -quit -projectPath 'Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX' `
  -executeMethod ConfigureHDRP.Apply -logFile 'Z:/Unity/RTXMnesia/UnityProj/UMnesiaRTX/Logs/HDRP-Setup.log'
```

Setup completion is recorded in `Logs/HDRP-Setup-Result.txt`.
