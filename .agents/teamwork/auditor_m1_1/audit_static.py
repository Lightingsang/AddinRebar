import os
import re
import sys

FILES_TO_CHECK = [
    r"McpShared/HPRebar.Mcp.Contracts/HostScriptContracts.cs",
    r"McpShared/HPRebar.Mcp.Contracts/JsonRpc/JsonRpcMethods.cs",
    r"McpShared/HPRebar.Mcp.Contracts/Messages/ContextMessages.cs",
    r"McpShared/HPRebar.Mcp.Contracts/PipeNaming.cs",
    r"McpShared/HPRebar.Mcp.Server.Core/Hosts/HostProfile.cs",
    r"McpShared/HPRebar.McpBridge.Core/Scripting/AnalyzerProfile.cs",
    r"McpShared/HPRebar.McpBridge.Core/Scripting/GuardProfile.cs",
    r"McpShared/tools/live-verify-quality.py",
    r"McpShared/HPRebar.Mcp.Server.Core.Tests/Hosts/GrasshopperTestProfile.cs",
    r"McpShared/HPRebar.Mcp.Server.Core.Tests/Hosts/GrasshopperProfileTests.cs",
    r"McpShared/HPRebar.Mcp.Server.Core.Tests/HostNeutralityTests.cs",
]

def check_file_line_counts(repo_root):
    violations = []
    print("--- 1. File Line Count Check (C2: <= 300 lines) ---")
    for rel_path in FILES_TO_CHECK:
        full_path = os.path.join(repo_root, rel_path)
        with open(full_path, "r", encoding="utf-8", errors="replace") as f:
            lines = f.readlines()
        count = len(lines)
        status = "PASS" if count <= 300 else "FAIL"
        print(f"[{status}] {rel_path}: {count} lines")
        if count > 300:
            violations.append(f"C2 violation: {rel_path} has {count} lines (> 300)")
    return violations

def check_method_lengths(repo_root):
    violations = []
    print("\n--- 2. Method Length Check (M3: <= 50 lines) ---")
    for rel_path in FILES_TO_CHECK:
        if not rel_path.endswith(".cs"):
            continue
        full_path = os.path.join(repo_root, rel_path)
        with open(full_path, "r", encoding="utf-8", errors="replace") as f:
            content = f.read()
            lines = content.splitlines()

        # Simple brace matching tracker for method-like blocks
        # We can look for method signatures followed by {
        # Or parse block depth
        i = 0
        while i < len(lines):
            line = lines[i].strip()
            # Look for method-like signature
            # e.g. [Fact], public void Foo(), public static HostProfile Grasshopper(...) =>, etc.
            method_match = re.search(r'\b(public|private|internal|protected)\s+(static\s+|async\s+|override\s+|virtual\s+)*([\w<>?,\[\]]+)\s+([A-Za-z0-9_]+)\s*(\([^)]*\))\s*(\{)?', lines[i])
            if method_match:
                method_name = method_match.group(4)
                start_line = i + 1
                has_open_brace = method_match.group(6) is not None or "{" in lines[i]
                
                # Check expression-bodied method
                if "=>" in lines[i] and not has_open_brace:
                    # expression-bodied, find semicolon
                    end_line = start_line
                    while end_line <= len(lines) and ";" not in lines[end_line - 1]:
                        end_line += 1
                    length = end_line - start_line + 1
                    if length > 50:
                        violations.append(f"M3 violation in {rel_path}: method {method_name} at line {start_line} is {length} lines (> 50)")
                    i = end_line
                    continue

                # Block method
                brace_line = i
                if not has_open_brace:
                    while brace_line < len(lines) and "{" not in lines[brace_line]:
                        brace_line += 1
                if brace_line < len(lines):
                    depth = 0
                    started = False
                    end_line = brace_line + 1
                    for cur in range(brace_line, len(lines)):
                        l_text = lines[cur]
                        # count braces ignoring strings/comments (simplified)
                        # remove string literals
                        clean = re.sub(r'"(\\.|[^"\\])*"', '""', l_text)
                        clean = re.sub(r'//.*', '', clean)
                        for ch in clean:
                            if ch == '{':
                                depth += 1
                                started = True
                            elif ch == '}':
                                depth -= 1
                        if started and depth == 0:
                            end_line = cur + 1
                            break
                    length = end_line - start_line + 1
                    if length > 50:
                        violations.append(f"M3 violation in {rel_path}: method {method_name} at line {start_line} is {length} lines (> 50)")
                        print(f"  [FAIL] {rel_path}:{start_line} {method_name}(): {length} lines")
                    else:
                        pass
            i += 1
    if not violations:
        print("All methods in touched files are <= 50 lines. PASS!")
    return violations

