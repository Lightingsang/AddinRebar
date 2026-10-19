import os
import re

files = [
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

base_dir = r"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar"

print("=" * 80)
print("INDEPENDENT STATIC & INTEGRITY AUDIT REPORT")
print("=" * 80)

# 1. Search for dummy or trivial assertions: Assert.True(true), Assert.False(false), Assert.Equal(1, 1), empty methods
trivial_patterns = [
    re.compile(r'Assert\.True\s*\(\s*true\s*\)', re.IGNORECASE),
    re.compile(r'Assert\.False\s*\(\s*false\s*\)', re.IGNORECASE),
    re.compile(r'Assert\.Equal\s*\(\s*(\d+)\s*,\s*\1\s*\)', re.IGNORECASE),
    re.compile(r'Assert\.Equal\s*\(\s*["\']([^"\']+)["\']\s*,\s*["\']\1["\']\s*\)', re.IGNORECASE),
]

# 2. Search for commented out code
commented_code_keywords = [
    "Assert.", "var ", "new ", "if (", "for (", "while (", "return ", "throw ", "await ", "async ", "public ", "private "
]

# 3. Check catch blocks
catch_re = re.compile(r'catch\s*(\([^\)]*\))?\s*\{([^}]*)\}', re.DOTALL)

integrity_issues = []
commented_code_findings = []
undocumented_catches = []

for rel in files:
    full = os.path.join(base_dir, rel)
    fname = os.path.basename(rel)
    with open(full, 'r', encoding='utf-8') as f:
        content = f.read()
        lines = content.splitlines()

    # Integrity: check trivial assertions
    for i, line in enumerate(lines, 1):
        for pat in trivial_patterns:
            if pat.search(line):
                integrity_issues.append(f"{fname}:{i}: Trivial assertion: {line.strip()}")

    # Q-B1: Commented out code
    for i, line in enumerate(lines, 1):
        stripped = line.strip()
        if stripped.startswith("//"):
            comment_text = stripped[2:].strip()
            for kw in commented_code_keywords:
                if comment_text.startswith(kw):
                    commented_code_findings.append(f"{fname}:{i}: [{kw.strip()}] {stripped}")

    # Q-B2: Catch blocks
    for match in catch_re.finditer(content):
        block_content = match.group(2)
        has_comment = "//" in block_content or "/*" in block_content
        if not has_comment and not block_content.strip():
            undocumented_catches.append(f"{fname}: Empty catch without comment: {match.group(0)}")
        elif not has_comment and len(block_content.strip()) > 0:
            undocumented_catches.append(f"{fname}: Catch block without rationale comment: {match.group(0)}")

print(f"1. Integrity Issues (Trivial/Fake Assertions): {len(integrity_issues)}")
for issue in integrity_issues:
    print(f"   - {issue}")

print(f"2. Commented-out Code (Q-B1): {len(commented_code_findings)}")
for c in commented_code_findings:
    print(f"   - {c}")

print(f"3. Undocumented Catch Blocks (Q-B2): {len(undocumented_catches)}")
for catch in undocumented_catches:
    print(f"   - {catch}")

print("=" * 80)
