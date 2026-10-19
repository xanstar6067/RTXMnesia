param(
    [string]$PythonPath = "$env:USERPROFILE\AppData\Local\Comfy-Desktop\ComfyUI-Installs\ComfyUI\ComfyUI\.venv\Scripts\python.exe",
    [ValidateSet('opaque', 'alpha-test')][string]$Kind = 'opaque',
    [switch]$Status,
    [switch]$Stop,
    [switch]$Resume,
    [switch]$RetryFailed
)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$batchName = if ($Kind -eq 'alpha-test') { 'pbrify-alpha-v1' } else { 'pbrify-v1' }
$batchDirectory = Join-Path $projectRoot "sourceRTXRemix\_batch\$batchName"
$manifestFile = Join-Path $batchDirectory 'manifest.json'
$stopFile = [IO.Path]::ChangeExtension($manifestFile, '.stop')
$workerScript = Join-Path $PSScriptRoot 'Remix-Textures.py'
if ($Status) {
    $batchState = Get-Content -LiteralPath $manifestFile -Raw | ConvertFrom-Json
    $counts = @{}
    $batchState.entries | Group-Object state | ForEach-Object { $counts[$_.Name] = $_.Count }
    @{total=$batchState.entries.Count; states=$counts; worker=$batchState.worker; updated=$batchState.updated;
      active=@($batchState.entries | Where-Object { $_.state -in 'running','failed' } |
        Select-Object texture,prompt_id,error)} | ConvertTo-Json -Depth 8
    exit 0
}
if ($Stop) {
    New-Item -ItemType File -Path $stopFile -Force | Out-Null
    Write-Output 'Stop requested. The active texture will finish; no next texture will be submitted.'
    exit 0
}
if (-not (Test-Path -LiteralPath $PythonPath)) { throw 'Specify the ComfyUI Python interpreter with -PythonPath.' }
if (-not (Test-Path -LiteralPath $manifestFile)) {
    & $PythonPath $workerScript plan --kind $Kind
    if ($LASTEXITCODE -ne 0) { throw 'Planning failed.' }
}
if ($Resume -and (Test-Path -LiteralPath $stopFile)) { Remove-Item -LiteralPath $stopFile }
if (Test-Path -LiteralPath $stopFile) { throw 'A stop marker exists. Use -Resume to continue.' }
# The Python worker also holds an OS file lock, so a second launcher cannot process this manifest.
$runTag = Get-Date -Format 'yyyyMMdd-HHmmss-fff'
$stdoutPath = Join-Path $batchDirectory "worker-$runTag.log"
$stderrPath = Join-Path $batchDirectory "worker-$runTag.errors.log"
$workerArguments = @('-u', ('"' + $workerScript + '"'), 'run', '--kind', $Kind)
if ($RetryFailed) { $workerArguments += '--retry-failed' }
$workerProcess = Start-Process -FilePath $PythonPath -ArgumentList $workerArguments `
    -WorkingDirectory $projectRoot -WindowStyle Hidden -PassThru `
    -RedirectStandardOutput $stdoutPath -RedirectStandardError $stderrPath
@{pid=$workerProcess.Id; stdout=$stdoutPath; stderr=$stderrPath; started=(Get-Date).ToString('o')} |
    ConvertTo-Json | Set-Content -LiteralPath (Join-Path $batchDirectory 'launcher.json') -Encoding utf8
Write-Output "Worker PID: $($workerProcess.Id)"
Write-Output "Log: $stdoutPath"
Write-Output "Use tools/Start-RemixTextures.ps1 -Kind $Kind -Status to inspect, -Stop to stop after the active texture."
