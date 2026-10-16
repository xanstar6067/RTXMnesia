<#
.SYNOPSIS
  Сверка ручного порта скрипта уровня (.hps → C#) с оригиналом.

.DESCRIPTION
  Комментарии в обоих файлах отбрасываются, затем проверяется:
  1. каждая функция, объявленная в .hps, есть в C# с тем же именем;
  2. каждая строковая константа .hps есть в C# (имя функции-коллбэка засчитывается и как nameof(Имя));
  3. число вызовов каждой функции (Имя(...) с заглавной буквы) в .hps и в C# совпадает;
  4. каждое число из .hps встречается в C# (0.5 / .5 / 0.5f считаются одним числом);
  5. каждый коллбэк, заданный в .map (PlayerInteractCallback, CallbackFunc, …), есть в C#.
  Расхождения 1–3 и 5 — ошибки (код выхода 1), 4 — предупреждения. Ожидаемое расхождение не прячут:
  его объясняют в журнале docs/ScriptPort/chXX/<карта>.md, раздел «Отличия от оригинала».

.EXAMPLE
  powershell -ExecutionPolicy Bypass -File tools/Check-ScriptPort.ps1 -Map 03_archives -Port UnityProj/UMnesiaRTX/Assets/Scripts/Levels/Ch01/Archives.cs
#>
param(
    [Parameter(Mandatory = $true)][string]$Map,   # имя карты без расширения, например 03_archives
    [Parameter(Mandatory = $true)][string]$Port,  # путь к C#-файлу порта
    [string]$Chapter = "ch01",
    [ValidateSet("maps/main", "maps/ptest")][string]$MapRoot = "maps/main"
)

$root = Split-Path -Parent $PSScriptRoot
$sourceFolder = if ($MapRoot -eq "maps/ptest") { "source/maps/ptest" } else { "source/maps/main/$Chapter" }
$hpsPath = Join-Path $root "$sourceFolder/$Map.hps"
$csPath = if ([System.IO.Path]::IsPathRooted($Port)) { $Port } else { Join-Path $root $Port }
if (-not (Test-Path $hpsPath)) { Write-Host "Нет файла $hpsPath"; exit 2 }
if (-not (Test-Path $csPath)) { Write-Host "Нет файла $csPath"; exit 2 }

function Strip-Comments([string]$s) {
    # Строки сохраняются (в них бывает //), комментарии // и /* */ удаляются.
    $sb = New-Object System.Text.StringBuilder
    $i = 0; $n = $s.Length
    while ($i -lt $n) {
        $c = $s[$i]
        if ($c -eq '"') {
            $j = $i + 1
            while ($j -lt $n -and $s[$j] -ne '"') { if ($s[$j] -eq '\') { $j++ }; $j++ }
            [void]$sb.Append($s.Substring($i, [Math]::Min($j + 1, $n) - $i)); $i = $j + 1; continue
        }
        if ($c -eq '/' -and $i + 1 -lt $n -and $s[$i + 1] -eq '/') {
            while ($i -lt $n -and $s[$i] -ne "`n") { $i++ }; continue
        }
        if ($c -eq '/' -and $i + 1 -lt $n -and $s[$i + 1] -eq '*') {
            $e = $s.IndexOf('*/', $i + 2); if ($e -lt 0) { $e = $n - 2 }
            [void]$sb.Append(' '); $i = $e + 2; continue
        }
        [void]$sb.Append($c); $i++
    }
    $sb.ToString()
}

$latin1 = [System.Text.Encoding]::GetEncoding(28591)
$hps = Strip-Comments ([System.IO.File]::ReadAllText($hpsPath, $latin1))
$cs = Strip-Comments ([System.IO.File]::ReadAllText($csPath, [System.Text.Encoding]::UTF8))

$errors = 0; $warnings = 0
function Err($msg) { $script:errors++; Write-Host "ОШИБКА: $msg" }
function Warn($msg) { $script:warnings++; Write-Host "предупреждение: $msg" }

# 1. Функции.
$funcs = [regex]::Matches($hps, '(?m)^[ \t]*(?:void|int|float|bool|string|uint)[ \t]+([A-Za-z_]\w*)[ \t]*\(') | ForEach-Object { $_.Groups[1].Value } | Sort-Object -Unique
foreach ($f in $funcs) {
    if ($cs -notmatch "(?m)^[ \t]*(?:public |protected |private |override |static )*[\w\[\]<>]+[ \t]+$f[ \t]*\(") { Err "функция $f не найдена в порте" }
}

# 2. Строки.
$csStrings = @{}
foreach ($m in [regex]::Matches($cs, '"((?:[^"\\]|\\.)*)"')) { $csStrings[$m.Groups[1].Value] = $true }
$hpsStrings = [regex]::Matches($hps, '"((?:[^"\\]|\\.)*)"') | ForEach-Object { $_.Groups[1].Value } | Sort-Object -Unique
foreach ($s in $hpsStrings) {
    if ($csStrings.ContainsKey($s)) { continue }
    if ($s -match '^[A-Za-z_]\w*$' -and $cs -match "nameof\(\s*$s\s*\)") { continue }
    Err "строка ""$s"" не найдена в порте"
}

# 3. Вызовы.
$skip = @('if', 'for', 'while', 'switch', 'return')
function Count-Calls([string]$s) {
    $t = @{}
    foreach ($m in [regex]::Matches($s, '(?<![\w.])([A-Z]\w*)\s*\(')) {
        $name = $m.Groups[1].Value
        if ($skip -contains $name) { continue }
        if ($t.ContainsKey($name)) { $t[$name]++ } else { $t[$name] = 1 }
    }
    $t
}
# Объявления функций — не вызовы.
$hpsCalls = Count-Calls ([regex]::Replace($hps, '(?m)^[ \t]*(?:void|int|float|bool|string|uint)[ \t]+(?=[A-Za-z_]\w*[ \t]*\()', ''))
$csCalls = Count-Calls ([regex]::Replace($cs, '(?m)^[ \t]*(?:public |protected |private |override |static )*[\w\[\]<>]+[ \t]+(?=[A-Za-z_]\w*[ \t]*\()', ''))
foreach ($k in ($hpsCalls.Keys | Sort-Object)) {
    $a = $hpsCalls[$k]; $b = if ($csCalls.ContainsKey($k)) { $csCalls[$k] } else { 0 }
    if ($a -ne $b) { Err "вызовов $k в оригинале $a, в порте $b" }
}

# 4. Числа.
function Norm([string]$v) {
    $v = $v.TrimEnd('f', 'F')
    if ($v.StartsWith('.')) { $v = '0' + $v }
    if ($v.Contains('.')) { $v = $v.TrimEnd('0').TrimEnd('.') }
    if ($v -eq '') { $v = '0' }
    $v
}
function Numbers([string]$s) {
    $noStr = [regex]::Replace($s, '"((?:[^"\\]|\\.)*)"', '""')
    $set = @{}
    foreach ($m in [regex]::Matches($noStr, '(?<![\w.])(\d+\.?\d*[fF]?|\.\d+[fF]?)(?![\w.])')) { $set[(Norm $m.Groups[1].Value)] = $true }
    $set
}
$csNums = Numbers $cs
foreach ($v in ((Numbers $hps).Keys | Sort-Object)) { if (-not $csNums.ContainsKey($v)) { Warn "число $v не найдено в порте" } }

# 5. Коллбэки, заданные в самой карте (.map): скрипт их может нигде не вызывать, но движок вызывает по имени.
$mapPath = Join-Path $root "$sourceFolder/$Map.map"
if (Test-Path $mapPath) {
    $mapText = [System.IO.File]::ReadAllText($mapPath, $latin1)
    $pattern = '<Var Name="(AttachFunction|Callback|CallbackFunc|ConnectionStateChangeCallback|DiaryCallback|PlayerInteractCallback|PlayerLookAtCallback)" Value="([^"]+)"'
    $mapCallbacks = [regex]::Matches($mapText, $pattern) | ForEach-Object { $_.Groups[2].Value } | Sort-Object -Unique
    foreach ($f in $mapCallbacks) {
        if ($f -notmatch '^[A-Za-z_]\w*$') { continue }  # not a function name (some entities keep other data in "Callback")
        $inPort = $cs -match "(?m)^[ \t]*(?:public |protected |private |override |static )*[\w\[\]<>]+[ \t]+$f[ \t]*\("
        if ($inPort) { continue }
        if ($funcs -contains $f) { Err "коллбэк карты $f не найден в порте" }
        else { Warn "коллбэк карты $f нет и в оригинале .hps (ошибка оригинала, в порт не добавлять)" }
    }
}

Write-Host ""
Write-Host "$Map : функций $($funcs.Count), строк $($hpsStrings.Count), ошибок $errors, предупреждений $warnings"
if ($errors -gt 0) { exit 1 } else { exit 0 }
