<#
.SYNOPSIS
    Copies the resources needed for the port from the installed Amnesia: The Dark Descent into source/.
.DESCRIPTION
    The game folder is only read. source/ is git-ignored: its content must never be committed.
    Safe to re-run: robocopy only copies new or changed files.
#>
param(
    [string]$GamePath = 'E:\SteamLibrary\steamapps\common\Amnesia The Dark Descent',
    [string]$Destination = (Join-Path $PSScriptRoot '..\source')
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path (Join-Path $GamePath 'resources.cfg'))) {
    throw "Amnesia not found at '$GamePath' (resources.cfg missing)."
}

# Skipped: custom_stories (5.6 GB of user mods), editor, launcher, _CommonRedist, binaries.
$folders = @(
    'maps', 'static_objects', 'entities', 'textures', 'graphics', 'gui', 'fonts',
    'sounds', 'music', 'commentary', 'lang', 'particles', 'lights', 'billboards',
    'flashbacks', 'main_menu', 'models', 'misc', 'config', 'core', 'shaders'
)
$rootFiles = @('resources.cfg', 'materials.cfg', 'ptest_materials.cfg')

New-Item -ItemType Directory -Force $Destination | Out-Null
$Destination = (Resolve-Path $Destination).Path

foreach ($folder in $folders) {
    Write-Host "Copying $folder..."
    # .map_cache is a binary cache the engine regenerates; we read the .map XML instead.
    robocopy (Join-Path $GamePath $folder) (Join-Path $Destination $folder) /E /MT:16 /XF *.map_cache /NFL /NDL /NJH /NJS /NP | Out-Null
    if ($LASTEXITCODE -ge 8) { throw "robocopy failed for '$folder' (exit $LASTEXITCODE)." }
}

foreach ($file in $rootFiles) {
    Copy-Item (Join-Path $GamePath $file) $Destination -Force
}

$stats = Get-ChildItem $Destination -Recurse -File | Measure-Object Length -Sum
Write-Host ("Done: {0} files, {1:N0} MB in {2}" -f $stats.Count, ($stats.Sum / 1MB), $Destination)
