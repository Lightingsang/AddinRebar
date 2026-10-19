$forms = @(
    'DamCong_Frm',
    'MongCoc_Frm',
    'MongCoc_FrmLopThepThem',
    'MongCoc_FrmChonThepTuDong',
    'ThangMoi_Frm',
    'CocBT_Frm',
    'TKT_frmMain',
    'AutoStrWindow',
    'Bea_ToiUuCatThepV2',
    'DcePalette',
    'DceRibbon'
)

foreach ($f in $forms) {
    $out = & "C:\Users\STR-HP03\.dotnet\tools\ilspycmd.exe" "C:\Dce Pro\DCE Pro.dll" -t $f 2>&1
    if ($out -match ("class\s+" + [regex]::Escape($f))) {
        Write-Host "FOUND: $f"
    } else {
        Write-Host "NOT FOUND: $f"
    }
}
