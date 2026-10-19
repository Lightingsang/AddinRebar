import os
import re
import sys

FILES_TO_CHECK = [
    r"HPGrasshopper\HPGrasshopper.McpBridge\Tools\CreatedComponentInfo.cs",
    r"HPGrasshopper\HPGrasshopper.McpBridge\Tools\CanvasBatchValidator.cs",
    r"HPGrasshopper\HPGrasshopper.McpBridge\Tools\CanvasEditor.cs",
    r"HPGrasshopper\HPGrasshopper.McpBridge\Tools\PlayerInputFailure.cs",
    r"HPGrasshopper\HPGrasshopper.McpBridge\Tools\PlayerOutputExtractor.cs",
    r"HPGrasshopper\HPGrasshopper.McpBridge\Tools\PlayerDataTreeHelper.cs",
    r"HPGrasshopper\HPGrasshopper.McpBridge\Tools\PlayerRunner.cs",
    r"HPGrasshopper\HPGrasshopper.Mcp.Server\Registry\SeedLibrary\Revit\generate_rebar_curves\code.cs",
    r"HPGrasshopper\HPGrasshopper.McpBridge\Coordinator\GrasshopperCompilerReferences.cs",
    r"HPGrasshopper\HPGrasshopper.McpBridge\Coordinator\GrasshopperCompilerLifecycle.cs",
    r"HPGrasshopper\HPGrasshopper.McpBridge\Coordinator\CoordinatorWorkQueue.cs",
    r"HPGrasshopper\HPGrasshopper.McpBridge\Coordinator\GrasshopperBridgeCoordinator.cs",
    r"HPGrasshopper\HPGrasshopper.McpBridge\Coordinator\GrasshopperContextExtractor.cs",
    r"HPGrasshopper\HPGrasshopper.McpBridge.Tests\tools\FindingR6_PreflightAndAtomicTests.cs",
    r"HPGrasshopper\HPGrasshopper.McpBridge.Tests\tools\FindingR7_PlayerDataTreeAndErrorsTests.cs",
    r"HPGrasshopper\HPGrasshopper.McpBridge.Tests\Coordinator\FindingR8_R9_SeedsAndLifecycleTests.cs",
    r"HPGrasshopper\HPGrasshopper.Mcp.Server.Tests\Seeds\SeedSchemaValidationTests.cs",
]

ROOT = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar"

def check_file(rel_path):
    full_path = os.path.join(ROOT, rel_path)
    if not os.path.exists(full_path):
        return [f"FILE NOT FOUND: {rel_path}"]
    
    with open(full_path, "r", encoding="utf-8") as f:
        lines = f.readlines()
        
    findings = []
    
    # Check Rule C2: Line count <= 300
    total_lines = len(lines)
    if total_lines > 300:
        findings.append(f"Rule C2 VIOLATION: {rel_path} has {total_lines} lines (> 300)")
        
    # Check Rule Q-B1: Commented-out code
    # Common commented-out code patterns in C#
    code_patterns = [
        re.compile(r"^\s*//\s*(var|int|string|bool|double|float|return|if|for|foreach|while|try|catch|throw|using|new\s+[A-Z]|class|public|private|protected|internal)\s+"),
        re.compile(r"^\s*//\s*[a-zA-Z0-9_.]+\s*\([^)]*\)\s*;"),
        re.compile(r"^\s*//\s*[a-zA-Z0-9_.]+\s*=\s*[^;]+;"),
    ]
    for idx, line in enumerate(lines, 1):
        # Ignore comments that look like explanations or multi-version tags
        if "Multi-version:" in line or "TODO" in line or "XML" in line or "Summary" in line:
            continue
        for pat in code_patterns:
            if pat.search(line):
                findings.append(f"Rule Q-B1 VIOLATION: Line {idx} in {rel_path}: commented-out code: {line.strip()}")
                break

    # Check Rule Q-B2: Empty catch blocks without reason comment
    # Pattern: catch (...) { } with no non-whitespace or comments inside
    full_text = "".join(lines)
    # Simple regex for empty catch blocks
    empty_catch_pat = re.compile(r"catch\s*(\([^)]*\))?\s*\{\s*\}", re.MULTILINE)
    for m in empty_catch_pat.finditer(full_text):
        # find line number
        line_num = full_text[:m.start()].count("\n") + 1
        findings.append(f"Rule Q-B2 VIOLATION: Line {line_num} in {rel_path}: empty catch block: {m.group(0)}")

    # Check Rule M3: Method length <= 50 lines
    # Rough block-based method detection
    # Find methods by looking for signature followed by opening brace
    brace_depth = 0
    in_method = False
    method_name = ""
    method_start_line = 0
    method_brace_start_depth = 0
    
    method_sig_pattern = re.compile(
        r"^\s*(?:public|private|protected|internal|static|virtual|override|async|sealed|\s)+\s+"
        r"(?:(?:[a-zA-Z0-9_<>,\[\]\?]+)\s+)+([a-zA-Z0-9_]+)\s*\([^;]*\)\s*(?:where\s+.*)?$"
    )
    
    for idx, line in enumerate(lines, 1):
        stripped = line.strip()
        
        # Check method start if not in method (brace_depth == 1 for class, depth == 2 for nested class)
        if not in_method and brace_depth >= 1:
            match = method_sig_pattern.match(stripped)
            if match and not stripped.endswith(";"):
                potential_name = match.group(1)
                if potential_name not in ("class", "interface", "record", "struct", "enum"):
                    method_name = potential_name
                    method_start_line = idx
        
        # Count braces
        # Note: ignore braces inside string literals and char literals for rough check
        # A simple scanner:
        in_str = False
        in_char = False
        in_line_comment = False
        for c_idx, c in enumerate(line):
            if in_line_comment:
                break
            if c == '/' and c_idx + 1 < len(line) and line[c_idx+1] == '/':
                in_line_comment = True
                break
            if c == '"' and not in_char:
                if c_idx > 0 and line[c_idx-1] == '\\':
                    pass
                else:
                    in_str = not in_str
            elif c == "'" and not in_str:
                if c_idx > 0 and line[c_idx-1] == '\\':
                    pass
                else:
                    in_char = not in_char
            elif not in_str and not in_char:
                if c == '{':
                    brace_depth += 1
                    if method_name and not in_method:
                        in_method = True
                        method_brace_start_depth = brace_depth
                elif c == '}':
                    if in_method and brace_depth == method_brace_start_depth:
                        # Method ended
                        method_len = idx - method_start_line + 1
                        if method_len > 50:
                            findings.append(f"Rule M3 VIOLATION: Method '{method_name}' in {rel_path} lines {method_start_line}-{idx} has {method_len} lines (> 50)")
                        in_method = False
                        method_name = ""
                    brace_depth -= 1

    return total_lines, findings

total_all = 0
all_findings = []
for p in FILES_TO_CHECK:
    cnt, f = check_file(p)
    print(f"{p}: {cnt} lines")
    if f:
        all_findings.extend(f)

if all_findings:
    print("\nFINDINGS:")
    for f in all_findings:
        print("  - " + f)
else:
    print("\nALL FILES PASSED CLEAN CODE (C2, M3, Q-B1, Q-B2) CHECKS WITH ZERO VIOLATIONS!")
