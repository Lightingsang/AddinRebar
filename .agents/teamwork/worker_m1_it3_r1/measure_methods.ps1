param(
    [string[]]$Paths = @(
        "HPBim/src/HPBim.Core/Validation/BimModelSchemaValidator.cs",
        "HPBim/src/HPBim.Core/Validation/BimModelSchemaValidator.Elements.cs",
        "HPBim/src/HPBim.Client/Pipeline/BimPipelineEngine.PlanExecution.cs",
        "HPBim/src/HPBim.Client/Pipeline/ExecutionPlanBuilder.cs"
    ),
    [int]$MaxMethodLines = 35,
    [int]$MaxFileLines = 300
)

function Measure-FileMethods {
    param([string]$FilePath)

    $rawLines = Get-Content -Path $FilePath
    $totalFileLines = $rawLines.Count

    $methods = [System.Collections.Generic.List[PSObject]]::new()
    $inBlockComment = $false
    $inRawString = $false

    for ($i = 0; $i -lt $rawLines.Count; $i++) {
        $line = $rawLines[$i]

        if ($inBlockComment) {
            if ($line -match '\*/') {
                $inBlockComment = $false
                $line = $line -replace '^.*?\*/', ''
            } else {
                continue
            }
        }
        if ($line -match '/\*' -and -not $inRawString) {
            if ($line -match '/\*.*?\*/') {
                $line = $line -replace '/\*.*?\*/', ''
            } else {
                $inBlockComment = $true
                $line = $line -replace '/\*.*$', ''
            }
        }

        if ($inRawString) {
            if ($line -match '"""') {
                $inRawString = $false
            }
            continue
        }

        $cleanLine = $line -replace '//.*$', ''
        $trimmed = $cleanLine.Trim()

        $isMethod = $false
        $methodName = ""
        $isRawStringTemplate = $false

        if ($trimmed -match '^(public|private|internal|protected)\s+(static\s+|virtual\s+|override\s+|async\s+|abstract\s+|sealed\s+|new\s+)*([\w\<\>\[\]\?,\.\s]+?)\s+([A-Za-z0-9_]+)\s*(\<[\w\s,]+\>)?\s*\(' -or
            $trimmed -match '^(public|private|internal|protected)\s+([A-Za-z0-9_]+)\s*\(' -or
            $trimmed -match '^(async\s+)?(Task|void|ValueTask|bool|int|string|[\w\<\>\[\]\?]+)\s+([A-Za-z0-9_\.]+)\s*\(') {
            
            $methodName = if ($matches[4]) { $matches[4] } elseif ($matches[2]) { $matches[2] } else { $matches[3] }
            
            if ($methodName -notin @('if', 'for', 'foreach', 'while', 'switch', 'using', 'catch', 'lock', 'fixed', 'return', 'get', 'set', 'init') -and
                $trimmed -notmatch '\b(class|struct|interface|record|delegate)\b') {
                $isMethod = $true
            }
        }

        if ($isMethod) {
            $startLine = $i + 1
            $foundBody = $false
            $isExpressionBody = $false
            $braceDepth = 0
            $endLine = -1

            for ($j = $i; $j -lt $rawLines.Count; $j++) {
                $cur = $rawLines[$j]
                $curClean = $cur -replace '//.*$', ''

                if (-not $foundBody) {
                    if ($curClean -match '=>\s*"""') {
                        $isExpressionBody = $true
                        $isRawStringTemplate = $true
                        $foundBody = $true
                        for ($k = $j + 1; $k -lt $rawLines.Count; $k++) {
                            if ($rawLines[$k] -match '"""\s*;\s*$') {
                                $endLine = $k + 1
                                break
                            }
                        }
                        break
                    } elseif ($curClean -match '=>') {
                        $isExpressionBody = $true
                        $foundBody = $true
                    }
                }

                if ($isExpressionBody) {
                    if ($curClean -match ';\s*$') {
                        $endLine = $j + 1
                        break
                    }
                } else {
                    $noStrings = $curClean -replace '"([^"\\]|\\.)*"', '""'
                    foreach ($ch in $noStrings.ToCharArray()) {
                        if ($ch -eq '{') {
                            $braceDepth++
                            $foundBody = $true
                        } elseif ($ch -eq '}') {
                            $braceDepth--
                            if ($foundBody -and $braceDepth -eq 0) {
                                $endLine = $j + 1
                                break
                            }
                        }
                    }
                    if ($endLine -ne -1) { break }
                }
            }

            if ($endLine -ne -1) {
                $lineCount = $endLine - $startLine + 1
                $methods.Add([PSCustomObject]@{
                    File = (Split-Path -Leaf $FilePath)
                    FullPath = $FilePath
                    Method = $methodName
                    StartLine = $startLine
                    EndLine = $endLine
                    LineCount = $lineCount
                    IsRawStringTemplate = $isRawStringTemplate
                    ExceedsLimit = ($lineCount -gt $MaxMethodLines)
                })
                $i = $endLine - 1
            }
        }
    }

    return [PSCustomObject]@{
        File = (Split-Path -Leaf $FilePath)
        FullPath = $FilePath
        FileLineCount = $totalFileLines
        ExceedsC2 = ($totalFileLines -gt $MaxFileLines)
        Methods = $methods
    }
}

