$asm = [System.Reflection.Assembly]::LoadFrom("C:\Program Files\Autodesk\Revit 2026\AddIns\DynamoForRevit\ProtoCore.dll")
$t = $asm.GetType("ProtoCore.Mirror.MirrorData")
Write-Host "=== ProtoCore.Mirror.MirrorData ==="
$t.GetMembers() | ForEach-Object { Write-Host "$($_.MemberType): $($_.ToString())" }

$asmDict = [System.Reflection.Assembly]::LoadFrom("C:\Program Files\Autodesk\Revit 2026\AddIns\DynamoForRevit\DesignScriptBuiltin.dll")
$tDict = $asmDict.GetType("DesignScript.Builtin.Dictionary")
Write-Host "`n=== DesignScript.Builtin.Dictionary ==="
$tDict.GetMembers() | ForEach-Object { Write-Host "$($_.MemberType): $($_.ToString())" }
$tDict.GetInterfaces() | ForEach-Object { Write-Host "Interface: $($_.FullName)" }
