import os
import re

files = [
    r'HPBim/src/HPBim.Client/Recovery/ExtensibleStorageReceiptReader.cs',
    r'HPBim/src/HPBim.Client/Recovery/ReconcileService.cs',
    r'HPBim/src/HPBim.Client/Pipeline/BimPipelineEngine.PlanExecution.cs',
    r'HPBim/tests/HPBim.Client.Tests/Cli/GenerateCommandColdProcessRerunTests.cs',
    r'HPBim/tests/HPBim.Client.Tests/Fakes/FakeRevitPipeClient.cs',
    r'HPBim/tests/HPBim.Client.Tests/Recovery/ChallengerM2AdversarialPayloadTests.cs',
    r'HPBim/tests/HPBim.E2E.Tests/Tiers/Tier4_RealWorldWorkloadTests.cs'
]

print("=== CHECKING FILE LENGTHS (Rule C2 <= 300 lines) ===")
c2_passed = True
for f in files:
    with open(f, 'r', encoding='utf-8') as fp:
        lines = fp.readlines()
    count = len(lines)
    status = "PASS" if count <= 300 else "FAIL"
    print(f"{f}: {count} lines [{status}]")
    if count > 300:
        c2_passed = False

print("\n=== CHECKING COMMENTED-OUT CODE (Rule Q-B1) ===")
qb1_passed = True
code_patterns = [
    r'^\s*//\s*(var|int|string|bool|double|if|for|foreach|while|return|throw|try|catch|using|public|private|protected|internal)\s+',
    r'^\s*//\s*(\w+\s*=\s*.+;)',
    r'^\s*//\s*(\w+\(.*\);)'
]

for f in files:
    with open(f, 'r', encoding='utf-8') as fp:
        lines = fp.readlines()
    for idx, line in enumerate(lines, 1):
        for pat in code_patterns:
            if re.search(pat, line):
                print(f"Potential commented code in {f}:{idx}: {line.strip()}")
                qb1_passed = False

if qb1_passed:
    print("0 commented-out code lines found across all 7 files [PASS]")

print("\n=== CHECKING CATCH BLOCKS ANNOTATION (Rule Q-B2) ===")
qb2_passed = True
for f in files:
    with open(f, 'r', encoding='utf-8') as fp:
        content = fp.read()
    # Find all catch occurrences
    lines = content.splitlines()
    for idx, line in enumerate(lines, 1):
        if 'catch' in line and not line.strip().startswith('//'):
            # Check if line itself has a comment or if next 2 lines have a comment
            has_comment = '//' in line or '/*' in line
            if not has_comment:
                for next_idx in range(idx, min(idx + 3, len(lines))):
                    if '//' in lines[next_idx] or '/*' in lines[next_idx]:
                        has_comment = True
                        break
            print(f"Catch in {f}:{idx} -> '{line.strip()}' -> Has annotation comment: {has_comment}")
            if not has_comment:
                qb2_passed = False

print("\n=== PARSING ALL METHODS AND CONSTRUCTORS (Rule M3 <= 50 lines) ===")
m3_passed = True

def parse_members(filepath):
    with open(filepath, 'r', encoding='utf-8') as fp:
        lines = fp.readlines()
    
    # We want to find each method or constructor declaration and its span
    members = []
    
    i = 0
    n = len(lines)
    while i < n:
        line = lines[i]
        s = line.strip()
        
        # Check if line looks like method or constructor
        # Skip class, struct, record, interface, enum, namespace, using, comments
        if (any(s.startswith(mod) for mod in ['public ', 'private ', 'protected ', 'internal ', 'static ', 'async '])
            and '(' in s
            and not any(k in s for k in ['class ', 'struct ', 'record ', 'interface ', 'delegate '])):
            
            start_line = i + 1
            # Check if signature spans multiple lines
            sig = s
            curr = i
            while curr < n and '{' not in lines[curr] and '=>' not in lines[curr] and not lines[curr].strip().endswith(';'):
                curr += 1
                if curr < n:
                    sig += " " + lines[curr].strip()
            
            # Check if expression-bodied
            if '=>' in lines[curr]:
                # find the semicolon
                while curr < n and ';' not in lines[curr]:
                    curr += 1
                end_line = curr + 1
                length = end_line - start_line + 1
                members.append((sig.split('(')[0].strip(), start_line, end_line, length))
                i = curr + 1
                continue
            
            # Block bodied
            if curr < n and '{' in lines[curr]:
                brace_count = 0
                block_start = curr
                for b_idx in range(curr, n):
                    brace_count += lines[b_idx].count('{') - lines[b_idx].count('}')
                    if brace_count == 0:
                        end_line = b_idx + 1
                        length = end_line - start_line + 1
                        members.append((sig.split('(')[0].strip(), start_line, end_line, length))
                        i = b_idx + 1
                        break
                continue
        i += 1
    return members

for f in files:
    print(f"\n--- {f} ---")
    mems = parse_members(f)
    for name, start, end, count in mems:
        status = "PASS" if count <= 50 else "FAIL (>50)"
        target_status = "PASS" if count <= 40 else "WARN (>40)"
        print(f"  {name} (L{start}-L{end}): {count} lines [Rule M3: {status}, Target: {target_status}]")
        if count > 50:
            m3_passed = False

print("\n" + "="*60)
print(f"Rule C2 (<= 300 lines): {'PASS' if c2_passed else 'FAIL'}")
print(f"Rule M3 (<= 50 lines):  {'PASS' if m3_passed else 'FAIL'}")
print(f"Rule Q-B1 (0 comments): {'PASS' if qb1_passed else 'FAIL'}")
print(f"Rule Q-B2 (catch docs): {'PASS' if qb2_passed else 'FAIL'}")
