$jigs = @(
    'VeMBKC_JIG',
    'JigSlabRebar',
    'TagLeader',
    'TagChiaThep',
    'ijgRaithepchamtron',
    'ijgVeDaiVong',
    'JigVeDaiC',
    'VePolylineBatKy',
    'ijgVeThepVachCT',
    'JigVeVachHam',
    'DimNhanhMBKC'
)

foreach ($j in $jigs) {
    $out = & "C:\Users\STR-HP03\.dotnet\tools\ilspycmd.exe" "C:\Dce Pro\DCE Pro.dll" -t $j 2>&1
    if ($out -match ("class\s+" + [regex]::Escape($j))) {
        Write-Host "FOUND: $j"
    } else {
        Write-Host "NOT FOUND: $j"
    }
}
