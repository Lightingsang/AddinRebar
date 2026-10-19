# Deep Adversarial Test Suite for Remove-BenchmarkSandbox
$ErrorActionPreference = 'Stop'

# Extract Remove-BenchmarkSandbox from production run-benchmark.ps1
$prodScriptPath = "HPDynamo\tools\harness\run-benchmark.ps1"
$tokens = $null
$errors = $null
$ast = [System.Management.Automation.Language.Parser]::ParseFile($prodScriptPath, [ref]$tokens, [ref]$errors)
$fn = $ast.Find({ param($n) $n -is [System.Management.Automation.Language.FunctionDefinitionAst] -and $n.Name -eq 'Remove-BenchmarkSandbox' }, $true)
if (-not $fn) {
    throw "Could not extract function Remove-BenchmarkSandbox from $prodScriptPath"
}
Invoke-Expression $fn.Extent.Text

Write-Host "=== Running Deep Adversarial Test Suite on Remove-BenchmarkSandbox ===" -ForegroundColor Cyan

$testResults = @()
$sandboxRoot = [System.IO.Path]::GetFullPath((Join-Path $env:TEMP "HPDynamoBenchmark"))
if (-not (Test-Path $sandboxRoot)) {
    New-Item -ItemType Directory -Path $sandboxRoot -Force | Out-Null
}

function Test-Attack {
    param(
        [string]$Name,
        [string]$Path,
        [bool]$MustRefuse,
        [scriptblock]$SetupBlock = $null,
        [scriptblock]$CheckBlock = $null
    )

    $setupData = $null
    if ($SetupBlock) { $setupData = & $SetupBlock }

    $refused = $false
    $errorMsg = ""
    try {
        $errOut = $null
        Remove-BenchmarkSandbox -SandboxDir $Path 2>&1 | Tee-Object -Variable errOut | Out-Null
        if ($errOut -and ($errOut.ToString() -match "SECURITY REFUSAL")) {
            $refused = $true
            $errorMsg = $errOut.ToString()
        }
    } catch {
        if ($_.Exception.Message -match "SECURITY REFUSAL") {
            $refused = $true
            $errorMsg = $_.Exception.Message
        }
    }

    $checkPassed = $true
    if ($CheckBlock) {
        $checkPassed = & $CheckBlock $setupData $refused
    } else {
        $checkPassed = ($refused -eq $MustRefuse)
    }

    $status = if ($checkPassed) { "PASS" } else { "FAIL" }
    $color = if ($checkPassed) { "Green" } else { "Red" }
    Write-Host ("[{0}] {1} (Refused={2})" -f $status, $Name, $refused) -ForegroundColor $color
    return [PSCustomObject]@{ Name = $Name; Passed = $checkPassed; Refused = $refused; Error = $errorMsg }
}

# 1. Direct sandbox root without trailing slash
$testResults += Test-Attack -Name "Sandbox Root Without Slash" -Path $sandboxRoot -MustRefuse $true

