# One-off generator for the bridge's tier fixture: every method of every ETABSv1 interface, classified R / W / D
# by the rules of ADR-02 §1 (path-taking parameters first, then the destructive names, then the read-only
# allow-list, everything else W). Reads the installed ETABSv1.dll through reflection - the same surface the
# semantic analyzer binds against - and, when a CHM index dump is given, snapshots its "c*.X Method" topics so
# a test can prove the fixture covers everything the documentation names. Never runs in the build: the output is
# reviewed by hand and committed as Resources\etabs-oapi-tiers.txt (+ etabs-oapi-index.txt).
#
#   powershell -File tools\generate-oapi-tier-fixture.ps1 [-ChmIndex <index.txt>] [-EtabsDir <dir>]
param(
    [string]$EtabsDir = '',
    [string]$ChmIndex = '',
    [string]$OutDir = (Join-Path $PSScriptRoot '..\HPEtabs.McpBridge\Resources')
)

$ErrorActionPreference = 'Stop'
if (-not $EtabsDir) {
    $key = 'HKLM:\SOFTWARE\Classes\CLSID\{e4f6d00f-51a5-4d65-a09a-ba00fcbf1f82}\LocalServer32'
    $server = (Get-ItemProperty $key -ErrorAction SilentlyContinue).'(default)'
    if ($server) { $EtabsDir = Split-Path ($server.Trim('"')) -Parent }
    if (-not $EtabsDir) { $EtabsDir = Join-Path ${env:ProgramW6432} 'Computers and Structures\ETABS 22' }
}
$dll = Join-Path $EtabsDir 'ETABSv1.dll'
if (-not (Test-Path $dll)) { throw "ETABSv1.dll not found at $dll" }

$assembly = [System.Reflection.Assembly]::LoadFrom($dll)
$fileVersion = (Get-Item $dll).VersionInfo.FileVersion

# ---- rules (ADR-02 §1, in this order) ----------------------------------------------------------------------------
$pathParameterNames = @('FileName', 'csvFilePath', 'SourceFileName', 'FilePath', 'Path', 'fullPath')
$destructivePrefixes = '^(Start|Modify|Merge|Reset|Clear|Rename|Show|Export|Import|Replicate|Delete)'
$destructiveExact = @('SetModelIsLocked', 'RunAnalysis', 'DeleteResults', 'CreateAnalysisModel', 'InitializeNewModel', 'ApplyEditedTables',
    'Save', 'OpenFile', 'NewBlank', 'NewGridOnly', 'NewSteelDeck', 'NewConcreteDeck', 'NewWallModel')
$lifecycle = @('ApplicationExit', 'ApplicationStart', 'Hide', 'Unhide', 'SetAsActiveObject', 'UnsetAsActiveObject', 'InternalExec')
# Interfaces whose members never touch the model file: analysis results and their output setup, the view, the selection.
$readOnlyInterfaces = @('cAnalysisResults', 'cAnalysisResultsSetup', 'cView', 'cSelect')
$readOnlyPrefixes = '^(Get|Is|Has|Verify)'
$readOnlyExact = @('Count', 'RefreshView', 'Visible')

function Classify([string]$interface, [System.Reflection.MethodInfo]$method) {
    $name = $method.Name
    $paths = @()
    $i = 0
    foreach ($p in $method.GetParameters()) {
        # Inputs only: a `ref FileName` is the wrapper reporting where a section came from, not a file the call would touch.
        if (-not $p.ParameterType.IsByRef -and $pathParameterNames -contains $p.Name) { $paths += $i }
        $i++
    }
    if ($interface -eq 'cHelper') { return @('D', $paths) }
    if ($interface -eq 'cOAPI' -and $lifecycle -contains $name) { return @('D', $paths) }
    if ($paths.Count -gt 0) { return @('D', $paths) }
    if ($readOnlyInterfaces -contains $interface) { return @('R', $paths) }
    if ($name -match $destructivePrefixes) { return @('D', $paths) }
    if ($destructiveExact -contains $name) { return @('D', $paths) }
    if ($name -match $readOnlyPrefixes -or $readOnlyExact -contains $name) { return @('R', $paths) }
    return @('W', $paths)
}

$lines = New-Object System.Collections.Generic.List[string]
$lines.Add("# ETABS OAPI tier fixture - generated from ETABSv1.dll $fileVersion by tools\generate-oapi-tier-fixture.ps1, reviewed by hand.")
$lines.Add("# cInterface.Member<TAB>R|W|D[<TAB>path=<parameter indices>]   - R read-only, W writes the model (snapshot first), D destructive (second opt-in).")
$lines.Add("# Rules in order: cHelper.* and the cOAPI lifecycle are D; a by-value path-taking parameter ($($pathParameterNames -join '|')) is D;")
$lines.Add("# $($readOnlyInterfaces -join ', ') are R (results, view, selection never change the model file); $destructivePrefixes is D;")
$lines.Add("# the explicit D names ($($destructiveExact -join ', ')); $readOnlyPrefixes and $($readOnlyExact -join '/') are R; everything else is W.")
$lines.Add("# A member missing here is D (the analyzer fails closed). Navigation properties (sapModel.FrameObj) are not members of this table.")

$counts = @{ R = 0; W = 0; D = 0 }
$interfaces = $assembly.GetTypes() | Where-Object { $_.IsInterface -and $_.Namespace -eq 'ETABSv1' } | Sort-Object Name
foreach ($t in $interfaces) {
    $methods = $t.GetMethods() | Where-Object { -not $_.IsSpecialName } | Sort-Object Name -Unique
    foreach ($m in $methods) {
        $verdict = Classify $t.Name $m
        $tier = $verdict[0]
        $paths = $verdict[1]
        $counts[$tier]++
        $line = "$($t.Name).$($m.Name)`t$tier"
        if ($paths.Count -gt 0) { $line += "`tpath=$($paths -join ',')" }
        $lines.Add($line)
    }
}

New-Item -ItemType Directory -Force $OutDir | Out-Null
$fixture = Join-Path $OutDir 'etabs-oapi-tiers.txt'
[System.IO.File]::WriteAllLines($fixture, $lines, (New-Object System.Text.UTF8Encoding($false)))
"fixture: $fixture - $($counts.R) R, $($counts.W) W, $($counts.D) D over $($interfaces.Count) interfaces"

if ($ChmIndex) {
    $topics = Get-Content $ChmIndex -Encoding UTF8 | ForEach-Object { ($_ -split "`t")[0] } | Where-Object { $_ -match '^c[A-Za-z0-9]+\.[A-Za-z0-9_]+ Method$' } | Sort-Object -Unique
    $index = Join-Path $OutDir 'etabs-oapi-index.txt'
    $header = @("# CSI API ETABS v1.chm index - the 'cInterface.Member Method' topics, snapshot for EtabsTierFixtureTests (documentation names, one per overload group).")
    [System.IO.File]::WriteAllLines($index, ($header + ($topics | ForEach-Object { $_ -replace ' Method$', '' })), (New-Object System.Text.UTF8Encoding($false)))
    "index: $index - $($topics.Count) topics"
}
