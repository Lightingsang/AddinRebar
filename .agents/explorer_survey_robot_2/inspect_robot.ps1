$interopPath = "C:\Program Files\Autodesk\Robot Structural Analysis Professional 2026\Exe\Interop.RobotOM.dll"
Write-Host "Checking: $interopPath"
Write-Host "Exists: $(Test-Path $interopPath)"

if (Test-Path $interopPath) {
    $bytes = [System.IO.File]::ReadAllBytes($interopPath)
    Write-Host "Length: $($bytes.Length) bytes"
    
    # Load assembly
    $asm = [System.Reflection.Assembly]::Load($bytes)
    Write-Host "FullName: $($asm.FullName)"
    Write-Host "ImageRuntimeVersion: $($asm.ImageRuntimeVersion)"
    
    $types = $asm.GetExportedTypes()
    Write-Host "Exported Types Count: $($types.Count)"
    
    # Search for key interfaces and classes
    $keys = @("IRobotApplication", "RobotApplication", "RobotApplicationClass", "IRobotStructure", "IRobotProject", "IRobotNodeServer", "IRobotBarServer", "IRobotObjObjectServer", "IRobotLabelServer", "IRobotCaseServer", "IRobotCalcEngine", "IRobotStructuralAxisServer", "IRobotPreferences")
    
    foreach ($k in $keys) {
        $found = $types | Where-Object { $_.Name -eq $k }
        if ($found) {
            Write-Host "Found: $($found.FullName) (IsInterface: $($found.IsInterface), GUID: $($found.GUID))"
        } else {
            Write-Host "NOT FOUND: $k"
        }
    }
}

# Check Registry for ProgID
Write-Host "`n--- Registry Check for Robot.Application ---"
$regPaths = @(
    "HKLM:\Software\Classes\Robot.Application",
    "HKCU:\Software\Classes\Robot.Application",
    "HKCR:\Robot.Application"
)
foreach ($rp in $regPaths) {
    if (Test-Path $rp) {
        $clsid = (Get-ItemProperty -Path "$rp\CLSID" -ErrorAction SilentlyContinue).'(default)'
        Write-Host "ProgID found at $rp -> CLSID: $clsid"
        if ($clsid) {
            $inproc = (Get-ItemProperty -Path "HKCR:\CLSID\$clsid\LocalServer32" -ErrorAction SilentlyContinue).'(default)'
            Write-Host "LocalServer32: $inproc"
        }
    }
}
