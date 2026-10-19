param (
    [string]$dllPath
)

try {
    $bytes = [System.IO.File]::ReadAllBytes($dllPath)
    $dir = [System.IO.Path]::GetDirectoryName($dllPath)
    [System.AppDomain]::CurrentDomain.add_AssemblyResolve({
        param($sender, $args)
        $name = $args.Name.Split(',')[0]
        $candidate = [System.IO.Path]::Combine($dir, "$name.dll")
        if ([System.IO.File]::Exists($candidate)) {
            return [System.Reflection.Assembly]::LoadFrom($candidate)
        }
        return $null
    })

    $asm = [System.Reflection.Assembly]::Load($bytes)
    Write-Host "=== Assembly: $($asm.FullName) ==="
    $types = @()
    try {
        $types = $asm.GetTypes()
    } catch [System.Reflection.ReflectionTypeLoadException] {
        $types = $_.Exception.Types | Where-Object { $_ -ne $null }
        Write-Host "LoaderExceptions count: $($_.Exception.LoaderExceptions.Count)"
        foreach ($le in ($_.Exception.LoaderExceptions | Select-Object -Unique)) {
            Write-Host "  LE: $($le.Message)"
        }
    }

    Write-Host "Types found: $($types.Count)"
    foreach ($t in $types) {
        $methods = $t.GetMethods([System.Reflection.BindingFlags]::Public -bor [System.Reflection.BindingFlags]::Static -bor [System.Reflection.BindingFlags]::Instance -bor [System.Reflection.BindingFlags]::DeclaredOnly)
        if ($methods.Count -gt 0) {
            Write-Host "Type: $($t.FullName)"
            foreach ($m in $methods) {
                $params = ($m.GetParameters() | ForEach-Object { "$($_.ParameterType.Name) $($_.Name)" }) -join ", "
                Write-Host "  $($m.ReturnType.Name) $($m.Name)($params)"
            }
        }
    }
} catch {
    Write-Host "Outer Error: $($_.Exception.ToString())"
}
