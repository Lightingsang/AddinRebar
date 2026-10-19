import os
import re

files_to_check = [
    r"HPBim/src/HPBim.Client/Recovery/CoordinationReceiptRecord.cs",
    r"HPBim/src/HPBim.Client/Recovery/ExtensibleStorageQueryScriptTemplate.cs",
    r"HPBim/src/HPBim.Client/Recovery/ExtensibleStorageReceiptReader.cs",
    r"HPBim/src/HPBim.Client/Recovery/ReconcileService.cs",
    r"HPBim/src/HPBim.Client/Pipeline/BimPipelineEngine.PlanExecution.cs",
    r"HPBim/src/HPBim.Cli/Commands/GenerateCommand.cs",
    r"HPBim/tests/HPBim.Client.Tests/Fakes/FakeRevitPipeClient.cs",
    r"HPBim/tests/HPBim.Client.Tests/Recovery/ExtensibleStorageReceiptReaderTests.cs",
    r"HPBim/tests/HPBim.Client.Tests/Recovery/AdversarialReceiptReaderTests.cs",
    r"HPBim/tests/HPBim.Client.Tests/Recovery/ReceiptReaderTier5AdversarialTests.cs",
    r"HPBim/tests/HPBim.Client.Tests/Recovery/AdversarialReconcileIdempotencyTests.cs",
    r"HPBim/tests/HPBim.Client.Tests/Recovery/ReconcileServiceTests.cs",
    r"HPBim/tests/HPBim.Client.Tests/Recovery/ReconcileServiceTier5AdversarialTests.cs",
    r"HPBim/tests/HPBim.Client.Tests/Recovery/ReconcileReceiptCompletenessTests.cs",
    r"HPBim/tests/HPBim.Client.Tests/Recovery/ReconcileDocumentIdentityTests.cs",
    r"HPBim/tests/HPBim.Client.Tests/Cli/GenerateCommandColdProcessRerunTests.cs",
    r"HPBim/tests/HPBim.E2E.Tests/Tiers/Tier4_RealWorldWorkloadTests.cs"
]

violations = []

for rel_path in files_to_check:
    full_path = os.path.abspath(rel_path)
    if not os.path.exists(full_path):
        violations.append(f"MISSING FILE: {rel_path}")
        continue
    with open(full_path, "r", encoding="utf-8") as f:
        lines = f.readlines()
    
    line_count = len(lines)
    if line_count > 300:
        violations.append(f"C2 VIOLATION: {rel_path} has {line_count} lines (> 300)")
    else:
        print(f"PASS C2 ({line_count:3d} lines): {rel_path}")

    # Check for empty catches
    content = "".join(lines)
    # search for catch (...) { }
    empty_catches = re.findall(r"catch\s*\([^)]*\)\s*\{\s*\}", content)
    if empty_catches:
        violations.append(f"Q-B2 VIOLATION: Empty catch block in {rel_path}")

print("\n--- Summary ---")
if violations:
    print("Found violations:")
    for v in violations:
        print("  -", v)
else:
    print("All 17 files passed line-count (C2 <= 300) and catch checks!")
