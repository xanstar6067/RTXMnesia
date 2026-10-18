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

This is a desktop HDRP baseline. Hardware ray tracing (DXR) is supported by the asset: `supportRayTracing`, screen space
shadows with 16 slots (ray-traced shadows of punctual lights use them), SSR (only for ray-traced reflections). Cameras only trace rays with the "Ray tracing"
lighting of the graphics options (`HplRayTracing`); check: `RTXMnesia.EditorDev.RtPreview.RunBatch`, results in `Logs/RtCheck`.

NVIDIA DLSS (built-in module `com.unity.modules.nvidia`): the setup enables hardware dynamic resolution with DLSS as
the only upscaler (optimal settings, sharpness 0, mip bias). A camera only uses it while it allows dynamic resolution;
`HplDlss` turns that on for the game cameras when the RTX options tab selects a DLSS mode. Check on an RTX GPU:
`RTXMnesia.EditorDev.DlssPreview.RunBatch` (no `-quit` / `-nographics`), results in `Logs/DlssCheck`.
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

## Native HDR output (in progress)

`HplHdr` selects native HDR output at runtime and exposes calibration in the HDR options tab.
Player Settings > "Use HDR Display Output" (`useHDRDisplay`) must stay **on**: with it off Unity reports
`HDROutputSettings.available = false` and the display getters throw `InvalidOperationException` ("HDR is not enabled
in the player settings") on an HDR-capable system (checked with a standalone D3D12 player; with it on HDR activates
within ~2 s and HplHdr then applies the saved mode). `Editor/EnsureHdrDisplay.cs` keeps the setting on.
SDR-only players must not query `HDROutputSettings.HDRModeChangeRequested` or request a mode change:
Unity throws even when `HDROutputSettings.main` is non-null. Check display availability first;
without it the HDR volume stays inactive and startup continues in SDR. Runtime mode changes
also require `HDRDisplaySupportFlags.RuntimeSwitchable`.

Startup regression: `RTXMnesia.EditorDev.HdrStartupTests.BuildPlayerBatch` builds the real title
scene as a release player in `Logs/HdrStartup/Player`. Run `HdrStartup.exe -batchmode -nographics`
to check title UI creation, the playing menu music source and HDR options on an unsupported display.
The probe is included only with `RTXMNESIA_STARTUP_TEST`, writes `StartupResult.txt` and exits.
This does not verify HDR luminance, physical display output or the unfinished shader readback test.

## Screen values and gamma 2.2

HPL2 multiplies albedo, light, fog and effects as raw screen values written to a gamma ~2.2 monitor. The piecewise sRGB curve (SDR swap chain encode, `Color.linear`, HDR decode) is far brighter/darker than a power curve in the darks. `HplScreenEffects.shader` therefore works in a pure power 2.2 space; in SDR the last pass (always on) hands the swap chain `SRGBToLinear(value^(1/gamma))` so it shows exactly the value; in HDR the output is the linear buffer times paper white. `HplHdr.DisplayGamma` is the "Shadow depth" setting of the HDR tab (`hdrShadows`, 2.0-3.2, default 2.4 = BT.1886; 2.2 skips the pass): LCD panels without local dimming lift the HDR black to 0.3-0.5 nit, a higher exponent sinks the darks under it. Check: `RTXMnesia.EditorDev.HdrTests.RunRenderBatch` (GPU readback, no `-nographics`, Unity must not have the project open).
