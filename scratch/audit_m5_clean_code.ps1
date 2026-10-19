$targets = @(
    "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo\HPDynamo.Mcp.Server.Tests",
    "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo\HPDynamo.McpBridge.Tests",
    "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo\HPDynamo.McpBridge",
    "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo\HPDynamo.Mcp.Server"
)

$files = Get-ChildItem -Path $targets -Recurse -Filter "*.cs" | Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' }

Write-Host "Total files to audit: $($files.Count)"

$emptyCatches = @()
$commentedCode = @()

foreach ($f in $files) {
    $content = Get-Content $f.FullName -Raw
    $lines = Get-Content $f.FullName

    # Rule Q-B1: Empty catches
    # regex for catch block
    $matches = [regex]::Matches($content, 'catch\s*\([^\)]*\)\s*\{([^}]*)\}')
    foreach ($m in $matches) {
        $body = $m.Groups[1].Value.Trim()
        # check if empty or without comment
        if ($body -eq "" -or ($body -notmatch '//' -and $body -notmatch '/\*' -and $body.Trim().Length -eq 0)) {
            $emptyCatches += "$($f.Name): $($m.Value)"
        }
    }

    # Rule Q-B2: Commented-out code
    for ($i = 0; $i -lt $lines.Count; $i++) {
        $line = $lines[$i].Trim()
        if ($line.StartsWith("//")) {
            $cmt = $line.Substring(2).Trim()
            if ($cmt -match '^(var|return|if|for|while|await|using|throw|public|private|internal|protected|class|interface|record)\s+[a-zA-Z0-9_]' -or
                ($cmt -match ';$' -and $cmt -notmatch '^(TODO|NOTE|e\.g\.|Multi-version|Ref|Rule|https?://)')) {
                # exclude regular prose that happens to mention words
                if ($cmt -notmatch '^(See|Example|Sample|This|When|Where|Note|Rule|Multi-version)') {
                    $commentedCode += "$($f.Name):$($i+1): $line"
                }
            }
        }
    }
}

Write-Host ""
Write-Host "Rule Q-B1 (Unannotated empty catch) count: $($emptyCatches.Count)"
$emptyCatches | ForEach-Object { Write-Host "  $_" }

Write-Host ""
Write-Host "Rule Q-B2 (Commented-out code) count: $($commentedCode.Count)"
$commentedCode | ForEach-Object { Write-Host "  $_" }
