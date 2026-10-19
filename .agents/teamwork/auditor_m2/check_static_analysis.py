import os
import re

files = [
    r'McpShared/HPRebar.Mcp.Contracts/Messages/ExecuteResult.cs',
    r'HPRebar/HPRebar.McpBridge/Service/AutoDismissFailurePreprocessor.cs',
    r'HPRebar/HPRebar.McpBridge/Service/ScriptRunner.cs',
    r'HPBim/src/HPBim.Client/Pipeline/BimPipelineResult.cs',
    r'HPBim/src/HPBim.Client/Pipeline/StageResult.cs',
    r'HPBim/src/HPBim.Client/Pipeline/BimPipelineEngine.PlanExecution.cs',
    r'HPBim/src/HPBim.Client/Pipeline/BimPipelineEngine.cs',
    r'HPBim/tests/HPBim.Client.Tests/Fakes/FakeRevitPipeClient.cs',
    r'HPBim/tests/HPBim.E2E.Tests/Harness/E2EFakeRevitPipeClient.cs',
    r'HPBim/tests/HPBim.Client.Tests/Pipeline/DryRunRollbackHonestyTests.cs',
    r'HPBim/src/HPBim.Core/Qc/FoundationAuditModels.cs',
    r'HPBim/src/HPBim.Core/Qc/FoundationLedgerAuditor.cs',
    r'HPBim/tests/HPBim.Core.Tests/FoundationQcEngineTests.cs',
    r'HPBim/src/HPBim.Core/Verification/GeometricVerificationStatus.cs',
    r'HPBim/src/HPBim.Core/Verification/ActualElementGeometry.cs',
    r'HPBim/src/HPBim.Core/Verification/ElementGeometricAudit.cs',
    r'HPBim/src/HPBim.Core/Verification/GeometricAuditReport.cs',
    r'HPBim/src/HPBim.Client/Verification/IMeasuredGeometricVerifier.cs',
    r'HPBim/src/HPBim.Client/Verification/MeasuredGeometryJsonParser.cs',
    r'HPBim/src/HPBim.Client/Verification/RevitGeometryQueryScriptTemplate.cs',
    r'HPBim/src/HPBim.Client/Verification/MeasuredGeometricVerifier.cs',
    r'HPBim/tests/HPBim.Client.Tests/Verification/MeasuredGeometricVerifierTests.cs',
    r'HPBim/src/HPBim.Core/Qc/FoundationGridLevelValidator.cs',
    r'HPBim/src/HPBim.Core/Qc/FoundationQcEngine.cs',
    r'HPBim/src/HPBim.Core/Verification/MeasuredGeometricEngine.cs',
    r'HPBim/src/HPBim.Core/Verification/MeasuredGeometricEngine.Structural.cs',
    r'HPBim/src/HPBim.Core/Verification/MeasuredGeometricEngine.ProfileGrid.cs',
    r'HPBim/tests/HPBim.Core.Tests/Verification/MeasuredGeometricToleranceTests.cs',
    r'HPBim/tests/HPBim.Core.Tests/Verification/MeasuredGeometricEngineTests.cs'
]

print("=== CHECKING FOR EMPTY CATCHES ===")
empty_catches = []
catch_re = re.compile(r'catch\s*(?:\([^)]*\))?\s*\{([^}]*)\}', re.MULTILINE | re.DOTALL)

for f in files:
    with open(f, 'r', encoding='utf-8') as fp:
        content = fp.read()
    matches = catch_re.findall(content)
    for m in matches:
        body = m.strip()
        # If body is empty or has no comment or code
        if not body:
            empty_catches.append((f, "completely empty catch {}"))
        elif not any(c in body for c in ['//', '/*']) and not any(w in body for w in ['throw', 'return', 'log', 'Log', 'Console']):
            empty_catches.append((f, f"suspicious catch body: {body}"))

print(f"Empty/uncommented catches found: {len(empty_catches)}")
for ec in empty_catches:
    print(f"  {ec[0]}: {ec[1]}")

print("\n=== CHECKING FOR COMMENTED-OUT CODE ===")
commented_code = []
code_pattern = re.compile(r'^\s*//\s*(?:var\s+|if\s*\(|return\s+|for\s*\(|foreach\s*\(|new\s+[A-Z]|Assert\.|throw\s+).*;\s*$')

for f in files:
    with open(f, 'r', encoding='utf-8') as fp:
        lines = fp.readlines()
    for idx, l in enumerate(lines, 1):
        if code_pattern.match(l):
            # Exclude explanatory comments
            commented_code.append((f, idx, l.strip()))

print(f"Commented-out code lines found: {len(commented_code)}")
for cc in commented_code:
    print(f"  {cc[0]}:{cc[1]} -> {cc[2]}")

print("\n=== CHECKING FOR HARDCODED RETURN CONSTANTS IN LOGIC ===")
# Look for methods returning fixed literals like `return true;` without any computation
for f in files:
    if 'Tests' in f:
        continue
    with open(f, 'r', encoding='utf-8') as fp:
        lines = fp.readlines()
    for idx, l in enumerate(lines, 1):
        stripped = l.strip()
        if stripped in ('return true;', 'return false;') and idx > 1:
            prev = lines[idx-2].strip()
            # If method body is only `return true;`
            if prev == '{':
                print(f"Suspicious immediate literal return: {f}:{idx} -> {stripped}")