def check_qb1_dead_code(repo_root):
    violations = []
    print("\n--- 3. Q-B1: Commented-out Code Check ---")
    code_patterns = [
        re.compile(r'^\s*//\s*(var|int|string|bool|double|if\s*\(|for\s*\(|foreach\s*\(|return\s+|new\s+|using\s*\(|Assert\.)'),
        re.compile(r'/\*.*(var|return|Assert).*?\*/', re.DOTALL)
    ]
    for rel_path in FILES_TO_CHECK:
        if not rel_path.endswith(".cs"):
            continue
        full_path = os.path.join(repo_root, rel_path)
        with open(full_path, "r", encoding="utf-8", errors="replace") as f:
            lines = f.readlines()
        for idx, line in enumerate(lines, 1):
            for pat in code_patterns:
                if pat.search(line):
                    # Check if it's an example in xmldoc or prose comment
                    stripped = line.strip()
                    if "<example>" in line or "///" in line or "e.g." in line.lower() or "c>" in line:
                        continue
                    violations.append(f"Q-B1 suspicious commented code in {rel_path}:{idx}: {stripped}")
                    print(f"  [FLAG] {rel_path}:{idx}: {stripped}")
    if not violations:
        print("No commented-out code detected. PASS!")
    return violations

def check_qb2_empty_catch(repo_root):
    violations = []
    print("\n--- 4. Q-B2: Empty Catch Blocks Without Comment Check ---")
    catch_pattern = re.compile(r'catch(\s*\([^\)]*\))?\s*\{([^}]*)\}', re.MULTILINE)
    for rel_path in FILES_TO_CHECK:
        if not rel_path.endswith(".cs"):
            continue
        full_path = os.path.join(repo_root, rel_path)
        with open(full_path, "r", encoding="utf-8", errors="replace") as f:
            content = f.read()
        for m in catch_pattern.finditer(content):
            body = m.group(2).strip()
            if not body:
                violations.append(f"Q-B2 empty catch in {rel_path}: {m.group(0)}")
                print(f"  [FAIL] {rel_path}: empty catch without comment")
            elif not any(c in body for c in ["//", "/*"]) and len(body) == 0:
                violations.append(f"Q-B2 empty catch in {rel_path}: {m.group(0)}")
                print(f"  [FAIL] {rel_path}: empty catch without explanation comment")
    if not violations:
        print("No unannotated empty catch blocks detected. PASS!")
    return violations

def check_host_neutrality(repo_root):
    violations = []
    print("\n--- 5. Host Neutrality Check (No Rhino/Grasshopper binary references) ---")
    forbidden = ["rhinocommon", "grasshopper", "rhinoinside"]
    for root, dirs, files in os.walk(os.path.join(repo_root, "McpShared")):
        for f in files:
            if f.endswith((".csproj", ".props", ".targets")):
                fpath = os.path.join(root, f)
                with open(fpath, "r", encoding="utf-8", errors="replace") as proj:
                    txt = proj.read().lower()
                for forb in forbidden:
                    if forb in txt:
                        violations.append(f"Host neutrality violation in {fpath}: references '{forb}'")
                        print(f"  [FAIL] {fpath}: contains '{forb}'")
    if not violations:
        print("Strict host neutrality verified across all McpShared project files. PASS!")
    return violations

if __name__ == "__main__":
    repo_root = sys.argv[1] if len(sys.argv) > 1 else "."
    all_violations = []
    all_violations += check_file_line_counts(repo_root)
    all_violations += check_method_lengths(repo_root)
    all_violations += check_qb1_dead_code(repo_root)
    all_violations += check_qb2_empty_catch(repo_root)
    all_violations += check_host_neutrality(repo_root)
    
    print("\n" + "="*50)
    if all_violations:
        print(f"STATIC AUDIT FAILED with {len(all_violations)} violations:")
        for v in all_violations:
            print("  - " + v)
        sys.exit(1)
    else:
        print("STATIC AUDIT CLEAN: All 5 integrity checks passed!")
        sys.exit(0)
