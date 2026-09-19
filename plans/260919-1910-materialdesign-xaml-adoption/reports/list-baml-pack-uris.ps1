<#
.SYNOPSIS
    Spike S0-A measurement: what did ILRepack do with the MaterialDesign toolkit inside HPRebar.dll?

.DESCRIPTION
    Reads <Assembly>.g.resources of the merged DLL through System.Resources.ResourceReader and reports
    (1) resource names (BAML, fonts, generic.baml), (2) every ";component" pack-URI string found inside
    the BAML streams, grouped by assembly name, (3) loose toolkit DLLs beside the merged one,
    (4) ThemeInfo attribute, (5) file size. Pure metadata — nothing is executed from the DLL.

.EXAMPLE
    pwsh list-baml-pack-uris.ps1 -Dll "...\HPRebar\HPRebar\bin\Debug.R26\HPRebar.dll"
#>
param(
    [Parameter(Mandatory)] [string] $Dll
)

$ErrorActionPreference = 'Stop'
$dllPath = (Resolve-Path $Dll).Path
$folder = Split-Path $dllPath
$assemblyName = [System.IO.Path]::GetFileNameWithoutExtension($dllPath)

"== $dllPath"
"size: {0:N0} bytes" -f (Get-Item $dllPath).Length

"== loose toolkit DLLs beside it"
$loose = Get-ChildItem $folder -Filter '*.dll' | Where-Object { $_.Name -match 'MaterialDesign|Xaml\.Behaviors' }
if ($loose) { $loose | ForEach-Object { "  LOOSE: $($_.Name)" } } else { "  none (merged)" }

# Metadata-only load: no code runs, dependencies are not resolved.
$bytes = [System.IO.File]::ReadAllBytes($dllPath)
$asm = [System.Reflection.Assembly]::Load($bytes)

"== ThemeInfo attribute"
$themeInfo = $asm.GetCustomAttributesData() | Where-Object { $_.AttributeType.Name -eq 'ThemeInfoAttribute' }
if ($themeInfo) { "  " + ($themeInfo | ForEach-Object { $_.ToString() }) } else { "  MISSING" }

$resNames = @($asm.GetManifestResourceNames() | Where-Object { $_ -like '*.g.resources' })
"== manifest resources: " + ($asm.GetManifestResourceNames() -join ', ')
if (-not $resNames) { "  no .g.resources"; exit 1 }

$uriHits = @{}
foreach ($resName in $resNames) {
    $stream = $asm.GetManifestResourceStream($resName)
    $reader = New-Object System.Resources.ResourceReader -ArgumentList @(,$stream)
    $names = @()
    $enum = $reader.GetEnumerator()
    while ($enum.MoveNext()) {
        $key = [string] $enum.Key
        $names += $key
        if ($key -notlike '*.baml') { continue }
        $value = $enum.Value
        if ($value -is [System.IO.Stream]) {
            $ms = New-Object System.IO.MemoryStream; $value.CopyTo($ms); $data = $ms.ToArray()
        } elseif ($value -is [byte[]]) { $data = $value } else { continue }
        # BAML strings are UTF-8 length-prefixed; ";component" is plain ASCII inside them.
        $text = [System.Text.Encoding]::UTF8.GetString($data)
        foreach ($m in [regex]::Matches($text, '/([A-Za-z0-9_.]+);component/[A-Za-z0-9_./#\-]*')) {
            $asmInUri = $m.Groups[1].Value
            if (-not $uriHits.ContainsKey($asmInUri)) { $uriHits[$asmInUri] = New-Object System.Collections.Generic.List[string] }
            $uriHits[$asmInUri].Add("$resName :: $key -> $($m.Value)")
        }
    }
    $reader.Close()

    "== $resName entries: $($names.Count)"
    "  fonts (.ttf): " + (@($names | Where-Object { $_ -like '*.ttf' }).Count)
    "  generic.baml: " + (($names -contains 'themes/generic.baml'))
    "  toolkit defaults present: " + (@($names | Where-Object { $_ -like 'themes/materialdesign2.defaults.baml' -or $_ -like 'themes/materialdesigntheme.window.baml' }) -join ', ')
    "  own views present: " + (@($names | Where-Object { $_ -like '*columnrebarview.baml' -or $_ -like '*materialspikewindow.baml' }) -join ', ')
}

"== pack URIs inside BAML, by assembly name (the S0-A question)"
foreach ($k in ($uriHits.Keys | Sort-Object)) {
    "  {0,-28} {1,5} hits" -f $k, $uriHits[$k].Count
}
$foreign = $uriHits.Keys | Where-Object { $_ -ne $assemblyName }
if ($foreign) {
    "  !! URIs still pointing at another assembly:"
    foreach ($k in $foreign) { $uriHits[$k] | Select-Object -First 5 | ForEach-Object { "     $_" } }
    "RESULT: FAIL (unpatched pack URIs)"
} else {
    "RESULT: PASS (every ;component URI names $assemblyName)"
}
