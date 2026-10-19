import os
import re

FILES = [
    r"HPBim\src\HPBim.Client\Connection\IntentStatus.cs",
    r"HPBim\src\HPBim.Client\Connection\IntentJournalRecord.cs",
    r"HPBim\src\HPBim.Client\Connection\IIntentJournal.cs",
    r"HPBim\src\HPBim.Client\Connection\IntentJournal.cs",
    r"HPBim\src\HPBim.Client\Connection\TimeoutPolicy.cs",
    r"HPBim\src\HPBim.Client\Recovery\CoordinationReceiptRecord.cs",
    r"HPBim\src\HPBim.Client\Recovery\IExtensibleStorageReceiptReader.cs",
    r"HPBim\src\HPBim.Client\Recovery\ExtensibleStorageQueryScriptTemplate.cs",
    r"HPBim\src\HPBim.Client\Recovery\ExtensibleStorageReceiptReader.cs",
    r"HPBim\src\HPBim.Client\Recovery\ReconcileStatus.cs",
    r"HPBim\src\HPBim.Client\Recovery\ReconcileResult.cs",
    r"HPBim\src\HPBim.Client\Recovery\IReconcileService.cs",
    r"HPBim\src\HPBim.Client\Recovery\ReconcileService.cs",
    r"HPBim\src\HPBim.Client\Pipeline\BimPlanRunnerScriptTemplate.Receipt.cs",
    r"HPBim\src\HPBim.Client\Pipeline\BimPipelineEngine.IntentJournal.cs",
    r"HPBim\tests\HPBim.Client.Tests\Connection\IntentJournalTests.cs",
    r"HPBim\tests\HPBim.Client.Tests\Connection\TimeoutPolicyTests.cs",
    r"HPBim\tests\HPBim.Client.Tests\Recovery\ExtensibleStorageReceiptReaderTests.cs",
    r"HPBim\tests\HPBim.Client.Tests\Recovery\ReconcileServiceTests.cs",
    r"HPBim\tests\HPBim.Client.Tests\Recovery\AutoReconcileIdempotencyTests.cs",
    r"HPBim\src\HPBim.Client\Pipeline\BimPlanRunnerScriptTemplate.cs",
    r"HPBim\src\HPBim.Client\Pipeline\BimPipelineEngine.cs",
    r"HPBim\src\HPBim.Client\Pipeline\BimPipelineResult.cs",
    r"HPBim\tests\HPBim.Client.Tests\Fakes\FakeRevitPipeClient.cs",
]

root_dir = r"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar"

print("--- FILE LINE COUNTS (C2 <= 300) ---")
for rel in FILES:
    p = os.path.join(root_dir, rel)
    with open(p, 'r', encoding='utf-8') as f:
        lines = f.readlines()
    count = len(lines)
    status = "PASS" if count <= 300 else "FAIL"
    print(f"{rel:<70} | {count:4d} lines | C2: {status}")
