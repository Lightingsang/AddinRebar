# ==============================================================================
# HPDcePro Milestone M2: One-Way Engineering Asset Migration Script
# ==============================================================================
$ErrorActionPreference = "Stop"

$source = "C:\Dce Pro"
$repoRoot = "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar"
$dest = Join-Path $repoRoot "HPDcePro\assets"

Write-Host "Creating target directories..."
$dirs = @(
    "Cad",
    "AutoStr\00.FileDungChung",
    "Excel\File V.2020",
    "Excel\DceSteel",
    "MayHoc",
    "System\Cache",
    "Language\Cad",
    "Icon",
    "Cuix",
    "SystemSteel"
)
foreach ($d in $dirs) {
    $p = Join-Path $dest $d
    if (-not (Test-Path $p)) {
        New-Item -ItemType Directory -Path $p -Force | Out-Null
    }
}

Write-Host "Copying CAD DWGs..."
robocopy "$source\Cad" "$dest\Cad" *.dwg /R:1 /W:1 /NJH /NJS | Out-Null
robocopy "$source\AutoStr\00.FileDungChung" "$dest\AutoStr\00.FileDungChung" *.dwg *.docx *.json /R:1 /W:1 /NJH /NJS | Out-Null

Write-Host "Copying Excel workbooks..."
robocopy "$source\Excel" "$dest\Excel" "Menu DCE_2020.xlsm" /R:1 /W:1 /NJH /NJS | Out-Null
robocopy "$source\Excel\File V.2020" "$dest\Excel\File V.2020" *.xlsm /R:1 /W:1 /NJH /NJS | Out-Null
robocopy "$source\Excel\DceSteel" "$dest\Excel\DceSteel" *.xlsm /R:1 /W:1 /NJH /NJS | Out-Null

Write-Host "Copying MayHoc..."
robocopy "$source\MayHoc" "$dest\MayHoc" mauhoc.db model.zip /R:1 /W:1 /NJH /NJS | Out-Null

Write-Host "Copying System..."
robocopy "$source\System" "$dest\System" /E /R:1 /W:1 /NJH /NJS | Out-Null

Write-Host "Copying Language..."
robocopy "$source\Language" "$dest\Language" /E /R:1 /W:1 /NJH /NJS | Out-Null

Write-Host "Copying Icons..."
robocopy "$source\Icon" "$dest\Icon" /E /R:1 /W:1 /NJH /NJS | Out-Null

Write-Host "Copying CUIX..."
robocopy "$source" "$dest\Cuix" *.cuix /R:1 /W:1 /NJH /NJS | Out-Null

Write-Host "Copying AutoStr sample..."
robocopy "$source\AutoStr" "$dest\AutoStr" DuAnMau.autostr /R:1 /W:1 /NJH /NJS | Out-Null

Write-Host "Copying SystemSteel..."
robocopy "$source\AddinSteelCad\SystemSteel" "$dest\SystemSteel" /E /R:1 /W:1 /NJH /NJS | Out-Null

Write-Host "Copy complete. Verifying counts..."
$dwgCad = (Get-ChildItem "$dest\Cad\*.dwg").Count
$dwgAutoStr = (Get-ChildItem "$dest\AutoStr\00.FileDungChung\*.dwg").Count
$dwgTotal = (Get-ChildItem "$dest" -Recurse -Filter *.dwg).Count
$xlsmTotal = (Get-ChildItem "$dest\Excel" -Recurse -Filter *.xlsm).Count
$mlTotal = (Get-ChildItem "$dest\MayHoc" -File).Count
$sysTotal = (Get-ChildItem "$dest\System" -Recurse -File).Count
$langTotal = (Get-ChildItem "$dest\Language" -Recurse -File).Count
$iconTotal = (Get-ChildItem "$dest\Icon" -Recurse -File).Count
$cuixTotal = (Get-ChildItem "$dest\Cuix\*.cuix").Count
$steelTotal = (Get-ChildItem "$dest\SystemSteel" -Recurse -File).Count
$autoStrSample = Test-Path "$dest\AutoStr\DuAnMau.autostr"

Write-Host "Verification Results:"
Write-Host "  DWG Cad:         $dwgCad (expected 13)"
Write-Host "  DWG AutoStr:     $dwgAutoStr (expected 4)"
Write-Host "  DWG Total:       $dwgTotal (expected 17)"
Write-Host "  XLSM Total:      $xlsmTotal (expected 132)"
Write-Host "  MayHoc Total:    $mlTotal (expected 2)"
Write-Host "  System Total:    $sysTotal (expected 178)"
Write-Host "  Language Total:  $langTotal (expected 8)"
Write-Host "  Icon Total:      $iconTotal (expected 133)"
Write-Host "  Cuix Total:      $cuixTotal (expected 7)"
Write-Host "  SystemSteel:     $steelTotal (expected 112)"
Write-Host "  DuAnMau.autostr: $autoStrSample (expected True)"

if ($dwgTotal -eq 17 -and $xlsmTotal -eq 132 -and $mlTotal -eq 2 -and $sysTotal -eq 178 -and $langTotal -eq 8 -and $iconTotal -eq 133 -and $cuixTotal -eq 7 -and $autoStrSample) {
    Write-Host "ALL ASSET INVENTORY COUNTS MATCH 100%!" -ForegroundColor Green
} else {
    Write-Error "Asset inventory counts do not match!"
}
