$targets = @(
    "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo\HPDynamo.Mcp.Server.Tests",
    "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo\HPDynamo.McpBridge.Tests",
    "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo\HPDynamo.McpBridge",
    "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo\HPDynamo.Mcp.Server"
)

$files = Get-ChildItem -Path $targets -Recurse -Filter "*.cs" | Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' }

$methodViolations = @()

foreach ($file in $files) {
    $lines = Get-Content $file.FullName
    $inMethod = $false
    $methodName = ""
    $startLine = 0
    $braceCount = 0

    for ($i = 0; $i -lt $lines.Count; $i++) {
        $line = $lines[$i]
        $trimmed = $line.Trim()

        if (-not $inMethod) {
            # Check for method definition: access modifier + return type + name(...)
            if ($trimmed -match '^(public|private|protected|internal|static|async|override|virtual|sealed)\s+.*\(.*\)\s*$' -or
                $trimmed -match '^(public|private|protected|internal|static|async|override|virtual|sealed)\s+.*\(.*') {
                $methodName = $trimmed
                $startLine = $i + 1
            }
            if ($methodName -ne "" -and $line.Contains("{")) {
                $inMethod = $true
                $braceCount = 0
            }
        }

        if ($inMethod) {
            $openCount = ($line.ToCharArray() | Where-Object { $_ -eq '{' }).Count
            $closeCount = ($line.ToCharArray() | Where-Object { $_ -eq '}' }).Count
            $braceCount += ($openCount - $closeCount)

            if ($braceCount -le 0) {
                $endLine = $i + 1
                $length = $endLine - $startLine + 1
                if ($length -gt 50) {
                    $methodViolations += [PSCustomObject]@{
                        File = $file.Name
                        Method = $methodName
                        Start = $startLine
                        End = $endLine
                        Length = $length
                    }
                }
                $inMethod = $false
                $methodName = ""
            }
        }
    }
}

Write-Host "Rule M3 (Method length <= 50 lines) Violations: $($methodViolations.Count)"
$methodViolations | Format-Table -AutoSize
