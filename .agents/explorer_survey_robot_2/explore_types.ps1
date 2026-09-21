$interopPath = "C:\Program Files\Autodesk\Robot Structural Analysis Professional 2026\Exe\Interop.RobotOM.dll"
$asm = [System.Reflection.Assembly]::Load([System.IO.File]::ReadAllBytes($interopPath))
$types = $asm.GetExportedTypes()

function Search-Types($pattern) {
    Write-Host "`n=== Types matching '$pattern' ==="
    $types | Where-Object { $_.Name -like "*$pattern*" } | Select-Object -First 20 | ForEach-Object {
        Write-Host "$($_.FullName) (IsInterface: $($_.IsInterface), IsEnum: $($_.IsEnum))"
    }
}

Search-Types "Axis"
Search-Types "Grid"
Search-Types "Unit"
Search-Types "Format"
Search-Types "Reaction"
Search-Types "Displacement"
Search-Types "Calc"
Search-Types "Section"
Search-Types "Material"
Search-Types "Support"
Search-Types "Load"
Search-Types "Selection"
Search-Types "BarForce"
Search-Types "ObjObject"
