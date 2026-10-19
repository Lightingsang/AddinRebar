$coreDll = "G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\src\HPBim.Core\bin\Debug\netstandard2.0\HPBim.Core.dll"
[System.Reflection.Assembly]::LoadFrom($coreDll) | Out-Null

$script:results = [System.Collections.Generic.List[psobject]]::new()

function Record-Test($name, $passed, $details) {
    $script:results.Add([PSCustomObject]@{
        Test = $name
        Passed = $passed
        Details = $details
    })
}

# 1. CompositeSourceKey Stress Tests
try {
    $k1 = [HPBim.Core.Coordination.CompositeSourceKey]::Parse("[Drawing.dwg]#[Column]:A1")
    $k2 = [HPBim.Core.Coordination.CompositeSourceKey]::Parse("DRAWING.DWG#column:a1")
    Record-Test "Key Case-Insensitive Equality" ($k1.Equals($k2) -and ($k1.GetHashCode() -eq $k2.GetHashCode())) "Hash and Equals match across casing"
} catch {
    Record-Test "Key Case-Insensitive Equality" $false $_.Exception.Message
}

try {
    $parsed = [HPBim.Core.Coordination.CompositeSourceKey]::TryParse("invalid_format_without_hash", [ref]$null)
    Record-Test "Key TryParse Invalid Returns False" (-not $parsed) "Successfully rejected invalid format"
} catch {
    Record-Test "Key TryParse Invalid Returns False" $false $_.Exception.Message
}

try {
    $parsed = [HPBim.Core.Coordination.CompositeSourceKey]::TryParse("[Doc]#[NonExistentCategory]:H1", [ref]$null)
    Record-Test "Key TryParse Invalid Category Returns False" (-not $parsed) "Successfully rejected invalid category enum"
} catch {
    Record-Test "Key TryParse Invalid Category Returns False" $false $_.Exception.Message
}

try {
    $k = [HPBim.Core.Coordination.CompositeSourceKey]::Parse("   [Doc]   #   [Wall]   :   W100   ")
    Record-Test "Key Whitespace Trimming" ($k.DocIdentifier -eq "Doc" -and $k.Category.ToString() -eq "Wall" -and $k.PrimaryHandle -eq "W100") "Properly trimmed spaces"
} catch {
    Record-Test "Key Whitespace Trimming" $false $_.Exception.Message
}

# 2. DocumentIdentitySnapshot Stress Tests
try {
    $guid1 = [System.Guid]::NewGuid()
    $snap = [HPBim.Core.Execution.DocumentIdentitySnapshot]@{
        Title = "MyProject.rvt"
        VersionGuid = $guid1
        CentralGuid = [System.Guid]::NewGuid()
    }
    $matchSame = $snap.Matches("myproject.rvt", $guid1)
    $matchDiffTitle = $snap.Matches("OtherProject.rvt", $guid1)
    $matchDiffGuid = $snap.Matches("MyProject.rvt", [System.Guid]::NewGuid())
    Record-Test "Snapshot Title Casing & Match Verification" ($matchSame -and -not $matchDiffTitle -and -not $matchDiffGuid -and $snap.IsWorkshared) "Matches case-insensitively on Title and strict on VersionGuid"
} catch {
    Record-Test "Snapshot Title Casing & Match Verification" $false $_.Exception.Message
}

# 3. BimCoordinationLedger Case-Insensitive Mapping Lookup
try {
    $ledger = New-Object HPBim.Core.Coordination.BimCoordinationLedger
    $keyReg = [HPBim.Core.Coordination.CompositeSourceKey]::Parse("[PLAN_A.DWG]#[COLUMN]:COL_101")
    $mapping = [HPBim.Core.Coordination.LedgerElementMapping]::Create(
        $keyReg,
        "rvt-unique-id-9999",
        12345,
        [HPBim.Core.Coordination.CoordinationCategory]::Column,
        "C101",
        "hash-abc-123"
    )
    $ledger = $ledger.RegisterMapping($mapping)

    $keyLookup = [HPBim.Core.Coordination.CompositeSourceKey]::Parse("plan_a.dwg#column:col_101")
    $uidOut = $null
    $found = $ledger.TryGetTarget($keyLookup, [ref]$uidOut)
    Record-Test "Ledger Case-Insensitive Lookup" ($found -and ($uidOut -eq "rvt-unique-id-9999")) "Retrieved mapped UniqueId regardless of casing differences"
} catch {
    Record-Test "Ledger Case-Insensitive Lookup" $false $_.Exception.Message
}

# 4. IdempotentElementResolver Null Checks
try {
    [HPBim.Core.Coordination.IdempotentElementResolver]::Resolve($null, $ledger, $null)
    Record-Test "Resolver Null Check Throws" $false "Should have thrown ArgumentNullException"
} catch [System.Management.Automation.MethodInvocationException] {
    $inner = $_.Exception.InnerException
    $isArgNull = ($inner -is [System.ArgumentNullException])
    Record-Test "Resolver Null Check Throws" $isArgNull "Successfully threw ArgumentNullException on null argument"
} catch {
    Record-Test "Resolver Null Check Throws" $false $_.Exception.Message
}

$script:results | Format-Table -AutoSize
