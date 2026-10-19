$cadDll = "C:\kata_pro\Kata_pro64_Cad2025.dll"
$revitDll = "C:\kata_pro\Kata_Class_Lib_Revit.dll"
$ilspy = "$env:USERPROFILE\.dotnet\tools\ilspycmd.exe"

Write-Host "=== Inspecting Lenh_thep Slab Methods in Cad2025 ==="
$methods = @(
    "Ve_san",
    "Ve_san_polyline",
    "Ve_san_4d",
    "Chia_san1",
    "Trien_khai_day_san",
    "Khai_thong_tin_san",
    "Thong_ke_san",
    "Chuyen_block_thep_san_sang_theplk1",
    "show_thep_rai1",
    "Drop_panel1",
    "Ve_thep_openning1"
)

foreach ($m in $methods) {
    Write-Host "`n--- Method: $m ---"
    # Run ilspycmd to decompile the method from Lenh_thep
    $out = & $ilspy -t "Kata_pro64_Cad2025.Lenh_thep" $cadDll | Select-String -Pattern "void $m\(| $m\(" -Context 1, 30
    if ($out) {
        $out | ForEach-Object { $_.Line }
    } else {
        Write-Host "Method $m not found directly via regex, searching signature..."
    }
}
