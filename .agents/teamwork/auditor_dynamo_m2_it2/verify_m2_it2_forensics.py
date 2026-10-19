import re
import os
import sys

files = [
    r"HPDynamo/HPDynamo.McpBridge/Execution/CanvasGraphRunner.cs",
    r"HPDynamo/HPDynamo.McpBridge/Execution/DynamoEvaluationSync.cs",
    r"HPDynamo/HPDynamo.McpBridge/Support/DynamoScriptGlobals.cs",
    r"HPDynamo/HPDynamo.Mcp.Server/Registry/SeedLibrary/Graph/run_dyn_file/code.cs",
    r"HPDynamo/HPDynamo.McpBridge/Coordinator/DynamoExecutionEngine.cs",
    r"HPDynamo/HPDynamo.McpBridge/Coordinator/DynamoBridgeCoordinator.cs",
    r"HPDynamo/HPDynamo.McpBridge.Tests/Coordinator/Milestone2Tests.cs"
]

print("=" * 80)
print("FORENSIC INTEGRITY AUDIT — MILESTONE 2 ITERATION 2")
print("=" * 80)

all_passed = True

for f in files:
    print(f"\n[FILE]: {f}")
    if not os.path.exists(f):
        print(f"  ERROR: File not found: {f}")
        all_passed = False
        continue

    with open(f, "r", encoding="utf-8") as fp:
        lines = fp.readlines()

    total_lines = len(lines)
    print(f"  Total Lines: {total_lines} (Limit <= 300)")
    if total_lines > 300:
        print(f"  --> VIOLATION: Total lines {total_lines} > 300 (Rule C2)")
        all_passed = False
    else:
        print("  --> PASS: Total lines within limit.")

    # Check for commented-out code (Rule Q-B1)
    commented_code_lines = []
    for idx, l in enumerate(lines, 1):
        s = l.strip()
        if s.startswith("//"):
            # Exclude regular comments or docstrings or multi-version tags
            content = s[2:].strip()
            # Heuristic for commented out C# statements: ends with semicolon, or variable declaration / if / for / while
            if (content.endswith(";") and not content.startswith("http") and not content.startswith("<")) or \
               re.match(r'^(var|int|string|bool|return|if\s*\(|for\s*\(|while\s*\(|throw\s+new)\b', content):
                commented_code_lines.append((idx, s))

    if commented_code_lines:
        print(f"  --> VIOLATION: Commented-out code detected ({len(commented_code_lines)} lines):")
        for line_no, text in commented_code_lines:
            print(f"      L{line_no}: {text}")
        all_passed = False
    else:
        print("  --> PASS: 0 commented-out code lines.")

    # Check for empty catches (Rule Q-B2)
    # catch (...) { } or catch { } without comment or statement
    content_full = "".join(lines)
    empty_catches = re.findall(r'catch\s*(?:\([^)]*\))?\s*\{\s*\}', content_full)
    if empty_catches:
        print(f"  --> VIOLATION: Empty catch block without comment detected: {len(empty_catches)}")
        all_passed = False
    else:
        print("  --> PASS: 0 empty catch blocks without comment.")

    # Check method lengths (Rule M3 <= 50 lines)
    print("  Methods / Functions:")
    i = 0
    method_count = 0
    while i < len(lines):
        line = lines[i].strip()
        m = re.search(r'(?:public|private|internal|protected)\s+(?:static\s+|async\s+|override\s+|sealed\s+|virtual\s+)*([\w\<\>\[\],\?\.\s]+)\s+([A-Za-z0-9_]+)\s*\(', line)
        if not m and f.endswith("code.cs"):
            m = re.search(r'^(?:void|bool|int|string|Task|[\w\<\>\[\],\?]+)\s+([A-Za-z0-9_]+)\s*\(', line)
            if m and (line.startswith("if") or line.startswith("return") or line.startswith("log")):
                m = None

        if m:
            method_name = m.group(2) if m.lastindex >= 2 else m.group(1)
            start_line = i + 1
            brace_idx = i
            while brace_idx < len(lines) and "{" not in lines[brace_idx]:
                brace_idx += 1
            if brace_idx < len(lines) and "{" in lines[brace_idx]:
                depth = 0
                end_line = -1
                for j in range(brace_idx, len(lines)):
                    depth += lines[j].count("{") - lines[j].count("}")
                    if depth == 0:
                        end_line = j + 1
                        break
                if end_line != -1:
                    method_count += 1
                    m_total = end_line - start_line + 1
                    m_body = (end_line - 1) - (brace_idx + 1) + 1
                    status = "PASS" if m_total <= 50 else ("PASS (body<=50)" if m_body <= 50 else "OVER_50")
                    print(f"    - {method_name} (L{start_line}-L{end_line}): total={m_total}, body={m_body} [{status}]")
                    if m_total > 50 and m_body > 50:
                        print(f"      --> VIOLATION: Method exceeds 50 lines (Rule M3)")
                        all_passed = False
                    i = end_line
                    continue
        i += 1
    print(f"    Total methods analyzed: {method_count}")

print("\n" + "=" * 80)
if all_passed:
    print("FINAL VERDICT: ALL STATIC CHECKS PASSED CLEANLY")
else:
    print("FINAL VERDICT: STATIC CHECKS FAILED")
print("=" * 80)
