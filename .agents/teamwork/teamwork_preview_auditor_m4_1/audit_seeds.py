import os
import json
import re

seed_root = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo\HPDynamo.Mcp.Server\Registry\SeedLibrary"

forbidden_patterns = [
    r"System\.IO\.File\b",
    r"System\.IO\.Directory\b",
    r"System\.IO\.FileInfo\b",
    r"System\.IO\.DirectoryInfo\b",
    r"System\.Net\b",
    r"System\.Diagnostics\.Process\b",
    r"System\.Reflection\b",
    r"\bTaskDialog\b",
    r"\bMessageBox\b",
    r"DllImport\b",
]

commented_code_patterns = [
    r"^\s*//\s*(var|int|string|bool|return|if|for|foreach|while|class|public|private|throw|new)\s+[a-zA-Z0-9_]+",
    r"^\s*//\s*[a-zA-Z0-9_.]+\s*\(.*\)\s*;",
]

vague_variable_names = [
    r"\bvar\s+(data|tmp|temp|obj|res|val|foo|bar|stuff|thing)\b",
    r"\bint\s+(data|tmp|temp|obj|res|val|foo|bar|stuff|thing)\b",
    r"\bstring\s+(data|tmp|temp|obj|res|val|foo|bar|stuff|thing)\b",
    r"\bbool\s+(data|tmp|temp|obj|res|val|foo|bar|stuff|thing)\b",
]

results = []

for root, subdirs, files in os.walk(seed_root):
    if "tool.json" not in files:
        continue
    rel = os.path.relpath(root, seed_root)
    tool_name = os.path.basename(root)

    # 1. tool.json
    tool_path = os.path.join(root, "tool.json")
    with open(tool_path, "r", encoding="utf-8") as f:
        try:
            tool_json = json.load(f)
            tool_json_valid = True
        except Exception as e:
            tool_json = {}
            tool_json_valid = False
            tool_json_err = str(e)

    # 2. examples.json
    ex_path = os.path.join(root, "examples.json")
    with open(ex_path, "r", encoding="utf-8") as f:
        try:
            ex_json = json.load(f)
            ex_json_valid = True
        except Exception as e:
            ex_json = []
            ex_json_valid = False
            ex_json_err = str(e)

    # 3. code.cs
    code_path = os.path.join(root, "code.cs")
    with open(code_path, "r", encoding="utf-8") as f:
        code_lines = f.readlines()

    line_count = len(code_lines)
    code_text = "".join(code_lines)

    # Check forbidden tokens
    forbidden_matches = []
    for pat in forbidden_patterns:
        m = re.findall(pat, code_text)
        if m:
            forbidden_matches.extend(m)

    # Check commented code
    commented_code = []
    for idx, l in enumerate(code_lines, 1):
        for cp in commented_code_patterns:
            if re.search(cp, l):
                commented_code.append((idx, l.strip()))

    # Check vague variable names
    vague_vars = []
    for idx, l in enumerate(code_lines, 1):
        for vp in vague_variable_names:
            if re.search(vp, l):
                vague_vars.append((idx, l.strip()))

    # Check unannotated empty catch
    empty_catches = []
    for idx, l in enumerate(code_lines, 1):
        if "catch" in l:
            window = "".join(code_lines[idx-1:min(len(code_lines), idx+4)])
            if re.search(r"catch\s*\([^\)]*\)\s*\{\s*\}", window) or re.search(r"catch\s*\{\s*\}", window):
                empty_catches.append((idx, l.strip()))

    # Check block length by brace tracking
    max_block = 0
    stack = []
    for idx, l in enumerate(code_lines, 1):
        for ch in l:
            if ch == '{':
                stack.append(idx)
            elif ch == '}' and stack:
                start = stack.pop()
                length = idx - start + 1
                if length > max_block:
                    max_block = length

    # Argument parity
    declared_args = set(tool_json.get("inputSchema", {}).get("properties", {}).keys())
    used_args = set()
    for m in re.finditer(r'args\.(?:Require|Str|Bool|Int|Double|RequireDouble|DoubleOrNull|Obj|Strings|Has)\(\s*"([^"]+)"', code_text):
        used_args.add(m.group(1))

    # Facade check
    has_dynamo_api = any(token in code_text for token in [
        "Dynamo", "Workspace", "Node", "Connector", "InPorts", "OutPorts",
        "SearchModel", "CodeBlock", "ExecuteCommand", "UpdateValue", "RecordModelForModification"
    ])

    results.append({
        "dir": rel,
        "name": tool_name,
        "line_count": line_count,
        "max_block": max_block,
        "tool_json_valid": tool_json_valid,
        "ex_json_valid": ex_json_valid,
        "forbidden_matches": forbidden_matches,
        "commented_code": commented_code,
        "vague_vars": vague_vars,
        "empty_catches": empty_catches,
        "declared_args": sorted(list(declared_args)),
        "used_args": sorted(list(used_args)),
        "has_dynamo_api": has_dynamo_api,
    })

print("AUDIT RESULTS SUMMARY:")
all_clean = True
for r in results:
    print("=" * 70)
    print(f"Tool: {r['name']} ({r['dir']})")
    print(f"  Lines: {r['line_count']} (<=300: {r['line_count'] <= 300})")
    print(f"  Max block: {r['max_block']} (<=50: {r['max_block'] <= 50})")
    print(f"  tool.json valid: {r['tool_json_valid']}, examples.json valid: {r['ex_json_valid']}")
    print(f"  Forbidden tokens: {r['forbidden_matches']}")
    print(f"  Commented code: {r['commented_code']}")
    print(f"  Vague vars: {r['vague_vars']}")
    print(f"  Empty catches: {r['empty_catches']}")
    print(f"  Declared args: {r['declared_args']}")
    print(f"  Used args:     {r['used_args']}")
    print(f"  Uses Dynamo API: {r['has_dynamo_api']}")

    if r["line_count"] > 300 or r["max_block"] > 50 or not r["tool_json_valid"] or not r["ex_json_valid"] or r["forbidden_matches"] or r["commented_code"] or r["vague_vars"] or r["empty_catches"] or not r["has_dynamo_api"]:
        all_clean = False

print("\n" + "=" * 70)
print(f"TOTAL SEEDS CHECKED: {len(results)}")
print(f"OVERALL SEEDS STATUS: {'CLEAN' if all_clean else 'ISSUES FOUND'}")