$fileResults = @()
$allMethods = @()

foreach ($path in $Paths) {
    if (-not (Test-Path $path)) {
        Write-Warning "File not found: $path"
        continue
    }
    $res = Measure-FileMethods -FilePath $path
    $violCount = ($res.Methods | Where-Object { $_.ExceedsLimit -and -not $_.IsRawStringTemplate }).Count
    $fileResults += [PSCustomObject]@{
        File = $res.File
        Lines = $res.FileLineCount
        C2_Status = if ($res.ExceedsC2) { "FAIL (>300)" } else { "PASS" }
        Methods = $res.Methods.Count
        Violations = $violCount
        Status = if ($res.ExceedsC2 -or $violCount -gt 0) { "FAIL" } else { "PASS" }
    }
    $allMethods += $res.Methods
}

Write-Host "`n==========================================================================" -ForegroundColor Cyan
Write-Host "             HP CLEAN CODE VERIFICATION REPORT (Method <= $MaxMethodLines, File <= $MaxFileLines)      " -ForegroundColor Cyan
Write-Host "==========================================================================" -ForegroundColor Cyan

Write-Host "`n--- FILE COMPLIANCE (C2: <= 300 lines) ---" -ForegroundColor Yellow
$fileResults | Format-Table -AutoSize

Write-Host "`n--- METHOD COMPLIANCE (Limit: <= $MaxMethodLines lines) ---" -ForegroundColor Yellow
$methodReport = $allMethods | Select-Object File, Method, StartLine, EndLine, LineCount, @{Name="Type";Expression={if ($_.IsRawStringTemplate) { "Template" } else { "Method" }}}, @{Name="Status";Expression={if ($_.ExceedsLimit -and -not $_.IsRawStringTemplate) { "FAIL" } elseif ($_.IsRawStringTemplate) { "TEMPLATE" } else { "PASS" }}}
$methodReport | Format-Table -AutoSize

$logicViolations = $allMethods | Where-Object { $_.ExceedsLimit -and -not $_.IsRawStringTemplate }
$c2Violations = $fileResults | Where-Object { $_.C2_Status -ne "PASS" }

Write-Host "==========================================================================" -ForegroundColor Cyan
Write-Host "SUMMARY:"
Write-Host "  Files Analyzed: $($fileResults.Count)"
Write-Host "  Total Methods Analyzed: $($allMethods.Count)"
Write-Host "  C2 File Violations: $($c2Violations.Count)"
Write-Host "  Method Limit Violations (> $MaxMethodLines): $($logicViolations.Count)"

if ($logicViolations.Count -gt 0 -or $c2Violations.Count -gt 0) {
    Write-Host "`nVERDICT: FAIL" -ForegroundColor Red
    exit 1
} else {
    Write-Host "`nVERDICT: PASS - 100% compliant!" -ForegroundColor Green
    exit 0
}
