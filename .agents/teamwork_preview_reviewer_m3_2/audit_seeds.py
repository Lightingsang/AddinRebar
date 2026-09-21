import os
import json
import re

seeds_dir = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPTekla\HPTekla.Mcp.Server\Registry\SeedLibrary"

tools = [
    ("Model", "get_model_info"),
    ("Model", "select_objects"),
    ("Property", "get_part_properties"),
    ("Geometry", "create_beam"),
    ("Geometry", "create_column"),
    ("Geometry", "create_contour_plate"),
    ("Rebar", "create_rebar_group"),
    ("Rebar", "create_single_rebar"),
    ("Property", "modify_user_properties"),
    ("Rebar", "get_reinforcement_info"),
    ("Drawing", "list_drawings"),
    ("Export", "export_ifc")
]

print(f"Auditing {len(tools)} seed tools in {seeds_dir}...")

results = []
all_passed = True

def validate_type(val, expected_type):
    if expected_type == "string":
        return isinstance(val, str)
    elif expected_type == "number":
        return isinstance(val, (int, float)) and not isinstance(val, bool)
    elif expected_type == "integer":
        return isinstance(val, int) and not isinstance(val, bool)
    elif expected_type == "boolean":
        return isinstance(val, bool)
    elif expected_type == "array":
        return isinstance(val, list)
    elif expected_type == "object":
        return isinstance(val, dict)
    return True

