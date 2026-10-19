$resolver = [System.ResolveEventHandler]{
    param($sender, $args)
    $name = ($args.Name -split ',')[0] + ".dll"
    $searchPaths = @(
        "C:\kata_pro\$name",
        "C:\Program Files\Autodesk\AutoCAD 2025\$name",
        "C:\Program Files\Autodesk\AutoCAD 2026\$name",
        "C:\Program Files\Autodesk\Revit 2026\$name"
    )
    foreach ($p in $searchPaths) {
        if ([System.IO.File]::Exists($p)) {
            return [System.Reflection.Assembly]::LoadFrom($p)
        }
    }
    return $null
}

[System.AppDomain]::CurrentDomain.add_AssemblyResolve($resolver)

$asmPath = "C:\kata_pro\Kata_pro64_Cad2025.dll"
$asm = [System.Reflection.Assembly]::LoadFrom($asmPath)
Write-Host "Assembly loaded: $($asm.FullName)"

$types = @()
try {
    $types = $asm.GetTypes()
    Write-Host "Loaded all types cleanly: $($types.Count)"
} catch [System.Reflection.ReflectionTypeLoadException] {
    $types = $_.Exception.Types | Where-Object { $_ -ne $null }
    Write-Host "Loaded partial types: $($types.Count)"
    $_.Exception.LoaderExceptions | ForEach-Object { Write-Warning $_.Message }
}

$found = @()
foreach ($t in $types) {
    try {
        foreach ($m in $t.GetMethods([System.Reflection.BindingFlags]'Public,NonPublic,Static,Instance')) {
            try {
                $attrs = [System.Reflection.CustomAttributeData]::GetCustomAttributes($m)
                foreach ($attr in $attrs) {
                    if ($attr.AttributeType.Name -like "*CommandMethod*") {
                        $cmdName = $attr.ConstructorArguments[0].Value
                        $found += [PSCustomObject]@{
                            Command = $cmdName
                            Type = $t.FullName
                            Method = $m.Name
                        }
                    }
                }
            } catch {}
        }
    } catch {}
}

Write-Host "Found $($found.Count) commands."
$found | Out-File -FilePath "commands_all.txt" -Encoding utf8
$found | Where-Object { $_.Command -match "to_hop|thong_ke|coupler|tem|excel|tkt|tkco|itt" } | Format-Table -AutoSize
