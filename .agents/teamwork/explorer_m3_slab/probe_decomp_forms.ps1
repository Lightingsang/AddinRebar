$cadDll = "C:\kata_pro\Kata_pro64_Cad2025.dll"
$ilspy = "$env:USERPROFILE\.dotnet\tools\ilspycmd.exe"
$outDir = "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m3_slab\decompiled"

if (!(Test-Path $outDir)) {
    New-Item -ItemType Directory -Path $outDir -Force | Out-Null
}

$typesToDump = @(
    "Kata_pro64_Cad2025.Form_Chia_San",
    "Kata_pro64_Cad2025.Form_Phan_Loai_San",
    "Kata_pro64_Cad2025.Form_Rai_Thep_Ke_San",
    "Kata_pro64_Cad2025.TK_san",
    "Kata_pro64_Cad2025.view_san",
    "Kata_pro64_Cad2025.Info_slab",
    "Kata_pro64_Cad2025.Input_slab",
    "Kata_pro64_Cad2025.info_ck_san",
    "Kata_pro64_Cad2025.InfoThepCho",
    "Kata_pro64_Cad2025.PhanLoaiSan",
    "Kata_pro64_Cad2025.slab_Kata"
)

foreach ($t in $typesToDump) {
    $shortName = $t.Split('.')[-1]
    $dest = Join-Path $outDir "$shortName.cs"
    Write-Host "Dumping $t -> $dest"
    & $ilspy -t $t $cadDll > $dest
}
Write-Host "Done dumping forms and types."
