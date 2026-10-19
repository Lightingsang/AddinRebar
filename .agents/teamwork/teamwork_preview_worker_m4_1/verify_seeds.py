import os
import json
import re

seeds_dir = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo\HPDynamo.Mcp.Server\Registry\SeedLibrary"
assert os.path.exists(seeds_dir), "SeedLibrary folder not found"

tools = []
for root, dirs, files in os.walk(seeds_dir):
    if "tool.json" in files and "code.cs" in files and "examples.json" in files:
        tools.append(root)

print(f"Found {len(tools)} seed tools.")
assert len(tools) == 12, f"Expected 12 tools, got {len(tools)}"

all_clean = True
for t in tools:
    rel = os.path.relpath(t, seeds_dir).replace("\\", "/")
    print(f"\n--- Checking {rel} ---")
    
    with open(os.path.join(t, "tool.json"), encoding="utf-8") as f:
        tj = json.load(f)
    with open(os.path.join(t, "examples.json"), encoding="utf-8") as f:
        ex = json.load(f)
    with open(os.path.join(t, "code.cs"), encoding="utf-8") as f:
        code = f.read()

    lines = len([l for l in code.strip().splitlines() if l.strip()])
    print(f"  Script line count: {lines} lines")
    if lines > 300:
        print(f"  ERROR: Lines > 300 in {rel}")
        all_clean = False

    if not isinstance(ex, list) or len(ex) < 2:
        print(f"  ERROR: examples.json must have >= 2 examples in {rel}")
        all_clean = False

    props = set(tj.get("inputSchema", {}).get("properties", {}).keys())
    
    args_calls = set(re.findall(r'args\.\w+\(\s*"([^"]+)"', code))
    has_calls = set(re.findall(r'args\.Has\(\s*"([^"]+)"', code))
    total_args = args_calls.union(has_calls)
    
    print(f"  Declared properties: {sorted(list(props))}")
    print(f"  Accessed args:       {sorted(list(total_args))}")
    
    missing_in_code = props - total_args
    if missing_in_code:
        print(f"  ERROR: Schema declares properties not read in code: {missing_in_code}")
        all_clean = False
        
    undeclared_in_schema = total_args - props
    if undeclared_in_schema:
        print(f"  ERROR: Code reads args not declared in schema: {undeclared_in_schema}")
        all_clean = False

    for forbidden in ["System.IO.File", "System.Net", "System.Diagnostics.Process", "System.Reflection", "TaskDialog", "MessageBox"]:
        if forbidden in code:
            print(f"  ERROR: Forbidden token {forbidden} found in {rel}")
            all_clean = False

print("\n" + ("=" * 40))
if all_clean:
    print("ALL 12 SEEDS VERIFIED WITH ZERO ERRORS AND 100% ARGUMENT PARITY!")
else:
    print("VERIFICATION FAILED WITH ISSUES!")
