<#
Paired shots of the surface audit suspects (RTXMnesia.EditorDev.SurfaceAudit -> Logs/SurfaceAudit/views.tsv).
For every map of views.tsv runs tools/Run-EnhancedLightingPreview.ps1 with the enhanced lighting, RT and the lantern in
hand: classic textures and enhanced textures from the same spot. Each pair becomes one sheet
Logs/SurfaceAudit/<view>__<map>.png: left classic, right enhanced, brightness x1.3; the probe's own PNGs are moved to
Logs/SurfaceAudit/raw (-Recompose builds the sheets again from them, e.g. with another -Gain, without Unity).
Unity with this project must be closed.

  tools/Run-SurfaceAuditShots.ps1 [-Only s01_foo,s04_bar] [-Gain 1.3] [-Recompose]
#>
param(
    [string]$Only = "",
    [double]$Gain = 1.3,
    [string]$Modes = "enhanced_rt_classictex_lantern,enhanced_rt_tex_lantern",
    [switch]$Recompose
)
Add-Type -AssemblyName System.Drawing
$project = Join-Path $PSScriptRoot "..\UnityProj\UMnesiaRTX" | Resolve-Path
$audit = Join-Path $project "Logs\SurfaceAudit"
$probeOut = Join-Path $project "Logs\EnhancedLighting"
$raw = Join-Path $audit "raw"
New-Item -ItemType Directory -Force $raw | Out-Null
$rows = Get-Content (Join-Path $audit "views.tsv") -Encoding UTF8 | Where-Object { $_ } | ForEach-Object {
    $c = $_ -split "`t"; [pscustomobject]@{ Map = $c[0]; View = $c[1]; Place = $c[2]; Material = $c[3] }
}
if ($Only) { $keep = $Only -split ','; $rows = $rows | Where-Object { $keep -contains $_.View } }
$modeList = $Modes -split ','

function Save-Sheet($row) {
    $stem = [IO.Path]::GetFileNameWithoutExtension($row.Map)
    foreach ($mode in $modeList) {
        $shot = Join-Path $probeOut "${stem}_$($row.View)_$mode.png"
        if (Test-Path $shot) { Move-Item $shot $raw -Force }
    }
    $files = $modeList | ForEach-Object { Join-Path $raw "${stem}_$($row.View)_$_.png" }
    if (($files | Where-Object { -not (Test-Path $_) })) { Write-Warning "no shots for $($row.View)"; return }
    $images = $files | ForEach-Object { [Drawing.Image]::FromFile($_) }
    $w = 1200; $h = [int]($images[0].Height * $w / $images[0].Width); $bar = 34
    $sheet = New-Object Drawing.Bitmap ($w * 2 + 8), ($h + $bar)
    $g = [Drawing.Graphics]::FromImage($sheet)
    $g.Clear([Drawing.Color]::Black)
    $g.InterpolationMode = 'HighQualityBicubic'
    $m = New-Object Drawing.Imaging.ColorMatrix
    $m.Matrix00 = $Gain; $m.Matrix11 = $Gain; $m.Matrix22 = $Gain
    $attr = New-Object Drawing.Imaging.ImageAttributes
    $attr.SetColorMatrix($m)
    $font = New-Object Drawing.Font "Segoe UI", 13
    $labels = @("классика", "улучшенные")
    for ($i = 0; $i -lt 2; $i++) {
        $x = $i * ($w + 8)
        $g.DrawImage($images[$i], (New-Object Drawing.Rectangle $x, $bar, $w, $h), 0, 0, $images[$i].Width, $images[$i].Height, 'Pixel', $attr)
        $rt = if ($modeList[$i] -match "_rt") { "RT" } else { "без RT" }
        $g.DrawString("$($labels[$i]), $rt - $($row.Material)", $font, [Drawing.Brushes]::White, ($x + 8), 6)
    }
    $out = Join-Path $audit "$($row.View)__$stem.png"
    $sheet.Save($out, [Drawing.Imaging.ImageFormat]::Png)
    $g.Dispose(); $sheet.Dispose(); $images | ForEach-Object { $_.Dispose() }
    "sheet $out"
}

if ($Recompose) { foreach ($row in $rows) { Save-Sheet $row }; return }
foreach ($group in ($rows | Group-Object Map)) {
    $views = ($group.Group | ForEach-Object { "$($_.View):$($_.Place)" }) -join ';'
    "== $($group.Name): $($group.Count) views"
    & (Join-Path $PSScriptRoot "Run-EnhancedLightingPreview.ps1") -Map $group.Name -Views $views -Modes $Modes | Select-Object -Last 4
    foreach ($row in $group.Group) { Save-Sheet $row }
}
