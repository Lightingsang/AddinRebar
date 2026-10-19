$files = @(
    'HPBim/src/HPBim.Client/Pipeline/BimPipelineEngine.ElementRecords.cs',
    'HPBim/src/HPBim.Client/Connection/IntentJournal.cs',
    'HPBim/tests/HPBim.Client.Tests/Connection/IntentJournalAdversarialTests.cs',
    'HPBim/tests/HPBim.Client.Tests/Pipeline/BimPipelineEngineRecordReconciliationTests.cs'
)

foreach ($file in $files) {
    Write-Host "=== $file ==="
    $lines = Get-Content $file
    $inMethod = $false
    $methodName = ''
    $braceCount = 0
    $startLine = 0
    $sawBrace = $false
    for ($i = 0; $i -lt $lines.Count; $i++) {
        $line = $lines[$i]
        if (-not $inMethod) {
            if ($line -match '^\s*(public|private|internal|protected)\s+.*?\s+([A-Za-z0-9_]+)\s*\(' -and $line -notmatch ';$') {
                $methodName = $Matches[2]
                $startLine = $i + 1
                $inMethod = $true
                $braceCount = 0
                $sawBrace = $false
            }
        }
        if ($inMethod) {
            $open = ($line.ToCharArray() | Where-Object { $_ -eq '{' }).Count
            $close = ($line.ToCharArray() | Where-Object { $_ -eq '}' }).Count
            if ($open -gt 0) { $sawBrace = $true }
            $braceCount += ($open - $close)
            if ($sawBrace -and $braceCount -le 0) {
                $len = ($i + 1) - $startLine + 1
                $warn = if ($len -gt 50) { ' [VIOLATION > 50]' } else { '' }
                Write-Host "  $methodName : $len lines (L$startLine-L$($i+1))$warn"
                $inMethod = $false
            }
        }
    }
}
