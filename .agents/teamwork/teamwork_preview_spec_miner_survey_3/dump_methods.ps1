param (
    [string]$dllPath,
    [string]$outPath
)

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
$sb = New-Object System.Text.StringBuilder
[void]$sb.AppendLine("=== Assembly: $($asm.FullName) ===")

$types = @()
try {
    $types = $asm.GetTypes()
} catch [System.Reflection.ReflectionTypeLoadException] {
    $types = $_.Exception.Types | Where-Object { $_ -ne $null }
}

foreach ($t in $types) {
    try {
        $methods = $t.GetMethods([System.Reflection.BindingFlags]::Public -bor [System.Reflection.BindingFlags]::NonPublic -bor [System.Reflection.BindingFlags]::Static -bor [System.Reflection.BindingFlags]::Instance -bor [System.Reflection.BindingFlags]::DeclaredOnly)
        foreach ($m in $methods) {
            try {
                $pList = @()
                foreach ($p in $m.GetParameters()) {
                    $pList += "$($p.ParameterType.ToString()) $($p.Name) = $($p.DefaultValue)"
                }
                $paramsStr = $pList -join ", "
                $isStatic = if ($m.IsStatic) { "static " } else { "" }
                $attrs = ($m.GetCustomAttributes($false) | ForEach-Object { $_.GetType().Name }) -join ", "
                $attrStr = if ($attrs) { " [$attrs]" } else { "" }
                [void]$sb.AppendLine("$($t.FullName) :: $isStatic$($m.ReturnType.ToString()) $($m.Name)($paramsStr)$attrStr")
            } catch {
                [void]$sb.AppendLine("$($t.FullName) :: $($m.Name) (Error getting details: $($_.Exception.Message))")
            }
        }
    } catch {
        [void]$sb.AppendLine("$($t.FullName) (Error getting methods: $($_.Exception.Message))")
    }
}

[System.IO.File]::WriteAllText($outPath, $sb.ToString(), [System.Text.Encoding]::UTF8)
Write-Host "Exported to $outPath"
