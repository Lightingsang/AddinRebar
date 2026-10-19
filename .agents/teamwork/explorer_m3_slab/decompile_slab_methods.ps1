Add-Type -Path "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\scripts\survey_kata\bin\Debug\net10.0\ICSharpCode.Decompiler.dll"

$cad2013 = "C:\kata_pro\Kata_pro64_Cad2013.dll"
$cad2025 = "C:\kata_pro\Kata_pro64_Cad2025.dll"
$outDir = "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m3_slab\decompiled"

$settings = New-Object ICSharpCode.Decompiler.DecompilerSettings
$settings.ThrowOnAssemblyResolveErrors = $false

$decompiler = New-Object ICSharpCode.Decompiler.CSharp.CSharpDecompiler($cad2013, $settings)
$type = $decompiler.TypeSystem.FindType((New-Object ICSharpCode.Decompiler.TypeSystem.FullTypeName("Kata_pro64_Cad2013.Lenh_thep")))

$slabMethods = @(
    "Ve_san",
    "Ve_san_polyline",
    "Ve_san_4d",
    "Chia_san1",
    "Trien_khai_day_san",
    "Khai_thong_tin_san",
    "Thong_ke_san",
    "Phan_loai_san1",
    "Chuyen_block_thep_san_sang_theplk1",
    "show_thep_rai1",
    "Drop_panel1",
    "Ve_thep_openning1"
)

foreach ($mName in $slabMethods) {
    $found = $false
    foreach ($m in $type.GetMethods()) {
        if ($m.Name -eq $mName) {
            $found = $true
            $code = $decompiler.DecompileAsString($m.MetadataToken)
            $dest = Join-Path $outDir "decomp_Lenh_thep_$mName.cs"
            [System.IO.File]::WriteAllText($dest, $code, [System.Text.Encoding]::UTF8)
            Write-Host "Decompiled $mName -> $dest"
        }
    }
    if (-not $found) {
        Write-Host "Method $mName not found in Kata_pro64_Cad2013.Lenh_thep"
    }
}

Write-Host "Done decompiling Lenh_thep slab methods."
