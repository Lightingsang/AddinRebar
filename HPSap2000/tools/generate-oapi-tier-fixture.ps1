# Generator for SAP2000 tier fixture: every method of every SAP2000v1 interface, classified R / W / D.
param(
    [string]$SapDir = 'C:\Program Files\Computers and Structures\SAP2000 27',
    [string]$OutDir = (Join-Path $PSScriptRoot '..\HPSap2000.McpBridge\Resources')
)

$ErrorActionPreference = 'Stop'
$dll = Join-Path $SapDir 'SAP2000v1.dll'
if (-not (Test-Path $dll)) { throw "SAP2000v1.dll not found at $dll" }

$assembly = [System.Reflection.Assembly]::LoadFrom($dll)
$fileVersion = (Get-Item $dll).VersionInfo.FileVersion

$pathParameterNames = @('FileName', 'csvFilePath', 'SourceFileName', 'FilePath', 'Path', 'fullPath')
$destructivePrefixes = '^(Start|Modify|Merge|Reset|Clear|Rename|Show|Export|Import|Replicate|Delete)'
$destructiveExact = @('SetModelIsLocked', 'RunAnalysis', 'DeleteResults', 'CreateAnalysisModel', 'InitializeNewModel', 'ApplyEditedTables',
    'Save', 'OpenFile', 'NewBlank', 'New2DFrame', 'New3DFrame', 'NewBeam', 'NewSolidBlock', 'NewWall')
$lifecycle = @('ApplicationExit', 'ApplicationStart', 'Hide', 'Unhide', 'SetAsActiveObject', 'UnsetAsActiveObject', 'InternalExec')
$readOnlyInterfaces = @('cAnalysisResults', 'cAnalysisResultsSetup', 'cView', 'cSelect')
$readOnlyPrefixes = '^(Get|Is|Has|Verify)'
$readOnlyExact = @('Count', 'RefreshView', 'Visible')

function Classify([string]$interface, [System.Reflection.MethodInfo]$method) {
    $name = $method.Name
    $paths = @()
    $i = 0
    foreach ($p in $method.GetParameters()) {
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
$lines.Add("# SAP2000 OAPI tier fixture - generated from SAP2000v1.dll $fileVersion by tools\generate-oapi-tier-fixture.ps1")
$lines.Add("# cInterface.Member<TAB>R|W|D[<TAB>path=<parameter indices>]")

$counts = @{ R = 0; W = 0; D = 0 }
$interfaces = $assembly.GetTypes() | Where-Object { $_.IsInterface -and $_.Namespace -eq 'SAP2000v1' } | Sort-Object Name
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
$fixture = Join-Path $OutDir 'sap2000-oapi-tiers.txt'
[System.IO.File]::WriteAllLines($fixture, $lines, (New-Object System.Text.UTF8Encoding($false)))
Write-Output "fixture: $fixture - $($counts.R) R, $($counts.W) W, $($counts.D) D over $($interfaces.Count) interfaces"
