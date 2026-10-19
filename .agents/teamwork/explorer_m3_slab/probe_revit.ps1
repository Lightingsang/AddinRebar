$revitDll = "C:\kata_pro\Kata_Class_Lib_Revit.dll"
$asm = [System.Reflection.Assembly]::LoadFrom($revitDll)
Write-Host "=== Types in Kata_Class_Lib_Revit.dll matching Slab / Floor / San ==="
$matches = $asm.GetTypes() | Where-Object { $_.FullName -match "Slab|Floor|San" }
foreach ($t in $matches) {
    Write-Host $t.FullName
    foreach ($m in $t.GetMethods([System.Reflection.BindingFlags]"Public,Instance,Static,DeclaredOnly")) {
        Write-Host "   METHOD: $($m.Name) ($($m.GetParameters().Length) params)"
    }
}
