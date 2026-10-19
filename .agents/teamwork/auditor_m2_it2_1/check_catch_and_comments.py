import sys
import io

# Force stdout to utf-8
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')

FILES = [
    r"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\src\HPBim.Client\Recovery\ExtensibleStorageReceiptReader.cs",
    r"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\src\HPBim.Client\Recovery\ReconcileService.cs",
    r"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\src\HPBim.Client\Pipeline\BimPipelineEngine.PlanExecution.cs",
    r"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\tests\HPBim.Client.Tests\Cli\GenerateCommandColdProcessRerunTests.cs",
    r"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\tests\HPBim.Client.Tests\Fakes\FakeRevitPipeClient.cs",
    r"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\tests\HPBim.Client.Tests\Recovery\ChallengerM2AdversarialPayloadTests.cs",
    r"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\tests\HPBim.E2E.Tests\Tiers\Tier4_RealWorldWorkloadTests.cs"
]

for fpath in FILES:
    print(f"=== Checking {fpath} ===")
    with open(fpath, 'r', encoding='utf-8') as f:
        lines = f.readlines()
    
    # Catch inspection
    catches = []
    for i, line in enumerate(lines):
        if 'catch' in line:
            catches.append((i+1, line.strip()))
            print(f"  Line {i+1}: {line.strip()}")
            for j in range(i+1, min(len(lines), i+6)):
                print(f"    Line {j+1}: {lines[j].strip()}")

    # Comment inspection: print every comment line
    comment_lines = []
    for i, line in enumerate(lines):
        s = line.strip()
        if s.startswith('//') or s.startswith('/*') or s.startswith('*'):
            comment_lines.append((i+1, s))
    print(f"  Total comment lines: {len(comment_lines)}")
    for lno, c in comment_lines:
        print(f"    L{lno}: {c}")

print("\nAll files scanned successfully.")
