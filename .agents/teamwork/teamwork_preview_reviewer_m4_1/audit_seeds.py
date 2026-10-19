import os
import json
import re

seed_root = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo\HPDynamo.Mcp.Server\Registry\SeedLibrary"

tools = []
for root, dirs, files in os.walk(seed_root):
    if "tool.json" in files:
        tools.append(root)

tools.sort()

print("=" * 80)
print(f"ARGUMENT PARITY AND SCHEMA AUDIT FOR {len(tools)} SEEDS")
print("=" * 80)

for td in tools:
    rel = os.path.relpath(td, seed_root)
    with open(os.path.join(td, "tool.json"), "r", encoding="utf-8") as f:
        tdata = json.load(f)
    with open(os.path.join(td, "code.cs"), "r", encoding="utf-8") as f:
        code = f.read()
    with open(os.path.join(td, "examples.json"), "r", encoding="utf-8") as f:
        examples = json.load(f)

    props = list(tdata.get("inputSchema", {}).get("properties", {}).keys())
    reqs = list(tdata.get("inputSchema", {}).get("required", []))
    
    # find args usage
    used = re.findall(r'args\.[A-Za-z0-9_]+\(\s*"([^"]+)"', code)
    used += re.findall(r'args\[\s*"([^"]+)"\s*\]', code)
    used_set = set(used)
    props_set = set(props)

    diff1 = props_set - used_set
    diff2 = used_set - props_set

    status = "OK" if not diff1 and not diff2 else "FAIL"
    print(f"\n[{status}] {rel}")
    print(f"  Category: {tdata.get('category')}, Transaction: {tdata.get('transaction')}, Destructive: {tdata.get('destructive')}")
    print(f"  Declared Properties ({len(props)}): {props}")
    print(f"  Required Properties ({len(reqs)}): {reqs}")
    print(f"  Args read in code ({len(used_set)}): {sorted(list(used_set))}")
    print(f"  Examples count: {len(examples)}")
    if diff1:
        print(f"  [ERROR] Declared but unused: {diff1}")
    if diff2:
        print(f"  [ERROR] Used but undeclared: {diff2}")
