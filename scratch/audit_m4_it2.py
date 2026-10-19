import os
import re
import sys

target_files = [
    r"HPDynamo\tools\harness\run-benchmark.ps1",
    r"plans\261009-1118-hpdynamo-mcp-performance\reports\baseline-vs-upgraded.json",
    r".agents\skills\hp-mcp-dynamo\SKILL.md",
    r"HPDynamo\HPDynamo.E2E.Tests\Contracts\DynamoE2EContracts.cs",
    r"HPDynamo\HPDynamo.E2E.Tests\Tiers\Tier1_ContractsAndServerTests.cs",
    r"HPDynamo\HPDynamo.E2E.Tests\Tiers\Tier3_CrossFeatureInteractionTests.cs",
    r"HPDynamo\HPDynamo.E2E.Tests\Tiers\Tier5_SeedParameterFuzzingTests.cs",
]

print("=== CHECK 1: File existence and line counts ===")
all_files_ok = True
for rel_path in target_files:
    if not os.path.exists(rel_path):
        print(f"FAIL: File does not exist: {rel_path}")
        all_files_ok = False
        continue
    with open(rel_path, "r", encoding="utf-8", errors="replace") as f:
        lines = f.readlines()
    line_count = len(lines)
    status = "PASS" if line_count <= 300 else "FAIL (>300 lines)"
    if line_count > 300:
        all_files_ok = False
    print(f"[{status}] {rel_path} -> {line_count} lines")

print("\n=== CHECK 2: Code quality (Q-B1 dead code, Q-B2 catch) in C# target files ===")
csharp_files = [f for f in target_files if f.endswith(".cs")]

for rel_path in csharp_files:
    with open(rel_path, "r", encoding="utf-8") as f:
        content = f.read()
        lines = content.splitlines()

    print(f"\nAnalyzing {rel_path}:")
    
    # Check for empty catch
    catch_matches = re.finditer(r'catch\s*(\([^)]*\))?\s*\{([^}]*)\}', content, re.DOTALL)
    for m in catch_matches:
        body = m.group(2).strip()
        if not body:
            print(f"  FAIL Q-B2: Empty catch block without comment in {rel_path}")
            all_files_ok = False
        elif '//' not in body and '/*' not in body and len(body.splitlines()) <= 1 and 'throw' not in body and 'log' not in body and 'issues.Add' not in body and 'Assert' not in body:
            print(f"  WARNING catch block: {m.group(0)}")

    # Check for commented-out C# code
    # Heuristics: lines starting with // followed by typical C# statements like var, Assert, if, for, return, int, string, await
    commented_code_lines = []
    for idx, line in enumerate(lines, 1):
        stripped = line.strip()
        if stripped.startswith("//") and not stripped.startswith("///"):
            comment_text = stripped[2:].strip()
            # check if it looks like code
            if re.match(r'^(var |int |string |double |bool |if\s*\(|for\s*\(|while\s*\(|return |Assert\.|throw |await |public |private |protected |new )', comment_text):
                # Exclude obvious documentation sentences
                if not any(w in comment_text.lower() for w in ["step", "note", "seed:", "check", "verify", "1.", "2.", "3.", "tier"]):
                    commented_code_lines.append((idx, line))

    if commented_code_lines:
        print(f"  POTENTIAL Q-B1 commented code in {rel_path}:")
        for lno, ltext in commented_code_lines:
            print(f"    Line {lno}: {ltext}")
    else:
        print(f"  PASS Q-B1: No commented-out code detected.")

print("\n=== CHECK 3: Method line counts in C# target files ===")
def analyze_methods(file_path):
    with open(file_path, "r", encoding="utf-8") as f:
        code = f.read()

    # Simple brace matcher for methods
    lines = code.splitlines()
    method_regex = re.compile(r'^\s*(?:public|private|protected|internal|static|async|sealed|\s)+[\w<>\[\],\s\?]+\s+(\w+)\s*\([^)]*\)\s*(?:where[^{]+)?\s*\{')
    
    long_methods = []
    for i, line in enumerate(lines):
        m = method_regex.match(line)
        if m:
            method_name = m.group(1)
            # Find matching closing brace
            open_braces = 0
            start_line = i + 1
            end_line = start_line
            for j in range(i, len(lines)):
                open_braces += lines[j].count('{')
                open_braces -= lines[j].count('}')
                if open_braces == 0 and '{' in ''.join(lines[i:j+1]):
                    end_line = j + 1
                    break
            span = end_line - start_line + 1
            if span > 50:
                long_methods.append((method_name, start_line, end_line, span))
    return long_methods

for rel_path in csharp_files:
    long_m = analyze_methods(rel_path)
    if long_m:
        print(f"FAIL M3 in {rel_path}:")
        for mname, s, e, span in long_m:
            print(f"  Method {mname} ({s}-{e}): {span} lines (>50)")
            all_files_ok = False
    else:
        print(f"PASS M3: All methods <= 50 lines in {rel_path}")

print(f"\nFinal Analysis Status: {'ALL CLEAN' if all_files_ok else 'VIOLATIONS FOUND'}")
