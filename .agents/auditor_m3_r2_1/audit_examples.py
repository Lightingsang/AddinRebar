import os
import json
import glob

seed_dir = r"HPRobot/HPRobot.Mcp.Server/Registry/SeedLibrary"
seeds = glob.glob(os.path.join(seed_dir, "*", "*"))
errors = []
count = 0

for s in sorted(seeds):
    tj_path = os.path.join(s, "tool.json")
    ex_path = os.path.join(s, "examples.json")
    if not os.path.isfile(tj_path) or not os.path.isfile(ex_path):
        continue
    count += 1
    rel = os.path.relpath(s, seed_dir).replace("\\", "/")
    with open(tj_path, "r", encoding="utf-8") as f:
        tj = json.load(f)
    with open(ex_path, "r", encoding="utf-8") as f:
        ex = json.load(f)
    
    if not isinstance(ex, list):
        errors.append(f"{rel}: examples.json is not a list")
        continue
    if len(ex) < 2:
        errors.append(f"{rel}: examples count < 2 (got {len(ex)})")
    
    schema = tj.get("inputSchema", {})
    props = set(k.lower() for k in schema.get("properties", {}).keys())
    reqs = set(k.lower() for k in schema.get("required", []))
    
    for i, item in enumerate(ex):
        if not isinstance(item, dict):
            errors.append(f"{rel} ex[{i}]: item is not a dict")
            continue
        if "title" not in item or not item["title"]:
            errors.append(f"{rel} ex[{i}]: missing title")
        if "input" in item:
            errors.append(f"{rel} ex[{i}]: uses forbidden key 'input'")
        if "args" not in item or not isinstance(item["args"], dict):
            errors.append(f"{rel} ex[{i}]: missing or invalid 'args'")
            continue
        args = item["args"]
        arg_keys = set(k.lower() for k in args.keys())
        missing_reqs = reqs - arg_keys
        if missing_reqs:
            errors.append(f"{rel} ex[{i}]: missing required args: {missing_reqs}")
        undeclared = arg_keys - props
        if undeclared:
            errors.append(f"{rel} ex[{i}]: undeclared args: {undeclared}")

print(f"Audited {count} seeds.")
print(f"Total schema errors found: {len(errors)}")
for e in errors:
    print("  FAIL:", e)
if not errors:
    print("ALL 12 SEED EXAMPLES COMPLY 100% WITH CONTRACT!")
