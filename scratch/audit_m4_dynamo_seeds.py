import os
import re
import json
import sys

SEEDS_ROOT = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo\HPDynamo.Mcp.Server\Registry\SeedLibrary"

SEED_LIST = [
    ("CodeBlock", "create_code_block_node"),
    ("Data", "get_node_output_values"),
    ("Export", "export_graph_to_image"),
    ("Graph", "run_dyn_file"),
    ("Graph", "validate_dyn_file"),
    ("Library", "inspect_node_type"),
    ("Library", "search_nodes_library"),
    ("Node", "add_node_to_workspace"),
    ("Node", "set_node_properties"),
    ("Wire", "connect_node_ports"),
    ("Wire", "disconnect_node_ports"),
    ("Workspace", "get_active_graph_info"),
]

FORBIDDEN_NS = [
    "System.Net",
    "System.Diagnostics.Process",
    "System.Reflection",
    "System.Runtime.InteropServices",
    "System.Runtime.Loader",
    "System.Threading.Tasks",
    "System.Security",
    "Microsoft.CodeAnalysis",
    "Microsoft.Win32",
    "System.Linq.Expressions",
]

def check_script(cat, name):
    seed_dir = os.path.join(SEEDS_ROOT, cat, name)
    tool_file = os.path.join(seed_dir, "tool.json")
    code_file = os.path.join(seed_dir, "code.cs")
    ex_file = os.path.join(seed_dir, "examples.json")

    results = {
        "seed": f"{cat}/{name}",
        "exists_tool": os.path.exists(tool_file),
        "exists_code": os.path.exists(code_file),
        "exists_examples": os.path.exists(ex_file),
        "line_count": 0,
        "max_block_lines": 0,
        "empty_catches": 0,
        "commented_code": 0,
        "forbidden_ns_violations": [],
        "json_valid": True,
        "issues": []
    }

    if not (results["exists_tool"] and results["exists_code"] and results["exists_examples"]):
        results["issues"].append("Missing triad file(s)")
        return results

    # Verify tool.json
    try:
        with open(tool_file, 'r', encoding='utf-8') as f:
            tool_data = json.load(f)
        if tool_data.get("name") != name:
            results["issues"].append(f"tool.json name mismatch: {tool_data.get('name')} != {name}")
        if "inputSchema" not in tool_data:
            results["issues"].append("tool.json missing inputSchema")
    except Exception as e:
        results["json_valid"] = False
        results["issues"].append(f"Invalid tool.json: {e}")

    # Verify examples.json
    try:
        with open(ex_file, 'r', encoding='utf-8') as f:
            ex_data = json.load(f)
        if not isinstance(ex_data, list) or len(ex_data) == 0:
            results["issues"].append("examples.json is not a non-empty array")
    except Exception as e:
        results["json_valid"] = False
        results["issues"].append(f"Invalid examples.json: {e}")

    # Verify code.cs
    with open(code_file, 'r', encoding='utf-8') as f:
        code_lines = f.readlines()

    results["line_count"] = len(code_lines)
    code_text = "".join(code_lines)

    # 1. Line count <= 300
    if len(code_lines) > 300:
        results["issues"].append(f"Line count {len(code_lines)} > 300")

    # 2. Check forbidden namespaces
    for ns in FORBIDDEN_NS:
        if ns in code_text:
            results["forbidden_ns_violations"].append(ns)
            results["issues"].append(f"Forbidden namespace referenced: {ns}")

    # Check System.IO (only System.IO.Path is allowed)
    io_matches = re.findall(r'System\.IO(?:\.([A-Za-z0-9_]+))?', code_text)
    for m in io_matches:
        if m != "Path" and m != "":
            results["forbidden_ns_violations"].append(f"System.IO.{m}")
            results["issues"].append(f"Forbidden System.IO member: System.IO.{m}")

    # 3. Check empty catch blocks
    catch_matches = re.finditer(r'catch\s*\([^\)]*\)\s*\{([^}]*)\}', code_text, re.DOTALL)
    for cm in catch_matches:
        body = cm.group(1)
        # Check if empty or only whitespace
        stripped = re.sub(r'//.*', '', body)
        stripped = re.sub(r'/\*.*?\*/', '', stripped, flags=re.DOTALL).strip()
        if not stripped and not ('//' in body or '/*' in body):
            results["empty_catches"] += 1
            results["issues"].append("Unannotated empty catch block found")

    # 4. Check block line spans
    # Simple brace depth tracker
    stack = []
    max_b = 0
    for idx, line in enumerate(code_lines):
        line_num = idx + 1
        # Strip string literals and comments for brace counting
        l_clean = re.sub(r'".*?"', '""', line)
        l_clean = re.sub(r'//.*', '', l_clean)
        for char in l_clean:
            if char == '{':
                stack.append(line_num)
            elif char == '}':
                if stack:
                    start_l = stack.pop()
                    span = line_num - start_l + 1
                    if span > max_b:
                        max_b = span
                    if span > 50:
                        results["issues"].append(f"Block span {span} lines (L{start_l}-L{line_num}) > 50 lines")

    results["max_block_lines"] = max_b

    # 5. Check commented out code
    for idx, line in enumerate(code_lines):
        l_str = line.strip()
        if l_str.startswith("//"):
            # Check if looks like code
            comment_content = l_str[2:].strip()
            # If comment ends with semicolon or has braces/assignments
            if re.match(r'^(var|if|for|foreach|return|new|int|string|bool)\b', comment_content) or comment_content.endswith(";"):
                results["commented_code"] += 1
                results["issues"].append(f"Suspected commented-out code at line {idx+1}: {l_str}")

    return results

print(f"{'Seed':<35} | {'Lines':<6} | {'MaxBlock':<8} | {'EmptyCatch':<10} | {'Forbidden':<10} | {'Status':<10}")
print("-" * 95)

all_clean = True
for cat, name in SEED_LIST:
    res = check_script(cat, name)
    status = "CLEAN" if len(res["issues"]) == 0 else "FAIL"
    if status == "FAIL":
        all_clean = False
    print(f"{res['seed']:<35} | {res['line_count']:<6} | {res['max_block_lines']:<8} | {res['empty_catches']:<10} | {len(res['forbidden_ns_violations']):<10} | {status:<10}")
    if res["issues"]:
        for iss in res["issues"]:
            print(f"   -> {iss}")

print("-" * 95)
if all_clean:
    print("VERDICT: 100% CLEAN — All 12 seeds satisfy all Clean Code and Authenticity rules!")
    sys.exit(0)
else:
    print("VERDICT: INTEGRITY VIOLATION DETECTED")
    sys.exit(1)
