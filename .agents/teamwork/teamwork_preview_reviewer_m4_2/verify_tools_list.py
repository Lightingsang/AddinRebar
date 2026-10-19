import json
import os

path = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_reviewer_m4_2\tools_list.json"
with open(path, "r", encoding="utf-8") as f:
    data = json.load(f)

tools = data.get("result", {}).get("tools", [])
print(f"Total advertised tools: {len(tools)}")
assert len(tools) == 24, f"Expected 24 tools, got {len(tools)}"

core_tools = {"execute_dynamo_code", "execute_designscript", "get_dynamo_context", "cancel_execution"}
meta_tools = {"search_tools", "get_tool", "run_tool", "get_run", "propose_tool", "test_tool", "publish_tool", "manage_tool"}
seed_tools = {
    "run_dyn_file", "get_active_graph_info", "search_nodes_library", "inspect_node_type",
    "add_node_to_workspace", "connect_node_ports", "disconnect_node_ports", "set_node_properties",
    "get_node_output_values", "export_graph_to_image", "validate_dyn_file", "create_code_block_node"
}

names = {t["name"] for t in tools}

print(f"Missing core tools: {core_tools - names}")
assert core_tools.issubset(names)

print(f"Missing meta tools: {meta_tools - names}")
assert meta_tools.issubset(names)

print(f"Missing seed tools: {seed_tools - names}")
assert seed_tools.issubset(names)

assert "inspect_type" not in names, "ERROR: inspect_type must be suppressed!"

for t in tools:
    assert "inputSchema" in t, f"Missing inputSchema for {t['name']}"
    assert t["inputSchema"].get("type") == "object", f"inputSchema type not object for {t['name']}"
    desc = t.get("description", "")
    assert len(desc) > 0, f"Empty description for {t['name']}"
    print(f"OK: {t['name']:<25} | Schema props: {list(t['inputSchema'].get('properties', {}).keys())}")

print("\nALL 24 TOOLS STRICTLY VALIDATED!")
