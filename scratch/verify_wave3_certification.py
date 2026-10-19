import os
import re

files = [
    r'HPBim/src/HPBim.Client/Connection/CrossProcessFileLock.cs',
    r'HPBim/src/HPBim.Client/Connection/IntentJournal.cs',
    r'HPBim/src/HPBim.Client/Connection/IntentJournal.Storage.cs',
    r'HPBim/src/HPBim.Client/Recovery/ReconcileService.cs',
    r'HPBim/src/HPBim.Client/Recovery/ReconcileService.DocSnapshot.cs',
    r'HPBim/src/HPBim.Core/Models/DocSnapshot.cs',
    r'HPBim/src/HPBim.Core/Coordination/CompositeSourceKeyJsonConverter.cs',
    r'HPBim/src/HPBim.Client/Pipeline/BimPipelineEngine.ElementRecords.cs',
    r'HPBim/src/HPBim.Client/Pipeline/BimPipelineEngine.ElementRecords.Validation.cs',
    r'HPBim/src/HPBim.Client/Pipeline/BimPipelineEngine.PlanExecution.cs',
    r'HPBim/src/HPBim.Core/Verification/MeasuredGeometricEngine.cs',
    r'HPBim/src/HPBim.Core/Verification/MeasuredGeometricEngine.Structural.cs',
    r'HPBim/src/HPBim.Core/Verification/MeasuredGeometricEngine.ProfileGrid.cs',
    r'HPBim/tests/HPBim.Core.Tests/Verification/StandardizedGeometricMeasurementR5Tests.cs'
]

print("=== 1. FILE LINE COUNTS (Rule C2 <= 300) ===")
all_c2_pass = True
for f in files:
    with open(f, 'r', encoding='utf-8') as fp:
        lines = fp.readlines()
    count = len(lines)
    status = "PASS" if count <= 300 else "FAIL"
    if count > 300:
        all_c2_pass = False
    print(f"{status}: {count:3d} lines | {f}")
print(f"Rule C2 Overall: {'PASS' if all_c2_pass else 'FAIL'}\n")

print("=== 2. COMMENTED-OUT CODE (Rule Q-B1) ===")
code_comment_pattern = re.compile(r'^\s*//\s*(?:[a-zA-Z0-9_<>]+\s+[a-zA-Z0-9_]+\s*=|return\s|if\s*\(|for\s*\(|while\s*\(|var\s|await\s|\S+\.\S+\(.*;|.*\{|.*\};)')
all_qb1_pass = True
for f in files:
    with open(f, 'r', encoding='utf-8') as fp:
        lines = fp.readlines()
    found = []
    for idx, line in enumerate(lines, 1):
        if line.strip().startswith("///"):
            continue
        if code_comment_pattern.search(line):
            found.append((idx, line.strip()))
    if found:
        all_qb1_pass = False
        print(f"FAIL: {f} has potential commented-out code:")
        for lnum, ltext in found:
            print(f"  Line {lnum}: {ltext}")
    else:
        print(f"PASS: 0 commented-out code | {f}")
print(f"Rule Q-B1 Overall: {'PASS' if all_qb1_pass else 'FAIL'}\n")

print("=== 3. CATCH BLOCKS (Rule Q-B2: zero unannotated empty catches) ===")
all_qb2_pass = True
for f in files:
    with open(f, 'r', encoding='utf-8') as fp:
        content = fp.read()
    catch_matches = list(re.finditer(r'catch\s*(?:\([^)]*\))?\s*\{([^}]*)\}', content))
    file_pass = True
    for m in catch_matches:
        body = m.group(1).strip()
        if not body:
            file_pass = False
            all_qb2_pass = False
            print(f"FAIL: {f} has empty catch without comment: {m.group(0)}")
    if file_pass:
        print(f"PASS: 0 unannotated catches ({len(catch_matches)} valid catches) | {f}")
print(f"Rule Q-B2 Overall: {'PASS' if all_qb2_pass else 'FAIL'}\n")

print("=== 4. PURE NETSTANDARD2.0 & ZERO AUTODESK REFERENCES ===")
core_proj = r'HPBim/src/HPBim.Core/HPBim.Core.csproj'
with open(core_proj, 'r', encoding='utf-8') as fp:
    core_csproj_txt = fp.read()

has_autodesk_proj = 'Autodesk' in core_csproj_txt
is_netstandard20 = '<TargetFramework>netstandard2.0</TargetFramework>' in core_csproj_txt
print(f"TargetFramework is netstandard2.0: {is_netstandard20}")
print(f"Autodesk references in HPBim.Core.csproj: {has_autodesk_proj}")

core_autodesk_files = []
for root, _, cfiles in os.walk(r'HPBim/src/HPBim.Core'):
    for cf in cfiles:
        if cf.endswith('.cs'):
            cpath = os.path.join(root, cf)
            with open(cpath, 'r', encoding='utf-8') as fp:
                txt = fp.read()
                if 'Autodesk' in txt:
                    core_autodesk_files.append(cpath)

print(f"Autodesk references in HPBim.Core source files: {len(core_autodesk_files)} (Files: {core_autodesk_files})")
purity_pass = is_netstandard20 and not has_autodesk_proj and len(core_autodesk_files) == 0
print(f"Project Purity Overall: {'PASS' if purity_pass else 'FAIL'}")
