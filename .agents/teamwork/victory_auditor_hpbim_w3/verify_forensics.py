import os
import re
import sys

TOUCHED_FILES = [
    r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\src\HPBim.Client\Connection\CrossProcessFileLock.cs",
    r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\src\HPBim.Client\Connection\IntentJournal.cs",
    r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\src\HPBim.Client\Connection\IntentJournal.Storage.cs",
    r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\src\HPBim.Client\Recovery\ReconcileService.cs",
    r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\src\HPBim.Client\Recovery\ReconcileService.DocSnapshot.cs",
    r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\src\HPBim.Core\Models\DocSnapshot.cs",
    r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\src\HPBim.Core\Coordination\CompositeSourceKeyJsonConverter.cs",
    r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\src\HPBim.Client\Pipeline\BimPipelineEngine.ElementRecords.cs",
    r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\src\HPBim.Client\Pipeline\BimPipelineEngine.ElementRecords.Validation.cs",
    r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\src\HPBim.Client\Pipeline\BimPipelineEngine.PlanExecution.cs",
    r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\src\HPBim.Core\Verification\MeasuredGeometricEngine.cs",
    r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\src\HPBim.Core\Verification\MeasuredGeometricEngine.Structural.cs",
    r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\src\HPBim.Core\Verification\MeasuredGeometricEngine.ProfileGrid.cs",
    r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\tests\HPBim.Core.Tests\Verification\StandardizedGeometricMeasurementR5Tests.cs",
]

def check_file_lengths():
    print("=== Rule C2: File Lengths (<= 300 lines) ===")
    violations = []
    for path in TOUCHED_FILES:
        if not os.path.exists(path):
            print(f"ERROR: File not found: {path}")
            violations.append((path, "File not found"))
            continue
        with open(path, "r", encoding="utf-8") as f:
            lines = f.readlines()
        line_count = len(lines)
        status = "PASS" if line_count <= 300 else "FAIL"
        print(f"[{status}] {os.path.basename(path)}: {line_count} lines")
        if line_count > 300:
            violations.append((path, f"{line_count} > 300"))
    return violations

def check_qb1_commented_code():
    print("\n=== Rule Q-B1: Commented-out code ===")
    code_patterns = [
        re.compile(r"^\s*//\s*(var|int|string|bool|double|return|if|for|foreach|while|await|throw|new|public|private|protected)\s+"),
        re.compile(r"^\s*//\s*([a-zA-Z0-9_]+\s*=\s*[^;]+;)"),
        re.compile(r"^\s*//\s*([a-zA-Z0-9_.]+\([^)]*\);)"),
    ]
    violations = []
    for path in TOUCHED_FILES:
        with open(path, "r", encoding="utf-8") as f:
            lines = f.readlines()
        for idx, line in enumerate(lines, 1):
            stripped = line.strip()
            # ignore XML doc comments
            if stripped.startswith("///"):
                continue
            for pat in code_patterns:
                if pat.match(line):
                    # Check if it's descriptive explanatory comment
                    if "e.g." in line or "example" in line.lower() or "todo" in line.lower() or "note" in line.lower():
                        continue
                    print(f"[FAIL] {os.path.basename(path)}:{idx}: {stripped}")
                    violations.append((path, idx, stripped))
    if not violations:
        print("[PASS] Zero commented-out code detected across all 14 files.")
    return violations

def check_qb2_empty_catches():
    print("\n=== Rule Q-B2: Unannotated empty catch blocks ===")
    catch_pattern = re.compile(r"catch\s*(\([^\)]*\))?\s*\{([^}]*)\}", re.DOTALL)
    violations = []
    for path in TOUCHED_FILES:
        with open(path, "r", encoding="utf-8") as f:
            content = f.read()
        for match in catch_pattern.finditer(content):
            body = match.group(2).strip()
            if not body:
                print(f"[FAIL] {os.path.basename(path)}: Empty catch without comment: {match.group(0)}")
                violations.append((path, match.group(0)))
            elif not any(c.isalnum() for c in body):
                # Only whitespace or symbols
                print(f"[FAIL] {os.path.basename(path)}: Blank catch: {match.group(0)}")
                violations.append((path, match.group(0)))
            else:
                # If body contains only comments, ensure it has explanation
                code_lines = [l.strip() for l in body.split("\n") if l.strip() and not l.strip().startswith("//")]
                if not code_lines:
                    # Catch body is only comments. Verify it explains why
                    comment_text = body.replace("//", "").strip()
                    if len(comment_text) < 5:
                        print(f"[FAIL] {os.path.basename(path)}: Empty catch with trivial comment: {match.group(0)}")
                        violations.append((path, match.group(0)))
                    else:
                        print(f"[PASS - Annotated] {os.path.basename(path)}: Catch annotated with comment: '{comment_text}'")
    if not violations:
        print("[PASS] Zero unannotated empty catch blocks detected.")
    return violations

def check_method_lengths():
    print("\n=== Rule M3: Method Lengths (<= 50 lines) ===")
    violations = []
    method_regex = re.compile(
        r'^[ \t]*(?:public|private|protected|internal|static|\s)+[ \t]+(?:async\s+)?(?:[\w<>\[\],.?]+)[ \t]+([A-Za-z0-9_]+)\s*\([^\)]*\)\s*(?:where\s+.*?)?\s*\{',
        re.MULTILINE
    )
    for path in TOUCHED_FILES:
        with open(path, "r", encoding="utf-8") as f:
            lines = f.readlines()
        content = "".join(lines)
        
        # Count braces for method bodies
        for m in method_regex.finditer(content):
            method_name = m.group(1)
            # Find start line
            start_pos = m.start()
            start_line = content[:start_pos].count("\n") + 1
            
            # Find matching closing brace
            brace_pos = m.end() - 1
            depth = 1
            i = brace_pos + 1
            while i < len(content) and depth > 0:
                if content[i] == '{':
                    depth += 1
                elif content[i] == '}':
                    depth -= 1
                elif content[i] == '"' or content[i] == "'":
                    # skip strings
                    quote = content[i]
                    i += 1
                    while i < len(content) and content[i] != quote:
                        if content[i] == '\\':
                            i += 1
                        i += 1
                i += 1
            end_line = content[:i].count("\n") + 1
            method_length = end_line - start_line + 1
            
            if method_length > 50:
                print(f"[FAIL] {os.path.basename(path)}::{method_name} L{start_line}-L{end_line}: {method_length} lines (> 50)")
                violations.append((path, method_name, method_length, start_line, end_line))
            else:
                # print(f"[PASS] {os.path.basename(path)}::{method_name}: {method_length} lines")
                pass
                
    if not violations:
        print("[PASS] All methods across all 14 touched files are <= 50 lines.")
    return violations

if __name__ == "__main__":
    v_c2 = check_file_lengths()
    v_qb1 = check_qb1_commented_code()
    v_qb2 = check_qb2_empty_catches()
    v_m3 = check_method_lengths()
    
    total_violations = len(v_c2) + len(v_qb1) + len(v_qb2) + len(v_m3)
    print(f"\n======================================")
    print(f"Total Violations: {total_violations}")
    if total_violations > 0:
        sys.exit(1)
    else:
        print("ALL STATIC QUALITY & CLEAN CODE CHECKS PASSED!")
