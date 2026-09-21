import json
import os
import glob

base_dir = r"G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPRobot\HPRobot.Mcp.Server\Registry\SeedLibrary"
tool_files = glob.glob(os.path.join(base_dir, "*", "*", "tool.json"))

print(f"Discovered {len(tool_files)} tool.json files.")
assert len(tool_files) == 12, f"Expected 12 tools, got {len(tool_files)}"

for tf in sorted(tool_files):
    d = os.path.dirname(tf)
    tool_rel = os.path.relpath(d, base_dir).replace("\\", "/")
    ef = os.path.join(d, "examples.json")
    assert os.path.exists(ef), f"Missing examples.json in {tool_rel}"
    
    with open(tf, "r", encoding="utf-8") as f:
        tool_data = json.load(f)
    with open(ef, "r", encoding="utf-8") as f:
        ex_data = json.load(f)
        
    assert isinstance(ex_data, list), f"{tool_rel}: examples not a list"
    assert len(ex_data) >= 2, f"{tool_rel}: less than 2 examples (has {len(ex_data)})"
    
    schema = tool_data.get("inputSchema", {})
    props = set(schema.get("properties", {}).keys())
    required = set(schema.get("required", []))
    
    seen_titles = set()
    seen_args = []
    for i, ex in enumerate(ex_data):
        assert "title" in ex and ex["title"].strip(), f"{tool_rel} ex[{i}]: missing title"
        assert ex["title"] not in seen_titles, f"{tool_rel} ex[{i}]: duplicate title"
        seen_titles.add(ex["title"])

        assert "input" not in ex, f"{tool_rel} ex[{i}]: has forbidden 'input' key"
        assert "args" in ex and isinstance(ex["args"], dict), f"{tool_rel} ex[{i}]: missing 'args' dict"
        
        args = ex["args"]
        arg_keys = set(args.keys())
        
        missing_req = required - arg_keys
        assert not missing_req, f"{tool_rel} ex[{i}]: missing required {missing_req}"
        
        undeclared = arg_keys - props
        assert not undeclared, f"{tool_rel} ex[{i}]: undeclared properties {undeclared}"
        
        if props:  # Only check distinct args if tool takes parameters
            args_str = json.dumps(args, sort_keys=True)
            assert args_str not in seen_args, f"{tool_rel} ex[{i}]: duplicate args payload"
            seen_args.append(args_str)
        
    print(f"PASS: {tool_rel} ({len(ex_data)} distinct examples, all schema rules satisfied)")

print("\nAll 12 seeds verified successfully and 100% compliant!")
