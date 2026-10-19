import os
import re

src_dir = "HPBim/src"
test_dir = "HPBim/tests"

suspicious_patterns = [
    (r'return\s+(?:true|false)\s*;', "Literal boolean return"),
    (r'throw\s+new\s+NotImplementedException', "NotImplementedException stub"),
    (r'TODO', "TODO comment"),
    (r'FIXME', "FIXME comment"),
    (r'hack', "Hack keyword"),
    (r'mock', "Mock in src"),
    (r'fake', "Fake in src"),
    (r'stub', "Stub in src"),
]

print("=" * 80)
print("ADVERSARIAL INTEGRITY AUDIT: CODE SCAN")
print("=" * 80)

# Scan src
findings = []
for root, dirs, files in os.walk(src_dir):
    for f in files:
        if not f.endswith(".cs"): continue
        path = os.path.join(root, f)
        with open(path, "r", encoding="utf-8") as file:
            lines = file.readlines()
        for idx, line in enumerate(lines):
            l = line.strip()
            # check hardcoded test names in src
            if any(term in l for term in ["NhaPho_5x18m", "Col-220x220", "TestProject", "TestDoc"]):
                if not path.endswith("Fixture.cs"):
                    findings.append((path, idx+1, "Test artifact reference in production code", l))
            for pat, desc in suspicious_patterns:
                if re.search(pat, l, re.I):
                    # Check if it's benign (like public bool IsValid => true; or fake client)
                    if desc == "Literal boolean return":
                        # benign cases like: if (condition) return true;
                        if re.search(r'^\s*(?:return\s+(?:true|false)\s*;)', line) and not any(k in lines[idx-1] for k in ["if", "case", "else"]):
                            findings.append((path, idx+1, desc, l))
                    elif "in src" in desc:
                        if not any(k in path.lower() for k in ["fake", "mock", "test"]):
                            findings.append((path, idx+1, desc, l))
                    else:
                        findings.append((path, idx+1, desc, l))

print(f"Total suspicious items flagged in src: {len(findings)}")
for f in findings[:25]:
    print(f"[{f[2]}] {f[0]}:{f[1]} -> {f[3]}")

# Also verify worker line counts reported in worker_m3_r1/report.md vs actual lines
print("\n" + "=" * 80)
print("LINE COUNT ATTESTATION CROSS-EXAMINATION")
print("=" * 80)

reported = {
    'HPBim/src/HPBim.Core/Execution/DocumentIdentitySnapshot.cs': 51,
    'HPBim/src/HPBim.Client/Pipeline/DocumentIdentityQuery.cs': 96,
    'HPBim/src/HPBim.Core/Execution/ElementPlanItem.cs': 30,
    'HPBim/src/HPBim.Core/Coordination/CompositeSourceKey.cs': 141,
    'HPBim/src/HPBim.Core/Coordination/LedgerElementMapping.cs': 58,
    'HPBim/src/HPBim.Core/Coordination/BimCoordinationLedger.cs': 102,
    'HPBim/src/HPBim.Core/Coordination/BimCoordinationLedgerService.cs': 290,
    'HPBim/src/HPBim.Core/Coordination/BimCoordinationLedgerService.Mappings.cs': 107,
    'HPBim/src/HPBim.Core/Coordination/IdempotentElementResolver.cs': 128,
    'HPBim/src/HPBim.Client/Pipeline/BimPlanRunnerScriptTemplate.cs': 181,
    'HPBim/src/HPBim.Client/Pipeline/BimPlanRunnerScriptTemplate.Structure.cs': 267,
    'HPBim/src/HPBim.Client/Pipeline/BimPlanRunnerScriptTemplate.Architecture.cs': 229,
    'HPBim/src/HPBim.Client/Pipeline/BimPlanRunnerScriptTemplate.MepAndRebar.cs': 184,
    'HPBim/src/HPBim.Client/Pipeline/BimPipelineEngine.PlanExecution.cs': 273,
    'HPBim/src/HPBim.Client/Pipeline/BimPipelineResult.cs': 40,
    'HPBim/tests/HPBim.Core.Tests/Coordination/CompositeSourceKeyTests.cs': 185,
    'HPBim/tests/HPBim.Client.Tests/Pipeline/PreFlightGateTests.cs': 179,
    'HPBim/tests/HPBim.Core.Tests/Coordination/BimCoordinationLedgerIdempotencyTests.cs': 196,
    'HPBim/tests/HPBim.Client.Tests/Pipeline/IdempotentExecutionTests.cs': 206
}

for path, rep_lines in reported.items():
    if not os.path.exists(path):
        print(f"MISSING: {path}")
        continue
    with open(path, "r", encoding="utf-8") as file:
        actual = len(file.readlines())
    diff = actual - rep_lines
    status = "EXACT" if diff == 0 else f"DELTA: {diff:+d}"
    print(f"{path}: Reported={rep_lines}, Actual={actual} [{status}]")
