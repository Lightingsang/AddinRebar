import os
import re

ROOT = r"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar"

FILES = [
    r"HPBim\tests\HPBim.Core.Tests\Adversarial\BimModelSchemaValidatorAdversarialCoverageTests.cs",
    r"HPBim\tests\HPBim.Core.Tests\Adversarial\IdempotentElementResolverAdversarialCoverageTests.cs",
    r"HPBim\tests\HPBim.Core.Tests\Adversarial\CompositeSourceKeyAdversarialCoverageTests.cs",
    r"HPBim\tests\HPBim.Core.Tests\Adversarial\MeasuredGeometricVerifierAdversarialCoverageTests.cs",
    r"HPBim\tests\HPBim.Client.Tests\Connection\IntentJournalTier5AdversarialTests.cs",
    r"HPBim\tests\HPBim.Client.Tests\Connection\TimeoutPolicyTier5AdversarialTests.cs",
    r"HPBim\tests\HPBim.Client.Tests\Recovery\ReceiptReaderTier5AdversarialTests.cs",
    r"HPBim\tests\HPBim.Client.Tests\Recovery\ReconcileServiceTier5AdversarialTests.cs",
    r"HPBim\tests\HPBim.Client.Tests\Pipeline\BimPlanRunnerScriptTemplateTier5AdversarialTests.cs",
    r"HPBim\tests\HPBim.Client.Tests\Pipeline\BimPipelineEngineTier5AdversarialTests.cs",
]

def analyze_file(rel_path):
    full_path = os.path.join(ROOT, rel_path)
    with open(full_path, "r", encoding="utf-8") as f:
        lines = f.readlines()
    total_lines = len(lines)

    # find namespace
    namespace = None
    for line in lines:
        m = re.match(r"^\s*namespace\s+([\w\.]+)", line)
        if m:
            namespace = m.group(1)
            break

    # find class name
    class_name = None
    for line in lines:
        m = re.match(r"^\s*public\s+class\s+(\w+)", line)
        if m:
            class_name = m.group(1)
            break

    return {
        "rel_path": rel_path,
        "filename": os.path.basename(rel_path),
        "total_lines": total_lines,
        "namespace": namespace,
        "class_name": class_name,
    }

for f in FILES:
    res = analyze_file(f)
    print(f"{res['filename']} | Lines: {res['total_lines']} | NS: {res['namespace']} | Class: {res['class_name']}")