for cat, name in tools:
    t_dir = os.path.join(seeds_dir, cat, name)
    t_res = {"category": cat, "name": name, "errors": [], "warnings": [], "details": {}}
    if not os.path.isdir(t_dir):
        t_res["errors"].append(f"Directory missing: {t_dir}")
        results.append(t_res)
        all_passed = False
        continue

    # Check files
    f_tool = os.path.join(t_dir, "tool.json")
    f_ex = os.path.join(t_dir, "examples.json")
    f_code = os.path.join(t_dir, "code.cs")

    for fn, fp in [("tool.json", f_tool), ("examples.json", f_ex), ("code.cs", f_code)]:
        if not os.path.isfile(fp):
            t_res["errors"].append(f"Missing file: {fn}")

    # Check tool.json
    tool_data = None
    if os.path.isfile(f_tool):
        try:
            with open(f_tool, "r", encoding="utf-8") as f:
                tool_data = json.load(f)
            t_res["details"]["tool"] = tool_data
            
            if tool_data.get("name") != name:
                t_res["errors"].append(f"name mismatch: got '{tool_data.get('name')}', expected '{name}'")
            if tool_data.get("host") != "tekla":
                t_res["errors"].append(f"host mismatch: got '{tool_data.get('host')}', expected 'tekla'")
            if tool_data.get("hostVersions") != ["2025"]:
                t_res["errors"].append(f"hostVersions mismatch: got {tool_data.get('hostVersions')}, expected ['2025']")
            if tool_data.get("category") != cat:
                t_res["errors"].append(f"category mismatch: got '{tool_data.get('category')}', expected '{cat}'")
            if tool_data.get("transaction") not in ["none", "auto", "manual"]:
                t_res["errors"].append(f"transaction invalid: got '{tool_data.get('transaction')}'")
            if not isinstance(tool_data.get("timeoutSeconds"), int) or tool_data.get("timeoutSeconds") <= 0:
                t_res["errors"].append(f"timeoutSeconds invalid: got {tool_data.get('timeoutSeconds')}")
            
            schema = tool_data.get("inputSchema")
            if not schema or not isinstance(schema, dict) or schema.get("type") != "object":
                t_res["errors"].append("inputSchema must be an object with type: 'object'")
            else:
                props = schema.get("properties", {})
                req = schema.get("required", [])
                t_res["details"]["properties"] = list(props.keys())
                t_res["details"]["required"] = req
        except Exception as e:
            t_res["errors"].append(f"tool.json parse error: {e}")

    # Check examples.json
    ex_data = None
    if os.path.isfile(f_ex):
        try:
            with open(f_ex, "r", encoding="utf-8") as f:
                ex_data = json.load(f)
            t_res["details"]["examples_count"] = len(ex_data) if isinstance(ex_data, list) else 0
            if not isinstance(ex_data, list) or len(ex_data) < 1:
                t_res["errors"].append("examples.json must contain at least 1 example in an array")
            else:
                # Check each example
                for idx, ex in enumerate(ex_data):
                    if not isinstance(ex, dict):
                        t_res["errors"].append(f"Example {idx} is not an object")
                        continue
                    # Check if args is present
                    ex_args = ex.get("args") if "args" in ex else (ex.get("arguments") if "arguments" in ex else ex)
                    if not isinstance(ex_args, dict):
                        t_res["errors"].append(f"Example {idx} args is not a dict: {ex_args}")
                        continue
                    
                    # Validate against schema
                    if tool_data and "inputSchema" in tool_data:
                        schema = tool_data["inputSchema"]
                        req_props = schema.get("required", [])
                        schema_props = schema.get("properties", {})
                        allow_additional = schema.get("additionalProperties", True)

                        for rp in req_props:
                            if rp not in ex_args:
                                t_res["errors"].append(f"Example {idx} missing required parameter '{rp}'")
                        
                        if not allow_additional:
                            for ak in ex_args.keys():
                                if ak not in schema_props:
                                    t_res["errors"].append(f"Example {idx} contains undeclared property '{ak}' (additionalProperties=false)")

                        for ak, av in ex_args.items():
                            if ak in schema_props:
                                exp_type = schema_props[ak].get("type")
                                if exp_type and not validate_type(av, exp_type):
                                    t_res["errors"].append(f"Example {idx} property '{ak}' has value {av} which does not match schema type '{exp_type}'")

        except Exception as e:
            t_res["errors"].append(f"examples.json parse error: {e}")

    # Check code.cs
    if os.path.isfile(f_code):
        try:
            with open(f_code, "r", encoding="utf-8") as f:
                code_text = f.read()
            t_res["details"]["code_chars"] = len(code_text)
            t_res["details"]["code_lines"] = len(code_text.splitlines())

            # Return statement
            if not re.search(r"\breturn\b", code_text):
                t_res["errors"].append("code.cs missing return statement")

            # Check prohibited
            denied_items = [
                ("MessageBox", "MessageBox"),
                ("System.Windows.Forms", "System.Windows.Forms namespace"),
                ("Tekla.Structures.Dialog", "Tekla.Structures.Dialog namespace"),
                ("HPTekla.McpBridge", "HPTekla.McpBridge namespace"),
                ("HPRebar.McpBridge.Core.Host", "HPRebar.McpBridge.Core.Host namespace"),
                ("Process.Start", "Process.Start"),
                ("#r ", "#r directive"),
                ("#load ", "#load directive"),
            ]
            for needle, desc in denied_items:
                if needle in code_text:
                    t_res["errors"].append(f"code.cs contains prohibited {desc}")

            # Check model.CommitChanges()
            if re.search(r"\bmodel\s*\.\s*CommitChanges\s*\(", code_text):
                t_res["errors"].append("code.cs calls model.CommitChanges() directly (prohibited by GuardProfile.Tekla)")

            # Check parameter usage vs schema
            if tool_data and "inputSchema" in tool_data:
                props = set(tool_data["inputSchema"].get("properties", {}).keys())
                arg_calls = re.findall(r'args\s*\.\s*(?:Str|Int|Double|Bool|List|Obj(?:<[^>]+>)?)\s*\(\s*"([^"]+)"', code_text)
                t_res["details"]["args_accessed"] = arg_calls
                for ac in arg_calls:
                    if ac not in props:
                        t_res["warnings"].append(f"Code accesses argument '{ac}' which is NOT in tool.json inputSchema properties: {list(props)}")

        except Exception as e:
            t_res["errors"].append(f"code.cs read error: {e}")

    if t_res["errors"]:
        all_passed = False
    results.append(t_res)

print("\n" + "="*80)
print(f"AUDIT SUMMARY: {'ALL PASSED' if all_passed else 'FAILURES DETECTED'}")
print("="*80)
for r in results:
    status = "PASS" if not r["errors"] else "FAIL"
    print(f"\n[{status}] {r['category']}/{r['name']}")
    print(f"  Properties ({len(r['details'].get('properties', []))}): {r['details'].get('properties', [])}")
    print(f"  Required: {r['details'].get('required', [])}")
    print(f"  Examples: {r['details'].get('examples_count', 0)}")
    print(f"  Code: {r['details'].get('code_lines', 0)} lines, {r['details'].get('code_chars', 0)} chars")
    print(f"  Args accessed in code: {r['details'].get('args_accessed', [])}")
    if r["errors"]:
        print(f"  ERRORS: {r['errors']}")
    if r["warnings"]:
        print(f"  WARNINGS: {r['warnings']}")