# 2. Direct sandbox root with trailing backslash
$testResults += Test-Attack -Name "Sandbox Root With Trailing Backslash" -Path ($sandboxRoot + "\") -MustRefuse $true

# 3. Direct sandbox root with trailing forward slash
$testResults += Test-Attack -Name "Sandbox Root With Trailing ForwardSlash" -Path ($sandboxRoot + "/") -MustRefuse $true

# 4. Sibling directory with underscore suffix
$sib1 = Join-Path $env:TEMP "HPDynamoBenchmark_evil"
$testResults += Test-Attack -Name "Sibling Underscore" -Path $sib1 -MustRefuse $true -SetupBlock {
    New-Item -ItemType Directory -Path $sib1 -Force | Out-Null
    return $sib1
} -CheckBlock {
    param($dir, $ref)
    $exists = Test-Path $dir
    Remove-Item -Path $dir -Recurse -Force -ErrorAction SilentlyContinue
    return ($ref -and $exists)
}

# 5. Sibling directory with hyphen suffix
$sib2 = Join-Path $env:TEMP "HPDynamoBenchmark-evil"
$testResults += Test-Attack -Name "Sibling Hyphen" -Path $sib2 -MustRefuse $true -SetupBlock {
    New-Item -ItemType Directory -Path $sib2 -Force | Out-Null
    return $sib2
} -CheckBlock {
    param($dir, $ref)
    $exists = Test-Path $dir
    Remove-Item -Path $dir -Recurse -Force -ErrorAction SilentlyContinue
    return ($ref -and $exists)
}

# 6. Sibling directory with number suffix
$sib3 = Join-Path $env:TEMP "HPDynamoBenchmark2"
$testResults += Test-Attack -Name "Sibling Number" -Path $sib3 -MustRefuse $true -SetupBlock {
    New-Item -ItemType Directory -Path $sib3 -Force | Out-Null
    return $sib3
} -CheckBlock {
    param($dir, $ref)
    $exists = Test-Path $dir
    Remove-Item -Path $dir -Recurse -Force -ErrorAction SilentlyContinue
    return ($ref -and $exists)
}

# 7. Sibling directory with dot suffix
$sib4 = Join-Path $env:TEMP "HPDynamoBenchmark.bak"
$testResults += Test-Attack -Name "Sibling Dot Extension" -Path $sib4 -MustRefuse $true -SetupBlock {
    New-Item -ItemType Directory -Path $sib4 -Force | Out-Null
    return $sib4
} -CheckBlock {
    param($dir, $ref)
    $exists = Test-Path $dir
    Remove-Item -Path $dir -Recurse -Force -ErrorAction SilentlyContinue
    return ($ref -and $exists)
}

# 8. Directory traversal with forward slashes
$realVictim = Join-Path $env:TEMP "OtherVictim"
$trav1 = (Join-Path $env:TEMP "HPDynamoBenchmark/subdir/../../OtherVictim").Replace('\', '/')
$testResults += Test-Attack -Name "Traversal Forward Slashes" -Path $trav1 -MustRefuse $true -SetupBlock {
    New-Item -ItemType Directory -Path $realVictim -Force | Out-Null
    return $realVictim
} -CheckBlock {
    param($victim, $ref)
    $exists = Test-Path $victim
    Remove-Item -Path $victim -Recurse -Force -ErrorAction SilentlyContinue
    return ($ref -and $exists)
}

# 9. Case variation in prefix (lowercase temp)
$legit1 = Join-Path ($sandboxRoot.ToLower()) "Session_CaseTest"
$testResults += Test-Attack -Name "Legitimate Lowercase Prefix" -Path $legit1 -MustRefuse $false -SetupBlock {
    New-Item -ItemType Directory -Path $legit1 -Force | Out-Null
    return $legit1
} -CheckBlock {
    param($dir, $ref)
    $deleted = -not (Test-Path $dir)
    return ((-not $ref) -and $deleted)
}

# 10. NTFS Junction Attack (Crucial!)
$victimFolder = Join-Path $env:TEMP "AdversaryJunctionVictim"
$victimFile = Join-Path $victimFolder "precious_file.txt"
$junctionSandbox = Join-Path $sandboxRoot "Session_JunctionAttack"
$junctionLink = Join-Path $junctionSandbox "external_junction"

$testResults += Test-Attack -Name "NTFS Junction Escape Protection" -Path $junctionSandbox -MustRefuse $false -SetupBlock {
    New-Item -ItemType Directory -Path $victimFolder -Force | Out-Null
    Set-Content -Path $victimFile -Value "DO_NOT_DELETE" -Force
    New-Item -ItemType Directory -Path $junctionSandbox -Force | Out-Null
    cmd /c ("mklink /J `"$junctionLink`" `"$victimFolder`"") | Out-Null
    return @{ VictimFolder = $victimFolder; VictimFile = $victimFile; Sandbox = $junctionSandbox }
} -CheckBlock {
    param($data, $ref)
    $sandboxDeleted = -not (Test-Path $data.Sandbox)
    $victimSurvived = (Test-Path $data.VictimFile) -and ((Get-Content $data.VictimFile -Raw).Trim() -eq "DO_NOT_DELETE")
    Remove-Item -Path $data.VictimFolder -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host ("    [Detail] SandboxDeleted={0}, VictimSurvived={1}" -f $sandboxDeleted, $victimSurvived) -ForegroundColor Gray
    return ($sandboxDeleted -and $victimSurvived)
}

# 11. Empty and whitespace paths
$testResults += Test-Attack -Name "Empty String Path" -Path "" -MustRefuse $false -CheckBlock { param($d, $ref) return (-not $ref) }
$testResults += Test-Attack -Name "Whitespace String Path" -Path "   " -MustRefuse $false -CheckBlock { param($d, $ref) return (-not $ref) }

# 12. Non-existent path
$nonExist = Join-Path $sandboxRoot "Session_NonExistent_9999"
$testResults += Test-Attack -Name "Non-Existent Path Safely Ignored" -Path $nonExist -MustRefuse $false -CheckBlock { param($d, $ref) return (-not $ref) }

# 13. System32 / Windows attack
$testResults += Test-Attack -Name "Windows System32 Attack" -Path "C:\Windows\System32" -MustRefuse $true

# 14. Repo root attack
$testResults += Test-Attack -Name "Repository Root Attack" -Path (Get-Location).Path -MustRefuse $true

# 15. Legitimate nested session with files & subfolders
$legitSession = Join-Path $sandboxRoot "Session_LegitComplex"
$testResults += Test-Attack -Name "Legitimate Complex Nested Session Cleanup" -Path $legitSession -MustRefuse $false -SetupBlock {
    New-Item -ItemType Directory -Path (Join-Path $legitSession "sub1\sub2") -Force | Out-Null
    Set-Content -Path (Join-Path $legitSession "sub1\file1.txt") -Value "data" -Force
    Set-Content -Path (Join-Path $legitSession "sub1\sub2\file2.rvt") -Value "model" -Force
    return $legitSession
} -CheckBlock {
    param($dir, $ref)
    $deleted = -not (Test-Path $dir)
    return ((-not $ref) -and $deleted)
}

$total = $testResults.Count
$passed = ($testResults | Where-Object { $_.Passed }).Count
Write-Host ("=== Deep Adversary Summary: {0} / {1} Passed ===" -f $passed, $total) -ForegroundColor Cyan

if ($passed -ne $total) {
    exit 1
}
