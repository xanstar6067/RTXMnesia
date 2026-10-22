<#
Renders views of a map in the classic and enhanced lighting (RTXMnesia.EditorDev.EnhancedLightingPreview).
Unity with this project must be closed. ProjectSettings are compared before / after (a batch run must not change them).

  tools/Run-EnhancedLightingPreview.ps1 -Map maps/main/ch01/01_old_archives.map `
      -Views "stove:30 -2 24 34 -1 17;hall:-5 -2 10 -9 0 5|enhanced_rt,enhanced_rt_nofill" `
      -Modes "classic_rt,enhanced_raster,enhanced_rt" [-Lit candlestick01_1,torch_static01_3]

Views: "name:feet_x feet_y feet_z target_x target_y target_z" in HPL2 (.map) coordinates, ";" between views,
"|modes" after a view overrides -Modes for it. Modes and their flags: see docs/EnhancedLighting/README.md.
Output: UnityProj/UMnesiaRTX/Logs/EnhancedLighting/<map>_<view>_<mode>.png and <map>_report.txt.
#>
param(
    [Parameter(Mandatory = $true)][string]$Map,
    [Parameter(Mandatory = $true)][string]$Views,
    [string]$Modes = "classic_rt,enhanced_raster,enhanced_rt",
    [string]$Lit = "",
    [string]$Size = "1600x900",
    [string]$Unity = "C:/Program Files/Unity/Hub/Editor/6000.6.2f1/Editor/Unity.exe"
)
$project = Join-Path $PSScriptRoot "..\UnityProj\UMnesiaRTX" | Resolve-Path
$lock = Join-Path $project "Temp\UnityLockfile"
if (Test-Path $lock) {
    try { [IO.File]::Open($lock, 'Open', 'ReadWrite', 'None').Close() } catch { Write-Error "Unity is open with this project: close it first"; exit 1 }
}
$snapshot = Join-Path $env:TEMP "rtxmnesia_projectsettings"
New-Item -ItemType Directory -Force $snapshot | Out-Null
Copy-Item (Join-Path $project "ProjectSettings\*") $snapshot -Force
$log = Join-Path $project "Logs\EnhancedLighting_run.log"
$arguments = @('-projectPath', "`"$project`"", '-batchmode', '-executeMethod', 'RTXMnesia.EditorDev.EnhancedLightingPreview.RunBatch',
    '-amnesiaMap', $Map, '-lightViews', "`"$Views`"", '-lightModes', $Modes, '-lightSize', $Size, '-logFile', "`"$log`"")
if ($Lit) { $arguments += @('-lightLit', $Lit) }
$process = Start-Process $Unity -ArgumentList $arguments -PassThru -Wait
"Unity exit code $($process.ExitCode); log $log"
Select-String -Path $log -Pattern "error CS|Exception|\[EnhancedLighting\]" | Select-Object -Last 40 | ForEach-Object { $_.Line }
$changed = Get-ChildItem (Join-Path $project "ProjectSettings") -File |
    Where-Object { (Get-FileHash $_.FullName).Hash -ne (Get-FileHash (Join-Path $snapshot $_.Name)).Hash }
if ($changed) { Write-Warning ("ProjectSettings changed by the run: " + ($changed.Name -join ', ') + " (copies in $snapshot)") }
else { "ProjectSettings unchanged" }
